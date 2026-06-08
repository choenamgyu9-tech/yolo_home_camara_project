using System.ComponentModel;
using System.IO;
using System.Windows;

namespace yolo_home_camera_project.Views
{
    public partial class AnalysisProgressWindow : Window
    {
        private bool _canClose;

        public event EventHandler? CancelRequested;

        public AnalysisProgressWindow(string videoPath)
        {
            InitializeComponent();
            VideoNameText.Text = Path.GetFileName(videoPath);
        }

        public void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        public void MarkCancelling()
        {
            StatusText.Text = "분석을 취소하고 있습니다.";
            CancelButton.IsEnabled = false;
        }

        public void CloseAfterAnalysis()
        {
            _canClose = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_canClose)
            {
                e.Cancel = true;
                RequestCancellation();
                return;
            }

            base.OnClosing(e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            RequestCancellation();
        }

        private void RequestCancellation()
        {
            if (!CancelButton.IsEnabled)
            {
                return;
            }

            MarkCancelling();
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
