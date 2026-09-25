namespace серьёзный.Core.CoreServices.Launch;

public class LaunchInfo
{
    public LaunchMethod Method { get; set; }

    public string Target { get; set; } = "";

    public string Arguments { get; set; } = "";

    public string WorkingDirectory { get; set; } = "";
}