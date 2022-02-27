using static CloudManufacturingSharedLibrary.Constants;

namespace EventsSimulator.Models
{
    public class BreakMachineEvent : CMfgSystemEvent
    {
        public int MachineId { get; set; }
        public int DownTime { get; set; }
        public BreakMachineEvent() { }
        public BreakMachineEvent(int machineId, int downTime)
        {
            Type = EVENT_TYPE.BREAK_MACHINE;
            MachineId = machineId;
            DownTime = downTime;
        }
    }
}
