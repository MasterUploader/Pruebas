Así va quedando el códgio con la smodificaciones que voy realizando:

using Microsoft.AspNetCore.Mvc;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.ValidarTransaccion;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Services.ValidarTransacciones;
using System.Net.Mime;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Controllers;

/// <summary>
/// Clase controlador para la validación de transacciones POS.
/// </summary>
/// <param name="validarTransaccionService">Parametro Dto que contiene la estructura requerida para la petición.</param>
[Route("api/ProcesamientoTransaccionesPOS/[controller]")]
[ApiController]
public class ValidarTransaccionesController(IValidarTransaccionService validarTransaccionService) : ControllerBase
{
    private readonly IValidarTransaccionService _validarTransaccionService = validarTransaccionService;

    /// <summary>
    /// REndpoint para validar las transacciones POS.
    /// </summary>
    /// <param name="validarTransaccionesDto">Ver <see cref="ValidarTransaccionesDto"/> para detalles, ejemplos y validaciones semánticas.</param>
    /// <param name="ct">Token de Cancelación.</param>
    /// <returns>200/4xx/5xx con <see cref="RespuestaValidarTransaccionesDto"/>.</returns>
    /// <remarks>
    /// <para><b>Ruta</b>:</para>
    /// <code>POST api/ProcesamientoTransaccionesPOS/ValidarTransacciones/ValidarTransacciones</code>
    /// </remarks>
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status500InternalServerError)]
    [HttpPost("ValidarTransacciones")]
    public async Task<IActionResult> ValidarTransacciones([FromBody] ValidarTransaccionesDto validarTransaccionesDto, CancellationToken ct = default)
    {
        // Validación estructural (DataAnnotations + IValidatableObject) antes de negocio
        if (!ModelState.IsValid)
        {
            // Mapeo explícito de "solicitud inválida" a HTTP mediante BizHttpMapper
            return StatusCode(BizHttpMapper.ToHttpStatusInt("400"), new
            {
                BizCodes.SolicitudInvalida,
                message = "Solicitud inválida, modelo DTO, invalido."
            });
        }

        try
        {
            // Servicio: reglas, idempotencia por idTransaccionUnico y persistencia
            var respuesta = await _validarTransaccionService.ValidarTransaccionesAsync(validarTransaccionesDto, ct);

            // Normalizar salida: code/message + HTTP vía BizHttpMapper (fuente de verdad)
            var code = respuesta.CodigoError ?? BizCodes.ErrorDesconocido;
            var http = BizHttpMapper.ToHttpStatusInt(code);

            RespuestaGuardarTransaccionesDto result = new()
            {
                CodigoError = code,
                DescripcionError = respuesta.DescripcionError
            };

            // Observabilidad: el middleware ya coloca X-Correlation-Id en la respuesta
            return StatusCode(http, result);
        }
        catch (Exception ex)
        {
            // Manejo controlado: no exponer detalles internos; log en middleware/servicio unificado
            RespuestaGuardarTransaccionesDto dto = new()
            {
                CodigoError = "400",
                DescripcionError = ex.Message
            };

            return BadRequest(dto);
        }                 
    }
}


using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.ValidarTransaccion;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Services.ValidarTransacciones;

/// <summary>
/// Interface para el servicio de validación de transacciones POS.
/// </summary>
public interface IValidarTransaccionService
{
    /// <summary>
    /// Método para validar las transacciones POS.
    /// </summary>
    /// <param name="validarTransaccionDto">Validar transacción DTO.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns></returns>
    Task<RespuestaValidarTransaccionesDto> ValidarTransaccionesAsync(ValidarTransaccionesDto validarTransaccionDto, CancellationToken ct = default);
}



using Connections.Abstractions;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.ValidarTransaccion;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using System.Data.Common;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Services.ValidarTransacciones;

