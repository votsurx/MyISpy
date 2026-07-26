using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace iSpyApplication.Yolo
{
    public class YoloTcpClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly int _port;
        private readonly object _lock = new object();

        public YoloTcpClient(int cameraId)
        {
            _port = 5000 + cameraId;
        }

        public bool IsConnected => _client?.Connected ?? false;

        public void Connect()
        {
            _client = new TcpClient();
            _client.Connect("localhost", _port);
            _stream = _client.GetStream();
            Console.WriteLine($"YOLO TCP: подключён к порту {_port}");
        }

        public void Send(byte[] data)
        {
            if (!IsConnected) return;
            lock (_lock)
            {
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
        }

        public void SendCommand(string cmd)
        {
            var bytes = Encoding.UTF8.GetBytes(cmd);
            var lenBytes = BitConverter.GetBytes(bytes.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lenBytes);

            Send(lenBytes.Concat(bytes).ToArray());
        }

        public string ReadResponse(int timeoutMs = 2000)
        {
            if (!IsConnected) return "SKIP";  // Возвращаем SKIP а не null

            try
            {
                var buffer = new byte[1024];
                var task = Task.Run(() => _stream.Read(buffer, 0, buffer.Length));
                if (task.Wait(timeoutMs))
                {
                    var count = task.Result;
                    if (count > 0)
                        return Encoding.UTF8.GetString(buffer, 0, count);
                }
                return "SKIP";  // Таймаут → SKIP
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"YOLO: ошибка чтения ответа - {ex.Message}");
                return "SKIP";  // Ошибка → SKIP
            }
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _client?.Close();
            _client = null;
        }
    }
}