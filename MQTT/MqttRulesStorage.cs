using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace iSpyApplication.MQTT
{
    public static class MqttRulesStorage
    {
        public static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "iSpy", "XML", "mqtt_rules.json");

        public static List<MqttRule> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                        {
                            var serializer = new DataContractJsonSerializer(typeof(List<MqttRule>));
                            var rules = serializer.ReadObject(ms) as List<MqttRule>;
                            return rules ?? new List<MqttRule>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MQTT: ошибка загрузки - {ex.Message}");
            }
            return new List<MqttRule>();
        }

        public static void Save(List<MqttRule> rules)
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var ms = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<MqttRule>));
                    serializer.WriteObject(ms, rules ?? new List<MqttRule>());
                    var json = Encoding.UTF8.GetString(ms.ToArray());
                    File.WriteAllText(FilePath, FormatJson(json));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MQTT: ошибка сохранения - {ex.Message}");
            }
        }

        private static string FormatJson(string json)
        {
            // Просто возвращаем как есть, но можно добавить форматирование при желании
            return json;
        }
    }
}