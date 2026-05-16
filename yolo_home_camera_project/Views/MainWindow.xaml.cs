using System.Windows;
using System.Windows.Media.Animation;

namespace yolo_home_camera_project.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isMenuOpen;

        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new HomePage());
        }

        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isMenuOpen)
            {
                CloseSideMenu();
            }
            else
            {
                OpenSideMenu();
            }
        }

        private void OpenSideMenu()
        {
            SideMenuPanel.Visibility = Visibility.Visible;
            _isMenuOpen = true;
            AnimateSideMenu(-220, 0);
        }

        private void CloseSideMenu()
        {
            _isMenuOpen = false;

            DoubleAnimation animation = CreateSlideAnimation(0, -220);
            animation.Completed += (_, _) => SideMenuPanel.Visibility = Visibility.Collapsed;
            SideMenuTranslateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation);
        }

        private void AnimateSideMenu(double from, double to)
        {
            DoubleAnimation animation = CreateSlideAnimation(from, to);
            SideMenuTranslateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation);
        }

        private static DoubleAnimation CreateSlideAnimation(double from, double to)
        {
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };
        }

        private void VideoAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new VideoAnalysisPage());
            CloseSideMenu();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HomePage());
            CloseSideMenu();
        }

        private void KeywordManageButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new KeywordManagePage());
            CloseSideMenu();
        }

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ReportPage());
            CloseSideMenu();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage());
            CloseSideMenu();
        }
    }
}
