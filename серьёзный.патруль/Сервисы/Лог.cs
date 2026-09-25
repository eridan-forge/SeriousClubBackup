using System;
using System.IO;

namespace серьёзный.патруль.Сервисы
{
    public static class Лог
    {
        private static readonly string файл =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SeriousClubPatrol",
                "patrol.log");

        public static void Записать(string текст)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(файл)!);

                File.AppendAllText(
                    файл,
                    $"[{DateTime.Now:HH:mm:ss}] {текст}\n");
            }
            catch
            {
                // логирование не должно ронять процесс само по себе
            }
        }
    }
}