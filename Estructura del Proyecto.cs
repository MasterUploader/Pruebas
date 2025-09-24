// ======================================================================
// Helpers repuestos: VerCta, Trunc, EtiquetaConcepto, ObtenerTasaCompraUsd
// ======================================================================

/// <summary>
/// Resultado de <see cref="VerCta"/>: clasificación mínima de la cuenta
/// tal como la usa el RPG para armar descripciones.
/// </summary>
private sealed class VerCtaResult
{
    /// <summary>Etiqueta corta que se imprime en AL3 (p.ej. "AHO" o "CHE").</summary>
    public string DescCorta { get; init; } = string.Empty;

    /// <summary>Descripción extendida opcional (no obligatoria).</summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>True si se considera cuenta de ahorro.</summary>
    public bool EsAhorro { get; init; }

    /// <summary>True si se considera cuenta de cheques.</summary>
    public bool EsCheques { get; init; }
}

/// <summary>
/// Emula la lógica del procedimiento RPG <c>Ver_cta</c> para distinguir
/// entre Ahorros/Cheques y devolver la etiqueta corta usada en AL3.
/// 
/// Nota: si no conoces aún el mapeo exacto por producto/tablas, esta versión
/// aplica una heurística segura (prefijo) y, si quieres, aquí puedes
/// reemplazar por una consulta a BNKPRD01.TAP002 cuando tengas las columnas
/// definitivas.
/// </summary>
/// <param name="numeroCuenta">Cuenta del cliente/comercio.</param>
private VerCtaResult VerCta(string numeroCuenta)
{
    if (string.IsNullOrWhiteSpace(numeroCuenta))
        return new VerCtaResult { DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };

    var cta = numeroCuenta.Trim();

    // ——————————————————————————————————————————————————————————————
    // Heurística genérica: ajusta aquí a tus reglas reales de Ver_cta
    // (por ejemplo, por familia de producto, sucursal, dígitos, etc.)
    // ——————————————————————————————————————————————————————————————
    // Ejemplo de fallback:
    //  - Prefijos 01/03/40 → Ahorros
    //  - Prefijos 02/04/41 → Cheques
    var pref2 = cta.Length >= 2 ? cta[..2] : cta;

    if (pref2 is "01" or "03" or "40")
        return new VerCtaResult { DescCorta = "AHO", Descripcion = "Ahorros", EsAhorro = true, EsCheques = false };

    if (pref2 is "02" or "04" or "41")
        return new VerCtaResult { DescCorta = "CHE", Descripcion = "Cheques", EsAhorro = false, EsCheques = true };

    // Si no se pudo determinar, no bloquees el proceso: deja vacío.
    return new VerCtaResult { DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };
}

/// <summary>
/// Trunca una cadena a <paramref name="max"/> caracteres (segura para null).
/// </summary>
private static string Trunc(string? s, int max)
{
    if (string.IsNullOrEmpty(s)) return string.Empty;
    if (max <= 0) return string.Empty;
    return s.Length <= max ? s : s[..max];
}

/// <summary>
/// Devuelve la etiqueta de concepto que el RPG imprime en AL3:
/// "CR" para naturaleza Crédito ("C") y "DB" para Débito ("D").
/// </summary>
private static string EtiquetaConcepto(string? naturaleza)
{
    if (string.IsNullOrWhiteSpace(naturaleza)) return "";
    return naturaleza.Trim().Equals("C", StringComparison.OrdinalIgnoreCase) ? "CR" : "DB";
}

/// <summary>
/// Obtiene la tasa de compra USD (si aplica). Si aún no tienes
/// la tabla/fuente, devuelve 0 sin detener el proceso.
/// </summary>
private decimal ObtenerTasaCompraUsd()
{
    try
    {
        // Si tienes tabla de tasas, reemplaza este bloque por tu SELECT real.
        // Ejemplo ilustrativo (ajusta nombres reales):
        // var q = QueryBuilder.Core.QueryBuilder
        //     .From("TASAS", "BNKPRD01")
        //     .Select("TASA_COMPRA_USD")
        //     .OrderBy("FECHA", QueryBuilder.Enums.SortDirection.Desc)
        //     .FetchNext(1)
        //     .Build();
        //
        // using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        // var obj = cmd.ExecuteScalar();
        // return obj is null || obj is DBNull ? 0m : Convert.ToDecimal(obj, CultureInfo.InvariantCulture);

        return 0m; // fallback seguro
    }
    catch
    {
        return 0m; // nunca rompas el flujo por tasa
    }
}
