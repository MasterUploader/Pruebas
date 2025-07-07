     A                                      UNIQUE
     A          R IBTSACTA
     A            INOCONFIR     11          TEXT('NO.CONFIRMACION')
     A                                      COLHDG(-
     A                                      'NUMERO' -
     A                                      'CONFIRMACION')
     A            IDATRECI       8A         TEXT('FECHA.RECIBIDA')
     A                                      COLHDG(-
     A                                      'FECHA' -
     A                                      'RECIBIDA')
     A            IHORRECI       9A         TEXT('HORA RECIBIDA')
     A                                      COLHDG(-
     A                                      'HORA' -
     A                                      'RECIBIDA')
     A            IDATCONF       8A         TEXT('FECHA.CONFIRMO')
     A                                      COLHDG(-
     A                                      'FECHA' -
     A                                      'CONFIRMO')
     A            IHORCONF       9A         TEXT('HORA CONFIRMO')
     A                                      COLHDG(-
     A                                      'HORA' -
     A                                      'CONFIRMO')
     A            IDATVAL        8A         TEXT('FECHA.VALIDO')
     A                                      COLHDG(-
     A                                      'FECHA' -
     A                                      'VALIDO')
     A            IHORVAL        9A         TEXT('HORA VALIDO')
     A                                      COLHDG(-
     A                                      'HORA' -
     A                                      'VALIDO')
     A            IDATPAGO       8A         TEXT('FECHA.PAGO')
     A                                      COLHDG(-
     A                                      'FECHA' -
     A                                      'PAGO')
     A            IHORPAGO       9A         TEXT('HORA PAGO')
     A                                      COLHDG(-
     A                                      'HORA' -
     A                                      'PAGO')
     A            IDATACRE       8A         TEXT('FECHA.ACREDITO')
     A                                      COLHDG(-
     A                                      'FECHA' -
     A                                      'ACREDITO')
     A            IHORACRE       9A         TEXT('HORA ACREDITO')
     A                                      COLHDG(-
     A                                      'HORA' -
     A                                      'ACREDITO')
     A            IDATRECH       8A         TEXT('FECHA.RECHAZO')
     A                                      COLHDG(-
     A                                      'FECHA' -
     A                                      'RECHAZO')
     A            IHORRECH       9A         TEXT('HORA RECHAZO')
     A                                      COLHDG(-
     A                                      'HORA' -
     A                                      'RECHAZO')
     A            ITIPPAGO       3          TEXT('TIPO.PAGO')
     A                                      COLHDG(-
     A                                      'TIPO' -
     A                                      'PAGO')
     C* ==========NUEVOS CAMPOS======================
     A            ISERVICD       3          TEXT('SERVICIOCD')
     A                                      COLHDG(-
     A                                      'SERVICIO')
     A            IDESPAIS       3          TEXT('PAIS_DESTINO')
     A                                      COLHDG(-
     A                                      'PAIS' -
     A                                      'DESTINO')
     A            IDESMONE       3          TEXT('MONEDA_DESTINO')
     A                                      COLHDG(-
     A                                      'MANEDA' -
     A                                      'DESTINO')
     A            ISAGENCD      10          TEXT('S_AGENTE')
     A                                      COLHDG(-
     A                                      'S_AGENTE' -
     A                                      'CD')
     A            ISPAISCD       3          TEXT('S_PAISCD')
     A                                      COLHDG(-
     A                                      'S_PAIS' -
     A                                      'CD')
     A            ISTATECD       3          TEXT('S_ESTADO')
     A                                      COLHDG(-
     A                                      'S_ESTADO' -
     A                                      'CD')
     A            IRAGENCD      10          TEXT('R_AGENTE_CD')
     A                                      COLHDG(-
     A                                      'R AGENTE' -
     A                                      'CD')
     C* ===========================================
     A            ITICUENTA      3          TEXT('TIPO.CUENTA')
     A                                      COLHDG(-
     A                                      'TIPO' -
     A                                      'CUENTA')
     A            INOCUENTA     15          TEXT('NO.CUENTA')
     A                                      COLHDG(-
     A                                      'NUMERO' -
     A                                      'CUENTA')
     A            INUMREFER     20          TEXT('NO.REFERE')
     A                                      COLHDG(-
     A                                      'NUMERO' -
     A                                      'REFERENCIA')
     A            ISTSREM        3A         TEXT('ESTATUS DE REMESAS')
     A            ISTSPRO       10A         TEXT('ESTATUS DE PROCESO')
     A            IERR           4A         TEXT('MENSAJE')
     A            IERRDSC       70A         TEXT('DESCRI.MENSAJE')
     A            IDSCRECH      70A         TEXT('DESCRI.RECHAZO')
     A* DATOS RECIBIDOS DE RESPUESTA QRY
     A            ACODPAIS       3
     A            ACODMONED      3
     A            AMTOENVIA     20
     A            AMTOCALCU     20
     A            AFACTCAMB     21
     A* remitente
     A            BPRIMNAME     40
     A            BSECUNAME     40
     A            BAPELLIDO     40
     A            BSEGUAPE      40
     A            BDIRECCIO     65
     A            BCIUDAD       40
     A            BESTADO        3
     A            BPAIS          3
     A            BCODPOST      10
     A            BTELEFONO     15
     A* beneficiario
     A            CPRIMNAME     40
     A            CSECUNAME     40
     A            CAPELLIDO     40
     A            CSEGUAPE      40
     A            CDIRECCIO     65
     A            CCIUDAD       40
     A            CESTADO        3
     A            CPAIS          3
     A            CCODPOST      10
     A            CTELEFONO     15
     A*    IDENTIDAD DE LA PERSONA QUE SE LE PAGO
     A            DTIDENT       20
     A*    DATOS DE REFERENCIA
     A            ESALEDT        8
     A            EMONREFER      3
     A            ETASAREFE     21
     A            EMTOREF       20
     A
     A          K INOCONFIR
