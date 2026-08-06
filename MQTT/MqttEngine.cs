using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace iSpyApplication.MQTT
{
    public class MqttEngine : IDisposable
    {
        private IMqttClient _client;
        private MqttClientOptions _options;
        private bool _isConnected;
        private string _brokerAddress;
        private int _brokerPort;
        private string _username;
        private string _password;

        public bool IsConnected => _isConnected;

        public MqttEngine(string brokerAddress = "localhost", int port = 1883,
            string username = "", string password = "")
        {
            _brokerAddress = brokerAddress;
            _brokerPort = port;
            _username = username;
            _password = password;

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _client.ConnectedAsync += OnConnected;
            _client.DisconnectedAsync += OnDisconnected;

            _client.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = e.ApplicationMessage.ConvertPayloadToString();
                OnMessageReceived?.Invoke(topic, payload);
                return Task.CompletedTask;
            };
        }

        public async Task SubscribeAsync(string topic)
        {
            if (!_isConnected) return;

            await _client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);
            Debug.WriteLine($"MQTT: подписан на {topic}");
        }

        // Событие при получении сообщения
        public event Action<string, string> OnMessageReceived;

        public async Task<bool> ConnectAsync()
        {
            try
            {
                var builder = new MqttClientOptionsBuilder()
                    .WithTcpServer(_brokerAddress, _brokerPort)
                    .WithClientId($"MyiSpy_{Environment.MachineName}_{Guid.NewGuid():N}")
                    // === LWT: если отключился — брокер опубликует "offline" ===

                    .WithWillRetain(true);

                if (!string.IsNullOrEmpty(_username))
                {
                    builder.WithCredentials(_username, _password);
                }

                _options = builder.Build();
                await _client.ConnectAsync(_options);

                return _isConnected;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MQTT: ошибка подключения - {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_isConnected)
                {
                    await _client.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MQTT: ошибка отключения - {ex.Message}");
            }
        }

        public async Task<bool> PublishAsync(string topic, string payload, bool retain = false,
            MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce)
        {
            if (!_isConnected) return false;

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(qos)
                    .WithRetainFlag(retain)
                    .Build();

                await _client.PublishAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MQTT: ошибка публикации - {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PublishDetectionAsync(MqttRule rule, int cameraId, string cameraName,
    List<YoloDetection> detections, byte[] snapshotData = null)
        {
            var topic = rule.GetProcessedTopic(cameraId.ToString(), cameraName);
            string payload = "ON";

            if (rule.IncludeJson)
            {
                // Формируем JSON вручную
                var objectsJson = new StringBuilder();
                objectsJson.Append("[");
                for (int i = 0; i < detections.Count; i++)
                {
                    if (i > 0) objectsJson.Append(",");
                    objectsJson.Append("{");
                    objectsJson.Append($"\"Class\":\"{detections[i].Class}\",");
                    objectsJson.Append($"\"Confidence\":{detections[i].Confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                    objectsJson.Append("}");
                }
                objectsJson.Append("]");

                payload = "{";
                payload += $"\"camera_id\":{cameraId},";
                payload += $"\"camera_name\":\"{cameraName}\",";
                payload += $"\"event_type\":\"detection\",";
                payload += $"\"timestamp\":\"{DateTime.Now.ToString("o")}\",";
                payload += $"\"objects\":{objectsJson},";
                payload += $"\"snapshot_available\":{rule.IncludeSnapshot.ToString().ToLower()}";
                payload += "}";
            }

            return await PublishAsync(topic, payload, rule.Retain, (MqttQualityOfServiceLevel)rule.QoS);
        }

        public async Task<bool> PublishMotionAsync(MqttRule rule, int cameraId, string cameraName, float motionLevel)
        {
            var topic = rule.GetProcessedTopic(cameraId.ToString(), cameraName);
            string payload = "ON";

            if (rule.IncludeJson)
            {
                payload = "{";
                payload += $"\"camera_id\":{cameraId},";
                payload += $"\"camera_name\":\"{cameraName}\",";
                payload += $"\"event_type\":\"motion\",";
                payload += $"\"timestamp\":\"{DateTime.UtcNow.ToString("o")}\",";
                payload += $"\"motion_level\":{motionLevel.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                payload += "}";
            }

            return await PublishAsync(topic, payload, rule.Retain, (MqttQualityOfServiceLevel)rule.QoS);
        }

        public async Task<bool> PublishCameraStatusAsync(MqttRule rule, int cameraId, string cameraName,
            bool online, bool recording, double fps)
        {
            var topic = rule.GetProcessedTopic(cameraId.ToString(), cameraName);

            var payload = "{";
            payload += $"\"camera_id\":{cameraId},";
            payload += $"\"camera_name\":\"{cameraName}\",";
            payload += $"\"online\":{online.ToString().ToLower()},";
            payload += $"\"recording\":{recording.ToString().ToLower()},";
            payload += $"\"fps\":{fps.ToString(System.Globalization.CultureInfo.InvariantCulture)},";
            payload += $"\"timestamp\":\"{DateTime.UtcNow.ToString("o")}\"";
            payload += "}";

            return await PublishAsync(topic, payload, rule.Retain, (MqttQualityOfServiceLevel)rule.QoS);
        }

        private Task OnConnected(MqttClientConnectedEventArgs args)
        {
            _isConnected = true;
            Debug.WriteLine($"MQTT: подключён к {_brokerAddress}:{_brokerPort}");
            return Task.CompletedTask;
        }

        private Task OnDisconnected(MqttClientDisconnectedEventArgs args)
        {
            _isConnected = false;
            Debug.WriteLine($"MQTT: отключён ({args.Reason})");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }

    public class YoloDetection
    {
        public string Class { get; set; }
        public double Confidence { get; set; }
    }
}