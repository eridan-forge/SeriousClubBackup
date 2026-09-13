namespace серьёзный.Core.CoreThemes;

// Одна тема = одна папка с ровно тремя файлами: bg.mp4 (фон), fg.mp4
// (эффект поверх всего окна) и logo.png (надпись "СЕРЬЁЗНЫЙ").
// Никакого JSON-манифеста не нужно — Id темы это просто имя папки.
public class ТемаВхода
{
    public string Id { get; set; } = "";        // имя папки = уникальный Id темы
    public string ПутьФон { get; set; } = "";     // полный путь к bg.mp4
    public string ПутьЭффект { get; set; } = "";  // полный путь к fg.mp4
    public string ПутьЛого { get; set; } = "";    // полный путь к logo.png
}