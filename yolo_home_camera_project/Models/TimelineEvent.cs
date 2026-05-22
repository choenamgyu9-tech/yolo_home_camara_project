namespace yolo_home_camera_project.Models
{
    public class TimelineEvent
    {
        public string Keyword { get; set; } = string.Empty;

        public double StartSeconds { get; set; }

        public double EndSeconds { get; set; }

        public string StartTime { get; set; } = string.Empty;

        public string EndTime { get; set; } = string.Empty;

        public double MaxConfidence { get; set; }

        public int DetectionCount { get; set; }

        public string SnapshotPath { get; set; } = string.Empty;
    }
}
