Al igual que en la tabla que creaste anteriormente, necesito una tabla para esta respuesta, ademas de la tabla crea el insert de la misma para C#.
La tabla se va a llamar BTS02REP y como es para el as400 los nombres de los campos no pueden pasarse de 8 caracteres, similar a la tabla anterior, ademas tendra un campo GUID de 100 caracteres como parte de la llave,
Esa tabla entregamela en este formato que te muestro como ejemplo para copiar y pegar
A                                      UNIQUE                       
A          R $BTS01REP                                              
A            REP00UID     100A         COLHDG('Guid')               
A            REP01DCOR     14S 0       COLHDG('DetAct Correlativo') 
A            REP02ATTC      4A         COLHDG('Agent Trans Ty Code')
A            REP03ACTC      3A         COLHDG('Contract Type Code') 
A            REP04ADT       8A         COLHDG('Activity DT')        
A            REP05ACD       3A         COLHDG('Agent CD')           
A            REP06SCD       3A         COLHDG('Service CD')         
A            REP06OCCD      3A         COLHDG('Orig Country CD')    
A            REP08OCCD      3A         COLHDG('Orig Currency CD')   
A            REP09DCCD      3A         COLHDG('Dest Country CD')    
A            REP10DCCD      3A         COLHDG('Dest Currency CD')   
A            REP11PTCD      3A         COLHDG('Payment Type CD')    
A            REP12OACD      3A         COLHDG('O Agent Cd')         
A            REP13DCOR     14S 0       COLHDG('Details Correlativo')
A            REP14MTC       4A         COLHDG('Movement Type Code') 
A            REP15CNM      11A         COLHDG('Confirmation Number')

"data": {
        "opCode": "1308",
        "processMsg": "SDEP ACCEPTED ORDER",
        "errorParamFullName": "",
        "processDt": "20250707",
        "processTm": "101521",
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
                    "senderStateCode": "TX ",
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
                            "stateCode": "CA ",
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
                    "senderStateCode": "TX ",
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
                            "stateCode": "TX ",
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
                    "senderStateCode": "TX ",
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
                            "stateCode": "TX ",
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
                    "senderStateCode": "TX ",
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
                            "stateCode": "TX ",
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
                    "senderStateCode": "TX ",
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
                            "stateCode": "TX ",
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
                    "senderStateCode": "TX ",
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
                            "stateCode": "TX ",
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
                    "senderStateCode": "TX ",
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
                            "stateCode": "CA ",
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
                    "senderStateCode": "TX ",
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
                            "stateCode": "TX ",
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
                    "senderStateCode": "CA ",
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
                            "stateCode": "AZ ",
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
                    "senderStateCode": "CA ",
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
                            "stateCode": "AZ ",
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
