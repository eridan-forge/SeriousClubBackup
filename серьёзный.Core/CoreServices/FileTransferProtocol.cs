using System;

namespace серьёзный.Core.CoreServices
{
    public class FileTransferHeader
    {
        public Guid JobId { get; set; }

        public string FileName { get; set; } = "";

        public string GameId { get; set; } = "";

        public long Size { get; set; }

        public bool IsImage { get; set; }
    }
}