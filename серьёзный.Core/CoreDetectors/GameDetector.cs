using System.IO;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreDetectors;

public static class GameDetector
{
    private static readonly Dictionary<string, (string Name, string Category)>
        KnownGames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["gta5.exe"] = ("Grand Theft Auto V", "Экшен"),
                ["playgta5.exe"] = ("Grand Theft Auto V", "Экшен"),
                ["cs2.exe"] = ("Counter-Strike 2", "Шутер"),
                ["dota2.exe"] = ("Dota 2", "MOBA"),
                ["valorant.exe"] = ("VALORANT", "Шутер"),
                ["leagueclient.exe"] = ("League of Legends", "MOBA"),
                ["r5apex.exe"] = ("Apex Legends", "Шутер"),
                ["fortniteclient-win64-shipping.exe"] = ("Fortnite", "Battle Royale"),
                ["eldenring.exe"] = ("Elden Ring", "RPG"),
                ["witcher3.exe"] = ("The Witcher 3", "RPG"),
                ["cyberpunk2077.exe"] = ("Cyberpunk 2077", "RPG"),
                ["minecraftlauncher.exe"] = ("Minecraft", "Песочница"),
                ["javaw.exe"] = ("Minecraft Java", "Песочница"),
                ["overwatch.exe"] = ("Overwatch 2", "Шутер"),
                ["wow.exe"] = ("World of Warcraft", "MMORPG"),
                ["rdr2.exe"] = ("Red Dead Redemption 2", "Экшен"),
                ["eafc25.exe"] = ("EA SPORTS FC 25", "Спорт"),
                ["rocketleague.exe"] = ("Rocket League", "Спорт"),
                ["terraria.exe"] = ("Terraria", "Песочница"),
                ["factorio.exe"] = ("Factorio", "Стратегия"),
                ["hoi4.exe"] = ("Hearts of Iron IV", "Стратегия"),
                ["eu4.exe"] = ("Europa Universalis IV", "Стратегия")
            };

    public static GameInfo Detect(string exePath)
    {
        var file =
            Path.GetFileName(exePath);

        var launcher =
            LauncherDetector.Detect(exePath);

        if (KnownGames.TryGetValue(file, out var game))
        {
            return new GameInfo
            {
                Name = game.Name,
                Category = game.Category,
                Path = exePath,
                Launcher = launcher,
                Publisher = ExeMetadataReader.GetCompany(exePath)
            };
        }

        var meta =
            ExeMetadataReader.GetName(exePath);

        return new GameInfo
        {
            Name = meta,
            Category = string.IsNullOrWhiteSpace(launcher)
                ? "Игры"
                : launcher,
            Path = exePath,
            Launcher = launcher,
            Publisher = ExeMetadataReader.GetCompany(exePath)
        };
    }
}