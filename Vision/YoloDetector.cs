using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace iSpyApplication.Vision
{
    public class YoloDetector : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly YoloConfig _config;
        private readonly string[] _labels = new[] {
            "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck",
            "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench",
            "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra",
            "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee",
            "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove",
            "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup",
            "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange",
            "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "couch",
            "potted plant", "bed", "dining table", "toilet", "tv", "laptop", "mouse",
            "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink",
            "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier",
            "toothbrush"
        };

        public YoloDetector(YoloConfig config)
        {
            _config = config;
            var options = new SessionOptions();

            if (_config.UseGPU)
            {
                try
                {
                    options.AppendExecutionProvider("DirectML"); // для любой видеокарты
                }
                catch
                {
                    try
                    {
                        options.AppendExecutionProvider("CUDA"); // если NVIDIA
                    }
                    catch
                    {
                        _config.UseGPU = false;
                    }
                }
            }

            _session = new InferenceSession(_config.ModelPath, options);
        }

        public List<YoloPrediction> Detect(Bitmap frame)
        {
            var inputTensor = Preprocess(frame);
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("images", inputTensor)
            };
            var results = _session.Run(inputs).ToArray();
            var output = results[0].AsTensor<float>();
            return Postprocess(output, frame.Width, frame.Height);
        }

        private DenseTensor<float> Preprocess(Bitmap frame)
        {
            int w = _config.InputWidth;
            int h = _config.InputHeight;
            var tensor = new DenseTensor<float>(new[] { 1, 3, h, w });

            using var resized = new Bitmap(frame, w, h);
            var data = tensor.Buffer.Span;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var pixel = resized.GetPixel(x, y);
                    int idx = y * w + x;
                    data[0 * h * w + idx] = pixel.R / 255f;
                    data[1 * h * w + idx] = pixel.G / 255f;
                    data[2 * h * w + idx] = pixel.B / 255f;
                }
            }

            return tensor;
        }

        private List<YoloPrediction> Postprocess(Tensor<float> output, int origW, int origH)
        {
            var predictions = new List<YoloPrediction>();
            int numDetections = output.Dimensions[1];
            int numClasses = output.Dimensions[2] - 4;
            float scaleX = (float)origW / _config.InputWidth;
            float scaleY = (float)origH / _config.InputHeight;

            for (int i = 0; i < numDetections; i++)
            {
                float cx = output[0, i, 0];
                float cy = output[0, i, 1];
                float w = output[0, i, 2];
                float h = output[0, i, 3];

                float maxConf = 0;
                int bestClass = -1;
                for (int j = 0; j < numClasses; j++)
                {
                    float conf = output[0, i, j + 4];
                    if (conf > maxConf)
                    {
                        maxConf = conf;
                        bestClass = j;
                    }
                }

                if (maxConf < _config.ConfidenceThreshold) continue;

                float x = (cx - w / 2) * scaleX;
                float y = (cy - h / 2) * scaleY;
                float width = w * scaleX;
                float height = h * scaleY;

                predictions.Add(new YoloPrediction
                {
                    ClassId = bestClass,
                    ClassName = _labels[bestClass],
                    Confidence = maxConf,
                    BBox = new Rectangle((int)x, (int)y, (int)width, (int)height)
                });
            }

            return NMS(predictions);
        }

        private List<YoloPrediction> NMS(List<YoloPrediction> preds)
        {
            var result = new List<YoloPrediction>();
            var sorted = preds.OrderByDescending(p => p.Confidence).ToList();

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                result.Add(best);
                sorted.RemoveAt(0);

                sorted.RemoveAll(p => IoU(best.BBox, p.BBox) > _config.NmsThreshold);
            }

            return result;
        }

        private float IoU(Rectangle a, Rectangle b)
        {
            int xA = Math.Max(a.X, b.X);
            int yA = Math.Max(a.Y, b.Y);
            int xB = Math.Min(a.X + a.Width, b.X + b.Width);
            int yB = Math.Min(a.Y + a.Height, b.Y + b.Height);

            int interArea = Math.Max(0, xB - xA) * Math.Max(0, yB - yA);
            float unionArea = a.Width * a.Height + b.Width * b.Height - interArea;

            return unionArea == 0 ? 0 : interArea / unionArea;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}