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
    }
}
