using System.IO;

namespace FoxESSMonitor;

public static class AppLog
{
    static readonly object Gate = new();

    public static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FoxESS Monitor",
        "FoxESS Monitor.log");

    public static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                var dir = Path.GetDirectoryName(LogPath)!;
                Directory.CreateDirectory(dir);

                // Basic log rotation if file exceeds 5 MB
                var fi = new FileInfo(LogPath);
                if (fi.Exists && fi.Length > 5 * 1024 * 1024)
                {
                    var backupPath = LogPath + ".old";
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                    File.Move(LogPath, backupPath);
                }

                File.AppendAllText(LogPath, $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch { }
    }
}
