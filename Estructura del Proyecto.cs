Así deje la tabla y el logico 

     A* ================================================================
     A*  PF: POSRE01G  (Librería: BCAH96DTA)
     A*  Proposito : Registrar posteos POS / movimientos contables.
     A*  Programador: Brayan René Banegas Mejía
     A*  Fecha: 7 de noviembre de 2025
     A* ================================================================
     A                                      UNIQUE
                R $POSRE01G                 TEXT('Posteos POS / Movimientos')
     A* -- Clave primaria y metadatos de control ------------------------
     A            GUID          36A         ALIAS(GUUID)
     A                                      COLHDG('GUID (UUID)')
     A                                      TEXT('GUID/UUID clave primaria')

     A            FECHAPOST      8A         ALIAS(FECHA_POSTEO)
     A                                      COLHDG('Fecha  AAAAMMDD')
     A                                      TEXT('Fecha posteo (AAAAMMDD)')

     A            HORAPOST       6A         ALIAS(HORA_POSTEO)
     A                                      COLHDG('Hora  HHMMSS')
     A                                      TEXT('Hora posteo (texto HHMMSS)')

     A* -- Datos de la operacion ----------------------------------------
     A            NUMCUENTA     16A         ALIAS(NUMERO_CUENTA)
     A                                      COLHDG('Numero cuenta')
     A                                      TEXT('Número de cuenta/PAN (texto)')

     A            MTODEBITO     18A         ALIAS(MONTO_DEBITADO)
     A                                      COLHDG('Monto debitado')
     A                                      TEXT('Monto debitado como CHAR')

     A            MTOACREDI     18A         ALIAS(MONTO_ACREDITADO)
     A                                      COLHDG('Monto acreditado')
     A                                      TEXT('Monto acreditado como CHAR')

     A            CODCOMERC      7A         ALIAS(CODIGO_COMERCIO)
     A                                      COLHDG('Cod comercio')
     A                                      TEXT('Codigo de comercio (7)')

     A            NOMCOMERC    100A         ALIAS(NOMBRE_COMERCIO)
     A                                      COLHDG('Nombre comercio')
     A                                      TEXT('Nombre del comercio')

     A            TERMINAL       8A         ALIAS(TERMINAL_COMERCIO)
     A                                      COLHDG('Terminal')
     A                                      TEXT('ID terminal (8)')

     A            DESCRIPC     200A         ALIAS(DESCRIPCION)
     A                                      COLHDG('Descripcion')
     A                                      TEXT('Descripcion/leyenda')

     A* -- Naturaleza / control contable --------------------------------
     A            NATCONTA       1A         ALIAS(NATURALEZA_CONTABLE)
     A                                      COLHDG('Naturaleza D/C')
     A                                      TEXT('D=Debito, C=Credito')

     A            NUMCORTE       6A         ALIAS(NUMERO_CORTE)
     A                                      COLHDG('Numero corte')
     A                                      TEXT('Numero de corte/lote (6)')

     A            IDTRANUNI      6A         ALIAS(ID_TRANSACCION_UNICO)
     A                                      COLHDG('Id trans unico')
     A                                      TEXT('STAN/correlativo externo (6)')

     A* -- Estado y errores ---------------------------------------------
     A            ESTADO         1A         ALIAS(ESTADO_TRANSACCION)
     A                                      COLHDG('Estado')
     A                                      TEXT('Según flujo')

     A            DESCESTADO   100A         ALIAS(DESCRIPCION_ESTADO)
     A                                      COLHDG('Descripcion estado')
     A                                      TEXT('Descripcion del estado')

     A            CODERROR       5A         ALIAS(CODIGO_ERROR)
     A                                      COLHDG('Codigo error(5)')
     A                                      TEXT('Codigo de error')

     A            DESCERROR    200A         ALIAS(DESCRIPCION_ERROR)
     A                                      COLHDG('Descripcion error')
     A                                      TEXT('Descripcion del error')

     A* -- Definicion de la clave primaria -------------------------------
     A          K IDTRANUNI




     A* ================================================================
     A*  LF: POSRE01G01  (Librería: BCAH96DTA)
     A*  Propósito : Índice UNICO para consulta por IDTRANUNI y NUMCORTE
     A*  Nota      : Usa campos del PF POSRE01G (PFILE).
     A* ================================================================
     A                                      UNIQUE

     A          R $POSRE01G                 PFILE(BCAH96DTA/POSRE01G)
     A                                      TEXT('IDX UNICO DEDUP POS')
     A          K NUMCORTE
     A          K IDTRANUNI




Necesito ahora que el enpoint ValidarTransacciones, lea esta tabla usando el indice, si existe el registro devuelva error, sino existe el registro devuelva todos los datos.
          



