using System;
using System.IO;
using System.Windows;

namespace серьёзный.ЭкранКлуба
{
    public partial class App : Application
    {
        public bool ЭтоПервыйЗапускShell { get; private set; }

        public App()
        {
            ЭтоПервыйЗапускShell = true;

            DispatcherUnhandledException += (_, e) =>
            {
                ЗаписатьЛог("DispatcherUnhandledException: " + e.Exception);

                // MessageBox тут НЕ показываем — этот экран видят игроки
                // за компьютерами, всплывающая ошибка их только напугает.
                // Пишем в лог, чтобы админ мог найти причину.
                e.Handled = true;
            };
        }

        private static void ЗаписатьЛог(string текст)
        {
            try
            {
                var папка = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub",
                    "logs");

                Directory.CreateDirectory(папка);

                File.AppendAllText(
                    Path.Combine(папка, "club-screen-crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {текст}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}