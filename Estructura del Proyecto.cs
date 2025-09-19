using Connections.Abstractions;
using Connections.Helpers;
using Microsoft.AspNetCore.Http;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;

/// <summary>
/// Servicio unificado para ejecutar programas RPG (APACD764 o APACD767),
/// con todos los parámetros escritos explícitamente.
/// </summary>
public class ApacdCaller
{
    private readonly IDatabaseConnection _connection;
    private readonly IHttpContextAccessor _contextAccessor;

    public ApacdCaller(IDatabaseConnection connection, IHttpContextAccessor contextAccessor)
    {
        _connection = connection;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Ejecuta un programa RPG (ej. APACD764 o APACD767) con los 31 parámetros exactos.
    /// </summary>
    /// <param name="programa">Nombre del programa RPG (ej. "APACD764" o "APACD767").</param>
    /// <param name="perfil">Perfil transerver.</param>
    /// <param name="moneda">Código de moneda.</param>
    /// <param name="descripcion1">Leyenda 1.</param>
    /// <param name="descripcion2">Leyenda 2.</param>
    /// <param name="descripcion3">Leyenda 3.</param>
    /// <param name="descripcion4">Leyenda 4.</param>
    /// <param name="tasaAtm">Tasa ATM (15,9).</param>
    /// <returns>(CodigoError, DescripcionError)</returns>
    public async Task<(int CodigoError, string? DescripcionError)> EjecutarProgramaAsync(
        string programa,
        string perfil,
        int moneda,
        string descripcion1,
        string descripcion2,
        string descripcion3,
        string descripcion4,
        decimal tasaAtm
    )
    {
        var builder = ProgramCallBuilder.For(_connection, "BCAH96DTA", programa)
            .UseSqlNaming()
            .WrapCallWithBraces();

        // ===================== Movimiento 1 =====================
        builder.InDecimal("PMTIPO01", 0, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA01", 0, precision: 13, scale: 0);
        builder.InDecimal("PMVALR01", 0, precision: 19, scale: 8);
        builder.InChar("PMDECR01", "", 1);
        builder.InDecimal("PMCCOS01", 0, precision: 5, scale: 0);
        builder.InDecimal("PMMONE01", 0, precision: 3, scale: 0);

        // ===================== Movimiento 2 =====================
        builder.InDecimal("PMTIPO02", 0, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA02", 0, precision: 13, scale: 0);
        builder.InDecimal("PMVALR02", 0, precision: 19, scale: 8);
        builder.InChar("PMDECR02", "", 1);
        builder.InDecimal("PMCCOS02", 0, precision: 5, scale: 0);
        builder.InDecimal("PMMONE02", 0, precision: 3, scale: 0);

        // ===================== Movimiento 3 =====================
        builder.InDecimal("PMTIPO03", 0, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA03", 0, precision: 13, scale: 0);
        builder.InDecimal("PMVALR03", 0, precision: 19, scale: 8);
        builder.InChar("PMDECR03", "", 1);
        builder.InDecimal("PMCCOS03", 0, precision: 5, scale: 0);
        builder.InDecimal("PMMONE03", 0, precision: 3, scale: 0);

        // ===================== Movimiento 4 =====================
        builder.InDecimal("PMTIPO04", 0, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA04", 0, precision: 13, scale: 0);
        builder.InDecimal("PMVALR04", 0, precision: 19, scale: 8);
        builder.InChar("PMDECR04", "", 1);
        builder.InDecimal("PMCCOS04", 0, precision: 5, scale: 0);
        builder.InDecimal("PMMONE04", 0, precision: 3, scale: 0);

        // ===================== Generales =====================
        builder.InChar("PMPERFIL", perfil, 13);
        builder.InDecimal("MONEDA", moneda, precision: 3, scale: 0);
        builder.InChar("DES001", descripcion1, 40);
        builder.InChar("DES002", descripcion2, 40);
        builder.InChar("DES003", descripcion3, 40);
        builder.InChar("DES004", descripcion4, 40);
        builder.InDecimal("TASATM", tasaAtm, precision: 15, scale: 9);

        // ===================== OUT =====================
        builder.OutDecimal("CODER", 2, 0);
        builder.OutChar("DESERR", 70);

        var result = await builder.CallAsync(_contextAccessor.HttpContext);

        result.TryGet("CODER", out int codigoError);
        result.TryGet("DESERR", out string? descripcionError);

        return (codigoError, descripcionError);
    }
}
