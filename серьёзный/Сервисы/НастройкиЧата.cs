using System;
using System.IO;

namespace серьёзный.Сервисы
{
    public static class НастройкиЧата
    {
        private static readonly string путь =
            Path.Combine(
                AppContext.BaseDirectory,
                "chat-settings.txt");

        public static string ЗагрузитьИмяАдминистратора()
        {
            try
            {
                if (!File.Exists(путь))
                    return "Администратор";

                var текст =
                    File.ReadAllText(путь).Trim();

                return string.IsNullOrWhiteSpace(текст)
                    ? "Администратор"
                    : текст;
            }
            catch
            {
                return "Администратор";
            }
        }

        public static void СохранитьИмяАдминистратора(
            string имя)
        {
            try
            {
                File.WriteAllText(путь, имя);
            }
            catch
            {
            }
        }
    }
}