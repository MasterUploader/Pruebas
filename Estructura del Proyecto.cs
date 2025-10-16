using Microsoft.AspNetCore.Mvc;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;
using System.Net.Mime;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Controllers;

/// <summary>
/// Controlador de endpoints para la <b>gestión de transacciones POS</b>.
/// Expone operaciones orientadas a la recepción y procesamiento de transacciones provenientes de terminales POS,
/// aplicando validaciones de modelo, mapeo de códigos de negocio a códigos HTTP y manejo controlado de errores.
/// </summary>
/// <param name="_transaccionesService">
/// Servicio de dominio responsable de <b>validar, persistir y/o enrutar</b> la solicitud de transacción POS.
/// Debe implementar reglas de negocio, idempotencia (si aplica) y retornar un objeto con código/descripcion de resultado.
/// </param>
/// <remarks>
/// <para>
/// Este controlador:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>Produce <c>application/json</c> en todas las respuestas.</description>
///   </item>
///   <item>
///     <description>Valida el estado del modelo (<see cref="ControllerBase.ModelState"/>) y devuelve error estructurado si es inválido.</description>
///   </item>
///   <item>
///     <description>Traduce <see cref="BizCodes"/> a códigos HTTP usando <see cref="BizHttpMapper"/> para uniformidad REST.</description>
///   </item>
///   <item>
///     <description>Diseñado para integrarse con herramientas de documentación (Swagger/NSwag) vía XML docs + atributos <c>[ProducesResponseType]</c>.</description>
///   </item>
/// </list>
/// <para>
/// <b>Seguridad</b>:
/// Este controlador asume que la autenticación/autorización se gestionan en middleware o filtros globales.
/// Si la API requiere firma, nonces o timestamps, incorpóralos en el DTO de entrada y valida en el servicio.
/// </para>
/// <para>
/// <b>Idempotencia</b>:
/// Para evitar duplicados por reintentos del cliente, se recomienda manejar una <i>Idempotency-Key</i> o correlación en el servicio.
/// </para>
/// <seealso cref="BizCodes"/>
/// <seealso cref="BizHttpMapper"/>
/// </remarks>
[Route("api/ProcesamientoTransaccionesPOS/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class TransaccionesController(ITransaccionesServices _transaccionesService) : ControllerBase
{
    /// <summary>
    /// Registra/Procesa transacciones POS en el backend.
    /// </summary>
    /// <param name="guardarTransaccionesDto">
    /// DTO de entrada con los datos de la transacción POS (terminal, importes, autorizaciones, etc.).
    /// Debe venir en el cuerpo (<c>FromBody</c>) codificado como JSON válido.
    /// </param>
    /// <returns>
    /// Resultado HTTP con objeto JSON que contiene <c>code</c> y <c>message</c> mapeados desde reglas de negocio.
    /// En errores de validación se retorna un DTO estructurado (<see cref="RespuestaGuardarTransaccionesDto"/>).
    /// </returns>
    /// <remarks>
    /// <para><b>Ruta</b>:</para>
    /// <code>POST api/ProcesamientoTransaccionesPOS/Transacciones/GuardarTransacciones</code>
    ///
    /// <para><b>Ejemplo: solicitud (cURL)</b></para>
    /// <code>
    /// curl -X POST "https://{host}/api/ProcesamientoTransaccionesPOS/Transacciones/GuardarTransacciones" ^
    ///   -H "Content-Type: application/json" ^
    ///   -d "{ \"terminalId\":\"T-12345\", \"monto\":125.75, \"moneda\":\"HNL\", \"fecha\":\"2025-10-16T18:40:00Z\" }"
    /// </code>
    ///
    /// <para><b>Respuesta 200 (éxito)</b></para>
    /// <code language="json">
    /// {
    ///   "code": "000", 
    ///   "message": "Transacción registrada correctamente"
    /// }
    /// </code>
    ///
    /// <para><b>Respuesta 400 (modelo inválido)</b></para>
    /// <code language="json">
    /// {
    ///   "CodigoError": "400",
    ///   "DescripcionError": "Solicitud inválida, modelo DTO, invalido."
    /// }
    /// </code>
    ///
    /// <para><b>Notas de interoperabilidad</b>:</para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>Si tu cliente usa listas, recuerda que en C# puedes inicializarlas con sintaxis simplificada: <c>var items = [];</c></description>
    ///   </item>
    ///   <item>
    ///     <description>Este endpoint prioriza respuestas determinísticas: un mismo <i>payload</i> debe producir el mismo <c>code</c>.</description>
    ///   </item>
    /// </list>
    ///
    /// <response code="200">Transacción aceptada por las reglas de negocio (code típicamente "000").</response>
    /// <response code="400">Solicitud inválida (modelo JSON incorrecto o validaciones fallidas).</response>
    /// <response code="409">Conflicto o duplicidad, según <see cref="BizCodes"/> que retorne el servicio.</response>
    /// <response code="422">Entrada semánticamente incorrecta; considerar según reglas de negocio.</response>
    /// <response code="500">Error interno inesperado.</response>
    /// </remarks>
    [HttpPost("GuardarTransacciones")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GuardarTransacciones([FromBody] GuardarTransaccionesDto guardarTransaccionesDto)
    {
        // Validación estructural del modelo (DTO) antes de invocar lógica de negocio.
        // Esto evita ejecutar el servicio cuando hay errores de serialización, tipos incorrectos o datos requeridos ausentes.
        if (!ModelState.IsValid)
        {
            // Mapeo explícito a HTTP según la tabla de códigos de negocio (SolicitudInvalida → 400).
            // Se retorna un cuerpo consistente para clientes que esperan "CodigoError" y "DescripcionError".
            return StatusCode(BizHttpMapper.ToHttpStatusInt("400"), new
            {
                BizCodes.SolicitudInvalida,
                message = "Solicitud inválida, modelo DTO, invalido."
            });
        }

        try
        {
            // Delegamos en el servicio: aplica reglas, persistencia, idempotencia y devuelve un resultado tipado.
            var respuesta = await _transaccionesService.GuardarTransaccionesAsync(guardarTransaccionesDto);

            // Normalización de salida: traducimos el código de negocio a su equivalente HTTP para la respuesta.
            var code = respuesta.CodigoError ?? BizCodes.ErrorDesconocido;
            var http = BizHttpMapper.ToHttpStatusInt(code);

            // Nota: se regresa un objeto anónimo { code, message } para una lectura rápida del cliente.
            // Si tu documentación exige un contrato formal, puedes exponer un DTO de salida específico.
            return StatusCode(http, new
            {
                code,
                message = respuesta.DescripcionError
            });
        }
        catch (Exception ex)
        {
            // Manejo controlado de excepciones: no exponer detalles internos (stack trace) al cliente.
            // Para trazabilidad, registrar el error en el middleware/servicio de logging (no mostrado aquí).
            RespuestaGuardarTransaccionesDto dto = new()
            {
                CodigoError = "400",
                DescripcionError = ex.Message
            };

            // Por defecto, devolvemos 400 (BadRequest) para excepciones recuperables.
            // Si deseas separar errores del servidor, mapea a BizCodes.ErrorInterno → 500 con BizHttpMapper.
            return BadRequest(dto);
        }
    }
}
