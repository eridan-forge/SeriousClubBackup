namespace серьёзный.Core.CoreModels;

using System.IO;

public class InstalledGame
{
    public string Name { get; set; } = "";

    public string Category { get; set; } = "Игры";

    public string Path { get; set; } = "";

    public string Launcher { get; set; } = "";
}