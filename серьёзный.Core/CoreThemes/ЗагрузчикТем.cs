using System.IO;

namespace серьёзный.Core.CoreThemes;

public static class ЗагрузчикТем
{
    // Ищем темы в двух местах:
    // 1) Темы\ рядом с exe — темы, которые едут вместе со сборкой.
    // 2) %ProgramData%\SeriousClub\Themes\ — темы, добавленные позже
    //    простым копированием папки, без пересборки Visual Studio.
    private static IEnumerable<string> ПапкиСТемами()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Темы");

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub", "Themes");
    }

    // Тема попадает в список, только если у неё есть все три файла.
    public static List<ТемаВхода> ЗагрузитьВсе()
    {
        var результат = new List<ТемаВхода>();

        foreach (var корень in ПапкиСТемами())
        {
            if (!Directory.Exists(корень))
                continue;

            foreach (var папкаТемы in Directory.GetDirectories(корень))
            {
                var фон = Path.Combine(папкаТемы, "bg.mp4");
                var эффект = Path.Combine(папкаТемы, "fg.mp4");
                var лого = Path.Combine(папкаТемы, "logo.png");

                if (!File.Exists(фон) || !File.Exists(эффект) || !File.Exists(лого))
                    continue;

                результат.Add(new ТемаВхода
                {
                    Id = Path.GetFileName(папкаТемы),
                    ПутьФон = фон,
                    ПутьЭффект = эффект,
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
}