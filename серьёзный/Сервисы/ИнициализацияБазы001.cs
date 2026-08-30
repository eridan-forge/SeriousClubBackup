using Microsoft.Data.Sqlite;

namespace серьёзный.Сервисы
{
    public class ИнициализацияБазы001
    {
        private readonly СервисБазы001 база =
            new();

        public void Создать()
        {
            using var db =
                база.Открыть();

            Выполнить(db,
@"
CREATE TABLE IF NOT EXISTS Accounts(
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    RemainingSeconds INTEGER NOT NULL,
    PlayedSeconds INTEGER NOT NULL,
    SessionCount INTEGER NOT NULL,
    LastSession TEXT
);
");

            Выполнить(db,
@"
CREATE TABLE IF NOT EXISTS Sessions(
    Id INTEGER PRIMARY KEY,
    PcId INTEGER NOT NULL,
    AccountId TEXT,
    PlayerName TEXT,
    StartTime TEXT NOT NULL,
    PlannedEnd TEXT NOT NULL,
    Price REAL NOT NULL,
    AccountSeconds INTEGER NOT NULL,
    PurchasedSeconds INTEGER NOT NULL,
    UseBalance INTEGER NOT NULL
);
");

            Выполнить(db,
@"
CREATE TABLE IF NOT EXISTS SessionArchive(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PcId INTEGER,
    PlayerName TEXT,
    AccountId TEXT,
    StartTime TEXT,
    EndTime TEXT,
    PlayedSeconds INTEGER,
    ReturnedSeconds INTEGER,
    Price REAL,
    Reason TEXT
);
");

            Выполнить(db,
@"
CREATE TABLE IF NOT EXISTS ChatMessages(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PcId INTEGER,
    AccountId TEXT,
    Sender TEXT,
    Message TEXT,
    Time TEXT
);
");

            Выполнить(db,
@"
CREATE TABLE IF NOT EXISTS Settings(
    Key TEXT PRIMARY KEY,
    Value TEXT
);
");

            // Безопасная миграция: у старых баз, созданных
            // до этого фикса, может не быть двух новых колонок.
            // ALTER TABLE ADD COLUMN падает, если колонка уже есть,
            // поэтому оборачиваем в try/catch — безопасно на любой версии базы.
            ДобавитьКолонкуЕслиНужно(db, "ChatMessages", "FromAdmin", "INTEGER NOT NULL DEFAULT 0");
            ДобавитьКолонкуЕслиНужно(db, "ChatMessages", "IsRead", "INTEGER NOT NULL DEFAULT 0");
        }

        private static void ДобавитьКолонкуЕслиНужно(
            SqliteConnection db,
            string таблица,
            string колонка,
            string тип)
        {
            try
            {
                Выполнить(
                    db,
                    $"ALTER TABLE {таблица} ADD COLUMN {колонка} {тип};");
            }
            catch
            {
                // Колонка уже существует — это нормально, пропускаем.
            }
        }

        private static void Выполнить(
            SqliteConnection db,
            string sql)
        {
            using var cmd =
                db.CreateCommand();

            cmd.CommandText = sql;

            cmd.ExecuteNonQuery();
        }
    }
}