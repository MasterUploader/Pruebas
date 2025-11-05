Tengo esta respuesta exitosa 200 y que deberia devolver el formato completo porque fue el que me respondio:


============== INICIO HTTP CLIENT ==============
TraceId        : 0HNGS3F4LHTFU:0000000B
Fecha/Hora     : 2025-11-04 21:32:46.870
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
    "pSystemsTraceAuditNumber": "000003"
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
      "ResponseCodeDescription": "00 - Aprobada o completada exitosamente",
      "ResponseCode": "00",
      "TransactionType": "S",
      "SystemsTraceAuditNumber": "000003",
      "TimeLocalTrans": "211603",
      "Amount": "10,000.00",
      "MerchantID": "4001021",
      "MCC": "5999",
      "AuthorizationIdentificationResponse": "792830",
      "CurrencyCode": "340",
      "PrimaryAccountNumber": "541333******4039",
      "DateLocalTrans": "1104",
      "RetrievalReferenceNumber": "530903000003",
      "TerminalID": "P0055468"
    }
  }
}
Duración (ms)  : 4647
=============== FIN HTTP CLIENT ================


----------------------------------Response Info---------------------------------
Inicio: 2025-11-04 21:33:59
-------------------------------------------------------------------------------
Código Estado: 200
Headers: [Content-Type, application/json; charset=utf-8]
Cuerpo:

                              {
                                "header": {
                                  "responseId": "5d313c4a55c4499792ebc98df552de00",
                                  "timestamp": "2025-11-05T03:33:56.4126971Z",
                                  "processingTime": "81839ms",
                                  "statusCode": "00",
                                  "message": "00 - Aprobada o completada exitosamente",
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
                                      "responseCode": "00",
                                      "authorizationCode": "",
                                      "transactionId": "530903000003",
                                      "message": "00 - Aprobada o completada exitosamente",
                                      "timestamp": "2025-11-05T03:33:48.6696319Z"
                                    }
                                  }
                                }
                              }



Pero lop devuelvo incompleto, es posible que el problema este en la asignación en el controlador?:



using Microsoft.AspNetCore.Mvc;
using Pagos_Davivienda_TNP.Models.Dtos;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Services.Interfaces;
using Pagos_Davivienda_TNP.Utils;
using System.Diagnostics;

namespace Pagos_Davivienda_TNP.Controllers;

/// <summary>
/// API de Pagos DaviviendaTNP (v1).
/// Base URL: /davivienda-tnp/api/v1
/// </summary>
[ApiController]
[Route("v1/[controller]")]
[Produces("application/json")]
public class PagosDaviviendaTnpController(IPaymentAuthorizationService paymentService) : ControllerBase
{
    private readonly IPaymentAuthorizationService _paymentService = paymentService;

    /// <summary>Procesa una autorización manual.</summary>
    /// <param name="request">Request con header + body.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("Authorization/")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ResponseModel<GetAuthorizationManualResultEnvelope>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuthorizationManual([FromBody] AuthorizationRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        var input = request.Body.GetAuthorizationManual;

        var result = await _paymentService.AuthorizeManualAsync(new AuthorizationBody
        {
            GetAuthorizationManual =
            {
                PMerchantID = input.PMerchantID,
                PTerminalID = input.PTerminalID,
                PPrimaryAccountNumber = input.PPrimaryAccountNumber,
                PDateExpiration = input.PDateExpiration,
                PCVV2 = input.PCVV2,
                PAmount = input.PAmount,
                PSystemsTraceAuditNumber = input.PSystemsTraceAuditNumber
            }

        }, ct);

        var envelope = new GetAuthorizationManualResultEnvelope
        {
            GetAuthorizationManualResponse = new GetAuthorizationManualResponseContainer
            {
                GetAuthorizationManualResult = result
            }
        };

        sw.Stop();

        var apiResponse = new ResponseModel<GetAuthorizationManualResultEnvelope>
        {
            Header = new ResponseHeader
            {
                ResponseId = Guid.NewGuid().ToString("N"),
                Timestamp = DateTime.UtcNow.ToString("o"),
                ProcessingTime = $"{sw.ElapsedMilliseconds}ms",
                StatusCode = result.ResponseCode, // negocio ("00","68","12",…)
                Message = result.Message,
                RequestHeader = request.Header
            },
            Data = envelope
        };

        // 200 si "00"; si no, mapear a HTTP error preservando el body estándar
        var http = ErrorCodeMapper.ToHttpStatus(result.ResponseCode);
        return StatusCode((int)http, apiResponse);
    }
}
