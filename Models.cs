namespace FoxESSMonitor;

public sealed class AppConfig
{
    public string Endpoint { get; set; } = "https://www.foxesscloud.com";
    public string SerialNumber { get; set; } = "";
    public string ProtectedApiKey { get; set; } = "";
    public int RefreshMinutes { get; set; } = 5;
    public bool AlwaysOnTop { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool DemoMode { get; set; } = true;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool RememberWindowPosition { get; set; } = true;
    public bool MinimizeToTrayOnClose { get; set; } = false;
}

public sealed class EnergySnapshot
{
    public double? Pv { get; set; }
    public double? Pv1 { get; set; }
    public double? Pv2 { get; set; }
    public double? Load { get; set; }
    public double? Import { get; set; }
    public double? Export { get; set; }
    public double? Soc { get; set; }
    public double? BatChargePower { get; set; }
    public double? BatDischargePower { get; set; }
    public double? BatPower { get; set; }
    public double? BatTemperature { get; set; }
    public double? ResidualEnergyKwh { get; set; }
    public double? TodayKwh { get; set; }
    public double? MonthKwh { get; set; }
    public double? Temperature { get; set; }
    public string Status { get; set; } = "—";
    public string Updated { get; set; } = "—";
    public string Notice { get; set; } = "";
    public bool IsDemo { get; set; }

    /// <summary>
    /// Percentage of household load currently covered by self-generated solar/battery energy (0 to 100%).
    /// </summary>
    public double? SelfSufficiencyRatio
    {
        get
        {
            if (Load is null || Load.Value <= 0.01) return Pv is not null && Pv.Value > 0 ? 100.0 : null;
            var netGridImport = Import.GetValueOrDefault();
            if (netGridImport <= 0.01) return 100.0;
            var covered = Math.Max(0, Load.Value - netGridImport);
            return Math.Clamp((covered / Load.Value) * 100.0, 0.0, 100.0);
        }
    }

    /// <summary>
    /// Human-readable battery activity text in Portuguese.
    /// </summary>
    public string BatteryActivityText
    {
        get
        {
            if (BatChargePower.HasValue && BatChargePower.Value > 0.02)
                return $"A carregar {BatChargePower.Value:0.00} kW";
            if (BatDischargePower.HasValue && BatDischargePower.Value > 0.02)
                return $"A descarregar {BatDischargePower.Value:0.00} kW";
            if (BatPower.HasValue)
            {
                if (BatPower.Value > 0.02) return $"A carregar {BatPower.Value:0.00} kW";
                if (BatPower.Value < -0.02) return $"A descarregar {Math.Abs(BatPower.Value):0.00} kW";
            }
            if (Soc.HasValue && Soc.Value >= 99)
                return "Bateria cheia";
            return "Em repouso";
        }
    }
}
