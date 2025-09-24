/// <summary>
/// Parámetros adicionales para INT_LOTES.
/// Provee la información de la cuenta interna (contra-partida) y, opcionalmente,
/// overrides de descripciones para cliente e interno.
/// </summary>
public sealed class IntLotesParamsDto
{
    // --- Lado interno (Movimiento 2) ---
    /// <summary>Tipo de cuenta interna (1=Ahorros, 6=Cheques, 40=Contable).</summary>
    public decimal? TipoCuentaInterna { get; set; }
    /// <summary>Número de cuenta interna.</summary>
    public decimal? NumeroCuentaInterna { get; set; }
    /// <summary>Centro de costo interno (p.ej., 162 para POS).</summary>
    public decimal? CentroCostoInterno { get; set; }
    /// <summary>Moneda del movimiento interno.</summary>
    public decimal? MonedaInterna { get; set; }

    // --- Descripciones (opcionales) ---
    /// <summary>Descripciones para el lado cliente.</summary>
    public string? DescripcionCliente1 { get; set; }
    public string? DescripcionCliente2 { get; set; }
    public string? DescripcionCliente3 { get; set; }

    /// <summary>Descripciones para el lado interno.</summary>
    public string? DescripcionInterna1 { get; set; }
    public string? DescripcionInterna2 { get; set; }
    public string? DescripcionInterna3 { get; set; }

    // --- Movimientos 3 y 4 (si los usas) ---
    public decimal? TipoCuenta03 { get; set; }
    public decimal? NumeroCuenta03 { get; set; }
    public decimal? Valor03 { get; set; }
    public string?  DeCr03 { get; set; }
    public decimal? CentroCosto03 { get; set; }
    public decimal? Moneda03 { get; set; }

    public decimal? TipoCuenta04 { get; set; }
    public decimal? NumeroCuenta04 { get; set; }
    public decimal? Valor04 { get; set; }
    public string?  DeCr04 { get; set; }
    public decimal? CentroCosto04 { get; set; }
    public decimal? Moneda04 { get; set; }
}



