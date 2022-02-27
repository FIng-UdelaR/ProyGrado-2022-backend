using CloudManufacturingAPI.Models.Machine;
using CloudManufacturingDBAccess.Models;
using CloudManufacturingSharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudManufacturingAPI.Models.Work
{
    /// <summary>
    /// Order representation model
    /// </summary>
    public class OrderDTO
    {
        public string Id { get; set; }
        public DateTime ArrivalDate { get; set; }
        public DateTime EstimatedCompletionDate { get; set; }
        public IEnumerable<Workload> Workloads { get; set; }
        public IEnumerable<MachineDTO> Machines { get; set; }

        /// <summary>
        /// Creates a new OrderDTO object from an OrderDBO one.
        /// </summary>
        /// <param name="orderDBO"></param>
        /// <returns></returns>
        internal static OrderDTO FromDBO(OrderDBO orderDBO, IEnumerable<MachineDTO> machineList)
        {
            if (orderDBO == null) return null;
            return new OrderDTO()
            {
                ArrivalDate = orderDBO.ArrivalDate,
                EstimatedCompletionDate = orderDBO.EstimatedCompletionDate,
                Id = orderDBO.Id,
                Machines = machineList.Where(m => orderDBO.MachineIds.Contains(m.Id)),
                Workloads = orderDBO.Workloads ?? new List<Workload>()
            };
        }
    }
}
