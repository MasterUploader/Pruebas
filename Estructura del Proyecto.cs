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
        // Ojo: asumo que GlobalConnection.Current.Host termina con "/" (ajústalo si no).
        var url = $"{GlobalConnection.Current.Host}authorization/manual";

        using var client = _httpClientFactory.CreateClient("TNP");

        // Si configuraste BaseAddress en el named client "TNP", usa ruta relativa:
        // using var resp = await client.PostAsync("authorization/manual", content, ct);

        var json = JsonConvert.SerializeObject(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await client.PostAsync(url, content, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        // ❶ Si no es 2xx: mapea a error controlado (tu middleware lo transformará)
        if (!resp.IsSuccessStatusCode)
        {
            // Propaga el detalle para que el middleware genere:
            // { "error": "...", "status": <http>, "timestamp": ... }
            throw new HttpRequestException(
                $"TNP respondió {(int)resp.StatusCode} {resp.StatusCode}: {body}",
                null,
                resp.StatusCode);
        }

        // ❷ Tolerar dos formatos: envelope completo o dto directo
        //    a) { "GetAuthorizationManualResponse": { "GetAuthorizationManualResult": { ... } } }
        //    b) { "responseCode": "...", ... }
        try
        {
            // Intento a) Envelope
            var env = JsonConvert.DeserializeObject<GetAuthorizationManualResultEnvelope>(body);
            if (env?.GetAuthorizationManualResponse?.GetAuthorizationManualResult is not null)
                return env.GetAuthorizationManualResponse.GetAuthorizationManualResult;
        }
        catch { /* ignora y prueba formato b */ }

        try
        {
            // Intento b) DTO directo
            var dto = JsonConvert.DeserializeObject<ResponseAuthorizationManualDto>(body);
            if (dto is not null) return dto;
        }
        catch { /* caerá al fallback */ }

        // ❸ Fallback: no pudimos deserializar → error controlado
        throw new HttpRequestException(
            "No se pudo interpretar la respuesta del TNP.",
            null,
            HttpStatusCode.BadGateway);
    }
}
