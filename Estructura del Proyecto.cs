Así va quedando:

using Microsoft.AspNetCore.Mvc;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;
using System.Net.Mime;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Controllers;

/// <summary>
/// Controlador de endpoints para la <b>gestión de transacciones POS</b>.
/// Expone operaciones para recibir, validar y procesar transacciones POS, estandarizando
/// la respuesta mediante mapeo de códigos de negocio (<see cref="BizCodes"/>) a códigos HTTP con <see cref="BizHttpMapper"/>.
/// </summary>
/// <param name="_transaccionesService">
/// Servicio de dominio responsable de aplicar reglas de negocio, idempotencia (por <c>idTransaccionUnico</c>)
/// y persistencia. Debe devolver un resultado con <c>CodigoError</c> y <c>DescripcionError</c>.
/// </param>
/// <remarks>
/// <para><b>Características clave</b>:</para>
/// <list type="bullet">
///   <item><description>Produce <c>application/json</c> en todas las respuestas.</description></item>
///   <item><description>Valida el modelo (<see cref="ControllerBase.ModelState"/>) antes de invocar lógica de negocio.</description></item>
///   <item><description>Traduce <see cref="BizCodes"/> a códigos HTTP con <see cref="BizHttpMapper"/> de forma consistente.</description></item>
///   <item><description>Listo para herramientas de documentación (Swagger/NSwag) vía XML docs + atributos <c>[ProducesResponseType]</c>.</description></item>
/// </list>
/// <para><b>Idempotencia recomendada</b>:</para>
/// Utiliza <c>idTransaccionUnico</c> para evitar registros duplicados ante reintentos del cliente o reenvíos de gateway.
/// El servicio debe respetar esta llave para retornar un resultado determinístico ante el mismo payload.
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
    /// Objeto JSON con la transacción POS. Propiedades esperadas:
    /// <list type="table">
    ///   <item>
    ///     <term><c>numeroCuenta</c></term>
    ///     <description>Cuenta asociada a la operación (string). Formato según core bancario (p. ej., enmascarado o completo).</description>
    ///   </item>
    ///   <item>
    ///     <term><c>montoDebitado</c></term>
    ///     <description>Importe debitado (string). Se recomienda usar punto decimal. El servicio normaliza separadores.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>montoAcreditado</c></term>
    ///     <description>Importe acreditado (string). Igual criterios que <c>montoDebitado</c>.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>codigoComercio</c></term>
    ///     <description>Código único del comercio (string). Útil para conciliación y auditoría.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>nombreComercio</c></term>
    ///     <description>Nombre legible del comercio (string), para trazabilidad y reportes.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>terminal</c></term>
    ///     <description>Identificador de la terminal POS (string). Recomendado para antifraude y correlación.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>descripción</c></term>
    ///     <description>Descripción libre de la operación (string). Puede incluir referencia externa.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>naturalezaContable</c></term>
    ///     <description>Clasificación contable de la transacción (string). Ej.: <c>D</c>/<c>C</c> o códigos internos.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>numeroDeCorte</c></term>
    ///     <description>Número de corte o batch del comercio/operador (string) para cierres y arqueos.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>idTransaccionUnico</c></term>
    ///     <description>Identificador único de idempotencia (string). Debe mantenerse constante por reintento.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>estado</c></term>
    ///     <description>Estado de la transacción (string). Ej.: <c>APROBADA</c>, <c>PENDIENTE</c>, <c>RECHAZADA</c>.</description>
    ///   </item>
    ///   <item>
    ///     <term><c>descripcionEstado</c></term>
    ///     <description>Descripción humana del estado (string). Útil para mensajes al cliente.</description>
    ///   </item>
    /// </list>
    /// </param>
    /// <returns>
    /// Respuesta HTTP con objeto JSON normalizado:
    /// <list type="bullet">
    ///   <item><description>Éxito/negocio: <c>{ "code": "000", "message": "..." }</c> (código y mensaje de negocio mapeados a HTTP).</description></item>
    ///   <item><description>Validación: <see cref="RespuestaGuardarTransaccionesDto"/> con detalles (<c>CodigoError</c>, <c>DescripcionError</c>).</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Ruta</b>:</para>
    /// <code>POST api/ProcesamientoTransaccionesPOS/Transacciones/GuardarTransacciones</code>
    ///
    /// <example>
    /// <para><b>Ejemplo de solicitud (JSON)</b></para>
    /// <code language="json">
    /// {
    ///   "numeroCuenta": "001234567890",
    ///   "montoDebitado": "125.75",
    ///   "montoAcreditado": "0.00",
    ///   "codigoComercio": "MC123",
    ///   "nombreComercio": "COMERCIO XYZ S.A.",
    ///   "terminal": "TERM-0001",
    ///   "descripción": "Pago POS ticket 98765",
    ///   "naturalezaContable": "DB",
    ///   "numeroDeCorte": "20251016-01",
    ///   "idTransaccionUnico": "6c1b1e00-6a66-4c0b-a4f7-1f77dfb9f9ef",
    ///   "estado": "APROBADA",
    ///   "descripcionEstado": "Operación aprobada por el emisor"
    /// }
    /// </code>
    /// </example>
    ///
    /// <para><b>Ejemplo de respuesta (éxito)</b></para>
    /// <code language="json">
    /// { "code": "000", "message": "Transacción registrada correctamente" }
    /// </code>
    ///
    /// <para><b>Ejemplo de respuesta (modelo inválido)</b></para>
    /// <code language="json">
    /// { "CodigoError": "400", "DescripcionError": "Solicitud inválida, modelo DTO, invalido." }
    /// </code>
    ///
    /// <para><b>Notas</b>:</para>
    /// <list type="bullet">
    ///   <item><description><b>Montos como string</b>: el servicio debe normalizar separadores (coma/punto) antes del cálculo/registro.</description></item>
    ///   <item><description><b>Idempotencia</b>: enviar siempre el mismo <c>idTransaccionUnico</c> por reintento.</description></item>
    ///   <item><description><b>Auditoría</b>: conservar <c>codigoComercio</c>, <c>terminal</c> y <c>numeroDeCorte</c> para conciliaciones.</description></item>
    /// </list>
    ///
    /// <response code="200">Transacción aceptada por reglas de negocio (p. ej., code "000").</response>
    /// <response code="400">Solicitud inválida (estructura/validaciones).</response>
    /// <response code="409">Conflicto/duplicidad (idempotencia violada, según <see cref="BizCodes"/>).</response>
    /// <response code="500">Error interno inesperado (excepción no controlada en capa de servicio).</response>
    /// </remarks>
    [HttpPost("GuardarTransacciones")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GuardarTransacciones([FromBody] GuardarTransaccionesDto guardarTransaccionesDto)
    {
        // Validación estructural del modelo antes de invocar negocio:
        // evita ejecutar lógica si hay tipos incompatibles, campos faltantes o JSON mal formado.
        if (!ModelState.IsValid)
        {
            // Mapeo explícito de "solicitud inválida" a HTTP 400 mediante BizHttpMapper
            // para mantener consistencia con el ecosistema de códigos de negocio.
            return StatusCode(BizHttpMapper.ToHttpStatusInt("400"), new
            {
                BizCodes.SolicitudInvalida,
                message = "Solicitud inválida, modelo DTO, invalido."
            });
        }

        try
        {
            // Delegación de la operación: el servicio aplica reglas,
            // idempotencia por idTransaccionUnico, persistencia y retorna un resultado tipado.
            var respuesta = await _transaccionesService.GuardarTransaccionesAsync(guardarTransaccionesDto);

            // Normalización de la salida: todo resultado de negocio se traduce a HTTP con BizHttpMapper.
            var code = respuesta.CodigoError ?? BizCodes.ErrorDesconocido;
            var http = BizHttpMapper.ToHttpStatusInt(code);

            // Contrato de salida compacto y legible por clientes móviles o integraciones de bajo acoplamiento.
            return StatusCode(http, new
            {
                code,
                message = respuesta.DescripcionError
            });
        }
        catch (Exception ex)
        {
            // Manejo controlado: no exponer stack trace ni detalles internos.
            // Recomendación: registrar la excepción en tu middleware/servicio de logging unificado.
            RespuestaGuardarTransaccionesDto dto = new()
            {
                CodigoError = "400",
                DescripcionError = ex.Message
            };

            // Por estándar, retornamos 400 en errores recuperables en esta ruta.
            // Alternativa: mapear errores no recuperables a 500 mediante BizCodes.ErrorInterno.
            return BadRequest(dto);
        }
    }
}


Como podriamos mejorarlo, si hay forma hazlo, no se si mejorar la descripción de los dtos, para complementar?
