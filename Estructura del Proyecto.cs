A esta clase le hacen falta modificaciones, agragalas:

using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Utils;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Services;

/// <summary>
/// Servicio que consume el endpoint externo de TNP para la **autorización manual** de pagos.
/// <para>
/// - Usa <see cref="IHttpClientFactory"/> con el cliente nombrado <c>"TNP"</c> para
///   conservar handlers de logging y configuración centralizada.
/// - Serializa y deserializa con <b>System.Text.Json</b> respetando los nombres
///   exactos exigidos por el tercero (atributos <see cref="JsonPropertyNameAttribute"/>).
/// - Nunca lanza excepciones hacia el controlador: ante errores del tercero o de red,
///   retorna un <see cref="ResponseAuthorizationManualDto"/> con <c>responseCode</c> ≠ "00".
/// </para>
/// </summary>
public sealed class PaymentAuthorizationService(IHttpClientFactory httpClientFactory)
    : Interfaces.IPaymentAuthorizationService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    #region System.Text.Json options compartidas

    /// <summary>
    /// Opciones de escritura: sin políticas de cambio de nombre y sin ignorar nulos,
    /// para respetar exactamente los alias definidos con <c>[JsonPropertyName]</c>.
    /// </summary>
    private static readonly JsonSerializerOptions StjWriteOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // ← acentos sin \u00F3
    };

    /// <summary>
    /// Opciones de lectura: <c>case-insensitive</c> para tolerar mayúsculas/minúsculas
    /// en respuestas del tercero.
    /// </summary>
    private static readonly JsonSerializerOptions StjReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #endregion

    /// <summary>
    /// Invoca al endpoint <c>/authorization/manual</c> del proveedor TNP.
    /// </summary>
    /// <param name="request">
    /// Cuerpo de la petición con la raíz <c>GetAuthorizationManual</c> y las propiedades solicitadas por el tercero.
    /// </param>
    /// <param name="ct">Token de cancelación cooperativa.</param>
    /// <returns>
    /// Un <see cref="ResponseAuthorizationManualDto"/> con:
    /// <list type="bullet">
    /// <item><description><c>responseCode="00"</c> y datos de autorización cuando la transacción es aprobada.</description></item>
    /// <item><description>Un código de negocio ≠ "00" cuando ocurre cualquier error (validación, red, timeout, 4xx/5xx, formato inesperado, etc.).</description></item>
    /// </list>
    /// </returns>
    public async Task<AuthorizationServiceResult> AuthorizeManualAsync(AuthorizationBody request, CancellationToken ct = default)
    {
        // 1) Construcción segura de URL base + ruta.
        var baseHost = (GlobalConnection.Current.Host ?? string.Empty).TrimEnd('/');
        var url = $"{baseHost}/authorization/manual";

        try
        {
            // 2) Cliente HTTP centralizado (con logging handler encadenado por DI).
            using var client = _httpClientFactory.CreateClient("TNP");

            // 3) Asegurar cabeceras comunes para JSON.
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 4) Serializar EXACTAMENTE el contrato que espera el tercero (root: GetAuthorizationManual).
            var json = JsonSerializer.Serialize(request, StjWriteOptions);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 5) Ejecutar la llamada HTTP (respetando cancelación).
            using var resp = await client.PostAsync(url, content, ct);

            // 6) Leer estado y cuerpo SIEMPRE para diagnóstico (aunque sea error).
            var status = resp.StatusCode;
            var body = resp.Content is null ? string.Empty : await resp.Content.ReadAsStringAsync(ct);

            // 7) // --- HTTP != 2xx: devolvemos SOLO Normalized ---
            if (!resp.IsSuccessStatusCode)
            {
                var code = ErrorCodeMapper.FromHttpStatus(status); // 400→"12", 504→"68", 5xx→"96", etc.

                // Intenta leer el error JSON del tercero: { "error": "...", "status": 400, "timestamp": ... }
                if (SafeDeserializeStj<TnpErrorResponse>(body, out var tnpErr) && tnpErr is not null)
                {
                    return new AuthorizationServiceResult
                    {
                        Normalized = TnpAuthorizationMapper.FromError(tnpErr, code),
                        RawTnpEnvelope = null
                    };
                }

                // Si no se pudo leer el error del tercero, armamos un mensaje genérico.
                var snippet = body is { Length: > 4096 } ? body[..4096] + "…(truncado)" : body;
                return new AuthorizationServiceResult
                {
                    Normalized = new ResponseAuthorizationManualDto
                    {
                        ResponseCode = code,
                        AuthorizationCode = string.Empty,
                        TransactionId = string.Empty,
                        Message = $"TNP respondió {(int)status} {status}: {snippet}",
                        Timestamp = TimeUtil.IsoNowUtc()
                    },
                    RawTnpEnvelope = null
                };
            }

            // 8) 2xx sin cuerpo
            if (string.IsNullOrWhiteSpace(body))
            {
                return new AuthorizationServiceResult
                {
                    Normalized = new ResponseAuthorizationManualDto
                    {
                        ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway), // "96"
                        AuthorizationCode = string.Empty,
                        TransactionId = string.Empty,
                        Message = "TNP devolvió una respuesta vacía.",
                        Timestamp = TimeUtil.IsoNowUtc()
                    },
                    RawTnpEnvelope = null
                };
            }

            // 9) Éxito 200: intentamos el envelope TNP (forma oficial)
            if (SafeDeserializeStj<TnpAuthorizationEnvelope>(body, out var tnpEnv) &&
    tnpEnv?.GetAuthorizationManualResponse?.GetAuthorizationManualResult is TnpAuthorizationResult ok)
            {
                return new AuthorizationServiceResult
                {
                    Normalized = TnpAuthorizationMapper.FromSuccess(ok), // llena 5 campos
                    RawTnpEnvelope = tnpEnv                              // ← guardamos el CRUDO
                };
            }

            // 10) (Fallback) si alguna vez el tercero retornara tu DTO directo (poco probable)
            if (SafeDeserializeStj<ResponseAuthorizationManualDto>(body, out var dto) && dto is not null)
            {
                return new AuthorizationServiceResult
                {
                    Normalized = dto,
                    RawTnpEnvelope = null
                };
            }

            // 11) 2xx pero formato inesperado
            return new AuthorizationServiceResult
            {
                Normalized = new ResponseAuthorizationManualDto
                {
                    ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway),
                    AuthorizationCode = string.Empty,
                    TransactionId = string.Empty,
                    Message = "No se pudo interpretar la respuesta del TNP.",
                    Timestamp = TimeUtil.IsoNowUtc()
                },
                RawTnpEnvelope = null
            };
            }
        // 12) Timeout del HttpClient (o cancelación por tiempo límite de Polly, etc.).
        catch (TaskCanceledException)
        {
            return new ResponseAuthorizationManualDto
            {
                ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.GatewayTimeout), // "68"
                AuthorizationCode = string.Empty,
                TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = "Timeout al invocar el servicio TNP.",
                Timestamp = TimeUtil.IsoNowUtc()
            };
        }
        // 13) Cancelación explícita iniciada aguas arriba (p. ej. ct.Cancel()).
        catch (OperationCanceledException)
        {
            return new ResponseAuthorizationManualDto
            {
                ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.GatewayTimeout), // "68"
                AuthorizationCode = string.Empty,
                TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = "La operación fue cancelada.",
                Timestamp = TimeUtil.IsoNowUtc()
            };
        }
        // 14) Errores de red/HTTP con StatusCode embebido (DNS, TLS, 502, etc.).
        catch (HttpRequestException ex)
        {
            var status = ex.StatusCode ?? HttpStatusCode.BadGateway; // fallback: 502
            return new ResponseAuthorizationManualDto
            {
                ResponseCode = ErrorCodeMapper.FromHttpStatus(status),
                AuthorizationCode = string.Empty,
                TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = $"Error al invocar el servicio TNP: {ex.Message}",
                Timestamp = TimeUtil.IsoNowUtc()
            };
        }
        // 15) Cualquier otra excepción no contemplada → falla genérica de pasarela.
        catch (Exception ex)
        {
            return new ResponseAuthorizationManualDto
            {
                ResponseCode = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway), // "96"
                AuthorizationCode = string.Empty,
                TransactionId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant(),
                Message = $"Error inesperado al invocar TNP: {ex.Message}",
                Timestamp = TimeUtil.IsoNowUtc()
            };
        }
    }

    /// <summary>
    /// Deserializa con <b>System.Text.Json</b> de manera segura (sin excepción);
    /// devuelve <c>true</c> cuando el parseo fue exitoso y <paramref name="value"/> no es <c>null</c>.
    /// </summary>
    /// <typeparam name="T">Tipo de destino.</typeparam>
    /// <param name="json">Texto JSON a deserializar.</param>
    /// <param name="value">Resultado deserializado o <c>default</c> si falla.</param>
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
