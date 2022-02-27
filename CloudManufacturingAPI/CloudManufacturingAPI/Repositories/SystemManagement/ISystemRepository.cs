using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingAPI.Repositories.SystemManagement
{
    public interface ISystemRepository
    {
        void RunMonitoring();
        bool GetRunMonitoringAsService();
        SCHEDULING_METHOD GetSchedulingMethod();
        void SetSchedulingMethod(SCHEDULING_METHOD schedulingMethod);
        void ClearSystemMemory(bool setDefaultSchedulingMethod = false);
    }
}
