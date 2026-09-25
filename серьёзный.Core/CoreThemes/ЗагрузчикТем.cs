using System.IO;

namespace серьёзный.Core.CoreThemes;

public static class ЗагрузчикТем
{
    private static readonly string[] ФайлыФоновойКартинки =
    {
        "bg.png", "bg.jpg", "bg.jpeg"
    };

    // Темы ищем в двух местах:
    // 1) Темы\ рядом с exe — вшитые в сборку.
    // 2) %ProgramData%\SeriousClub\Themes\ — добавленные копированием папки.
    private static IEnumerable<string> ПапкиСТемами()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Темы");

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub", "Themes");
    }

    // Тема валидна, если есть logo.png И хотя бы один mp4 ИЛИ картинка фона.
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

                // ВСЕ mp4 папки = плейлист темы. Порядок — естественная
                // сортировка имён, поэтому bg2.mp4 идёт раньше bg10.mp4.
                List<string> видео;

                try
                {
                    видео = Directory.GetFiles(папкаТемы, "*.mp4")
                        .Where(файл => !Path.GetFileName(файл)
                            .StartsWith("fg", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(файл => Path.GetFileName(файл), ЕстественноеСравнение.Экземпляр)
                        .ToList();
                }
                catch
                {
                    видео = new List<string>();
                }

                var картинка = ФайлыФоновойКартинки
                    .Select(имя => Path.Combine(папкаТемы, имя))
                    .FirstOrDefault(File.Exists);

                if (видео.Count == 0 && картинка == null)
                    continue;

                результат.Add(new ТемаВхода
                {
                    Id = Path.GetFileName(папкаТемы),
                    Видео = видео,
                    ПутьФонИзображение = видео.Count > 0 ? "" : картинка!,
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

    // "bg2.mp4" < "bg10.mp4" — обычная строковая сортировка дала бы наоборот.
    private sealed class ЕстественноеСравнение : IComparer<string>
    {
        public static readonly ЕстественноеСравнение Экземпляр = new();

        public int Compare(string? первая, string? вторая)
        {
            var a = первая ?? "";
            var b = вторая ?? "";

            int i = 0, j = 0;

            while (i < a.Length && j < b.Length)
            {
                if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
                {
                    int началоA = i, началоB = j;

                    while (i < a.Length && char.IsDigit(a[i])) i++;
                    while (j < b.Length && char.IsDigit(b[j])) j++;

                    var числоA = a.Substring(началоA, i - началоA).TrimStart('0');
                    var числоB = b.Substring(началоB, j - началоB).TrimStart('0');

                    if (числоA.Length != числоB.Length)
                        return числоA.Length - числоB.Length;

                    var сравнение = string.CompareOrdinal(числоA, числоB);

                    if (сравнение != 0)
                        return сравнение;
                }
                else
                {
                    var сравнение =
                        char.ToUpperInvariant(a[i]).CompareTo(char.ToUpperInvariant(b[j]));

                    if (сравнение != 0)
                        return сравнение;

                    i++;
                    j++;
                }
            }

            return (a.Length - i) - (b.Length - j);
        }
    }
}