using System;

namespace серьёзный.Core.CoreModels
{
    public class TransferJob
    {
        public Guid JobId { get; set; } =
            Guid.NewGuid();

        public int TargetPc { get; set; }

        public string GameId { get; set; } =
            "";

        public string GameName { get; set; } =
            "";

        public string SourceExe { get; set; } =
            "";

        public string SourceImage { get; set; } =
            "";

        public long Size { get; set; }

        public DateTime Created { get; set; } =
            DateTime.Now;
    }
}