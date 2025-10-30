Así va el codigo, ayudame a validar:

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pagos_Davivienda_TNP.Models.Dtos;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Services.Interfaces;

namespace Pagos_Davivienda_TNP.Controllers;

/// <summary>
/// API de Pagos DaviviendaTNP (v1).
/// Base URL: /davivienda-tnp/api/v1
/// </summary>
[ApiController]
[Route("davivienda-tnp/api/v1")]
[Produces("application/json")]
public class PagosDaviviendaTnpController(IPaymentAuthorizationService paymentService) : ControllerBase
{
    private readonly IPaymentAuthorizationService _paymentService = paymentService;

    /// <summary>Verifica salud del servicio.</summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
        => Ok(new { status = "UP", service = "DaviviendaTNP Payment API" });

    /// <summary>Procesa una autorización manual.</summary>
    /// <param name="request">Request con header + body.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("authorization/manual")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ResponseModel<GetAuthorizationManualResultEnvelope>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuthorizationManual([FromBody] AuthorizationRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // 1) Extraer el payload del cuerpo
        var input = request.Body.GetAuthorizationManual;

        // 2) Invocar servicio de dominio (que a su vez llama al tercero)
        var result = await _paymentService.AuthorizeManualAsync(new GetauthorizationManualDto
        {
            PMerchantID = input.PMerchantID,
            PTerminalID = input.PTerminalID,
            PPrimaryAccountNumber = input.PPrimaryAccountNumber,
            PDateExpiration = input.PDateExpiration,
            PCVV2 = input.PCVV2,
            PAmount = input.PAmount,
            PSystemsTraceAuditNumber = input.PSystemsTraceAuditNumber
        }, ct);

        // 3) Envolver en el contrato de datos solicitado
        var envelope = new GetAuthorizationManualResultEnvelope
        {
            GetAuthorizationManualResponse = new GetAuthorizationManualResponseContainer { GetResponseAuthorizationManualResult = result }
        };

        sw.Stop();

        // 4) Construir RESPUESTA FINAL: header + data
        var apiResponse = new ResponseModel<GetAuthorizationManualResultEnvelope>
        {
            Header = new ResponseHeader
            {
                ResponseId = Guid.NewGuid().ToString("N"),
                Timestamp = DateTime.UtcNow.ToString("o"),
                ProcessingTime = $"{sw.ElapsedMilliseconds}ms",
                StatusCode = result.ResponseCode, // "00" si aprobada
                Message = result.Message,
                RequestHeader = request.Header
            },
            Data = envelope
        };

        return Ok(apiResponse);
    }
}



using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Utils;
using System.Net;
using System.Text;

namespace Pagos_Davivienda_TNP.Services;

/// <summary>
/// Clase que implementa el servicio de autorización de pagos.
/// </summary>
/// <remarks>
/// Constructor de la clase PaymentAuthorizationService.
/// </remarks>
/// <param name="httpClientFactory">Instancia de la interfaz IHttpClientFactory para la Libreria RestUtilities.Connections</param>
public class PaymentAuthorizationService(IHttpClientFactory httpClientFactory) : Interfaces.IPaymentAuthorizationService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<ResponseAuthorizationManualDto> AuthorizeManualAsync(GetauthorizationManualDto request, CancellationToken ct = default)
    {

        /*Inicio del proceso de llamada al servicio externo TNP*/

        string host = GlobalConnection.Current.Host + "authorization/manual"; //Endpoint del servicio externo TNP

        try
        {
            using var client = _httpClientFactory.CreateClient("TNP");

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8);

            using HttpResponseMessage response = await client.PostAsync(host, content, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            var deserialized = JsonConvert.DeserializeObject<ResponseAuthorizationManualDto>(responseContent);

            if (deserialized is not null)
            {
                return deserialized;
            }
            else
            {
                var txnId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant();

                var responseAuthorization = new ResponseAuthorizationManualDto
                {
                    ResponseCode = "00",
                    AuthorizationCode = "123456",
                    TransactionId = txnId,
                    Message = "Transacción no se puedo completar",
                    Timestamp = TimeUtil.IsoNowUtc()
                };

                return responseAuthorization;
            }


        }
        catch (Exception ex)
        {
            var txnId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant();

            var responseAuthorization = new ResponseAuthorizationManualDto
            {
                ResponseCode = "00",
                AuthorizationCode = "123456",
                TransactionId = txnId,
                Message = ex.Message,
                Timestamp = TimeUtil.IsoNowUtc()
            };

            return responseAuthorization;
        }

    }
}



Lo que me indican es que las respuesta correcta puede ser:

**Estructura del Cuerpo de Respuesta**:
```json
{
  "GetAuthorizationManualResponse": {
    "GetAuthorizationManualResult": {
      "responseCode": "string",
      "authorizationCode": "string",
      "transactionId": "string",
      "message": "string",
      "timestamp": "string"
    }
  }
}
```

**Descripción de Campos de Respuesta**:
- `responseCode`: Código de respuesta de la transacción (00 = aprobada)
- `authorizationCode`: Código de autorización si fue aprobada
- `transactionId`: Identificador único de la transacción
- `message`: Mensaje de respuesta legible
- `timestamp`: Marca de tiempo del procesamiento de la transacción

---


Pero tambien se puede dar de esta forma:
---

## ⚠️ **Manejo de Errores**

### **Códigos de Estado HTTP**:
- `200 OK`: Petición procesada exitosamente
- `400 Bad Request`: Formato de petición inválido o campos requeridos faltantes
- `404 Not Found`: Endpoint no encontrado
- `405 Method Not Allowed`: Método HTTP no soportado para el endpoint
- `500 Internal Server Error`: Error de procesamiento del servidor

### **Formato de Respuesta de Error**:
```json
{
  "error": "Descripción del error",
  "status": 400,
  "timestamp": 1698765432000
}
```

### **Escenarios Comunes de Error**:

#### Campos Requeridos Faltantes:
```json
{
  "error": "Campo requerido faltante o vacío: pMerchantID",
  "status": 400,
  "timestamp": 1698765432000
}
```

#### Formato JSON Inválido:
```json
{
  "error": "Formato JSON inválido: Carácter inesperado en posición 15",
  "status": 400, 
  "timestamp": 1698765432000
}
```

Pero no estoy seguro si son errores que me devuelven o que darian antes.

