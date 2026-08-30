using System;
using System.Collections.Generic;
using System.IO;

namespace серьёзный.Сервисы.ОпределениеИгр
{
    public static class GameDetector
    {
        private static readonly Dictionary<string, GamePreset> presets =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["gta5.exe"] = new("Grand Theft Auto V", "Rockstar"),
                ["playgtav.exe"] = new("Grand Theft Auto V", "Rockstar"),
                ["cs2.exe"] = new("Counter-Strike 2", "Steam"),
                ["csgo.exe"] = new("Counter-Strike 2", "Steam"),
                ["dota2.exe"] = new("Dota 2", "Steam"),
                ["valorant.exe"] = new("VALORANT", "Riot"),
                ["leagueclient.exe"] = new("League of Legends", "Riot"),
                ["epicgameslauncher.exe"] = new("Epic Games Launcher", "Epic"),
                ["steam.exe"] = new("Steam", "Steam"),
                ["battle.net.exe"] = new("Battle.net", "Battle.net"),
                ["launcher.exe"] = new("Rockstar Games Launcher", "Rockstar")
            };

        public static GamePreset Detect(string exePath)
        {
            var exe =
                Path.GetFileName(exePath);

            if (presets.TryGetValue(exe, out var game))
            {
                return game with { Executable = exePath };
            }

            return new GamePreset(
                Path.GetFileNameWithoutExtension(exe),
                "Игры")
            {
                Executable = exePath
            };
        }
    }

    public record GamePreset(string Name, string Category)
    {
        public string Executable { get; init; } = "";

        public string? Cover { get; init; }
    }
}