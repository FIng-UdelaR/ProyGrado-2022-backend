using CloudManufacturingAPI.Models.SystemManagement;
using CloudManufacturingSharedLibrary;
using CloudManufacturingSharedLibrary.Models;
using System.Collections.Generic;

namespace CloudManufacturingAPI.Repositories.SystemManagement
{
    public interface IScheduler
    {
        /// <summary>
        /// Schedules new work among all the machines supporting the tasks.
        /// If there's no way to do the job with the available machines, returns an exception.
        /// </summary>
        /// <param name="workloads"></param>
        /// <param name="newOrder"></param>
        /// <returns></returns>
        ScheduleWorkloadResultDTO ScheduleWork(IEnumerable<Workload> workloads, bool newOrder = false);
        /// <summary>
        /// One item for a given order is momentarily not supported. Cancel the rest of the work for the order
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="machineIds"></param>
        void RemoveOrderFromMachines(string orderId, IEnumerable<int> machineIds = null);
        /// <summary>
        /// There's a new machine available that needs to be taken into account for the current workloads.
        /// This method redistributes the current work between the available machines that can handle it.
        /// </summary>
        /// <param name="machineId"></param>
        void MachineWokeUp(int machineId);
        /// <summary>
        /// Returns the current scheduling method
        /// </summary>
        /// <returns></returns>
        Constants.SCHEDULING_METHOD GetSchedulingMethod();
        /// <summary>
        /// Updates the scheduling preferred algorithm
        /// </summary>
        /// <param name="schedulingMethod"></param>
        void SetSchedulingMethod(Constants.SCHEDULING_METHOD schedulingMethod);
    }
}
