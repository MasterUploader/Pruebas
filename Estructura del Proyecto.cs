Actualiza estos metodos para que usen RestYtilities.QueryBuilder, y actualiza el controlador.

    using Microsoft.AspNetCore.Mvc;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.ValidaImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.ValidaImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using System.Data.OleDb;


namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers;

/// <summary>
/// Controlador que contiene los Endpoints de Validación de la impresión.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ValidaImpresionController (IValidaImpresionService _validaImpresion) : ControllerBase
{
    /// <inheritdoc />
    protected GetValidaImpresionResponseDto _getValidaImpresionResponseDto = new();

    private readonly ResponseHandler _responseHandler = new();
 
    /// <summary>
    /// Endpoint que valida la impresión.
    /// </summary>
    /// <param name="getValidaImpresionDto">Objeto Dto</param>
    /// <returns>Retorna una respuesta Http</returns>
    [HttpGet]
    [Route("ValidaImpresion")]
    public async Task<IActionResult> ValidaImpresion([FromQuery] GetValidaImpresionDto getValidaImpresionDto)
    {
        await Task.Delay(1);
        try
        {
            var respuesta = _validaImpresion.ValidaImpresion(getValidaImpresionDto);

            return _responseHandler.HandleResponse(respuesta, respuesta.Codigo.Status);
        }
        catch (Exception ex)
        {
            GetValidaImpresionResponseDto getValidaImpresionResponseDto = new();

            getValidaImpresionResponseDto.Imprime = true;
            getValidaImpresionResponseDto.Codigo.Message = ex.Message;
            getValidaImpresionResponseDto.Codigo.Status = "BadRequest";
            getValidaImpresionResponseDto.Codigo.Error = "400";
            getValidaImpresionResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

            return _responseHandler.HandleResponse(getValidaImpresionResponseDto, getValidaImpresionResponseDto.Codigo.Status);
        }
    }
}

using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.ValidaImpresion;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.ValidaImpresion;

/// <summary>
/// Interfaz IValidaImpresion
/// </summary>
public interface IValidaImpresionService
{
    /// <summary>
    /// Método encargado de Validar si la impresión se realizo correctamente.
    /// </summary>
    /// <param name="getValidaImpresionDto">Objeto Dto.</param>
    /// <returns>Retorna  objeto GetValidaImpresionResponseDto.</returns>
    GetValidaImpresionResponseDto ValidaImpresion(GetValidaImpresionDto getValidaImpresionDto);
}

using API_1_TERCEROS_REMESADORAS.Utilities;
using Connections.Abstractions;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.ValidaImpresion;
using System.Data.Common;
using System.Data.OleDb;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.ValidaImpresion;

/// <summary>
/// Clase de servicio ValidaImpresionService
/// </summary>
/// <param name="_connection">Instancia de IDatabaseConnection.</param>
/// <param name="_contextAccessor">Instancia de IHttpContextAccessor.</param>
public class ValidaImpresionService(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : IValidaImpresionService
{
    /// <inheritdoc />
    protected GetValidaImpresionResponseDto _getValidaImpresionResponseDto = new();

    /// <inheritdoc />
    public GetValidaImpresionResponseDto ValidaImpresion(GetValidaImpresionDto getValidaImpresionDto)
    {
        string codigoTarjeta = getValidaImpresionDto.CodigoTarjeta;

        return BusquedaUNI5400(codigoTarjeta);
    }

    /*Busca datos en tabla S38FILEBA.UNI5400*/
    private GetValidaImpresionResponseDto BusquedaUNI5400(string codigoTarjeta)
    {
        _connection.Open();
        FieldsQueryL param = new();

        //string sqlQuery = "SELECT * FROM S38FILEBA.UNI5400 WHERE ST_CODIGO_TARJETA  = ? OR ST_FECHA_IMPRESION > ? OR ST_HORA_IMPRESION > ? OR ST_USUARIO_IMPRESION != ? ORDER BY ST_CODIGO_TARJETA , ST_CENTRO_COSTO_IMPR_TARJETA , ST_CENTRO_COSTO_APERTURA , ST_FECHA_IMPRESION , ST_HORA_IMPRESION , ST_USUARIO_IMPRESION";
        string sqlQuery = "SELECT * FROM S38FILEBA.UNI5400 WHERE ST_CODIGO_TARJETA  = ? AND (ST_FECHA_IMPRESION > ? AND ST_HORA_IMPRESION > ? AND ST_USUARIO_IMPRESION != ? ) ORDER BY ST_CODIGO_TARJETA , ST_CENTRO_COSTO_IMPR_TARJETA , ST_CENTRO_COSTO_APERTURA , ST_FECHA_IMPRESION , ST_HORA_IMPRESION , ST_USUARIO_IMPRESION";
        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
        command.CommandText = sqlQuery;
        command.CommandType = System.Data.CommandType.Text;

        param.AddOleDbParameter(command, "ST_CODIGO_TARJETA", OleDbType.Char, codigoTarjeta);
        param.AddOleDbParameter(command, "ST_FECHA_IMPRESION", OleDbType.Numeric, 0);
        param.AddOleDbParameter(command, "ST_HORA_IMPRESION", OleDbType.Numeric, 0);
        param.AddOleDbParameter(command, "ST_USUARIO_IMPRESION", OleDbType.Char, "");

        using DbDataReader reader = command.ExecuteReader();

        if (reader.HasRows)
        {
            _getValidaImpresionResponseDto.Imprime = true;
            _getValidaImpresionResponseDto.Codigo.Message = "Tarjeta impresa";
            _getValidaImpresionResponseDto.Codigo.Status = "success";
            _getValidaImpresionResponseDto.Codigo.Error = "200";
            _getValidaImpresionResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

            return _getValidaImpresionResponseDto;
        }

        _getValidaImpresionResponseDto.Imprime = false;
        _getValidaImpresionResponseDto.Codigo.Message = "Tarjeta no impresa";
        _getValidaImpresionResponseDto.Codigo.Status = "success";
        _getValidaImpresionResponseDto.Codigo.Error = "200";
        _getValidaImpresionResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

        return _getValidaImpresionResponseDto;
    }
}
