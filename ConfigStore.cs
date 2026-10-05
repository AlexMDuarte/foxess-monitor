using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FoxESSMonitor;

public static class ConfigStore
{
    static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FoxESS Monitor");

    static readonly string FilePath = Path.Combine(Folder, "settings.json");

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void Save(AppConfig c)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var json = JsonSerializer.Serialize(c, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            AppLog.Write("ERROR", "Failed to save configuration: " + ex.Message);
        }
    }

    public static string Unprotect(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        try
        {
            return Encoding.UTF8.GetString(Transform(Convert.FromBase64String(text), false));
        }
        catch
        {
            return "";
        }
    }

    public static string Protect(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        try
        {
            return Convert.ToBase64String(Transform(Encoding.UTF8.GetBytes(text), true));
        }
        catch
        {
            return "";
        }
    }

    static byte[] Transform(byte[] input, bool protect)
    {
        var inBlob = new Blob { cbData = input.Length, pbData = Marshal.AllocHGlobal(input.Length) };
        Marshal.Copy(input, 0, inBlob.pbData, input.Length);
        var outBlob = new Blob();

        try
        {
            bool ok = protect
                ? CryptProtectData(ref inBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, ref outBlob)
                : CryptUnprotectData(ref inBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, ref outBlob);

            if (!ok) throw new System.ComponentModel.Win32Exception();

            var result = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, result, 0, result.Length);
            return result;
        }
        finally
        {
            if (inBlob.pbData != IntPtr.Zero) Marshal.FreeHGlobal(inBlob.pbData);
            if (outBlob.pbData != IntPtr.Zero) LocalFree(outBlob.pbData);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Blob
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CryptProtectData(ref Blob input, string? desc, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, ref Blob output);

    [DllImport("crypt32.dll", SetLastError = true)]
    static extern bool CryptUnprotectData(ref Blob input, IntPtr desc, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, ref Blob output);

    [DllImport("kernel32.dll")]
    static extern IntPtr LocalFree(IntPtr h);
}
