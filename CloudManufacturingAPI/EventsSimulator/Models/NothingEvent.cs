using static CloudManufacturingSharedLibrary.Constants;

namespace EventsSimulator.Models
{
    public class NothingEvent : CMfgSystemEvent
    {
        public NothingEvent()
        {
            Type = EVENT_TYPE.NOTHING;
        }
    }
}
