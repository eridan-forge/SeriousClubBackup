using System.Text.Json;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreDetectors;

public static class EpicLibraryScanner
{
    public static List<GameInfo> Scan()
    {
        var result = new List<GameInfo>();

        var file =
            EpicDetector.GetLauncher();

        if (file == null)
            return result;

        try
        {
            using var doc =
                JsonDocument.Parse(
                    File.ReadAllText(file));

            foreach (var game in doc.RootElement
                         .GetProperty("InstallationList")
                         .EnumerateArray())
            {
                result.Add(new GameInfo
                {
                    Name =
                        game.GetProperty("AppName").GetString() ?? "",
                    Category = "Epic",
                    Path =
                        game.GetProperty("InstallLocation").GetString() ?? ""
                });
            }
        }
        catch
        {
        }

        return result;
    }
}