using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Pagos_Davivienda_TNP.Models.Dtos;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Request raíz: encabezado + cuerpo de autorización manual.
/// </summary>
public sealed class AuthorizationRequest
{
    /// <summary>Metadatos del request (trazabilidad/canal/usuario).</summary>
    [Required]
    [JsonPropertyName("header")]
    public RequestHeader Header { get; set; } = new();

    /// <summary>Contenido del request (parámetros del negocio).</summary>
    [Required]
    [JsonPropertyName("body")]
    public AuthorizationBody Body { get; set; } = new();
}




using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos;

/// <summary>Encabezado estándar para trazabilidad y control.</summary>
public sealed class RequestHeader
{
    [Required, JsonPropertyName("h-request-id")]
    public string HRequestId { get; set; } = string.Empty;

    [Required, JsonPropertyName("h-channel")]
    public string HChannel { get; set; } = string.Empty;

    [Required, JsonPropertyName("h-terminal")]
    public string HTerminal { get; set; } = string.Empty;

    [Required, JsonPropertyName("h-organization")]
    public string HOrganization { get; set; } = string.Empty;

    [Required, JsonPropertyName("h-user-id")]
    public string HUserId { get; set; } = string.Empty;

    [Required, JsonPropertyName("h-provider")]
    public string HProvider { get; set; } = string.Empty;

    [JsonPropertyName("h-session-id")]
    public string HSessionId { get; set; } = Guid.NewGuid().ToString("N");

    [Required, JsonPropertyName("h-client-ip")]
    public string HClientIp { get; set; } = string.Empty;

    /// <summary>ISO 8601 UTC (ej.: 2025-10-28T12:34:56Z).</summary>
    [Required, JsonPropertyName("h-timestamp")]
    public string HTimestamp { get; set; } = string.Empty;
}




using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>Cuerpo del request para autorización manual.</summary>
public sealed class AuthorizationBody
{
    /// <summary>
    /// Envoltura de negocio requerida por el contrato.
    /// Si el tercero NO la usa, elimina este nivel y mueve las props a AuthorizationBody.
    /// </summary>
    [Required]
    [JsonPropertyName("GetAuthorizationManual")]
    public AuthorizationPayload GetAuthorizationManual { get; set; } = new();
}

/// <summary>Parámetros de la autorización manual.</summary>
public sealed class AuthorizationPayload
{
    [Required, JsonPropertyName("pMerchantID")]
    public string PMerchantID { get; set; } = string.Empty;

    [Required, JsonPropertyName("pTerminalID")]
    public string PTerminalID { get; set; } = string.Empty;

    [Required, JsonPropertyName("pPrimaryAccountNumber")]
    public string PPrimaryAccountNumber { get; set; } = string.Empty;

    /// <summary>MMAA</summary>
    [Required, JsonPropertyName("pDateExpiration")]
    public string PDateExpiration { get; set; } = string.Empty;

    [Required, JsonPropertyName("pCVV2")]
    public string PCVV2 { get; set; } = string.Empty;

    /// <summary>Entero positivo como string (ej. "10000").</summary>
    [Required, JsonPropertyName("pAmount")]
    public string PAmount { get; set; } = string.Empty;

    /// <summary>STAN de 6 dígitos.</summary>
    [Required, JsonPropertyName("pSystemsTraceAuditNumber")]
    public string PSystemsTraceAuditNumber { get; set; } = string.Empty;
}



[HttpPost("authorization/manual")]
[Consumes("application/json")]
public async Task<IActionResult> GetAuthorizationManual([FromBody] AuthorizationRequest request, CancellationToken ct)
{
    // request.Header → metadatos (si necesitas log/trazabilidad)
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
        Response = new GetAuthorizationManualResponseContainer { Result = result }
    };

    return Ok(envelope);
}



builder.Services
    .AddControllers(o => o.Filters.Add<ModelStateToErrorResponseFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null; // respeta nombres exactos (con guiones via JsonPropertyName)
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);









