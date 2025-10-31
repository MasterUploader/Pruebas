// Utils/ErrorCodeMapper.cs
using System.Net;

namespace Pagos_Davivienda_TNP.Utils;

/// <summary>
/// Mapea códigos HTTP ↔ códigos de negocio ISO-like.
/// Ajusta si tu matriz oficial difiere.
/// </summary>
public static class ErrorCodeMapper
{
    // HTTP → Código negocio
    public static string FromHttpStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => "68", // Timeout
        HttpStatusCode.BadRequest                                     => "12", // Solicitud inválida
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden       => "05", // No autorizado
        HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable
        or HttpStatusCode.InternalServerError                         => "96", // Falla del sistema proveedor
        _                                                             => "96"
    };

    // Código negocio → HTTP
    public static HttpStatusCode ToHttpStatus(string responseCode) => responseCode switch
    {
        "00" => HttpStatusCode.OK,
        "68" => HttpStatusCode.GatewayTimeout,
        "12" => HttpStatusCode.BadRequest,
        "05" => HttpStatusCode.Unauthorized,   // o 403 si tu política lo exige
        "91" => HttpStatusCode.ServiceUnavailable, // opcional si lo usas
        _    => HttpStatusCode.BadGateway
    };
}





// Services/PaymentAuthorizationService.cs (solo bloques catch ajustados)
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Services;

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

            var body = resp.Content is null ? string.Empty : await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                var snippet = body is { Length: > 4096 } ? body[..4096] + "…(truncado)" : body;
                // Mapea el HTTP del tercero a código negocio y regresa DTO
                return new ResponseAuthorizationManualDto
                {
                    ResponseCode = ErrorCodeMapper.FromHttpStatus(status),
                    AuthorizationCode = string.Empty,
                    TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                    Message = $"TNP respondió {(int)status} {status}: {snippet}",
                    Timestamp = TimeUtil.IsoNowUtc()
                };
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return new ResponseAuthorizationManualDto
                {
                    ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway),
                    AuthorizationCode = string.Empty,
                    TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                    Message = "TNP devolvió una respuesta vacía.",
                    Timestamp = TimeUtil.IsoNowUtc()
                };
            }

            var env = SafeDeserialize<GetAuthorizationManualResultEnvelope>(body);
            if (env?.GetAuthorizationManualResponse?.GetAuthorizationManualResult is not null)
                return env.GetAuthorizationManualResponse.GetAuthorizationManualResult;

            var dto = SafeDeserialize<ResponseAuthorizationManualDto>(body);
            if (dto is not null)
                return dto;

            return new ResponseAuthorizationManualDto
            {
                ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway),
                AuthorizationCode = string.Empty,
                TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = "No se pudo interpretar la respuesta del TNP.",
                Timestamp = TimeUtil.IsoNowUtc()
            };
        }
        catch (TaskCanceledException)
        {
            return new ResponseAuthorizationManualDto
            {
                ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.GatewayTimeout),
                AuthorizationCode = string.Empty,
                TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = "Timeout al invocar el servicio TNP.",
                Timestamp = TimeUtil.IsoNowUtc()
            };
        }
        catch (OperationCanceledException)
        {
            return new ResponseAuthorizationManualDto
            {
                ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.GatewayTimeout),
                AuthorizationCode = string.Empty,
                TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = "La operación fue cancelada.",
                Timestamp = TimeUtil.IsoNowUtc()
            };
        }
        catch (HttpRequestException ex)
        {
            var status = ex.StatusCode ?? HttpStatusCode.BadGateway;
            return new ResponseAuthorizationManualDto
            {
                ResponseCode = ErrorCodeMapper.FromHttpStatus(status),
                AuthorizationCode = string.Empty,
                TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = $"Error al invocar el servicio TNP: {ex.Message}",
                Timestamp = TimeUtil.IsoNowUtc()
            };
        }
        catch (Exception ex)
        {
            return new ResponseAuthorizationManualDto
            {
                ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway),
                AuthorizationCode = string.Empty,
                TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = $"Error inesperado al invocar TNP: {ex.Message}",
                Timestamp = TimeUtil.IsoNowUtc()
            };
        }
    }

    private static T? SafeDeserialize<T>(string json)
    {
        try { return JsonConvert.DeserializeObject<T>(json); }
        catch { return default; }
    }
}





// Controllers/PagosDaviviendaTnpController.cs (método actualizado)
[HttpPost("authorization/manual")]
[Consumes("application/json")]
[ProducesResponseType(typeof(ResponseModel<GetAuthorizationManualResultEnvelope>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GetAuthorizationManual([FromBody] AuthorizationRequest request, CancellationToken ct)
{
    var sw = Stopwatch.StartNew();

    var input = request.Body.GetAuthorizationManual;

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
            ResponseId     = Guid.NewGuid().ToString("N"),
            Timestamp      = DateTime.UtcNow.ToString("o"),
            ProcessingTime = $"{sw.ElapsedMilliseconds}ms",
            StatusCode     = result.ResponseCode, // negocio ("00","68","12",…)
            Message        = result.Message,
            RequestHeader  = request.Header
        },
        Data = envelope
    };

    // 200 si "00"; si no, mapear a HTTP error preservando el body estándar
    var http = ErrorCodeMapper.ToHttpStatus(result.ResponseCode);
    return StatusCode((int)http, apiResponse);
}

