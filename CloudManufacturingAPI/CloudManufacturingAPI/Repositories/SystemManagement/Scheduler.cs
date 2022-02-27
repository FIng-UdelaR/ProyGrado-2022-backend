using CloudManufacturingAPI.Models.Machine;
using CloudManufacturingAPI.Models.SystemManagement;
using CloudManufacturingAPI.Repositories.Machine;
using CloudManufacturingSharedLibrary;
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

namespace CloudManufacturingAPI.Repositories.SystemManagement
{
    public class Scheduler : IScheduler
    {
        private readonly ILogger<Scheduler> _logger;
        private readonly IMachineRepository _machineRepository;
        private readonly IMachineHttpClient _machineHttpClient;
        static CloudManufacturingDBAccess.DBAccess DBAccess;
        private static Constants.SCHEDULING_METHOD _schedulingMethod = Constants.SCHEDULING_METHOD.LESS_WORKLOAD;

        public Scheduler(ILogger<Scheduler> logger, IMachineRepository machineRepository, IMachineHttpClient machineHttpClient)
        {
            _logger = logger;
            _machineRepository = machineRepository;
            _machineHttpClient = machineHttpClient;
            DBAccess = CloudManufacturingDBAccess.DBAccess.GetInstance();
        }

        public ScheduleWorkloadResultDTO ScheduleWork(IEnumerable<Workload> workloads, bool newOrder = false)
        {
            //Workloads for the same order
            if (workloads == null) throw new ArgumentException("Invalid null object");
            if (!workloads.Any()) throw new ArgumentException("Must specify workload");

            ScheduleWorkloadResultDTO result = new ScheduleWorkloadResultDTO();
            DateTime estimatedCompletionDate = new DateTime();

            IEnumerable<int> materials = workloads.Select(x => (int)x.Material).Distinct();
            List<MachineDTO> machinesUsedForThisOrder = new List<MachineDTO>();

            foreach (var material in materials)
            {
                //Get the machines that may do the work (available and compatible)
                IEnumerable<MachineDTO> machinesForMaterial = _machineRepository.Get(material: material, onlyAvailableOnes: true);
                if (!machinesForMaterial.Any())
                {
                    RemoveOrderFromMachines(workloads.First().OrderId, machinesUsedForThisOrder.Select(m => m.Id));
                    if (Enum.IsDefined(typeof(Constants.MATERIAL), material))
                        throw new InvalidOperationException($"Material {Enum.GetName(typeof(Constants.MATERIAL), material)} " +
                            $"momentarily not supported");
                    else throw new InvalidOperationException($"Id {material} is not a valid material code.");
                }

                //Execute the configured algorithm
                switch (_schedulingMethod)
                {
                    case Constants.SCHEDULING_METHOD.LESS_WORKLOAD:
                        Scheduler_LessWorkload(workloads, material, machinesForMaterial, ref machinesUsedForThisOrder, ref estimatedCompletionDate);
                        break;
                    case Constants.SCHEDULING_METHOD.FIFO:
                        Scheduler_FIFO(workloads, material, machinesForMaterial, ref machinesUsedForThisOrder, ref estimatedCompletionDate);
                        break;
                }
            }

            //return the expected finalization time
            result.Success = true;
            result.ExpectedCompletionDate = estimatedCompletionDate == new DateTime() ? DateTime.MaxValue : estimatedCompletionDate;
            result.Machines = machinesUsedForThisOrder;
            if (newOrder) //TODO: review this.
                DBAccess.InsertOrder(new CloudManufacturingDBAccess.Models.OrderDBO()
                {
                    EstimatedCompletionDate = result.ExpectedCompletionDate,
                    Id = workloads.First().OrderId,
                    MachineIds = machinesUsedForThisOrder.Select(m => m.Id),
                    Workloads = workloads,
                    ArrivalDate = DateTime.UtcNow
                });
            else
            {
                DBAccess.UpdateOrderMachine(workloads.First().OrderId, null, machinesUsedForThisOrder.Select(m => m.Id));
                DBAccess.UpdateOrderEstimatedCompletionDate(workloads.First().OrderId, result.ExpectedCompletionDate);
            }
            return result;
        }

