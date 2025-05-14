        // ========================================================
        // Procedimiento Enviar Posteo al API
        // ========================================================
        dcl-proc EnviarPost;
          dcl-s fd int(10);
          dcl-s filePath pointer;
          dcl-s bytesRead int(10);
          dcl-s headers varchar(200);
          dcl-s responseLen int(10);

          // ----------------------------------------
          // Realiza el POST, guarda en archivo IFS
          // ----------------------------------------
          //callp http_debug(*on: vFullFileH); //Guarda archivo log
          callp http_debug(*on); //No guarda archivo log

          callp HTTP_setCCSIDs(1208: 0);

          rc = HTTP_POST(%Trim(pUrlPost)
                     : %addr(jsonBuffer )
                     : jsonLen
                     : %Trim(vFullFileR)
                     : 60
                     : HTTP_USERAGENT
                     : 'application/json');

          // Validaciones del resultado        

          if rc < 0;
              // Error grave de conexión
              response = '{ "error": "Fallo de conexión o red." }';
              ErrorGenerico(rc:
                              response);
          elseif rc > 0;
              // Error HTTP
              response = '{ "error": "Error HTTP. Código RC=' + 
              %char(rc) + '" }';
              ErrorGenerico(rc:
                              response);
          elseif %trim(response) = *blanks;
              // Respuesta vacía
              response = '{ "error": "Respuesta vacía de la API. RC=0" }';
              ErrorGenerico(rc:
                              response);
          elseif %scan('error': %xlate('":,{}[]' : '        ' :
                  %trim(response))) > 0;
              // Contenido contiene palabra error
              response = '{ "warning": "La API respondió con posible error" }';
              ErrorGenerico(rc:
                              response);
          endif;

        end-proc;


dcl-proc ErrorGenerico export;
            dcl-pi ErrorGenerico;
               error int(10);
               mensaje char(100);
            end-pi;
            
          dcl-s jsonGen int(10);            
          dcl-s errMsg varchar(500);

           jsonGen = yajl_genOpen(*OFF);

          // Comenzar el objeto JSON principal
          callp yajl_beginObj();
          callp yajl_beginObj();

          // --- HEADER ---
          callp yajl_addChar('header');
          callp yajl_beginObj();
          callp yajl_addChar('statuscode': %char(error));
          callp yajl_addChar('message': %trim(mensaje));
          callp yajl_endObj();
          // --- HEADER ---

          callp yajl_endObj();
          callp yajl_endObj();
          // --- Comenzar el objeto JSON principal ---//

          // Obtener el buffer JSON generado
            yajl_saveBuf(vFullFileR: errMsg);

          //  if errMsg <> '';
          // // handle error
          // endif;

          // Cerrar el generador de JSON
          callp yajl_genClose();         
          
        end-proc;
