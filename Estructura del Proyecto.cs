Como coloco este codigo rpgle

          callp yajl_addChar('pMerchantID': %trim(input.MerchantID));
          callp yajl_addChar('pTerminalID': %trim(input.TerminalID));
          callp yajl_addChar('pPrimaryAccountNumber' :%trim(input.PrimaryAccNumber));
          callp yajl_addChar('pDateExpiration': %trim(input.DateExpiration));
          callp yajl_addChar('pCVV2': %trim(input.CVV2));
          callp yajl_addChar('pAmount': %trim(input.Amount));
          callp yajl_addChar('pSystemsTraceAuditNumber':%trim(input.Amount));

Sabiendo que el limite no puede sobrepasar cierta longitud, por ejemplo la linea callp yajl_addChar('pMerchantID': %trim(input.MerchantID));, marca el limite.
