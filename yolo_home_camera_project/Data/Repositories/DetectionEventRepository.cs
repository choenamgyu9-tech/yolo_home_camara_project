using Microsoft.Data.Sqlite;
using yolo_home_camera_project.Models;

namespace yolo_home_camera_project.Data.Repositories
{
    public class DetectionEventRepository
    {
        private readonly AppDbContext _dbContext = new();

        public async Task SaveDetectionResultsAsync(int analysisRunId, IEnumerable<YoloDetectionResult> detections)
        {
            await _dbContext.InitializeDatabaseAsync();

            await using SqliteConnection connection = _dbContext.CreateConnection();
            await connection.OpenAsync();

            using SqliteTransaction transaction = connection.BeginTransaction();

            foreach (YoloDetectionResult detection in detections)
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    """
                    INSERT INTO VideoEvents (
                        AnalysisRunId,
                        VideoPath,
                        EventTime,
                        EventSeconds,
                        FrameIndex,
                        Keyword,
                        Confidence,
                        SnapshotPath,
                        BoxX1,
                        BoxY1,
                        BoxX2,
                        BoxY2
                    )
                    VALUES (
                        @analysisRunId,
                        @videoPath,
                        @eventTime,
                        @eventSeconds,
                        @frameIndex,
                        @keyword,
                        @confidence,
                        @snapshotPath,
                        @boxX1,
                        @boxY1,
                        @boxX2,
                        @boxY2
                    );
                    """;

                command.Parameters.AddWithValue("@analysisRunId", analysisRunId);
                command.Parameters.AddWithValue("@videoPath", detection.VideoPath);
                command.Parameters.AddWithValue("@eventTime", detection.EventTime);
                command.Parameters.AddWithValue("@eventSeconds", detection.EventSeconds);
                command.Parameters.AddWithValue("@frameIndex", detection.FrameIndex);
                command.Parameters.AddWithValue("@keyword", detection.Keyword);
                command.Parameters.AddWithValue("@confidence", detection.Confidence);
                command.Parameters.AddWithValue("@snapshotPath", detection.SnapshotPath);
                command.Parameters.AddWithValue("@boxX1", detection.Box.X1);
                command.Parameters.AddWithValue("@boxY1", detection.Box.Y1);
                command.Parameters.AddWithValue("@boxX2", detection.Box.X2);
                command.Parameters.AddWithValue("@boxY2", detection.Box.Y2);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }

        public async Task<List<DetectionEvent>> GetEventsByAnalysisRunIdAsync(int analysisRunId)
        {
            await _dbContext.InitializeDatabaseAsync();

            List<DetectionEvent> events = new();

            await using SqliteConnection connection = _dbContext.CreateConnection();
            await connection.OpenAsync();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT
                    Id,
                    AnalysisRunId,
                    VideoPath,
                    EventTime,
                    EventSeconds,
                    FrameIndex,
                    Keyword,
                    Confidence,
                    IFNULL(SnapshotPath, ''),
                    BoxX1,
                    BoxY1,
                    BoxX2,
                    BoxY2,
                    CreatedAt
                FROM VideoEvents
                WHERE AnalysisRunId = @analysisRunId
                ORDER BY EventSeconds ASC, Id ASC;
                """;

            command.Parameters.AddWithValue("@analysisRunId", analysisRunId);

            await using SqliteDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                events.Add(new DetectionEvent
                {
                    Id = reader.GetInt32(0),
                    AnalysisRunId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    VideoPath = reader.GetString(2),
                    EventTime = reader.GetString(3),
                    EventSeconds = reader.GetDouble(4),
                    FrameIndex = reader.GetInt32(5),
                    Keyword = reader.GetString(6),
                    Confidence = reader.GetDouble(7),
                    SnapshotPath = reader.GetString(8),
                    BoxX1 = reader.GetInt32(9),
                    BoxY1 = reader.GetInt32(10),
                    BoxX2 = reader.GetInt32(11),
                    BoxY2 = reader.GetInt32(12),
                    CreatedAt = DateTime.TryParse(reader.GetString(13), out DateTime createdAt)
                        ? createdAt
                        : DateTime.MinValue
                });
            }

            return events;
        }

        public async Task<int> GetTodayEventCountAsync()
        {
            await _dbContext.InitializeDatabaseAsync();

            await using SqliteConnection connection = _dbContext.CreateConnection();
            await connection.OpenAsync();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT COUNT(*)
                FROM VideoEvents
                WHERE date(CreatedAt, 'localtime') = date('now', 'localtime');
                """;

            object? result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        public async Task<List<DetectionEvent>> GetRecentEventsAsync(int count)
        {
            await _dbContext.InitializeDatabaseAsync();

            List<DetectionEvent> events = new();

            await using SqliteConnection connection = _dbContext.CreateConnection();
            await connection.OpenAsync();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT
                    Id,
                    AnalysisRunId,
                    VideoPath,
                    EventTime,
                    EventSeconds,
                    FrameIndex,
                    Keyword,
                    Confidence,
                    IFNULL(SnapshotPath, ''),
                    BoxX1,
                    BoxY1,
                    BoxX2,
                    BoxY2,
                    CreatedAt
                FROM VideoEvents
                ORDER BY datetime(CreatedAt) DESC, Id DESC
                LIMIT @count;
                """;

            command.Parameters.AddWithValue("@count", count);

            await using SqliteDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                events.Add(new DetectionEvent
                {
                    Id = reader.GetInt32(0),
                    AnalysisRunId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    VideoPath = reader.GetString(2),
                    EventTime = reader.GetString(3),
                    EventSeconds = reader.GetDouble(4),
                    FrameIndex = reader.GetInt32(5),
                    Keyword = reader.GetString(6),
                    Confidence = reader.GetDouble(7),
                    SnapshotPath = reader.GetString(8),
                    BoxX1 = reader.GetInt32(9),
                    BoxY1 = reader.GetInt32(10),
                    BoxX2 = reader.GetInt32(11),
                    BoxY2 = reader.GetInt32(12),
                    CreatedAt = DateTime.TryParse(reader.GetString(13), out DateTime createdAt)
                        ? createdAt
                        : DateTime.MinValue
                });
            }

            return events;
        }
    }
}
