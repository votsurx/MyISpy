using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace iSpyApplication.Vision
{
    public class YoloPipe : IDisposable
    {
        private NamedPipeServerStream _pipe;
        private readonly string _pipeName;
        private readonly object _lock = new object();

        public YoloPipe(int cameraId)
        {
            _pipeName = $@"\\.\pipe\cam{cameraId}_yolo";
            _pipe = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous
            );
        }
        public bool IsConnected() => _pipe != null && _pipe.IsConnected;

        public string GetPipeName() => _pipeName;

        public void Start()
        {
            _pipe.WaitForConnection();
            Console.WriteLine($"YOLO Pipe {_pipeName}: клиент подключён");
        }

        public void Send(byte[] data)
        {
            if (!_pipe.IsConnected) return;
            lock (_lock)
            {
                _pipe.Write(data, 0, data.Length);
                _pipe.Flush();
            }
        }

        public void SendCommand(string cmd)
        {
            var bytes = Encoding.UTF8.GetBytes(cmd);
            Send(bytes);
        }

        public string ReadResponse(int timeoutMs = 2000)
        {
            if (!_pipe.IsConnected) return null;
            var buffer = new byte[1024];
            var task = Task.Run(() => _pipe.Read(buffer, 0, buffer.Length));
            if (task.Wait(timeoutMs))
            {
                var count = task.Result;
                return Encoding.UTF8.GetString(buffer, 0, count);
            }
            return null;
        }

        public void Dispose()
        {
            _pipe?.Dispose();
            _pipe = null;
        }
    }
}