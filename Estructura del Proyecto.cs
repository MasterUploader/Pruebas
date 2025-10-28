using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos;

/// <summary>
/// Encabezado estándar de la respuesta.
/// </summary>
public sealed class ResponseHeader
{
    /// <summary>Identificador único de la respuesta.</summary>
    [JsonPropertyName("responseId")]
    public string ResponseId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Marca de tiempo en UTC (ISO 8601).</summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

    /// <summary>Tiempo total de procesamiento del request.</summary>
    [JsonPropertyName("processingTime")]
    public string ProcessingTime { get; set; } = string.Empty;

    /// <summary>Código de estado propio del negocio (p.ej. "00").</summary>
    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;

    /// <summary>Mensaje legible para el consumidor.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Header original del request, eco para trazabilidad.</summary>
    [JsonPropertyName("requestHeader")]
    public RequestHeader RequestHeader { get; set; } = new();
}

/// <summary>
/// Contenedor genérico de respuesta: header + data.
/// </summary>
/// <typeparam name="TData">Tipo del payload de datos.</typeparam>
public sealed class ResponseModel<TData>
{
    [JsonPropertyName("header")]
    public ResponseHeader Header { get; set; } = new();

    [JsonPropertyName("data")]
    public TData? Data { get; set; }
}




using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pagos_Davivienda_TNP.Models.Dtos;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Services.Interfaces;

namespace Pagos_Davivienda_TNP.Controllers;

/// <summary>
/// API de Pagos DaviviendaTNP (v1).
/// Base URL: /davivienda-tnp/api/v1
/// </summary>
[ApiController]
[Route("davivienda-tnp/api/v1")]
[Produces("application/json")]
public class PagosDaviviendaTnpController(IPaymentAuthorizationService paymentService) : ControllerBase
{
    private readonly IPaymentAuthorizationService _paymentService = paymentService;

    /// <summary>Verifica salud del servicio.</summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
        => Ok(new { status = "UP", service = "DaviviendaTNP Payment API" });

    /// <summary>Procesa una autorización manual.</summary>
    /// <param name="request">Request con header + body.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("authorization/manual")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ResponseModel<GetAuthorizationManualResultEnvelope>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuthorizationManual([FromBody] AuthorizationRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // 1) Extraer el payload del cuerpo
        var input = request.Body.GetAuthorizationManual;

        // 2) Invocar servicio de dominio (que a su vez llama al tercero)
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

        // 3) Envolver en el contrato de datos solicitado
        var envelope = new GetAuthorizationManualResultEnvelope
        {
            Response = new GetAuthorizationManualResponseContainer { Result = result }
        };

        sw.Stop();

        // 4) Construir RESPUESTA FINAL: header + data
        var apiResponse = new ResponseModel<GetAuthorizationManualResultEnvelope>
        {
            Header = new ResponseHeader
            {
                ResponseId    = Guid.NewGuid().ToString("N"),
                Timestamp     = DateTime.UtcNow.ToString("o"),
                ProcessingTime= $"{sw.ElapsedMilliseconds}ms",
                StatusCode    = result.ResponseCode, // "00" si aprobada
                Message       = result.Message,
                RequestHeader = request.Header
            },
            Data = envelope
        };

        return Ok(apiResponse);
    }
}



builder.Services
    .AddControllers(o => o.Filters.Add<ModelStateToErrorResponseFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null; // respeta nombres exactos
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });




