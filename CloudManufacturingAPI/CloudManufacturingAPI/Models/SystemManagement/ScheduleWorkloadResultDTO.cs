using CloudManufacturingAPI.Models.Machine;
using System;
using System.Collections.Generic;

namespace CloudManufacturingAPI.Models.SystemManagement
{
    public class ScheduleWorkloadResultDTO
    {
        public string OrderId { get; set; }
        public bool Success { get; set; }
        public DateTime ExpectedCompletionDate { get; set; }
        public IEnumerable<MachineDTO> Machines { get; set; }

        public ScheduleWorkloadResultDTO()
        {
            Success = false;
            ExpectedCompletionDate = DateTime.MaxValue;
        }
    }
}
