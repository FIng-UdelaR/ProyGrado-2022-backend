using System.Collections.Generic;
using static CloudManufacturingSharedLibrary.Constants;

namespace EventsSimulator.Models
{
    public class CreateMachineEvent : CMfgSystemEvent
    {
        public MATERIAL SupportedMaterial { get; set; }
        public List<SIZE> SupportedSizes { get; set; }
        public List<QUALITY> SupportedQualities { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public CreateMachineEvent() { }
        public CreateMachineEvent(MATERIAL supportedMaterial, List<SIZE> supportedSizes, List<QUALITY> supportedQualities, double lat, double lng)
        {
            Type = EVENT_TYPE.CREATE_MACHINE;
            SupportedMaterial = supportedMaterial;
            SupportedQualities = supportedQualities;
            SupportedSizes = supportedSizes;
            Latitude = lat;
            Longitude = lng;
        }
    }
}
