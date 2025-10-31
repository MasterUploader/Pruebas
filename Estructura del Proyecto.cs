// Models/Dtos/Health/HealthStatusDto.cs
using System.Text.Json.Serialization;
using System.Net;

namespace Pagos_Davivienda_TNP.Models.Dtos.Health;

/// <summary>
/// Resultado del health estándar.
/// </summary>
public sealed class HealthStatusDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "DOWN"; // "UP" | "DOWN"

    [JsonPropertyName("service")]
    public string Service { get; set; } = "DaviviendaTNP Payment API";

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

    /// <summary>Detalle opcional del problema (no incluir datos sensibles).</summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }

    /// <summary>HTTP a devolver por el controller. No se serializa.</summary>
    [JsonIgnore]
    public HttpStatusCode HttpStatus { get; set; } = HttpStatusCode.ServiceUnavailable;
}





// Services/Interfaces/IHealthService.cs
using System.Threading;
using System.Threading.Tasks;
using Pagos_Davivienda_TNP.Models.Dtos.Health;

namespace Pagos_Davivienda_TNP.Services.Interfaces;

/// <summary>Contrato para verificar la salud del tercero TNP.</summary>
public interface IHealthService
{
    /// <summary>
    /// Verifica la salud llamando al endpoint /health del tercero.
    /// Retorna un DTO listo para responder y el código HTTP sugerido en <see cref="HealthStatusDto.HttpStatus"/>.
    /// </summary>
    Task<HealthStatusDto> CheckAsync(CancellationToken ct = default);
}




// Services/HealthService.cs
using System.Net;
using System.Text.Json;
using Pagos_Davivienda_TNP.Models.Dtos.Health;
using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Services;

/// <summary>Implementa la verificación de salud contra el tercero TNP.</summary>
public sealed class HealthService(IHttpClientFactory httpClientFactory) : Interfaces.IHealthService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<HealthStatusDto> CheckAsync(CancellationToken ct = default)
    {
        var baseHost = (GlobalConnection.Current.Host ?? string.Empty).TrimEnd('/');
        var url = $"{baseHost}/health";

        try
        {
            using var client = _httpClientFactory.CreateClient("TNP");
            using var resp = await client.GetAsync(url, ct);
            var body = resp.Content is null ? string.Empty : await resp.Content.ReadAsStringAsync(ct);

            // 1) No-2xx → DOWN con mapeo de HTTP
            if (!resp.IsSuccessStatusCode)
            {
                var snippet = string.IsNullOrWhiteSpace(body) ? string.Empty
                              : body.Length > 4096 ? body[..4096] + "…(truncado)" : body;

                return new HealthStatusDto
                {
                    Status = "DOWN",
                    Service = "DaviviendaTNP Payment API",
                    Timestamp = TimeUtil.IsoNowUtc(),
                    Details = $"TNP respondió {(int)resp.StatusCode} {resp.StatusCode}. {snippet}",
                    HttpStatus = resp.StatusCode
                };
            }

            // 2) 2xx pero vacío → DOWN 502
            if (string.IsNullOrWhiteSpace(body))
            {
                return new HealthStatusDto
                {
                    Status = "DOWN",
                    Service = "DaviviendaTNP Payment API",
                    Timestamp = TimeUtil.IsoNowUtc(),
                    Details = "TNP devolvió respuesta vacía.",
                    HttpStatus = HttpStatusCode.BadGateway
                };
            }

            // 3) Intentar leer el JSON del tercero; si trae "status":"UP" lo reflejamos
            var third = SafeDeserialize<ThirdHealth>(body);

            if (third is not null && string.Equals(third.status, "UP", StringComparison.OrdinalIgnoreCase))
            {
                return new HealthStatusDto
                {
                    Status = "UP",
                    Service = third.service ?? "DaviviendaTNP Payment API",
                    Timestamp = TimeUtil.IsoNowUtc(),
                    Details = null,
                    HttpStatus = HttpStatusCode.OK
                };
            }

            // 4) 2xx pero status no es UP → considerar DOWN 503
            return new HealthStatusDto
            {
                Status = "DOWN",
                Service = third?.service ?? "DaviviendaTNP Payment API",
                Timestamp = TimeUtil.IsoNowUtc(),
                Details = third is null ? "Formato de respuesta no reconocido." : $"Estado reportado: {third.status}",
                HttpStatus = HttpStatusCode.ServiceUnavailable
            };
        }
        catch (TaskCanceledException)
        {
            return new HealthStatusDto
            {
                Status = "DOWN",
                Service = "DaviviendaTNP Payment API",
                Timestamp = TimeUtil.IsoNowUtc(),
                Details = "Timeout al invocar el servicio TNP.",
                HttpStatus = HttpStatusCode.GatewayTimeout
            };
        }
        catch (OperationCanceledException)
        {
            return new HealthStatusDto
            {
                Status = "DOWN",
                Service = "DaviviendaTNP Payment API",
                Timestamp = TimeUtil.IsoNowUtc(),
                Details = "La operación fue cancelada.",
                HttpStatus = HttpStatusCode.GatewayTimeout
            };
        }
        catch (Exception ex)
        {
            return new HealthStatusDto
            {
                Status = "DOWN",
                Service = "DaviviendaTNP Payment API",
                Timestamp = TimeUtil.IsoNowUtc(),
                Details = $"Error inesperado: {ex.Message}",
                HttpStatus = HttpStatusCode.BadGateway
            };
        }
    }

    private static T? SafeDeserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { return default; }
    }

    // Modelo para leer el JSON del tercero
    private sealed class ThirdHealth
    {
        public string? status { get; set; }
        public string? service { get; set; }
    }
}

// Controllers/PagosDaviviendaTnpController.cs (solo el método Health)
[HttpGet("health")]
[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
[ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
public async Task<IActionResult> Health([FromServices] IHealthService healthService, CancellationToken ct)
{
    var dto = await healthService.CheckAsync(ct);
    // Devuelve el cuerpo estándar de health y el HTTP correspondiente
    return StatusCode((int)dto.HttpStatus, dto);

}


// Program.cs (fragmento)
builder.Services.AddScoped<IHealthService, HealthService>();

// Si aún no lo hiciste, configura el HttpClient "TNP"
// con BaseAddress/Timeout/Polly si aplica.
builder.Services.AddHttpClient("TNP", http =>
{
    // http.BaseAddress = new Uri("https://tercero-tnp.ejemplo/api/v1/"); // si prefieres base
    http.Timeout = TimeSpan.FromSeconds(30);
});

