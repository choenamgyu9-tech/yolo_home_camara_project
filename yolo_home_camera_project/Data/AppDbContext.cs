using Microsoft.Data.Sqlite;
using System.IO;

namespace yolo_home_camera_project.Data
{
    public class AppDbContext
    {
        private readonly string _databasePath;

        public AppDbContext()
        {
            string projectRoot = FindProjectRoot();
            string dataDir = Path.Combine(projectRoot, "Data");

            Directory.CreateDirectory(dataDir);

            _databasePath = Path.Combine(dataDir, "homecam.db");
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection($"Data Source={_databasePath}");
        }

        public async Task InitializeDatabaseAsync()
        {
            await using SqliteConnection connection = CreateConnection();
            await connection.OpenAsync();

            await ExecuteNonQueryAsync(
                connection,
                """
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS SearchKeywords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Keyword TEXT NOT NULL UNIQUE,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    Threshold REAL NOT NULL DEFAULT 0.5,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS AnalysisRuns (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VideoName TEXT NOT NULL,
                    VideoPath TEXT NOT NULL,
                    Confidence REAL NOT NULL,
                    VidStride INTEGER NOT NULL,
                    Keywords TEXT NOT NULL,
                    EventCount INTEGER NOT NULL DEFAULT 0,
                    AnalyzedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS VideoEvents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AnalysisRunId INTEGER,
                    VideoPath TEXT NOT NULL,
                    EventTime TEXT NOT NULL,
                    EventSeconds REAL NOT NULL,
                    FrameIndex INTEGER NOT NULL,
                    Keyword TEXT NOT NULL,
                    Confidence REAL NOT NULL,
                    SnapshotPath TEXT,
                    BoxX1 INTEGER NOT NULL DEFAULT 0,
                    BoxY1 INTEGER NOT NULL DEFAULT 0,
                    BoxX2 INTEGER NOT NULL DEFAULT 0,
                    BoxY2 INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (AnalysisRunId) REFERENCES AnalysisRuns(Id)
                );

                CREATE INDEX IF NOT EXISTS IX_VideoEvents_EventSeconds
                ON VideoEvents(EventSeconds);

                CREATE INDEX IF NOT EXISTS IX_VideoEvents_Keyword
                ON VideoEvents(Keyword);
                """
            );

            if (!await ColumnExistsAsync(connection, "VideoEvents", "AnalysisRunId"))
            {
                await ExecuteNonQueryAsync(
                    connection,
                    "ALTER TABLE VideoEvents ADD COLUMN AnalysisRunId INTEGER;"
                );

                await ExecuteNonQueryAsync(
                    connection,
                    """
                    CREATE INDEX IF NOT EXISTS IX_VideoEvents_AnalysisRunId
                    ON VideoEvents(AnalysisRunId);
                    """
                );
            }

            await ExecuteNonQueryAsync(
                connection,
                """
                CREATE INDEX IF NOT EXISTS IX_VideoEvents_AnalysisRunId
                ON VideoEvents(AnalysisRunId);
                """
            );
        }

        private static async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql)
        {
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string tableName, string columnName)
        {
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";

            await using SqliteDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string FindProjectRoot()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);

            while (current is not null)
            {
                string dataPath = Path.Combine(current.FullName, "Data");
                string projectDataPath = Path.Combine(current.FullName, "yolo_home_camera_project", "Data");

                if (Directory.Exists(dataPath))
                {
                    return current.FullName;
                }

                if (Directory.Exists(projectDataPath))
                {
                    return Path.Combine(current.FullName, "yolo_home_camera_project");
                }

                current = current.Parent;
            }

            return AppContext.BaseDirectory;
        }
    }
}
