namespace серьёзный.Сервисы.ОпределениеИгр
{
    // Раньше был отдельный от Core.CoreDetectors словарь известных игр —
    // одна и та же игра получала разную категорию в зависимости от
    // способа добавления. Теперь оба пути идут через один детектор.
    public static class GameDetector
    {
        public static GamePreset Detect(string exePath)
        {
            var info =
                серьёзный.Core.CoreDetectors.GameDetector.Detect(exePath);

            return new GamePreset(info.Name, info.Category)
            {
                Executable = exePath,
                AppId = info.AppId,
                Launcher = info.Launcher
            };
        }
    }

    public record GamePreset(string Name, string Category)
    {
        public string Executable { get; init; } = "";

        public string? Cover { get; init; }

        public string AppId { get; init; } = "";

        public string Launcher { get; init; } = "";
    }
}