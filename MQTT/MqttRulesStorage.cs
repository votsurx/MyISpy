// Файл: MQTT/MqttRulesStorage.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace iSpyApplication.MQTT
{
    public static class MqttRulesStorage
    {
        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "iSpy", "XML", "mqtt_rules.xml");

        public static List<MqttRule> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var serializer = new XmlSerializer(typeof(List<MqttRule>));
                    using var fs = new FileStream(FilePath, FileMode.Open);
                    return (List<MqttRule>)serializer.Deserialize(fs) ?? new List<MqttRule>();
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

                var serializer = new XmlSerializer(typeof(List<MqttRule>));
                using var fs = new FileStream(FilePath, FileMode.Create);
                serializer.Serialize(fs, rules);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MQTT: ошибка сохранения - {ex.Message}");
            }
        }
    }
}