        public void RemoveOrderFromMachines(string orderId, IEnumerable<int> machineIds = null)
        {
            var machines = _machineRepository.Get();
            if (!machines.Any()) return;
            if (machineIds != null && !machineIds.Any()) return; //The first machine for this order was not compatible, therefore there's nothing to cancel
            if (machineIds != null) machines = machines.Where(m => machineIds.Contains(m.Id));

            foreach (var machine in machines)
            {
                var objectForBody = JsonConvert.SerializeObject(new
                {
                    requestId = Guid.NewGuid().ToString(),
                    inputArguments = new List<object>() {
                        new {
                            modelType = new {name = "OperationVariable" },
                            value = new {
                                idShort = "OrderIdsToRemove",
                                modelType = new { name = "Property"},
                                valueType = new { dataObjectType = new { name = "string" } },
                                value = new List<string>(){ orderId }
                            }
                        }
                    }
                });
                _machineHttpClient.PostAsync(
                    machine.Uri + "/submodel/operations/RemoveWorkLoad",
                    new StringContent(objectForBody,
                    Encoding.UTF8,
                    "application/json"));
            }
        }

        public void MachineWokeUp(int machineId)
        {
            var machines = _machineRepository.Get(onlyAvailableOnes: true);
            var newMachine = machines.FirstOrDefault(m => m.Id == machineId);
            if (newMachine != null)
            {
                var machinesWithSameMaterial = machines.Where(m => m.Id != machineId && m.SupportedMaterial == newMachine.SupportedMaterial).ToList();
                if (machinesWithSameMaterial.Any())
                {
                    //Remove the current workload for every machine
                    List<Workload> workloads = new List<Workload>();
                    foreach (var machine in machinesWithSameMaterial)
                    {
                        workloads.AddRange(RemoveWorkload(machine).Result);
                    }

                    if (workloads.Any())
                    {
                        //Group workloads by OrderId and reschedula the work between all the machines that supports this material
                        var orderIds = workloads.Select(wl => wl.OrderId).Distinct();
                        foreach (var orderId in orderIds)
                        {
                            ScheduleWork(workloads.Where(wl => wl.OrderId == orderId));
                        }
                    }
                }
            }
        }

        public Constants.SCHEDULING_METHOD GetSchedulingMethod()
        {
            return _schedulingMethod;
        }

        public void SetSchedulingMethod(Constants.SCHEDULING_METHOD schedulingMethod)
        {
            _schedulingMethod = schedulingMethod;
        }

