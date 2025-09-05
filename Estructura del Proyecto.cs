Con los camhios que realizaste ya no se muestra el log HTTP, te muestro como quedo el log, al cual le falta el HTTTP

---------------------------Inicio de Log-------------------------
Inicio: 2025-09-05 09:07:58
-------------------------------------------------------------------

---------------------------Enviroment Info-------------------------
Inicio: 2025-09-05 09:07:58
-------------------------------------------------------------------
Application: API_1_TERCEROS_REMESADORAS
Environment: Development
ContentRoot: C:\Git\Librerias Davivienda\Temporal\API_1_TERCEROS_REMESADORAS
Execution ID: 0HNFCHNQVJC1B:0000000B
Client IP: ::1
User Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36
Machine Name: HNCSTG015243WAP
OS: Microsoft Windows NT 10.0.20348.0
Host: localhost:7116
Distribución: N/A
  -- Extras del HttpContext --
    Scheme              : https
    Protocol            : HTTP/2
    Method              : POST
    Path                : /v1/Bts/ReporteriaSDEP
    Query               : 
    ContentType         : application/json-patch+json
    ContentLength       : 401
    ClientPort          : 64020
    LocalIp             : ::1
    LocalPort           : 7116
    ConnectionId        : 0HNFCHNQVJC1B
    Referer             : https://localhost:7116/swagger/index.html
----------------------------------------------------------------------
---------------------------Enviroment Info-------------------------
Fin: 2025-09-05 09:07:58
-------------------------------------------------------------------

-------------------------------------------------------------------------------
Controlador: Bts
Action: ObtenerTransaccionesClienteBts
Inicio: 2025-09-05 09:07:58
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------

----------------------------------Request Info---------------------------------
Inicio: 2025-09-05 09:07:58
-------------------------------------------------------------------------------
Método: POST
URL: /v1/Bts/ReporteriaSDEP
Cuerpo:

                              {
                                "header": {
                                  "h-request-id": "string",
                                  "h-channel": "string",
                                  "h-terminal": "string",
                                  "h-organization": "string",
                                  "h-user-id": "string",
                                  "h-provider": "string",
                                  "h-session-id": "string",
                                  "h-client-ip": "string",
                                  "h-timestamp": "string"
                                },
                                "body": {
                                  "execTR": {
                                    "request": {
                                      "data": {
                                        "rowCount": "50"
                                      }
                                    }
                                  }
                                }
                              }
----------------------------------Request Info---------------------------------
Fin: 2025-09-05 09:07:58
-------------------------------------------------------------------------------

