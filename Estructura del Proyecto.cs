Ahora me dicen que quien se encargara de los lotes es uno de estos 2 programas RPG:

Este es uno

      *________________________________________________________________*
      * Programa       Debitos y Creditos en un solo Lote Transserver  *
      * Fecha          7 Mayo 2016                                     *
      
     H Option( *SRCSTMT: *NODEBUGIO )
     fPODAPP    Uf A e           k disk    USROPN
     Fglc002    if   e           k disk
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
     c*
     c                   If        PMTIPO01 > *zeros and PMCTAA01 > *zeros
     c                             and PMVALR01 > *zeros and PMDECR01 <> ' '
     c                             and CODER$=*ZEROS
     c                   Exsr      Primera
     c*No hay transaccion
     c                   Else
     C                   z-add     01            CODER$
     c                   MOVEL     Err(01)       DESERR
     c                   endif
     c*Segunda Transaccion
     c                   If        PMTIPO02 > *zeros and PMCTAA02 > *zeros
     c                             and PMVALR02 > *zeros and PMDECR02 <> ' '
     c                             and CODER$=*ZEROS
     c                   Exsr      Segunda
     c*No hay transaccion
     c                   Else
     C                   z-add     01            CODER$
     c                   MOVEL     Err(01)       DESERR
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
     C                   Close     PODAPP
     c*Integra transacciones
     c                   if        CODER$ = *Zeros And Wcontador > *Zeros
     c                   Call      'APACD765'                           59
     c                   Parm                    PMPerfil
     c                   Parm                    PODFILE
     c                   Parm      *Blanks       CLError           1
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c                   z-add     PMVALR02      peamt
     c* Si la Tranferecia es en Moneda Extranjera
     c                   If        PMMONE01 > *zeros and
     c                             PMMONE02 > *zeros
     c                   Clear                   Peamt
     c     PMVALR01      Mult(h)   Tasacal       Peamt
     c                   Endif
      *
     c                   z-add     PMVALR01      pefcy
     c                   z-add     PMMONE01      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador         5 0
     c                   z-add     wcontador     recnum
     c*
     c                   Movel     DES003        Pedsc3
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        pedesc
     c**                 movel     Des003        Pedsc2
     c**                 clear                   Pedsc3
     c**                 endif
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        pedesc
     c**                 movel     Des003        Pedsc2
     c**                 clear                   Pedsc3
     C**                 endif
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
     c                   z-add     PMVALR02      peamt
     c*
     c*    PMVALR02      Mult(h)   Tasacal       peamt
     c                   if        pmvalr03 <> *ZEROS
     c                             and pmdecr03='C'
     c                             and pmmone03 = *zeros
     c                   eval(H)   peamt = PMVALR01-PMVALR03
     C                   ELSE
     c                   z-add     PMVALR01      peamt
     C                   ENDIF
     c* Si la Tranferecia es en Moneda Extranjera
     c                   If        PMMONE01 > *zeros and
     c                             PMMONE02 > *zeros
     c                   Clear                   Peamt
     c     PMVALR01      Mult(h)   Tasacal       Peamt
     c                   endif
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
     c*
     c                   Movel     DES004        Pedsc3
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
     c*
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
     c*                  z-add     PMVALR03      peamt
     c                   EVAL(H)   peamt =PMVALR03
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
     c     PMVALR03      Mult(h)   Tasacal       peamt
     c                   z-add     PMVALR03      pefcy
     c                   z-add     PMMONE03      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador
     c                   z-add     wcontador     recnum
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c     PMVALR04      Mult(h)   Tasacal       peamt
     c                   z-add     PMVALR04      pefcy
     c                   z-add     PMMONE04      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador
     c                   z-add     wcontador     recnum
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
     C                   If        tasatm > 1.000000000
     C                   z-add     tasatm        tasacal
     c                   else
     C                   z-add     gbbkxr        tasacal          15 9
     c                   endif
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
     c                   eval      pebnam='APP2016155'
     c                   eval      peempm = 'AUT'
     c                   eval      peempa = 'AUT'
      *Descripciones
     c                   clear                   pedesc
     c                   clear                   pedsc2
     c                   clear                   pedsc3
     c                   Movel     DES001        pedesc
     c                   Movel     DES002        Pedsc2
     c                   Movel     DES003        Pedsc3
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
     c                   Call      'APACD766'                           59
     c                   Parm      *blanks       PODFILE          10

     C                   OPEN      PODAPP                               19
     c                   Read      PODAPP                                 25
     c                   Dow       not *in25
     c                   Delete    pot9111
     c                   Read      PODAPP                                 25
     c                   enddo
      * Parametros de entrada
     C     *Entry        Plist
     c*Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO01
     c                   PARM                    PMTIPO01          2 0
     C*Numero de cuenta
     c                   PARM                    PMCTAA01         13 0
     c*Valor segun moneda (lps=lps, Usd=Usd Eur=Eur)
     C                   PARM                    PMVALR01         19 8
     c*Tipo de movimiento C=Credito D=Debito
     C                   PARM                    PMDECR01          1
     c*Centro de costos
     C                   PARM                    PMCCOS01          5 0
     c*Moneda
     C                   PARM                    PMMONE01          3 0
      *
     c                   PARM                    PMTIPO02          2 0
     c                   PARM                    PMCTAA02         13 0
     C                   PARM                    PMVALR02         19 8
     C                   PARM                    PMDECR02          1
     C                   PARM                    PMCCOS02          5 0
     c*Moneda
     C                   PARM                    PMMONE02          3 0
      *
     c                   PARM                    PMTIPO03          2 0
     c                   PARM                    PMCTAA03         13 0
     C                   PARM                    PMVALR03         19 8
     C                   PARM                    PMDECR03          1
     C                   PARM                    PMCCOS03          5 0
     c*Moneda
     C                   PARM                    PMMONE03          3 0
      *
     c                   PARM                    PMTIPO04          2 0
     c                   PARM                    PMCTAA04         13 0
     C                   PARM                    PMVALR04         19 8
     C                   PARM                    PMDECR04          1
     C                   PARM                    PMCCOS04          5 0
     c*Moneda
     C                   PARM                    PMMONE04          3 0
     C*perfil
     C                   PARM                    PMPerfil         13
     c*Moneda
     C                   PARM                    Moneda            3 0
     c*Descripciones
     C                   PARM                    DES001           40
     C                   PARM                    DES002           40
     C                   PARM                    DES003           40
     C                   PARM                    DES004           40
     c                   parm                    tasatm           15 9
     c*Codigos de respuesta si se efectuo la transaccion
     C                   PARM                    CODER$            2 0
     C                   PARM                    DESERR           70
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


