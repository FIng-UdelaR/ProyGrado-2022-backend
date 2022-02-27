using System.Collections.Generic;

namespace CloudManufacturingDBAccess.Models
{
    public class MachineDBO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PortNumber { get; set; }
        public string Uri { get; set; }
        public int SupportedMaterial { get; set; }
        public List<int> SupportedSizes { get; set; }
        public List<int> SupportedQualities { get; set; }
        public Location Location { get; set; }

        internal bool ValidateInsert()
        {
            return this != null
                && !string.IsNullOrWhiteSpace(Name)
                && !string.IsNullOrWhiteSpace(Uri)
                && PortNumber > 0
                && SupportedMaterial >= 0;
                //&& !string.IsNullOrWhiteSpace(SupportedSizesJson)
                //&& !string.IsNullOrWhiteSpace(SupportedQualitiesJson)
                //&& !string.IsNullOrWhiteSpace(LocationJson);
        }
    }
}
