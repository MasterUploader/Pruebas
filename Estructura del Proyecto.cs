using System;
using System.Globalization;

public static class ConsultaResponseEnricher
{
    /// <summary>
    /// Completa MARKET_REF_* cuando vengan vacíos y aplica redondeo bancario:
    /// - MARKET_REF_CURRENCY_CD  <- ORIG_CURRENCY_CD
    /// - MARKET_REF_CURRENCY_FX  <- EXCH_RATE_FX  (5 decimales, bankers round)
    /// - MARKET_REF_CURRENCY_AM  <- ORIGIN_AM     (2 decimales, bankers round)
    /// Además, limita ExchangeRateFx a máx 10 caracteres para la salida JSON.
    /// </summary>
    public static void Apply(ConsultaResponseData resp)
    {
        if (resp?.Data is null) return;
        var d = resp.Data;

        // 1) MARKET_REF_CURRENCY_CD
        if (string.IsNullOrWhiteSpace(d.MarketRefCurrencyCd) && !string.IsNullOrWhiteSpace(d.OrigCurrencyCd))
            d.MarketRefCurrencyCd = d.OrigCurrencyCd.Trim();

        // 2) MARKET_REF_CURRENCY_FX  (5 decimales, bankers)
        if (string.IsNullOrWhiteSpace(d.MarketRefCurrencyFx) && TryParseDec(d.ExchangeRateFx, out var fx))
            d.MarketRefCurrencyFx = BankersRoundToFixed(fx, 5);

        // 3) MARKET_REF_CURRENCY_AM  (2 decimales, bankers)
        if (string.IsNullOrWhiteSpace(d.MarketRefCurrencyAm) && TryParseDec(d.OrigAmount, out var am))
            d.MarketRefCurrencyAm = BankersRoundToFixed(am, 2);

        // 4) ExchangeRateFx máx 10 chars (salida JSON)
        if (!string.IsNullOrEmpty(d.ExchangeRateFx) && d.ExchangeRateFx.Length > 10)
            d.ExchangeRateFx = d.ExchangeRateFx[..10];
    }

    private static bool TryParseDec(string? s, out decimal val)
    {
        s = (s ?? string.Empty).Trim();
        // Invariant, con fallback de coma a punto
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out val))
            return true;

        if (s.Contains(',') && !s.Contains('.'))
            return decimal.TryParse(s.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out val);

        val = default;
        return false;
    }

    private static string BankersRoundToFixed(decimal value, int decimals)
        => Math.Round(value, decimals, MidpointRounding.ToEven)
               .ToString("F" + Math.Max(decimals, 0), CultureInfo.InvariantCulture);
}



// ...
GetResponseEnvelope<GetResponseBody<ExecTRResponseConsulta>> result =
    (GetResponseEnvelope<GetResponseBody<ExecTRResponseConsulta>>)serializer.Deserialize(reader)!;

var dto = result.Body.ExectTRResponse.ExecTRResult.RESPONSE;

// 👇 Aplica el enriquecimiento para la salida JSON
ConsultaResponseEnricher.Apply(dto);

return (dto, statusCode);
