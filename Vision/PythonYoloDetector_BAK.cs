using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace iSpyApplication.Vision
{
    public class PythonYoloDetector : IDisposable
    {
        private readonly string _tempPath = @"C:\Temp\";
        private readonly string _scriptPath;

        public PythonYoloDetector(string scriptPath)
        {
            _scriptPath = scriptPath;
        }

        public YoloPrediction[] Detect(Bitmap frame)
        {
            try
            {
                // Создаём папку, если нет
                if (!Directory.Exists(_tempPath))
                    Directory.CreateDirectory(_tempPath);

                // Генерируем уникальное имя файла
                var timestamp = DateTime.Now.Ticks;
                var jpgPath = Path.Combine(_tempPath, $"yolo_frame_{timestamp}.jpg");
                var jsonPath = Path.Combine(_tempPath, $"yolo_frame_{timestamp}.json");

                // Сохраняем кадр (с защитой от внешних исключений)
                try
                {
                    using var safeFrame = new Bitmap(frame);
                    safeFrame.Save(jpgPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка сохранения кадра: {ex.Message}");
                    return Array.Empty<YoloPrediction>();
                }

                // Ждём появления JSON-файла (макс. 5 секунд)
                int attempts = 0;
                while (!File.Exists(jsonPath) && attempts < 50)
                {
                    Thread.Sleep(100);
                    attempts++;
                }

                if (!File.Exists(jsonPath))
                {
                    try { File.Delete(jpgPath); } catch { }
                    return Array.Empty<YoloPrediction>();
                }

                // Читаем JSON
                string json;
                try
                {
                    json = File.ReadAllText(jsonPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка чтения JSON: {ex.Message}");
                    try { File.Delete(jpgPath); } catch { }
                    try { File.Delete(jsonPath); } catch { }
                    return Array.Empty<YoloPrediction>();
                }

                // Удаляем файлы после чтения
                try { File.Delete(jpgPath); } catch { }
                try { File.Delete(jsonPath); } catch { }

                if (string.IsNullOrEmpty(json))
                    return Array.Empty<YoloPrediction>();

                // Парсим JSON
                var predictions = new List<YoloPrediction>();
                try
                {
                    var docs = JsonDocument.Parse(json);
                    var root = docs.RootElement;
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in root.EnumerateArray())
                        {
                            var className = item.GetProperty("class").GetString();
                            var confidence = (float)item.GetProperty("confidence").GetDouble();
                            var bbox = item.GetProperty("bbox").EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();
                            if (bbox.Length >= 4)
                            {
                                predictions.Add(new YoloPrediction
                                {
                                    ClassName = className,
                                    Confidence = confidence,
                                    BBox = new Rectangle((int)bbox[0], (int)bbox[1], (int)(bbox[2] - bbox[0]), (int)(bbox[3] - bbox[1]))
                                });
                            }
                        }
                    }
                }
                catch
                {
                    return Array.Empty<YoloPrediction>();
                }

                return predictions.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Общая ошибка: {ex.Message}");
                return Array.Empty<YoloPrediction>();
            }
        }

        public void Dispose()
        {
        }
    }
}