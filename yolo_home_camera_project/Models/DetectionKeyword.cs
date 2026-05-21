namespace yolo_home_camera_project.Models
{
    public class DetectionKeyword
    {
        public string Keyword { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public double Threshold { get; set; } = 0.5;
    }
}