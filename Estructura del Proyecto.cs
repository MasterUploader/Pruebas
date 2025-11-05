Este es el codigo:
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Services;

public class PaymentAuthorizationService(IHttpClientFactory httpClientFactory)
    : Interfaces.IPaymentAuthorizationService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<ResponseAuthorizationManualDto> AuthorizeManualAsync(AuthorizationBody request, CancellationToken ct = default)
    {
        var baseHost = (GlobalConnection.Current.Host ?? string.Empty).TrimEnd('/');
        var url = $"{baseHost}/authorization/manual";

        try
        {
            //1. Crear cliente HTTP
            using var client = _httpClientFactory.CreateClient("TNP");

            //2.Headers
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy  = null, //Respeta nombres anotados en DTOs
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            });
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

Pero ahora no funciona SafeDeserializa, debe usar System.Text ahora.
