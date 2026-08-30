using System;
using System.Collections.Generic;
using System.Linq;
using серьёзный.Модели;

namespace серьёзный.Сервисы
{
    public class СервисЧата
    {
        private readonly СервисБазы001 база =
            new();

        private readonly Dictionary<int, List<ЗаписьЧата>>
            история = new();

        public event Action<int>? ИсторияИзменилась;

        public СервисЧата()
        {
            ЗагрузитьИзБазы();
        }

        private void ЗагрузитьИзБазы()
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"SELECT
                    Id,
                    PcId,
                    AccountId,
                    Sender,
                    Message,
                    Time,
                    FromAdmin,
                    IsRead
                  FROM ChatMessages
                  ORDER BY Time ASC, Id ASC;";

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var запись =
                    new ЗаписьЧата
                    {
                        Id =
                            reader.GetInt64(0),

                        КомпьютерId =
                            reader.GetInt32(1),

                        АккаунтGuid =
                            reader.IsDBNull(2)
                                ? null
                                : Guid.Parse(
                                    reader.GetString(2)),

                        Имя =
                            reader.IsDBNull(3)
                                ? ""
                                : reader.GetString(3),

                        Текст =
                            reader.IsDBNull(4)
                                ? ""
                                : reader.GetString(4),

                        Время =
                            DateTime.Parse(
                                reader.GetString(5)),

                        ОтАдминистратора =
                            reader.GetInt32(6) == 1,

                        Прочитано =
                            reader.GetInt32(7) == 1
                    };

                if (!история.ContainsKey(
                        запись.КомпьютерId))
                {
                    история[запись.КомпьютерId] =
                        new List<ЗаписьЧата>();
                }

