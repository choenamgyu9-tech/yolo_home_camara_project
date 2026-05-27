using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using yolo_home_camera_project.Data.Repositories;
using yolo_home_camera_project.Models;
using yolo_home_camera_project.Services;

namespace yolo_home_camera_project.Views
{
    public partial class VideoAnalysisPage : Page
    {
        private static readonly string[] VideoExtensions = [".mp4", ".avi", ".mov", ".mkv"];

        private readonly DispatcherTimer _playbackTimer;
        private readonly ObservableCollection<VideoListItem> _videos = [];
        private VideoSortMode _sortMode = VideoSortMode.Name;
        private bool _sortAscending = true;
        private bool _isDraggingPosition;
        private bool _isPlaying;

        private readonly PythonYoloService _pythonYoloService = new();
        private readonly KeywordService _keywordService = new();
        private readonly DetectionEventRepository _detectionEventRepository = new();
        private readonly AnalysisRunRepository _analysisRunRepository = new();
        private readonly ObservableCollection<TimelineEvent> _timelineEvents = [];
        private string? _selectedVideoPath;

        public VideoAnalysisPage()
        {
            InitializeComponent();

            VideoListBox.ItemsSource = _videos;
            TimelineListBox.ItemsSource = _timelineEvents;

            Loaded += VideoAnalysisPage_Loaded;

            _playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _playbackTimer.Tick += PlaybackTimer_Tick;

            LoadVideoList();
            UpdateSortMarks();
        }

        private async void VideoAnalysisPage_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
            await RefreshActiveKeywordsTextAsync();
        }

        private static List<TimelineEvent> BuildTimelineEvents(IEnumerable<YoloDetectionResult> detections, double maxGapSeconds)
        {
            List<YoloDetectionResult> ordered = detections
                .OrderBy(d => d.Keyword)
                .ThenBy(d => d.EventSeconds)
                .ToList();

            List<TimelineEvent> result = [];
            TimelineEvent? current = null;

            foreach (YoloDetectionResult detection in ordered)
            {
                if (current is null)
                {
                    current = CreateTimelineEvent(detection);
                    continue;
                }

                bool sameKeyword = current.Keyword == detection.Keyword;
                bool closeEnough = detection.EventSeconds - current.EndSeconds <= maxGapSeconds;

                if (sameKeyword && closeEnough)
                {
                    current.EndSeconds = detection.EventSeconds;
                    current.EndTime = FormatTime(TimeSpan.FromSeconds(detection.EventSeconds));
                    current.DetectionCount++;

                    if (detection.Confidence > current.MaxConfidence)
                    {
                        current.MaxConfidence = Math.Round(detection.Confidence, 4);
                        current.SnapshotPath = detection.SnapshotPath;
                    }
                }
                else
                {
                    result.Add(current);
                    current = CreateTimelineEvent(detection);
                }
            }

            if (current is not null)
            {
                result.Add(current);
            }

            return result
                .OrderBy(item => item.StartSeconds)
                .ToList();
        }

