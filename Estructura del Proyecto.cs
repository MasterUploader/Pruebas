Este es el controlador mejoralo:

using Microsoft.AspNetCore.Mvc;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.RegistraImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.RegistraImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;


namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers;

/// <summary>
/// Controlador que contiene los endpoints correspondientes a Registro de Impresión.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class RegistraImpresionController(IRegistraImpresionService _registraImpresionService) : ControllerBase
{
    /// <inheritdoc />
    protected PostRegistraImpresionResponseDto _postRegistraImpresionResponseDto = new();
    private readonly ResponseHandler _responseHandler = new();

    /// <summary>
    /// Endpoint encargado de registrar una tarjeta impresa.
    /// </summary>
    /// <param name="postRegistraImpresionDto"></param>
    /// <returns>Retorna respuesta Http con el resultado.</returns>
    [HttpPost]
    [Route("RegistraImpresion")]
    public async Task<IActionResult> RegistraImpresion([FromBody] PostRegistraImpresionDto postRegistraImpresionDto)
    {
        try
        {
            bool exito = await _registraImpresionService.RegistraImpresion(postRegistraImpresionDto);

            if (exito)
            {
                _postRegistraImpresionResponseDto.ImpresionExitosa = exito;
                _postRegistraImpresionResponseDto.Codigo.Message = "Se Registro Correctamente el valor";
                _postRegistraImpresionResponseDto.Codigo.Error = "200";
                _postRegistraImpresionResponseDto.Codigo.Status = "success";
                _postRegistraImpresionResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);
            }
            else
            {
                _postRegistraImpresionResponseDto.ImpresionExitosa = exito;
                _postRegistraImpresionResponseDto.Codigo.Message = "No se Pudo Ingresar Fecha, Hora y Usuario de Imprime";
                _postRegistraImpresionResponseDto.Codigo.Error = "304";
                _postRegistraImpresionResponseDto.Codigo.Status = "NotModified";
                _postRegistraImpresionResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);
                
            }
            return _responseHandler.HandleResponse(_postRegistraImpresionResponseDto, _postRegistraImpresionResponseDto.Codigo.Status);
        }
        catch (Exception ex)
        {
            _postRegistraImpresionResponseDto.ImpresionExitosa = false;
            _postRegistraImpresionResponseDto.Codigo.Message = ex.Message;
            _postRegistraImpresionResponseDto.Codigo.Error = "400";
            _postRegistraImpresionResponseDto.Codigo.Status = "BadRequest";
            _postRegistraImpresionResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

            return _responseHandler.HandleResponse(_postRegistraImpresionResponseDto, _postRegistraImpresionResponseDto.Codigo.Status);
        }
    }    
}