                история[запись.КомпьютерId]
                    .Add(запись);
            }
        }

        public void Добавить(
            ЗаписьЧата сообщение)
        {
            if (!история.ContainsKey(
                    сообщение.КомпьютерId))
            {
                история[сообщение.КомпьютерId] =
                    new List<ЗаписьЧата>();
            }

            using (var db = база.Открыть())
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText =
                    @"
INSERT INTO ChatMessages
(
    PcId,
    AccountId,
    Sender,
    Message,
    Time,
    FromAdmin,
    IsRead
)
VALUES
(
    @Pc,
    @Account,
    @Sender,
    @Message,
    @Time,
    @FromAdmin,
    @IsRead
);";

                cmd.Parameters.AddWithValue(
                    "@Pc",
                    сообщение.КомпьютерId);

                cmd.Parameters.AddWithValue(
                    "@Account",
                    (object?)сообщение.АккаунтGuid?.ToString()
                    ?? DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@Sender",
                    сообщение.Имя);

                cmd.Parameters.AddWithValue(
                    "@Message",
                    сообщение.Текст);

                cmd.Parameters.AddWithValue(
                    "@Time",
                    сообщение.Время.ToString("O"));

                cmd.Parameters.AddWithValue(
                    "@FromAdmin",
                    сообщение.ОтАдминистратора
                        ? 1
                        : 0);

                cmd.Parameters.AddWithValue(
                    "@IsRead",
                    сообщение.Прочитано
                        ? 1
                        : 0);

                cmd.ExecuteNonQuery();

                using var idCmd =
                    db.CreateCommand();

                idCmd.CommandText =
                    "SELECT last_insert_rowid();";

                сообщение.Id =
     Convert.ToInt64(
         idCmd.ExecuteScalar());
            }

            история[сообщение.КомпьютерId]
                .Add(сообщение);

            ИсторияИзменилась?.Invoke(
                сообщение.КомпьютерId);
        }

        public IReadOnlyList<ЗаписьЧата>
            ПолучитьИсторию(
                int компьютерId)
        {
            if (!история.TryGetValue(
                    компьютерId,
                    out var список))
            {
                return Array.Empty<ЗаписьЧата>();
            }

            return список;
        }

        public IReadOnlyList<ЗаписьЧата>
            ПолучитьИсториюПоАккаунту(
                Guid аккаунтId)
        {
            return история.Values
                .SelectMany(x => x)
                .Where(x =>
                    x.АккаунтGuid == аккаунтId)
                .OrderBy(x => x.Время)
                .ToList();
        }

        public int ПолучитьНепрочитанные(
            int компьютерId)
        {
            if (!история.TryGetValue(
                    компьютерId,
                    out var список))
            {
                return 0;
            }

            return список.Count(x =>
                !x.ОтАдминистратора &&
                !x.Прочитано);
        }

        public void ОтметитьПрочитанными(
            int компьютерId)
        {
            if (!история.TryGetValue(
                    компьютерId,
                    out var список))
            {
                return;
            }

            var непрочитанные =
                список
                    .Where(x =>
                        !x.ОтАдминистратора &&
                        !x.Прочитано)
                    .ToList();

            if (непрочитанные.Count == 0)
                return;

            foreach (var сообщение
                     in непрочитанные)
            {
                сообщение.Прочитано = true;
            }

            using (var db = база.Открыть())
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText =
                    @"
UPDATE ChatMessages
SET IsRead = 1
WHERE PcId = @Pc
  AND FromAdmin = 0
  AND IsRead = 0;";

                cmd.Parameters.AddWithValue(
                    "@Pc",
                    компьютерId);

                cmd.ExecuteNonQuery();
            }

            ИсторияИзменилась?.Invoke(
                компьютерId);
        }

        public IEnumerable<ЗаписьЧата> Поиск(
            int компьютерId,
            string текст)
        {
            if (!история.TryGetValue(
                    компьютерId,
                    out var список))
            {
                return Enumerable.Empty<ЗаписьЧата>();
            }

            return список.Where(x =>
                x.Текст.Contains(
                    текст,
                    StringComparison.OrdinalIgnoreCase));
        }

        public void Удалить(
            long сообщениеId)
        {
            ЗаписьЧата? найденное = null;

            foreach (var список
                     in история.Values)
            {
                найденное =
                    список.FirstOrDefault(
                        x => x.Id == сообщениеId);

                if (найденное != null)
                    break;
            }

            if (найденное == null)
                return;

            using (var db = база.Открыть())
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText =
                    @"
DELETE FROM ChatMessages
WHERE Id = @Id;";

                cmd.Parameters.AddWithValue(
                    "@Id",
                    сообщениеId);

                cmd.ExecuteNonQuery();
            }

            if (история.TryGetValue(
                    найденное.КомпьютерId,
                    out var списокСообщений))
            {
                списокСообщений.RemoveAll(
                    x => x.Id == сообщениеId);
            }

            ИсторияИзменилась?.Invoke(
                найденное.КомпьютерId);
        }

        public void УдалитьИсторию(
            int компьютерId)
        {
            история.Remove(компьютерId);

            using (var db = база.Открыть())
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText =
                    "DELETE FROM ChatMessages WHERE PcId = @Pc;";

                cmd.Parameters.AddWithValue(
                    "@Pc",
                    компьютерId);

                cmd.ExecuteNonQuery();
            }

            ИсторияИзменилась?.Invoke(
                компьютерId);
        }

        public void УдалитьИсториюПоАккаунту(
            Guid аккаунтId)
        {
            foreach (var список
                     in история.Values)
            {
                список.RemoveAll(
                    x => x.АккаунтGuid ==
                         аккаунтId);
            }

            using (var db = база.Открыть())
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText =
                    @"
DELETE FROM ChatMessages
WHERE AccountId = @Account;";

                cmd.Parameters.AddWithValue(
                    "@Account",
                    аккаунтId.ToString());

                cmd.ExecuteNonQuery();
            }
        }

        public void УдалитьВсё()
        {
            история.Clear();

            using (var db = база.Открыть())
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText =
                    "DELETE FROM ChatMessages;";

                cmd.ExecuteNonQuery();
            }
        }
    }
}