Tengo esta petición:

curl -k -X POST "https://localhost:8443/davivienda-tnp/api/v1/authorization/manual" \
  -H "Content-Type: application/json" \
  -d '{
    "GetAuthorizationManual": {
      "pMerchantID": "4001021",
      "pTerminalID": "P0055468",
      "pPrimaryAccountNumber": "5413330057004039",
      "pDateExpiration": "2512",
      "pCVV2": "000",
      "pAmount": "10000",
      "pSystemsTraceAuditNumber": "000002"
    }
  }'

La hago con este código:

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

            HttpClientHandler handler = new()
            {
                ServerCertificateCustomValidationCallback = (sender, certificate, chain, SslPolicyErrors) => true

            };

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

Pero la conexión es directa no necesito usuario y contraseña, osea que no ocupo en usuario TNP, como lo hago sin afectar los logs de la librerias RestUtilities.Connections y RestUtilities.Logging
