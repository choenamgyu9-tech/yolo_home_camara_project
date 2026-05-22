using System.Text.Json.Serialization;

namespace yolo_home_camera_project.Models
{
    public class YoloDetectionResult
    {
        [JsonPropertyName("videoPath")]
        public string VideoPath { get; set; } = string.Empty;

        [JsonPropertyName("eventTime")]
        public string EventTime { get; set; } = string.Empty;

        [JsonPropertyName("eventSeconds")]
        public double EventSeconds { get; set; }

        [JsonPropertyName("frameIndex")]
        public int FrameIndex { get; set; }

        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("snapshotPath")]
        public string SnapshotPath { get; set; } = string.Empty;

        [JsonPropertyName("box")]
        public YoloBox Box { get; set; } = new();
    }

    public class YoloBox
    {
        [JsonPropertyName("x1")]
        public int X1 { get; set; }

        [JsonPropertyName("y1")]
        public int Y1 { get; set; }

        [JsonPropertyName("x2")]
        public int X2 { get; set; }

        [JsonPropertyName("y2")]
        public int Y2 { get; set; }
    }
}
