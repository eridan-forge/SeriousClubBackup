using System;

namespace серьёзный.Core.CoreModels;

public class PlayerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Nickname { get; set; } = "Игрок";

    public string Avatar { get; set; } = "";

    public int Level { get; set; } = 1;

    public int Experience { get; set; }

    public int FriendsCount { get; set; }

    public int AchievementsCount { get; set; }

    public int Visits { get; set; }

    public TimeSpan TotalPlayTime { get; set; }

    public string Frame { get; set; } = "Default";

    public string Theme { get; set; } = "Blue";
}