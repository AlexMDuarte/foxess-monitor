namespace FoxESSMonitor;

public static class EndpointNormalizer
{
    public static bool TryNormalize(string? value, out Uri endpoint, out string error)
    {
        var text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
            text = "https://www.foxesscloud.com";

        if (!text.Contains("://", StringComparison.Ordinal))
            text = "https://" + text;
        else if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            text = "https://" + text[7..];

        if (!Uri.TryCreate(text, UriKind.Absolute, out var entered) ||
            !entered.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(entered.Host) ||
            !string.IsNullOrEmpty(entered.UserInfo))
        {
            endpoint = new Uri("https://www.foxesscloud.com");
            error = "URL HTTPS de endpoint inválido.";
            return false;
        }

        // Developer portal domains are often confused with the actual API endpoint
        if (entered.Host.Equals("developer-eu.foxesscloud.com", StringComparison.OrdinalIgnoreCase) ||
            entered.Host.Equals("developer.foxesscloud.com", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = new Uri("https://www.foxesscloud.com");
            error = "";
            return true;
        }

        var builder = new UriBuilder(entered)
        {
            Path = "",
            Query = "",
            Fragment = ""
        };

        endpoint = builder.Uri;
        error = "";
        return true;
    }
}
