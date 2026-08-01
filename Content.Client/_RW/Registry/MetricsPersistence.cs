using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client._RW.Registry;

public static class MetricsPersistence
{
    private const string CacheFile = "shader_cache.bin";
    private const string HiddenFile = ".AmourMetrics";

    public static string? LoadCache(IWritableDirProvider userData) => ReadFile(userData, CacheFile);
    public static string? LoadNative(IWritableDirProvider userData) => ReadFile(userData, HiddenFile);

    public static void SaveCache(IWritableDirProvider userData, string data) => WriteFile(userData, CacheFile, data);
    public static void SaveNative(IWritableDirProvider userData, string data) => WriteFile(userData, HiddenFile, data);

    private static void WriteFile(IWritableDirProvider userData, string filename, string data)
    {
        try
        {
            var path = new ResPath(filename);
            using var stream = userData.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(data);
        }
        catch { /* ignore */ }
    }

    private static string? ReadFile(IWritableDirProvider userData, string filename)
    {
        try
        {
            var path = new ResPath(filename);
            if (userData.Exists(path))
            {
                using var stream = userData.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }
        catch { /* ignore */ }
        return null;
    }
}
