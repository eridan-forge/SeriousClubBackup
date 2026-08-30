using System.IO;

namespace серьёзный.Core.CoreLaunch;

public static class GameLaunchGuard
{
    public static bool CanLaunch(
        string path,
        out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Путь к игре пуст.";
            return false;
        }

        if (!File.Exists(path))
        {
            error = "Исполняемый файл не найден.";
            return false;
        }

        try
        {
            using var stream =
                File.Open(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

            return true;
        }
        catch
        {
            error = "Нет доступа к игре.";
            return false;
        }
    }
}