        #region PRIVATE METHODS
        /// <summary>
        /// Adds a workload to the machine and returns the EstimatedCompletionTime
        /// </summary>
        /// <param name="workload"></param>
        /// <param name="machine"></param>
        /// <returns></returns>
        private async Task<DateTime> AddWorkload(Workload workload, MachineDTO machine)
        {
            var objectForBody = JsonConvert.SerializeObject(new
            {
                requestId = Guid.NewGuid().ToString(),
                inputArguments = new List<object>() {
                        new {
                            modelType = new {name = "OperationVariable" },
                            value = new {
                                idShort = "NewWorkLoad",
                                modelType = new { name = "Property"},
                                valueType = new { dataObjectType = new { name = "anytype" } },
                                value = new List<Workload>(){ workload }
                            }
                        }
                    }
            });

            var response = await _machineHttpClient.PostAsync(
                machine.Uri + "/submodel/operations/AddWorkLoad",
                new StringContent(objectForBody,
                Encoding.UTF8,
                "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var contents = await response.Content.ReadAsStringAsync();
                var parsedJsonIntoJObject = JObject.Parse(contents);
                var stringDate = parsedJsonIntoJObject["outputArguments"][0]["value"]["value"].ToString();

                var estimatedCompletionDate = DateTime.Parse(stringDate);

                _machineRepository.SetMachineWorkload(
                                machine.Id,
                                (estimatedCompletionDate - DateTime.UtcNow).TotalMinutes);
                return estimatedCompletionDate;
            }
            _logger.LogWarning($"Failed to add workload to machine {machine.Uri}. Object: {objectForBody}");
            return DateTime.MaxValue;
        }

        /// <summary>
        /// Removes the machine's not started workload and returns the list of tasks removed.
        /// Also, updates the machine's EstimatedCompletionDate
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        private async Task<List<Workload>> RemoveWorkload(MachineDTO machine)
        {
            var objectForBody = JsonConvert.SerializeObject(new
            {
                requestId = Guid.NewGuid().ToString(),
                inputArguments = new List<object>()
            });

            var response = await _machineHttpClient.PostAsync(
                machine.Uri + "/submodel/operations//RemoveAndReturnNotStartedWorkLoad",
                new StringContent(objectForBody,
                Encoding.UTF8,
                "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var contents = await response.Content.ReadAsStringAsync();
                var parsedJsonIntoJObject = JObject.Parse(contents);
                var stringResult = parsedJsonIntoJObject["outputArguments"][0]["value"]["value"].ToString();

                var removedWorkload = JsonConvert.DeserializeObject<List<Workload>>(stringResult);

                var estimatedCompletionDate = _machineRepository.GetMachineEstimatedCompletionDate(machine.Id);
                _machineRepository.SetMachineWorkload(
                                machine.Id,
                                (estimatedCompletionDate.Value - DateTime.UtcNow).TotalMinutes);

                return removedWorkload;
            }
            _logger.LogWarning($"Failed to remove workload to machine {machine.Uri}. Object: {objectForBody}");
            return new List<Workload>();
        }

        private void Scheduler_FIFO(
            IEnumerable<Workload> workloads,
            int material,
            IEnumerable<MachineDTO> machinesForMaterial,
            ref List<MachineDTO> machinesUsedForThisOrder,
            ref DateTime estimatedCompletionDate)
        {
            _logger.LogInformation($"Executing FIFO algorithm");
            foreach (var work in workloads.Where(wl => (int)wl.Material == material))
            {
                //Sort by distance, quality and size, take the closest machine
                var selectedMachine = machinesForMaterial
                                        .Where(m => m.SupportedQualities.Contains((int)work.Quality)
                                                    && m.SupportedSizes.Contains((int)work.Size))
                                        .OrderBy(m => Helpers.DistanceTo(work.SourceLatitude,
                                                                                       work.SourceLongitude,
                                                                                       m.Latitude,
                                                                                       m.Longitude))
                                        .FirstOrDefault();
                if (selectedMachine == null)
                {
                    RemoveOrderFromMachines(work.OrderId, machinesUsedForThisOrder.Select(m => m.Id));
                    throw new InvalidOperationException($"Size {Enum.GetName(typeof(Constants.SIZE), work.Size)} " +
                        $"and Quality {Enum.GetName(typeof(Constants.QUALITY), work.Quality)} momentarily not supported " +
                        $"for material {Enum.GetName(typeof(Constants.MATERIAL), material)}");
                }

                //Set the new workload 
                var completionTimeForMachine = AddWorkload(work, selectedMachine).Result;
                if (completionTimeForMachine > estimatedCompletionDate) estimatedCompletionDate = completionTimeForMachine;
                if (!machinesUsedForThisOrder.Any(m => m.Id == selectedMachine.Id)) machinesUsedForThisOrder.Add(selectedMachine);
                work.EstimatedCompletionDate = completionTimeForMachine;
            }
        }

        private void Scheduler_LessWorkload(
            IEnumerable<Workload> workloads,
            int material,
            IEnumerable<MachineDTO> machinesForMaterial,
            ref List<MachineDTO> machinesUsedForThisOrder,
            ref DateTime estimatedCompletionDate)
        {
            _logger.LogInformation($"Executing LESS_WORKLOAD algorithm");
            foreach (var work in workloads.Where(wl => (int)wl.Material == material))
            {
                //Sort by distance, quality and size
                var nearestCompatibleMachines = machinesForMaterial
                                        .Where(m => m.SupportedQualities.Contains((int)work.Quality)
                                                    && m.SupportedSizes.Contains((int)work.Size))
                                        .OrderBy(m => Helpers.DistanceTo(work.SourceLatitude,
                                                                                       work.SourceLongitude,
                                                                                       m.Latitude,
                                                                                       m.Longitude))
                                        .Take(Constants.MAX_MACHINES_TAKEN_INTO_ACCOUNT_SCHEDULER);
                if (!nearestCompatibleMachines.Any())
                {
                    RemoveOrderFromMachines(work.OrderId, machinesUsedForThisOrder.Select(m => m.Id));
                    throw new InvalidOperationException($"Size {Enum.GetName(typeof(Constants.SIZE), work.Size)} " +
                        $"and Quality {Enum.GetName(typeof(Constants.QUALITY), work.Quality)} momentarily not supported " +
                        $"for material {Enum.GetName(typeof(Constants.MATERIAL), material)}");
                }

                //Sort for workload, select the machine and set the new workload 
                var selectedMachine = nearestCompatibleMachines.OrderBy(m => m.Workload).First();
                var completionTimeForMachine = AddWorkload(work, selectedMachine).Result;
                if (completionTimeForMachine > estimatedCompletionDate) estimatedCompletionDate = completionTimeForMachine;
                if (!machinesUsedForThisOrder.Any(m => m.Id == selectedMachine.Id)) machinesUsedForThisOrder.Add(selectedMachine);
                work.EstimatedCompletionDate = completionTimeForMachine;
            }
        }
        #endregion PRIVATE METHODS
    }
}
