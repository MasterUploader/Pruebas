public async Task<(int CodigoErrorPosteo, string? DescripcionErrorPosteo, string? nomArc)> PosteoLoteAsync(
    IntLotesParamsDto parametros,
    decimal tipoCuenta,          // CUENTA DEL CLIENTE
    decimal numeroCuenta,        // CUENTA DEL CLIENTE
    decimal monto,
    string naturalezaContable,   // "D" o "C"
    decimal centroCosto,         // por defecto (p.ej., 162)
    decimal moneda,              // por defecto
    string perfil,
    string descripcion1,         // texto base cliente
    string descripcion2,
    string descripcion3
)
{
    try
    {
        bool isDebCliente = naturalezaContable?.Trim()
                             .Equals("D", StringComparison.OrdinalIgnoreCase) == true;

        // ---- Datos lado interno (con defaults si no vienen) ----
        decimal tpoInterno  = parametros.TipoCuentaInterna  ?? 40m;  // 40=contable por defecto
        decimal ctaInterna  = parametros.NumeroCuentaInterna?? 0m;   // DEBE VENIR para postear
        decimal ccosInterno = parametros.CentroCostoInterno ?? centroCosto;
        decimal monInterno  = parametros.MonedaInterna      ?? moneda;

        // ---- Descripciones (permite overrides desde el DTO) ----
        string cli1 = parametros.DescripcionCliente1 ?? descripcion1;
        string cli2 = parametros.DescripcionCliente2 ?? descripcion2;
        string cli3 = parametros.DescripcionCliente3 ?? descripcion3;

        string int1 = parametros.DescripcionInterna1 ?? descripcion1;
        string int2 = parametros.DescripcionInterna2 ?? descripcion2;
        string int3 = parametros.DescripcionInterna3 ?? descripcion3;

        var builder = ProgramCallBuilder.For(_connection, "BCAH96", "INT_LOTES")
                                        .UseSqlNaming()
                                        .WrapCallWithBraces();

        if (isDebCliente)
        {
            // =========================================================
            // NATURALEZA = "D"  → Cliente DEBITO / Interno CREDITO
            //  Mov.1 = Cliente (D)
            //  Mov.2 = Interno (C)
            //  DESDBx = Cliente   (debito)
            //  DESCRx = Internas  (crédito)
            // =========================================================

            // ---------- Movimiento 1 (Cliente - Débito) ----------
            builder.InDecimal("PMTIPO01", tipoCuenta,   precision: 2,  scale: 0);
            builder.InDecimal("PMCTAA01", numeroCuenta, precision: 13, scale: 0);
            builder.InDecimal("PMVALR01", monto,        precision: 13, scale: 2);
            builder.InChar   ("PMDECR01", "D",          1);
            builder.InDecimal("PMCCOS01", 0m,           precision: 5,  scale: 0);
            builder.InDecimal("PMMONE01", moneda,       precision: 3,  scale: 0);

            // ---------- Movimiento 2 (Interno - Crédito) ----------
            builder.InDecimal("PMTIPO02", tpoInterno,   precision: 2,  scale: 0);
            builder.InDecimal("PMCTAA02", ctaInterna,   precision: 13, scale: 0);
            builder.InDecimal("PMVALR02", monto,        precision: 13, scale: 2);
            builder.InChar   ("PMDECR02", "C",          1);
            builder.InDecimal("PMCCOS02", ccosInterno,  precision: 5,  scale: 0);
            builder.InDecimal("PMMONE02", monInterno,   precision: 3,  scale: 0);

            // ---------- Descripciones ----------
            // Nuevas (DESDBx) = las del débito → cliente
            builder.InChar("DESDB1", cli1, 40);
            builder.InChar("DESDB2", cli2, 40);
            builder.InChar("DESDB3", cli3, 40);

            // Originales (DESCRx) = las del crédito → interno
            builder.InChar("DESCR1", int1, 40);
            builder.InChar("DESCR2", int2, 40);
            builder.InChar("DESCR3", int3, 40);
        }
        else
        {
            // =========================================================
            // NATURALEZA = "C"  → Cliente CREDITO / Interno DEBITO
            //  Mov.1 = Interno (D)
            //  Mov.2 = Cliente (C)
            //  DESDBx = Internas (debito)
            //  DESCRx = Cliente  (crédito)
            // =========================================================

            // ---------- Movimiento 1 (Interno - Débito) ----------
            builder.InDecimal("PMTIPO01", tpoInterno,   precision: 2,  scale: 0);
            builder.InDecimal("PMCTAA01", ctaInterna,   precision: 13, scale: 0);
            builder.InDecimal("PMVALR01", monto,        precision: 13, scale: 2);
            builder.InChar   ("PMDECR01", "D",          1);
            builder.InDecimal("PMCCOS01", ccosInterno,  precision: 5,  scale: 0);
            builder.InDecimal("PMMONE01", monInterno,   precision: 3,  scale: 0);

            // ---------- Movimiento 2 (Cliente - Crédito) ----------
            builder.InDecimal("PMTIPO02", tipoCuenta,   precision: 2,  scale: 0);
            builder.InDecimal("PMCTAA02", numeroCuenta, precision: 13, scale: 0);
            builder.InDecimal("PMVALR02", monto,        precision: 13, scale: 2);
            builder.InChar   ("PMDECR02", "C",          1);
            builder.InDecimal("PMCCOS02", centroCosto,  precision: 5,  scale: 0);
            builder.InDecimal("PMMONE02", moneda,       precision: 3,  scale: 0);

            // ---------- Descripciones ----------
            // Nuevas (DESDBx) = las del débito → internas
            builder.InChar("DESDB1", int1, 40);
            builder.InChar("DESDB2", int2, 40);
            builder.InChar("DESDB3", int3, 40);

            // Originales (DESCRx) = las del crédito → cliente
            builder.InChar("DESCR1", cli1, 40);
            builder.InChar("DESCR2", cli2, 40);
            builder.InChar("DESCR3", cli3, 40);
        }

        // ===================== Generales y OUT =====================
        builder.InChar   ("PMPERFIL", perfil, 13);
        builder.InDecimal("MONEDA",   moneda, precision: 3, scale: 0);

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
