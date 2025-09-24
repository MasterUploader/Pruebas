No, el cambio seria en este método agregue por ejemplo IntLotesParamsDto, para recibir los otros datos, ahora se debe colocar los datos segun corresponda, en credito o debito.


/// <summary>
/// Ejecuta un programa RPG INT_LOTES con los 35 parámetros exactos.
/// </summary>
///<param name="tipoCuenta">Tipo de cuenta (1-ahorros/6-cheques/40-Contable).</param>
///<param name="numeroCuenta">Número de Cuenta a Debitar/Acredita.r</param>
///<param name="monto">Monto a Debitar/Acreditar.</param>
///<param name="naturalezaContable">Naturaleza Contable Debito o Credito  D ó C.</param>
///<param name="centroCosto">Centro de costo (162 para POS).</param>
/// <param name="perfil">Perfil transerver.</param>
/// <param name="moneda">Código de moneda.</param>
/// <param name="descripcion1">Leyenda 1.</param>
/// <param name="descripcion2">Leyenda 2.</param>
/// <param name="descripcion3">Leyenda 3.</param>
/// <returns>(CodigoError, DescripcionError)</returns>
public async Task<(int CodigoErrorPosteo, string? DescripcionErrorPosteo, string? nomArc)> PosteoLoteAsync(
    IntLotesParamsDto parametros,        
    decimal tipoCuenta,
    decimal numeroCuenta,
    decimal monto,
    string naturalezaContable,
    decimal centroCosto,
    decimal moneda,
    string perfil,
    string descripcion1,
    string descripcion2,
    string descripcion3
)
{
    try
    {
        var builder = ProgramCallBuilder.For(_connection, "BCAH96", "INT_LOTES")
        .UseSqlNaming()
        .WrapCallWithBraces();

        // ===================== Movimiento 1 =====================
        builder.InDecimal("PMTIPO01", tipoCuenta, precision: 2, scale: 0); //Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO01
        builder.InDecimal("PMCTAA01", naturalezaContable.Contains('D') ? numeroCuenta : 0, precision: 13, scale: 0); //Numero de cuenta a Debitar = PMCTAA01
        builder.InDecimal("PMVALR01", naturalezaContable.Contains('D') ? monto : 0m, precision: 13, scale: 2); //Valor segun moneda (lps=lps, Usd=Usd Eur=Eur)
        builder.InChar("PMDECR01", "D", 1); //Tipo de movimiento C=Credito D=Debito
        builder.InDecimal("PMCCOS01", 0, precision: 5, scale: 0); //Centro de costos
        builder.InDecimal("PMMONE01", moneda, precision: 3, scale: 0); //Moneda del movimiento

        // ===================== Movimiento 2 =====================
        builder.InDecimal("PMTIPO02", naturalezaContable.Contains('C') ? tipoCuenta : 0, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA02", naturalezaContable.Contains('C') ? numeroCuenta : 0, precision: 13, scale: 0); //Numero de cuenta a Acreditar = PMCTAA02
        builder.InDecimal("PMVALR02", naturalezaContable.Contains('C') ? monto : 0m, precision: 13, scale: 2);
        builder.InChar("PMDECR02", "C", 1);
        builder.InDecimal("PMCCOS02", naturalezaContable.Contains('C') ? centroCosto : 0, precision: 5, scale: 0);
        builder.InDecimal("PMMONE02", moneda, precision: 3, scale: 0); //Moneda del movimiento

        // ===================== Movimiento 3 =====================
        builder.InDecimal("PMTIPO03", 0m, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA03", 0m, precision: 13, scale: 0);
        builder.InDecimal("PMVALR03", 0m, precision: 13, scale: 2);
        builder.InChar("PMDECR03", "", 1);
        builder.InDecimal("PMCCOS03", 0m, precision: 5, scale: 0);
        builder.InDecimal("PMMONE03", 0m, precision: 3, scale: 0); //Moneda del movimiento

        // ===================== Movimiento 4 =====================
        builder.InDecimal("PMTIPO04", 0m, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA04", 0m, precision: 13, scale: 0);
        builder.InDecimal("PMVALR04", 0m, precision: 13, scale: 2);
        builder.InChar("PMDECR04", "", 1);
        builder.InDecimal("PMCCOS04", 0m, precision: 5, scale: 0);
        builder.InDecimal("PMMONE04", 0m, precision: 3, scale: 0); //Moneda del movimiento

        // ===================== Generales =====================
        builder.InChar("PMPERFIL", perfil, 13); //Perfil transerver
        builder.InDecimal("MONEDA", moneda, precision: 3, scale: 0);

        // ===================== Descripciones Nuevas =====================
        builder.InChar("DESDB1", naturalezaContable.Contains('D') ? descripcion1 : "", 40); //Descripción 1
        builder.InChar("DESDB2", naturalezaContable.Contains('D') ? descripcion2 : "", 40); //Descripción 2
        builder.InChar("DESDB3", naturalezaContable.Contains('D') ? descripcion3 : "", 40); //Descripción 3

        // ===================== Descripciones Originales =====================
        builder.InChar("DESCR1", naturalezaContable.Contains('C') ? descripcion1 : "", 40); //Descripción 1
        builder.InChar("DESCR2", naturalezaContable.Contains('C') ? descripcion2 : "", 40); //Descripción 2
        builder.InChar("DESCR3", naturalezaContable.Contains('C') ? descripcion3 : "", 40); //Descripción 3

        // ===================== OUT =====================
        builder.OutDecimal("CODER", 2, 0);
        builder.OutChar("DESERR", 70);
        builder.OutChar("NomArc", 10);

        var result = await builder.CallAsync(_contextAccessor.HttpContext);

        result.TryGet("CODER", out int codigoError);
        result.TryGet("DESERR", out string? descripcionError);
        result.TryGet("NomArc", out string? nomArc);

        return (codigoError, descripcionError, nomArc);
    }
    catch (Exception ex)
    {
        return (-1, "Error general en PosteoLoteAsync: " + ex.Message, "");
    }
}
