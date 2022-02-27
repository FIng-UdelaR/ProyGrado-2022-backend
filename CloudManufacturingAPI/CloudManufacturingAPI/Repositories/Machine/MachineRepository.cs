using CloudManufacturingAPI.Models.Machine;
using CloudManufacturingAPI.Repositories.SystemManagement;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingAPI.Repositories.Machine
{
    public class MachineRepository : IMachineRepository
    {
        private readonly ILogger<MachineRepository> _logger;
        static CloudManufacturingDBAccess.DBAccess DBAccess;
        private readonly List<MachineDTO> _machines;
        private readonly IMachineHttpClient _httpClient;
        private DateTime MachineListMustReload = DateTime.UtcNow;
        private DateTime MachineListLastReloaded = new DateTime();

        public MachineRepository(ILogger<MachineRepository> logger, IMachineHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _machines = new List<MachineDTO>();
            DBAccess = CloudManufacturingDBAccess.DBAccess.GetInstance();
            ReloadMachines();
        }

        public IEnumerable<MachineDTO> Get(int? material = null, bool onlyAvailableOnes = false)
        {
            if (MachineListLastReloaded < MachineListMustReload)
                ReloadMachines();
            var result = material.HasValue
                    ? _machines.Where(_m => _m.SupportedMaterial == material.Value)
                    : _machines;

            return onlyAvailableOnes
                    ? result.Where(_m => _m.Available)
                    : result;
        }

        public MachineDTO Create(MachineCreationDTO item)
        {
            try
            {
                if (item == null) throw new ArgumentException("Invalid null object");
                if (item.SupportedSizes == null || !item.SupportedSizes.Any()) throw new ArgumentException("Must specify Supported Sizes");
                if (item.SupportedQualities == null || !item.SupportedQualities.Any()) throw new ArgumentException("Must specify Supported Qualities");
                if (item.Latitude == 0 || item.Longitude == 0) throw new ArgumentException("Must specify the machine location");
                if (string.IsNullOrWhiteSpace(item.MachineName)) item.MachineName = "Default";

                int portNumber = _machines.Any() ? _machines.Max(m => m.PortNumber) + 1 : 5000;

                CloudManufacturingDBAccess.Models.MachineDBO machineDBO = new CloudManufacturingDBAccess.Models.MachineDBO()
                {
                    Name = CheckMachineName(item.MachineName),
                    Location = new CloudManufacturingDBAccess.Models.Location() { Latitude = item.Latitude, Longitude = item.Longitude },
                    SupportedMaterial = item.SupportedMaterial,
                    SupportedQualities = item.SupportedQualities,
                    SupportedSizes = item.SupportedSizes,
                    PortNumber = portNumber,
                    Uri = $"http://localhost:{portNumber}"
                };

                //Insert the new machine into the DB for BaSyx to execute the Digital Twin
                DBAccess.InsertMachine(machineDBO);

                AddMachineToList(machineDBO);
                //Start taking into account the new machine 5 seconds after the last one was created.
                //If we're creating machines in bulk, then, we'll refresh the list once they're all created.
                MachineListMustReload = DateTime.UtcNow.AddSeconds(5);
                return new MachineDTO(machineDBO, available: false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception Creating Machine: {ex}");
                throw;
            }
        }

        public async Task<MACHINE_STATUS> GetMachineStatus(string baseUri)
        {
            try
            {
                var response = await _httpClient.GetAsync(baseUri + "/submodel/properties/MachineStatus/value");
                var contents = await response.Content.ReadAsStringAsync();
                var parsedJsonIntoJObject = JObject.Parse(contents);
                string statusStr = parsedJsonIntoJObject["value"].ToString();
                return statusStr.ToUpper() switch
                {
                    "AVAILABLE" => MACHINE_STATUS.AVAILABLE,
                    "NEEDS_MAINTENANCE" => MACHINE_STATUS.NEEDS_MAINTENANCE,
                    "WORKING" => MACHINE_STATUS.WORKING,
                    _ => MACHINE_STATUS.OFFLINE,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception getting machine status for {baseUri}: {ex}");
                return MACHINE_STATUS.OFFLINE;
            }
        }

        public async Task SetMachineStatus(MachineDTO machine, MACHINE_STATUS newStatus)
        {
            try
            {
                var objectForBody = JsonConvert.SerializeObject(new
                {
                    valueType = new
                    {
                        dataObjectType = new
                        {
                            name = ""
                        },
                        value = (int)newStatus
                    }
                });

                var response = await _httpClient.PutAsync(
                    machine.Uri + "/submodel/properties/MachineStatus/value",
                    new StringContent(objectForBody,
                    Encoding.UTF8,
                    "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    if (!machine.Available &&
                        (newStatus == MACHINE_STATUS.AVAILABLE || newStatus == MACHINE_STATUS.WORKING))
                        SetMachineAvailability(machine.Id, true);
                    else if (machine.Available &&
                        (newStatus == MACHINE_STATUS.NEEDS_MAINTENANCE || newStatus == MACHINE_STATUS.OFFLINE))
                        SetMachineAvailability(machine.Id, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception setting machine status for {machine.Uri}: {ex}");
            }
        }

        public void SetMachineAvailability(int id, bool availability)
        {
            var machine = _machines.FirstOrDefault(m => m.Id == id);
            if (machine != null) machine.Available = availability;
            else _logger.LogWarning($"Machine with id={id} not found.");
        }

        public DateTime? GetMachineEstimatedCompletionDate(int id)
        {
            var machine = _machines.FirstOrDefault(m => m.Id == id);
            try
            {
                if (machine != null)
                {
                    var objectForBody = JsonConvert.SerializeObject(new
                    {
                        requestId = Guid.NewGuid().ToString(),
                        inputArguments = new List<object>()
                    });

                    var response = _httpClient.PostAsync(
                        machine.Uri + "/submodel/operations/GetEstimatedCompletionDate",
                        new StringContent(objectForBody,
                        Encoding.UTF8,
                        "application/json")).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var contents = response.Content.ReadAsStringAsync().Result;
                        var parsedJsonIntoJObject = JObject.Parse(contents);
                        var stringDate = parsedJsonIntoJObject["outputArguments"][0]["value"]["value"].ToString();

                        return !string.IsNullOrWhiteSpace(stringDate) ? DateTime.Parse(stringDate) : (DateTime?)null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception setting machine status for {machine.Uri}: {ex}");
            }
            return DateTime.MaxValue; //The machine is not responding
        }

        public void SetMachineWorkload(int id, double totalMinutes)
        {
            if (totalMinutes < 0) totalMinutes = 0;
            var machine = _machines.FirstOrDefault(m => m.Id == id);
            if (machine != null) machine.Workload = totalMinutes;
            else _logger.LogWarning($"Machine with id={id} not found.");
        }

        #region PRIVATE METHODS
        private void AddMachineToList(CloudManufacturingDBAccess.Models.MachineDBO machine, bool available = false)
        {
            if (machine.Id == 0) machine.Id = _machines.Any() ? _machines.Max(m => m.Id) + 1 : 1;
            _machines.Add(new MachineDTO(machine, available: available));
        }

        private void ReloadMachines()
        {
            var machinesInDB = DBAccess.GetMachines();
            if (machinesInDB == null || !machinesInDB.Any())
                _machines.Clear();
            else
            {
                bool firstStart = !_machines.Any();
                var machinesToAdd = machinesInDB.Where(m => !_machines.Select(_m => _m.Id).Contains(m.Id));
                var machinesToRemove = _machines.Where(_m => !machinesInDB.Select(m => m.Id).Contains(_m.Id));
                foreach (var machine in machinesToAdd)
                {
                    //Every time the API starts, the machines are available
                    _machines.Add(new MachineDTO(machine, available: firstStart));
                }

                if (machinesToRemove.Any())
                    _machines.RemoveAll(_m => machinesToRemove.Select(m => m.Id).Contains(_m.Id));
            }
            MachineListLastReloaded = DateTime.UtcNow;
        }

        private string CheckMachineName(string name)
        {
            string result = "";

            if (name == null) name = "";
            var nameAsList = name.ToList();
            foreach (var character in nameAsList)
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                    result += character;
                else if (character == ' ')
                    result += "_";
            }
            if (string.IsNullOrWhiteSpace(result) || result.ToLower() == "default")
            {
                int portNumber = _machines.Any() ? _machines.Max(m => m.PortNumber) + 1 : 5000;
                result = $"Printer Machine For Port {portNumber}";
            }

            if (char.IsDigit(result[0])) result = "a" + result;

            return result;
        }

        public void ClearMemory()
        {
            _machines.Clear();
        }
        #endregion PRIVATE METHODS
    }
}
