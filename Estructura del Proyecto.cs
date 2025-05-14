dcl-proc ErrorGenerico export;
  dcl-pi ErrorGenerico;
    error int(10);
    mensaje char(100);
  end-pi;

  dcl-s jsonGen int(10);
  dcl-s jsonStr varchar(32700);
  dcl-s jsonLen int(10);
  dcl-s fd int(10);
  dcl-s errMsg varchar(500);

  // 1. Generar JSON de error con YAJL
  jsonGen = yajl_genOpen(*OFF);

  callp yajl_beginObj();
    callp yajl_addChar('header');
    callp yajl_beginObj();
      callp yajl_addChar('statuscode': %char(error));
      callp yajl_addChar('message': %trim(mensaje));
    callp yajl_endObj();
  callp yajl_endObj();

  callp yajl_copyBuf(0: %addr(jsonStr): %size(jsonStr): jsonLen);
  callp yajl_genClose();

  // 2. Sobrescribir el archivo de respuesta
  fd = open(%addr(vFullFileR): O_WRONLY + O_TRUNC + O_CREAT: 0666);
  if fd > 0;
    callp write(fd: %addr(jsonStr): jsonLen);
    callp close(fd);
  endif;
end-proc;
