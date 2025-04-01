// =======================================================
// Procedimiento que guarda la respuesta JSON en el IFS
// =======================================================
dcl-proc GuardarResponseJson;

   dcl-s fd int(10);
   dcl-s filePath pointer;

   filePath = %addr(vFullFile);  // Se asume que vFullFile ya tiene el path completo
   fd = IFS_OPEN(filePath: 577: 0: 0); // O_WRONLY+O_CREAT+O_TRUNC

   if fd >= 0;
      callp IFS_WRITE(fd: %addr(response): %len(%trim(response)));
      callp IFS_CLOSE(fd);
   endif;

end-proc;


dcl-pr IFS_OPEN int(10) extproc('_C_IFS_open');
  path pointer value;
  oflag int(10) value;
  mode int(10) value;
  ccsid int(10) value;
end-pr;

dcl-pr IFS_WRITE int(10) extproc('_C_IFS_write');
  fd int(10) value;
  buffer pointer value;
  length int(10) value;
end-pr;

dcl-pr IFS_CLOSE int(10) extproc('_C_IFS_close');
  fd int(10) value;
end-pr;

dcl-s vFullFile varchar(300);

GuardarResponseJson();
