using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

public sealed class TnpAuthorizationResult
{
    [JsonPropertyName("ResponseCodeDescription")] public string? ResponseCodeDescription { get; set; }
    [JsonPropertyName("ResponseCode")] public string? ResponseCode { get; set; }

    // 🔽 NUEVO CAMPO: viene cuando la transacción es aprobada (00)
    [JsonPropertyName("AuthorizationIdentificationResponse")] 
    public string? AuthorizationIdentificationResponse { get; set; }

    [JsonPropertyName("AuthorizationCode")] public string? AuthorizationCode { get; set; }
    [JsonPropertyName("RetrievalReferenceNumber")] public string? RetrievalReferenceNumber { get; set; }
    [JsonPropertyName("SystemsTraceAuditNumber")] public string? SystemsTraceAuditNumber { get; set; }
    [JsonPropertyName("TransactionType")] public string? TransactionType { get; set; }
    [JsonPropertyName("TimeLocalTrans")] public string? TimeLocalTrans { get; set; }
    [JsonPropertyName("DateLocalTrans")] public string? DateLocalTrans { get; set; }
    [JsonPropertyName("Amount")] public string? Amount { get; set; }
    [JsonPropertyName("MerchantID")] public string? MerchantID { get; set; }
    [JsonPropertyName("MCC")] public string? MCC { get; set; }
    [JsonPropertyName("CurrencyCode")] public string? CurrencyCode { get; set; }
    [JsonPropertyName("PrimaryAccountNumber")] public string? PrimaryAccountNumber { get; set; }
    [JsonPropertyName("TerminalID")] public string? TerminalID { get; set; }
}





public static ResponseAuthorizationManualDto FromSuccess(TnpAuthorizationResult src)
{
    // Preferimos el RRN; si no viene, usamos STAN.
    var txnId = !string.IsNullOrWhiteSpace(src.RetrievalReferenceNumber)
        ? src.RetrievalReferenceNumber!
        : (src.SystemsTraceAuditNumber ?? string.Empty);

    // 🔽 Usamos AuthorizationIdentificationResponse si AuthorizationCode viene vacío.
    var authCode = !string.IsNullOrWhiteSpace(src.AuthorizationCode)
        ? src.AuthorizationCode!
        : (src.AuthorizationIdentificationResponse ?? string.Empty);

    return new ResponseAuthorizationManualDto
    {
        ResponseCode      = src.ResponseCode ?? string.Empty,
        AuthorizationCode = authCode,
        TransactionId     = txnId,
        Message           = src.ResponseCodeDescription ?? string.Empty,
        Timestamp         = TimeUtil.IsoNowUtc()
    };
}





