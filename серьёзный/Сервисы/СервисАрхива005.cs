using System;
using System.IO;
using System.Text.Json;
using серьёзный.Модели;
using серьёзный.Сервисы;




namespace серьёзный.Сервисы
{

    public class СервисАрхива005
    {
        private readonly string путь =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub",
                "archive005.json");
        private readonly СервисБазы001 база =
    new();

        public void Добавить(
    ЗаписьАварии005 запись)
        {
            using var db =
                база.Открыть();

            using var cmd =
                db.CreateCommand();

            cmd.CommandText =
        @"
INSERT INTO SessionArchive
(
PcId,
PlayerName,
AccountId,
StartTime,
EndTime,
PlayedSeconds,
ReturnedSeconds,
Price,
Reason
)
VALUES
(
@Pc,
@Player,
@Account,
@Start,
@End,
@Played,
@Returned,
@Price,
@Reason
);";

            cmd.Parameters.AddWithValue(
                "@Pc",
                запись.КомпьютерId);

            cmd.Parameters.AddWithValue(
                "@Player",
                запись.Игрок);

            cmd.Parameters.AddWithValue(
                "@Account",
                (object?)запись.АккаунтGuid?.ToString()
                    ?? DBNull.Value);

            cmd.Parameters.AddWithValue(
                "@Start",
                запись.Начало.ToString("O"));

            cmd.Parameters.AddWithValue(
                "@End",
                запись.Отключение.ToString("O"));

            cmd.Parameters.AddWithValue(
                "@Played",
                (long)запись.Сыграно.TotalSeconds);

            cmd.Parameters.AddWithValue(
                "@Returned",
                (long)запись.Возвращено.TotalSeconds);

            cmd.Parameters.AddWithValue(
                "@Price",
                запись.Стоимость);

            cmd.Parameters.AddWithValue(
    "@Reason",
    запись.Причина);

            cmd.ExecuteNonQuery();
        }

        public List<ЗаписьАварии005> ПолучитьВсе()
        {
            using var db =
                база.Открыть();

            using var cmd =
                db.CreateCommand();

            cmd.CommandText =
    @"
SELECT
Id,
PcId,
PlayerName,
AccountId,
StartTime,
EndTime,
PlayedSeconds,
ReturnedSeconds,
Price
FROM SessionArchive
ORDER BY EndTime DESC;
";

            using var reader =
                cmd.ExecuteReader();

            var список =
                new List<ЗаписьАварии005>();

            while (reader.Read())
            {
                список.Add(
                    new ЗаписьАварии005
                    {
                        Id = reader.GetInt32(0),

                        КомпьютерId = reader.GetInt32(1),

                        Игрок =
                            reader.GetString(2),

                        АккаунтGuid =
                            reader.IsDBNull(3)
                                ? null
                                : Guid.Parse(reader.GetString(3)),

                        Начало =
                            DateTime.Parse(reader.GetString(4)),

                        Отключение =
                            DateTime.Parse(reader.GetString(5)),

                        Сыграно =
                            TimeSpan.FromSeconds(reader.GetInt64(6)),

                        Возвращено =
                            TimeSpan.FromSeconds(reader.GetInt64(7)),

                        Стоимость =
                            reader.GetDecimal(8)
                    });
            }

            return список;
        }

        public void УдалитьЗапись(int id)
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText = "DELETE FROM SessionArchive WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        public void УдалитьПоАккаунту(Guid аккаунтId)
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText = "DELETE FROM SessionArchive WHERE AccountId = @Account;";
            cmd.Parameters.AddWithValue("@Account", аккаунтId.ToString());
            cmd.ExecuteNonQuery();
        }

        public void УдалитьВсё()
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText = "DELETE FROM SessionArchive;";
            cmd.ExecuteNonQuery();
        }
    }
}