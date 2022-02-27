using System;
using static AssetAdministrationShellProject.Utils.LoggerConstants;

namespace CloudManufacturingSharedLibrary
{
    public class LogHelpers
    {
        /// <summary>
        /// Return string to be logged
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="machineId"></param>
        /// <param name="date"></param>
        /// <param name="orderId"></param>
        /// <returns></returns>

        public static string GenerateLogString(DateTime date, string machineId, LOGG_EVENT_TYPE logEvent, string value = "", string orderId = "")
        {
            return $"{date:yyyy-MM-dd HH:mm:ss.fff};{machineId};{logEvent};{value};{orderId}";
        }
    }
}
