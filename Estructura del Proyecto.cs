Así fue la petición, fue exitosa porque respondio, pero dio un mensaje de error de negocios, lo que veo es que la respuesta no se mapeo correctamente:


============== INICIO HTTP CLIENT ==============
TraceId        : 0HNGS2RSG0DTF:0000000B
Fecha/Hora     : 2025-11-04 20:58:23.119
Método         : POST
URL            : https://192.168.75.10:8443/davivienda-tnp/api/v1/authorization/manual
---- Request Headers ----
Accept: application/json

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
      "TimeLocalTrans": "204144",
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
Duración (ms)  : 256
=============== FIN HTTP CLIENT ================


----------------------------------Response Info---------------------------------
Inicio: 2025-11-04 20:59:53
-------------------------------------------------------------------------------
Código Estado: 502
Headers: [Content-Type, application/json; charset=utf-8]
Cuerpo:

                              {
                                "header": {
                                  "responseId": "5181d7a2118d4e3ca10060b356dd474e",
                                  "timestamp": "2025-11-05T02:59:28.7628459Z",
                                  "processingTime": "76198ms",
                                  "statusCode": "94",
                                  "message": "",
                                  "requestHeader": {
                                    "h-request-id": "string",
                                    "h-channel": "string",
                                    "h-terminal": "string",
                                    "h-organization": "string",
                                    "h-user-id": "string",
                                    "h-provider": "string",
                                    "h-session-id": "string",
                                    "h-client-ip": "string",
                                    "h-timestamp": "string"
                                  }
                                },
                                "data": {
                                  "GetAuthorizationManualResponse": {
                                    "GetAuthorizationManualResult": {
                                      "responseCode": "94",
                                      "authorizationCode": "",
                                      "transactionId": "",
                                      "message": "",
                                      "timestamp": ""
                                    }
                                  }
                                }
                              }
----------------------------------Response Info---------------------------------
Fin: 2025-11-04 20:59:53
-------------------------------------------------------------------------------


    Así tengo las clases de respuesta:

using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos;

/// <summary>
/// Encabezado estándar de la respuesta.
/// </summary>
public sealed class ResponseHeader
{
    /// <summary>Identificador único de la respuesta.</summary>
    [JsonPropertyName("responseId")]
    public string ResponseId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Marca de tiempo en UTC (ISO 8601).</summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

    /// <summary>Tiempo total de procesamiento del request.</summary>
    [JsonPropertyName("processingTime")]
    public string ProcessingTime { get; set; } = string.Empty;

    /// <summary>Código de estado propio del negocio (p.ej. "00").</summary>
    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    /// <summary>Mensaje legible para el consumidor.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Header original del request, eco para trazabilidad.</summary>
    [JsonPropertyName("requestHeader")]
    public RequestHeader RequestHeader { get; set; } = new();
}

using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos;

/// <summary>
/// Contenedor genérico de respuesta: header + data.
/// </summary>
/// <typeparam name="TData">Tipo del payload de datos.</typeparam>
public sealed class ResponseModel<TData>
{
    [JsonPropertyName("header")]
    public ResponseHeader Header { get; set; } = new();

    [JsonPropertyName("data")]
    public TData? Data { get; set; }
}
