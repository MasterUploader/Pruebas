Mira esta respuesta no mapeo todos los campos:


============== INICIO HTTP CLIENT ==============
TraceId        : 0HNGS3TH8P4ES:0000000B
Fecha/Hora     : 2025-11-04 21:58:34.220
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
      "TimeLocalTrans": "214155",
      "Amount": "10,000.00",
      "MerchantID": "4001021",
      "MCC": "5999",
      "CurrencyCode": "340",
      "PrimaryAccountNumber": "541333******4039",
      "DateLocalTrans": "1104",
      "RetrievalReferenceNumber": "530903000002",
      "TerminalID": "P0055468"
    }
  }
}
Duración (ms)  : 1368
=============== FIN HTTP CLIENT ================


----------------------------------Response Info---------------------------------
Inicio: 2025-11-04 21:59:15
-------------------------------------------------------------------------------
Código Estado: 409
Headers: [Content-Type, application/json; charset=utf-8]
Cuerpo:

                              {
                                "header": {
                                  "responseId": "0fe95eae65a242d9bec240a33c0621c3",
                                  "timestamp": "2025-11-05T03:59:05.1109321Z",
                                  "processingTime": "39524ms",
                                  "statusCode": "94",
                                  "message": "94 - Transacción duplicada",
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
                                      "transactionId": "530903000002",
                                      "message": "94 - Transacción duplicada",
                                      "timestamp": "2025-11-05T03:59:00.3959178Z"
                                    }
                                  }
                                }
                              }
----------------------------------Response Info---------------------------------
Fin: 2025-11-04 21:59:15
-------------------------------------------------------------------------------
