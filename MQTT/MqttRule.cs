using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace iSpyApplication.MQTT
{
    public enum MqttEventType
    {
        Motion,           // Движение обнаружено
        Detection,        // YOLO нашёл объект
        RecordingStart,   // Запись началась
        RecordingStop,    // Запись остановилась
        CameraOnline,     // Камера онлайн
        CameraOffline,    // Камера оффлайн
        Alert,            // Тревога
        Snapshot,         // Периодический снапшот
        Error             // Ошибка
    }

    public enum MqttQoS
    {
        AtMostOnce = 0,   // Доставка не гарантирована
        AtLeastOnce = 1,  // Минимум один раз
        ExactlyOnce = 2   // Ровно один раз
    }

    [DataContract]
    public class MqttRule
    {
        [DataMember]
        public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        [DataMember]
        public bool Enabled { get; set; } = true;
        [DataMember]
        public string Name { get; set; } = "Новое правило";
        [DataMember]
        public MqttEventType EventType { get; set; } = MqttEventType.Motion;
        [DataMember]
        public string Topic { get; set; } = "ispy/{event_type}/{camera_name}";
        [DataMember]
        public List<string> CameraIds { get; set; } = new List<string>();
        [DataMember]
        public List<string> ObjectFilter { get; set; } = new List<string>();
        [DataMember]
        public float MinConfidence { get; set; } = 0.5f;
        [DataMember]
        public bool IncludeJson { get; set; } = true;
        [DataMember]
        public bool IncludeSnapshot { get; set; } = false;
        [DataMember]
        public bool SnapshotAsUrl { get; set; } = true;
        [DataMember]
        public int SnapshotQuality { get; set; } = 70;
        [DataMember]
        public int SnapshotWidth { get; set; } = 320;
        [DataMember]
        public int SnapshotHeight { get; set; } = 240;
        [DataMember]
        public int IntervalSeconds { get; set; } = 30;
        [DataMember]
        public bool Retain { get; set; } = false;
        [DataMember]
        public MqttQoS QoS { get; set; } = MqttQoS.AtLeastOnce;
    

        // Вспомогательные методы
        public bool MatchesCamera(string cameraId)
        {
            if (CameraIds == null || CameraIds.Count == 0)
                return true;
            return CameraIds.Contains(cameraId);
        }

        public bool MatchesObject(string objectClass, float confidence)
        {
            if (confidence < MinConfidence)
                return false;
            if (ObjectFilter == null || ObjectFilter.Count == 0)
                return true;
            return ObjectFilter.Contains(objectClass);
        }

        public string GetProcessedTopic(string cameraId, string cameraName, string objectClass = null)
        {
            var topic = Topic
                .Replace("{camera_id}", cameraId)
                .Replace("{camera_name}", cameraName)
                .Replace("{event_type}", EventType.ToString().ToLower())
                .Replace("{rule_id}", Id)
                .Replace("{timestamp}", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));

            if (objectClass != null)
                topic = topic.Replace("{object_class}", objectClass);

            return topic;
        }

        public override string ToString()
        {
            var status = Enabled ? "✓" : "✗";
            return $"[{status}] {Name} ({EventType})";
        }
    }
}