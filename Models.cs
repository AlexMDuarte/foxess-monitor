namespace FoxESSMonitor;
public sealed class AppConfig { public string Endpoint {get;set;}="https://www.foxesscloud.com"; public string SerialNumber {get;set;}=""; public string ProtectedApiKey {get;set;}=""; public int RefreshMinutes {get;set;}=5; public bool AlwaysOnTop {get;set;}=true; public bool StartWithWindows {get;set;}=false; public bool DemoMode {get;set;}=true; }
public sealed class EnergySnapshot { public double? Pv, Pv1, Pv2, Load, Import, Export, Soc, TodayKwh, Temperature; public string Status="—", Updated="—", Notice=""; public bool IsDemo; }
