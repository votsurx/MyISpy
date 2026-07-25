using System.Drawing;

namespace iSpyApplication.Vision
{
    public class YoloPrediction
    {
        public string ClassName { get; set; }
        public float Confidence { get; set; }
        public Rectangle BBox { get; set; }
        public int ClassId { get; set; }
    }
}