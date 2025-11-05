Deje la clase así :

using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Datos del resultado de autorización manual.
/// </summary>
public class ResponseAuthorizationManualDto
{
    /// <summary>Código de respuesta (00 = aprobada).</summary>
    [JsonPropertyName("ResponseCode")]
    public string ResponseCode { get; set; } = string.Empty;

    /// <summary>Código de autorización si aplica.</summary>
    [JsonPropertyName("AuthorizationCode")]
    public string AuthorizationCode { get; set; } = string.Empty;

    /// <summary>Identificador único de la transacción.</summary>
    [JsonPropertyName("TransactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Mensaje legible.</summary>
    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Marca de tiempo ISO 8601.</summary>
    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("ResponseCodeDescription")] public string? ResponseCodeDescription { get; set; }
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

Si la respuesta es 200, esto viene en la respuesta:

---- Request Body ----
{
  "GetAuthorizationManual": {
    "pMerchantID": "4001021",
    "pTerminalID": "P0055468",
    "pPrimaryAccountNumber": "5413330057004039",
    "pDateExpiration": "2512",
    "pCVV2": "000",
    "pAmount": "10000",
    "pSystemsTraceAuditNumber": "000002"
  }
}
---- Response ----
Status Code    : 200 OK
---- Response Headers ----
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Content-Type

---- Response Body ----
{
  "GetAuthorizationManualResponse": {
    "GetAuthorizationManualResult": {
      "ResponseCodeDescription": "94 - Transacci\u00F3n duplicada",
      "ResponseCode": "94",
      "TransactionType": "S",
      "SystemsTraceAuditNumber": "000002",
      "TimeLocalTrans": "205748",
      "Amount": "10,000.00",
      "MerchantID": "4001021",
      "MCC": "5999",
      "CurrencyCode": "340",
      "PrimaryAccountNumber": "541333******4039",
      "DateLocalTrans": "1104",
      "RetrievalReferenceNumber": "530902000002",
      "TerminalID": "P0055468"
    }
  }
}
Duración (ms)  : 316
=============== FIN HTTP CLIENT ================





si viene otro codigo distinto de 200 viene así la respuesta:

============== INICIO HTTP CLIENT ==============
TraceId        : 0HNGS35ED4JKT:00000003
Fecha/Hora     : 2025-11-04 21:17:02.533
Método         : POST
URL            : https://192.168.75.10:8443/davivienda-tnp/api/v1/authorization/manual
---- Request Headers ----
Accept: application/json

---- Request Body ----
{
  "GetAuthorizationManual": {
    "pMerchantID": "",
    "pTerminalID": "P0055468",
    "pPrimaryAccountNumber": "5413330057004039",
    "pDateExpiration": "2512",
    "pCVV2": "000",
    "pAmount": "10000",
    "pSystemsTraceAuditNumber": "000002"
  }
}
---- Response ----
Status Code    : 400 BadRequest
---- Response Headers ----
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Content-Type

---- Response Body ----
{
  "error": "Missing or empty required field: pMerchantID",
  "status": 400,
  "timestamp": 1762311624199
}
Duración (ms)  : 77
=============== FIN HTTP CLIENT ================


    Esperemos que al retornar la respuesta devuelva solo los campos que corresponden.

    Ademas de que si vienen acentos estos no se colocan como es debido

    
