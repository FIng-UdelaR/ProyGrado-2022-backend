using System;

namespace AssetAdministrationShellProject.Models
{
    /// <summary>
    /// Represents a geographical location that is determined by latitude and longitude coordinates.
    /// </summary>
    public class GeoCoordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public GeoCoordinate() { }
        public GeoCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
        public GeoCoordinate(CloudManufacturingDBAccess.Models.Location location)
        {
            if(location != null)
            {
                Latitude = location.Latitude;
                Longitude = location.Longitude;
            }
        }

        /// <summary>
        /// Returns the distance in kilometers between the latitude and longitude coordinates 
        /// that are specified by this GeoCoordinate and another specified GeoCoordinate.
        /// </summary>
        /// <param name="targetCoordinates"></param>
        /// <returns></returns>
        public double DistanceTo(GeoCoordinate targetCoordinates)
        {
            double baseRad = Math.PI * Latitude / 180;
            double targetRad = Math.PI * targetCoordinates.Latitude / 180;
            double theta = Longitude - targetCoordinates.Longitude;
            double thetaRad = Math.PI * theta / 180;

            double dist =
                Math.Sin(baseRad) * Math.Sin(targetRad) + Math.Cos(baseRad) *
                Math.Cos(targetRad) * Math.Cos(thetaRad);
            dist = Math.Acos(dist);

            dist = dist * 180 / Math.PI;
            dist = dist * 60 * 1.1515; //miles

            return dist * 1.609344; //kilometers
        }
    }
}
