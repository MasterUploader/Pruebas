CREATE TABLE ApiStatusLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,            -- Identificador único
    ApiName NVARCHAR(100) NOT NULL,              -- Nombre de la API
    Endpoint NVARCHAR(255) NOT NULL,             -- Endpoint específico
    HttpMethod NVARCHAR(10) NOT NULL,            -- Método HTTP (GET, POST, PUT, DELETE)
    HttpStatusCode INT NOT NULL,                 -- Código HTTP (200, 404, 500, etc.)
    StatusMessage NVARCHAR(255) NULL,            -- Mensaje opcional (OK, Error, Timeout, etc.)
    ResponseTimeMs INT NULL,                     -- Tiempo de respuesta en milisegundos
    ClientIp NVARCHAR(45) NULL,                  -- IP del cliente que consumió el endpoint
    UserAgent NVARCHAR(255) NULL,                -- Información del cliente/navegador/app
    CorrelationId UNIQUEIDENTIFIER NULL,         -- Identificador para rastreo de la petición
    LoggedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),  -- Fecha y hora del registro
    Environment NVARCHAR(50) NULL                -- DEV, QA, UAT, PROD
);