public async Task<(int CodigoErrorPosteo, string? DescripcionErrorPosteo, string? nomArc)> PosteoLoteAsync(
    IntLotesParamsDto parametros,
    decimal tipoCuenta,          // cuenta del cliente
    decimal numeroCuenta,        // cuenta del cliente
    decimal monto,
    string naturalezaContable,   // "D" o "C"
    decimal centroCosto,         // por defecto para cliente (si aplica) o interno
    decimal moneda,              // por defecto para cliente
    string perfil,
    string descripcion1,         // defaults para cliente si no hay override
    string descripcion2,
    string descripcion3
)
{
    try
    {
        var isDebCliente = naturalezaContable?.Trim().Equals("D", StringComparison.OrdinalIgnoreCase) == true;
        var isCreCliente = !isDebCliente; // solo "D" o "C"

        // ---- Cliente (Movimiento 1) ----
        var tpo01  = tipoCuenta;
        var cta01  = numeroCuenta;
        var val01  = monto;
        var dcr01  = isDebCliente ? "D" : "C";
        // normalmente CC del cliente = 0; si en tu core aplica, puedes usar "centroCosto"
        var ccos01 = 0m;
        var mon01  = moneda;

        // Descripciones lado cliente (si no hay override, usa las base)
        var desCli1 = parametros.DescripcionCliente1 ?? descripcion1;
        var desCli2 = parametros.DescripcionCliente2 ?? descripcion2;
        var desCli3 = parametros.DescripcionCliente3 ?? descripcion3;

        // ---- Interno (Movimiento 2) ----
        var tpo02  = parametros.TipoCuentaInterna  ?? 40m; // contable por defecto
        var cta02  = parametros.NumeroCuentaInterna?? 0m;  // OBL: debes enviarla
        var val02  = monto;
        var dcr02  = isDebCliente ? "C" : "D"; // contrario del cliente
        var ccos02 = parametros.CentroCostoInterno ?? centroCosto; // p.ej. 162
        var mon02  = parametros.MonedaInterna      ?? moneda;

        // Descripciones lado interno (si no hay override, reusa las del cliente)
        var desInt1 = parametros.DescripcionInterna1 ?? descripcion1;
        var desInt2 = parametros.DescripcionInterna2 ?? descripcion2;
        var desInt3 = parametros.DescripcionInterna3 ?? descripcion3;

        var builder = ProgramCallBuilder.For(_connection, "BCAH96", "INT_LOTES")
                                        .UseSqlNaming()
                                        .WrapCallWithBraces();

        // ===================== Movimiento 1 (Cliente) =====================
        builder.InDecimal("PMTIPO01", tpo01,  precision: 2,  scale: 0);
        builder.InDecimal("PMCTAA01", cta01,  precision: 13, scale: 0);
        builder.InDecimal("PMVALR01", val01,  precision: 19, scale: 8);
        builder.InChar   ("PMDECR01", dcr01,  1);
        builder.InDecimal("PMCCOS01", ccos01, precision: 5,  scale: 0);
        builder.InDecimal("PMMONE01", mon01,  precision: 3,  scale: 0);

        // ===================== Movimiento 2 (Interno) =====================
        builder.InDecimal("PMTIPO02", tpo02,  precision: 2,  scale: 0);
        builder.InDecimal("PMCTAA02", cta02,  precision: 13, scale: 0);
        builder.InDecimal("PMVALR02", val02,  precision: 19, scale: 8);
        builder.InChar   ("PMDECR02", dcr02,  1);
        builder.InDecimal("PMCCOS02", ccos02, precision: 5,  scale: 0);
        builder.InDecimal("PMMONE02", mon02,  precision: 3,  scale: 0);

        // ===================== Movimiento 3 (Opcional) ===================
        builder.InDecimal("PMTIPO03", parametros.TipoCuenta03  ?? 0m, precision: 2,  scale: 0);
        builder.InDecimal("PMCTAA03", parametros.NumeroCuenta03?? 0m, precision: 13, scale: 0);
        builder.InDecimal("PMVALR03", parametros.Valor03       ?? 0m, precision: 19, scale: 8);
        builder.InChar   ("PMDECR03", string.IsNullOrEmpty(parametros.DeCr03) ? " " : parametros.DeCr03[..1], 1);
        builder.InDecimal("PMCCOS03", parametros.CentroCosto03 ?? 0m, precision: 5,  scale: 0);
        builder.InDecimal("PMMONE03", parametros.Moneda03      ?? 0m, precision: 3,  scale: 0);

        // ===================== Movimiento 4 (Opcional) ===================
        builder.InDecimal("PMTIPO04", parametros.TipoCuenta04  ?? 0m, precision: 2,  scale: 0);
        builder.InDecimal("PMCTAA04", parametros.NumeroCuenta04?? 0m, precision: 13, scale: 0);
        builder.InDecimal("PMVALR04", parametros.Valor04       ?? 0m, precision: 19, scale: 8);
        builder.InChar   ("PMDECR04", string.IsNullOrEmpty(parametros.DeCr04) ? " " : parametros.DeCr04[..1], 1);
        builder.InDecimal("PMCCOS04", parametros.CentroCosto04 ?? 0m, precision: 5,  scale: 0);
        builder.InDecimal("PMMONE04", parametros.Moneda04      ?? 0m, precision: 3,  scale: 0);

        // ===================== Generales =====================
        builder.InChar   ("PMPERFIL", perfil, 13);
        builder.InDecimal("MONEDA",   moneda, precision: 3, scale: 0);

        // ===================== Descripciones en grupos correctos =====================
        if (isDebCliente)
        {
            // Cliente = Débito → DESDBx ; Interno = Crédito → DESCRx
            builder.InChar("DESDB1", desCli1, 40);
            builder.InChar("DESDB2", desCli2, 40);
            builder.InChar("DESDB3", desCli3, 40);
            builder.InChar("DESCR1", desInt1, 40);
            builder.InChar("DESCR2", desInt2, 40);
            builder.InChar("DESCR3", desInt3, 40);
        }
        else
        {
            // Cliente = Crédito → DESCRx ; Interno = Débito → DESDBx
            builder.InChar("DESCR1", desCli1, 40);
            builder.InChar("DESCR2", desCli2, 40);
            builder.InChar("DESCR3", desCli3, 40);
            builder.InChar("DESDB1", desInt1, 40);
            builder.InChar("DESDB2", desInt2, 40);
            builder.InChar("DESDB3", desInt3, 40);
        }

        // ===================== OUTS =====================
        builder.OutDecimal("CODER", 2, 0);
        builder.OutChar   ("DESERR", 70);
        builder.OutChar   ("NomArc", 10);

        var result = await builder.CallAsync(_contextAccessor.HttpContext);
        result.TryGet("CODER",  out int    codigoError);
        result.TryGet("DESERR", out string? descripcionError);
        result.TryGet("NomArc", out string? nomArc);

        return (codigoError, descripcionError, nomArc);
    }
    catch (Exception ex)
    {
        return (-1, "Error general en PosteoLoteAsync: " + ex.Message, "");
    }
}
