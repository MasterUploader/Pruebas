A* ================================================================
A*  PF: POSPOSTEO  (Librería: BCAH96DTA)
A*  Propósito : Registrar posteos POS / movimientos contables.
A*  Notas     : - Todo CHAR, tamaños según tu estándar.
A*              - GUID es clave primaria (UNIQUE + K).
A*              - ALIAS conservan nombres "largos" para SQL.
A* ================================================================
     A          UNIQUE
     A          TEXT('Posteos POS / Movimientos')
     A          R POSPOSTEOR

A* -- Clave primaria y metadatos de control ------------------------
     A            GUID          36A          ALIAS('GUID')
     A                                      COLHDG('GUID (UUID)')
     A                                      TEXT('GUID/UUID clave primaria')

     A            FECHAPOST      8A          ALIAS('FechaPosteo')
     A                                      COLHDG('Fecha  AAAAMMDD')
     A                                      TEXT('Fecha posteo (texto AAAAMMDD)')

     A            HORAPOST       6A          ALIAS('HoraPosteo')
     A                                      COLHDG('Hora   HHMMSS')
     A                                      TEXT('Hora posteo (texto HHMMSS)')

A* -- Datos de la operación ----------------------------------------
     A            NUMCUENTA     16A          ALIAS('numeroCuenta')
     A                                      COLHDG('Numero cuenta')
     A                                      TEXT('Número de cuenta/PAN (texto)')

     A            MTODEBITO     18A          ALIAS('montoDebitado')
     A                                      COLHDG('Monto debitado')
     A                                      TEXT('Monto debitado como CHAR')

     A            MTOACREDI     18A          ALIAS('montoAcreditado')
     A                                      COLHDG('Monto acreditado')
     A                                      TEXT('Monto acreditado como CHAR')

     A            CODCOMERC      7A          ALIAS('codigoComercio')
     A                                      COLHDG('Cod comercio')
     A                                      TEXT('Código de comercio (7)')

     A            NOMCOMERC    100A          ALIAS('nombreComercio')
     A                                      COLHDG('Nombre comercio')
     A                                      TEXT('Nombre del comercio')

     A            TERMINAL       8A          ALIAS('terminal')
     A                                      COLHDG('Terminal')
     A                                      TEXT('ID terminal (8)')

     A            DESCRIPC     200A          ALIAS('descripcion')
     A                                      COLHDG('Descripcion')
     A                                      TEXT('Descripción/leyenda de la transacción')

A* -- Naturaleza / control contable --------------------------------
     A            NATCONTA       1A          ALIAS('naturalezaContable')
     A                                      COLHDG('Naturaleza D/C')
     A                                      TEXT('D=Débito, C=Crédito')

     A            NUMCORTE       6A          ALIAS('numeroDeCorte')
     A                                      COLHDG('Numero corte')
     A                                      TEXT('Número de corte/lote (6)')

     A            IDTRANUNI      6A          ALIAS('idTransaccionUnico')
     A                                      COLHDG('Id trans unico')
     A                                      TEXT('STAN/correlativo externo (6)')

A* -- Estado y errores ---------------------------------------------
     A            ESTADO         1A          ALIAS('estado')
     A                                      COLHDG('Estado')
     A                                      TEXT('P/A/R/E/O/X según flujo')

     A            DESCESTADO   100A          ALIAS('descripcionEstado')
     A                                      COLHDG('Descripcion estado')
     A                                      TEXT('Descripción del estado')

     A            CODERROR       5A          ALIAS('CodigoError')
     A                                      COLHDG('Codigo error(5)')
     A                                      TEXT('Código de error (numérico-texto, 5)')

     A            DESCERROR    200A          ALIAS('DescripcionError')
     A                                      COLHDG('Descripcion error')
     A                                      TEXT('Descripción del error')

A* -- Definición de la clave primaria -------------------------------
     A          K GUID





A* ================================================================
A*  LF: POSPOSTEO_DEDUP  (Librería: BCAH96DTA)
A*  Propósito : Índice UNICO para deduplicar operaciones.
A*  Nota      : Usa campos del PF POSPOSTEO (PFILE).
A* ================================================================
     A          UNIQUE
     A          TEXT('IDX unico dedup POS')
     A          PFILE(POSPOSTEO)

     A          K CODCOMERC
     A          K TERMINAL
     A          K IDTRANUNI
     A          K FECHAPOST
     A          K HORAPOST






A* ================================================================
A*  LF: POSPOSTEO_DT  (Librería: BCAH96DTA)
A*  Propósito : Consultas por rango de fecha/hora.
A* ================================================================
     A          TEXT('IDX por fecha y hora POS')
     A          PFILE(POSPOSTEO)

     A          K FECHAPOST
     A          K HORAPOST





A* ================================================================
A*  LF: POSPOSTEO_CTA  (Librería: BCAH96DTA)
A*  Propósito : Búsquedas por cuenta y fecha.
A* ================================================================
     A          TEXT('IDX por cuenta y fecha POS')
     A          PFILE(POSPOSTEO)

     A          K NUMCUENTA
     A          K FECHAPOST



CRTSRCPF FILE(BCAH96DTA/QDDSSRC) RCDLEN(112) TEXT('Fuentes DDS POS')



ADDPFM FILE(BCAH96DTA/QDDSSRC) MBR(POSPOSTEO)        SRCTYPE(PF) TEXT('PF POSPOSTEO')
ADDPFM FILE(BCAH96DTA/QDDSSRC) MBR(POSPOSTEO_DEDUP)  SRCTYPE(LF) TEXT('LF unico dedup')
ADDPFM FILE(BCAH96DTA/QDDSSRC) MBR(POSPOSTEO_DT)     SRCTYPE(LF) TEXT('LF fecha/hora')
ADDPFM FILE(BCAH96DTA/QDDSSRC) MBR(POSPOSTEO_CTA)    SRCTYPE(LF) TEXT('LF cuenta/fecha')




  CRTPF FILE(BCAH96DTA/POSPOSTEO)       SRCFILE(BCAH96DTA/QDDSSRC) SRCMBR(POSPOSTEO)
CRTLF FILE(BCAH96DTA/POSPOSTEO_DEDUP) SRCFILE(BCAH96DTA/QDDSSRC) SRCMBR(POSPOSTEO_DEDUP)
CRTLF FILE(BCAH96DTA/POSPOSTEO_DT)    SRCFILE(BCAH96DTA/QDDSSRC) SRCMBR(POSPOSTEO_DT)
CRTLF FILE(BCAH96DTA/POSPOSTEO_CTA)   SRCFILE(BCAH96DTA/QDDSSRC) SRCMBR(POSPOSTEO_CTA)





  

  
     
     

     


     
