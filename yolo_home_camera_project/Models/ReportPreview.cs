using System.Collections.ObjectModel;

namespace yolo_home_camera_project.Models
{
    public class ReportPreview
    {
        public string Title { get; set; } = "탐지 보고서";

        public string CameraName { get; set; } = "Home Camera 01";

        public string DetectionDate { get; set; } = string.Empty;

        public string DetectionTime { get; set; } = string.Empty;

        public string VideoName { get; set; } = string.Empty;

        public string SnapshotPath { get; set; } = string.Empty;

        public ObservableCollection<ReportDetectionRow> DetectionRows { get; } = new();

        public ObservableCollection<ReportSummaryRow> SummaryRows { get; } = new();
    }

    public class ReportDetectionRow
    {
        public string Keyword { get; set; } = string.Empty;

        public string StartTime { get; set; } = string.Empty;

        public string EndTime { get; set; } = string.Empty;

        public string Confidence { get; set; } = string.Empty;
    }

    public class ReportSummaryRow
    {
        public string Keyword { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
