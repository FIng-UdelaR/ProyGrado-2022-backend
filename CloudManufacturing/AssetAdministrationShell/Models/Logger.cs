using System;
using System.IO;
using System.Threading;

namespace AssetAdministrationShellProject.Models
{
    public static class Logger
    {
        static readonly int MillisecondsTimeout = int.MaxValue;
        static readonly ReaderWriterLock locker = new ReaderWriterLock();
        public static void WriteDebug(this string text)
        {
            try
            {
                locker.AcquireWriterLock(MillisecondsTimeout); //You might wanna change timeout value 
                File.AppendAllLines(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", ""), "debug.txt"), new[] { text });
            }
            finally
            {
                locker.ReleaseWriterLock();
            }
        }

        public static void InitializeDebug(string text = null)
        {
            try
            {
                locker.AcquireWriterLock(MillisecondsTimeout); //You might wanna change timeout value 
                var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", "");
                if (File.Exists(Path.Combine(basePath, "debug.txt")))
                    File.Move(Path.Combine(basePath, "debug.txt"), Path.Combine(basePath, $"debug__{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.txt"));
                File.WriteAllText(Path.Combine(basePath, "debug.txt"), string.IsNullOrWhiteSpace(text) ? string.Empty : text);
            }
            finally
            {
                locker.ReleaseWriterLock();
            }
        }
    }
}
