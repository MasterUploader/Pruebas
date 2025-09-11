Ahora mejora este controlador:
using Microsoft.AspNetCore.Mvc;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.DetalleTarjetaImprimir;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.DetalleTarjetasImprimir;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers;

/// <summary>
/// Controlador Que contiene los endpoints de DetalleTarjetasImprimir.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class DetalleTarjetasImprimirController(IDetalleTarjetasImprimirServices _detalleTarjetaImprimirService) : ControllerBase
{
    /// <inheritdoc />
    protected GetDetallesTarjetasImprimirResponseDto _getDetalleTarjetaImprimirResponseDto = new();

    private readonly ResponseHandler _responseHandler = new();

    /// <summary>
    /// Endpoint GetTarjetas, obtiene la lista de tarjetas pendientes por imprimir correspondientes a una combinación de código de agencia de apertura y Código de agencia de impresión.
    /// </summary>
    /// <param name="getDetalleTarjetaImprimirDto"></param>
    /// <returns>Retorna respuesta HTTP con cuerpo</returns>
    [HttpGet]
    [Route("GetTarjetas")]
    public async Task<IActionResult> GetTarjetas([FromQuery] GetDetalleTarjetaImprimirDto getDetalleTarjetaImprimirDto)
    {
        _getDetalleTarjetaImprimirResponseDto = await _detalleTarjetaImprimirService.GetTarjetas(getDetalleTarjetaImprimirDto);
        
       return _responseHandler.HandleResponse(_getDetalleTarjetaImprimirResponseDto, _getDetalleTarjetaImprimirResponseDto.Codigo.Status);
    }
}