Este es otro:
      *________________________________________________________________*
      * Programa       Debitos y Creditos en un solo Lote Transserver  *
      * Fecha          7 Mayo 2016                                     *
      
     H Option( *SRCSTMT: *NODEBUGIO )
     fPODAPP    Uf A e           k disk    USROPN
     Fglc002    if   e           k disk
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
     C/COPY CFSORC4,SRC000C
      *------------------------------------------------------------------*  ****
      *Determina si es traslado entre monedas LPS-Dolares = tasa Venta nto
      **
     c                   exsr      Trasmone
      **
      * Procesa transacciones segun: Tipo cuenta, cuenta, Monto, Movimiento
      **
     c                   If        CODER$ = 1 or CODER$ = 2
     c                   Eval      CODER$ = 0
     c                   EndIf

     c                   If        PMTIPO01 > *zeros and PMCTAA01 > *zeros  and
     c                             PMVALR01 > *zeros and PMDECR01 <> ' '    and
     c                             CODER$ = *ZEROS
     c                   Exsr      Primera
     c*No hay transaccion
     c                   Else
     C                   z-add     01            CODER$
     c                   MOVEL     Err(01)       DESERR
     c                   endif
      ** Segunda Transaccion
     c                   If        CODER$ = 1
     c                   Eval      CODER$ = 0
     c                   EndIf

     c                   If        PMTIPO02 > *zeros and PMCTAA02 > *zeros  and
     c                             PMVALR02 > *zeros and PMDECR02 <> ' '    and
     c                             CODER$=*ZEROS
     c                   Exsr      Segunda
     c*No hay transaccion
     c                   Else
     C                   z-add     01            CODER$
     c                   MOVEL     Err(01)       DESERR
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
     C                   Close     PODAPP
     c*Integra transacciones
     c                   if        CODER$ = *Zeros And Wcontador > *Zeros
     c                   Call      'APACD760'                           59
     c                   Parm                    PMPerfil
     c                   Parm                    PODFILE
     c                   Parm      *Blanks       CLError           1
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c                   z-add     PMVALR02      peamt
     c* Si la Tranferecia es en Moneda Extranjera
     c                   If        PMMONE01 > *zeros and
     c                             PMMONE02 > *zeros
     c                   Clear                   Peamt
     c     PMVALR01      Mult(h)   Tasacal       Peamt
     c                   Endif
      *
     c                   z-add     PMVALR01      pefcy
     c                   z-add     PMMONE01      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador         5 0
     c                   z-add     wcontador     recnum
     c*
     c                   Movel     DES003        Pedsc3
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        pedesc
     c**                 movel     Des003        Pedsc2
     c**                 clear                   Pedsc3
     c**                 endif
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        pedesc
     c**                 movel     Des003        Pedsc2
     c**                 clear                   Pedsc3
     C**                 endif
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
     c                   z-add     PMVALR02      peamt
     c*
     c*    PMVALR02      Mult(h)   Tasacal       peamt
     c                   if        pmvalr03 <> *ZEROS
     c                             and pmdecr03='C'
     c                             and pmmone03 = *zeros
     c                   eval(H)   peamt = PMVALR01-PMVALR03
     C                   ELSE
     c                   z-add     PMVALR01      peamt
     C                   ENDIF
     c* Si la Tranferecia es en Moneda Extranjera
     c                   If        PMMONE01 > *zeros and
     c                             PMMONE02 > *zeros
     c                   Clear                   Peamt
     c     PMVALR01      Mult(h)   Tasacal       Peamt
     c                   endif
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
     c*
     c                   Movel     DES004        Pedsc3
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
     c*
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
     c*                  z-add     PMVALR03      peamt
     c                   EVAL(H)   peamt =PMVALR03
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
     c     PMVALR03      Mult(h)   Tasacal       peamt
     c                   z-add     PMVALR03      pefcy
     c                   z-add     PMMONE03      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador
     c                   z-add     wcontador     recnum
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c*Credito la descipcion viene campo (70) viene en la dos se pasa uno
     c**                 if        DES001<>*blanks
     c**                 movel     des002        Des001
     c**                 movel     Des003        Des002
     c**                 clear                   Des003
     c**                 endif
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
     c     PMVALR04      Mult(h)   Tasacal       peamt
     c                   z-add     PMVALR04      pefcy
     c                   z-add     PMMONE04      pecurr
     c                   move      '1'           peovrc
     c                   Endif
     c*numero de registro
     c                   add       1             wcontador
     c                   z-add     wcontador     recnum
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
     C                   If        tasatm > 1.000000000
     C                   z-add     tasatm        tasacal
     c                   else
     C                   z-add     gbbkxr        tasacal          15 9
     c                   endif
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
     c                   eval      pebnam='APP2016155'
     c                   eval      peempm = 'AUT'
     c                   eval      peempa = 'AUT'
      *Descripciones
     c                   clear                   pedesc
     c                   clear                   pedsc2
     c                   clear                   pedsc3
     c                   Movel     DES001        pedesc
     c                   Movel     DES002        Pedsc2
     c                   Movel     DES003        Pedsc3
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
      **
      * Determina Traslados entre cuentas de Lps- Dolar
      **
     c     Trasmone      Begsr
     c                   movel     'N'           Trasla            1
      * Debito es LPS y Credito Dolares = Moneda precio venta
     c                   If        (PMTIPO01 <> 40 and PMDECR01 = 'D' and
     c                              PMMONE01 = *zeros)                and
     c                             (PMTIPO02 <> 40 and PMDECR02 = 'C' and
     c                              PMMONE02 > *zeros)
     c                   Eval      trasla='S'
     c                   else
     c                   Eval      Trasla='N'
     c                   endif
     c                   endsr
     C*------------------------------------------------------------------*  ****
      * ---------------------------
      *Valores Iniciales
      * ---------------------------
      *
     c     *inzsr        begsr
     c                   Call      'APACD761'
     c                   Parm      *blanks       PODFILE          10

     C                   OPEN      PODAPP                               19
     c                   Read      PODAPP                                 25
     c                   Dow       not *in25
     c                   Delete    pot9111
     c                   Read      PODAPP                                 25
     c                   enddo
      * Parametros de entrada
     C     *Entry        Plist
     c*Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO01
     c                   PARM                    PMTIPO01          2 0
     C*Numero de cuenta
     c                   PARM                    PMCTAA01         13 0
     c*Valor segun moneda (lps=lps, Usd=Usd Eur=Eur)
     C                   PARM                    PMVALR01         15 8
     c*Tipo de movimiento C=Credito D=Debito
     C                   PARM                    PMDECR01          1
     c*Centro de costos
     C                   PARM                    PMCCOS01          5 0
     c*Moneda
     C                   PARM                    PMMONE01          3 0
      *
     c                   PARM                    PMTIPO02          2 0
     c                   PARM                    PMCTAA02         13 0
     C                   PARM                    PMVALR02         15 8
     C                   PARM                    PMDECR02          1
     C                   PARM                    PMCCOS02          5 0
     c*Moneda
     C                   PARM                    PMMONE02          3 0
      *
     c                   PARM                    PMTIPO03          2 0
     c                   PARM                    PMCTAA03         13 0
     C                   PARM                    PMVALR03         15 8
     C                   PARM                    PMDECR03          1
     C                   PARM                    PMCCOS03          5 0
     c*Moneda
     C                   PARM                    PMMONE03          3 0
      *
     c                   PARM                    PMTIPO04          2 0
     c                   PARM                    PMCTAA04         13 0
     C                   PARM                    PMVALR04         15 8
     C                   PARM                    PMDECR04          1
     C                   PARM                    PMCCOS04          5 0
     c*Moneda
     C                   PARM                    PMMONE04          3 0
     C*perfil
     C                   PARM                    PMPerfil         13
     c*Moneda
     C                   PARM                    Moneda            3 0
     c*Descripciones
     C                   PARM                    DES001           40
     C                   PARM                    DES002           40
     C                   PARM                    DES003           40
     C                   PARM                    DES004           40
     c                   parm                    tasatm           15 9
     c*Codigos de respuesta si se efectuo la transaccion
     C                   PARM                    CODER$            2 0
     C                   PARM                    DESERR           70
     C*Contador de registros
     c                   Z-add     *zeros        WCONTADOR         5 0
     C*Control de Errores
     C                   Z-ADD     *ZEROS        CODER$
     C                   CLEAR                   DESERR
     c*Verifica si caja esta activo
     C                   CLEAR                   ACTIVO            1
     C                   CALL      'CCRDTACL'                           59
     C                   PARM                    ACTIVO
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

