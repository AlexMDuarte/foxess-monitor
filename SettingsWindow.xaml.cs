using System.Windows;

namespace FoxESSMonitor;

public partial class SettingsWindow : Window
{
    readonly AppConfig original;
    bool isKeyVisible;

    public AppConfig Result { get; private set; } = new();

    public SettingsWindow(AppConfig c)
    {
        InitializeComponent();
        original = c;

        EndpointBox.Text = c.Endpoint;
        SerialBox.Text = c.SerialNumber;
        IntervalBox.Text = c.RefreshMinutes.ToString();
        DemoBox.IsChecked = c.DemoMode;
        TopBox.IsChecked = c.AlwaysOnTop;
        StartupBox.IsChecked = c.StartWithWindows;
        PosBox.IsChecked = c.RememberWindowPosition;
        CloseToTrayBox.IsChecked = c.MinimizeToTrayOnClose;

        if (!string.IsNullOrWhiteSpace(c.ProtectedApiKey))
        {
            KeyStatusLabel.Text = "✓ Chave configurada e protegida com DPAPI (deixe em branco para manter a atual).";
            KeyStatusLabel.Foreground = (System.Windows.Media.Brush)FindResource("AccentGreen");
        }
        else
        {
            KeyStatusLabel.Text = "Nenhuma chave configurada atualmente.";
        }
    }

    void ToggleKeyVisibility_Click(object sender, RoutedEventArgs e)
    {
        isKeyVisible = !isKeyVisible;
        if (isKeyVisible)
        {
            KeyVisibleBox.Text = KeyBox.Password;
            KeyVisibleBox.Visibility = Visibility.Visible;
            KeyBox.Visibility = Visibility.Collapsed;
            ToggleKeyBtn.Content = "🙈";
        }
        else
        {
            KeyBox.Password = KeyVisibleBox.Text;
            KeyBox.Visibility = Visibility.Visible;
            KeyVisibleBox.Visibility = Visibility.Collapsed;
            ToggleKeyBtn.Content = "👁";
        }
    }

    string GetEnteredKey()
    {
        var raw = isKeyVisible ? KeyVisibleBox.Text : KeyBox.Password;
        return (raw ?? "").Trim();
    }

    bool TryReadEndpoint(out Uri uri)
    {
        if (!EndpointNormalizer.TryNormalize(EndpointBox.Text, out uri, out var error))
        {
            AppLog.Write("ERROR", "Endpoint validation rejected input: " + error);
            System.Windows.MessageBox.Show(
                this,
                "Não foi possível validar o endereço introduzido como um URL HTTPS válido.\nExemplo correto: https://www.foxesscloud.com",
                "Endpoint FoxESS Inválido",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        EndpointBox.Text = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }

    async void TestClick(object s, RoutedEventArgs e)
    {
        if (!TryReadEndpoint(out var endpoint)) return;

        var key = GetEnteredKey();
        if (string.IsNullOrWhiteSpace(key))
            key = ConfigStore.Unprotect(original.ProtectedApiKey);

        if (string.IsNullOrWhiteSpace(key))
        {
            AppLog.Write("WARN", "Connection test aborted: API key is empty.");
            System.Windows.MessageBox.Show(
                this,
                "Introduza a sua Chave API FoxESS para testar a ligação.",
                "Chave API Necessária",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var serial = (SerialBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(serial))
        {
            AppLog.Write("WARN", "Connection test aborted: Serial number is empty.");
            System.Windows.MessageBox.Show(
                this,
                "Introduza o Número de Série do Inversor para testar a ligação.",
                "Número de Série Necessário",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var button = TestBtn;
        button.IsEnabled = false;
        button.Content = "A testar…";

        try
        {
            var testConfig = new AppConfig
            {
                Endpoint = endpoint.GetLeftPart(UriPartial.Authority),
                SerialNumber = serial
            };

            AppLog.Write("INFO", $"Connection test started for host {endpoint.Host} (SN: {serial}).");
            await new FoxApiClient(testConfig, key).CheckConnectionAsync(CancellationToken.None);
            AppLog.Write("INFO", "Connection test succeeded.");

            System.Windows.MessageBox.Show(
                this,
                "Ligação com a FoxESS estabelecida com sucesso!\nA API aceitou o endpoint, a chave e o número de série.",
                "Ligação FoxESS OK",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLog.Write("ERROR", "Connection test failed: " + ex.Message);
            System.Windows.MessageBox.Show(
                this,
                ex.Message,
                "Falha na Ligação FoxESS",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            button.IsEnabled = true;
            button.Content = "Testar ligação";
        }
    }

    void SaveClick(object s, RoutedEventArgs e)
    {
        if (!TryReadEndpoint(out var u)) return;

        if (!int.TryParse(IntervalBox.Text, out var min) || min < 5 || min > 60)
        {
            System.Windows.MessageBox.Show(
                this,
                "O intervalo de atualização deve ser um número inteiro entre 5 e 60 minutos para respeitar a política de pedidos da FoxESS Cloud.",
                "Intervalo Inválido",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var serial = (SerialBox.Text ?? "").Trim();
        if (DemoBox.IsChecked != true && string.IsNullOrWhiteSpace(serial))
        {
            System.Windows.MessageBox.Show(
                this,
                "Introduza o número de série do inversor (ou ative o Modo Demonstração).",
                "Número de Série Obrigatório",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var enteredKey = GetEnteredKey();
        var keyToStore = string.IsNullOrWhiteSpace(enteredKey)
            ? original.ProtectedApiKey
            : ConfigStore.Protect(enteredKey);

        Result = new AppConfig
        {
            Endpoint = u.GetLeftPart(UriPartial.Authority),
            SerialNumber = serial,
            ProtectedApiKey = keyToStore,
            RefreshMinutes = min,
            AlwaysOnTop = TopBox.IsChecked == true,
            StartWithWindows = StartupBox.IsChecked == true,
            DemoMode = DemoBox.IsChecked == true,
            RememberWindowPosition = PosBox.IsChecked == true,
            MinimizeToTrayOnClose = CloseToTrayBox.IsChecked == true,
            WindowLeft = original.WindowLeft,
            WindowTop = original.WindowTop,
            WindowWidth = original.WindowWidth,
            WindowHeight = original.WindowHeight
        };

        DialogResult = true;
    }
}
