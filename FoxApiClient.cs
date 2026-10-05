using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FoxESSMonitor;

public sealed class FoxApiClient(AppConfig config, string apiKey)
{
    static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public async Task CheckConnectionAsync(CancellationToken ct)
    {
        var root = config.Endpoint.TrimEnd('/');
        _ = await Get(root, "/op/v1/device/detail?sn=" + Uri.EscapeDataString(config.SerialNumber), ct);
    }

    public async Task<EnergySnapshot> ReadAsync(CancellationToken ct)
    {
        var root = config.Endpoint.TrimEnd('/');
        var snap = new EnergySnapshot();

        // 1. Fetch real-time metrics
        try
        {
            var rt = await Post(root, "/op/v1/device/real/query", new { sns = new[] { config.SerialNumber } }, ct);
            var result = rt.RootElement.GetProperty("result");
            var item = result.ValueKind == JsonValueKind.Array ? result.EnumerateArray().FirstOrDefault() : result;
            var d = item.ValueKind == JsonValueKind.Object && item.TryGetProperty("datas", out var ds) ? ds : default;

            if (d.ValueKind == JsonValueKind.Array)
            {
                var map = d.EnumerateArray()
                    .Where(x => x.TryGetProperty("variable", out _))
                    .ToDictionary(
                        x => x.GetProperty("variable").GetString() ?? "",
                        x => Number(x, "value"),
                        StringComparer.OrdinalIgnoreCase);

                // Solar generation
                snap.Pv = First(map, "pvPower", "generationPower", "ppv");
                snap.Pv1 = First(map, "pv1Power", "ppv1");
                snap.Pv2 = First(map, "pv2Power", "ppv2");

                // Grid interaction
                snap.Import = First(map, "gridConsumptionPower", "gridPower");
                snap.Export = First(map, "feedinPower");

                // Battery metrics (crucial for hybrid inverters like H1 series)
                var batCharge = First(map, "batChargePower", "chargePower");
                var batDischarge = First(map, "batDischargePower", "dischargePower");
                var batPower = First(map, "batPower");

                if (batPower.HasValue && !batCharge.HasValue && !batDischarge.HasValue)
                {
                    if (batPower.Value > 0) batCharge = batPower.Value;
                    else if (batPower.Value < 0) batDischarge = Math.Abs(batPower.Value);
                }

                snap.BatChargePower = batCharge;
                snap.BatDischargePower = batDischarge;
                snap.BatPower = batPower ?? (batCharge.GetValueOrDefault() - batDischarge.GetValueOrDefault());

                snap.Soc = First(map, "SoC", "soc", "batSoc");
                snap.BatTemperature = First(map, "batTemperature", "bmsTemperature");
                snap.ResidualEnergyKwh = First(map, "residualEnergy", "batResidualCapacity");

                // Home consumption
                snap.Load = First(map, "loadsPower", "loadPower", "totalLoadsPower", "epsPower");

                // Inverter temperature
                snap.Temperature = First(map, "invTemp", "ambientTemp", "temperature", "ambientTemperature");

                // Timestamp
                var t = d.EnumerateArray()
                    .Select(x => x.TryGetProperty("time", out var v) ? v.GetString() : null)
                    .FirstOrDefault(x => !string.IsNullOrEmpty(x));
                snap.Updated = t ?? DateTime.Now.ToString("g");
            }
        }
        catch (Exception ex)
        {
            AppLog.Write("ERROR", "Failed to query real-time data: " + ex.Message);
            throw;
        }

        // 2. Fetch generation totals (today and month)
        try
        {
            var g = await Get(root, "/op/v0/device/generation?sn=" + Uri.EscapeDataString(config.SerialNumber), ct);
            if (g.RootElement.TryGetProperty("result", out var gr))
            {
                if (gr.TryGetProperty("today", out var today)) snap.TodayKwh = Num(today);
                if (gr.TryGetProperty("month", out var month)) snap.MonthKwh = Num(month);
            }
        }
        catch (Exception ex)
        {
            AppLog.Write("WARN", "Optional generation query failed: " + ex.Message);
        }

        // 3. Fetch device status and model
        try
        {
            var de = await Get(root, "/op/v1/device/detail?sn=" + Uri.EscapeDataString(config.SerialNumber), ct);
            if (de.RootElement.TryGetProperty("result", out var dr) && dr.TryGetProperty("status", out var st))
            {
                snap.Status = st.GetInt32() switch
                {
                    1 => "Online",
                    2 => "Falha",
                    3 => "Offline",
                    _ => st.ToString()
                };
            }
        }
        catch
        {
            snap.Status ??= "Online";
        }

        // 4. Physical calculation for Home Load if missing:
        // Load = PV + Grid Import - Grid Export + Battery Discharge - Battery Charge
        if (snap.Load is null && snap.Pv is not null)
        {
            var pv = snap.Pv.GetValueOrDefault();
            var imp = snap.Import.GetValueOrDefault();
            var exp = snap.Export.GetValueOrDefault();
            var dis = snap.BatDischargePower.GetValueOrDefault();
            var chg = snap.BatChargePower.GetValueOrDefault();
            var estLoad = Math.Max(0, pv + imp - exp + dis - chg);
            snap.Load = Math.Round(estLoad, 2);
        }

        return snap;
    }

