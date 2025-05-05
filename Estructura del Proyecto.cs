ctl-opt dftactgrp(*no) actgrp(*caller) option(*srcstmt: *nodebugio);

// Prototipos necesarios
dcl-pr open int(10) extproc('open');
  path pointer value;
  oflag int(10) value;
  mode int(10) value options(*nopass);
end-pr;

dcl-pr write int(10) extproc('write');
  fd int(10) value;
  buf pointer value;
  count int(10) value;
end-pr;

dcl-pr close int(10) extproc('close');
  fd int(10) value;
end-pr;

dcl-pr fcntl int(10) extproc('fcntl');
  fd int(10) value;
  cmd int(10) value;
  arg int(10) value;
end-pr;

dcl-pr http_addHeader int(10) extproc('http_addHeader');
  name varchar(256) const;
  value varchar(2048) const;
end-pr;

dcl-pr http_post int(10) extproc('HTTP_POST');
  url varchar(1024) const;
  pRequest pointer value;
  requestLen int(10) value;
  responseFile varchar(1024) const;
  timeout int(10) value;
  userAgent varchar(256) const;
  headers pointer value options(*nopass);
end-pr;

// Constantes
dcl-c O_WRONLY 8;
dcl-c O_CREAT  256;
dcl-c O_TRUNC  512;
dcl-c F_SETCCSID 26;
dcl-c MODE 438; // rw-r--r--

// Variables
dcl-s json varchar(2048) ccsid(1208) inz('{ "mensaje": "José López & Compañía" }');
dcl-s filePath varchar(1024) inz('/tmp/request.json');
dcl-s fd int(10);
dcl-s rc int(10);
dcl-s headers pointer;
dcl-s responsePath varchar(1024) inz('/tmp/response.json');
dcl-s url varchar(1024) inz('https://miapi.com/endpoint');

// Abrir archivo con CCSID 1208
fd = open(%addr(filePath): O_WRONLY + O_CREAT + O_TRUNC: MODE);
if fd >= 0;
   rc = fcntl(fd: F_SETCCSID: 1208);
   rc = write(fd: %addr(json): %len(%trimr(json)));
   rc = close(fd);
endif;

// Agregar headers HTTP
http_addHeader('Content-Type': 'application/json; charset=UTF-8');
http_addHeader('Accept': 'application/json');

// Enviar la solicitud POST
rc = http_post(%trim(url)
             : %addr(json)
             : %len(%trimr(json))
             : %trim(responsePath)
             : 30
             : 'HTTPAPI-RPG'
             : headers);

// Validar respuesta
if rc < 0;
   dsply 'Error en HTTP_POST';
endif;
