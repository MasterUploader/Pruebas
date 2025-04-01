// ========================================================
// Procedimiento Enviar Posteo al API
// ========================================================
dcl-proc EnviarPost;

   dcl-s rc int(10);
   dcl-s fd int(10);
   dcl-s filePath pointer;

   dcl-s headers varchar(200);
   dcl-s responseLen int(10);

   // Preparar punteros dinámicos con datos de GetApiConfig
   urlPtr = %addr(pUrlPost);
   headers = 'Content-Type: application/json';
   hdrPtr = %addr(headers);
   reqPtr = %addr(jsonBuffer);
   resPtr = %addr(response);
   responseLen = %len(response);

   // Realizar el POST usando libhttp_post
   rc = libhttp_post(
         reqPtr: %len(%trim(jsonBuffer)):
         resPtr:
         responseLen:
         hdrPtr:
         urlPtr
       );

   // Validaciones del resultado
   if rc < 0;
      // Error grave de conexión
      response = '{ "error": "Fallo de conexión o red." }';
   elseif rc > 0;
      // Error HTTP
      response = '{ "error": "Error HTTP. Código RC=' + %char(rc) + '" }';
   elseif %trim(response) = *blanks;
      // Respuesta vacía
      response = '{ "error": "Respuesta vacía de la API. RC=0" }';
   elseif %scan('error': %xlate('":,{}[]' : '        ' : %trim(response))) > 0;
      // Contenido contiene palabra error
      response = '{ "warning": "La API respondió con posible error" }';
   endif;

   // Guardar la respuesta en un archivo
   filePath = %addr(vFullFile);
   fd = IFS_OPEN(filePath: O_WRONLY+O_CREAT+O_TRUNC: 0);

   if fd >= 0;
      callp IFS_WRITE(fd: %addr(response): %len(%trim(response)));
      callp IFS_CLOSE(fd);
   endif;

end-proc;
