// Models/Dtos/GetAuthorizationManual/AuthorizationServiceResult.cs
using System.Net;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

public sealed class AuthorizationServiceResult
{
    /// <summary>Salida normalizada (5 campos) que tu header/consumidores esperan.</summary>
    public ResponseAuthorizationManualDto Normalized { get; set; } = new();

    /// <summary>Envelope crudo del tercero cuando HTTP 200; null de lo contrario.</summary>
    public TnpAuthorizationEnvelope? RawTnpEnvelope { get; set; }

    /// <summary>Código HTTP devuelto por el tercero.</summary>
    public HttpStatusCode UpstreamStatusCode { get; set; }

    /// <summary>Conveniencia: true si el tercero respondió 2xx.</summary>
    public bool UpstreamWasSuccess => ((int)UpstreamStatusCode >= 200 && (int)UpstreamStatusCode <= 299);
}




// Models/Dtos/GetAuthorizationManual/TnpAuthorizationResult.cs
using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

public sealed class TnpAuthorizationEnvelope
{
    [JsonPropertyName("GetAuthorizationManualResponse")]
    public TnpAuthorizationResponse? GetAuthorizationManualResponse { get; set; }
}

public sealed class TnpAuthorizationResponse
{
    [JsonPropertyName("GetAuthorizationManualResult")]
    public TnpAuthorizationResult? GetAuthorizationManualResult { get; set; }
}

public sealed class TnpAuthorizationResult
{
    [JsonPropertyName("ResponseCodeDescription")] public string? ResponseCodeDescription { get; set; }
    [JsonPropertyName("ResponseCode")]           public string? ResponseCode { get; set; }

    // Aprobadas suelen traer este nombre:
    [JsonPropertyName("AuthorizationIdentificationResponse")] 
    public string? AuthorizationIdentificationResponse { get; set; }

    // Algunas integraciones usan este:
    [JsonPropertyName("AuthorizationCode")]      public string? AuthorizationCode { get; set; }

    [JsonPropertyName("RetrievalReferenceNumber")] public string? RetrievalReferenceNumber { get; set; }
    [JsonPropertyName("SystemsTraceAuditNumber")]  public string? SystemsTraceAuditNumber { get; set; }
    [JsonPropertyName("TransactionType")]          public string? TransactionType { get; set; }
    [JsonPropertyName("TimeLocalTrans")]           public string? TimeLocalTrans { get; set; }
    [JsonPropertyName("DateLocalTrans")]           public string? DateLocalTrans { get; set; }
    [JsonPropertyName("Amount")]                   public string? Amount { get; set; }
    [JsonPropertyName("MerchantID")]               public string? MerchantID { get; set; }
    [JsonPropertyName("MCC")]                      public string? MCC { get; set; }
    [JsonPropertyName("CurrencyCode")]             public string? CurrencyCode { get; set; }
    [JsonPropertyName("PrimaryAccountNumber")]     public string? PrimaryAccountNumber { get; set; }
    [JsonPropertyName("TerminalID")]               public string? TerminalID { get; set; }
}




// Models/Dtos/GetAuthorizationManual/TnpAuthorizationMapper.cs
using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

public static class TnpAuthorizationMapper
{
    public static ResponseAuthorizationManualDto FromSuccess(TnpAuthorizationResult src)
    {
        // Preferimos el RRN; si no viene, STAN
        var txnId = !string.IsNullOrWhiteSpace(src.RetrievalReferenceNumber)
            ? src.RetrievalReferenceNumber!
            : (src.SystemsTraceAuditNumber ?? string.Empty);

        // Authorization: usa el que venga
        var auth = !string.IsNullOrWhiteSpace(src.AuthorizationCode)
            ? src.AuthorizationCode!
            : (src.AuthorizationIdentificationResponse ?? string.Empty);

        return new ResponseAuthorizationManualDto
        {
            ResponseCode      = src.ResponseCode ?? string.Empty,
            AuthorizationCode = auth,
            TransactionId     = txnId,
            Message           = src.ResponseCodeDescription ?? string.Empty,
            Timestamp         = TimeUtil.IsoNowUtc()
        };
    }

    public static ResponseAuthorizationManualDto FromError(TnpErrorResponse err, string fallbackResponseCode)
        => new()
        {
            ResponseCode      = fallbackResponseCode,
            AuthorizationCode = string.Empty,
            TransactionId     = string.Empty,
            Message           = err.Error ?? "Error no especificado por TNP.",
            Timestamp         = TimeUtil.IsoNowUtc()
        };
}



