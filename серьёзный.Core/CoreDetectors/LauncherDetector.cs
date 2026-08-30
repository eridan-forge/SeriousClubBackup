namespace серьёзный.Core.CoreDetectors;

using System.IO;

public static class LauncherDetector
{
    public static string Detect(string exePath)
    {
        var path = exePath.ToLowerInvariant();

        if (path.Contains(@"\steamapps\"))
            return "Steam";

        if (path.Contains("epic games"))
            return "Epic";

        if (path.Contains("rockstar"))
            return "Rockstar";

        if (path.Contains("riot"))
            return "Riot";

        if (path.Contains("ubisoft"))
            return "Ubisoft";

        if (path.Contains("electronic arts") ||
            path.Contains("ea games"))
            return "EA App";

        if (path.Contains("battle.net") ||
            path.Contains("blizzard"))
            return "Battle.net";

        if (path.Contains("minecraft"))
            return "Minecraft";

        if (path.Contains("gog"))
            return "GOG";

        if (path.Contains("heroic"))
            return "Heroic";

        if (path.Contains("itch"))
            return "itch.io";

        if (path.Contains("playnite"))
            return "Playnite";

        return "";
    }
}