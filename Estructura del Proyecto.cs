begsr EnviarPost;

  dcl-s rc int(10);

  // 🟡 Preparar punteros dinámicos con datos de GetApiConfig
  urlPtr = %addr(pUrlPost);
  headers = 'Content-Type: application/json';
  hdrPtr = %addr(headers);
  reqPtr = %addr(jsonBuffer);
  resPtr = %addr(response);
  responseLen = %len(response);

  // 🟢 Realizar el POST usando libhttp_post
  rc = libhttp_post(reqPtr: %len(%trim(jsonBuffer)): resPtr: responseLen: hdrPtr: urlPtr);

  // ============================================
  // 🔍 Validaciones del resultado
  // ============================================
  if rc < 0;
     // Error grave de conexión, red, DNS, etc.
     response = '{ "error": "Fallo de conexión o red. RC=' + %char(rc) + '" }';
  elseIf rc > 0;
     // Error HTTP 4xx o 5xx
     response = '{ "error": "Error HTTP. Código RC=' + %char(rc) + '" }';
  elseIf %trim(response) = *blanks;
     // Respuesta vacía
     response = '{ "error": "Respuesta vacía de la API. RC=0" }';
  elseIf %scan('error' : %xlate('"': '': %trim(response))) > 0;
     // Contenido contiene palabra "error" (posible error lógico)
     response = '{ "warning": "La API respondió con posible error. Verificar contenido." , "original": ' + %trim(response) + ' }';
  endif;

  // ============================================
  // 📂 Guardar la respuesta en un archivo en IFS
  // ============================================
  dcl-pr IFS_WRITE extproc('_C_IFS_write');
    fileName pointer value;
    buffer   pointer value;
    length   int(10) value;
  end-pr;

  dcl-pr IFS_OPEN extproc('_C_IFS_open');
    pathName pointer value;
    flags    int(10) value;
    mode     int(10) value;
    options  int(10) value;
  end-pr;

  dcl-pr IFS_CLOSE extproc('_C_IFS_close');
    fd int(10) value;
  end-pr;

  dcl-s fd int(10);
  dcl-s filePath pointer;

  filePath = %addr(vFullFile);
  fd = IFS_OPEN(filePath: 577: 0: 0); // O_WRONLY+O_CREAT+O_TRUNC

  if fd >= 0;
     callp IFS_WRITE(fd: %addr(response): %len(%trim(response)));
     callp IFS_CLOSE(fd);
  endif;

endsr;
