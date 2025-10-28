El response quedaria algo así

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
[Route("api/v1/davivienda-tnp/[controller]")]
[Produces("application/json")]
public class PagosDaviviendaTnpController(IPaymentAuthorizationService paymentService) : ControllerBase
{
    private readonly IPaymentAuthorizationService _paymentService = paymentService;

    /// <summary>Verifica salud del servicio.</summary>
    /// <returns>Estado UP y nombre de servicio.</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        var response = new
        {
            status = "UP",
            service = "DaviviendaTNP Payment API"
        };
        return Ok(response);
    }

    /// <summary>Procesa una autorización manual.</summary>
    /// <param name="request">Payload de autorización manual.</param>
    /// <returns>Envoltura GetAuthorizationManualResponse/GetAuthorizationManualResult.</returns>
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

        var response = new ResponseModels
        {
            Header = new ResponseHeader
            {
                //Faltan datos de header reales
                ReponseId = "00"
            },
            Data = envelope
        };

        

        return Ok(response);
    }

}



using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos;

public class ResponseHeader
{
    [JsonPropertyName("responseId")]
    public string ReponseId { get; set; } = Guid.NewGuid().ToString();


    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");


    [JsonPropertyName("processingtime")]
    public string ProcessingTime { get; set; } = string.Empty;


    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = string.Empty;


    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;


    [JsonPropertyName("requestheader")]
    public RequestHeader RequestHeader { get; set; } = new();
}




using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos;

public class ResponseModels
{
    [JsonPropertyName("header")]
    public ResponseHeader Header { get; set; } = new();


    [JsonPropertyName("data")]
    public object Data { get; set; } = new();
}

