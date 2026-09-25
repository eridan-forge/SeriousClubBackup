using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using серьёзный.Core.CoreLogs;

namespace серьёзный.ЭкранКлуба.Сервисы;

public static class PatrolProcessLauncher
{
    private const string ИмяПроцесса = "серьёзный.патруль";

    private const string ИмяФайла = "серьёзный.патруль.exe";

    public static bool Запущен()
    {
        try
        {
            return Process.GetProcessesByName(ИмяПроцесса).Length > 0;
        }
        catch
        {
            // На ошибке считаем "запущен", чтобы не плодить дубликаты.
            return true;
        }
    }

    public static void ЗапуститьЕслиНужно()
    {
        try
        {
            if (Запущен())
                return;

            var путь = НайтиПутьКПатрулю();

            if (string.IsNullOrWhiteSpace(путь))
            {
                LaunchLogger.Write(
                    "PatrolProcessLauncher: exe Патруля не найден ни по одному из путей.");

                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = путь,
                WorkingDirectory = Path.GetDirectoryName(путь),
                UseShellExecute = true
            });

            LaunchLogger.Write($"PatrolProcessLauncher: запущен {путь}");
        }
        catch (Exception ошибка)
        {
            LaunchLogger.Write("PatrolProcessLauncher: ошибка запуска — " + ошибка);
        }
    }

    // =========================================================
    // ПОИСК EXE ПАТРУЛЯ
    // =========================================================

    private static string? НайтиПутьКПатрулю()
    {
        var изКонфига = ПутьИзКонфига();

        if (!string.IsNullOrWhiteSpace(изКонфига) && File.Exists(изКонфига))
            return изКонфига;

        var втойЖеПапке = Path.Combine(AppContext.BaseDirectory, ИмяФайла);

        if (File.Exists(втойЖеПапке))
            return втойЖеПапке;

        var соседний = СоседнийПутьПоРешению();

        if (!string.IsNullOrWhiteSpace(соседний) && File.Exists(соседний))
            return соседний;

        return null;
    }

    private static string? ПутьИзКонфига()
    {
        try
        {
            var файлКонфига =
                Path.Combine(AppContext.BaseDirectory, "client-config.json");

            if (!File.Exists(файлКонфига))
                return null;

            var json = File.ReadAllText(файлКонфига);

            var конфиг = JsonSerializer.Deserialize<КлиентскаяКонфигурация>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return конфиг?.ПутьКПатрулю;
        }
        catch
        {
            return null;
        }
    }

    // Заменяет сегмент "серьёзный.ЭкранКлуба" в текущем пути на
    // "серьёзный.патруль", оставляя остальное (bin\Debug\netX...)
    // без изменений — работает при любой конфигурации сборки, пока
    // оба проекта лежат рядом по структуре решения (как в .slnx).
    private static string? СоседнийПутьПоРешению()
    {
        try
        {
            var текущийПуть =
                AppContext.BaseDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

            var маркер =
                Path.DirectorySeparatorChar +
                "серьёзный.ЭкранКлуба" +
                Path.DirectorySeparatorChar;

            var индекс =
                текущийПуть.IndexOf(маркер, StringComparison.OrdinalIgnoreCase);

            if (индекс < 0)
                return null;

            var до = текущийПуть.Substring(0, индекс);
            var после = текущийПуть.Substring(индекс + маркер.Length);

            var папкаПатруля =
                Path.Combine(до, "серьёзный.патруль", после);

            return Path.Combine(папкаПатруля, ИмяФайла);
        }
        catch
        {
            return null;
        }
    }

    private class КлиентскаяКонфигурация
    {
        public string? ПутьКПатрулю { get; set; }
    }
}