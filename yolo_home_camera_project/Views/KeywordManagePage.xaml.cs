using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using yolo_home_camera_project.Models;
using yolo_home_camera_project.Services;

namespace yolo_home_camera_project.Views
{
    public partial class KeywordManagePage : Page
    {
        private readonly KeywordService _keywordService = new();
        private readonly List<DetectionKeyword> _keywords = [];
        private bool _isLoaded;
        private bool _isRendering;

        public KeywordManagePage()
        {
            InitializeComponent();
            Loaded += KeywordManagePage_Loaded;
        }

        private async void KeywordManagePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;
            await LoadKeywordsAsync();
        }

        private async Task LoadKeywordsAsync()
        {
            _keywords.Clear();
            _keywords.AddRange(await _keywordService.LoadKeywordsAsync());
            RenderKeywords();
        }

        private void RenderKeywords()
        {
            _isRendering = true;
            KeywordChipPanel.Children.Clear();

            IEnumerable<DetectionKeyword> filteredKeywords = ApplySearch(_keywords);
            filteredKeywords = ApplySort(filteredKeywords);

            foreach (DetectionKeyword keyword in filteredKeywords)
            {
                ToggleButton keywordButton = new()
                {
                    Content = keyword.Keyword,
                    IsChecked = keyword.IsEnabled,
                    Style = (Style)FindResource("KeywordToggleButtonStyle"),
                    Tag = keyword
                };

                keywordButton.Checked += KeywordButton_CheckedChanged;
                keywordButton.Unchecked += KeywordButton_CheckedChanged;

                KeywordChipPanel.Children.Add(keywordButton);
            }

            int enabledCount = _keywords.Count(keyword => keyword.IsEnabled);
            KeywordCountText.Text = $"{enabledCount}개 선택 / {_keywords.Count}개 등록";
            _isRendering = false;
        }

        private IEnumerable<DetectionKeyword> ApplySearch(IEnumerable<DetectionKeyword> keywords)
        {
            string searchText = SearchKeywordTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return keywords;
            }

            return keywords.Where(keyword =>
                keyword.Keyword.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<DetectionKeyword> ApplySort(IEnumerable<DetectionKeyword> keywords)
        {
            string? sortMode = (SortComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            return sortMode == "NameDescending"
                ? keywords.OrderByDescending(keyword => keyword.Keyword)
                : keywords.OrderBy(keyword => keyword.Keyword);
        }

        private async void KeywordButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_isRendering)
            {
                return;
            }

            if (sender is not ToggleButton button || button.Tag is not DetectionKeyword keyword)
            {
                return;
            }

            keyword.IsEnabled = button.IsChecked == true;
            await SaveKeywordsAsync();
            RenderKeywords();
        }

        private void SearchKeywordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded)
            {
                return;
            }

            RenderKeywords();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
            {
                return;
            }

            RenderKeywords();
        }

        private async void AddKeywordButton_Click(object sender, RoutedEventArgs e)
        {
            await AddKeywordAsync();
        }

        private async void AddKeywordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            await AddKeywordAsync();
            e.Handled = true;
        }

        private async Task AddKeywordAsync()
        {
            string keywordText = AddKeywordTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(keywordText))
            {
                MessageBox.Show("추가할 키워드를 입력하세요.");
                return;
            }

            bool alreadyExists = _keywords.Any(keyword =>
                keyword.Keyword.Equals(keywordText, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                MessageBox.Show("이미 등록된 키워드입니다.");
                return;
            }

            _keywords.Add(new DetectionKeyword
            {
                Keyword = keywordText,
                IsEnabled = true,
                Threshold = 0.5
            });

            AddKeywordTextBox.Clear();
            SearchKeywordTextBox.Clear();

            await SaveKeywordsAsync();
            RenderKeywords();
        }

        private async Task SaveKeywordsAsync()
        {
            await _keywordService.SaveKeywordsAsync(_keywords);
        }
    }
}
