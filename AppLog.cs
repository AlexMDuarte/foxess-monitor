using System.IO;
namespace FoxESSMonitor;
public static class AppLog{
 static readonly object Gate=new();
 public static string LogPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"FoxESS Monitor","FoxESS Monitor.log");
 public static void Write(string level,string message){try{lock(Gate){Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);File.AppendAllText(LogPath,$"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} [{level}] {message}{Environment.NewLine}");}}catch{}}
}
