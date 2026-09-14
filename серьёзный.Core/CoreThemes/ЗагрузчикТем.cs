using System.IO;

namespace серьёзный.Core.CoreThemes;

public static class ЗагрузчикТем
{
    private static readonly string[] ФайлыФоновойКартинки =
    {
        "bg.png", "bg.jpg", "bg.jpeg"
    };

    // Темы ищем в двух местах:
    // 1) Темы\ рядом с exe — темы, вшитые в сборку.
    // 2) %ProgramData%\SeriousClub\Themes\ — темы, добавленные копированием
    //    папки, без пересборки Visual Studio. Именно сюда стоит класть
    //    новые темы (Космос и т.д.).
    private static IEnumerable<string> ПапкиСТемами()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Темы");

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub", "Themes");
    }

    // Тема валидна, если есть logo.png И хотя бы один вариант фона:
    // bg.mp4 (видео, в приоритете) ИЛИ bg.png/bg.jpg/bg.jpeg (картинка).
    // fg.mp4 больше не читается вообще.
    public static List<ТемаВхода> ЗагрузитьВсе()
    {
        var результат = new List<ТемаВхода>();

        foreach (var корень in ПапкиСТемами())
        {
            if (!Directory.Exists(корень))
                continue;

            foreach (var папкаТемы in Directory.GetDirectories(корень))
            {
                var лого = Path.Combine(папкаТемы, "logo.png");

                if (!File.Exists(лого))
                    continue;

                var видео = Path.Combine(папкаТемы, "bg.mp4");
                var естьВидео = File.Exists(видео);

                var картинка = ФайлыФоновойКартинки
                    .Select(имя => Path.Combine(папкаТемы, имя))
                    .FirstOrDefault(File.Exists);

                if (!естьВидео && картинка == null)
                    continue;

                результат.Add(new ТемаВхода
                {
                    Id = Path.GetFileName(папкаТемы),
                    ПутьФонВидео = естьВидео ? видео : "",
                    ПутьФонИзображение = естьВидео ? "" : картинка!,
                    ПутьЛого = лого
                });
            }
        }

        return результат
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .OrderBy(x => x.Id)
            .ToList();
    }

    public static ТемаВхода? НайтиПоId(string id)
    {
        return ЗагрузитьВсе().FirstOrDefault(x =>
            string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static ТемаВхода? НайтиПоУмолчанию()
    {
        var все = ЗагрузитьВсе();

        if (все.Count == 0)
            return null;

        return все.FirstOrDefault(x =>
                   string.Equals(x.Id, "Дракон", StringComparison.OrdinalIgnoreCase))
               ?? все[0];
    }
}