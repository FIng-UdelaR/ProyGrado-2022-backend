using CloudManufacturingAPI.Models.Machine;
using CloudManufacturingAPI.Models.SystemManagement;
using CloudManufacturingAPI.Models.Work;
using CloudManufacturingAPI.Repositories.Machine;
using CloudManufacturingAPI.Repositories.SystemManagement;
using CloudManufacturingSharedLibrary.Models;
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

namespace CloudManufacturingAPI.Repositories.Work
{
    public class WorkRepository : IWorkRepository
    {
        private readonly IScheduler _scheduler;
        private readonly IMachineHttpClient _httpClient;
        private readonly IMachineRepository _machineRepository;
        static CloudManufacturingDBAccess.DBAccess DBAccess;
        private readonly ILogger<WorkRepository> _logger;

        public WorkRepository(ILogger<WorkRepository> logger, IScheduler scheduler, IMachineHttpClient httpClient, IMachineRepository machineRepository)
        {
            _logger = logger;
            _scheduler = scheduler;
            _httpClient = httpClient;
            _machineRepository = machineRepository;
            DBAccess = CloudManufacturingDBAccess.DBAccess.GetInstance();
        }

        public ScheduleWorkloadResultDTO AddWork(IEnumerable<AddWorkDTO> newWork)
        {
            if (newWork == null) throw new ArgumentException("Invalid null object");
            if (!newWork.Any()) throw new ArgumentException("Must specify at least one work item");

            var orderId = Guid.NewGuid().ToString();
            List<Workload> workload = new List<Workload>();
            int itemId = 1;
            foreach (var item in newWork)
            {
                workload.Add(new Workload()
                {
                    ItemId = itemId,
                    Material = (MATERIAL)item.Material,
                    Quality = (QUALITY)item.Quality,
                    Size = (SIZE)item.Size,
                    OrderArrivalTime = DateTime.UtcNow,
                    OrderId = orderId,
                    Priority = 0,
                    SourceLatitude = item.Latitude,
                    SourceLongitude = item.Longitude
                });
                itemId++;
            }
            var schedulerResult = _scheduler.ScheduleWork(workload, newOrder:true);
            schedulerResult.OrderId = orderId;
            return schedulerResult;
        }

        public IEnumerable<OrderDTO> GetOrders(int? start, int? length, string order, string orderId, out int filteredRecords, out int totalRecords)
        {
            var result = DBAccess.GetOrders(out filteredRecords, out totalRecords, start ?? default, length ?? default, order, orderId);
            return result?.Select(r=>OrderDTO.FromDBO(r, _machineRepository.Get()));
        }

        public async Task<IEnumerable<Workload>> GetWorkload(MachineDTO machine)
        {
            try
            {
                var objectForBody = JsonConvert.SerializeObject(new
                {
                    requestId = Guid.NewGuid().ToString(),
                    inputArguments = new List<object>()
                });

                var response = await _httpClient.PostAsync(
                    machine.Uri + "/submodel/operations/GetWorkLoad",
                    new StringContent(objectForBody,
                    Encoding.UTF8,
                    "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var contents = await response.Content.ReadAsStringAsync();
                    var parsedJsonIntoJObject = JObject.Parse(contents);
                    var stringWorkload = parsedJsonIntoJObject["outputArguments"][0]["value"]["value"].ToString();

                    var result = JsonConvert.DeserializeObject<List<Workload>>(stringWorkload);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception getting machine workload for {machine.Uri}: {ex}");
            }
            return null;
        }

        public void RemoveWork(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) throw new ArgumentException("Invalid empty OrderId");
            _scheduler.RemoveOrderFromMachines(orderId);
        }

        public async Task<bool> RemoveAllWorkload(MachineDTO machine)
        {
            try
            {
                var objectForBody = JsonConvert.SerializeObject(new
                {
                    requestId = Guid.NewGuid().ToString(),
                    inputArguments = new List<object>()
                });

                var response = await _httpClient.PostAsync(
                    machine.Uri + "/submodel/operations/RemoveAllWorkLoad",
                    new StringContent(objectForBody,
                    Encoding.UTF8,
                    "application/json"));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception removing all machine workload for {machine.Uri}: {ex}");
            }
            return false;
        }
    }
}
