// Models/Dtos/GetAuthorizationManual/AuthorizationServiceResult.cs
using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Resultado combinado del servicio: salida normalizada + envelope crudo (si hubo 200).
/// </summary>
public sealed class AuthorizationServiceResult
{
    /// <summary>Salida estándar (5 campos) que usa el header y el mapeo de HTTP.</summary>
    public ResponseAuthorizationManualDto Normalized { get; set; } = new();

    /// <summary>
    /// Envelope crudo del tercero **solo cuando HTTP 200**. Nulo en 4xx/5xx.
    /// </summary>
    public TnpAuthorizationEnvelope? RawTnpEnvelope { get; set; }
}




// Services/Interfaces/IPaymentAuthorizationService.cs
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

namespace Pagos_Davivienda_TNP.Services.Interfaces;

public interface IPaymentAuthorizationService
{
    /// <summary>Autorización manual hacia TNP.</summary>
    Task<AuthorizationServiceResult> AuthorizeManualAsync(AuthorizationBody request, CancellationToken ct = default);
}




// Dentro de PaymentAuthorizationService.AuthorizeManualAsync(...)
using System.Text.Encodings.Web; // para Encoder (acentos sin escapes)

private static readonly JsonSerializerOptions StjWriteOptions = new()
{
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // ← acentos sin \u00F3
};

private static readonly JsonSerializerOptions StjReadOptions = new()
{
    PropertyNameCaseInsensitive = true
};

// ...

using var resp = await client.PostAsync(url, content, ct);
var status = resp.StatusCode;
var body   = resp.Content is null ? string.Empty : await resp.Content.ReadAsStringAsync(ct);

// --- HTTP != 2xx: devolvemos SOLO Normalized ---
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
            ResponseCode      = code,
            AuthorizationCode = string.Empty,
            TransactionId     = string.Empty,
            Message           = $"TNP respondió {(int)status} {status}: {snippet}",
            Timestamp         = TimeUtil.IsoNowUtc()
        },
        RawTnpEnvelope = null
    };
}

// --- HTTP 2xx, sin cuerpo: tratar como pasarela defectuosa (solo Normalized) ---
if (string.IsNullOrWhiteSpace(body))
{
    return new AuthorizationServiceResult
    {
        Normalized = new ResponseAuthorizationManualDto
        {
            ResponseCode      = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway), // "96"
            AuthorizationCode = string.Empty,
            TransactionId     = string.Empty,
            Message           = "TNP devolvió una respuesta vacía.",
            Timestamp         = TimeUtil.IsoNowUtc()
        },
        RawTnpEnvelope = null
    };
}

// --- HTTP 2xx, con cuerpo: intentamos TNP envelope oficial ---
if (SafeDeserializeStj<TnpAuthorizationEnvelope>(body, out var tnpEnv) &&
    tnpEnv?.GetAuthorizationManualResponse?.GetAuthorizationManualResult is TnpAuthorizationResult ok)
{
    return new AuthorizationServiceResult
    {
        Normalized = TnpAuthorizationMapper.FromSuccess(ok), // llena 5 campos
        RawTnpEnvelope = tnpEnv                              // ← guardamos el CRUDO
    };
}

// (Fallback) si alguna vez devolvieran el DTO directo (poco probable)
if (SafeDeserializeStj<ResponseAuthorizationManualDto>(body, out var dto) && dto is not null)
{
    return new AuthorizationServiceResult
    {
        Normalized = dto,
        RawTnpEnvelope = null
    };
}

// 2xx pero formato inesperado: error de pasarela (solo Normalized)
return new AuthorizationServiceResult
{
    Normalized = new ResponseAuthorizationManualDto
    {
        ResponseCode      = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway),
        AuthorizationCode = string.Empty,
        TransactionId     = string.Empty,
        Message           = "No se pudo interpretar la respuesta del TNP.",
        Timestamp         = TimeUtil.IsoNowUtc()
    },
    RawTnpEnvelope = null
};



using System.Text.Encodings.Web;
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping; // ← acentos OK
});

[HttpPost("Authorization/")]
public async Task<IActionResult> GetAuthorizationManual([FromBody] AuthorizationRequest request, CancellationToken ct)
{
    var sw = Stopwatch.StartNew();

    var input = request.Body.GetAuthorizationManual;

    // Llamas al servicio y recibes Normalized + Raw
    var svc = await _paymentService.AuthorizeManualAsync(new AuthorizationBody
    {
        GetAuthorizationManual =
        {
            PMerchantID = input.PMerchantID,
            PTerminalID = input.PTerminalID,
            PPrimaryAccountNumber = input.PPrimaryAccountNumber,
            PDateExpiration = input.PDateExpiration,
            PCVV2 = input.PCVV2,
            PAmount = input.PAmount,
            PSystemsTraceAuditNumber = input.PSystemsTraceAuditNumber
        }
    }, ct);

    sw.Stop();

    // Header estándar tomando el Normalized
    var header = new ResponseHeader
    {
        ResponseId     = Guid.NewGuid().ToString("N"),
        Timestamp      = DateTime.UtcNow.ToString("o"),
        ProcessingTime = $"{sw.ElapsedMilliseconds}ms",
        StatusCode     = svc.Normalized.ResponseCode,
        Message        = svc.Normalized.Message,
        RequestHeader  = request.Header
    };

    object data; // puede ser el TNP crudo o tu envelope interno

    if (svc.Normalized.ResponseCode == "00" && svc.RawTnpEnvelope is not null)
    {
        // Éxito de negocio → reenviar el envelope COMPLETO del tercero
        data = svc.RawTnpEnvelope;
    }
    else
    {
        // Cualquier otro caso → tu envelope ESTÁNDAR minimalista (5 campos)
        data = new GetAuthorizationManualResultEnvelope
        {
            GetAuthorizationManualResponse = new GetAuthorizationManualResponseContainer
            {
                GetAuthorizationManualResult = svc.Normalized
            }
        };
    }

    var apiResponse = new ResponseModel<object> // ← usa object para alternar shapes
    {
        Header = header,
        Data   = data
    };

    var http = ErrorCodeMapper.ToHttpStatus(svc.Normalized.ResponseCode);
    return StatusCode((int)http, apiResponse);
}


[JsonPropertyName("AuthorizationIdentificationResponse")]
public string? AuthorizationIdentificationResponse { get; set; }





AuthorizationCode = src.AuthorizationCode 
                    ?? src.AuthorizationIdentificationResponse 
                    ?? string.Empty,











