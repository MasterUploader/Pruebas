CREATE TABLE CASHRESP (
  ID_RESP          CHAR(36),        -- ID global de la respuesta
  TIMESTAMP        CHAR(25),        -- Fecha/hora de la respuesta
  TRACE_ID         CHAR(50),        -- Identificador único de la llamada
  STATUS           DEC(5, 0),       -- Código HTTP
  MESSAGE          VARCHAR(200),    -- Resultado: "SUCCESS", "ERROR", etc.
  ERROR_MSG        VARCHAR(200),    -- ErrorMessage (si aplica)
  COUNTRY_ID       CHAR(3),         -- Código ISO del país

  -- BALANCES
  SEQ_BALANCE      SMALLINT,        -- N° de balance dentro del arreglo
  BALANCE_TYPE     CHAR(20),        -- Total, CashIn, CashOut
  DATE_BALANCE     CHAR(25),        -- Fecha/hora del balance
  DEVICE_CODE_BAL  CHAR(30),        -- Código del dispositivo (balance)

  -- TRANSACTIONS
  SEQ_TRANSACTION  SMALLINT,        -- N° de transacción dentro del arreglo
  TRANSACTION_ID   CHAR(50),
  TRANS_DATE       CHAR(25),
  RECEIPT_NUMBER   CHAR(20),
  TRAN_TYPE        CHAR(20),        -- CASHIN, CASHOUT, etc.
  TRANS_SUBTYPE    CHAR(20),        -- DISPENSE, EXCHANGE, etc.
  CASHIER_ID       CHAR(20),
  CASHIER_NAME     CHAR(50),
  DEVICE_CODE_TRAN CHAR(30),        -- Código del dispositivo (transacción)

  -- CURRENCY (balance o transacción)
  SEQ_CURRENCY     SMALLINT,        -- N° dentro del array de Currency[]
  CURRENCY_CODE    CHAR(3),
  CURRENCY_AMOUNT  DEC(15, 2),

  -- DENOMINATIONS
  SEQ_DENOM        SMALLINT,        -- N° dentro del array de Denomination[]
  DENOM_VALUE      DEC(9, 0),
  DENOM_QUANTITY   DEC(9, 0),
  DENOM_AMOUNT     DEC(15, 2),
  DENOM_TYPE       CHAR(10),        -- NOTE, COIN

  -- FLAGS
  IS_BALANCE       CHAR(1),         -- 'Y' o 'N'
  IS_TRANSACTION   CHAR(1)          -- 'Y' o 'N'
);
