using System.Collections.Generic;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using Swashbuckle.AspNetCore.Filters;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Transacciones;

/// <summary>
/// Proveedor de <b>múltiples ejemplos</b> de payload para <see cref="GuardarTransaccionesDto"/>.
/// Incluye tres variantes realistas para facilitar pruebas y documentación.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description><b>Compra DB</b>: Débito clásico con acreditado en cero.</description></item>
///   <item><description><b>Devolución CR</b>: Reembolso acreditando al cliente.</description></item>
///   <item><description><b>Compra con propina</b>: Débito con importe adicional de propina.</description></item>
/// </list>
/// </remarks>
public sealed class GuardarTransaccionesRequestMultipleExamples : IMultipleExamplesProvider<GuardarTransaccionesDto>
{
    /// <summary>
    /// Devuelve ejemplos con nombre que aparecerán en Swagger.
    /// </summary>
    public IEnumerable<SwaggerExample<GuardarTransaccionesDto>> GetExamples()
    {
        // ⚠️ Respetar longitudes de tu DTO:
        // - NumeroDeCorte: [StringLength(2)] → usar "01"/"02"/"03".
        // - Terminal: [StringLength(15)].
        // - NumeroCuenta: [StringLength(15)].
        // - Monto...: [StringLength(12)].
        // - Descripcion: [StringLength(200)].

        // 1) Compra típica (DB)
        yield return new SwaggerExample<GuardarTransaccionesDto>
        {
            Name = "Compra débito (DB)",
            Value = new()
            {
                NumeroCuenta = "001234567890",
                MontoDebitado = "125.75",
                MontoAcreditado = "0.00",
                CodigoComercio = "MC123",
                NombreComercio = "COMERCIO XYZ S.A.",
                Terminal = "TERM-0001",
                Descripcion = "Compra en tienda física - ticket 98765",
                NaturalezaContable = "D", // tu DTO indica 2 chars; usa D/C si esa es tu convención
                NumeroDeCorte = "01",
                IdTransaccionUnico = "f5a2c0a1-3c76-4c38-894d-2c9a9b167100",
                Estado = "APROBADA",
                DescripcionEstado = "Operación aprobada por el emisor"
            }
        };

        // 2) Devolución (CR)
        yield return new SwaggerExample<GuardarTransaccionesDto>
        {
            Name = "Devolución (CR)",
            Value = new()
            {
                NumeroCuenta = "001234567890",
                MontoDebitado = "0.00",
                MontoAcreditado = "125.75",
                CodigoComercio = "MC123",
                NombreComercio = "COMERCIO XYZ S.A.",
                Terminal = "TERM-0001",
                Descripcion = "Devolución parcial por artículo defectuoso",
                NaturalezaContable = "C",
                NumeroDeCorte = "02",
                IdTransaccionUnico = "0cdddfa0-0d5e-4b9c-9c9f-93a6a4fc9f11",
                Estado = "APROBADA",
                DescripcionEstado = "Reembolso aprobado"
            }
        };

        // 3) Compra con propina (DB)
        yield return new SwaggerExample<GuardarTransaccionesDto>
        {
            Name = "Compra con propina (DB)",
            Value = new()
            {
                NumeroCuenta = "001234567890",
                MontoDebitado = "210.50",
                MontoAcreditado = "0.00",
                CodigoComercio = "MC987",
                NombreComercio = "RESTAURANTE ABC",
                Terminal = "TERM-0444",
                Descripcion = "Cuenta mesa 12 + propina 10%",
                NaturalezaContable = "D",
                NumeroDeCorte = "03",
                IdTransaccionUnico = "9b8e1ed9-5b0f-4f2a-b9f7-4c0e8b42a3aa",
                Estado = "APROBADA",
                DescripcionEstado = "Transacción con propina"
            }
        };
    }
}




using System.Collections.Generic;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using Swashbuckle.AspNetCore.Filters;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common;

