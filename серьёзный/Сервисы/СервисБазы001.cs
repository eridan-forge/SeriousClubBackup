using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace серьёзный.Сервисы
{
    public class СервисБазы001
    {
        private readonly string путь;

        private static bool таблицыСозданы;

        private static readonly object блокировка =
            new();

        private const int ТекущаяВерсияБазы = 3;


        // =========================================================
        // КОНСТРУКТОР
        // =========================================================

        public СервисБазы001()
        {
            путь =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub",
                    "SeriousClub.db");
        }


        // =========================================================
        // ОТКРЫТЬ БАЗУ
        // =========================================================

        public SqliteConnection Открыть()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(путь)!);

            var соединение =
                new SqliteConnection(
                    $"Data Source={путь}");

            соединение.Open();

            using (var pragma = соединение.CreateCommand())
            {
                pragma.CommandText =
                    @"
PRAGMA journal_mode=WAL;
PRAGMA busy_timeout=5000;
PRAGMA foreign_keys=ON;";

                pragma.ExecuteNonQuery();
            }

            СоздатьТаблицыЕслиНужно(
                соединение);

            return соединение;
        }


        // =========================================================
        // СОЗДАНИЕ / МИГРАЦИЯ
        // =========================================================

        private static void СоздатьТаблицыЕслиНужно(
            SqliteConnection соединение)
        {
            lock (блокировка)
            {
                if (таблицыСозданы)
                {
                    return;
                }

                СоздатьТаблицы(
                    соединение);

                ПроверитьИОбновитьAccounts(
                    соединение);

                ПроверитьИСоздатьИндексы(
                    соединение);

                УстановитьВерсиюБазы(
                    соединение,
                    ТекущаяВерсияБазы);

                таблицыСозданы = true;
            }
        }


        // =========================================================
        // ОСНОВНЫЕ ТАБЛИЦЫ
        // =========================================================

        private static void СоздатьТаблицы(
            SqliteConnection соединение)
        {
            using var cmd =
                соединение.CreateCommand();

            cmd.CommandText =
                @"
CREATE TABLE IF NOT EXISTS Accounts
(
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    Password TEXT NOT NULL DEFAULT '',
    RemainingSeconds INTEGER NOT NULL DEFAULT 0,
    PlayedSeconds INTEGER NOT NULL DEFAULT 0,
    SessionCount INTEGER NOT NULL DEFAULT 0,
    LastSession TEXT NULL
);

CREATE TABLE IF NOT EXISTS SessionArchive
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    PcId INTEGER NOT NULL,

    PlayerName TEXT NOT NULL,

    AccountId TEXT NULL,

    StartTime TEXT NOT NULL,

    EndTime TEXT NOT NULL,

    PlayedSeconds INTEGER NOT NULL DEFAULT 0,

    ReturnedSeconds INTEGER NOT NULL DEFAULT 0,

    Price REAL NOT NULL DEFAULT 0,

    Reason TEXT NULL
);