Ahora necesito crear una CLLE para realizar el llamado a uno u otro, considera que de lo actual en C#, solo quedara esto, y posterior a la ultima linea que te muestro se llamara a una de las CLLE


await Task.Yield(); // Simula asincronía para cumplir con la firma async.
//Procesos Previos

_connection.Open(); //Abrimos la conexión a la base de datos

//LLamada a método FecReal, reemplaza llamado a CLLE fecha Real (FECTIM) ICBSUSER/FECTIM
//  var (error, fecsys, horasys) = FecReal()

//Llamada a método VerFecha, reemplaza llamado a CLLE VerFecha (DSCDT) BNKPRD01/TAP001
var (seObtuvoFecha, yyyyMMdd, fechaJuliana) = VerFecha();
if (!seObtuvoFecha) return BuildError("400", "No se pudo obtener la fecha del sistema.");

//============================Validaciones Previas============================//

// Normalización de importes: tolera "." o "," y espacios
var deb = Utilities.ParseMonto(guardarTransaccionesDto.MontoDebitado);
var cre = Utilities.ParseMonto(guardarTransaccionesDto.MontoAcreditado);

//Validamos que al menos uno de los montos sea mayor a 0, no se puede postear ambos en 0.
if (deb <= 0m && cre <= 0m) return BuildError("400", "No hay importes a postear (ambos montos son 0).");

