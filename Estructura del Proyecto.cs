En una prueba que realice me genero dos archivos de logs.

    El primero es este que deberia ser donde se guarda, y por lo que veo si guardo algo sobre los sql:


---------------------------Inicio de Log-------------------------
Inicio: 2025-09-03 11:57:28
-------------------------------------------------------------------

---------------------------Enviroment Info-------------------------
Inicio: 2025-09-03 11:57:28
-------------------------------------------------------------------
Application: MS_BAN_43_Embosado_Tarjetas_Debito
Environment: Development
ContentRoot: C:\Git\MS_BAN_43_EmbosadoTarjetasDebito\BACKEND\MS_BAN_43_Embosado_Tarjetas_Debito
Execution ID: 0HNFB2CP86H3A:0000000B
Client IP: ::1
User Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36
Machine Name: HNCSTG015243WAP
OS: Microsoft Windows NT 10.0.20348.0
Host: localhost:7275
Distribución: N/A
  -- Extras del HttpContext --
    Scheme              : https
    Protocol            : HTTP/2
    Method              : POST
    Path                : /api/Auth/Login
    Query               : 
    ContentType         : application/json
    ContentLength       : 46
    ClientPort          : 58493
    LocalIp             : ::1
    LocalPort           : 7275
    ConnectionId        : 0HNFB2CP86H3A
    Referer             : https://localhost:7275/swagger/index.html
----------------------------------------------------------------------
---------------------------Enviroment Info-------------------------
Fin: 2025-09-03 11:57:28
-------------------------------------------------------------------

-------------------------------------------------------------------------------
Controlador: Auth
Action: Login
Inicio: 2025-09-03 11:57:28
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------

----------------------------------Request Info---------------------------------
Inicio: 2025-09-03 11:57:28
-------------------------------------------------------------------------------
Método: POST
URL: /api/Auth/Login
Cuerpo:

                              {
                                "user": "string",
                                "password": "string"
                              }
----------------------------------Request Info---------------------------------
Fin: 2025-09-03 11:57:28
-------------------------------------------------------------------------------

──────────────── SQL COMMAND ────────────────
SELECT * FROM BCAH96DTA.IETD01LOG ORDER BY LOGA01AID DESC
──────────────────────────────────────────────
──────────────── SQL COMMAND ────────────────
INSERT INTO BCAH96DTA.ETD01LOG (LOGA01AID, LOGA02UID, LOGA03TST,  LOGA04SUC,  LOGA05IPA,  LOGA06MNA,  LOGA07SID,  LOGA08FRE,  LOGA09ACO,  LOGA10UAG,  LOGA11BRO,  LOGA12SOP,  LOGA13DIS) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
──────────────────────────────────────────────
──────────────── SQL COMMAND ────────────────
SELECT * FROM BCAH96DTA.IETD02LOG WHERE LOGB01UID = ?
──────────────────────────────────────────────
──────────────── SQL COMMAND ────────────────
UPDATE BCAH96DTA.IETD02LOG SET LOGB02UIL = ?, LOGB03TIL = ?, LOGB04SEA = ?,  LOGB05UDI = ?, LOGB06UTD = ?, LOGB07UNA = ?, LOGB09UIF = ?, LOGB10TOK = ?  WHERE LOGB01UID = ?
──────────────────────────────────────────────
----------------------------------Response Info---------------------------------
Inicio: 2025-09-03 11:57:30
-------------------------------------------------------------------------------
Código Estado: 401
Cuerpo:

                              {
                                "token": {
                                  "token": "",
                                  "expiration": "0001-01-01T00:00:00"
                                },
                                "activeDirectoryData": {
                                  "agenciaAperturaCodigo": "",
                                  "agenciaImprimeCodigo": "",
                                  "nombreUsuario": "",
                                  "usuarioICBS": ""
                                },
                                "codigo": {
                                  "status": "Unauthorized",
                                  "message": "Credenciales Inválidas",
                                  "timeStamp": "11:57:30 AM",
                                  "error": "401"
                                }
                              }
----------------------------------Response Info---------------------------------
Fin: 2025-09-03 11:57:30
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------
Controlador: Auth
Action: Login
Fin: 2025-09-03 11:57:30
-------------------------------------------------------------------------------


[Tiempo Total de Ejecución]: 1765 ms
---------------------------Fin de Log-------------------------
Final: 2025-09-03 11:57:30
-------------------------------------------------------------------





    Y esto lo siguio guardando siempre en el archivo General donde no es correcto, hay que revisar porque una parte se guardo bien, y porque la otra no:

===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-03 11:57:30.116
Duración          : 34.2678 ms
Base de Datos     : Desconocida
IP                : Desconocida
Puerto            : 0
Esquema           : bcah96dta
Tabla             : bcah96dta.ietd02log
Veces Ejecutado   : 1
Filas Afectadas   : 1
SQL:
UPDATE BCAH96DTA.IETD02LOG SET LOGB02UIL = ?, LOGB03TIL = ?, LOGB04SEA = ?,  LOGB05UDI = ?, LOGB06UTD = ?, LOGB07UNA = ?, LOGB09UIF = ?, LOGB10TOK = ?  WHERE LOGB01UID = ?
================================