CREATE TABLE IF NOT EXISTS DatabaseInfo
(
    Id INTEGER PRIMARY KEY CHECK(Id = 1),

    Version INTEGER NOT NULL
);";

            cmd.ExecuteNonQuery();
        }


        // =========================================================
        // MIGRATION ACCOUNTS
        // =========================================================

        private static void ПроверитьИОбновитьAccounts(
            SqliteConnection соединение)
        {
            if (!ТаблицаСодержитСтолбец(
                    соединение,
                    "Accounts",
                    "Password"))
            {
                ДобавитьСтолбецPassword(
                    соединение);
            }

            /*
             * Старые версии программы использовали:
             *
             * LastName = второе поле аккаунта.
             *
             * Теперь это поле означает пароль.
             *
             * Поэтому переносим старое значение только
             * в том случае, если Password ещё пустой.
             */

            if (ТаблицаСодержитСтолбец(
                    соединение,
                    "Accounts",
                    "LastName"))
            {
                using (var cmd = соединение.CreateCommand())
                {
                    cmd.CommandText =
                        @"
UPDATE Accounts
SET Password = LastName
WHERE
    (Password IS NULL OR Password = '')
    AND LastName IS NOT NULL
    AND LastName <> '';";

                    cmd.ExecuteNonQuery();
                }

                /*
                 * КРИТИЧНО: старая схема объявляла LastName как
                 * TEXT NOT NULL без значения по умолчанию.
                 *
                 * SQLite не умеет снимать NOT NULL через ALTER TABLE,
                 * поэтому единственный безопасный способ убрать это
                 * ограничение — пересоздать таблицу без колонки
                 * LastName, перенеся все данные.
                 *
                 * Без этого шага INSERT OR REPLACE INTO Accounts(...),
                 * который больше не указывает LastName, падает с
                 * ошибкой "NOT NULL constraint failed: Accounts.LastName".
                 */

                using (var cmd = соединение.CreateCommand())
                {
                    cmd.CommandText =
                        @"
CREATE TABLE Accounts_New
(
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    Password TEXT NOT NULL DEFAULT '',
    RemainingSeconds INTEGER NOT NULL DEFAULT 0,
    PlayedSeconds INTEGER NOT NULL DEFAULT 0,
    SessionCount INTEGER NOT NULL DEFAULT 0,
    LastSession TEXT NULL
);

INSERT INTO Accounts_New
(
    Id,
    FirstName,
    Password,
    RemainingSeconds,
    PlayedSeconds,
    SessionCount,
    LastSession
)
SELECT
    Id,
    FirstName,
    COALESCE(Password, ''),
    COALESCE(RemainingSeconds, 0),
    COALESCE(PlayedSeconds, 0),
    COALESCE(SessionCount, 0),
    LastSession
FROM Accounts;

DROP TABLE Accounts;

ALTER TABLE Accounts_New RENAME TO Accounts;
";

                    cmd.ExecuteNonQuery();
                }
            }


            /*
             * Нормализуем старые NULL/отрицательные значения.
             */

            using (var cmd = соединение.CreateCommand())
            {
                cmd.CommandText =
                    @"
UPDATE Accounts
SET
    Password =
        COALESCE(Password, ''),

    RemainingSeconds =
        CASE
            WHEN RemainingSeconds < 0
                THEN 0
            ELSE RemainingSeconds
        END,

    PlayedSeconds =
        CASE
            WHEN PlayedSeconds < 0
                THEN 0
            ELSE PlayedSeconds
        END,

    SessionCount =
        CASE
            WHEN SessionCount < 0
                THEN 0
            ELSE SessionCount
        END;";

                cmd.ExecuteNonQuery();
            }
        }


        private static void ДобавитьСтолбецPassword(
            SqliteConnection соединение)
        {
            using var cmd =
                соединение.CreateCommand();

            cmd.CommandText =
                @"
ALTER TABLE Accounts
ADD COLUMN Password TEXT NOT NULL DEFAULT '';";

            cmd.ExecuteNonQuery();
        }


        // =========================================================
        // ПРОВЕРКА СТОЛБЦА
        // =========================================================

        private static bool ТаблицаСодержитСтолбец(
            SqliteConnection соединение,
            string таблица,
            string столбец)
        {
            using var cmd =
                соединение.CreateCommand();

            cmd.CommandText =
                $"PRAGMA table_info({таблица});";

            using var reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                var имя =
                    reader.IsDBNull(1)
                        ? string.Empty
                        : reader.GetString(1);

                if (string.Equals(
                        имя,
                        столбец,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }


        // =========================================================
        // ИНДЕКСЫ
        // =========================================================

        private static void ПроверитьИСоздатьИндексы(
            SqliteConnection соединение)
        {
            using var cmd =
                соединение.CreateCommand();

            cmd.CommandText =
                @"
CREATE INDEX IF NOT EXISTS IX_Accounts_FirstName
ON Accounts(FirstName);

CREATE INDEX IF NOT EXISTS IX_Accounts_Password
ON Accounts(Password);

CREATE INDEX IF NOT EXISTS IX_Accounts_LastSession
ON Accounts(LastSession);

CREATE INDEX IF NOT EXISTS IX_SessionArchive_PcId
ON SessionArchive(PcId);

CREATE INDEX IF NOT EXISTS IX_SessionArchive_AccountId
ON SessionArchive(AccountId);

CREATE INDEX IF NOT EXISTS IX_SessionArchive_StartTime
ON SessionArchive(StartTime);

CREATE INDEX IF NOT EXISTS IX_SessionArchive_EndTime
ON SessionArchive(EndTime);";

            cmd.ExecuteNonQuery();
        }


        // =========================================================
        // ВЕРСИЯ БАЗЫ
        // =========================================================

        private static void УстановитьВерсиюБазы(
            SqliteConnection соединение,
            int версия)
        {
            using var cmd =
                соединение.CreateCommand();

            cmd.CommandText =
                @"
INSERT INTO DatabaseInfo
(
    Id,
    Version
)
VALUES
(
    1,
    @Version
)
ON CONFLICT(Id)
DO UPDATE SET
    Version = excluded.Version;";

            cmd.Parameters.AddWithValue(
                "@Version",
                версия);

            cmd.ExecuteNonQuery();
        }


        // =========================================================
        // ПОЛУЧЕНИЕ ПУТИ БАЗЫ
        // =========================================================

        public string ПолучитьПуть()
        {
            return путь;
        }
    }
}