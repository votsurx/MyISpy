using System;
using System.Diagnostics;
using System.IO;

namespace iSpyApplication.MQTT
{
    public static class MosquittoLauncher
    {
        private static Process _mosquittoProcess;

        public static string MosquittoPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mosquitto", "mosquitto.exe");
        public static bool IsInstalled => File.Exists(MosquittoPath);
        public static bool IsRunning => _mosquittoProcess != null && !_mosquittoProcess.HasExited;

        public static void Start(int port = 1883)
        {
            if (IsRunning) return;
            if (!IsInstalled) return;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = MosquittoPath,
                    Arguments = $"-p {port}",
                    WorkingDirectory = Path.GetDirectoryName(MosquittoPath),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                _mosquittoProcess = Process.Start(startInfo);
            }
            catch { }
        }

        public static void Stop()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("mosquitto"))
                {
                    try { p.Kill(); p.WaitForExit(3000); } catch { }
                }
            }
            catch { }
            _mosquittoProcess = null;
        }
    }
}