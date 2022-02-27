namespace CloudManufacturingAPI.Models.Work
{
    public class AddWorkDTO
    {
        public int Material { get; set; }
        public int Size { get; set; }
        public int Quality { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
