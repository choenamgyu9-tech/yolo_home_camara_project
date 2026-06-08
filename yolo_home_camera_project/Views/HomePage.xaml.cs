using System.Windows;
using System.Windows.Controls;
using yolo_home_camera_project.Data.Repositories;
using yolo_home_camera_project.Models;

namespace yolo_home_camera_project.Views
{
    public partial class HomePage : Page
    {
        private readonly AnalysisRunRepository _analysisRunRepository = new();
        private readonly DetectionEventRepository _detectionEventRepository = new();

        public HomePage()
        {
            InitializeComponent();
            Loaded += HomePage_Loaded;
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadHomeDataAsync();
        }

        private async Task LoadHomeDataAsync()
        {
            int todayEventCount = await _detectionEventRepository.GetTodayEventCountAsync();
            int todayAnalysisCount = await _analysisRunRepository.GetTodayAnalysisRunCountAsync();
            AnalysisRun? latestAnalysisRun = await _analysisRunRepository.GetLatestAnalysisRunAsync();
            List<DetectionEvent> recentEvents = await _detectionEventRepository.GetRecentEventsAsync(5);
            List<AnalysisRun> recentReports = await _analysisRunRepository.GetRecentAnalysisRunsAsync(5);

            TodayEventCountText.Text = $"{todayEventCount}개";
            TodayAnalysisCountText.Text = $"{todayAnalysisCount}개";

            if (latestAnalysisRun is null)
            {
                LatestAnalysisText.Text = "없음";
                LatestAnalysisDateText.Text = "";
            }
            else
            {
                LatestAnalysisText.Text = latestAnalysisRun.VideoName;
                LatestAnalysisDateText.Text = latestAnalysisRun.AnalyzedAt.ToString("yyyy-MM-dd HH:mm");
            }

            RecentEventItems.ItemsSource = recentEvents.Count == 0
                ? new List<string> { "최근 감지 이벤트가 없습니다." }
                : recentEvents.Select(FormatRecentEvent).ToList();

            RecentReportItems.ItemsSource = recentReports.Count == 0
                ? new List<string> { "최근 보고서가 없습니다." }
                : recentReports.Select(FormatRecentReport).ToList();
        }

        private static string FormatRecentEvent(DetectionEvent detectionEvent)
        {
            return $"{detectionEvent.EventTime}  {detectionEvent.Keyword} 감지";
        }

        private static string FormatRecentReport(AnalysisRun analysisRun)
        {
            return $"보고서 {analysisRun.Id} ({analysisRun.AnalyzedAt:MM/dd})";
        }
    }
}
