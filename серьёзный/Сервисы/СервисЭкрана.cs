using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace серьёзный.Сервисы
{
    public static class СервисЭкрана
    {
        private static readonly string путь =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub",
                "SeriousClub.db");

        private static SqliteConnection Открыть()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(путь)!);

            var db =
                new SqliteConnection(
                    $"Data Source={путь}");

            db.Open();

            using var cmd =
                db.CreateCommand();

            cmd.CommandText =
@"
PRAGMA journal_mode=WAL;
PRAGMA busy_timeout=3000;

CREATE TABLE IF NOT EXISTS ScreenState
(
    Id INTEGER PRIMARY KEY CHECK(Id=1),
    Locked INTEGER,
    Password TEXT,
    Title TEXT
);

INSERT OR IGNORE INTO ScreenState
VALUES(1,1,'123456','Серьёзный');
";

            cmd.ExecuteNonQuery();

            return db;
        }

        public static void Заблокировать()
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                "UPDATE ScreenState SET Locked=1 WHERE Id=1";

            cmd.ExecuteNonQuery();
        }

        public static void Разблокировать()
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                "UPDATE ScreenState SET Locked=0 WHERE Id=1";

            cmd.ExecuteNonQuery();
        }

        public static void СменитьПароль(string пароль)
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                "UPDATE ScreenState SET Password=$p WHERE Id=1";

            cmd.Parameters.AddWithValue("$p", пароль);

            cmd.ExecuteNonQuery();
        }

        public static void ИзменитьТекст(string текст)
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                "UPDATE ScreenState SET Title=$t WHERE Id=1";

            cmd.Parameters.AddWithValue("$t", текст);

            cmd.ExecuteNonQuery();
        }
    }
}