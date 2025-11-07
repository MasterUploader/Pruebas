using Swashbuckle.AspNetCore.Filters;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.ExampleFilters(); // <-- habilita ejemplos
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<
    MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.ValidarTransaccionesOkExample>();



using Swashbuckle.AspNetCore.Filters;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.ValidarTransaccion;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger;

// ====== Request ======
public sealed class ValidarTransaccionesRequestExample : IExamplesProvider<ValidarTransaccionesDto>
{
    public ValidarTransaccionesDto GetExamples() => new()
    {
        IdTransaccionUnico = "TX-20251107-000002",
        NumeroDeCorte      = "01"
    };
}

// ====== Responses ======

// 200 OK -> No existe, se puede continuar (incluye llaves y mensaje)
public sealed class ValidarTransaccionesOkExample : IExamplesProvider<RespuestaValidarTransaccionesDto>
{
    public RespuestaValidarTransaccionesDto GetExamples() => new()
    {
        NumeroCorte        = "01",
        IdTransaccionUnico = "TX-20251107-000002",
        CodigoError        = "00000",
        DescripcionError   = "No existe el registro. Puede continuar."
    };
}

// 409 Conflict -> Ya existe: devuelve TODOS los datos del registro
public sealed class ValidarTransaccionesConflictExample : IExamplesProvider<RespuestaValidarTransaccionesDto>
{
    public RespuestaValidarTransaccionesDto GetExamples() => new()
    {
        NumeroCuenta       = "123456789012345",          // 15
        MontoDebitado      = "000000000000100000",        // 18
        MontoAcreditado    = "000000000000100000",        // 18
        CodigoComercio     = "0004010210",                // 10
        NombreComercio     = "COMERCIO DEMO S.A.",        // 60
        TerminalComercio   = "P0055468TERM001",           // 15
        Descripcion        = "Compra manual POS",         // 200
        NaturalezaContable = "DB",                        // 2
        NumeroCorte        = "01",                        // 2
        IdTransaccionUnico = "TX-20251107-000002",        // 100
        EstadoTransaccion  = "Aprobada",                  // 100
        DescripcionEstado  = "Transaccion aprobada",      // 100
        CodigoError        = "40901",                     // BizCode 5 dígitos
        DescripcionError   = "La transaccion ya existe (NUMCORTE, IDTRANUNI)."
    };
}

// 400 BadRequest -> Error de validacion de entrada
public sealed class ValidarTransaccionesBadRequestExample : IExamplesProvider<RespuestaValidarTransaccionesDto>
{
    public RespuestaValidarTransaccionesDto GetExamples() => new()
    {
        CodigoError      = "40001",
        DescripcionError = "Solicitud invalida: campos requeridos faltantes o formato incorrecto."
    };
}

// 500 InternalServerError -> Error no controlado
public sealed class ValidarTransaccionesErrorExample : IExamplesProvider<RespuestaValidarTransaccionesDto>
{
    public RespuestaValidarTransaccionesDto GetExamples() => new()
    {
        CodigoError      = "50001",
        DescripcionError = "Error interno al validar la transaccion."
    };
}





using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger;

[HttpPost("ValidarTransacciones")]
[Consumes(MediaTypeNames.Application.Json)]
[SwaggerRequestExample(typeof(ValidarTransaccionesDto), typeof(ValidarTransaccionesRequestExample))]
[SwaggerResponse(StatusCodes.Status200OK,    "No existe: puede continuar.", typeof(RespuestaValidarTransaccionesDto))]
[SwaggerResponseExample(StatusCodes.Status200OK,    typeof(ValidarTransaccionesOkExample))]
[SwaggerResponse(StatusCodes.Status409Conflict, "Duplicado: ya existe.",     typeof(RespuestaValidarTransaccionesDto))]
[SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ValidarTransaccionesConflictExample))]
[SwaggerResponse(StatusCodes.Status400BadRequest, "Solicitud invalida.",     typeof(RespuestaValidarTransaccionesDto))]
[SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidarTransaccionesBadRequestExample))]
[SwaggerResponse(StatusCodes.Status500InternalServerError, "Error interno.", typeof(RespuestaValidarTransaccionesDto))]
[SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(ValidarTransaccionesErrorExample))]
public async Task<IActionResult> ValidarTransacciones(
    [FromBody] ValidarTransaccionesDto validarTransaccionesDto, CancellationToken ct = default)
{
    // ... tu logica actual (sin cambios) ...
}
