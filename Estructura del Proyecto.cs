dcl-proc GenerarRequestJson;
  // Declaración del puntero que usará YAJL
  dcl-s jsonPtr pointer;
  dcl-s jsonLen int(10);
  dcl-s jsonGen pointer;
  dcl-s errMsg varchar(500);

  // Abrir el generador JSON (N o Y, *OFF o *ON)
  callp yajl_genOpen('N'); // Usamos 'N' para no hacer escape en UTF-8

  // Comenzar el objeto JSON principal
  callp yajl_beginObj();

  // --- HEADER ---
  callp yajl_addChar(jsonGen: 'Header');
  callp yajl_beginObj(jsonGen);
  callp yajl_addChar(jsonGen: 'h-request-id': %trim(HRequestId));
  callp yajl_addChar(jsonGen: 'channel': %trim(HChannel));
  callp yajl_addChar(jsonGen: 'session-id': %trim(HSessionId));
  callp yajl_addChar(jsonGen: 'client-ip': %trim(HClientIp));
  callp yajl_addChar(jsonGen: 'user-id': %trim(HUserId));
  callp yajl_addChar(jsonGen: 'provider': %trim(HProvider));
  callp yajl_addChar(jsonGen: 'organization': %trim(HOrganization));
  callp yajl_addChar(jsonGen: 'terminal': %trim(HTerminal));
  callp yajl_addChar(jsonGen: 'timestamp': %trim(HTimestamp));
  callp yajl_endObj(jsonGen);

  // --- REQUEST ---
  callp yajl_addChar(jsonGen: 'Request');
  callp yajl_beginObj(jsonGen);
  callp yajl_addChar(jsonGen: 'AGENT_CD': %trim(AgentCode));
  callp yajl_addChar(jsonGen: 'AGENT_TRANS_TYPE_CODE': %trim(AgentTransactionTypeCode));
  callp yajl_addChar(jsonGen: 'CONFIRMATION_NM': %trim(ConfirmationNm));
  callp yajl_addChar(jsonGen: 'REGION_SD': %trim(RegionSd));
  callp yajl_addChar(jsonGen: 'BRANCH_SD': %trim(BranchSd));
  callp yajl_addChar(jsonGen: 'STATE_CD': %trim(StateCd));
  callp yajl_addChar(jsonGen: 'COUNTRY_CD': %trim(CountryCd));
  callp yajl_addChar(jsonGen: 'USER_MN': %trim(Username));
  callp yajl_addChar(jsonGen: 'TERMINAL_IO': %trim(Terminal));
  callp yajl_addChar(jsonGen: 'AGENT_DT': %trim(AgentDt));
  callp yajl_addChar(jsonGen: 'AGENT_TM': %trim(AgentTm));
  callp yajl_endObj(jsonGen);

  // --- Fin del objeto JSON ---
  callp yajl_endObj(jsonGen);

  // Obtener el buffer JSON generado
  jsonPtr = *null;
  jsonLen = 0;

  if jsonPtr <> *null;
    jsonBuffer = %subst(%str(jsonPtr): 1: jsonLen);
  else;
    jsonBuffer = *blanks;
  endif;

  // Cerrar el generador de JSON
  callp yajl_genClose(jsonGen);

end-proc;
