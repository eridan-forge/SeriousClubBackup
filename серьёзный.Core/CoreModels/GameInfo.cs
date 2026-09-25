namespace серьёзный.Core.CoreModels;

using System.IO;

public class GameInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = "";

    public string Category { get; set; } = "Игры";

    public string Path { get; set; } = "";

    public string Image { get; set; } = "";

    public string Launcher { get; set; } = "";

    public string AppId { get; set; } = "";

    public string Publisher { get; set; } = "";

    public bool Hidden { get; set; }

    public string LaunchArguments { get; set; } = "";

    public string WorkingDirectory { get; set; } = "";
}