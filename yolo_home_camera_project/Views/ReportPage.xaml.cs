using System.Windows;
using System.Windows.Controls;
using yolo_home_camera_project.Data.Repositories;
using yolo_home_camera_project.Models;

namespace yolo_home_camera_project.Views
{
    /// <summary>
    /// ReportPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReportPage : Page
    {
        private readonly AnalysisRunRepository _analysisRunRepository = new();
        private readonly DetectionEventRepository _detectionEventRepository = new();

        public ReportPage()
        {
            InitializeComponent();
            Loaded += ReportPage_Loaded;
        }

        private async void ReportPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadReportsAsync();
        }

        private async Task LoadReportsAsync()
        {
            List<AnalysisRun> analysisRuns = await _analysisRunRepository.GetAnalysisRunsAsync();

            ReportListBox.ItemsSource = analysisRuns;

            if (analysisRuns.Count == 0)
            {
                ShowEmptyReport();
                return;
            }

            ReportListBox.SelectedIndex = 0;
        }

        private async void ReportListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportListBox.SelectedItem is not AnalysisRun selectedRun)
            {
                ShowEmptyReport();
                return;
            }

            List<DetectionEvent> events = await _detectionEventRepository.GetEventsByAnalysisRunIdAsync(selectedRun.Id);
            ReportPreview reportPreview = CreateReportPreview(selectedRun, events);

            EmptyReportText.Visibility = Visibility.Collapsed;
            ReportPreviewContent.Visibility = Visibility.Visible;
            ReportPreviewContent.Content = reportPreview;
        }

        private static ReportPreview CreateReportPreview(AnalysisRun analysisRun, List<DetectionEvent> events)
        {
            ReportPreview reportPreview = new()
            {
                DetectionDate = analysisRun.AnalyzedAt.ToString("yyyy-MM-dd"),
                DetectionTime = analysisRun.AnalyzedAt.ToString("HH:mm:ss"),
                VideoName = analysisRun.VideoName,
                SnapshotPath = events.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.SnapshotPath))?.SnapshotPath
                    ?? "스냅샷 없음"
            };

            foreach (IGrouping<string, DetectionEvent> keywordGroup in events.GroupBy(e => e.Keyword).OrderBy(g => g.Key))
            {
                double startSeconds = keywordGroup.Min(e => e.EventSeconds);
                double endSeconds = keywordGroup.Max(e => e.EventSeconds);
                double confidence = keywordGroup.Max(e => e.Confidence);

                reportPreview.DetectionRows.Add(new ReportDetectionRow
                {
                    Keyword = keywordGroup.Key,
                    StartTime = FormatSeconds(startSeconds),
                    EndTime = FormatSeconds(endSeconds),
                    Confidence = confidence.ToString("0.00")
                });

                reportPreview.SummaryRows.Add(new ReportSummaryRow
                {
                    Keyword = keywordGroup.Key,
                    Count = keywordGroup.Count()
                });
            }

            return reportPreview;
        }

        private void ShowEmptyReport()
        {
            ReportPreviewContent.Content = null;
            ReportPreviewContent.Visibility = Visibility.Collapsed;
            EmptyReportText.Visibility = Visibility.Visible;
        }

        private static string FormatSeconds(double seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
        }
    }
}
