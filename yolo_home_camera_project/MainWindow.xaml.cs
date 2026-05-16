using System.Windows;

namespace yolo_home_camera_project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new VideoAnalysisPage());
        }

        private void VideoAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new VideoAnalysisPage());
        }

        private void KeywordManageButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new KeywordManagePage());
        }

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReportPage());
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage());
        }
    }
}
