Ahora dime si es posible que el log se muestre así por ejemplo para el select:


===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-03 13:53:22.223
Duración          : 68.4817 ms
Base de Datos     : Desconocida
IP                : Desconocida
Puerto            : 0
Esquema           : bcah96dta
Tabla             : bcah96dta.ietd02log
Veces Ejecutado   : 1
Filas Afectadas   : 0
SQL:
SELECT * FROM BCAH96DTA.IETD02LOG WHERE LOGB01UID = "VALOR ENVIADO"
================================

Y por ejemplo para un update, el insert seria similar:

===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-03 13:53:22.257
Duración          : 28.0638 ms
Base de Datos     : Desconocida
IP                : Desconocida
Puerto            : 0
Esquema           : bcah96dta
Tabla             : bcah96dta.ietd02log
Veces Ejecutado   : 1
Filas Afectadas   : 1
SQL:
UPDATE BCAH96DTA.IETD02LOG SET LOGB02UIL = "VALOR ENVIADO", LOGB03TIL = "VALOR ENVIADO", LOGB04SEA = "VALOR ENVIADO",  LOGB05UDI = "VALOR ENVIADO", LOGB06UTD = "VALOR ENVIADO", LOGB07UNA = "VALOR ENVIADO", LOGB09UIF = "VALOR ENVIADO", LOGB10TOK = "VALOR ENVIADO"  WHERE LOGB01UID = "VALOR ENVIADO"
================================

Y si fuera para un insert multiple

===== LOG DE EJECUCIÓN SQL =====
Fecha y Hora      : 2025-09-03 13:53:22.257
Duración          : 28.0638 ms
Base de Datos     : Desconocida
IP                : Desconocida
Puerto            : 0
Esquema           : bcah96dta
Tabla             : bcah96dta.ietd02log
Veces Ejecutado   : 1
Filas Afectadas   : 2
SQL:
INSERT INTO nombre_de_la_tabla (columna1, columna2, ..., columnaN)
VALUES
    (valor1_fila1, valor2_fila1, ..., valorN_fila1),
    (valor1_fila2, valor2_fila2, ..., valorN_fila2),
    -- ... (más filas)
    (valor1_filaM, valor2_filaM, ..., valorN_filaM);

================================


Dime solo si es posible no cambies codigo aun

