using Connections.Abstractions;
using RestUtilities.QueryBuilder;
using System.Data.Common;

namespace Adquirencia.Services;

/// <summary>
/// DTO que modela la tabla BNKPRD01.TAP001 para consultas tipadas.
/// </summary>
public class TAP001
{
    /// <summary>Clave de banco (NUMERIC 3).</summary>
    public int DSBK { get; set; }

    /// <summary>
    /// Fecha de sistema en formato IBM i CYYMMDD (NUMERIC 7).
    /// C = 0 => 19xx, C = 1 => 20xx.
    /// </summary>
    public int DSCDT { get; set; }
}

/// <summary>
/// Servicio con la lógica equivalente a la subrutina RPGLE VerFecha.
/// </summary>
public class FechaService(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor)
{
    /// <summary>
    /// Lee DSCDT desde BNKPRD01.TAP001 (DSBK=001) y retorna:
    /// - el valor bruto DSCDT (CYYMMDD)
    /// - la fecha formateada YYYYMMDD
    /// </summary>
    /// <returns>found, dscdtCyyMmDd, yyyyMMdd</returns>
    public (bool found, int dscdt, string yyyyMMdd) VerFecha()
    {
        // Valores de salida predeterminados para conservar contrato estable.
        var dscdt = 0;            // valor crudo CYYMMDD
        var yyyyMMdd = string.Empty;

        try
        {
            _connection.Open();

            // SELECT DSCDT FROM BNKPRD01.TAP001 WHERE DSBK = 1 FETCH FIRST 1 ROW ONLY
            // - Se usa DTO para habilitar lambdas tipadas y evitar cadenas mágicas.
            var query = QueryBuilder.Core.QueryBuilder
                .From("TAP001", "BNKPRD01")
                .Select<TAP001>(x => x.DSCDT)             // solo la columna necesaria
                .Where<TAP001>(x => x.DSBK == 1)          // DSBK = 001 en RPGLE
                .FetchNext(1)                              // equivalente a CHAIN + %FOUND
                .Build();

            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = query.Sql;

            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                return (false, dscdt, yyyyMMdd);

            // Lectura directa: índice 0 porque solo seleccionamos DSCDT.
            dscdt = rd.GetInt32(0);

            // Conversión de CYYMMDD → YYYYMMDD para uso homogéneo en .NET/SQL.
            yyyyMMdd = ConvertCyyMmDdToYyyyMmDd(dscdt);

            return (true, dscdt, yyyyMMdd);
        }
        finally
        {
            _connection.Close();
        }
    }

    /// <summary>
    /// Convierte un entero en formato IBM i CYYMMDD (p. ej. 1240912) a "YYYYMMDD".
    /// </summary>
    /// <remarks>
    /// - C: siglo relativo a 1900 (0=>1900, 1=>2000, etc.).  
    /// - YY: año dentro del siglo.  
    /// - MM: mes, DD: día.
    /// </remarks>
    private static string ConvertCyyMmDdToYyyyMmDd(int cyymmdd)
    {
        // Separación de C, YY, MM, DD usando división/módulo para evitar parseos de string.
        var c  =  cyymmdd / 1000000;                 // dígito del siglo
        var yy = (cyymmdd / 10000) % 100;            // dos dígitos de año
        var mm = (cyymmdd / 100)   % 100;            // mes
        var dd =  cyymmdd          % 100;            // día

        // Año absoluto: 1900 + (C * 100) + YY. Para C=1 => 2000+YY.
        var yyyy = 1900 + (c * 100) + yy;

        // Composición sin separadores para uso en sistemas que requieren 8 caracteres.
        return $"{yyyy:0000}{mm:00}{dd:00}";
    }
}
