Así tengo el llamado en C#, estos son los campos que necesito recibir para que se procese el lote:

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
public async Task<(int CodigoErrorPosteo, string? DescripcionErrorPosteo)> PosteoLoteAsync(
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

        CargaLibrerias(); // Asegura que las librerías necesarias estén en el entorno

        var builder = ProgramCallBuilder.For(_connection, "BCAH96", "INT_LOTES")
        .UseSqlNaming()
        .WrapCallWithBraces();

        // ===================== Movimiento 1 =====================
        builder.InDecimal("PMTIPO01", naturalezaContable.Contains('D') ? tipoCuenta : 0, precision: 2, scale: 0); //Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO01
        builder.InDecimal("PMCTAA01", naturalezaContable.Contains('D') ? numeroCuenta : 0, precision: 13, scale: 0); //Numero de cuenta a Debitar = PMCTAA01
        builder.InDecimal("PMVALR01", naturalezaContable.Contains('D') ? monto : 0, precision: 19, scale: 8); //Valor segun moneda (lps=lps, Usd=Usd Eur=Eur)
        builder.InChar("PMDECR01", "D", 1); //Tipo de movimiento C=Credito D=Debito
        builder.InDecimal("PMCCOS01", 0, precision: 5, scale: 0); //Centro de costos
        builder.InDecimal("PMMONE01", moneda, precision: 3, scale: 0); //Moneda del movimiento

        // ===================== Movimiento 2 =====================
        builder.InDecimal("PMTIPO02", naturalezaContable.Contains('C') ? tipoCuenta : 0, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA02", naturalezaContable.Contains('C') ? numeroCuenta : 0, precision: 13, scale: 0); //Numero de cuenta a Acreditar = PMCTAA02
        builder.InDecimal("PMVALR02", naturalezaContable.Contains('C') ? monto : 0, precision: 19, scale: 8);
        builder.InChar("PMDECR02", "C", 1);
        builder.InDecimal("PMCCOS02", naturalezaContable.Contains('C') ? centroCosto : 0, precision: 5, scale: 0);
        builder.InDecimal("PMMONE02", moneda, precision: 3, scale: 0); //Moneda del movimiento

        // ===================== Movimiento 3 =====================
        builder.InDecimal("PMTIPO03", 0, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA03", 0, precision: 13, scale: 0);
        builder.InDecimal("PMVALR03", 0, precision: 19, scale: 8);
        builder.InChar("PMDECR03", "", 1);
        builder.InDecimal("PMCCOS03", 0, precision: 5, scale: 0);
        builder.InDecimal("PMMONE03", 0, precision: 3, scale: 0); //Moneda del movimiento

        // ===================== Movimiento 4 =====================
        builder.InDecimal("PMTIPO04", 0, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA04", 0, precision: 13, scale: 0);
        builder.InDecimal("PMVALR04", 0, precision: 19, scale: 8);
        builder.InChar("PMDECR04", "", 1);
        builder.InDecimal("PMCCOS04", 0, precision: 5, scale: 0);
        builder.InDecimal("PMMONE04", 0, precision: 3, scale: 0); //Moneda del movimiento

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
        builder.OutChar("NomArc", 70);

        var result = await builder.CallAsync(_contextAccessor.HttpContext);

        result.TryGet("CODER", out int codigoError);
        result.TryGet("DESERR", out string? descripcionError);

        return (codigoError, descripcionError);
    }
    catch (Exception ex)
    {
        return (-1, "Error general en PosteoLoteAsync: " + ex.Message);
    }
}

private void CargaLibrerias()
{
    // Lista completa que quieres dejar en LIBL (ajusta el orden a tu necesidad)
    var libl = "QTEMP ICBS BCAH96 BCAH96DTA BNKPRD01 QGPL GX COVENPGMV4";

    // Comando CL en un SOLO statement
    var clCmd = $"CHGLIBL LIBL({libl})";

    // Longitud para QCMDEXC = número de caracteres del comando, con escala 5
    static decimal QcmdexcLen(string s) => Convert.ToDecimal(s.Length.ToString() + ".00000", System.Globalization.CultureInfo.InvariantCulture);

    using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
    cmd.CommandText = "CALL QSYS2.QCMDEXC(?, ?)";
    var p1 = cmd.CreateParameter(); p1.DbType = System.Data.DbType.String; p1.Value = clCmd; cmd.Parameters.Add(p1);
    var p2 = cmd.CreateParameter(); p2.DbType = System.Data.DbType.Decimal; p2.Precision = 15; p2.Scale = 5; p2.Value = QcmdexcLen(clCmd); cmd.Parameters.Add(p2);

    cmd.ExecuteNonQuery();
}


Así es el RPGLE INT_LOTES:

     H Option( *SRCSTMT: *NODEBUGIO )
     fPODINT    Uf A e           k disk    USROPN
     Fglc002    if   e           k disk
     fapplogs   if a e           k disk    rename(applogs:otro) prefix(f_)
      *
     D ERR             s             70    DIM(2) CTDATA PERRCD(1)
     d                 ds
     d  TLRLDA                      512
     D  LIBR                   1     10
      * Rutina Principal
     C     GLC2KEY       KLIST
     C                   KFLD                    GBBANK
     C                   KFLD                    GBCODE
     C                   KFLD                    GBEFDT
     C                   KFLD                    GBEFTM
      *
     C     *dtaara       DEFINE                  TLRLDA
     C                   IN        TLRLDA                               60
     C*    Execute start up routine before first read
     C/COPY CFSORC4,SRC000C
     C*------------------------------------------------------------------*  ****
     c*Determina si es traslado entre monedas LPS-Dolares = tasa Venta nto
     C*------------------------------------------------------------------*  ****
     c                   exsr      Trasmone
     C*------------------------------------------------------------------*  ****
     c*Procesa transacciones segun: Tipo cuenta, cuenta, Monto, Movimiento
     C*------------------------------------------------------------------*  ****
     c                   If        PMTIPO01 > *zeros and PMCTAA01 > *zeros
     c                             and PMVALR01 > *zeros and PMDECR01 <> ' '
     c                             and CODER$=*ZEROS
     c                   Exsr      Primera
     c*No hay transaccion
     c                   Else
     c                   Eval      f_applogs = 'PRIMERA'              +
     c                                         %Trim(%Char(PMTIPO01)) +
     c                                         %Trim(%Char(PMCTAA01)) +
     c                                         %Trim(%Char(PMVALR01)) +
     c                                         %Trim(PMDECR01) +
     c                                         %Trim(%Char(CODER$))
     C                   z-add     01            CODER$
     c                   MOVEL     Err(01)       DESERR
     c                   write     otro
     c                   endif
     c*Segunda Transaccion
     c                   If        PMTIPO02 > *zeros and PMCTAA02 > *zeros
     c                             and PMVALR02 > *zeros and PMDECR02 <> ' '
     c                             and CODER$=*ZEROS
     c                   Exsr      Segunda
     c*No hay transaccion
     c                   Else
     c                   Eval      f_applogs = 'SEGUNDA'              +
     c                                         %Trim(%Char(PMTIPO02)) +
     c                                         %Trim(%Char(PMCTAA02)) +
     c                                         %Trim(%Char(PMVALR02)) +
     c                                         %Trim(PMDECR02) +
     c                                         %Trim(%Char(CODER$))
     C                   z-add     01            CODER$
     c                   MOVEL     Err(01)       DESERR
     c                   write     otro
     c                   endif
     C*Tercera transaccion
     c                   If        PMTIPO03 > *zeros and PMCTAA03 > *zeros
     c                             and PMVALR03 > *zeros and PMDECR03 <> ' '
     c                             and CODER$=*ZEROS
     c                   Exsr      Tercera
     c                   endif
     C*Cuarta transaccion
     c                   If        PMTIPO04 > *zeros and PMCTAA04 > *zeros
     c                             and PMVALR04 > *zeros and PMDECR04 <> ' '
     c                             and CODER$=*ZEROS
     c                   Exsr      Cuarta
     c                   endif
     C                   CLOSE     PODINT
     c*Integra transacciones
     c                   if        CODER$ = *Zeros And Wcontador > *Zeros
     c                   Call      'INTEG_LOTE'                         59
     c                   Parm                    PMPerfil
     c                   Parm      *Blanks       CLError           1
     c                   Parm                    PODFILE          10
     c                   Parm      'BEL'         Canal            10
      *
     C     *In59         Ifeq      *On
     C                   Z-add     2             CODER$
     c                   MOVEL     Err(02)       DESERR
     C                   ENDIF
     C                   ENDIF
      *Error en programas llamados en CL
     C     CLError       Ifne      ' '
     C                   Z-add     2             CODER$
     c                   MOVEL     Err(02)       DESERR
     C                   ENDIF
     C*
     C
     c                   Eval      *Inlr = *On
     c
     C*------------------------------------------------------------------*  ****
     c*Primera transaccion segun: Tipo cuenta, cuenta, Monto, Movimiento
     C*------------------------------------------------------------------*  ****
     c     Primera       Begsr
     c*Asigna valores generales
     c                   Exsr      Generales
     c*Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO01
     c                   Select
     c*Cuenta de ahorros=1
     c                   When      PMTIPO01=1
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR01='D'
     c                   z-add     50002678      pertr
     c                   z-add     78            petran
     c                   Else
     c                   z-add     50002624      pertr
     c                   z-add     24            petran
     c*
     c                   endif
     c                   z-add     *zeros        pecost
     c*Cuenta de cheques=6
     c                   When        PMTIPO01=6
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR01='D'
     c                   z-add     50002073      pertr
     c                   z-add     73            petran
     c                   Else
     c                   z-add     50002015      pertr
     c                   z-add     15            petran
     c*
     c                   endif
     c                   z-add     *zeros        pecost
     c*Cuenta contable=40
     c                   When      PMTIPO01=40
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR01='D'
     c                   z-add     50004081      pertr
     c                   z-add     81            petran
     c                   Else
     c                   z-add     50004010      pertr
     c                   z-add     10            petran
     c                   endif
     c                   z-add     PMCCOS01      pecost
     c                   Endsl
     c*Numero de cuenta
     c                   z-add     PMCTAA01      peacct
     c*Montos lempiras
     c                   If        PMMONE01 = *zeros
     c                   z-add     PMVALR01      peamt
     c                   z-add     *zeros        pefcy
     c                   z-add     *zeros        perate
     c                   z-add     *zeros        pecurr
     c                   move      *blank        peovrc
     c*Moneda extranjera obtiene tasa de cambio
     c                   else
     C                   z-add     PMMONE01      monecal           3 0
     c                   exsr      GetTasa
     C                   z-add     tasacal       perate
      *
     c                   If        CodCyb = 'CYB'
     c* EDGAR 2022       If        CodCyb = 'CYB'  or  CodCyb = 'PER'
     c                   If        TipCom = 'BEN'
     c     PMVALR02      Mult(h)   Tasacal       ValCom           11 2
     c                   z-add     PMVALR03      peamt
     c                   add       ValCom        peamt
     c                   Else
     c                   z-add     PMVALR02      peamt
     c                   Endif
      *
     c                   Else
     c     PMVALR01      Mult(h)   Tasacal       peamt
     c                   EndIf
     c                   z-add     PMVALR01      pefcy
     c                   z-add     PMMONE01      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador         5 0
     c                   z-add     wcontador     recnum
      * Descripcion Enviada desde los canales Debito
     c                   if        PMDECR01='D'
     c                   Movel     DESDB1        pedesc
     c                   Movel     DESDB2        Pedsc2
     c                   Movel     DESDB3        Pedsc3
     c                   Else
      * Descripcion Enviada desde los canales Credito
     c                   Movel     DESCR1        pedesc
     c                   Movel     DESCR2        Pedsc2
     c                   Movel     DESCR3        Pedsc3
     c                   Endif
     c*
     c                   write     pot9111
     c                   endsr
     C*------------------------------------------------------------------*  ****
     c*Segunda transaccion segun: Tipo cuenta, cuenta, Monto, Movimiento
     c*siempre se asume que la segunda transaccion es un debito        o
     C*------------------------------------------------------------------*  ****
     c     Segunda       Begsr
     c*Asigna valores generales
     c                   Exsr      Generales
     c*Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO02
     c                   Select
     c*Cuenta de ahorros=1
     c                   When      PMTIPO02=1
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR02='D'
     c                   z-add     50002678      pertr
     c                   z-add     78            petran
     c                   Else
     c                   z-add     50002624      pertr
     c                   z-add     24            petran
     c*
     c                   endif
     c                   z-add     *zeros        pecost
     c*Cuenta de cheques=6
     c                   When        PMTIPO02=6
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR02='D'
     c                   z-add     50002073      pertr
     c                   z-add     73            petran
     c                   Else
     c                   z-add     50002015      pertr
     c                   z-add     15            petran
     c*
     c                   endif
     c                   z-add     *zeros        pecost
     c*Cuenta contable=40
     c                   When        PMTIPO02=40
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR02='D'
     c                   z-add     50004081      pertr
     c                   z-add     81            petran
     c                   Else
     c                   z-add     50004010      pertr
     c                   z-add     10            petran
     c                   endif
     c                   z-add     PMCCOS02      pecost
     c                   Endsl
     c*Numero de cuenta
     c                   z-add     PMCTAA02      peacct
     c*Montos lempiras
     c                   If        PMMONE02 = *zeros
     c                   z-add     PMVALR02      peamt
     c                   z-add     *zeros        pefcy
     c                   z-add     *zeros        perate
     c                   z-add     *zeros        pecurr
     c                   move      *blank        peovrc
     c*Moneda extranjera obtiene tasa de cambio
     c                   else
     C                   z-add     PMMONE02      monecal           3 0
     c                   exsr      GetTasa
     C                   z-add     tasacal       perate
     c*
     c**** PMVALR02      Mult(h)   Tasacal       peamt
     c                   If        CodCyb = 'CYB'
     c* EDGAR 2022       If        CodCyb = 'CYB'  or CodCyb = 'PER'
     c                   z-add     PMVALR02      peamt
     c                   If        TipCom <>'BEN'
     c                   z-add     PMVALR02      peamt
     c                   Else
     c     PMVALR02      Mult(h)   Tasacal       peamt
     c                   Endif
     c                   Else
     c     PMVALR02      Mult(h)   Tasacal       peamt
     c                   EndIf
      * SI ES TRALADO EBANKING NO HACE ESTOOOOO
     c*Pregunta si es traslado entre monedas (Credito)
     c*                  if        trasla='S' and PMTIPO02<>40
     c*                            and PMDECR02='C'
     C*                  z-add     tasaven       perate
     c*
     c*    PMVALR02      Mult(h)   Tasaven       peamt
     c*                  endif
     c*
     c                   z-add     PMVALR02      pefcy
     c                   z-add     PMMONE02      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador
     c                   z-add     wcontador     recnum
      * Descripcion Enviada desde los canales Debito
     c                   if        PMDECR02='D'
     c                   Movel     DESDB1        pedesc
     c                   Movel     DESDB2        Pedsc2
     c                   Movel     DESDB3        Pedsc3
     c                   Else
      * Descripcion Enviada desde los canales Credito
     c                   Movel     DESCR1        pedesc
     c                   Movel     DESCR2        Pedsc2
     c                   Movel     DESCR3        Pedsc3
     c                   Endif
     c*
     c                   write     pot9111
     c                   endsr
     C*------------------------------------------------------------------*  ****
     c*Tercera transaccion segun: Tipo cuenta, cuenta, Monto, Movimiento
     C*------------------------------------------------------------------*  ****
     c     Tercera       Begsr
     c*Asigna valores generales
     c                   Exsr      Generales
     c*Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO03
     c                   Select
     c*Cuenta de ahorros=1
     c                   When      PMTIPO03=1
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR03='D'
     c                   z-add     50002678      pertr
     c                   z-add     78            petran
     c                   Else
     c                   z-add     50002624      pertr
     c                   z-add     24            petran
     c*
     c                   endif
     c                   z-add     *zeros        pecost
     c*Cuenta de cheques=6
     c                   When        PMTIPO03=6
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR03='D'
     c                   z-add     50002073      pertr
     c                   z-add     73            petran
     c                   Else
     c                   z-add     50002015      pertr
     c                   z-add     15            petran
     c*                                                                   t
     c                   endif
     c                   z-add     *zeros        pecost
     c*Cuenta contable=40
     c                   When        PMTIPO03=40
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR03='D'
     c                   z-add     50004081      pertr
     c                   z-add     81            petran
     c                   Else
     c                   z-add     50004010      pertr
     c                   z-add     10            petran
     c                   endif
     c                   z-add     PMCCOS03      pecost
     c                   Endsl
     c*Numero de cuenta
     c                   z-add     PMCTAA03      peacct
     c*Montos lempiras
     c                   If        PMMONE03 = *zeros
     c                   z-add     PMVALR03      peamt
     c                   z-add     *zeros        pefcy
     c                   z-add     *zeros        perate
     c                   z-add     *zeros        pecurr
     c                   move      *blank        peovrc
     c*Moneda extranjera obtiene tasa de cambio
     c                   else
     C                   z-add     PMMONE03      monecal           3 0
     c                   exsr      GetTasa
     C                   z-add     tasacal       perate
     c*
     c***  PMVALR03      Mult(h)   Tasacal       peamt
     c                   If        CodCyb = 'CYB'
     c* EDGAR 2022       If        CodCyb = 'CYB' or CodCyb = 'PER'
     c                   z-add     PMVALR03      peamt
     c                   Else
     c     PMVALR03      Mult(h)   Tasacal       peamt
     c                   EndIf
     c                   z-add     PMVALR03      pefcy
     c                   z-add     PMMONE03      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador
     c                   z-add     wcontador     recnum
      * Descripcion Enviada desde los canales Debito
     c                   if        PMDECR03='D'
     c                   Movel     DESDB1        pedesc
     c                   Movel     DESDB2        Pedsc2
     c                   Movel     DESDB3        Pedsc3
     c                   Else
      * Descripcion Enviada desde los canales Credito
     c                   Movel     DESCR1        pedesc
     c                   Movel     DESCR2        Pedsc2
     c                   Movel     DESCR3        Pedsc3
     c                   Endif
     c*
     c                   write     pot9111
     c                   endsr
     C*------------------------------------------------------------------*  ****
     c*Cuarta transaccion segun: Tipo cuenta, cuenta, Monto, Movimiento
     C*------------------------------------------------------------------*  ****
     c     Cuarta        Begsr
     c*Asigna valores generales
     c                   Exsr      Generales
     c*Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO04
     c                   Select
     c*Cuenta de ahorros=1
     c                   When      PMTIPO04=1
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR04='D'
     c                   z-add     50002678      pertr
     c                   z-add     78            petran
     c                   Else
     c                   z-add     50002624      pertr
     c                   z-add     24            petran
     c*
     c                   endif
     c                   z-add     *zeros        pecost
     c*Cuenta de cheques=6
     c                   When      PMTIPO04=6
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR04='D'
     c                   z-add     50002073      pertr
     c                   z-add     73            petran
     c                   Else
     c                   z-add     50002015      pertr
     c                   z-add     15            petran
     c*
     c                   endif
     c                   z-add     *zeros        pecost
     c*Cuenta contable=40
     c                   When      PMTIPO04=40
     c*Tipo movimiento C=Credito D=Debito
     c                   if        PMDECR04='D'
     c                   z-add     50004081      pertr
     c                   z-add     81            petran
     c                   Else
     c                   z-add     50004010      pertr
     c                   z-add     10            petran
     c                   endif
     c                   z-add     PMCCOS04      pecost
     c                   Endsl
     c*Numero de cuenta
     c                   z-add     PMCTAA04      peacct
     c*Montos lempiras
     c                   If        PMMONE04 = *zeros
     c                   z-add     PMVALR04      peamt
     c                   z-add     *zeros        pefcy
     c                   z-add     *zeros        perate
     c                   z-add     *zeros        pecurr
     c                   move      *blank        peovrc
     c*Moneda extranjera obtiene tasa de cambio
     c                   else
     C                   z-add     PMMONE04      monecal           3 0
     c                   exsr      GetTasa
     C                   z-add     tasacal       perate
     c*
     c***  PMVALR04      Mult(h)   Tasacal       peamt
     c                   If        CodCyb = 'CYB'
     c* EDGAR 2022       If        CodCyb = 'CYB' or CodCyb = 'PER'
     c                   z-add     PMVALR04      peamt
     c                   Else
     c     PMVALR04      Mult(h)   Tasacal       peamt
     c                   EndIf
     c                   z-add     PMVALR04      pefcy
     c                   z-add     PMMONE04      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador
     c                   z-add     wcontador     recnum
      * Descripcion Enviada desde los canales Debito
     c                   if        PMDECR04='D'
     c                   Movel     DESDB1        pedesc
     c                   Movel     DESDB2        Pedsc2
     c                   Movel     DESDB3        Pedsc3
     c                   Else
      * Descripcion Enviada desde los canales Credito
     c                   Movel     DESCR1        pedesc
     c                   Movel     DESCR2        Pedsc2
     c                   Movel     DESCR3        Pedsc3
     c                   Endif
     c*
     c                   write     pot9111
     c                   endsr
     C*------------------------------------------------------------------*  ****
     c*Obtiene tasa de cambio de ICBS
     C*------------------------------------------------------------------*  ****
     c     GetTasa       Begsr
     C                   Z-ADD     1             gbbank
     C                   Z-ADD     monecal       gbcode
     C                   move      *HIVAL        gbefdt
     C                   move      *HIVAL        gbeftm
     C                   z-add     *zeros        tasacal
     C                   z-add     *zeros        tasaven          15 4
     C*
     C     glc2key       setgt     Glc002
     C                   readp     Glc002
     C                   dow       Not %Eof(Glc002)
     c*Encuentra moneda
     C                   if        gbcode=monecal
     C                   z-add     gbbkxr        tasacal          15 9
     C                   eval(h)   Tasaven=((gbbkxr * 0.7)/100) + gbbkxr
     C                   leave
     C                   endif
     C*
     C                   readp     Glc002
     C*
     C                   enddo
     c                   endsr
     C*------------------------------------------------------------------*  ****
     c*Asigna valores generales en cada lote
     C*------------------------------------------------------------------*  ****
     c     Generales     Begsr
     c                   z-add     1             pebk
     c                   z-add     *zeros        peblk
     c                   z-add     6987          pebtch
     c                   z-add     1             peseq
     c                   z-add     *zeros        pesseq
     c                   z-add     *zeros        peser
      *
     c                   z-add     1             pebseq
     c                   eval      pebnam='INT8077600'
     c                   eval      peempm = 'AUT'
     c                   eval      peempa = 'AUT'
      *Descripciones
     c                   clear                   pedesc
     c                   clear                   pedsc2
     c                   clear                   pedsc3
      *
     c                   z-add     *Zeros        peefdt
     c                   z-add     *zeros        pecurr
     c                   z-add     *zeros        pefcy
     c                   z-add     *zeros        perate
     c                   move      *blank        peovrc
     c                   z-add     *zeros        peortr
     c                   z-add     *zeros        peoac
     c                   move      *blank        peonm
     c                   move      *blank        peref
     c                   move      *blank        pednm
     c                   move      *blank        pedel
     c                   endsr
     C*------------------------------------------------------------------*  ****
     c*Determina Traslados entre cuentas de Lps- Dolar
     C*------------------------------------------------------------------*  ****
     c     Trasmone      Begsr
     c                   movel     'N'           Trasla            1
     c*Debito es LPS y Credito Dolares = Moneda precio venta
     c                   If        (PMTIPO01<>40 and PMDECR01='D' and
     c                             PMMONE01 = *zeros) and
     c                             (PMTIPO02<>40 and PMDECR02='C' and
     c                             PMMONE02 > *zeros)
     c                   Eval      trasla='S'
     c                   else
     c                   Eval      Trasla='N'
     c                   endif
     c*
     c                   endsr
     C*------------------------------------------------------------------*  ****
      * ---------------------------
      *Valores Iniciales
      * ---------------------------
      *
     c     *inzsr        begsr
      * Arma el nombre del archivo al que se pondra OVR
     c                   Call      'INTEG_L61'
     c                   Parm      *blanks       PODFILE          10
      * Borra pot9111
     C                   OPEN      PODINT                               19
     c                   Read      PODINT                                 25
     c                   Dow       not *in25
     c                   Delete    pot9111
     c                   Read      PODINT                                 25
     c                   enddo
      * Parametros de entrada
     C     *Entry        Plist
     c*Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO01
     c                   PARM                    PMTIPO01          2 0
     C*Numero de cuenta
     c                   PARM                    PMCTAA01         13 0
     c*Valor segun moneda (lps=lps, Usd=Usd Eur=Eur)
     C                   PARM                    PMVALR01         13 2
     c*Tipo de movimiento C=Credito D=Debito
     C                   PARM                    PMDECR01          1
     c*Centro de costos
     C                   PARM                    PMCCOS01          5 0
     c*Moneda
     C                   PARM                    PMMONE01          3 0
      *
     c                   PARM                    PMTIPO02          2 0
     c                   PARM                    PMCTAA02         13 0
     C                   PARM                    PMVALR02         13 2
     C                   PARM                    PMDECR02          1
     C                   PARM                    PMCCOS02          5 0
     c*Moneda
     C                   PARM                    PMMONE02          3 0
      *
     c                   PARM                    PMTIPO03          2 0
     c                   PARM                    PMCTAA03         13 0
     C                   PARM                    PMVALR03         13 2
     C                   PARM                    PMDECR03          1
     C                   PARM                    PMCCOS03          5 0
     c*Moneda
     C                   PARM                    PMMONE03          3 0
      *
     c                   PARM                    PMTIPO04          2 0
     c                   PARM                    PMCTAA04         13 0
     C                   PARM                    PMVALR04         13 2
     C                   PARM                    PMDECR04          1
     C                   PARM                    PMCCOS04          5 0
     c*Moneda
     C                   PARM                    PMMONE04          3 0
     C*perfil
     C                   PARM                    PMPerfil         13
     c*Moneda
     C                   PARM                    Moneda            3 0
     c*Descripciones
     C                   PARM                    DESDB1           40
     C                   PARM                    DESDB2           40
     C                   PARM                    DESDB3           40
     c*Descripciones
     C                   PARM                    DESCR1           40
     C                   PARM                    DESCR2           40
     C                   PARM                    DESCR3           40
     c*Codigos de respuesta si se efectuo la transaccion
     C                   PARM                    CODER$            2 0
     C                   PARM                    DESERR           70
     C                   PARM                    NomArc           10
     C*Codigo Cyberbank
     c                   Clear                   CodCyb            3
     c                   Eval      CodCyb = %SubSt(DesErr:1:3)
     C*Codigo Cyberbank Tipo de comision
     c                   Clear                   TipCom            3
     c                   Eval      TipCom = %SubSt(DesErr:4:3)
     C*Contador de registros
     c                   Z-add     *zeros        WCONTADOR         5 0
     C*Control de Errores
     C                   Z-ADD     *ZEROS        CODER$
     C                   CLEAR                   DESERR
     c*Verifica si caja esta activo
     C*                  CLEAR                   ACTIVO            1
     C*                  CALL      'CCRDTACL'                           59
     C*                  PARM                    ACTIVO
     C*ACTIVO=A CAJA ESTA ACTIVO NO HAY PROBLEMA
     C*    ACTIVO        IFEQ      'A'
     C*CAJA ESTA ACTIVO VERIFICA QUE NO HAYA PROGRAMAS CON ERROR
     C*                  CLEAR                   CAJAER            1
     C*                  CALL      'CCERRCJ1'                           59
     C*                  PARM                    CAJAER
     C*                  ENDIF
     C*CAJAER= '0' NO HAY ERROR
     c*                  if        CAJAER<>'0'
     C*                  z-add     02            CODER$
     c*                  MOVEL     Err(02)       DESERR
     C*                  Endif
     C*
     c                   endsr
**
TRANSACCION UNO/DOS SIN VALORES NO SE GENERO LOTE
No Puede Ejecutarse Transaccion debido a Intefaces...


Y este es el procedimiento almacenado que cree:

--  Generar SQL 
--  Versión:                   	V7R4M0 190621 
--  Generado en:              	23/09/25 09:28:26 
--  Base de datos relacional:       	DVHNDEV 
--  Opción de estándares:          	Db2 for i 
CREATE PROCEDURE BCAH96.INT_LOTES ( 
	IN PMTIPO01 DECIMAL(2, 0) , 
	IN PMCTAA01 DECIMAL(13, 0) , 
	IN PMVALR01 DECIMAL(19, 8) , 
	IN PMDECR01 CHAR(1) , 
	IN PMCCOS01 DECIMAL(5, 0) , 
	IN PMMONE01 DECIMAL(3, 0) , 
	IN PMTIPO02 DECIMAL(2, 0) , 
	IN PMCTAA02 DECIMAL(13, 0) , 
	IN PMVALR02 DECIMAL(19, 8) , 
	IN PMDECR02 CHAR(1) , 
	IN PMCCOS02 DECIMAL(5, 0) , 
	IN PMMONE02 DECIMAL(3, 0) , 
	IN PMTIPO03 DECIMAL(2, 0) , 
	IN PMCTAA03 DECIMAL(13, 0) , 
	IN PMVALR03 DECIMAL(19, 8) , 
	IN PMDECR03 CHAR(1) , 
	IN PMCCOS03 DECIMAL(5, 0) , 
	IN PMMONE03 DECIMAL(3, 0) , 
	IN PMTIPO04 DECIMAL(2, 0) , 
	IN PMCTAA04 DECIMAL(13, 0) , 
	IN PMVALR04 DECIMAL(19, 8) , 
	IN PMDECR04 CHAR(1) , 
	IN PMCCOS04 DECIMAL(5, 0) , 
	IN PMMONE04 DECIMAL(3, 0) , 
	IN PMPERFIL CHAR(13) , 
	IN MONEDA DECIMAL(3, 0) , 
	IN DESDB1 CHAR(40) , 
	IN DESDB2 CHAR(40) , 
	IN DESDB3 CHAR(40) ,   
        IN DESCR1 CHAR(40) , 
	IN DESCR2 CHAR(40) , 
	IN DESCR3 CHAR(40) ,
	OUT CODER DECIMAL(2, 0) , 
	OUT DESERR CHAR(70),
        OUT NOMARC CHAR(70) ) 
	LANGUAGE RPGLE 
	SPECIFIC BCAH96.INT_LOTES
	DETERMINISTIC 
	MODIFIES SQL DATA 
	CALLED ON NULL INPUT 
	EXTERNAL NAME 'BCAH96/INT_LOTES' 
	PARAMETER STYLE GENERAL ; 
  
GRANT EXECUTE   
ON SPECIFIC PROCEDURE BCAH96.INT_LOTES
TO PUBLIC ; 
  
GRANT ALTER , EXECUTE   
ON SPECIFIC PROCEDURE BCAH96.INT_LOTES
TO TFCASTRO3 WITH GRANT OPTION ;

GRANT ALTER , EXECUTE   
ON SPECIFIC PROCEDURE BCAH96.INT_LOTES
TO TBBANEGA1 WITH GRANT OPTION ;