/// <summary>
/// Clase que implementa el servicio de validación de transacciones POS.
/// </summary>
/// <param name="_connection">Inyección de clase IDatabaseConnection.</param>
/// <param name="_contextAccessor">Inyección de clase IHttpContextAccessor.</param>
public class ValidarTransaccionService(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : IValidarTransaccionService
{
    /// <summary>
    /// Representa la respuesta de la validación de transacciones.
    /// </summary>
    /// <remarks>En este campo se espera que se guarde <see cref="RespuestaValidarTransaccionesDto"/>
    /// que contine la información de la existencia de una transacción.</remarks>
    protected RespuestaValidarTransaccionesDto _respuestaValidarTransaccionesDto = new();

    /// <inheritdoc/>
    public async Task<RespuestaValidarTransaccionesDto> ValidarTransaccionesAsync(ValidarTransaccionesDto validarTransaccionDto, CancellationToken ct = default)
    {

        var corte = validarTransaccionDto.NumeroDeCorte;
        var stan = validarTransaccionDto.IdTransaccionUnico;

        // ============================
        // Query con SelectQueryBuilder
        // ============================
        var qb = new SelectQueryBuilder("POSRE01G01", "BCAH96DTA", SqlDialect.Db2i)
            // Selección (usamos alias para mapear limpio al DTO)
            .Select(
                ("GUID", "GUID"),
                ("FECHAPOST", "FECHA_POSTEO"),
                ("HORAPOST", "HORA_POSTEO"),
                ("NUMCUENTA", "NUMERO_CUENTA"),
                ("MTODEBITO", "MONTO_DEBITADO"),
                ("MTOACREDI", "MONTO_ACREDITADO"),
                ("CODCOMERC", "CODIGO_COMERCIO"),
                ("NOMCOMERC", "NOMBRE_COMERCIO"),
                ("TERMINAL", "TERMINAL_COMERCIO"),
                ("DESCRIPC", "DESCRIPCION"),
                ("NATCONTA", "NATURALEZA_CONTABLE"),
                ("NUMCORTE", "NUMERO_CORTE"),
                ("IDTRANUNI", "ID_TRANSACCION_UNICO"),
                ("ESTADO", "ESTADO_TRANSACCION"),
                ("DESCESTADO", "DESCRIPCION_ESTADO"),
                ("CODERROR", "CODIGO_ERROR"),
                ("DESCERROR", "DESCRIPCION_ERROR")
            )
            // Filtrado por la clave del LF
            .WhereRaw($"NUMCORTE = '{corte}'")
            .WhereRaw($"IDTRANUNI = '{stan}'")
            .FetchNext(1); // AS400: FETCH FIRST N ROWS ONLY


        var qr = qb.Build(); // -> QueryResult con .Sql (y .Parameters vacío en tu versión)

        _connection.Open();
        if (!_connection.IsConnected)
        {
            return BuildError(BizCodes.ConexionDbFallida, "No se pudo conectar a la base de datos.");
        }

        try
        {
            // Preferimos el overload que acepta QueryResult (inyecta logging y params si aplica)
            DbCommand cmd;
            try
            {
                cmd = _connection.GetDbCommand(qr, _contextAccessor?.HttpContext);
            }
            catch
            {
                cmd = _connection.GetDbCommand();
                cmd.CommandText = qr.Sql;
            }

            using var rd = await cmd.ExecuteReaderAsync(ct);
            if (await rd.ReadAsync(ct))
            {
                var r = new RespuestaValidarTransaccionesDto
                {
                    NumeroCuenta = rd["NUMERO_CUENTA"]?.ToString() ?? "",
                    MontoDebitado = rd["MONTO_DEBITADO"]?.ToString() ?? "",
                    MontoAcreditado = rd["MONTO_ACREDITADO"]?.ToString() ?? "",
                    CodigoComercio = rd["CODIGO_COMERCIO"]?.ToString() ?? "",
                    NombreComercio = rd["NOMBRE_COMERCIO"]?.ToString() ?? "",
                    TerminalComercio = rd["TERMINAL_COMERCIO"]?.ToString() ?? "",
                    Descripcion = rd["DESCRIPCION"]?.ToString() ?? "",
                    NaturalezaContable = rd["NATURALEZA_CONTABLE"]?.ToString() ?? "",
                    NumeroCorte = rd["NUMERO_CORTE"]?.ToString() ?? "",
                    IdTransaccionUnico = rd["ID_TRANSACCION_UNICO"]?.ToString() ?? "",
                    EstadoTransaccion = rd["ESTADO_TRANSACCION"]?.ToString() ?? "",
                    DescripcionEstado = rd["DESCRIPCION_ESTADO"]?.ToString() ?? "",
                    CodigoError = rd["CODIGO_ERROR"]?.ToString() ?? "",
                    DescripcionError = rd["DESCRIPCION_ERROR"]?.ToString() ?? ""
                };

                return BuildError(BizCodes.YaProcesado, "La transaccion ya existe (NUMCORTE, IDTRANUNI).");
            }

            return BuildError(BizCodes.Ok, "No existe el registro. Puede continuar.");

        }
        finally
        {
            _connection.Close();
        }
    }

    /// <summary>
    /// Crea un DTO de respuesta de error con metadatos consistentes.
    /// </summary>
    private static RespuestaValidarTransaccionesDto BuildError(string code, string message)
        => new()
        {
            CodigoError = code,
            DescripcionError = message
        };
}




using System.Text.Json.Serialization;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.ValidarTransaccion;

/// <summary>
/// Clase DTO para la respuesta de la validación de transacciones POS.
/// </summary>
public class RespuestaValidarTransaccionesDto
{
    /// <summary>
    /// Número de Cuenta del Comercio.
    /// </summary>
    [JsonPropertyName("numeroCuenta")]
    public string NumeroCuenta { get; set; } = string.Empty;

    /// <summary>
    /// Monto Debitado de la transacción.
    /// </summary>
    [JsonPropertyName("montoDebitado")]
    public string MontoDebitado { get; set; } = string.Empty;

    /// <summary>
    /// Monto Acreditado de la transacción.
    /// </summary>
    [JsonPropertyName("montoAcreditado")]
    public string MontoAcreditado { get; set; } = string.Empty;

    /// <summary>
    /// Código del Comercio.
    /// </summary>
    [JsonPropertyName("codigoComercio")]
    public string CodigoComercio { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del Comercio.
    /// </summary>
    [JsonPropertyName("nombreComercio")]
    public string NombreComercio { get; set; } = string.Empty;

    /// <summary>
    /// Número de Terminal del Comercio.
    /// </summary>
    [JsonPropertyName("terminalComercio")]
    public string TerminalComercio { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la transacción.
    /// </summary>
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Naturaleza Contable de la transacción.
    /// </summary>
    [JsonPropertyName("naturalezaContable")]
    public string NaturalezaContable { get; set; } = string.Empty;

    /// <summary>
    /// Número de Corte de la transacción.
    /// </summary>
    [JsonPropertyName("numeroCorte")]
    public string NumeroCorte { get; set; } = string.Empty;

    /// <summary>
    /// Id transacción único.
    /// </summary>
    [JsonPropertyName("idTransaccionUnico")]
    public string IdTransaccionUnico { get; set; } = string.Empty;

    /// <summary>
    /// Estado de la transacción.
    /// </summary>
    [JsonPropertyName("estadoTransaccion")]
    public string EstadoTransaccion { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del estado de la transacción.
    /// </summary>
    [JsonPropertyName("descripcionEstado")]
    public string DescripcionEstado { get; set; } = string.Empty;

    /// <summary>
    /// Codigo de error de la transacción.
    /// </summary>
    [JsonPropertyName("codigoError")]
    public string CodigoError { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del error de la transacción.
    /// </summary>
    [JsonPropertyName("descripcionError")]
    public string DescripcionError { get; set; } = string.Empty;
}



using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.ValidarTransaccion;

/// <summary>
/// Clase DTO para validar transacciones POS.
/// </summary>
public class ValidarTransaccionesDto
{
    /// <summary>
    /// Identificador único de la transacción.
    /// </summary>
    [Required(ErrorMessage = "El id de transacción es obligatorio.")]
    [JsonProperty("idTransaccionUnico")]
    public string IdTransaccionUnico { get; set; } = string.Empty;

    /// <summary>
    /// Número de corte asociado a la transacción.
    /// </summary>
    [Required(ErrorMessage = "El número de corte es obligatorio.")]
    [JsonProperty("numeroDeCorte")]
    public string NumeroDeCorte { get; set; } = string.Empty;
}



Pero necesito que en la opción en que si encuentra datos, los devuelva en la respuetas ya que RespuestaValidarTransaccionesDto, los contiene.


