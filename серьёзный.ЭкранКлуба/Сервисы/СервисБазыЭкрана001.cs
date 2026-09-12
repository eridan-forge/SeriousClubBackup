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
            // ВАЖНО: колонки в INSERT перечислены явно. Раньше было
            // "VALUES (1,1,1)" без имён — SQLite в этом случае требует
            // значение НА КАЖДУЮ колонку таблицы. Ниже, в
            // ДобавитьКолонкуАккаунтаЕслиНужно, к ScreenState через
            // ALTER TABLE добавляется 4-я колонка (AccountId). После
            // первого же её добавления ЛЮБОЙ следующий вызов Открыть()
            // падал с "table ScreenState has 4 columns but 3 values
            // were supplied" ещё до возврата соединения — именно
            // поэтому кнопка "Обслуживание" не открывала окно пароля.
            // С явными именами колонок SQLite подставляет значения
            // только в них, остальные (AccountId) остаются NULL —
            // независимо от реального числа колонок в таблице.
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
(
    Id,
    AdminName,
    Password,
    Title
)
VALUES
(
    1,
    'Администратор',
    '123456',
    'Обратитесь к администратору'
);

INSERT OR IGNORE INTO ScreenState
(
    Id,
    Locked,
    PcId
)
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