using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>Envelope que devuelve el tercero TNP (nombres tal cual vienen).</summary>
public sealed class TnpAuthorizationEnvelope
{
    [JsonPropertyName("GetAuthorizationManualResponse")]
    public TnpAuthorizationResponse? GetAuthorizationManualResponse { get; set; }
}

public sealed class TnpAuthorizationResponse
{
    [JsonPropertyName("GetAuthorizationManualResult")]
    public TnpAuthorizationResult? GetAuthorizationManualResult { get; set; }
}

/// <summary>Resultado del tercero (PascalCase).</summary>
public sealed class TnpAuthorizationResult
{
    [JsonPropertyName("ResponseCodeDescription")] public string? ResponseCodeDescription { get; set; }
    [JsonPropertyName("ResponseCode")]           public string? ResponseCode { get; set; }
    [JsonPropertyName("AuthorizationCode")]      public string? AuthorizationCode { get; set; }
    [JsonPropertyName("RetrievalReferenceNumber")] public string? RetrievalReferenceNumber { get; set; }
    [JsonPropertyName("SystemsTraceAuditNumber")]  public string? SystemsTraceAuditNumber { get; set; }
    [JsonPropertyName("TransactionType")]          public string? TransactionType { get; set; }
    [JsonPropertyName("TimeLocalTrans")]           public string? TimeLocalTrans { get; set; }
    [JsonPropertyName("DateLocalTrans")]           public string? DateLocalTrans { get; set; }
    [JsonPropertyName("Amount")]                   public string? Amount { get; set; }
    [JsonPropertyName("MerchantID")]               public string? MerchantID { get; set; }
    [JsonPropertyName("MCC")]                      public string? MCC { get; set; }
    [JsonPropertyName("CurrencyCode")]             public string? CurrencyCode { get; set; }
    [JsonPropertyName("PrimaryAccountNumber")]     public string? PrimaryAccountNumber { get; set; }
    [JsonPropertyName("TerminalID")]               public string? TerminalID { get; set; }
}


using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>Forma de error que devuelve el tercero cuando HTTP ≠ 2xx.</summary>
public sealed class TnpErrorResponse
{
    [JsonPropertyName("error")]     public string? Error { get; set; }
    [JsonPropertyName("status")]    public int? Status { get; set; }
    [JsonPropertyName("timestamp")] public long? Timestamp { get; set; } // epoch ms
}using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>Adaptadores de la respuesta del tercero a tu contrato interno.</summary>
public static class TnpAuthorizationMapper
{
    /// <summary>
    /// Éxito HTTP 200: mapea el resultado TNP al DTO interno.
    /// </summary>
    public static ResponseAuthorizationManualDto FromSuccess(TnpAuthorizationResult src)
    {
        // Preferimos el RRN; si no viene, caemos al STAN (número de auditoría).
        var txnId = !string.IsNullOrWhiteSpace(src.RetrievalReferenceNumber)
            ? src.RetrievalReferenceNumber!
            : (src.SystemsTraceAuditNumber ?? string.Empty);

        return new ResponseAuthorizationManualDto
        {
            ResponseCode      = src.ResponseCode ?? string.Empty,
            AuthorizationCode = src.AuthorizationCode ?? string.Empty,
            TransactionId     = txnId,
            Message           = src.ResponseCodeDescription ?? string.Empty,
            Timestamp         = TimeUtil.IsoNowUtc()
        };
    }

    /// <summary>
    /// Error HTTP ≠ 2xx: mapea el error TNP a tu DTO interno.
    /// </summary>
    public static ResponseAuthorizationManualDto FromError(TnpErrorResponse err, string fallbackResponseCode)
        => new()
        {
            ResponseCode      = fallbackResponseCode,              // mapeado desde el HTTP
            AuthorizationCode = string.Empty,
            TransactionId     = string.Empty,
            Message           = err.Error ?? "Error no especificado por TNP.",
            Timestamp         = TimeUtil.IsoNowUtc()
        };
}





// 7) No-2xx → intenta leer el error TNP y mapear a tu DTO
if (!resp.IsSuccessStatusCode)
{
    var code = ErrorCodeMapper.FromHttpStatus(status); // p.ej. 400->"12", 504->"68", 5xx->"96"
    if (SafeDeserializeStj<TnpErrorResponse>(body, out var tnpErr) && tnpErr is not null)
        return TnpAuthorizationMapper.FromError(tnpErr, code);

    // Si el cuerpo no es el error esperado, devolvemos DTO de error genérico
    var snippet = body is { Length: > 4096 } ? body[..4096] + "…(truncado)" : body;
    return new ResponseAuthorizationManualDto
    {
        ResponseCode      = code,
        AuthorizationCode = string.Empty,
        TransactionId     = string.Empty,
        Message           = $"TNP respondió {(int)status} {status}: {snippet}",
        Timestamp         = TimeUtil.IsoNowUtc()
    };
}

// 8) 2xx sin cuerpo
if (string.IsNullOrWhiteSpace(body))
{
    return new ResponseAuthorizationManualDto
    {
        ResponseCode      = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway), // "96"
        AuthorizationCode = string.Empty,
        TransactionId     = string.Empty,
        Message           = "TNP devolvió una respuesta vacía.",
        Timestamp         = TimeUtil.IsoNowUtc()
    };
}

// 9) Éxito 200: intentamos el envelope TNP (forma oficial)
if (SafeDeserializeStj<TnpAuthorizationEnvelope>(body, out var tnpEnv) &&
    tnpEnv?.GetAuthorizationManualResponse?.GetAuthorizationManualResult is TnpAuthorizationResult ok)
{
    return TnpAuthorizationMapper.FromSuccess(ok);
}

// 10) (Fallback) si alguna vez el tercero retornara tu DTO directo (poco probable)
if (SafeDeserializeStj<ResponseAuthorizationManualDto>(body, out var dto) && dto is not null)
    return dto;

// 11) 2xx pero formato inesperado
return new ResponseAuthorizationManualDto
{
    ResponseCode      = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway),
    AuthorizationCode = string.Empty,
    TransactionId     = string.Empty,
    Message           = "No se pudo interpretar la respuesta del TNP.",
    Timestamp         = TimeUtil.IsoNowUtc()
};