// Dentro de AuthorizeManualAsync(...)

// Tras hacer PostAsync:
var status = resp.StatusCode;
var body   = resp.Content is null ? string.Empty : await resp.Content.ReadAsStringAsync(ct);

// HTTP != 2xx
if (!resp.IsSuccessStatusCode)
{
    var code = ErrorCodeMapper.FromHttpStatus(status);

    if (SafeDeserializeStj<TnpErrorResponse>(body, out var tnpErr) && tnpErr is not null)
    {
        return new AuthorizationServiceResult
        {
            Normalized         = TnpAuthorizationMapper.FromError(tnpErr, code),
            RawTnpEnvelope     = null,
            UpstreamStatusCode = status
        };
    }

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
        RawTnpEnvelope     = null,
        UpstreamStatusCode = status
    };
}

// 2xx sin cuerpo
if (string.IsNullOrWhiteSpace(body))
{
    return new AuthorizationServiceResult
    {
        Normalized = new ResponseAuthorizationManualDto
        {
            ResponseCode      = ErrorCodeMapper.FromHttpStatus(HttpStatusCode.BadGateway),
            AuthorizationCode = string.Empty,
            TransactionId     = string.Empty,
            Message           = "TNP devolvió una respuesta vacía.",
            Timestamp         = TimeUtil.IsoNowUtc()
        },
        RawTnpEnvelope     = null,
        UpstreamStatusCode = status
    };
}

// 2xx con cuerpo → intenta envelope
if (SafeDeserializeStj<TnpAuthorizationEnvelope>(body, out var tnpEnv) &&
    tnpEnv?.GetAuthorizationManualResponse?.GetAuthorizationManualResult is TnpAuthorizationResult ok)
{
    return new AuthorizationServiceResult
    {
        Normalized         = TnpAuthorizationMapper.FromSuccess(ok),
        RawTnpEnvelope     = tnpEnv,    // ← preserva el crudo SIEMPRE que 200
        UpstreamStatusCode = status
    };
}

// fallback DTO directo…
if (SafeDeserializeStj<ResponseAuthorizationManualDto>(body, out var dto) && dto is not null)
{
    return new AuthorizationServiceResult
    {
        Normalized         = dto,
        RawTnpEnvelope     = null,
        UpstreamStatusCode = status
    };
}

// 2xx pero no interpretable
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
    RawTnpEnvelope     = null,
    UpstreamStatusCode = status
};





// Política de HTTP: escoge una
const bool PreserveUpstream2xx = true; // B: si upstream fue 200 → devuelves 200
// const bool PreserveUpstream2xx = false; // A: mapeas por responseCode (94→409)

[HttpPost("Authorization/")]
public async Task<IActionResult> GetAuthorizationManual([FromBody] AuthorizationRequest request, CancellationToken ct)
{
    var sw = Stopwatch.StartNew();

    var input = request.Body.GetAuthorizationManual;

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

    var header = new ResponseHeader
    {
        ResponseId     = Guid.NewGuid().ToString("N"),
        Timestamp      = DateTime.UtcNow.ToString("o"),
        ProcessingTime = $"{sw.ElapsedMilliseconds}ms",
        StatusCode     = svc.Normalized.ResponseCode,                // "00", "94", etc.
        Message        = svc.Normalized.Message,                     // usa descripción TNP si llegó
        RequestHeader  = request.Header
    };

    object data;

    // Si el upstream fue 200, reenvía el envelope COMPLETO del tercero (pediste “formato completo”)
    if (svc.UpstreamWasSuccess && svc.RawTnpEnvelope is not null)
    {
        data = svc.RawTnpEnvelope; // ← TODOS los campos del TNP
    }
    else
    {
        // Caso 4xx/5xx (o no interpretable): usa envelope minimalista (5 campos)
        data = new GetAuthorizationManualResultEnvelope
        {
            GetAuthorizationManualResponse = new GetAuthorizationManualResponseContainer
            {
                GetAuthorizationManualResult = svc.Normalized
            }
        };
    }

    var apiResponse = new ResponseModel<object>
    {
        Header = header,
        Data   = data
    };

    // Selección de StatusCode de salida
    var http = PreserveUpstream2xx
        ? (svc.UpstreamWasSuccess ? HttpStatusCode.OK : ErrorCodeMapper.ToHttpStatus(svc.Normalized.ResponseCode))
        : ErrorCodeMapper.ToHttpStatus(svc.Normalized.ResponseCode);

    return StatusCode((int)http, apiResponse);
}