/// <summary>
/// Proveedor de <b>múltiples ejemplos</b> para <see cref="RespuestaGuardarTransaccionesDto"/>.
/// Se reutiliza el mismo tipo para 200 y para errores, porque tu contrato unifica la salida.
/// </summary>
/// <remarks>
/// <list type="bullet">
//   <item><description><b>200</b>: "00000" (OK) ó "20001" (idempotente ya procesado).</description></item>
///   <item><description><b>400</b>: validación/entrada inválida.</description></item>
///   <item><description><b>409</b>: conflicto (idempotencia duplicada).</description></item>
///   <item><description><b>500</b>: error interno inesperado.</description></item>
/// </list>
/// </remarks>
public sealed class RespuestaGuardarTransaccionesMultipleExamples : IMultipleExamplesProvider<RespuestaGuardarTransaccionesDto>
{
    /// <summary> Devuelve ejemplos con nombre para todas las familias de respuesta. </summary>
    public IEnumerable<SwaggerExample<RespuestaGuardarTransaccionesDto>> GetExamples()
    {
        // 200 OK - Éxito
        yield return new SwaggerExample<RespuestaGuardarTransaccionesDto>
        {
            Name = "200 - Éxito",
            Value = new()
            {
                CodigoError = "00000",
                DescripcionError = "Transacción registrada correctamente"
            }
        };

        // 200 OK - Reintento idempotente (misma key, mismo resultado)
        yield return new SwaggerExample<RespuestaGuardarTransaccionesDto>
        {
            Name = "200 - Ya procesado (idempotente)",
            Value = new()
            {
                CodigoError = "20001",
                DescripcionError = "Transacción previamente registrada con el mismo idTransaccionUnico"
            }
        };

        // 400 BadRequest - modelo inválido
        yield return new SwaggerExample<RespuestaGuardarTransaccionesDto>
        {
            Name = "400 - Solicitud inválida",
            Value = new()
            {
                CodigoError = "40000",
                DescripcionError = "Solicitud inválida, modelo DTO, invalido."
            }
        };

        // 409 Conflict - id único duplicado
        yield return new SwaggerExample<RespuestaGuardarTransaccionesDto>
        {
            Name = "409 - Duplicado por idempotencia",
            Value = new()
            {
                CodigoError = "40901",
                DescripcionError = "Transacción previamente registrada con el mismo idTransaccionUnico"
            }
        };

        // 500 Internal Server Error - inesperado
        yield return new SwaggerExample<RespuestaGuardarTransaccionesDto>
        {
            Name = "500 - Error interno",
            Value = new()
            {
                CodigoError = "50099",
                DescripcionError = "Ha ocurrido un error inesperado al procesar la solicitud"
            }
        };
    }
}





// ...
[HttpPost("GuardarTransacciones")]
[Consumes(MediaTypeNames.Application.Json)]
[SwaggerRequestExample(
    typeof(GuardarTransaccionesDto),
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Transacciones.GuardarTransaccionesRequestMultipleExamples))]
[SwaggerResponseExample(
    StatusCodes.Status200OK,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransaccionesMultipleExamples))]
[SwaggerResponseExample(
    StatusCodes.Status400BadRequest,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransaccionesMultipleExamples))]
[SwaggerResponseExample(
    StatusCodes.Status409Conflict,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransaccionesMultipleExamples))]
[SwaggerResponseExample(
    StatusCodes.Status500InternalServerError,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransaccionesMultipleExamples))]
[ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> GuardarTransacciones([FromBody] GuardarTransaccionesDto guardarTransaccionesDto)
{
    // ... (sin cambios)
}


using Swashbuckle.AspNetCore.Filters;
// ...

builder.Services.AddSwaggerExamplesFromAssemblyOf<
    MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Transacciones.GuardarTransaccionesRequestMultipleExamples>();

builder.Services.AddSwaggerGen(options =>
{
    // ... (tu configuración actual)
    options.ExampleFilters(); // <- habilita los múltiples ejemplos en Swagger
});






