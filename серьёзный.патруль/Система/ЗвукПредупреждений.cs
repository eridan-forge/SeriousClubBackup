using System;
using System.IO;
using NAudio.Wave;
using серьёзный.патруль.Сервисы;

namespace серьёзный.Патруль.Система
{
    public static class ЗвукПредупреждений
    {
        private static readonly object блокировка = new();

        private static WaveOutEvent? вывод;
        private static AudioFileReader? читатель;

        private static readonly string[] Расширения =
            { ".wav", ".mp3" };

        public static void Проиграть(string имяФайла)
        {
            try
            {
                var путь = НайтиФайл(имяФайла);

                if (путь == null)
                {
                    Лог.Записать(
                        $"Звук предупреждения не найден: Sounds\\{имяФайла}.*");
                    return;
                }

                lock (блокировка)
                {
                    // Если предыдущий звук ещё играет — обрываем,
                    // чтобы предупреждения не накладывались друг на друга.
                    Остановить();

                    читатель = new AudioFileReader(путь);

                    вывод = new WaveOutEvent();

                    вывод.PlaybackStopped += (_, _) =>
                    {
                        lock (блокировка)
                        {
                            Остановить();
                        }
                    };

                    вывод.Init(читатель);
                    вывод.Play();
                }
            }
            catch (Exception ошибка)
            {
                Лог.Записать("Ошибка звука предупреждения: " + ошибка);
            }
        }

        private static void Остановить()
        {
            try { вывод?.Dispose(); } catch { }
            try { читатель?.Dispose(); } catch { }

            вывод = null;
            читатель = null;
        }

        private static string? НайтиФайл(string имяФайла)
        {
            var папка = Path.Combine(AppContext.BaseDirectory, "Sounds");

            foreach (var расширение in Расширения)
            {
                var путь = Path.Combine(папка, имяФайла + расширение);

                if (File.Exists(путь))
                    return путь;
            }

            return null;
        }
    }
}