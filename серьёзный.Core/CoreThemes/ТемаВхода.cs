namespace серьёзный.Core.CoreThemes;

// Тема — папка с логотипом (logo.png) и фоном: bg.mp4 (видео) ИЛИ
// bg.png/bg.jpg/bg.jpeg (картинка) — заполнено ровно одно из двух
// полей фона. Эффект пепла (fg.mp4) больше не поддерживается: если
// такой файл остался в старой папке темы, он просто игнорируется.
public class ТемаВхода
{
    public string Id { get; set; } = "";

    public string ПутьФонВидео { get; set; } = "";

    public string ПутьФонИзображение { get; set; } = "";

    public bool ФонЭтоВидео =>
        !string.IsNullOrEmpty(ПутьФонВидео);

    public string ПутьЛого { get; set; } = "";
}