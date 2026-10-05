using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
namespace FoxESSMonitor;
public static class ConfigStore {
 static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"FoxESS Monitor");
 static readonly string FilePath=Path.Combine(Folder,"settings.json");
 public static AppConfig Load(){try{return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FilePath))??new();}catch{return new();}}
 public static void Save(AppConfig c){Directory.CreateDirectory(Folder);File.WriteAllText(FilePath,JsonSerializer.Serialize(c,new JsonSerializerOptions{WriteIndented=true}));}
 public static string Unprotect(string text){if(string.IsNullOrEmpty(text))return "";try{return Encoding.UTF8.GetString(Transform(Convert.FromBase64String(text),false));}catch{return "";}}
 public static string Protect(string text)=>string.IsNullOrEmpty(text)?"":Convert.ToBase64String(Transform(Encoding.UTF8.GetBytes(text),true));
 static byte[] Transform(byte[] input,bool protect){var i=new Blob{cbData=input.Length,pbData=Marshal.AllocHGlobal(input.Length)};Marshal.Copy(input,0,i.pbData,input.Length);var d=new Blob();try{bool ok=protect?CryptProtectData(ref i,null,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,ref d):CryptUnprotectData(ref i,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,ref d);if(!ok)throw new System.ComponentModel.Win32Exception();var b=new byte[d.cbData];Marshal.Copy(d.pbData,b,0,b.Length);return b;}finally{if(i.pbData!=IntPtr.Zero)Marshal.FreeHGlobal(i.pbData);if(d.pbData!=IntPtr.Zero)LocalFree(d.pbData);}}
 [StructLayout(LayoutKind.Sequential)] struct Blob{public int cbData;public IntPtr pbData;}
 [DllImport("crypt32.dll",SetLastError=true,CharSet=CharSet.Unicode)]static extern bool CryptProtectData(ref Blob input,string? desc,IntPtr entropy,IntPtr reserved,IntPtr prompt,int flags,ref Blob output);
 [DllImport("crypt32.dll",SetLastError=true)]static extern bool CryptUnprotectData(ref Blob input,IntPtr desc,IntPtr entropy,IntPtr reserved,IntPtr prompt,int flags,ref Blob output);
 [DllImport("kernel32.dll")]static extern IntPtr LocalFree(IntPtr h);
}
