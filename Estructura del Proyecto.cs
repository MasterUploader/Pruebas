using Microsoft.AspNetCore.Mvc;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Common;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;
using System.Net.Mime;
using Swashbuckle.AspNetCore.Filters;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Controllers;

/// <summary>
/// Controlador de endpoints para la <b>gestión de transacciones POS</b>.
/// Estandariza validación, mapeo de <see cref="BizCodes"/> a HTTP con <see cref="BizHttpMapper"/> y manejo de errores.
/// </summary>
/// <param name="_transaccionesService">
/// Servicio de dominio que aplica reglas de negocio, idempotencia por <c>idTransaccionUnico</c> y persistencia.
/// </param>
/// <remarks>
/// <para><b>Características clave</b>:</para>
/// <list type="bullet">
///   <item><description>Produce <c>application/json</c> en todas las respuestas.</description></item>
///   <item><description>Valida <see cref="ControllerBase.ModelState"/> previo a la lógica de negocio.</description></item>
///   <item><description>Documentación enriquecida (XML docs + Examples) para Swagger/NSwag.</description></item>
/// </list>
/// <para><b>Observabilidad</b>:</para>
/// El middleware agrega/propaga <c>X-Correlation-Id</c>. Se recomienda registrar <i>TraceId</i>, <i>CorrelationId</i>, <c>codigoComercio</c>, <c>terminal</c> y <c>idTransaccionUnico</c>.
/// <para><b>Mapa referencial BizCodes → HTTP</b> (tu <see cref="BizHttpMapper"/> es la fuente de verdad):</para>
/// <code>
/// "000" (Éxito)                         → 200 OK
/// "400"/SolicitudInvalida               → 400 BadRequest
/// "409xx"/Duplicado/Conflicto           → 409 Conflict
/// "500"/ErrorInterno                    → 500 InternalServerError
/// otros códigos                         → según reglas (BizHttpMapper)
/// </code>
/// </remarks>
[Route("api/ProcesamientoTransaccionesPOS/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class TransaccionesController(ITransaccionesServices _transaccionesService) : ControllerBase
{
    /// <summary>
    /// Registra/Procesa transacciones POS en el backend.
    /// </summary>
    /// <param name="guardarTransaccionesDto">Ver <see cref="GuardarTransaccionesDto"/> para detalles, ejemplos y validaciones semánticas.</param>
    /// <returns>200 con <see cref="ApiResultDto"/>; 4xx/5xx con <see cref="RespuestaGuardarTransaccionesDto"/>.</returns>
    /// <remarks>
    /// <para><b>Ruta</b>:</para>
    /// <code>POST api/ProcesamientoTransaccionesPOS/Transacciones/GuardarTransacciones</code>
    /// </remarks>
    [HttpPost("GuardarTransacciones")]
    [Consumes(MediaTypeNames.Application.Json)]
    [SwaggerRequestExample(typeof(GuardarTransaccionesDto), typeof(Swagger.Examples.Transacciones.GuardarTransaccionesRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Swagger.Examples.Common.ApiResultDtoExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(Swagger.Examples.Common.RespuestaGuardarTransaccionesDtoExample))]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(Swagger.Examples.Common.RespuestaGuardarTransaccionesDtoExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(Swagger.Examples.Common.RespuestaGuardarTransaccionesDtoExample))]
    [ProducesResponseType(typeof(ApiResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GuardarTransacciones([FromBody] GuardarTransaccionesDto guardarTransaccionesDto)
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
            var respuesta = await _transaccionesService.GuardarTransaccionesAsync(guardarTransaccionesDto);

            // Normalizar salida: code/message + HTTP vía BizHttpMapper (fuente de verdad)
            var code = respuesta.CodigoError ?? BizCodes.ErrorDesconocido;
            var http = BizHttpMapper.ToHttpStatusInt(code);

            ApiResultDto result = new()
            {
                Code = code,
                Message = respuesta.DescripcionError
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
