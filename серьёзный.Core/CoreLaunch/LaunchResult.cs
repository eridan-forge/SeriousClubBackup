using System.Diagnostics;
using System.IO;

namespace серьёзный.Core.CoreLaunch;

public class LaunchResult
{
    public bool Success { get; set; }

    public Process? Process { get; set; }

    public string Message { get; set; } = "";
}