===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-05 09:09:20.498
Duración          : 140.203 ms
Base de Datos     : DVHNDEV
IP                : 166.178.81.19
Puerto            : 0
Esquema           : bcah96dta
Tabla             : bcah96dta.btsacta
Veces Ejecutado   : 1
Filas Afectadas   : 10
SQL:
INSERT INTO BCAH96DTA.BTSACTA (INOCONFIR, IDATRECI, IHORRECI, IDATCONF, IHORCONF, IDATVAL, IHORVAL, IDATPAGO, IHORPAGO, IDATACRE, IHORACRE, IDATRECH, IHORRECH, ITIPPAGO, ISERVICD, IDESPAIS, IDESMONE, ISAGENCD, ISPAISCD, ISTATECD, IRAGENCD, ITICUENTA, INOCUENTA, INUMREFER, ISTSREM, ISTSPRO, IERR, IERRDSC, IDSCRECH, ACODPAIS, ACODMONED, AMTOENVIA, AMTOCALCU, AFACTCAMB, BPRIMNAME, BSECUNAME, BAPELLIDO, BSEGUAPE, BDIRECCIO, BCIUDAD, BESTADO, BPAIS, BCODPOST, BTELEFONO, CPRIMNAME, CSECUNAME, CAPELLIDO, CSEGUAPE, CDIRECCIO, CCIUDAD, CESTADO, CPAIS, CCODPOST, CTELEFONO, DTIDENT, ESALEDT, EMONREFER, ETASAREFE, EMTOREF) VALUES ('89901012400', '20250905', '090915327', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'HNL', 'BTS', 'USA', 'TX', 'HSH', 'NOT', '5200342008', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '5400.00', '140754.24', '26.0656000000', 'MIGUEL', 'ANGEL', 'SEPULVEDA', 'HENDERSON', '820 N WILCOX AVE', 'MONTEBELLO', 'CA', 'USA', '90640', '13238873090', 'EUGENIA', 'FATIMA', 'GALEANO', 'DIAZ', 'DOMICILIO CONOCIDO', 'CIUDAD CONOCIDA', 'ATL', 'HND', '31001', '+5045244034', ' ', '20250703', 'USD', '26.0656000000', '5400.00'), ('89901012418', '20250905', '090915331', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'HNL', 'BTS', 'USA', 'TX', 'HSH', 'NOT', '3011618679', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '3500.00', '91229.60', '26.0656000000', 'MIGUEL', 'ANGEL', 'MONDRAGON', 'BUSTILLOS', '721 CASTROVILLE RD', 'SAN ANTONIO', 'TX', 'USA', '78237', '+12104320949', 'FABIOLA', 'PATRICIA', 'BONILLA', 'HERNANDEZ', 'DOMICILIO CONOCIDO', 'CIUDAD CONOCIDA', 'ATL', 'HND', '31001', '+5044161416', ' ', '20250703', 'USD', '26.0656000000', '3500.00'), ('89901012426', '20250905', '090915331', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'USD', 'BTS', 'USA', 'TX', 'HSH', 'NOT', '5200368287', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '2000.00', '2000.00', '1.0000000000', 'MIGUEL', 'ANGEL', 'CHAVARRIA', 'TOLENTINO', '721 CASTROVILLE RD', 'SAN ANTONIO', 'TX', 'USA', '78237', '+12104320949', 'EUGENIA', 'FATIMA', 'GALEANO', 'DIAZ', 'DOMICILIO CONOCIDO', 'CIUDAD CONOCIDA', 'ATL', 'HND', '31001', '+5048281414', ' ', '20250703', 'USD', '1.0000000000', '2000.00'), ('89901012434', '20250905', '090915331', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'USD', 'BTS', 'USA', 'TX', 'HSH', 'NOT', '6303133567', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '2100.00', '2100.00', '1.0000000000', 'MANUEL', 'ANTONIO', 'ONTIVEROS', 'MARAVILLA', '721 CASTROVILLE RD', 'SAN ANTONIO', 'TX', 'USA', '78237', '+12104320949', 'FABIOLA', 'PATRICIA', 'BONILLA', 'HERNANDEZ', 'DOMICILIO CONOCIDO', 'CIUDAD CONOCIDA', 'ATL', 'HND', '31001', '+5047321536', ' ', '20250703', 'USD', '1.0000000000', '2100.00'), ('89901012442', '20250905', '090915331', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'USD', 'BTS', 'USA', 'TX', 'HSH', 'NOT', '1041202367', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '2000.00', '2000.00', '1.0000000000', 'MANUEL', 'ANTONIO', 'HENRIQUEZ', 'VILLARREAL', '721 CASTROVILLE RD', 'SAN ANTONIO', 'TX', 'USA', '78237', '+12104320949', 'CARLOS', 'ROBERTO', 'REYES', 'ARGUETA', 'DOMICILIO CONOCIDO', 'CIUDAD CONOCIDA', 'ATL', 'HND', '31001', '+5044443114', ' ', '20250703', 'USD', '1.0000000000', '2000.00'), ('89901012459', '20250905', '090915331', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'USD', 'BTS', 'USA', 'TX', 'HSH', 'NOT', '1041202367', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '2000.00', '2000.00', '1.0000000000', 'MANUEL', 'ANTONIO', 'SOLORZANO', 'VALENZUELA', '721 CASTROVILLE RD', 'SAN ANTONIO', 'TX', 'USA', '78237', '+12104320949', 'CARLOS', 'ROBERTO', 'REYES', 'ARGUETA', 'DOMICILIO CONOCIDO', 'CIUDAD CONOCIDA', 'ATL', 'HND', '31001', '+5048332044', ' ', '20250703', 'USD', '1.0000000000', '2000.00'), ('89901012475', '20250905', '090915331', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'USD', 'BTS', 'USA', 'TX', 'HSH', 'NOT', '5013407495', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '3000.00', '3000.00', '1.0000000000', 'HUGO', 'ERNESTO', 'SANTILLAN', 'VILLALOBOS', '5403 UNIVERSITY AVE', 'SAN DIEGO', 'CA', 'USA', '92105', '+(1)6192-659701', 'ZENIA', 'MARCELA', 'SUAZO', 'LOPEZ', 'DOMICILIO CONOCIDO', 'CIUDAD CONOCIDA', 'ATL', 'HND', '31001', '+5047312313', ' ', '20250703', 'USD', '1.0000000000', '3000.00'), ('89901012467', '20250905', '090915331', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'USD', 'BTS', 'USA', 'TX', 'HSH', 'NOT', '5013407495', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '3000.00', '3000.00', '1.0000000000', 'MANUEL', 'ANTONIO', 'RODRIGUES', 'VILLANUEVA', '527 FAIR AVE', 'SAN ANTONIO', 'TX', 'USA', '78223', 'NONE', 'ZENIA', 'MARCELA', 'SUAZO', 'LOPEZ', 'DOMICILIO CONOCIDO', 'CIUDAD CONOCIDA', 'ATL', 'HND', '31001', '+5044272838', ' ', '20250703', 'USD', '1.0000000000', '3000.00'), ('70908937005', '20250905', '090915331', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'HNL', 'BTS', 'USA', 'CA', 'HSH', 'NOT', '7649274531', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '95.02', '2483.81', '26.1398000000', 'CARMELO', 'DE JESUS', 'GUTIERREZ', 'TIRADO', 'ADDRESS DE CLIENTE', 'YUMA', 'AZ', 'USA', '90210', '7476691161852', 'YOSELIN', ' ', 'SANTOS', 'RODRIGUEZ', 'ADDRESS BENEFICIARIO', 'TEGUCIGALPA', 'TEG', 'HND', '80100', ' ', ' ', '20250703', 'USD', '26.1398000000', '95.02'), ('70976846336', '20250905', '090915331', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'ACC', 'MTR', 'HND', 'USD', 'BTS', 'USA', 'CA', 'HSH', 'NOT', '7649274531', ' ', ' ', 'RECIBIDA', '1308', 'SDEP ACCEPTED ORDER', ' ', 'USA', 'USD', '100.00', '100.00', '1.0000000000', 'CARMELO', 'DE JESUS', 'GUTIERREZ', 'TIRADO', 'ADDRESS DE CLIENTE', 'YUMA', 'AZ', 'USA', '90210', '7476691161852', 'YOSELIN', ' ', 'SANTOS', 'RODRIGUEZ', 'ADDRESS BENEFICIARIO', 'TEGUCIGALPA', 'TEG', 'HND', '80100', ' ', ' ', '20250703', 'USD', '1.0000000000', '100.00')
================================

