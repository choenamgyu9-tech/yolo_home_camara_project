using Microsoft.Data.Sqlite;
using System.IO;
using yolo_home_camera_project.Models;

namespace yolo_home_camera_project.Data.Repositories
{
    public class AnalysisRunRepository
    {
        private readonly AppDbContext _dbContext = new();

        public async Task<int> CreateAnalysisRunAsync(
            string videoPath,
            double confidence,
            int vidStride,
            IEnumerable<string> keywords)
        {
            await _dbContext.InitializeDatabaseAsync();

            await using SqliteConnection connection = _dbContext.CreateConnection();
            await connection.OpenAsync();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO AnalysisRuns (
                    VideoName,
                    VideoPath,
                    Confidence,
                    VidStride,
                    Keywords
                )
                VALUES (
                    @videoName,
                    @videoPath,
                    @confidence,
                    @vidStride,
                    @keywords
                );

                SELECT last_insert_rowid();
                """;

            command.Parameters.AddWithValue("@videoName", Path.GetFileName(videoPath));
            command.Parameters.AddWithValue("@videoPath", videoPath);
            command.Parameters.AddWithValue("@confidence", confidence);
            command.Parameters.AddWithValue("@vidStride", vidStride);
            command.Parameters.AddWithValue("@keywords", string.Join(", ", keywords));

            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        public async Task UpdateEventCountAsync(int analysisRunId, int eventCount)
        {
            await _dbContext.InitializeDatabaseAsync();

            await using SqliteConnection connection = _dbContext.CreateConnection();
            await connection.OpenAsync();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                UPDATE AnalysisRuns
                SET EventCount = @eventCount
                WHERE Id = @analysisRunId;
                """;

            command.Parameters.AddWithValue("@eventCount", eventCount);
            command.Parameters.AddWithValue("@analysisRunId", analysisRunId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<AnalysisRun>> GetAnalysisRunsAsync()
        {
            await _dbContext.InitializeDatabaseAsync();

            List<AnalysisRun> analysisRuns = new();

            await using SqliteConnection connection = _dbContext.CreateConnection();
            await connection.OpenAsync();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT
                    Id,
                    VideoName,
                    VideoPath,
                    Confidence,
                    VidStride,
                    Keywords,
                    EventCount,
                    AnalyzedAt
                FROM AnalysisRuns
                ORDER BY datetime(AnalyzedAt) DESC, Id DESC;
                """;

            await using SqliteDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                analysisRuns.Add(new AnalysisRun
                {
                    Id = reader.GetInt32(0),
                    VideoName = reader.GetString(1),
                    VideoPath = reader.GetString(2),
                    Confidence = reader.GetDouble(3),
                    VidStride = reader.GetInt32(4),
                    Keywords = reader.GetString(5),
                    EventCount = reader.GetInt32(6),
                    AnalyzedAt = DateTime.TryParse(reader.GetString(7), out DateTime analyzedAt)
                        ? analyzedAt
                        : DateTime.MinValue
                });
            }

            return analysisRuns;
        }

        public async Task<int> GetTodayAnalysisRunCountAsync()
        {
            List<AnalysisRun> analysisRuns = await GetAnalysisRunsAsync();

            return analysisRuns.Count(analysisRun => analysisRun.AnalyzedAt.Date == DateTime.Today);
        }

        public async Task<AnalysisRun?> GetLatestAnalysisRunAsync()
        {
            List<AnalysisRun> analysisRuns = await GetAnalysisRunsAsync();

            return analysisRuns.FirstOrDefault();
        }

        public async Task<List<AnalysisRun>> GetRecentAnalysisRunsAsync(int count)
        {
            List<AnalysisRun> analysisRuns = await GetAnalysisRunsAsync();

            return analysisRuns.Take(count).ToList();
        }
    }
}
