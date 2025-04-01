// Prototipos para manejo IFS
dcl-pr IFS_OPEN int(10) extproc('_C_IFS_open');
  pathname pointer value;
  openFlags int(10) value;
  mode int(10) value;
end-pr;

dcl-pr IFS_WRITE int(10) extproc('_C_IFS_write');
  fd int(10) value;
  buffer pointer value;
  length int(10) value;
end-pr;

dcl-pr IFS_CLOSE int(10) extproc('_C_IFS_close');
  fd int(10) value;
end-pr;


dcl-c O_WRONLY 1;
dcl-c O_CREAT 8;
dcl-c O_TRUNC 512;



filePath = %addr(vFullFile);
fd = IFS_OPEN(filePath: O_WRONLY + O_CREAT + O_TRUNC: 438); // 438 = permisos 666 en decimal

if fd >= 0;
  rc = IFS_WRITE(fd: %addr(response): %len(%trim(response)));
  callp IFS_CLOSE(fd);
endif;
