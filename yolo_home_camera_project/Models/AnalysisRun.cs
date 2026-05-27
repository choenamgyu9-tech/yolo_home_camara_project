namespace yolo_home_camera_project.Models
{
    public class AnalysisRun
    {
        public int Id { get; set; }

        public string VideoName { get; set; } = string.Empty;

        public string VideoPath { get; set; } = string.Empty;

        public double Confidence { get; set; }

        public int VidStride { get; set; }

        public string Keywords { get; set; } = string.Empty;

        public int EventCount { get; set; }

        public DateTime AnalyzedAt { get; set; }
    }
}
