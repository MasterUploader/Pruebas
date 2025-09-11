using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.ValidaImpresion;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.ValidaImpresion
{
    /// <summary>
    /// Interfaz del servicio de validación de impresión.
    /// </summary>
    public interface IValidaImpresionService
    {
        /// <summary>
        /// Valida si la tarjeta ya fue impresa (consultando UNI5400).
        /// </summary>
        /// <param name="getValidaImpresionDto">Parámetros de validación.</param>
        /// <returns>DTO con el resultado de la validación.</returns>
        Task<GetValidaImpresionResponseDto> ValidaImpresionAsync(GetValidaImpresionDto getValidaImpresionDto);
    }
}


using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.ValidaImpresion;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using System.Data.Common;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.ValidaImpresion
{
    /// <summary>
    /// Servicio que valida si una tarjeta fue impresa consultando la tabla S38FILEBA.UNI5400.
    /// </summary>
    /// <remarks>
    /// Criterio: existe registro para ST_CODIGO_TARJETA con fecha/hora de impresión &gt; 0 y usuario no vacío.
    /// </remarks>
    public class ValidaImpresionService : IValidaImpresionService
    {
        private readonly IDatabaseConnection _connection;
        private readonly IHttpContextAccessor _contextAccessor;

        public ValidaImpresionService(IDatabaseConnection connection, IHttpContextAccessor contextAccessor)
        {
            _connection = connection;
            _contextAccessor = contextAccessor;
        }

        /// <inheritdoc />
        public async Task<GetValidaImpresionResponseDto> ValidaImpresionAsync(GetValidaImpresionDto getValidaImpresionDto)
        {
            var resp = new GetValidaImpresionResponseDto();

            // Validación básica
            var codigoTarjeta = (getValidaImpresionDto?.CodigoTarjeta ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codigoTarjeta))
            {
                resp.Imprime = false;
                resp.Codigo.Status = "BadRequest";
                resp.Codigo.Error = "400";
                resp.Codigo.Message = "El parámetro 'CodigoTarjeta' es obligatorio.";
                resp.Codigo.TimeStamp = DateTime.Now.ToString("HH:mm:ss tt");
                return resp;
            }

            _connection.Open();
            if (!_connection.IsConnected)
            {
                resp.Imprime = false;
                resp.Codigo.Status = "BadRequest";
                resp.Codigo.Error = "400";
                resp.Codigo.Message = "No hay conexión con la base de datos.";
                resp.Codigo.TimeStamp = DateTime.Now.ToString("HH:mm:ss tt");
                return resp;
            }

            // Construcción de SELECT con RestUtilities.QueryBuilder
            // WHERE:
            //   ST_CODIGO_TARJETA = <tarjeta>
            //   AND ST_FECHA_IMPRESION > 0
            //   AND ST_HORA_IMPRESION > 0
            //   AND COALESCE(ST_USUARIO_IMPRESION, '') <> ''
            // ORDER BY (para consistencia/determinismo)
            var qb = new SelectQueryBuilder("UNI5400", "S38FILEBA")
                .Select("ST_CODIGO_TARJETA", "ST_CENTRO_COSTO_IMPR_TARJETA", "ST_CENTRO_COSTO_APERTURA",
                        "ST_FECHA_IMPRESION", "ST_HORA_IMPRESION", "ST_USUARIO_IMPRESION")
                // Usamos SqlHelper.FormatValue para evitar inyección y formatear valores
                .WhereRaw($"ST_CODIGO_TARJETA = {SqlHelper.FormatValue(codigoTarjeta)}")
                .WhereRaw("ST_FECHA_IMPRESION > 0")
                .WhereRaw("ST_HORA_IMPRESION > 0")
                .WhereRaw("COALESCE(ST_USUARIO_IMPRESION, '') <> ''")
                .OrderBy(("ST_CODIGO_TARJETA", SortDirection.Asc),
                         ("ST_CENTRO_COSTO_IMPR_TARJETA", SortDirection.Asc),
                         ("ST_CENTRO_COSTO_APERTURA", SortDirection.Asc),
                         ("ST_FECHA_IMPRESION", SortDirection.Asc),
                         ("ST_HORA_IMPRESION", SortDirection.Asc),
                         ("ST_USUARIO_IMPRESION", SortDirection.Asc));

            var query = qb.Build();

            // Ejecutar
            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = query.Sql;               // Si usas parámetros en tu builder, puedes usar GetDbCommand(query, ctx)
            cmd.CommandType = System.Data.CommandType.Text;

            using DbDataReader reader = await cmd.ExecuteReaderAsync();

            if (reader.HasRows)
            {
                resp.Imprime = true;
                resp.Codigo.Status = "success";
                resp.Codigo.Error = "200";
                resp.Codigo.Message = "Tarjeta impresa";
                resp.Codigo.TimeStamp = DateTime.Now.ToString("HH:mm:ss tt");
                return resp;
            }

            resp.Imprime = false;
            resp.Codigo.Status = "success";
            resp.Codigo.Error = "200";
            resp.Codigo.Message = "Tarjeta no impresa";
            resp.Codigo.TimeStamp = DateTime.Now.ToString("HH:mm:ss tt");
            return resp;
        }
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.ValidaImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.ValidaImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using System.Net.Mime;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers
{
    /// <summary>
    /// Endpoints de validación de impresión.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class ValidaImpresionController : ControllerBase
    {
        private readonly IValidaImpresionService _validaImpresion;
        private readonly ILogger<ValidaImpresionController> _logger;
        private readonly ResponseHandler _responseHandler = new();

        public ValidaImpresionController(
            IValidaImpresionService validaImpresion,
            ILogger<ValidaImpresionController> logger)
        {
            _validaImpresion = validaImpresion;
            _logger = logger;
        }

        /// <summary>
        /// Valida si la tarjeta fue impresa (consulta UNI5400).
        /// </summary>
        /// <param name="getValidaImpresionDto">Parámetros de búsqueda.</param>
        /// <returns>Respuesta HTTP con el resultado de la validación.</returns>
        [HttpGet("ValidaImpresion")]
        [ProducesResponseType(typeof(GetValidaImpresionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GetValidaImpresionResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidaImpresion([FromQuery] GetValidaImpresionDto getValidaImpresionDto)
        {
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
                _logger.LogInformation("Validando impresión de tarjeta. Filtros: {@dto}", getValidaImpresionDto);

                var respuesta = await _validaImpresion.ValidaImpresionAsync(getValidaImpresionDto);

                // Garantiza respuesta no nula para el handler
                respuesta ??= BuildError("BadRequest", "400", "No se obtuvo respuesta del servicio.");

                return _responseHandler.HandleResponse(respuesta, respuesta.Codigo.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar impresión. Filtros: {@dto}", getValidaImpresionDto);

                var error = BuildError("BadRequest", "400", ex.Message);
                return _responseHandler.HandleResponse(error, error.Codigo.Status);
            }
        }

        private static GetValidaImpresionResponseDto BuildError(string status, string code, string message)
            => new GetValidaImpresionResponseDto
            {
                Imprime = false,
                Codigo = new GetValidaImpresionResponseDto.CodigoRespuesta
                {
                    Status = status,
                    Error = code,
                    Message = message,
                    TimeStamp = DateTime.Now.ToString("HH:mm:ss tt")
                }
            };
    }
}

