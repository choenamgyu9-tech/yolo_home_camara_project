using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace yolo_home_camera_project.Views
{
    public partial class VideoAnalysisPage : Page
    {
        private readonly DispatcherTimer _playbackTimer;
        private bool _isDraggingPosition;

        public VideoAnalysisPage()
        {
            InitializeComponent();

            _playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _playbackTimer.Tick += PlaybackTimer_Tick;
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
            VideoPlayer.Stop();
            VideoPlayer.Source = new Uri(fileName);
            EmptyVideoText.Visibility = Visibility.Collapsed;
            SelectedVideoText.Text = $"Selected video: {Path.GetFileName(fileName)}";
            VideoPlayer.Play();
            _playbackTimer.Start();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.Source is null)
            {
                OpenVideoButton_Click(sender, e);
                return;
            }

            VideoPlayer.Play();
            _playbackTimer.Start();
            EmptyVideoText.Visibility = Visibility.Collapsed;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Pause();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            PositionSlider.Value = 0;
            UpdatePlaybackTime();
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
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
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _playbackTimer.Stop();
            VideoPlayer.Stop();
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

            LoadVideo(fileName);
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
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
            string extension = Path.GetExtension(fileName);
            return extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".avi", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".mov", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : null;
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
    }
}
