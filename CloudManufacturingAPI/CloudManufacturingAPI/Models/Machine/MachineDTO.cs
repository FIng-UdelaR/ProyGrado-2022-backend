using System.Collections.Generic;

namespace CloudManufacturingAPI.Models.Machine
{
    public class MachineDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PortNumber { get; set; }
        public string Uri { get; set; }
        public int SupportedMaterial { get; set; }
        public List<int> SupportedSizes { get; set; }
        public List<int> SupportedQualities { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool Available { get; set; }
        public double Workload { get; set; }

        public MachineDTO() { }

        public MachineDTO(CloudManufacturingDBAccess.Models.MachineDBO machine, bool available = true)
        {
            Id = machine.Id;
            Name = machine.Name;
            PortNumber = machine.PortNumber;
            Uri = machine.Uri;
            SupportedMaterial = machine.SupportedMaterial;
            SupportedSizes = machine.SupportedSizes;
            SupportedQualities = machine.SupportedQualities;
            Latitude = machine.Location.Latitude;
            Longitude = machine.Location.Longitude;
            Available = available;
        }
    }
}
