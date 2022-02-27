using System;

namespace CloudManufacturingSharedLibrary
{
    public class Helpers
    {
        /// <summary>
        /// Returns the distance in kilometers between the source and target coordinates
        /// </summary>
        /// <param name="sourceLat"></param>
        /// <param name="sourceLong"></param>
        /// <param name="targetLat"></param>
        /// <param name="targetLong"></param>
        /// <returns></returns>
        public static double DistanceTo(double sourceLat, double sourceLong, double targetLat, double targetLong)
        {
            double baseRad = Math.PI * sourceLat / 180;
            double targetRad = Math.PI * targetLat / 180;
            double theta = sourceLong - targetLong;
            double thetaRad = Math.PI * theta / 180;

            double dist =
                Math.Sin(baseRad) * Math.Sin(targetRad) + Math.Cos(baseRad) *
                Math.Cos(targetRad) * Math.Cos(thetaRad);
            dist = Math.Acos(dist);

            dist = dist * 180 / Math.PI;
            dist = dist * 60 * 1.1515; //miles

            return dist * 1.609344; //kilometers
        }

        /// <summary>
        /// Calculates the estimated completion date for a given task
        /// </summary>
        /// <param name="material"></param>
        /// <param name="size"></param>
        /// <param name="quality"></param>
        /// <param name="estimatedStartDate"></param>
        /// <returns></returns>
        public static DateTime EstimateCompletionDate(int material, int size, int quality, DateTime estimatedStartDate)
        {
            var simulationUnitToSeconds = Constants.DELAY_BETWEEN_SIMULATION_TIME_UNITS_IN_MILLISECONDS / 1000;
            var sizeWeight = (size + 1);
            var qualityWeight = (quality + 2);
            return estimatedStartDate.AddSeconds(simulationUnitToSeconds * sizeWeight * qualityWeight);
        }
    }
}
