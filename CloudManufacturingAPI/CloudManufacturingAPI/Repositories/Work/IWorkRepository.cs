using CloudManufacturingAPI.Models.Machine;
using CloudManufacturingAPI.Models.SystemManagement;
using CloudManufacturingAPI.Models.Work;
using CloudManufacturingSharedLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudManufacturingAPI.Repositories.Work
{
    public interface IWorkRepository
    {
        ScheduleWorkloadResultDTO AddWork(IEnumerable<AddWorkDTO> newWork);
        Task<IEnumerable<Workload>> GetWorkload(MachineDTO machine);
        IEnumerable<OrderDTO> GetOrders(int? start, int? length, string order, string orderId, out int filteredRecords, out int totalRecords);
        Task<bool> RemoveAllWorkload(MachineDTO machine);
    }
}
