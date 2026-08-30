using System;
using System.IO;

namespace серьёзный.Core.CoreBackup
{
    public class BackupManifest
    {
        public DateTime Created { get; set; } =
            DateTime.Now;

        public string Version { get; set; } =
            "1.0";

        public string ComputerName { get; set; } =
            Environment.MachineName;
    }
}