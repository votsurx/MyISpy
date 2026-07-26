using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
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
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                var builder = new MqttClientOptionsBuilder()
                    .WithTcpServer(_brokerAddress, _brokerPort)
                    .WithClientId($"MyiSpy_{Environment.MachineName}_{Guid.NewGuid():N}");

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

            string payload = null;

            if (rule.IncludeJson)
            {
                var detectionEvent = new
                {
                    camera_id = cameraId,
                    camera_name = cameraName,
                    event_type = "detection",
                    timestamp = DateTime.UtcNow.ToString("o"),
                    objects = detections,
                    snapshot_available = rule.IncludeSnapshot
                };
                payload = JsonSerializer.Serialize(detectionEvent);
            }
            else
            {
                payload = "ON"; // Простой формат для датчиков
            }

            var result = await PublishAsync(topic, payload, rule.Retain,
                (MqttQualityOfServiceLevel)rule.QoS);

            // Отправляем снапшот отдельным сообщением
            if (rule.IncludeSnapshot && snapshotData != null)
            {
                var snapshotTopic = topic + "/snapshot";

                if (rule.SnapshotAsUrl)
                {
                    // URL на снапшот (заглушка)
                    var snapshotUrl = $"http://localhost:8080/snapshot/{cameraId}";
                    await PublishAsync(snapshotTopic, snapshotUrl, false,
                        MqttQualityOfServiceLevel.AtMostOnce);
                }
                else
                {
                    // Base64 снапшот
                    var snapshotPayload = JsonSerializer.Serialize(new
                    {
                        camera_id = cameraId,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        format = "jpeg",
                        width = rule.SnapshotWidth,
                        height = rule.SnapshotHeight,
                        data = Convert.ToBase64String(snapshotData)
                    });
                    await PublishAsync(snapshotTopic, snapshotPayload, false,
                        MqttQualityOfServiceLevel.AtMostOnce);
                }
            }

            return result;
        }

        public async Task<bool> PublishMotionAsync(MqttRule rule, int cameraId, string cameraName,
            float motionLevel)
        {
            var topic = rule.GetProcessedTopic(cameraId.ToString(), cameraName);

            string payload;
            if (rule.IncludeJson)
            {
                payload = JsonSerializer.Serialize(new
                {
                    camera_id = cameraId,
                    camera_name = cameraName,
                    event_type = "motion",
                    timestamp = DateTime.UtcNow.ToString("o"),
                    motion_level = motionLevel
                });
            }
            else
            {
                payload = "ON";
            }

            return await PublishAsync(topic, payload, rule.Retain,
                (MqttQualityOfServiceLevel)rule.QoS);
        }

        public async Task<bool> PublishCameraStatusAsync(MqttRule rule, int cameraId, string cameraName,
            bool online, bool recording, double fps)
        {
            var topic = rule.GetProcessedTopic(cameraId.ToString(), cameraName);

            var payload = JsonSerializer.Serialize(new
            {
                camera_id = cameraId,
                camera_name = cameraName,
                online,
                recording,
                fps,
                timestamp = DateTime.UtcNow.ToString("o")
            });

            return await PublishAsync(topic, payload, rule.Retain,
                (MqttQualityOfServiceLevel)rule.QoS);
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

    // Вспомогательный класс для детекций
    public class YoloDetection
    {
        public string Class { get; set; }
        public double Confidence { get; set; }
    }
}