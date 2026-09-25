using System;
using System.IO;

namespace серьёзный.Core.CoreLogs;

public static class LaunchLogger
{
    private static readonly string Folder =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "Logs");

    private static readonly string FilePath =
        Path.Combine(Folder, "launcher.log");

    public static void Write(string text)
    {
        try
        {
            Directory.CreateDirectory(Folder);

            File.AppendAllText(
                FilePath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}