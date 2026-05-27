namespace yolo_home_camera_project.Models
{
    public class DetectionEvent
    {
        public int Id { get; set; }

        public int? AnalysisRunId { get; set; }

        public string VideoPath { get; set; } = string.Empty;

        public string EventTime { get; set; } = string.Empty;

        public double EventSeconds { get; set; }

        public int FrameIndex { get; set; }

        public string Keyword { get; set; } = string.Empty;

        public double Confidence { get; set; }

        public string SnapshotPath { get; set; } = string.Empty;

        public int BoxX1 { get; set; }

        public int BoxY1 { get; set; }

        public int BoxX2 { get; set; }

        public int BoxY2 { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
