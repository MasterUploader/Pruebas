Si la data viene así:

{
                                "header": {
                                  "h-request-id": "REQ1234567890",
                                  "h-channel": "WEB",
                                  "h-session-id": "SESSION123",
                                  "h-client-ip": "192.168.1.50",
                                  "h-user-id": "USRTEST",
                                  "h-provider": "DaviviendaTNP",
                                  "h-organization": "ORG01",
                                  "h-terminal": "P0055468",
                                  "h-timestamp": "2025-11-17T21:00:00Z"
                                },
                                "body": {
                                  "GetAuthorizationManual": {
                                    "pMerchantID": "4001021             ",
                                    "pTerminalID": "P0055468            ",
                                    "pPrimaryAccountNumber": "5413330057004039    ",
                                    "pDateExpiration": "2512                ",
                                    "pCVV2": "000                 ",
                                    "pAmount": "10000               ",
                                    "pSystemsTraceAuditNumber": "10000               "
                                  }
                                }
                              }


Porque da error Monto debe ser numérico positivo en formato string.
