using Swashbuckle.AspNetCore.Filters;

builder.Services.AddSwaggerGen(c =>
{
    c.ExampleFilters();
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<
    MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.ValidarTransaccionesRequestExample>();



// /Swagger/ValidarTransaccionesRequestExample.cs
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Filters;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.ValidarTransaccion;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger
{
    /// <summary>Ejemplos nombrados para el request de ValidarTransacciones.</summary>
    public sealed class ValidarTransaccionesRequestExample
        : IMultipleExamplesProvider<ValidarTransaccionesDto>
        // Si hay choque de nombres, usa:
        // : Swashbuckle.AspNetCore.Filters.IMultipleExamplesProvider<ValidarTransaccionesDto>
    {
        public IEnumerable<SwaggerExample<ValidarTransaccionesDto>> GetExamples()
        // Si hay choque de nombres, usa:
        // public IEnumerable<Swashbuckle.AspNetCore.Filters.SwaggerExample<ValidarTransaccionesDto>> GetExamples()
        {
            // 1) Valor duplicado
            yield return new SwaggerExample<ValidarTransaccionesDto>
            {
                Name  = "Valor duplicado",
                Value = new ValidarTransaccionesDto
                {
                    IdTransaccionUnico = "TX-20251107-000002",
                    NumeroDeCorte      = "01"
                }
            };

            // 2) Valor no duplicado
            yield return new SwaggerExample<ValidarTransaccionesDto>
            {
                Name  = "Valor no duplicado",
                Value = new ValidarTransaccionesDto
                {
                    IdTransaccionUnico = "TX-20251107-000002",
                    NumeroDeCorte      = "02"
                }
            };
        }
    }
}



// /Swagger/ValidarTransaccionesResponseExamples.cs
using Swashbuckle.AspNetCore.Filters;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.ValidarTransaccion;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger
{
    // 200 OK: no existe -> se puede continuar
    public sealed class ValidarTransaccionesOkExample : IExamplesProvider<RespuestaValidarTransaccionesDto>
    {
        public RespuestaValidarTransaccionesDto GetExamples() => new()
        {
            NumeroCorte        = "02",
            IdTransaccionUnico = "TX-20251107-000002",
            CodigoError        = "00000",
            DescripcionError   = "No existe el registro. Puede continuar."
        };
    }

    // 409 Conflict: existe -> devuelve todos los datos
    public sealed class ValidarTransaccionesConflictExample : IExamplesProvider<RespuestaValidarTransaccionesDto>
    {
        public RespuestaValidarTransaccionesDto GetExamples() => new()
        {
            NumeroCuenta       = "123456789012345",
            MontoDebitado      = "000000000000100000",
            MontoAcreditado    = "000000000000100000",
            CodigoComercio     = "0004010210",
            NombreComercio     = "COMERCIO DEMO S.A.",
            TerminalComercio   = "P0055468TERM001",
            Descripcion        = "Compra manual POS",
            NaturalezaContable = "DB",
            NumeroCorte        = "01",
            IdTransaccionUnico = "TX-20251107-000002",
            EstadoTransaccion  = "Aprobada",
            DescripcionEstado  = "Transaccion aprobada",
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
}



using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger;

[HttpPost("ValidarTransacciones")]
[Consumes(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status500InternalServerError)]
[SwaggerRequestExample(typeof(ValidarTransaccionesDto), typeof(ValidarTransaccionesRequestExample))]
[SwaggerResponseExample(StatusCodes.Status200OK,    typeof(ValidarTransaccionesOkExample))]
[SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(ValidarTransaccionesConflictExample))]
[SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidarTransaccionesBadRequestExample))]
[SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(ValidarTransaccionesErrorExample))]
public async Task<IActionResult> ValidarTransacciones(
    [FromBody] ValidarTransaccionesDto validarTransaccionesDto, CancellationToken ct = default)
{
    // ... tu lógica actual ...
}



