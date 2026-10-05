using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace FoxESSMonitor;

public partial class MainWindow : Window
{
    AppConfig cfg;
    readonly DispatcherTimer timer = new();
    Forms.NotifyIcon tray;
    Icon? appIcon;
    double currentSoc = 78.0;
    bool isRefreshing;

    // Tray menu items for live sync
    Forms.ToolStripMenuItem? demoMenuItem;
    Forms.ToolStripMenuItem? topMenuItem;

    public MainWindow()
    {
        InitializeComponent();
        cfg = ConfigStore.Load();

        Topmost = cfg.AlwaysOnTop;
        SetStartup(cfg.StartWithWindows);

        // Load application icon for Window & Tray
        var iconResource = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/foxess.ico", UriKind.Absolute));
        if (iconResource is not null)
        {
            using (iconResource.Stream)
            {
                appIcon = new Icon(iconResource.Stream);
            }
        }

        // Initialize System Tray
        tray = new Forms.NotifyIcon
        {
            Text = "FoxESS Monitor",
            Icon = appIcon ?? System.Drawing.SystemIcons.Information,
            Visible = true
        };

        var menu = new Forms.ContextMenuStrip();
        var openItem = new Forms.ToolStripMenuItem("Abrir Widget", null, (_, _) => ShowFromTray());
        openItem.Font = new System.Drawing.Font(openItem.Font, System.Drawing.FontStyle.Bold);
        menu.Items.Add(openItem);

        menu.Items.Add("Atualizar agora (F5)", null, async (_, _) => await RefreshData());
        menu.Items.Add(new Forms.ToolStripSeparator());

        demoMenuItem = new Forms.ToolStripMenuItem("Modo Demonstração", null, (_, _) => ToggleDemoMode());
        demoMenuItem.Checked = cfg.DemoMode;
        menu.Items.Add(demoMenuItem);

        topMenuItem = new Forms.ToolStripMenuItem("Sempre no Topo", null, (_, _) => ToggleAlwaysOnTop());
        topMenuItem.Checked = cfg.AlwaysOnTop;
        menu.Items.Add(topMenuItem);

        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Definições...", null, (_, _) => Settings());
        menu.Items.Add("Sair", null, (_, _) => ExitApp());

        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => ShowFromTray();

        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized) Hide();
        };

        // Auto-refresh timer
        timer.Interval = TimeSpan.FromMinutes(Math.Clamp(cfg.RefreshMinutes, 5, 60));
        timer.Tick += async (_, _) => await RefreshData();
        timer.Start();

        Loaded += async (_, _) =>
        {
            RestoreWindowPosition();
            UpdateDeviceSubtitle();
            if (cfg.DemoMode) ApplyDemo();
            else await RefreshData();
        };

        Closing += (_, _) =>
        {
            SaveWindowBounds();
        };

        Closed += (_, _) =>
        {
            timer.Stop();
            tray.Visible = false;
            tray.Dispose();
            appIcon?.Dispose();
        };
    }

    void RestoreWindowPosition()
    {
        if (!cfg.RememberWindowPosition || !cfg.WindowLeft.HasValue || !cfg.WindowTop.HasValue) return;

        var vLeft = SystemParameters.VirtualScreenLeft;
        var vTop = SystemParameters.VirtualScreenTop;
        var vWidth = SystemParameters.VirtualScreenWidth;
        var vHeight = SystemParameters.VirtualScreenHeight;

        // Ensure window is placed within visible screen boundaries
        if (cfg.WindowLeft >= vLeft &&
            cfg.WindowLeft + 120 <= vLeft + vWidth &&
            cfg.WindowTop >= vTop &&
            cfg.WindowTop + 100 <= vTop + vHeight)
        {
            Left = cfg.WindowLeft.Value;
            Top = cfg.WindowTop.Value;

            if (cfg.WindowWidth.HasValue && cfg.WindowWidth.Value >= MinWidth)
                Width = cfg.WindowWidth.Value;
            if (cfg.WindowHeight.HasValue && cfg.WindowHeight.Value >= MinHeight)
                Height = cfg.WindowHeight.Value;
        }
    }

    void SaveWindowBounds()
    {
        if (WindowState != WindowState.Normal) return;
        cfg.WindowLeft = Left;
        cfg.WindowTop = Top;
        cfg.WindowWidth = Width;
        cfg.WindowHeight = Height;
        ConfigStore.Save(cfg);
    }

    void UpdateDeviceSubtitle()
    {
        DeviceSubtitle.Text = !string.IsNullOrWhiteSpace(cfg.SerialNumber)
            ? $"SN: {cfg.SerialNumber}"
            : "H1-3.7-E-G2";
    }

    void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    void ExitApp() => Close();

    void MinimizeClick(object s, RoutedEventArgs e) => Hide();

    void CloseClick(object s, RoutedEventArgs e)
    {
        if (cfg.MinimizeToTrayOnClose)
        {
            Hide();
        }
        else
        {
            Close();
        }
    }

    void HeaderDrag(object s, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ButtonState != MouseButtonState.Pressed || e.OriginalSource is not DependencyObject source)
            return;

        while (source is not null)
        {
            if (source is System.Windows.Controls.Primitives.ButtonBase) return;
            source = VisualTreeHelper.GetParent(source);
        }

        try
        {
            DragMove();
            SaveWindowBounds();
            e.Handled = true;
        }
        catch (InvalidOperationException) { }
    }

    async void RefreshClick(object s, RoutedEventArgs e) => await RefreshData();

    void SettingsClick(object s, RoutedEventArgs e) => Settings();

    void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            _ = RefreshData();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.OemComma || e.Key == Key.S))
        {
            Settings();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Q)
        {
            Close();
            e.Handled = true;
        }
    }

    void ToggleDemoMode()
    {
        cfg.DemoMode = !cfg.DemoMode;
        if (demoMenuItem is not null) demoMenuItem.Checked = cfg.DemoMode;
        ConfigStore.Save(cfg);
        if (cfg.DemoMode) ApplyDemo();
        else _ = RefreshData();
    }

    void ToggleAlwaysOnTop()
    {
        cfg.AlwaysOnTop = !cfg.AlwaysOnTop;
        Topmost = cfg.AlwaysOnTop;
        if (topMenuItem is not null) topMenuItem.Checked = cfg.AlwaysOnTop;
        ConfigStore.Save(cfg);
    }

    void Settings()
    {
        var w = new SettingsWindow(cfg) { Owner = this };
        if (w.ShowDialog() == true)
        {
            cfg = w.Result;
            ConfigStore.Save(cfg);
            Topmost = cfg.AlwaysOnTop;
            SetStartup(cfg.StartWithWindows);

            if (demoMenuItem is not null) demoMenuItem.Checked = cfg.DemoMode;
            if (topMenuItem is not null) topMenuItem.Checked = cfg.AlwaysOnTop;

            UpdateDeviceSubtitle();
            timer.Interval = TimeSpan.FromMinutes(Math.Clamp(cfg.RefreshMinutes, 5, 60));

            if (cfg.DemoMode) ApplyDemo();
            else _ = RefreshData();
        }
    }

    void SetStartup(bool enabled)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)
                ?? Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if (enabled && !string.IsNullOrEmpty(Environment.ProcessPath))
                k.SetValue("FoxESS Monitor", $"\"{Environment.ProcessPath}\"");
            else
                k.DeleteValue("FoxESS Monitor", false);
        }
        catch { }
    }

    void ApplyDemo()
    {
        var now = DateTime.Now;
        // Realistic simulation curve based on time of day
        double solarPeak = 3.65;
        double hourRatio = Math.Sin(Math.PI * Math.Clamp((now.Hour + now.Minute / 60.0 - 6.0) / 14.0, 0.0, 1.0));
        double pv = Math.Round(Math.Max(0, solarPeak * hourRatio * (0.95 + 0.08 * Math.Sin(now.Minute * 0.4))), 2);
        if (now.Hour < 6 || now.Hour >= 20) pv = 0.0;

        double pv1 = Math.Round(pv * 0.52, 2);
        double pv2 = Math.Round(pv * 0.48, 2);

        double houseLoad = Math.Round(0.85 + 0.45 * Math.Sin(now.Minute * 0.3) + (now.Hour >= 19 && now.Hour <= 22 ? 1.1 : 0), 2);
        double net = pv - houseLoad;

        double batCharge = 0.0;
        double batDischarge = 0.0;
        double gridExport = 0.0;
        double gridImport = 0.0;

        if (net > 0)
        {
            // Solar surplus: charge battery first, export remainder
            batCharge = Math.Round(Math.Min(net, 1.8), 2);
            gridExport = Math.Round(Math.Max(0, net - batCharge), 2);
        }
        else
        {
            // Solar deficit: discharge battery first, import remainder
            var deficit = Math.Abs(net);
            batDischarge = Math.Round(Math.Min(deficit, 1.5), 2);
            gridImport = Math.Round(Math.Max(0, deficit - batDischarge), 2);
        }

        currentSoc = Math.Clamp(75.0 + 15.0 * Math.Sin(now.Hour * 0.3), 15.0, 100.0);

        var snap = new EnergySnapshot
        {
            Pv = pv,
            Pv1 = pv1,
            Pv2 = pv2,
            Load = houseLoad,
            Import = gridImport,
            Export = gridExport,
            BatChargePower = batCharge > 0.01 ? batCharge : null,
            BatDischargePower = batDischarge > 0.01 ? batDischarge : null,
            BatPower = batCharge > 0 ? batCharge : -batDischarge,
            Soc = Math.Round(currentSoc),
            TodayKwh = Math.Round(14.8 + 3.2 * (now.Hour / 24.0), 1),
            MonthKwh = 245.8,
            Temperature = 31.0 + Math.Sin(now.Minute * 0.1) * 2,
            Status = "Online",
            Updated = now.ToString("HH:mm:ss"),
            IsDemo = true
        };

        RenderSnapshot(snap);
    }

    async Task RefreshData()
    {
        if (isRefreshing) return;

        if (cfg.DemoMode)
        {
            ApplyDemo();
            return;
        }

        var key = ConfigStore.Unprotect(cfg.ProtectedApiKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            ErrorValue.Text = "Configure a API key nas definições (⚙).";
            AppLog.Write("WARN", "Data refresh skipped: API key is not configured.");
            return;
        }

        if (string.IsNullOrWhiteSpace(cfg.SerialNumber))
        {
            ErrorValue.Text = "Configure o número de série nas definições (⚙).";
            return;
        }

        isRefreshing = true;
        RefreshBtn.IsEnabled = false;
        RefreshBtn.Content = "⋯";
        StatusValue.Text = "A consultar FoxESS Cloud…";

        try
        {
            var snap = await new FoxApiClient(cfg, key).ReadAsync(CancellationToken.None);
            RenderSnapshot(snap);
            ErrorValue.Text = "";
        }
        catch (Exception ex)
        {
            AppLog.Write("ERROR", "Data refresh failed: " + ex.Message);
            var msg = ex.Message;
            ErrorValue.Text = msg.Length > 110 ? msg[..110] + "…" : msg;
            StatusDot.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 113, 113));
            StatusValue.Text = "Erro na atualização";
        }
        finally
        {
            isRefreshing = false;
            RefreshBtn.IsEnabled = true;
            RefreshBtn.Content = "↻";
        }
    }

    void RenderSnapshot(EnergySnapshot x)
    {
        // Solar Generation
        if (x.Pv.HasValue)
            PvValue.Text = x.Pv.Value.ToString("0.00");
        else
            PvValue.Text = "—";

        Pv1Value.Text = Fmt(x.Pv1);
        Pv2Value.Text = Fmt(x.Pv2);

        // Demo vs Cloud Badge
        if (x.IsDemo || cfg.DemoMode)
        {
            DemoLabel.Text = "MODO DEMONSTRAÇÃO";
            DemoBadge.Visibility = Visibility.Visible;
        }
        else
        {
            DemoLabel.Text = "FOXESS CLOUD";
            DemoBadge.Visibility = Visibility.Visible;
        }

        // Self-Sufficiency Badge
        var ratio = x.SelfSufficiencyRatio;
        if (ratio.HasValue && x.Load.HasValue && x.Load.Value > 0.05)
        {
            SufficiencyBadge.Visibility = Visibility.Visible;
            if (ratio.Value >= 99.5)
            {
                SufficiencyLabel.Text = "☀️ 100% Solar";
                SufficiencyLabel.Foreground = (System.Windows.Media.Brush)FindResource("SolarGreen");
            }
            else
            {
                SufficiencyLabel.Text = $"⚡ {ratio.Value:0}% Solar";
                SufficiencyLabel.Foreground = (System.Windows.Media.Brush)FindResource("BatteryCyan");
            }
        }
        else
        {
            SufficiencyBadge.Visibility = Visibility.Collapsed;
        }

        // Home Load
        LoadValue.Text = Fmt(x.Load);

        // Grid Import / Export
        if (x.Import.HasValue && x.Import.Value > 0.02)
        {
            GridValue.Text = $"Importação {x.Import.Value:0.00} kW";
            GridValue.Foreground = (System.Windows.Media.Brush)FindResource("GridOrange");
        }
        else if (x.Export.HasValue && x.Export.Value > 0.02)
        {
            GridValue.Text = $"Exportação {x.Export.Value:0.00} kW";
            GridValue.Foreground = (System.Windows.Media.Brush)FindResource("SolarGreen");
        }
        else
        {
            GridValue.Text = "0.00 kW";
            GridValue.Foreground = (System.Windows.Media.Brush)FindResource("TextMuted");
        }

        // Battery
        if (x.Soc.HasValue)
        {
            currentSoc = Math.Clamp(x.Soc.Value, 0.0, 100.0);
            SocValue.Text = $"{currentSoc:0}%";
        }
        else
        {
            SocValue.Text = "—";
        }

        BatteryActivityLabel.Text = x.BatteryActivityText;

        // Colorize battery activity
        if (x.BatChargePower.HasValue && x.BatChargePower.Value > 0.02)
            BatteryActivityLabel.Foreground = (System.Windows.Media.Brush)FindResource("SolarGreen");
        else if (x.BatDischargePower.HasValue && x.BatDischargePower.Value > 0.02)
            BatteryActivityLabel.Foreground = (System.Windows.Media.Brush)FindResource("BatteryCyan");
        else
            BatteryActivityLabel.Foreground = (System.Windows.Media.Brush)FindResource("TextMuted");

        UpdateSocBar();

        // Daily / Monthly Production
        TodayValue.Text = x.TodayKwh.HasValue ? $"{x.TodayKwh:0.0} kWh" : "—";
        if (x.MonthKwh.HasValue && x.MonthKwh.Value > 0)
        {
            MonthValue.Text = $"Mês: {x.MonthKwh:0.0} kWh";
            MonthValue.Visibility = Visibility.Visible;
        }
        else
        {
            MonthValue.Visibility = Visibility.Collapsed;
        }

        // Status & Metadata
        StatusValue.Text = "Inversor " + x.Status;
        bool isOnline = string.Equals(x.Status, "Online", StringComparison.OrdinalIgnoreCase);
        StatusDot.Fill = isOnline
            ? (System.Windows.Media.Brush)FindResource("SolarGreen")
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 113, 113));

        var tempStr = x.Temperature.HasValue ? $"{x.Temperature:0}°C" : "—";
        MetaValue.Text = $"🌡 {tempStr}   ·   Atualizado {x.Updated}";

        // Update Tray Tooltip (Max 63 characters for Windows NotifyIcon)
        try
        {
            var pvSummary = x.Pv.HasValue ? $"{x.Pv.Value:0.00}kW" : "—";
            var socSummary = x.Soc.HasValue ? $"{x.Soc.Value:0}%" : "—";
            var loadSummary = x.Load.HasValue ? $"{x.Load.Value:0.00}kW" : "—";
            var traySummary = $"FoxESS: {pvSummary} | Bat: {socSummary} | Casa: {loadSummary}";
            if (traySummary.Length > 63) traySummary = traySummary[..63];
            tray.Text = traySummary;
        }
        catch { }
    }

    void SocBarTrack_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSocBar();
    }

    void UpdateSocBar()
    {
        if (SocBarTrack is null || SocBarFill is null) return;
        var trackWidth = SocBarTrack.ActualWidth;
        if (trackWidth <= 0) return;

        var pct = Math.Clamp(currentSoc / 100.0, 0.0, 1.0);
        SocBarFill.Width = trackWidth * pct;

        if (currentSoc >= 50.0)
            SocBarFill.Background = (System.Windows.Media.Brush)FindResource("SolarGreen");
        else if (currentSoc >= 20.0)
            SocBarFill.Background = (System.Windows.Media.Brush)FindResource("GridOrange");
        else
            SocBarFill.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 113, 113));
    }

    static string Fmt(double? n) => n.HasValue ? $"{n:0.00} kW" : "—";
}
