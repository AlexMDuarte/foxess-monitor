using System.Windows;

namespace FoxESSMonitor;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        AppLog.Write("INFO", "FoxESS Monitor started.");

        if (EndpointNormalizer.TryNormalize("www.foxesscloud.com/op/v1/device/detail?sn=example", out var endpoint, out var error))
        {
            AppLog.Write("INFO", $"Endpoint self-check passed: scheme={endpoint.Scheme}; host={endpoint.Host}; path stripped={endpoint.AbsolutePath == "/"}.");
        }
        else
        {
            AppLog.Write("ERROR", "Endpoint self-check failed: " + error);
        }
    }
}