----------------------------------Response Info---------------------------------
Inicio: 2025-09-05 09:09:20
-------------------------------------------------------------------------------
Código Estado: 200
Headers: [Content-Type, application/json; charset=utf-8]; [Content-Length, 11969]
Cuerpo:

                              {
                                "header": {
                                  "reponseId": "3688fab2-1700-4f64-9462-f76e960b05a4",
                                  "timestamp": "2025-09-05T15:09:20.6657889Z",
                                  "processingTime": "100ms",
                                  "statusCode": "1308",
                                  "message": "SDEP ACCEPTED ORDER",
                                  "requestHeader": {
                                    "hRequestId": "string",
                                    "hChannel": "string",
                                    "hTerminal": "string",
                                    "hOrganization": "string",
                                    "hUserId": "string",
                                    "hProvider": "string",
                                    "hSessionId": "string",
                                    "hClientIp": "string",
                                    "hTimestamp": "string"
                                  }
                                },
                                "data": {
                                  "opCode": "1308",
                                  "processMsg": "SDEP ACCEPTED ORDER",
                                  "errorParamFullName": "",
                                  "processDt": "20250728",
                                  "processTm": "151412",
                                  "deposits": [
                                    {
                                      "data": {
                                        "confirmationNumber": "89901012400",
                                        "saleMovementId": "17373378",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "HNL",
                                        "originAmount": "5400.00",
                                        "destinationAmount": "140754.24",
                                        "exchangeRateFx": "26.0656000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "26.0656000000",
                                        "marketRefCurrencyAmount": "5400.00",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "TX",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "5200342008",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "MIGUEL",
                                          "middleName": "ANGEL",
                                          "lastName": "SEPULVEDA",
                                          "motherMaidenName": "HENDERSON",
                                          "address": {
                                            "addressLine": "820 N WILCOX AVE",
                                            "city": "MONTEBELLO",
                                            "stateCode": "CA",
                                            "countryCode": "USA",
                                            "zipCode": "90640",
                                            "phone": "13238873090"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "EUGENIA",
                                          "middleName": "FATIMA",
                                          "lastName": "GALEANO",
                                          "motherMaidenName": "DIAZ",
                                          "address": {
                                            "addressLine": "DOMICILIO CONOCIDO",
                                            "city": "CIUDAD CONOCIDA",
                                            "stateCode": "ATL",
                                            "countryCode": "HND",
                                            "zipCode": "31001",
                                            "phone": "+5045244034"
                                          }
                                        }
                                      }
                                    },
                                    {
                                      "data": {
                                        "confirmationNumber": "89901012418",
                                        "saleMovementId": "17373379",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "HNL",
                                        "originAmount": "3500.00",
                                        "destinationAmount": "91229.60",
                                        "exchangeRateFx": "26.0656000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "26.0656000000",
                                        "marketRefCurrencyAmount": "3500.00",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "TX",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "3011618679",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "MIGUEL",
                                          "middleName": "ANGEL",
                                          "lastName": "MONDRAGON",
                                          "motherMaidenName": "BUSTILLOS",
                                          "address": {
                                            "addressLine": "721 CASTROVILLE RD",
                                            "city": "SAN ANTONIO",
                                            "stateCode": "TX",
                                            "countryCode": "USA",
                                            "zipCode": "78237",
                                            "phone": "+12104320949"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "FABIOLA",
                                          "middleName": "PATRICIA",
                                          "lastName": "BONILLA",
                                          "motherMaidenName": "HERNANDEZ",
                                          "address": {
                                            "addressLine": "DOMICILIO CONOCIDO",
                                            "city": "CIUDAD CONOCIDA",
                                            "stateCode": "ATL",
                                            "countryCode": "HND",
                                            "zipCode": "31001",
                                            "phone": "+5044161416"
                                          }
                                        }
                                      }
                                    },
                                    {
                                      "data": {
                                        "confirmationNumber": "89901012426",
                                        "saleMovementId": "17373380",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "USD",
                                        "originAmount": "2000.00",
                                        "destinationAmount": "2000.00",
                                        "exchangeRateFx": "1.0000000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "1.0000000000",
                                        "marketRefCurrencyAmount": "2000.00",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "TX",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "5200368287",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "MIGUEL",
                                          "middleName": "ANGEL",
                                          "lastName": "CHAVARRIA",
                                          "motherMaidenName": "TOLENTINO",
                                          "address": {
                                            "addressLine": "721 CASTROVILLE RD",
                                            "city": "SAN ANTONIO",
                                            "stateCode": "TX",
                                            "countryCode": "USA",
                                            "zipCode": "78237",
                                            "phone": "+12104320949"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "EUGENIA",
                                          "middleName": "FATIMA",
                                          "lastName": "GALEANO",
                                          "motherMaidenName": "DIAZ",
                                          "address": {
                                            "addressLine": "DOMICILIO CONOCIDO",
                                            "city": "CIUDAD CONOCIDA",
                                            "stateCode": "ATL",
                                            "countryCode": "HND",
                                            "zipCode": "31001",
                                            "phone": "+5048281414"
                                          }
                                        }
                                      }
                                    },
                                    {
                                      "data": {
                                        "confirmationNumber": "89901012434",
                                        "saleMovementId": "17373381",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "USD",
                                        "originAmount": "2100.00",
                                        "destinationAmount": "2100.00",
                                        "exchangeRateFx": "1.0000000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "1.0000000000",
                                        "marketRefCurrencyAmount": "2100.00",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "TX",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "6303133567",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "MANUEL",
                                          "middleName": "ANTONIO",
                                          "lastName": "ONTIVEROS",
                                          "motherMaidenName": "MARAVILLA",
                                          "address": {
                                            "addressLine": "721 CASTROVILLE RD",
                                            "city": "SAN ANTONIO",
                                            "stateCode": "TX",
                                            "countryCode": "USA",
                                            "zipCode": "78237",
                                            "phone": "+12104320949"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "FABIOLA",
                                          "middleName": "PATRICIA",
                                          "lastName": "BONILLA",
                                          "motherMaidenName": "HERNANDEZ",
                                          "address": {
                                            "addressLine": "DOMICILIO CONOCIDO",
                                            "city": "CIUDAD CONOCIDA",
                                            "stateCode": "ATL",
                                            "countryCode": "HND",
                                            "zipCode": "31001",
                                            "phone": "+5047321536"
                                          }
                                        }
                                      }
                                    },
                                    {
                                      "data": {
                                        "confirmationNumber": "89901012442",
                                        "saleMovementId": "17373382",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "USD",
                                        "originAmount": "2000.00",
                                        "destinationAmount": "2000.00",
                                        "exchangeRateFx": "1.0000000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "1.0000000000",
                                        "marketRefCurrencyAmount": "2000.00",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "TX",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "1041202367",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "MANUEL",
                                          "middleName": "ANTONIO",
                                          "lastName": "HENRIQUEZ",
                                          "motherMaidenName": "VILLARREAL",
                                          "address": {
                                            "addressLine": "721 CASTROVILLE RD",
                                            "city": "SAN ANTONIO",
                                            "stateCode": "TX",
                                            "countryCode": "USA",
                                            "zipCode": "78237",
                                            "phone": "+12104320949"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "CARLOS",
                                          "middleName": "ROBERTO",
                                          "lastName": "REYES",
                                          "motherMaidenName": "ARGUETA",
                                          "address": {
                                            "addressLine": "DOMICILIO CONOCIDO",
                                            "city": "CIUDAD CONOCIDA",
                                            "stateCode": "ATL",
                                            "countryCode": "HND",
                                            "zipCode": "31001",
                                            "phone": "+5044443114"
                                          }
                                        }
                                      }
                                    },
                                    {
                                      "data": {
                                        "confirmationNumber": "89901012459",
                                        "saleMovementId": "17373383",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "USD",
                                        "originAmount": "2000.00",
                                        "destinationAmount": "2000.00",
                                        "exchangeRateFx": "1.0000000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "1.0000000000",
                                        "marketRefCurrencyAmount": "2000.00",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "TX",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "1041202367",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "MANUEL",
                                          "middleName": "ANTONIO",
                                          "lastName": "SOLORZANO",
                                          "motherMaidenName": "VALENZUELA",
                                          "address": {
                                            "addressLine": "721 CASTROVILLE RD",
                                            "city": "SAN ANTONIO",
                                            "stateCode": "TX",
                                            "countryCode": "USA",
                                            "zipCode": "78237",
                                            "phone": "+12104320949"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "CARLOS",
                                          "middleName": "ROBERTO",
                                          "lastName": "REYES",
                                          "motherMaidenName": "ARGUETA",
                                          "address": {
                                            "addressLine": "DOMICILIO CONOCIDO",
                                            "city": "CIUDAD CONOCIDA",
                                            "stateCode": "ATL",
                                            "countryCode": "HND",
                                            "zipCode": "31001",
                                            "phone": "+5048332044"
                                          }
                                        }
                                      }
                                    },
                                    {
                                      "data": {
                                        "confirmationNumber": "89901012475",
                                        "saleMovementId": "17373385",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "USD",
                                        "originAmount": "3000.00",
                                        "destinationAmount": "3000.00",
                                        "exchangeRateFx": "1.0000000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "1.0000000000",
                                        "marketRefCurrencyAmount": "3000.00",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "TX",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "5013407495",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "HUGO",
                                          "middleName": "ERNESTO",
                                          "lastName": "SANTILLAN",
                                          "motherMaidenName": "VILLALOBOS",
                                          "address": {
                                            "addressLine": "5403 UNIVERSITY AVE",
                                            "city": "SAN DIEGO",
                                            "stateCode": "CA",
                                            "countryCode": "USA",
                                            "zipCode": "92105",
                                            "phone": "+(1)6192-659701"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "ZENIA",
                                          "middleName": "MARCELA",
                                          "lastName": "SUAZO",
                                          "motherMaidenName": "LOPEZ",
                                          "address": {
                                            "addressLine": "DOMICILIO CONOCIDO",
                                            "city": "CIUDAD CONOCIDA",
                                            "stateCode": "ATL",
                                            "countryCode": "HND",
                                            "zipCode": "31001",
                                            "phone": "+5047312313"
                                          }
                                        }
                                      }
                                    },
                                    {
                                      "data": {
                                        "confirmationNumber": "89901012467",
                                        "saleMovementId": "17373386",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "USD",
                                        "originAmount": "3000.00",
                                        "destinationAmount": "3000.00",
                                        "exchangeRateFx": "1.0000000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "1.0000000000",
                                        "marketRefCurrencyAmount": "3000.00",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "TX",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "5013407495",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "MANUEL",
                                          "middleName": "ANTONIO",
                                          "lastName": "RODRIGUES",
                                          "motherMaidenName": "VILLANUEVA",
                                          "address": {
                                            "addressLine": "527 FAIR AVE",
                                            "city": "SAN ANTONIO",
                                            "stateCode": "TX",
                                            "countryCode": "USA",
                                            "zipCode": "78223",
                                            "phone": "NONE"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "ZENIA",
                                          "middleName": "MARCELA",
                                          "lastName": "SUAZO",
                                          "motherMaidenName": "LOPEZ",
                                          "address": {
                                            "addressLine": "DOMICILIO CONOCIDO",
                                            "city": "CIUDAD CONOCIDA",
                                            "stateCode": "ATL",
                                            "countryCode": "HND",
                                            "zipCode": "31001",
                                            "phone": "+5044272838"
                                          }
                                        }
                                      }
                                    },
                                    {
                                      "data": {
                                        "confirmationNumber": "70908937005",
                                        "saleMovementId": "17373486",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "HNL",
                                        "originAmount": "95.02",
                                        "destinationAmount": "2483.81",
                                        "exchangeRateFx": "26.1398000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "26.1398000000",
                                        "marketRefCurrencyAmount": "95.02",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "CA",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "7649274531",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "CARMELO",
                                          "middleName": "DE JESUS",
                                          "lastName": "GUTIERREZ",
                                          "motherMaidenName": "TIRADO",
                                          "address": {
                                            "addressLine": "ADDRESS DE CLIENTE",
                                            "city": "YUMA",
                                            "stateCode": "AZ",
                                            "countryCode": "USA",
                                            "zipCode": "90210",
                                            "phone": "7476691161852"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "YOSELIN",
                                          "middleName": "",
                                          "lastName": "SANTOS",
                                          "motherMaidenName": "RODRIGUEZ",
                                          "address": {
                                            "addressLine": "ADDRESS BENEFICIARIO",
                                            "city": "TEGUCIGALPA",
                                            "stateCode": "TEG",
                                            "countryCode": "HND",
                                            "zipCode": "80100",
                                            "phone": ""
                                          }
                                        }
                                      }
                                    },
                                    {
                                      "data": {
                                        "confirmationNumber": "70976846336",
                                        "saleMovementId": "17373487",
                                        "saleDate": "20250703",
                                        "saleTime": "205003",
                                        "serviceCode": "MTR",
                                        "paymentTypeCode": "ACC",
                                        "originCountryCode": "USA",
                                        "originCurrencyCode": "USD",
                                        "destinationCountryCode": "HND",
                                        "destinationCurrencyCode": "USD",
                                        "originAmount": "100.00",
                                        "destinationAmount": "100.00",
                                        "exchangeRateFx": "1.0000000000",
                                        "marketRefCurrencyCode": "USD",
                                        "marketRefCurrencyFx": "1.0000000000",
                                        "marketRefCurrencyAmount": "100.00",
                                        "senderAgentCode": "BTS",
                                        "senderCountryCode": "USA",
                                        "senderStateCode": "CA",
                                        "recipientAccountTypeCode": "NOT",
                                        "recipientAccountNumber": "7649274531",
                                        "recipientAgentCode": "HSH",
                                        "sender": {
                                          "firstName": "CARMELO",
                                          "middleName": "DE JESUS",
                                          "lastName": "GUTIERREZ",
                                          "motherMaidenName": "TIRADO",
                                          "address": {
                                            "addressLine": "ADDRESS DE CLIENTE",
                                            "city": "YUMA",
                                            "stateCode": "AZ",
                                            "countryCode": "USA",
                                            "zipCode": "90210",
                                            "phone": "7476691161852"
                                          }
                                        },
                                        "recipient": {
                                          "firstName": "YOSELIN",
                                          "middleName": "",
                                          "lastName": "SANTOS",
                                          "motherMaidenName": "RODRIGUEZ",
                                          "address": {
                                            "addressLine": "ADDRESS BENEFICIARIO",
                                            "city": "TEGUCIGALPA",
                                            "stateCode": "TEG",
                                            "countryCode": "HND",
                                            "zipCode": "80100",
                                            "phone": ""
                                          }
                                        }
                                      }
                                    }
                                  ]
                                }
                              }
----------------------------------Response Info---------------------------------
Fin: 2025-09-05 09:09:20
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------
Controlador: Bts
Action: ObtenerTransaccionesClienteBts
Fin: 2025-09-05 09:09:20
-------------------------------------------------------------------------------


[Tiempo Total de Ejecución]: 82571 ms
---------------------------Fin de Log-------------------------
Final: 2025-09-05 09:09:20
-------------------------------------------------------------------

