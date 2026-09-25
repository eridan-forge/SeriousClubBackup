using System;

namespace серьёзный.Core.CoreModels
{
    public class NetworkPacket
    {
        public string Type { get; set; } = string.Empty;

        public int PcId { get; set; }

        public Guid AccountId { get; set; }

        public DateTime Time { get; set; } = DateTime.UtcNow;

        public string Json { get; set; } = string.Empty;
    }
}