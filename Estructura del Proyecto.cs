CREATE TABLE CASHRESP (
  ID_RESP        CHAR(36),           -- ID de la respuesta (UUID o secuencial)
  TIMESTAMP      CHAR(25),           -- Fecha/hora de la respuesta
  TRACE_ID       CHAR(50),           -- Identificador de trazabilidad
  STATUS         DEC(5, 0),          -- Código HTTP
  MESSAGE        CHAR(100),          -- Mensaje de éxito o error
  ERROR_MSG      CHAR(200),          -- Descripción de error (si aplica)

  -- Información general
  COUNTRY_ID     CHAR(3),            -- Código ISO del país

  -- Información de balance (si aplica)
  BALANCE_TYPE   CHAR(20),           -- Total, CashIn, CashOut
  DATE_BALANCE   CHAR(25),           -- Fecha/hora del balance
  DEVICE_CODE    CHAR(30),           -- Código del dispositivo
  BAL_CURRENCY   CHAR(3),            -- Código de divisa del balance
  BAL_AMOUNT     DEC(15, 2),         -- Monto total del balance

  -- Denominaciones (si aplica)
  DENOM_VALUE    DEC(9, 0),          -- Valor de la denominación
  DENOM_QUANTITY DEC(9, 0),          -- Cantidad de billetes/monedas
  DENOM_AMOUNT   DEC(15, 2),         -- Monto total por denominación
  DENOM_TYPE     CHAR(10),           -- NOTE o COIN

  -- Información de transacción (si aplica)
  TRANSACTION_ID CHAR(50),
  TRANS_DATE     CHAR(25),
  RECEIPT_NUMBER CHAR(20),
  TRAN_TYPE      CHAR(20),           -- CASHIN, CASHOUT, MOVEIN, etc.
  CASHIER_ID     CHAR(20),
  CASHIER_NAME   CHAR(50),
  TRANS_CURRENCY CHAR(3),
  TRANS_AMOUNT   DEC(15, 2),
  TRANS_SUBTYPE  CHAR(20),           -- DISPENSE, EXCHANGE, etc.

  -- Flags de origen (balance o transacción)
  IS_BALANCE     CHAR(1),            -- 'Y' o 'N'
  IS_TRANSACTION CHAR(1)             -- 'Y' o 'N'
);
