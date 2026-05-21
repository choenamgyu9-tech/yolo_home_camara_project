using System.IO;
using System.Text.Json;
using yolo_home_camera_project.Models;

namespace yolo_home_camera_project.Services
{
    public class KeywordService
    {
        private readonly string _keywordFilePath;

        public KeywordService()
        {
            string projectRoot = FindProjectRoot();
            string configDir = Path.Combine(projectRoot, "Config");

            Directory.CreateDirectory(configDir);

            _keywordFilePath = Path.Combine(configDir, "keywords.json");

            EnsureKeywordFileExists();
        }

        public async Task<List<DetectionKeyword>> LoadKeywordsAsync()
        {
            EnsureKeywordFileExists();

            string json = await File.ReadAllTextAsync(_keywordFilePath);

            return JsonSerializer.Deserialize<List<DetectionKeyword>>(json)
                   ?? new List<DetectionKeyword>();
        }

        public async Task<List<string>> LoadEnabledKeywordNamesAsync()
        {
            List<DetectionKeyword> keywords = await LoadKeywordsAsync();

            return keywords
                .Where(keyword => keyword.IsEnabled)
                .Select(keyword => keyword.Keyword.Trim())
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task SaveKeywordsAsync(IEnumerable<DetectionKeyword> keywords)
        {
            string json = JsonSerializer.Serialize(
                keywords,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await File.WriteAllTextAsync(_keywordFilePath, json);
        }

        private void EnsureKeywordFileExists()
        {
            if (File.Exists(_keywordFilePath))
            {
                return;
            }

            List<DetectionKeyword> defaultKeywords = new()
            {
                new DetectionKeyword { Keyword = "person", IsEnabled = true, Threshold = 0.5 },
                new DetectionKeyword { Keyword = "dog", IsEnabled = true, Threshold = 0.5 },
                new DetectionKeyword { Keyword = "cat", IsEnabled = true, Threshold = 0.5 },
                new DetectionKeyword { Keyword = "car", IsEnabled = true, Threshold = 0.5 },
                new DetectionKeyword { Keyword = "package", IsEnabled = true, Threshold = 0.5 },
                new DetectionKeyword { Keyword = "smoke", IsEnabled = true, Threshold = 0.5 },
                new DetectionKeyword { Keyword = "fire", IsEnabled = true, Threshold = 0.5 },
                new DetectionKeyword { Keyword = "cellphone", IsEnabled = true, Threshold = 0.5 }
            };

            string json = JsonSerializer.Serialize(
                defaultKeywords,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(_keywordFilePath, json);
        }

        private static string FindProjectRoot()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);

            while (current is not null)
            {
                string configPath = Path.Combine(current.FullName, "Config");
                string pythonPath = Path.Combine(current.FullName, "Python");

                if (Directory.Exists(configPath) || Directory.Exists(pythonPath))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("프로젝트 루트를 찾을 수 없습니다.");
        }
    }
}