//Obtenemos perfil transerver de la configuración global
string perfilTranserver = GlobalConnection.Current.PerfilTranserver;

//Validamos, si no hay perfil transerver, retornamos error porque el proceso no puede continuar.
if (perfilTranserver.IsNullOrEmpty()) return BuildError("400", "No se ha configurado el perfil transerver a buscar en JSON.");

//Validamos si existe el comercio en la tabla BCAH96DTA/IADQCOM
var (existeComercio, codigoError, mensajeComercio) = BuscarComercio(guardarTransaccionesDto.NumeroCuenta, int.Parse(guardarTransaccionesDto.CodigoComercio));
if (!existeComercio) return BuildError(codigoError, mensajeComercio);

//Validación de Terminal
var (existeTerminal, esTerminalVirtual, codigoErrorTerminal, mensajeTerminal) = BuscarTerminal(guardarTransaccionesDto.Terminal, int.Parse(guardarTransaccionesDto.CodigoComercio));
if (!existeTerminal) return BuildError(codigoErrorTerminal, mensajeTerminal);

//============================Fin Validaciones Previas============================//

//============================Inicia Proceso Principal============================//

// 1. Obtenemos el Perfil Transerver del cliente.
var respuestaPerfil = VerPerfil(perfilTranserver);

// Si no existe el perfil, retornar error y no continuar con el proceso.
if (!respuestaPerfil.existePerfil) return BuildError(respuestaPerfil.codigoError, respuestaPerfil.descripcionError);


Por favor revisa y dime como serian las 2 clle que llamarian aquellos programas.
