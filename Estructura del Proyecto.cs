Me genero el log de la siguiente forma


──────────────── SQL COMMAND ────────────────
SELECT * FROM BCAH96DTA.IETD01LOG ORDER BY LOGA01AID DESC
──────────────────────────────────────────────
──────────────── SQL COMMAND ────────────────
INSERT INTO BCAH96DTA.ETD01LOG (LOGA01AID, LOGA02UID, LOGA03TST,  LOGA04SUC,  LOGA05IPA,  LOGA06MNA,  LOGA07SID,  LOGA08FRE,  LOGA09ACO,  LOGA10UAG,  LOGA11BRO,  LOGA12SOP,  LOGA13DIS) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
──────────────────────────────────────────────
===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-03 13:33:16.434
Duración          : 49.5058 ms
Base de Datos     : Desconocida
IP                : Desconocida
Puerto            : 0
Esquema           : bcah96dta
Tabla             : bcah96dta.etd01log
Veces Ejecutado   : 1
Filas Afectadas   : 1
SQL:
INSERT INTO BCAH96DTA.ETD01LOG (LOGA01AID, LOGA02UID, LOGA03TST,  LOGA04SUC,  LOGA05IPA,  LOGA06MNA,  LOGA07SID,  LOGA08FRE,  LOGA09ACO,  LOGA10UAG,  LOGA11BRO,  LOGA12SOP,  LOGA13DIS) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
================================

──────────────── SQL COMMAND ────────────────
SELECT * FROM BCAH96DTA.IETD02LOG WHERE LOGB01UID = ?
──────────────────────────────────────────────
──────────────── SQL COMMAND ────────────────
UPDATE BCAH96DTA.IETD02LOG SET LOGB02UIL = ?, LOGB03TIL = ?, LOGB04SEA = ?,  LOGB05UDI = ?, LOGB06UTD = ?, LOGB07UNA = ?, LOGB09UIF = ?, LOGB10TOK = ?  WHERE LOGB01UID = ?
──────────────────────────────────────────────
===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-03 13:33:16.539
Duración          : 34.5957 ms
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


Pero falta en algunos select el LOG DE EJECUCIÓN SQL, como los select no afectan filas al menos deberia de mostrarse así:

===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-03 13:33:16.539
Duración          : 34.5957 ms
Base de Datos     : Desconocida
IP                : Desconocida
Puerto            : 0
Esquema           : bcah96dta
Tabla             : bcah96dta.ietd02log
Veces Ejecutado   : 1
SQL:

SELECT * FROM BCAH96DTA.IETD01LOG ORDER BY LOGA01AID DESC
================================

Y se dan unos caso donde se duplican se ve así:

──────────────── SQL COMMAND ────────────────
UPDATE BCAH96DTA.IETD02LOG SET LOGB02UIL = ?, LOGB03TIL = ?, LOGB04SEA = ?,  LOGB05UDI = ?, LOGB06UTD = ?, LOGB07UNA = ?, LOGB09UIF = ?, LOGB10TOK = ?  WHERE LOGB01UID = ?
──────────────────────────────────────────────
===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-03 13:33:16.539
Duración          : 34.5957 ms
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


Y solo deberia ser así

===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-03 13:33:16.539
Duración          : 34.5957 ms
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
