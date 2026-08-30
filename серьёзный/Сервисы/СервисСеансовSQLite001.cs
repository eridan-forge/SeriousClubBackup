using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using серьёзный.Модели;

namespace серьёзный.Сервисы
{
    public class СервисСеансовSQLite001
    {
        private readonly СервисБазы001 база = new();

        public void Сохранить(Сеанс сеанс)
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
@"
INSERT OR REPLACE INTO Sessions
(
Id,
PcId,
AccountId,
PlayerName,
StartTime,
PlannedEnd,
Price,
AccountSeconds,
PurchasedSeconds,
UseBalance
)
VALUES
(
@Id,
@Pc,
@Account,
@Player,
@Start,
@End,
@Price,
@AccountSeconds,
@PurchasedSeconds,
@UseBalance
);
";

            cmd.Parameters.AddWithValue("@Id", сеанс.Id);
            cmd.Parameters.AddWithValue("@Pc", сеанс.КомпьютерId);
            cmd.Parameters.AddWithValue("@Account",
                (object?)сеанс.АккаунтGuid?.ToString() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Player", сеанс.ИмяКлиента);
            cmd.Parameters.AddWithValue("@Start", сеанс.Начало.ToString("O"));
            cmd.Parameters.AddWithValue("@End", сеанс.ЗапланированноеОкончание.ToString("O"));
            cmd.Parameters.AddWithValue("@Price", сеанс.Стоимость);
            cmd.Parameters.AddWithValue("@AccountSeconds", (long)сеанс.ВремяАккаунта.TotalSeconds);
            cmd.Parameters.AddWithValue("@PurchasedSeconds", (long)сеанс.КупленноеВремя.TotalSeconds);
            cmd.Parameters.AddWithValue("@UseBalance",
                сеанс.ИспользуетсяОстатокАккаунта ? 1 : 0);

            cmd.ExecuteNonQuery();
        }

        public void Удалить(int id)
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                "DELETE FROM Sessions WHERE Id=@Id";

            cmd.Parameters.AddWithValue("@Id", id);

            cmd.ExecuteNonQuery();
        }

        public List<Сеанс001> Загрузить()
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
@"
SELECT
Id,
PcId,
AccountId,
PlayerName,
StartTime,
PlannedEnd,
Price,
AccountSeconds,
PurchasedSeconds,
UseBalance
FROM Sessions;
";

            using var reader = cmd.ExecuteReader();

            var список = new List<Сеанс001>();

            while (reader.Read())
            {
                var начало =
                    DateTime.Parse(reader.GetString(4));

                var конец =
                    DateTime.Parse(reader.GetString(5));

                список.Add(
                    new Сеанс001
                    {
                        Id = reader.GetInt32(0),
                        КомпьютерId = reader.GetInt32(1),
                        АккаунтGuid =
                            reader.IsDBNull(2)
                                ? null
                                : Guid.Parse(reader.GetString(2)),
                        ИмяКлиента = reader.GetString(3),
                        Начало = начало,
                        ЗапланированноеОкончание = конец,
                        Стоимость = reader.GetDecimal(6),
                        ВремяАккаунта =
                            TimeSpan.FromSeconds(reader.GetInt64(7)),
                        КупленноеВремя =
                            TimeSpan.FromSeconds(reader.GetInt64(8)),
                        ИспользуетсяОстатокАккаунта =
                            reader.GetInt32(9) == 1
                    });
            }

            return список;
        }
    }
}