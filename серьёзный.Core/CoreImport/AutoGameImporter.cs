using System.IO;
using серьёзный.Core.CoreDetectors;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreImport;

public static class AutoGameImporter
{
    public static GameInfo ImportExe(string exePath)
    {
        var info = GameDetector.Detect(exePath);

        info.Path = exePath;

        if (string.IsNullOrWhiteSpace(info.Name))
            info.Name = Path.GetFileNameWithoutExtension(exePath);

        if (string.IsNullOrWhiteSpace(info.Category))
            info.Category = "Игры";

        return info;
    }
}