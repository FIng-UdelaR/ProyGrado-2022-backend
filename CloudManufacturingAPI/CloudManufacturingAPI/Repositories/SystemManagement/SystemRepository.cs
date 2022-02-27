using CloudManufacturingAPI.Repositories.Machine;
using CloudManufacturingAPI.Repositories.Work;
using CloudManufacturingSharedLibrary;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingAPI.Repositories.SystemManagement
{
    public class SystemRepository : ISystemRepository
    {
        private readonly IMachineRepository _machineRepository;
        private readonly IWorkRepository _workRepository;
        private readonly IScheduler _scheduler;
        static CloudManufacturingDBAccess.DBAccess DBAccess;
        private readonly ILogger<SystemRepository> _logger;
        private readonly bool _runMonitoringAsService = false;

        public SystemRepository(ILogger<SystemRepository> logger, IMachineRepository machineRepository, IWorkRepository workRepository, IScheduler scheduler)
        {
            _logger = logger;
            _machineRepository = machineRepository;
            _workRepository = workRepository;
            _scheduler = scheduler;
            DBAccess = CloudManufacturingDBAccess.DBAccess.GetInstance();
        }

        public void RunMonitoring()
        {
            var machines = _machineRepository.Get();
            foreach (var machine in machines)
            {
                //Check machine status
                MACHINE_STATUS machineStatus = _machineRepository.GetMachineStatus(machine.Uri).Result;

                if (machineStatus == MACHINE_STATUS.OFFLINE && machine.Available)
                {
                    //If the machine was available, set as unavailable
                    machine.Available = false;
                    _machineRepository.SetMachineAvailability(machine.Id, machine.Available);
                    //Reschedule this machine's work (if any)
                    var workload = _workRepository.GetWorkload(machine).Result;
                    if (workload != null && workload.Any())
                    {
                        IEnumerable<string> orderIds = workload.Select(wl => wl.OrderId).Distinct();
                        _ = _workRepository.RemoveAllWorkload(machine).Result; //Remove the entire workload that won't be processed and will be reasigned
                        foreach (var orderId in orderIds)
                        {
                            bool success = false;
                            try
                            {
                                var reschedulerResult = _scheduler.ScheduleWork(workload.Where(wl => wl.OrderId == orderId));
                                success = reschedulerResult.Success;
                                if (success) DBAccess.UpdateOrderMachine(orderId, new List<int>() { machine.Id }, null);
                            }
                            catch (Exception ex) { }
                            if (!success)
                                _logger.LogWarning($"The work for order {orderId} could not be rescheduled.");
                        }
                    }
                }
                else if (machineStatus == MACHINE_STATUS.NEEDS_MAINTENANCE)
                {
                    //Do not accept more work (if not, set MachineDTO as unavailable)
                    if (machine.Available)
                    {
                        machine.Available = false;
                        _machineRepository.SetMachineAvailability(machine.Id, machine.Available);
                    }
                    //If there's no work, set the machine offline (to fix it)
                    var workload = _workRepository.GetWorkload(machine).Result;
                    if (workload == null || !workload.Any())
                        _ = _machineRepository.SetMachineStatus(machine, MACHINE_STATUS.OFFLINE);
                }
                else if ((machineStatus == MACHINE_STATUS.AVAILABLE || machineStatus == MACHINE_STATUS.WORKING)
                    && !machine.Available)
                {
                    machine.Available = true;
                    //If not, set MachineDTO as available
                    _machineRepository.SetMachineAvailability(machine.Id, machine.Available);
                    try
                    {
                        _scheduler.MachineWokeUp(machine.Id); //Reschedule work taking into account this new machine
                    }
                    catch (Exception ex) { }
                }

                //Update machine estimated completion date
                var estimatedCompletionDate = _machineRepository.GetMachineEstimatedCompletionDate(machine.Id);
                _machineRepository.SetMachineWorkload(
                    machine.Id,
                    estimatedCompletionDate.HasValue
                        ? (estimatedCompletionDate.Value - DateTime.UtcNow).TotalMinutes
                        : 0);
            }
        }

        public SCHEDULING_METHOD GetSchedulingMethod()
        {
            return _scheduler.GetSchedulingMethod();
        }

        public void SetSchedulingMethod(SCHEDULING_METHOD schedulingMethod)
        {
            _scheduler.SetSchedulingMethod(schedulingMethod);
        }

        public bool GetRunMonitoringAsService()
        {
            return _runMonitoringAsService;
        }

        public void ClearSystemMemory(bool setDefaultSchedulingMethod = false)
        {
            _machineRepository.ClearMemory();
            if(setDefaultSchedulingMethod)
                _scheduler.SetSchedulingMethod(SCHEDULING_METHOD.LESS_WORKLOAD);
        }
    }
}
