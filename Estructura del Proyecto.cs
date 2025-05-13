{
    "header": {
        "responseId": "2611ddf4-64db-4c52-ad78-36a51823d920",
        "timestamp": "2025-05-13T03:36:42.7610835Z",
        "processingtime": "100ms",
        "statuscode": "1000",
        "message": "QRYI ACCEPTED ORDER",
        "requestheader": {
            "h-request-id": "010604a7-30b1-496a-af69-5019820a35c3",
            "h-channel": "Caja",
            "h-terminal": "POSTAM",
            "h-organization": "Davivienda",
            "h-user-id": "PRUEBAS",
            "h-provider": "POSTAM",
            "h-session-id": "0c6a608d-808d-4b4b-8ed9-b1465f95b17b",
            "h-client-ip": "localhost",
            "h-timestamp": "20250221"
        }
    },
    "data": {
        "opCode": "1000",
        "processMsg": "QRYI ACCEPTED ORDER",
        "errorParamFullName": "",
        "transStatusCd": "ONP",
        "transStatusDt": "20250207",
        "processDt": "20250512",
        "processTm": "223642",
        "data": {
            "saleDt": "20250207",
            "saleTm": "174941",
            "serviceCd": "MTR",
            "paymentTypeCd": "CSA",
            "origCountryCd": "USA",
            "origCurrencyCd": "USD",
            "destCountryCd": "HND",
            "DestCurrencyCd": "",
            "destAmount": "7657.0200",
            "origAmount": "300.0000",
            "exchangeRateFx": "25.5234000000",
            "marketRefCurrencyCd": "USD",
            "marketRefCurrencyFx": "25.52340",
            "marketRefCurrencyAm": "300.00",
            "sAgentCd": "BTS",
            "sPaymentTypeCd": "",
            "sAccountTypeCd": "",
            "sAccountNm": "",
            "sBankCd": "",
            "sBankRefNm": "",
            "rAccountTypeCd": "",
            "rAccountName": "",
            "rAgentCd": "",
            "rAgentRegionSd": "",
            "rAgentBranchSd": "",
            "bankRefNm": "",
            "promotionCode": "",
            "sender": {
                "firstName": "JOSE",
                "middleName": "LUIS",
                "lastName": "DELAGARZA",
                "motherMName": "VALDOVINOS",
                "address": {
                    "address": "5403 UNIVERSITY AVE",
                    "city": "SAN DIEGO",
                    "stateCd": "CA ",
                    "countryCd": "USA",
                    "zipCode": "92105",
                    "phone": "+16192659701"
                }
            },
            "recipient": {
                "firstName": "LUZ",
                "middleName": "AYDEE",
                "lastName": "ALVAREZ",
                "motherMName": "CRUZ",
                "identif_Type_Cd": "",
                "identif_Nm": "",
                "foreing_Name": {
                    "firstName": "",
                    "middleName": "",
                    "lastName": "",
                    "motherMName": ""
                },
                "address": {
                    "address": "DOMICILIO CONOCIDO",
                    "city": "CIUDAD CONOCIDA",
                    "stateCd": "ATL",
                    "countryCd": "HND",
                    "zipCode": "31001",
                    "phone": "+5047414233"
                }
            },
            "recipientIdentification": {
                "typeCd": "",
                "issuerCd": "",
                "issuerStateCd": "",
                "issuerCountryCd": "",
                "identFnum": "",
                "expirationDt": ""
            },
            "senderIdentification": {
                "typeCd": "",
                "issuerCd": "",
                "issuerStateCd": "",
                "issuerCountryCd": "",
                "identFnum": "",
                "expirationDt": ""
            }
        }
    }
}



docNode = yajl_stmf_load_tree(%Trim($vFileSav):errMsg);

               if errMsg <> '';

                  $error   ='001';
                  $mensaje   ='Error mapeo estructura json';

               Else;

                 //Verifica la existencia de campo
                 node = YAJL_object_find(docNode:'error');
                 resultP.error = YAJL_is_true(node);

                 //Extrae valores de nodo
                 node = YAJL_object_find(docNode:'error');
                 $error = YAJL_get_string(node);

                 node = YAJL_object_find(docNode:'mensaje');
                 $mensaje = YAJL_get_string(node);

              endif;

