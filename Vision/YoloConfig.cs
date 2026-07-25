using System;

namespace iSpyApplication.Vision
{
    [Serializable]
    public class YoloConfig
    {
        public string ModelPath { get; set; } = "Yolo/yolov8n.onnx";
        public float ConfidenceThreshold { get; set; } = 0.5f;
        public float NmsThreshold { get; set; } = 0.45f;
        public bool UseGPU { get; set; } = false;
        public int InputWidth { get; set; } = 640;
        public int InputHeight { get; set; } = 640;
    }
}