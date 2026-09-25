using System.Diagnostics;
using System.IO;

namespace серьёзный.Core.CoreLaunch;

public sealed class GameSessionTracker
{
    private readonly Stopwatch watch = new();

    private Process? process;

    public event Action<TimeSpan>? Finished;

    public void Start(Process? game)
    {
        if (game == null)
            return;

        process = game;

        watch.Restart();

        Task.Run(Loop);
    }

    private async Task Loop()
    {
        if (process == null)
            return;

        while (!process.HasExited)
            await Task.Delay(1000);

        watch.Stop();

        Finished?.Invoke(watch.Elapsed);
    }
}