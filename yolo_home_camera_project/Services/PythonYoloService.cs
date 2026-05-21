using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using yolo_home_camera_project.Models;

namespace yolo_home_camera_project.Services
{
    public class PythonYoloService
    {
        public async Task<List<YoloDetectionResult>> AnalyzeVideoAsync(
            string videoPath,
            IEnumerable<string> keywords,
            double confidence,
            int vidStride)
        {
            if (string.IsNullOrWhiteSpace(videoPath))
            {
                throw new ArgumentException("분석할 영상 경로가 비어 있습니다.", nameof(videoPath));
            }

            if (!File.Exists(videoPath))
            {
                throw new FileNotFoundException("분석할 영상 파일을 찾을 수 없습니다.", videoPath);
            }

            List<string> keywordList = keywords
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                .Select(keyword => keyword.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (keywordList.Count == 0)
            {
                throw new InvalidOperationException("YOLOE 분석에 사용할 활성 키워드가 없습니다.");
            }

            string projectRoot = FindProjectRoot();

            string pythonDir = Path.Combine(projectRoot, "Python");
            string scriptPath = Path.Combine(pythonDir, "yoloe_detect.py");
            string outputDir = Path.Combine(pythonDir, "output");
            string outputPath = Path.Combine(outputDir, "detection_result.json");
            string modelPath = Path.Combine(pythonDir, "models", "yoloe-11s-seg.pt");
            string snapshotDir = Path.Combine(projectRoot, "Assets", "Snapshots");

            if (!Directory.Exists(pythonDir))
            {
                throw new DirectoryNotFoundException($"Python 폴더를 찾을 수 없습니다: {pythonDir}");
            }

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("YOLOE 분석 스크립트 파일을 찾을 수 없습니다.", scriptPath);
            }

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException("YOLOE 모델 파일을 찾을 수 없습니다.", modelPath);
            }

            Directory.CreateDirectory(outputDir);
            Directory.CreateDirectory(snapshotDir);

            // 이전 결과가 남아 있으면 혼동될 수 있으므로 삭제
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            string keywordText = string.Join(",", keywordList);

            ProcessStartInfo startInfo = new()
            {
                FileName = "python",
                WorkingDirectory = pythonDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add(scriptPath);

            startInfo.ArgumentList.Add("--video");
            startInfo.ArgumentList.Add(videoPath);

            startInfo.ArgumentList.Add("--keywords");
            startInfo.ArgumentList.Add(keywordText);

            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add(modelPath);

            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(outputPath);

            startInfo.ArgumentList.Add("--snapshot-dir");
            startInfo.ArgumentList.Add(snapshotDir);

            startInfo.ArgumentList.Add("--conf");
            startInfo.ArgumentList.Add(confidence.ToString(CultureInfo.InvariantCulture));

            startInfo.ArgumentList.Add("--vid-stride");
            startInfo.ArgumentList.Add(vidStride.ToString(CultureInfo.InvariantCulture));

            startInfo.ArgumentList.Add("--imgsz");
            startInfo.ArgumentList.Add("480");

            using Process process = new()
            {
                StartInfo = startInfo
            };

            process.Start();

            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception(
                    "YOLOE 분석 중 오류가 발생했습니다."
                    + Environment.NewLine
                    + Environment.NewLine
                    + "[STDOUT]"
                    + Environment.NewLine
                    + stdout
                    + Environment.NewLine
                    + Environment.NewLine
                    + "[STDERR]"
                    + Environment.NewLine
                    + stderr
                );
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException(
                    "YOLOE 분석은 종료되었지만 detection_result.json 파일이 생성되지 않았습니다.",
                    outputPath
                );
            }

            string json = await File.ReadAllTextAsync(outputPath);

            List<YoloDetectionResult>? results = JsonSerializer.Deserialize<List<YoloDetectionResult>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return results ?? new List<YoloDetectionResult>();
        }

        private static string FindProjectRoot()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);

            while (current is not null)
            {
                string pythonScriptPath = Path.Combine(current.FullName, "Python", "yoloe_detect.py");

                if (File.Exists(pythonScriptPath))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException(
                "프로젝트 루트를 찾을 수 없습니다. Python/yoloe_detect.py 파일이 프로젝트 내부에 있는지 확인하세요."
            );
        }
    }
}