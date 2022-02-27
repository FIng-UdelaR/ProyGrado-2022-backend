using CloudManufacturingAPI.Models.Machine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingAPI.Repositories.Machine
{
    public interface IMachineRepository
    {
        IEnumerable<MachineDTO> Get(int? material = null, bool onlyAvailableOnes = false);
        MachineDTO Create(MachineCreationDTO item);
        Task<MACHINE_STATUS> GetMachineStatus(string baseUri);
        void SetMachineAvailability(int id, bool availability);
        Task SetMachineStatus(MachineDTO machine, MACHINE_STATUS newStatus);
        DateTime? GetMachineEstimatedCompletionDate(int id);
        void SetMachineWorkload(int id, double totalMinutes);
        void ClearMemory();
    }
}
