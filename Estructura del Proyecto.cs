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
            throw new HttpRequestException("Timeout al invocar el servicio TNP.", null, HttpStatusCode.GatewayTimeout);
        }
        catch (OperationCanceledException)
        {
            // Cancelación “activa” (por ejemplo, desde el caller)
            throw new HttpRequestException("La operación fue cancelada.", null, HttpStatusCode.GatewayTimeout);
        }
        // HttpRequestException: la re-lanza el middleware con el status embebido
        catch (HttpRequestException) { throw; }
        catch (Exception ex)
        {
            // Cualquier otra falla: 502 Bad Gateway
            throw new HttpRequestException($"Error inesperado al invocar TNP: {ex.Message}", ex, HttpStatusCode.BadGateway);
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
