Así va quedando el codigo:

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
            GetAuthorizationManualResponse = new GetAuthorizationManualResponseContainer { GetAuthorizationManualResult = result }
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



using System.Net;
using System.Text;
using Newtonsoft.Json;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Services;

/// <summary>Implementación que consume el API externo TNP.</summary>
public class PaymentAuthorizationService(IHttpClientFactory httpClientFactory)
    : Interfaces.IPaymentAuthorizationService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<ResponseAuthorizationManualDto> AuthorizeManualAsync(GetauthorizationManualDto request, CancellationToken ct = default)
    {
        var baseHost = (GlobalConnection.Current.Host ?? string.Empty).TrimEnd('/');
        var url = $"{baseHost}/authorization/manual";

        try
        {
            using var client = _httpClientFactory.CreateClient("TNP");

            var json = JsonConvert.SerializeObject(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await client.PostAsync(url, content, ct);
            var status = resp.StatusCode;

            // Leer siempre el cuerpo (si existe) para logging/diagnóstico
            var body = resp.Content is null ? string.Empty : await resp.Content.ReadAsStringAsync(ct);

            // 1) No-2xx → propagar con StatusCode real
            if (!resp.IsSuccessStatusCode)
            {
                // Limita el tamaño del body para no saturar logs/excepción
                var snippet = body is { Length: > 4096 } ? body[..4096] + "…(truncado)" : body;
                throw new HttpRequestException($"TNP respondió {(int)status} {status}: {snippet}", null, status);
            }

            // 2) 200/204 pero sin cuerpo → tratar como error de pasarela
            if (string.IsNullOrWhiteSpace(body))
                throw new HttpRequestException("TNP devolvió una respuesta vacía.", null, HttpStatusCode.BadGateway);

            // 3) Intento a) Envelope: { "GetAuthorizationManualResponse": { "GetAuthorizationManualResult": { ... } } }
            var env = SafeDeserialize<GetAuthorizationManualResultEnvelope>(body);
            if (env?.GetAuthorizationManualResponse?.GetAuthorizationManualResult is not null)
                return env.GetAuthorizationManualResponse.GetAuthorizationManualResult;

            // 4) Intento b) DTO directo: { "responseCode": "...", ... }
            var dto = SafeDeserialize<ResponseAuthorizationManualDto>(body);
            if (dto is not null)
                return dto;

            // 5) Formato inesperado aun con 2xx → error controlado
            throw new HttpRequestException("No se pudo interpretar la respuesta del TNP.", null, HttpStatusCode.BadGateway);
        }
        catch (TaskCanceledException)
        {
            // Timeout de HttpClient o cancelación por ct
            GetAuthorizationManualResultEnvelope resp = new()
            {
                GetAuthorizationManualResponse = new GetAuthorizationManualResponseContainer
                {
                    GetAuthorizationManualResult = new ResponseAuthorizationManualDto
                    {
                        ResponseCode = ((int)HttpStatusCode.GatewayTimeout).ToString(),
                        AuthorizationCode = string.Empty,
                        TransactionId = string.Empty,
                        Message = "Timeout al invocar el servicio TNP.",
                        Timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };
            return resp.GetAuthorizationManualResponse.GetAuthorizationManualResult;
        }
        catch (OperationCanceledException)
        {
            // Cancelación “activa” (por ejemplo, desde el caller)
            GetAuthorizationManualResultEnvelope resp = new()
            {
                GetAuthorizationManualResponse = new GetAuthorizationManualResponseContainer
                {
                    GetAuthorizationManualResult = new ResponseAuthorizationManualDto
                    {
                        ResponseCode = ((int)HttpStatusCode.GatewayTimeout).ToString(),
                        AuthorizationCode = string.Empty,
                        TransactionId = string.Empty,
                        Message = "La operación fue cancelada.",
                        Timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };
            return resp.GetAuthorizationManualResponse.GetAuthorizationManualResult;
        }
        // HttpRequestException: la re-lanza el middleware con el status embebido
        catch (HttpRequestException)
        {
            GetAuthorizationManualResultEnvelope resp = new()
            {
                GetAuthorizationManualResponse = new GetAuthorizationManualResponseContainer
                {
                    GetAuthorizationManualResult = new ResponseAuthorizationManualDto
                    {
                        ResponseCode = ((int)HttpStatusCode.BadGateway).ToString(),
                        AuthorizationCode = string.Empty,
                        TransactionId = string.Empty,
                        Message = "Error al invocar el servicio TNP.",
                        Timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };
            return resp.GetAuthorizationManualResponse.GetAuthorizationManualResult;

        }
        catch (Exception)
        {
            // Cualquier otra falla: 502 Bad Gateway
            GetAuthorizationManualResultEnvelope resp = new()
            {
                GetAuthorizationManualResponse = new GetAuthorizationManualResponseContainer
                {
                    GetAuthorizationManualResult = new ResponseAuthorizationManualDto
                    {
                        ResponseCode = ((int)HttpStatusCode.BadGateway).ToString(),
                        AuthorizationCode = string.Empty,
                        TransactionId = string.Empty,
                        Message = "Error inesperado al invocar el servicio TNP.",
                        Timestamp = DateTime.UtcNow.ToString("o")
                    }
                }
            };
            return resp.GetAuthorizationManualResponse.GetAuthorizationManualResult;
        }
    }

    /// <summary>
    /// Deserializa sin lanzar excepciones; devuelve null si falla.
    /// Útil para intentar múltiples formatos de respuesta.
    /// </summary>
    private static T? SafeDeserialize<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch
        {
            return default;
        }
    }
}


Necesito que cuando llegue al controlado la respuesta corresponda, si es OK es OK, y si es otra pues se devuelva con ese formato.
