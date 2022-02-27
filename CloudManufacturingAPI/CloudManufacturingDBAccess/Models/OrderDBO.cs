using CloudManufacturingSharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudManufacturingDBAccess.Models
{
    public class OrderDBO
    {
        public string Id { get; set; }
        public DateTime ArrivalDate { get; set; }
        public DateTime EstimatedCompletionDate { get; set; }
        public IEnumerable<Workload> Workloads { get; set; }
        public IEnumerable<int> MachineIds { get; set; }

        internal bool ValidateInsert()
        {
            return !string.IsNullOrWhiteSpace(Id)
                && MachineIds != null
                && MachineIds.Any()
                && Workloads != null 
                && Workloads.Any();
        }

        internal bool ValidateUpdate()
        {
            return true;
        }
    }
}
