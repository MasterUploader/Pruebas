**FREE
//------------------------------------------------------------
// Archivo: BTSCNSRDS_H.rpgle
// Descripción: Estructuras de datos para mapear la respuesta
//              del servicio de consulta (BTS Consulta)
//------------------------------------------------------------

dcl-ds HeaderDS qualified;
  responseId      char(40);
  timestamp       char(30);
  processingTime  char(20);
  statusCode      char(10);
  message         char(100);
end-ds;

dcl-ds AddressDS qualified;
  address    char(50);
  city       char(30);
  stateCd    char(5);
  countryCd  char(5);
  zipCode    char(10);
  phone      char(20);
end-ds;

dcl-ds IdentificationDS qualified;
  typeCd           char(10);
  issuerCd         char(10);
  issuerStateCd    char(10);
  issuerCountryCd  char(10);
  identifNm        char(30);
  expirationDt     char(8);
end-ds;

dcl-ds PersonDS qualified;
  firstName     char(20);
  middleName    char(20);
  lastName      char(20);
  motherMName   char(20);
  address       likeds(AddressDS);
end-ds;

dcl-ds RecipientDS qualified;
  firstName     char(20);
  middleName    char(20);
  lastName      char(20);
  motherMName   char(20);
  identifTypeCd char(10);
  identifNm     char(30);
  foreignName   likeds(PersonDS);
  address       likeds(AddressDS);
end-ds;

dcl-ds DataDetailDS qualified;
  saleDt         char(8);
  saleTm         char(6);
  serviceCd      char(10);
  paymentTypeCd  char(10);
  origCountryCd  char(5);
  origCurrencyCd char(5);
  destCountryCd  char(5);
  destCurrencyCd char(5);
  originAm       char(20);
  destinationAm  char(20);
  exchRateFx     char(20);
  marketRefCurrencyCd  char(5);
  marketRefCurrencyAm  char(20);
  sAgentCd       char(10);
  rAccountTypeCd char(10);
  rAccountNm     char(50);
  rAgentCd       char(10);
  rAgentRegionSd char(10);
  rAgentBranchSd char(10);
  sender         likeds(PersonDS);
  recipient      likeds(RecipientDS);
  senderIdent    likeds(IdentificationDS);
  recipientIdent likeds(IdentificationDS);
end-ds;

dcl-ds DataDS qualified;
  opCode         char(10);
  processMsg     char(100);
  errorParamFullName char(100);
  transStatusCd  char(10);
  transStatusDt  char(8);
  processDt      char(8);
  processTm      char(6);
  detail         likeds(DataDetailDS);
end-ds;

dcl-ds FullResponseDS qualified;
  header likeds(HeaderDS);
  data   likeds(DataDS);
end-ds;
