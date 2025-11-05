using System.Net;
using System.Net.Http.Headers;
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

    // Opciones STJ compartidas (respeta nombres anotados y es case-insensitive al leer)
    private static readonly JsonSerializerOptions StjWriteOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private static readonly JsonSerializerOptions StjReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ResponseAuthorizationManualDto> AuthorizeManualAsync(AuthorizationBody request, CancellationToken ct = default)
    {
        var baseHost = (GlobalConnection.Current.Host ?? string.Empty).TrimEnd('/');
        var url = $"{baseHost}/authorization/manual";

        try
        {
            using var client = _httpClientFactory.CreateClient("TNP");

            // Headers
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Serializar EXACTO lo que el tercero espera (ya viene con GetAuthorizationManual como raíz)
            var json = JsonSerializer.Serialize(request, StjWriteOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await client.PostAsync(url, content, ct);
            var status = resp.StatusCode;

            var body = resp.Content is null ? string.Empty : await resp.Content.ReadAsStringAsync(ct);

            // No-2xx → mapear a código negocio y devolver DTO de error de negocio
            if (!resp.IsSuccessStatusCode)
            {
                var snippet = body is { Length: > 4096 } ? body[..4096] + "…(truncado)" : body;
                return new ResponseAuthorizationManualDto
                {
                    ResponseCode = ErrorCodeMapper.FromHttpStatus(status),
                    AuthorizationCode = string.Empty,
                    TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                    Message = $"TNP respondió {(int)status} {status}: {snippet}",
                    Timestamp = TimeUtil.IsoNowUtc()
                };
            }

            // 2xx sin cuerpo
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

            // Intento A) Envelope esperado del tercero
            if (SafeDeserializeStj<GetAuthorizationManualResultEnvelope>(body, out var env) &&
                env?.GetAuthorizationManualResponse?.GetAuthorizationManualResult is not null)
            {
                return env.GetAuthorizationManualResponse.GetAuthorizationManualResult;
            }

            // Intento B) DTO directo
            if (SafeDeserializeStj<ResponseAuthorizationManualDto>(body, out var dto) && dto is not null)
                return dto;

            // Formato inesperado aun con 2xx
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

    private static bool SafeDeserializeStj<T>(string json, out T? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(json, StjReadOptions);
            return value is not null;
        }
        catch
        {
            value = default;
            return false;
        }
    }
}
