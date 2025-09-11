using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.DetalleTarjetaImprimir;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.DetalleTarjetasImprimir;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using System.Net.Mime;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers;

/// <summary>
/// Endpoints para obtener el detalle de tarjetas pendientes por imprimir.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class DetalleTarjetasImprimirController : ControllerBase
{
    private readonly IDetalleTarjetasImprimirServices _detalleTarjetaImprimirService;
    private readonly ILogger<DetalleTarjetasImprimirController> _logger;
    private readonly ResponseHandler _responseHandler = new();

    /// <summary>
    /// Crea una nueva instancia del controlador.
    /// </summary>
    public DetalleTarjetasImprimirController(
        IDetalleTarjetasImprimirServices detalleTarjetaImprimirService,
        ILogger<DetalleTarjetasImprimirController> logger)
    {
        _detalleTarjetaImprimirService = detalleTarjetaImprimirService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la lista de tarjetas pendientes por imprimir para una combinación de
    /// código de agencia de apertura y código de agencia de impresión.
    /// </summary>
    /// <param name="getDetalleTarjetaImprimirDto">Parámetros de filtro (query string).</param>
    /// <returns>Respuesta HTTP con el resultado del servicio.</returns>
    /// <remarks>
    /// Rutas soportadas:
    /// - <c>GET api/DetalleTarjetasImprimir</c>
    /// - <c>GET api/DetalleTarjetasImprimir/GetTarjetas</c> (alias por compatibilidad)
    /// </remarks>
    [HttpGet]
    [HttpGet("GetTarjetas")] // alias para compatibilidad con clientes existentes
    [ProducesResponseType(typeof(GetDetallesTarjetasImprimirResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GetDetallesTarjetasImprimirResponseDto), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(GetDetallesTarjetasImprimirResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTarjetas([FromQuery] GetDetalleTarjetaImprimirDto getDetalleTarjetaImprimirDto)
    {
        // Validación temprana del modelo (aprovechando [ApiController], pero devolvemos mensaje consistente).
        if (!ModelState.IsValid)
        {
            var msg = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var bad = BuildError("BadRequest", "400",
                string.IsNullOrWhiteSpace(msg) ? "Solicitud inválida." : msg);

            return _responseHandler.HandleResponse(bad, bad.Codigo.Status);
        }

        try
        {
            _logger.LogInformation("Consultando tarjetas a imprimir. Filtros: {@Filtros}", getDetalleTarjetaImprimirDto);

            var resp = await _detalleTarjetaImprimirService.GetTarjetas(getDetalleTarjetaImprimirDto);

            // Si el servicio retorna null, devolvemos una respuesta consistente (evita NRE en el handler).
            if (resp is null)
            {
                var noContent = BuildError("NoContent", "204", "Sin resultados para los parámetros enviados.");
                return _responseHandler.HandleResponse(noContent, noContent.Codigo.Status);
            }

            // El ResponseHandler usará el Status que venga desde el servicio (OK/NoContent/etc.).
            return _responseHandler.HandleResponse(resp, resp.Codigo.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tarjetas a imprimir. Filtros: {@Filtros}", getDetalleTarjetaImprimirDto);

            var bad = BuildError("BadRequest", "400", ex.Message);
            return _responseHandler.HandleResponse(bad, bad.Codigo.Status);
        }
    }

    /// <summary>
    /// Crea un DTO de respuesta de error con metadatos consistentes.
    /// </summary>
    private static GetDetallesTarjetasImprimirResponseDto BuildError(string status, string code, string message)
        => new GetDetallesTarjetasImprimirResponseDto
        {
            // Propiedades de datos quedan con sus valores por defecto.
            Codigo = new GetDetallesTarjetasImprimirResponseDto.CodigoRespuesta
            {
                Status = status,
                Error = code,
                Message = message,
                TimeStamp = DateTime.Now.ToString("HH:mm:ss tt")
            }
        };
}
