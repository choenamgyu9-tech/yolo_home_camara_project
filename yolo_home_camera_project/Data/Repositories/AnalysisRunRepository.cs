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
    }
}
