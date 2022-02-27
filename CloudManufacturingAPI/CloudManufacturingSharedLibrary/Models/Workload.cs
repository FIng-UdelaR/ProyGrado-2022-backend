using System;
using static CloudManufacturingSharedLibrary.Constants;

namespace CloudManufacturingSharedLibrary.Models
{
    /// <summary>
    /// Workload item representation for 3D Printers
    /// </summary>
    public class Workload
    {
        public string OrderId { get; set; }
        public DateTime OrderArrivalTime { get; set; }
        public int ItemId { get; set; } //Each work order can have multiple items
        public DateTime EstimatedCompletionDate { get; set; }
        public MATERIAL Material { get; set; }
        public SIZE Size { get; set; }
        public QUALITY Quality { get; set; }
        public PRIORITY Priority { get; set; } //Change for Delivery Deadline?
        public double SourceLatitude { get; set; }
        public double SourceLongitude { get; set; }
    }
}
