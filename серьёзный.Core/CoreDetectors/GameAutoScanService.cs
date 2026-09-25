using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using серьёзный.Core.CoreLauncher;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreDetectors;

public static class GameAutoScanService
{
    private static readonly string[] ЮнкСлова =
    {
        "unins", "setup", "install", "redist", "vcredist", "vc_redist",
        "directx", "dxsetup", "dotnet", "crashpad", "crashreporter",
        "crash_reporter", "crashhandler", "updater", "update.exe",
        "helper", "service", "cleanup", "config", "settings",
        "launcher.exe", "webhelper", "browserhelper", "reporter",
        "uploader", "benchmark", "editor.exe", "server.exe",
        "dedicated", "activation", "vcruntime", "dotnetfx",
        "prereq", "support", "diagnostics", "repair"
    };

    public static List<GameEntry> Сканировать()
    {
        var найдено = LauncherUniversalScanner.ScanAll();

        var результат = new List<GameEntry>();

        foreach (var игра in найдено)
        {
            if (string.IsNullOrWhiteSpace(игра.Name))
                continue;

            var путь = РазрешитьИсполняемый(игра.Path, игра.Name);

            if (string.IsNullOrWhiteSpace(путь))
                continue;

            var файл = Path.GetFileName(путь).ToLowerInvariant();

            if (ЮнкСлова.Any(слово => файл.Contains(слово)))
                continue;

            результат.Add(new GameEntry
            {
                Id = string.IsNullOrWhiteSpace(игра.Id)
                    ? Guid.NewGuid().ToString()
                    : игра.Id,

                Name = игра.Name.Trim(),

                Category = string.IsNullOrWhiteSpace(игра.Category)
                    ? "Игры"
                    : игра.Category,

                Description = игра.Launcher,

                Image = игра.Image,

                Path = путь,

                Hidden = false,

                AppId = игра.AppId ?? "",

                Launcher = игра.Launcher ?? ""
            });
        }

        return результат
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();
    }

    private static string? РазрешитьИсполняемый(
        string путьИлиПапка,
        string названиеИгры)
    {
        if (string.IsNullOrWhiteSpace(путьИлиПапка))
            return null;

        if (File.Exists(путьИлиПапка))
            return путьИлиПапка;

        if (!Directory.Exists(путьИлиПапка))
            return null;

        List<string> exeФайлы;

        try
        {
            exeФайлы = Directory.GetFiles(
                    путьИлиПапка,
                    "*.exe",
                    SearchOption.AllDirectories)
                .ToList();
        }
        catch
        {
            return null;
        }

        if (exeФайлы.Count == 0)
            return null;

        var кандидаты = exeФайлы
            .Where(f => !ЮнкСлова.Any(
                слово => Path.GetFileName(f)
                    .ToLowerInvariant()
                    .Contains(слово)))
            .ToList();

        if (кандидаты.Count == 0)
            кандидаты = exeФайлы;

        var нормальноеИмя =
            названиеИгры.Replace(" ", "").ToLowerInvariant();

        var поИмени = кандидаты.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f)
                .Replace(" ", "")
                .ToLowerInvariant()
                .Contains(нормальноеИмя));

        if (поИмени != null)
            return поИмени;

        try
        {
            return кандидаты
                .OrderByDescending(f => new FileInfo(f).Length)
                .First();
        }
        catch
        {
            return кандидаты.First();
        }
    }
}