        private void TimelineListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TimelineListBox.SelectedItem is not TimelineEvent selected)
            {
                return;
            }

            if (VideoPlayer.Source is null)
            {
                return;
            }

            TimeSpan target = TimeSpan.FromSeconds(selected.StartSeconds);

            VideoPlayer.Position = target;
            PositionSlider.Value = selected.StartSeconds;
            UpdatePlaybackTime(target);

            VideoPlayer.Play();
            _playbackTimer.Start();
            SetPlaybackState(true);
        }

        private static TimelineEvent CreateTimelineEvent(YoloDetectionResult detection)
        {
            return new TimelineEvent
            {
                Keyword = detection.Keyword,
                StartSeconds = detection.EventSeconds,
                EndSeconds = detection.EventSeconds,
                StartTime = FormatTime(TimeSpan.FromSeconds(detection.EventSeconds)),
                EndTime = FormatTime(TimeSpan.FromSeconds(detection.EventSeconds)),
                MaxConfidence = Math.Round(detection.Confidence, 4),
                DetectionCount = 1,
                SnapshotPath = detection.SnapshotPath
            };
        }

        private async Task RefreshActiveKeywordsTextAsync()
        {
            List<string> keywords = await _keywordService.LoadEnabledKeywordNamesAsync();

            if (keywords.Count == 0)
            {
                ActiveKeywordsText.Text = "Active keywords: none";
                return;
            }

            ActiveKeywordsText.Text = $"Active keywords: {string.Join(", ", keywords)}";
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedVideoPath))
            {
                MessageBox.Show("먼저 분석할 동영상을 선택하거나 드래그하세요.");
                return;
            }

            try
            {
                AnalyzeButton.IsEnabled = false;
                AnalysisStatusText.Text = "Status: loading keywords...";
                AnalysisProgressText.Text = "Progress: preparing";
                AnalysisProgressBar.IsIndeterminate = true;
                _timelineEvents.Clear();

                List<string> keywords = await _keywordService.LoadEnabledKeywordNamesAsync();

                if (keywords.Count == 0)
                {
                    MessageBox.Show("활성화된 탐색 키워드가 없습니다. Keyword Manage 페이지에서 키워드를 활성화하세요.");
                    AnalysisStatusText.Text = "Status: no active keywords";
                    return;
                }

                ActiveKeywordsText.Text = $"Active keywords: {string.Join(", ", keywords)}";

                double confidence = double.TryParse(
                    ConfidenceTextBox.Text,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double parsedConfidence)
                    ? parsedConfidence
                    : 0.5;

                int vidStride = int.TryParse(VidStrideTextBox.Text, out int parsedStride)
                    ? parsedStride
                    : 5;

                AnalysisStatusText.Text = "Status: YOLOE analyzing...";

                List<YoloDetectionResult> detections = await _pythonYoloService.AnalyzeVideoAsync(
                    _selectedVideoPath,
                    keywords,
                    confidence,
                    vidStride);

                int analysisRunId = await _analysisRunRepository.CreateAnalysisRunAsync(
                    _selectedVideoPath,
                    confidence,
                    vidStride,
                    keywords);

                await _detectionEventRepository.SaveDetectionResultsAsync(analysisRunId, detections);
                await _analysisRunRepository.UpdateEventCountAsync(analysisRunId, detections.Count);

                List<TimelineEvent> timelineEvents = BuildTimelineEvents(
                    detections,
                    maxGapSeconds: 1.0);

                foreach (TimelineEvent item in timelineEvents)
                {
                    _timelineEvents.Add(item);
                }

                AnalysisProgressBar.IsIndeterminate = false;
                AnalysisProgressBar.Value = 100;
                AnalysisProgressText.Text = "Progress: 100%";
                AnalysisStatusText.Text = $"Status: complete, {_timelineEvents.Count} timeline events";
            }
            catch (Exception ex)
            {
                AnalysisProgressBar.IsIndeterminate = false;
                AnalysisStatusText.Text = "Status: failed";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                AnalyzeButton.IsEnabled = true;
            }
        }

        private void OpenVideoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "Select a video",
                Filter = "Video files (*.mp4;*.avi;*.mov;*.mkv)|*.mp4;*.avi;*.mov;*.mkv|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            LoadVideo(dialog.FileName);
        }

        private void LoadVideo(string fileName)
        {
            _selectedVideoPath = fileName;

            VideoPlayer.Stop();
            VideoPlayer.Source = new Uri(fileName);
            ApplyPlaybackSpeed();
            EmptyVideoText.Visibility = Visibility.Collapsed;
            SelectedVideoText.Text = $"Selected video: {Path.GetFileName(fileName)}";
            VideoPlayer.Play();
            _playbackTimer.Start();
            SetPlaybackState(true);
        }

        private void LoadVideoList()
        {
            _videos.Clear();

            DirectoryInfo? videosFolder = FindVideosFolder();
            if (videosFolder is null || !videosFolder.Exists)
            {
                return;
            }

            IEnumerable<FileInfo> videoFiles = ApplyVideoSort(videosFolder
                .EnumerateFiles()
                .Where(file => IsVideoFile(file.FullName)));

            foreach (FileInfo file in videoFiles)
            {
                _videos.Add(new VideoListItem(
                    Path.GetFileNameWithoutExtension(file.Name),
                    file.FullName,
                    file.CreationTime,
                    file.LastWriteTime));
            }
        }

        private IEnumerable<FileInfo> ApplyVideoSort(IEnumerable<FileInfo> files)
        {
            return _sortMode switch
            {
                VideoSortMode.AddedDate => _sortAscending
                    ? files.OrderBy(file => file.LastWriteTime).ThenBy(file => file.Name)
                    : files.OrderByDescending(file => file.LastWriteTime).ThenBy(file => file.Name),

                VideoSortMode.CreatedDate => _sortAscending
                    ? files.OrderBy(file => file.CreationTime).ThenBy(file => file.Name)
                    : files.OrderByDescending(file => file.CreationTime).ThenBy(file => file.Name),

                _ => _sortAscending
                    ? files.OrderBy(file => file.Name)
                    : files.OrderByDescending(file => file.Name)
            };
        }

        private static DirectoryInfo? FindVideosFolder()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);

            while (current is not null)
            {
                DirectoryInfo candidate = new(Path.Combine(current.FullName, "Videos"));
                if (candidate.Exists)
                {
                    return candidate;
                }

                DirectoryInfo projectCandidate = new(Path.Combine(current.FullName, "yolo_home_camera_project", "Videos"));
                if (projectCandidate.Exists)
                {
                    return projectCandidate;
                }

                current = current.Parent;
            }

            return null;
        }

        private void VideoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideoListBox.SelectedItem is VideoListItem selectedVideo)
            {
                LoadVideo(selectedVideo.FilePath);
            }
        }

        private void OpenVideosFolderButton_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo videosFolder = FindVideosFolder() ?? CreateVideosFolder();

            Process.Start(new ProcessStartInfo
            {
                FileName = videosFolder.FullName,
                UseShellExecute = true
            });
        }

        private void ToggleSortPanelButton_Click(object sender, RoutedEventArgs e)
        {
            SortPopup.IsOpen = !SortPopup.IsOpen;
        }

        private void SortByNameButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSort(VideoSortMode.Name);
        }

        private void SortByCreatedButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSort(VideoSortMode.CreatedDate);
        }

        private void SortByAddedButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSort(VideoSortMode.AddedDate);
        }

        private void SortAscendingButton_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = true;
            LoadVideoList();
            UpdateSortMarks();
            SortPopup.IsOpen = false;
        }

        private void SortDescendingButton_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = false;
            LoadVideoList();
            UpdateSortMarks();
            SortPopup.IsOpen = false;
        }

        private void ChangeSort(VideoSortMode sortMode)
        {
            _sortMode = sortMode;
            LoadVideoList();
            UpdateSortMarks();
            SortPopup.IsOpen = false;
        }

        private void UpdateSortMarks()
        {
            const string selectedMark = "\u2022";

            NameSortMark.Text = _sortMode == VideoSortMode.Name ? selectedMark : string.Empty;
            CreatedSortMark.Text = _sortMode == VideoSortMode.CreatedDate ? selectedMark : string.Empty;
            AddedSortMark.Text = _sortMode == VideoSortMode.AddedDate ? selectedMark : string.Empty;
            AscendingSortMark.Text = _sortAscending ? selectedMark : string.Empty;
            DescendingSortMark.Text = _sortAscending ? string.Empty : selectedMark;
        }

        private static DirectoryInfo CreateVideosFolder()
        {
            DirectoryInfo folder = new(Path.Combine(AppContext.BaseDirectory, "Videos"));
            folder.Create();
            return folder;
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayPause();
        }

        private void TogglePlayPause()
        {
            if (VideoPlayer.Source is null)
            {
                EmptyVideoText.Visibility = Visibility.Visible;
                return;
            }

            if (_isPlaying)
            {
                VideoPlayer.Pause();
                SetPlaybackState(false);
                return;
            }

            VideoPlayer.Play();
            _playbackTimer.Start();
            EmptyVideoText.Visibility = Visibility.Collapsed;
            SetPlaybackState(true);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            PositionSlider.Value = 0;
            UpdatePlaybackTime();
            SetPlaybackState(false);
        }

        private void PlaybackSpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyPlaybackSpeed();
        }

        private void ApplyPlaybackSpeed()
        {
            if (PlaybackSpeedComboBox?.SelectedItem is ComboBoxItem item &&
                double.TryParse(item.Tag?.ToString(), out double speed))
            {
                VideoPlayer.SpeedRatio = speed;
            }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            ApplyPlaybackSpeed();

            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                PositionSlider.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            }

            UpdatePlaybackTime();
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _playbackTimer.Stop();
            PositionSlider.Value = 0;
            VideoPlayer.Stop();
            UpdatePlaybackTime();
            SetPlaybackState(false);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _playbackTimer.Stop();
            VideoPlayer.Stop();
            SetPlaybackState(false);
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                TogglePlayPause();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Right)
            {
                SeekBy(TimeSpan.FromSeconds(5));
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Left)
            {
                SeekBy(TimeSpan.FromSeconds(-5));
                e.Handled = true;
            }
        }

        private void SeekBy(TimeSpan offset)
        {
            if (VideoPlayer.Source is null)
            {
                return;
            }

            TimeSpan target = VideoPlayer.Position + offset;
            if (target < TimeSpan.Zero)
            {
                target = TimeSpan.Zero;
            }

            if (VideoPlayer.NaturalDuration.HasTimeSpan && target > VideoPlayer.NaturalDuration.TimeSpan)
            {
                target = VideoPlayer.NaturalDuration.TimeSpan;
            }

            VideoPlayer.Position = target;
            PositionSlider.Value = target.TotalSeconds;
            UpdatePlaybackTime();
        }

        private void SetPlaybackState(bool isPlaying)
        {
            _isPlaying = isPlaying;
            PlayPauseButton.Content = isPlaying ? "\u275A\u275A" : "\u25B6";
        }

        private void VideoDropArea_DragEnter(object sender, DragEventArgs e)
        {
            UpdateDragState(sender, e);
        }

        private void VideoDropArea_DragOver(object sender, DragEventArgs e)
        {
            UpdateDragState(sender, e);
        }

        private void VideoDropArea_DragLeave(object sender, DragEventArgs e)
        {
            ResetDropHighlight();
        }

        private void VideoDropArea_Drop(object sender, DragEventArgs e)
        {
            ResetDropHighlight();

            string? fileName = GetDroppedVideoFile(e);
            if (fileName is null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            string videoToPlay = AddDroppedVideoToFolder(fileName);
            LoadVideo(videoToPlay);
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private string AddDroppedVideoToFolder(string sourceFileName)
        {
            DirectoryInfo videosFolder = FindVideosFolder() ?? CreateVideosFolder();
            string targetFileName = Path.Combine(videosFolder.FullName, Path.GetFileName(sourceFileName));

            if (!File.Exists(targetFileName) && !IsSamePath(sourceFileName, targetFileName))
            {
                File.Copy(sourceFileName, targetFileName);
            }

            LoadVideoList();
            SelectVideoInList(targetFileName);
            return targetFileName;
        }

        private void SelectVideoInList(string fileName)
        {
            VideoListItem? video = _videos.FirstOrDefault(item => IsSamePath(item.FilePath, fileName));
            if (video is not null)
            {
                VideoListBox.SelectedItem = video;
            }
        }

        private void UpdateDragState(object sender, DragEventArgs e)
        {
            bool canDrop = GetDroppedVideoFile(e) is not null;
            e.Effects = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;

            VideoDropArea.Background = canDrop
                ? new SolidColorBrush(Color.FromRgb(42, 48, 58))
                : new SolidColorBrush(Color.FromRgb(64, 32, 32));
        }

        private void ResetDropHighlight()
        {
            VideoDropArea.Background = new SolidColorBrush(Color.FromRgb(32, 33, 36));
        }

        private static string? GetDroppedVideoFile(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return null;
            }

            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            {
                return null;
            }

            string fileName = files[0];

            return IsVideoFile(fileName)
                ? fileName
                : null;
        }

        private static bool IsVideoFile(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            return VideoExtensions.Any(videoExtension =>
                videoExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSamePath(string firstPath, string secondPath)
        {
            return Path.GetFullPath(firstPath).Equals(
                Path.GetFullPath(secondPath),
                StringComparison.OrdinalIgnoreCase);
        }

        private void PlaybackTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isDraggingPosition && VideoPlayer.Source is not null)
            {
                PositionSlider.Value = VideoPlayer.Position.TotalSeconds;
            }

            UpdatePlaybackTime();
        }

        private void PositionSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingPosition = true;
        }

        private void PositionSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingPosition = false;
            SeekToSliderPosition();
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingPosition)
            {
                UpdatePlaybackTime(TimeSpan.FromSeconds(PositionSlider.Value));
            }
        }

        private void SeekToSliderPosition()
        {
            if (VideoPlayer.Source is null)
            {
                return;
            }

            VideoPlayer.Position = TimeSpan.FromSeconds(PositionSlider.Value);
            UpdatePlaybackTime();
        }

        private void UpdatePlaybackTime(TimeSpan? currentPosition = null)
        {
            TimeSpan current = currentPosition ?? VideoPlayer.Position;
            TimeSpan total = VideoPlayer.NaturalDuration.HasTimeSpan
                ? VideoPlayer.NaturalDuration.TimeSpan
                : TimeSpan.Zero;

            PlaybackTimeText.Text = $"{FormatTime(current)} / {FormatTime(total)}";
        }

        private static string FormatTime(TimeSpan time)
        {
            return time.TotalHours >= 1
                ? time.ToString(@"hh\:mm\:ss")
                : time.ToString(@"mm\:ss");
        }

        private enum VideoSortMode
        {
            Name,
            AddedDate,
            CreatedDate
        }

        private sealed record VideoListItem(
            string Title,
            string FilePath,
            DateTime CreatedDate,
            DateTime AddedDate);
    }
}