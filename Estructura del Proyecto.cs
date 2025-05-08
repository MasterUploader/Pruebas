 jsonGen = yajl_genOpen(*OFF);

          // Comenzar el objeto JSON principal
          callp yajl_beginObj();

          // --- HEADER ---
          callp yajl_addChar('header');
          callp yajl_beginObj();
          callp yajl_addChar('h-request-id': %trim(input.HRequestId));
          callp yajl_addChar('h-channel': %trim(input.HChannel));
          callp yajl_addChar('h-session-id': %trim(input.HSessionId));
          // callp yajl_addChar('h-client-ip': %trim(input.HClientIp));
          callp yajl_addChar('h-client-ip': %trim('LOCALHOST'));
          callp yajl_addChar('h-user-id': %trim(input.HUserId));
          callp yajl_addChar('h-provider': %trim(input.HProvider));
          callp yajl_addChar('h-organization': %trim(input.HOrganization));
          callp yajl_addChar('h-terminal': %trim(input.HTerminal));
          //callp yajl_addChar('h-timestamp': %trim(input.HTimestamp));
          callp yajl_addChar('h-timestamp': %trim(vDate8));
          callp yajl_endObj();
          // --- HEADER ---

          // --- Body ---//
          callp yajl_addChar('body');
          callp yajl_beginObj();

          //--- ExecTR ---//
          callp yajl_addChar('execTR');
          callp yajl_beginObj();

          // --- REQUEST --- //
          callp yajl_addChar('request');
          callp yajl_beginObj();
          // callp yajl_addChar('AGENT_CD': %trim(AgentCode));
          // callp yajl_addChar('AGENT_TRANS_TYPE_CODE'
          // : %trim(AgentTransactionTypeCode));

          // --- Data --- //
          callp yajl_addChar('data');
          callp yajl_beginObj();
          callp yajl_addChar('confirmationNm': %trim(input.ConfirmationNm));

          // --- agent --- //
          callp yajl_addChar('agent');
          callp yajl_beginObj();
          callp yajl_addChar('regionSd': %trim(input.RegionSd));
          callp yajl_addChar('branchSd': %trim(input.BranchSd));
          callp yajl_addChar('stateCd': %trim(input.StateCd));
          //callp yajl_addChar('countryCd': %trim(input.CountryCd));
          callp yajl_addChar('username': %trim(input.Username));
          callp yajl_addChar('terminal': %trim(input.Terminal));
          //callp yajl_addChar('agentDt': %trim(input.AgentDt));
          //callp yajl_addChar('agentTm': %trim(input.AgentTm));
          callp yajl_addChar('agentDt': %trim(vDate8));
          callp yajl_addChar('agentTm': %trim(vTime6));

          callp yajl_endObj();
          // --- agent --- //

          callp yajl_endObj();
          // --- Data --- //

          callp yajl_endObj();
          // --- REQUEST --- //

          callp yajl_endObj();
          //--- ExecTR ---//

          callp yajl_endObj();
          // --- Body ---//

          callp yajl_endObj();
          // --- Comenzar el objeto JSON principal ---//

          // Obtener el buffer JSON generado
           CALLP yajl_copyBuf( 0
                      : %addr(jsonBuffer)
                      : %size(jsonBuffer)
                      : jsonLen );
