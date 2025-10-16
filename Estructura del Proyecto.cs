using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using Swashbuckle.AspNetCore.Filters;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common;

/// <summary>
/// Ejemplo único para <b>200 OK</b> usando <see cref="RespuestaGuardarTransaccionesDto"/>.
/// Se modela el caso de éxito/idempotencia aceptada por las reglas de negocio.
/// </summary>
public sealed class RespuestaGuardarTransacciones200Example() : IExamplesProvider<RespuestaGuardarTransaccionesDto>
{
    /// <summary>
    /// Devuelve un ejemplo canónico de éxito.
    /// </summary>
    public RespuestaGuardarTransaccionesDto GetExamples() => new()
    {
        CodigoError = "00000",                 // ← código de negocio de éxito
        DescripcionError = "Transacción registrada correctamente" // ← mensaje humano sin PII
    };
}





using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using Swashbuckle.AspNetCore.Filters;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common;

/// <summary>
/// Ejemplo único para <b>400 BadRequest</b> usando <see cref="RespuestaGuardarTransaccionesDto"/>.
/// Representa errores de entrada/validación semántica del DTO.
/// </summary>
public sealed class RespuestaGuardarTransacciones400Example() : IExamplesProvider<RespuestaGuardarTransaccionesDto>
{
    /// <summary>Devuelve un ejemplo de solicitud inválida.</summary>
    public RespuestaGuardarTransaccionesDto GetExamples() => new()
    {
        CodigoError = "40000",                         // ← tu mapeo BizCodes→HTTP
        DescripcionError = "Solicitud inválida, modelo DTO, invalido."
    };
}




using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using Swashbuckle.AspNetCore.Filters;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common;

/// <summary>
/// Ejemplo único para <b>409 Conflict</b> usando <see cref="RespuestaGuardarTransaccionesDto"/>.
/// Enfocado en duplicidad por idempotencia (<c>idTransaccionUnico</c>).
/// </summary>
public sealed class RespuestaGuardarTransacciones409Example() : IExamplesProvider<RespuestaGuardarTransaccionesDto>
{
    /// <summary>Devuelve un ejemplo de conflicto por idempotencia.</summary>
    public RespuestaGuardarTransaccionesDto GetExamples() => new()
    {
        CodigoError = "40901",
        DescripcionError = "Transacción previamente registrada con el mismo idTransaccionUnico"
    };
}




using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using Swashbuckle.AspNetCore.Filters;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common;

/// <summary>
/// Ejemplo único para <b>500 InternalServerError</b> usando <see cref="RespuestaGuardarTransaccionesDto"/>.
/// Representa una falla inesperada del servidor (no exponer detalles internos).
/// </summary>
public sealed class RespuestaGuardarTransacciones500Example() : IExamplesProvider<RespuestaGuardarTransaccionesDto>
{
    /// <summary>Devuelve un ejemplo de error interno genérico.</summary>
    public RespuestaGuardarTransaccionesDto GetExamples() => new()
    {
        CodigoError = "50099",
        DescripcionError = "Ha ocurrido un error inesperado al procesar la solicitud"
    };
}

using Swashbuckle.AspNetCore.Filters;
// ...

[HttpPost("GuardarTransacciones")]
[Consumes(MediaTypeNames.Application.Json)]
// Request puede seguir con múltiples ejemplos si quieres mantener el selector:
[SwaggerRequestExample(
    typeof(GuardarTransaccionesDto),
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Transacciones.GuardarTransaccionesRequestMultipleExamples))]

// ✅ Un ejemplo por status (sin selector múltiple en Swagger)
[SwaggerResponseExample(
    StatusCodes.Status200OK,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransacciones200Example))]
[SwaggerResponseExample(
    StatusCodes.Status400BadRequest,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransacciones400Example))]
[SwaggerResponseExample(
    StatusCodes.Status409Conflict,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransacciones409Example))]
[SwaggerResponseExample(
    StatusCodes.Status500InternalServerError,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransacciones500Example))]
public async Task<IActionResult> GuardarTransacciones([FromBody] GuardarTransaccionesDto guardarTransaccionesDto)
{
    // ... tu lógica actual sin cambios
}



