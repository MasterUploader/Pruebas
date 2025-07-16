A          R CASHRESPR
     A                                      UNIQUE
     A            IDRESP        36A         TEXT('ID de la respuesta JSON')
     A            TIMESTAMP     25A         TEXT('Fecha/hora ISO 8601')
     A            TRACEID       50A         TEXT('Identificador de trazabilidad')
     A            STATUS         5S 0       TEXT('Código HTTP')
     A            MESSAGE      200A         TEXT('Mensaje del sistema')
     A            ERRORMSG     200A         TEXT('Mensaje de error')

     A            COUNTRYID      3A         TEXT('Código ISO del país')

     A            SEQBAL         3S 0       TEXT('N° balance en el arreglo')
     A            BALTYPE       20A         TEXT('Tipo: Total, CashIn, CashOut')
     A            DATEBAL       25A         TEXT('Fecha/hora del balance')
     A            DEVCODEBAL    30A         TEXT('Código del dispositivo - balance')

     A            SEQTRAN        3S 0       TEXT('N° transacción en el arreglo')
     A            TRANID        50A         TEXT('ID de la transacción')
     A            TRANSDATE     25A         TEXT('Fecha de transacción')
     A            RECEIPTNUM    20A         TEXT('Número de recibo')
     A            TRANTYPE      20A         TEXT('Tipo de transacción')
     A            TRANSSUBT     20A         TEXT('Subtipo transacción')
     A            CASHIERID     20A         TEXT('ID del cajero')
     A            CASHIERNAME   50A         TEXT('Nombre del cajero')
     A            DEVCODETRAN   30A         TEXT('Código del dispositivo - transacción')

     A            SEQCURR        3S 0       TEXT('N° moneda en el arreglo')
     A            CURRCODE       3A         TEXT('Código de moneda')
     A            CURRAMOUNT    15S 2       TEXT('Monto en moneda')

     A            SEQDEN         3S 0       TEXT('N° denominación en el arreglo')
     A            DENVALUE       9S 0       TEXT('Valor de denominación')
     A            DENQTY         9S 0       TEXT('Cantidad')
     A            DENAMOUNT     15S 2       TEXT('Importe total')
     A            DENTYPE       10A         TEXT('Tipo: NOTE o COIN')

     A            ISBALANCE      1A         TEXT('Y/N: Registro es balance')
     A            ISTRANSACTION  1A         TEXT('Y/N: Registro es transacción')

     A          K IDRESP
     A          K SEQBAL
     A          K SEQTRAN
     A          K SEQCURR
     A          K SEQDEN
