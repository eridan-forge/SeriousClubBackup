using Microsoft.Data.Sqlite;

namespace серьёзный.Сервисы
{
    public class СервисСтатистики007
    {
        private readonly СервисБазы001 база = new();

        public СервисСтатистики007()
        {
            СоздатьТаблицуКорректировок();
        }

        private void СоздатьТаблицуКорректировок()
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS StatisticsAdjustments
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    AdjustmentDate TEXT NOT NULL,
    RevenueDelta REAL NOT NULL DEFAULT 0,
    SessionsDelta INTEGER NOT NULL DEFAULT 0,
    PlayedSecondsDelta INTEGER NOT NULL DEFAULT 0,
    Note TEXT
);";

            cmd.ExecuteNonQuery();
        }

        public СтатистикаКлуба007 Получить(
            DateTime начало,
            DateTime конец)
        {
            using var db = база.Открыть();

            decimal выручка;
            int сеансов;
            long игровыхСекунд;

            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    COUNT(*),
    COALESCE(SUM(Price), 0),
    COALESCE(SUM(PlayedSeconds), 0)
FROM SessionArchive
WHERE EndTime >= @From
  AND EndTime < @To;";

                cmd.Parameters.AddWithValue(
                    "@From",
                    начало.ToString("O"));

                cmd.Parameters.AddWithValue(
                    "@To",
                    конец.ToString("O"));

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    сеансов = reader.GetInt32(0);
                    выручка = reader.GetDecimal(1);
                    игровыхСекунд = reader.GetInt64(2);
                }
                else
                {
                    сеансов = 0;
                    выручка = 0;
                    игровыхСекунд = 0;
                }
            }

            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    COALESCE(SUM(RevenueDelta), 0),
    COALESCE(SUM(SessionsDelta), 0),
    COALESCE(SUM(PlayedSecondsDelta), 0)
FROM StatisticsAdjustments
WHERE AdjustmentDate >= @From
  AND AdjustmentDate < @To;";

                cmd.Parameters.AddWithValue(
                    "@From",
                    начало.Date.ToString("O"));

                cmd.Parameters.AddWithValue(
                    "@To",
                    конец.Date.ToString("O"));

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    выручка += reader.IsDBNull(0)
                        ? 0
                        : reader.GetDecimal(0);

                    сеансов += reader.IsDBNull(1)
                        ? 0
                        : reader.GetInt32(1);

                    игровыхСекунд += reader.IsDBNull(2)
                        ? 0
                        : reader.GetInt64(2);
                }
            }

            if (сеансов < 0)
                сеансов = 0;

            if (игровыхСекунд < 0)
                игровыхСекунд = 0;

            if (выручка < 0)
                выручка = 0;

            var среднийЧек =
                сеансов > 0
                    ? выручка / сеансов
                    : 0;

            return new СтатистикаКлуба007
            {
                Сеансов = сеансов,
                Выручка = выручка,
                ИгровыхСекунд = игровыхСекунд,
                СреднийЧек = среднийЧек
            };
        }

        public List<СтатистикаПоПК007> ПолучитьПоПК(
            DateTime начало,
            DateTime конец)
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText = @"
SELECT
    PcId,
    COUNT(*),
    COALESCE(SUM(Price), 0),
    COALESCE(SUM(PlayedSeconds), 0)
FROM SessionArchive
WHERE EndTime >= @From
  AND EndTime < @To
GROUP BY PcId
ORDER BY PcId;";

            cmd.Parameters.AddWithValue(
                "@From",
                начало.ToString("O"));

            cmd.Parameters.AddWithValue(
                "@To",
                конец.ToString("O"));

            using var reader = cmd.ExecuteReader();

            var результат =
                new List<СтатистикаПоПК007>();

            while (reader.Read())
            {
                результат.Add(
                    new СтатистикаПоПК007
                    {
                        КомпьютерId =
                            reader.GetInt32(0),

                        Сеансов =
                            reader.GetInt32(1),

                        Выручка =
                            reader.GetDecimal(2),

                        ИгровыхСекунд =
                            reader.GetInt64(3)
                    });
            }

            return результат;
        }

        public void ДобавитьКорректировку(
            DateTime дата,
            decimal выручка,
            int сеансы,
            TimeSpan игровоеВремя,
            string? примечание = null)
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText = @"
INSERT INTO StatisticsAdjustments
(
    AdjustmentDate,
    RevenueDelta,
    SessionsDelta,
    PlayedSecondsDelta,
    Note
)
VALUES
(
    @Date,
    @Revenue,
    @Sessions,
    @PlayedSeconds,
    @Note
);";

            cmd.Parameters.AddWithValue(
                "@Date",
                дата.Date.ToString("O"));

            cmd.Parameters.AddWithValue(
                "@Revenue",
                выручка);

            cmd.Parameters.AddWithValue(
                "@Sessions",
                сеансы);

            cmd.Parameters.AddWithValue(
                "@PlayedSeconds",
                (long)игровоеВремя.TotalSeconds);

            cmd.Parameters.AddWithValue(
                "@Note",
                (object?)примечание ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public void УдалитьВсеКорректировки(
            DateTime начало,
            DateTime конец)
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText = @"
DELETE FROM StatisticsAdjustments
WHERE AdjustmentDate >= @From
  AND AdjustmentDate < @To;";

            cmd.Parameters.AddWithValue(
                "@From",
                начало.Date.ToString("O"));

            cmd.Parameters.AddWithValue(
                "@To",
                конец.Date.ToString("O"));

            cmd.ExecuteNonQuery();
        }
    }

    public class СтатистикаКлуба007
    {
        public int Сеансов { get; set; }

        public decimal Выручка { get; set; }

        public long ИгровыхСекунд { get; set; }

        public decimal СреднийЧек { get; set; }

        public TimeSpan ИгровоеВремя =>
            TimeSpan.FromSeconds(
                Math.Max(0, ИгровыхСекунд));
    }

    public class СтатистикаПоПК007
    {
        public int КомпьютерId { get; set; }

        public int Сеансов { get; set; }

        public decimal Выручка { get; set; }

        public long ИгровыхСекунд { get; set; }

        public TimeSpan ИгровоеВремя =>
            TimeSpan.FromSeconds(
                Math.Max(0, ИгровыхСекунд));
    }
}