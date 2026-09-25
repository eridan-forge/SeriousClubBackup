using System.IO;

namespace серьёзный.Core.CoreRepair;

public static class GamePathRepairService
{
    public static string? Repair(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Протокольные ссылки (steam://, com.epicgames.launcher:// и
        // т.п.) — не файлы, сканировать диски в их поисках бессмысленно.
        if (path.Contains("://", StringComparison.Ordinal))
            return path;

        if (File.Exists(path))
            return path;

        string name = Path.GetFileName(path);

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
                continue;

            try
            {
                var file = Find(drive.RootDirectory.FullName, name);

                if (file != null)
                    return file;
            }
            catch
            {
            }
        }

        return null;
    }

    private static string? Find(string folder, string file)
    {
        try
        {
            foreach (var f in Directory.GetFiles(folder))
            {
                if (Path.GetFileName(f).Equals(
                    file,
                    StringComparison.OrdinalIgnoreCase))
                    return f;
            }

            foreach (var dir in Directory.GetDirectories(folder))
            {
                var found = Find(dir, file);

                if (found != null)
                    return found;
            }
        }
        catch
        {
        }

        return null;
    }
}