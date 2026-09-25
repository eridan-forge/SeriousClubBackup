using System.Collections.Generic;

namespace серьёзный.Core.CoreModels;

public class GameScanResultDto
{
    public List<GameEntry> Games { get; set; } = new();
}