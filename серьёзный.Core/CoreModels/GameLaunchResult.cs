namespace серьёзный.Core.CoreModels;

using System.IO;

public class GameLaunchResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = "";

    public string? FixedPath { get; set; }
}