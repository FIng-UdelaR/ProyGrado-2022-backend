using System.Collections.Generic;

namespace CloudManufacturingAPI.Models.Machine
{
    public class MachineCreationDTO
    {
        public string MachineName { get; set; }
        public int SupportedMaterial { get; set; }
        public List<int> SupportedSizes { get; set; }
        public List<int> SupportedQualities { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
