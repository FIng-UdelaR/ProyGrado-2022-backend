using System.Diagnostics;

namespace AssetAdministrationShellProject.Utils
{
    public static class ShellHelper
    {
        public static void Shell(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{escapedArgs}\"",
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                }
            };
            process.Start();
            return;
        }
    }
}
