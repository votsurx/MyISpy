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
                // Создаём временный конфиг в TEMP папке (везде есть права на запись!)
                string configPath = Path.Combine(Path.GetTempPath(), "mosquitto_ispy.conf");
                File.WriteAllText(configPath,
                    $"listener {port} 0.0.0.0\n" +
                    "allow_anonymous true\n");

                Debug.WriteLine($"MQTT: конфиг создан: {configPath}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = MosquittoPath,
                    Arguments = $"-c \"{configPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(MosquittoPath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                _mosquittoProcess = Process.Start(startInfo);
                Debug.WriteLine($"MQTT: Mosquitto запущен с конфигом {configPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MQTT: ошибка запуска - {ex.Message}");
            }
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