    async Task<JsonDocument> Post(string root, string path, object body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, root + path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        Sign(req, path);
        return await Send(req, ct);
    }

    async Task<JsonDocument> Get(string root, string path, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, root + path)
        {
            Content = new ByteArrayContent(Array.Empty<byte>())
        };
        req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var pathOnly = path.Split('?')[0];
        Sign(req, pathOnly);
        return await Send(req, ct);
    }

    void Sign(HttpRequestMessage req, string path)
    {
        var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var raw = $"{path}\r\n{apiKey}\r\n{stamp}";
        var sig = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

        req.Headers.TryAddWithoutValidation("token", apiKey);
        req.Headers.TryAddWithoutValidation("timestamp", stamp);
        req.Headers.TryAddWithoutValidation("signature", sig);
        req.Headers.TryAddWithoutValidation("lang", "en");
        req.Headers.TryAddWithoutValidation("User-Agent", "FoxESS-Monitor/1.2 (Windows)");
    }

    static async Task<JsonDocument> Send(HttpRequestMessage req, CancellationToken ct)
    {
        HttpResponseMessage res;
        try
        {
            res = await Http.SendAsync(req, ct);
        }
        catch (TaskCanceledException)
        {
            throw new Exception("Tempo limite esgotado ao contactar o servidor FoxESS.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("Falha de rede ao contactar FoxESS: " + ex.Message);
        }

        using (res)
        {
            if (res.StatusCode == HttpStatusCode.TooManyRequests)
                throw new Exception("FoxESS: limite de pedidos atingido. Aguarde antes de tentar novamente.");

            if (!res.IsSuccessStatusCode)
            {
                var path = req.RequestUri?.AbsolutePath ?? "/";
                var status = (int)res.StatusCode;
                if (status == 404)
                    throw new Exception($"Erro de endpoint (HTTP 404 em {path}). O endpoint base deve ser https://www.foxesscloud.com");
                if (status is 401 or 403)
                    throw new Exception($"FoxESS recusou o acesso (HTTP {status}). Confirme a API key e as permissões.");
                throw new Exception($"FoxESS respondeu com erro HTTP {status} em {path}.");
            }

            var content = await res.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("errno", out var e) && e.GetInt32() != 0)
            {
                var code = e.GetInt32();
                var msg = doc.RootElement.TryGetProperty("msg", out var m) ? m.GetString() : null;
                throw new Exception(TranslateFoxError(code, msg));
            }

            return doc;
        }
    }

    static string TranslateFoxError(int code, string? defaultMsg) => code switch
    {
        0 => "Operação bem sucedida.",
        40256 => "Inversor não encontrado. Verifique se o número de série está correto.",
        40257 => "Inversor encontra-se offline na FoxESS Cloud.",
        41807 => "Chave API FoxESS inválida. Verifique a chave nas configurações.",
        41808 => "Chave API FoxESS expirada no portal de programadores.",
        41809 => "Limite de frequência de pedidos excedido (Rate limit FoxESS). Aguarde 5 minutos.",
        41810 => "Falha na verificação da assinatura do pedido à FoxESS.",
        _ => !string.IsNullOrWhiteSpace(defaultMsg) ? $"FoxESS API: {defaultMsg}" : $"Erro FoxESS API {code}"
    };

    static double? Number(JsonElement o, string name)
    {
        if (!o.TryGetProperty(name, out var v)) return null;
        return Num(v);
    }

    static double? Num(JsonElement v)
    {
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
        if (double.TryParse(v.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) return d;
        return null;
    }

    static double? Get(Dictionary<string, double?> m, string k) => m.TryGetValue(k, out var v) ? v : null;

    static double? First(Dictionary<string, double?> m, params string[] keys) =>
        keys.Select(k => Get(m, k)).FirstOrDefault(x => x.HasValue);
}
