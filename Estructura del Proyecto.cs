using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Envelope que devuelve el tercero TNP (nombres tal cual vienen).
/// </summary>
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

/// <summary>
/// Resultado del tercero (campos en PascalCase).
/// </summary>
public sealed class TnpAuthorizationResult
{
    [JsonPropertyName("ResponseCodeDescription")] public string? ResponseCodeDescription { get; set; }
    [JsonPropertyName("ResponseCode")]           public string? ResponseCode { get; set; }
    [JsonPropertyName("AuthorizationCode")]      public string? AuthorizationCode { get; set; } // opcional en error
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






using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Adaptador de la respuesta TNP al contrato interno de salida.
/// </summary>
public static class TnpAuthorizationMapper
{
    /// <summary>
    /// - responseCode = ResponseCode
    /// - message = ResponseCodeDescription
    /// - transactionId = RetrievalReferenceNumber (fallback: SystemsTraceAuditNumber)
    /// - authorizationCode = AuthorizationCode (si viene)
    /// - timestamp = ahora (si TNP no lo manda en ISO)
    /// </summary>
    public static ResponseAuthorizationManualDto ToInternal(TnpAuthorizationResult src)
    {
        var txn = !string.IsNullOrWhiteSpace(src.RetrievalReferenceNumber)
                    ? src.RetrievalReferenceNumber!
                    : (src.SystemsTraceAuditNumber ?? string.Empty);

        return new ResponseAuthorizationManualDto
        {
            ResponseCode      = src.ResponseCode ?? string.Empty,
            AuthorizationCode = src.AuthorizationCode ?? string.Empty,
            TransactionId     = txn,
            Message           = src.ResponseCodeDescription ?? string.Empty,
            Timestamp         = TimeUtil.IsoNowUtc()
        };
    }
}


// Intento A: envelope TNP (modelos del tercero)
if (SafeDeserializeStj<TnpAuthorizationEnvelope>(body, out var tnpEnv) &&
    tnpEnv?.GetAuthorizationManualResponse?.GetAuthorizationManualResult is TnpAuthorizationResult tnpRes)
{
    // Convertimos al DTO interno estándar
    return TnpAuthorizationMapper.ToInternal(tnpRes);
}

// Intento B: tu DTO directo (por si cambian el formato algún día)
if (SafeDeserializeStj<ResponseAuthorizationManualDto>(body, out var dto) && dto is not null)
    return dto;

// Formato inesperado…






