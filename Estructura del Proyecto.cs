// ==========================================================
// Procedimiento: EnviarPost
// Descripción : Envía el JSON y carga la respuesta en archivo y variable
// ==========================================================
dcl-proc EnviarPost;

  dcl-s filePath pointer;
  dcl-s fd int(10);
  dcl-s bytesRead int(10);

  dcl-s headers varchar(200);
  dcl-s responseLen int(10);

// ----------------------------------------
// Preparar datos dinámicos
// ----------------------------------------
  urlPtr      = %addr(pUrlPost);
  headers     = 'Content-Type: application/json';
  hdrPtr      = %addr(headers);
  reqPtr      = %addr(jsonBuffer);
  resPtr      = %addr(response);
  responseLen = %len(response);

// ----------------------------------------
// Realiza el POST, guarda en archivo IFS
// ----------------------------------------
  rc = HTTP_POST(
        %addr(pUrlPost)
      : %addr(jsonBuffer)
      : %len(%trimr(jsonBuffer))
      : %trim(vFullFile)
      : HTTP_TIMEOUT
      : HTTP_USERAGENT
      : headers
  );

// ----------------------------------------
// Leer el contenido del archivo al buffer
// ----------------------------------------
  filePath = %trim(vFullFile);
  fd = open(filePath : 8 /* solo lectura */);

  if fd >= 0;
    bytesRead = read(fd : %addr(response) : %size(response));
    callp close(fd);
  endif;

end-proc;
