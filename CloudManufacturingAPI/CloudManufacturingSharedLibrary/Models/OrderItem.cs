using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingSharedLibrary.Models
{
    public class OrderItem
    {
        public MATERIAL Material { get; set; }
        public SIZE Size { get; set; }
        public QUALITY Quality { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
