using Swashbuckle.AspNetCore.Filters;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.ExampleFilters(); // habilita [SwaggerRequestExample]/[SwaggerResponseExample]
});

// registra el assembly que contiene las clases de ejemplo
builder.Services.AddSwaggerExamplesFromAssemblyOf<
    MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.ValidarTransaccionesOkExample>();



// /Swagger/ValidarTransaccionesExamples.cs
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

// 200 OK -> No existe: se puede continuar
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

// 409 Conflict -> Ya existe: devuelve todos los datos
public sealed class ValidarTransaccionesConflictExample : IExamplesProvider<RespuestaValidarTransaccionesDto>
{
    public RespuestaValidarTransaccionesDto GetExamples() => new()
    {
        NumeroCuenta       = "123456789012345",           // 15
        MontoDebitado      = "000000000000100000",         // 18
        MontoAcreditado    = "000000000000100000",         // 18
        CodigoComercio     = "0004010210",                 // 10
        NombreComercio     = "COMERCIO DEMO S.A.",         // 60
        TerminalComercio   = "P0055468TERM001",            // 15
        Descripcion        = "Compra manual POS",          // 200
        NaturalezaContable = "DB",                         // 2
        NumeroCorte        = "01",                         // 2
        IdTransaccionUnico = "TX-20251107-000002",         // 100
        EstadoTransaccion  = "Aprobada",                   // 100
        DescripcionEstado  = "Transaccion aprobada",       // 100
        CodigoError        = "40901",
        DescripcionError   = "La transaccion ya existe (NUMCORTE, IDTRANUNI)."
    };
}

// 400 BadRequest
public sealed class ValidarTransaccionesBadRequestExample : IExamplesProvider<RespuestaValidarTransaccionesDto>
{
    public RespuestaValidarTransaccionesDto GetExamples() => new()
    {
        CodigoError      = "40001",
        DescripcionError = "Solicitud invalida: modelo DTO invalido."
    };
}

// 500 InternalServerError
public sealed class ValidarTransaccionesErrorExample : IExamplesProvider<RespuestaValidarTransaccionesDto>
{
    public RespuestaValidarTransaccionesDto GetExamples() => new()
    {
        CodigoError      = "50001",
        DescripcionError = "Error interno al validar la transaccion."
    };
}




using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Swashbuckle.AspNetCore.Filters;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.ValidarTransaccion;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger;

[HttpPost("ValidarTransacciones")]
[Consumes(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status500InternalServerError)]
[SwaggerRequestExample(typeof(ValidarTransaccionesDto), typeof(ValidarTransaccionesRequestExample))]
[SwaggerResponseExample(StatusCodes.Status200OK, typeof(ValidarTransaccionesOkExample))]
[SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ValidarTransaccionesConflictExample))]
[SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidarTransaccionesBadRequestExample))]
[SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(ValidarTransaccionesErrorExample))]
public async Task<IActionResult> ValidarTransacciones(
    [FromBody] ValidarTransaccionesDto validarTransaccionesDto,
    CancellationToken ct = default)
{
    // tu lógica actual (sin cambios)
}






