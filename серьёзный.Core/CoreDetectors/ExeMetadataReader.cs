using System.Diagnostics;
using System.IO;

namespace серьёзный.Core.CoreDetectors;

public static class ExeMetadataReader
{
    public static string GetName(string exePath)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(exePath);

            if (!string.IsNullOrWhiteSpace(info.ProductName))
                return info.ProductName.Trim();

            if (!string.IsNullOrWhiteSpace(info.FileDescription))
                return info.FileDescription.Trim();

            return Path.GetFileNameWithoutExtension(exePath);
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(exePath);
        }
    }

    public static string GetCompany(string exePath)
    {
        try
        {
            return FileVersionInfo
                .GetVersionInfo(exePath)
                .CompanyName ?? "";
        }
        catch
        {
            return "";
        }
    }
}