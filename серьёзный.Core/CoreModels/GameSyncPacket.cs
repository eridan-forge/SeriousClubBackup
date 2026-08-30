using System.Collections.Generic;
using System.IO;

namespace серьёзный.Core.CoreModels
{
    public class GameSyncPacket
    {
        public int PcId { get; set; }

        public List<GameEntry> Games { get; set; } =
            new();
    }

    public class GameEntry
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public string Category { get; set; } = "";

        public string Description { get; set; } = "";

        public string Image { get; set; } = "";

        public string Path { get; set; } = "";

        public bool Hidden { get; set; }
    }
}