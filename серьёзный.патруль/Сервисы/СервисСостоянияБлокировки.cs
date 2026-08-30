using System.IO;
using System.Text.Json;

namespace серьёзный.Патруль.Сервисы
{
    public static class СервисСостоянияБлокировки
    {
        private static readonly string Папка =
            Path.Combine(
                System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub");

        private static readonly string Файл =
            Path.Combine(
                Папка,
                "patrol.state");

        public static void Сохранить(
            bool заблокирован,
            string названиеПК)
        {
            Directory.CreateDirectory(Папка);

            var состояние =
                new СостояниеБлокировки
                {
                    Заблокирован = заблокирован,
                    НазваниеПК = названиеПК
                };

            File.WriteAllText(
                Файл,
                JsonSerializer.Serialize(
                    состояние,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }
    }
}