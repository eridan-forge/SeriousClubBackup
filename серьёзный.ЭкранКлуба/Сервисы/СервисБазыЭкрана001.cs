using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace серьёзный.ЭкранКлуба.Сервисы;

public static class СервисБазыЭкрана001
{
    private static readonly string путь =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public static SqliteConnection Открыть()
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(путь)!);

        var db = new SqliteConnection(
            $"Data Source={путь}");

        db.Open();

        using (var pragma = db.CreateCommand())
        {
            pragma.CommandText = @"
PRAGMA journal_mode=WAL;
PRAGMA busy_timeout=3000;";
            pragma.ExecuteNonQuery();
        }

        using (var cmd = db.CreateCommand())
        {
            cmd.CommandText = @"

CREATE TABLE IF NOT EXISTS ScreenConfig
(
    Id INTEGER PRIMARY KEY CHECK(Id=1),
    AdminName TEXT NOT NULL,
    Password TEXT NOT NULL,
    Title TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ScreenState
(
    Id INTEGER PRIMARY KEY CHECK(Id=1),
    Locked INTEGER NOT NULL,
    PcId INTEGER NOT NULL
);

INSERT OR IGNORE INTO ScreenConfig
VALUES
(
1,
'Администратор',
'123456',
'Обратитесь к администратору'
);

INSERT OR IGNORE INTO ScreenState
VALUES
(
1,
1,
1
);
";
            cmd.ExecuteNonQuery();
        }

        ДобавитьКолонкуАккаунтаЕслиНужно(db);

        return db;
    }

    private static void ДобавитьКолонкуАккаунтаЕслиНужно(
        SqliteConnection db)
    {
        using var check = db.CreateCommand();

        check.CommandText = "PRAGMA table_info(ScreenState);";

        using (var reader = check.ExecuteReader())
        {
            while (reader.Read())
            {
                var имя = reader.GetString(1);

                if (имя == "AccountId")
                    return;
            }
        }

        using var alter = db.CreateCommand();

        alter.CommandText =
            "ALTER TABLE ScreenState ADD COLUMN AccountId TEXT;";

        alter.ExecuteNonQuery();
    }
}