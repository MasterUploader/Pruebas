-- Script demo: 100 inserts de prueba para HNDBAPISTATUS
-- Generado automáticamente

INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-001', 'UsuariosAPI', '/api/usuarios/listar', 'GET', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     479, '192.168.2.224', 'Mozilla/5.0', NEWID(), '2025-08-27 09:23:47', 'UAT', 'MOBILE', 'T037', 'ORG08', 'USR050', 'PROV09', 'SESS0388');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-002', 'UsuariosAPI', '/api/usuarios/login', 'GET', 500, 'Internal Server Error', 
     'SRV_ERR', 'Error interno del servidor de aplicaciones', 
     989, '192.168.4.78', 'AndroidApp/1.0', NEWID(), '2025-08-26 09:44:47', 'DEV', 'API', 'T038', 'ORG13', 'USR078', 'PROV07', 'SESS0319');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-003', 'AgenciasAPI', '/api/agencias/crear', 'POST', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1049, '192.168.4.33', 'Mozilla/5.0', NEWID(), '2025-08-21 06:24:47', 'DEV', 'WEB', 'T049', 'ORG15', 'USR059', 'PROV02', 'SESS0400');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-004', 'AgenciasAPI', '/api/agencias/listar', 'GET', 502, 'Bad Gateway', 
     'GATEWAY_ERR', 'Error en gateway o balanceador', 
     936, '192.168.0.233', 'PostmanRuntime', NEWID(), '2025-08-23 09:58:47', 'DEV', 'ATM', 'T024', 'ORG18', 'USR169', 'PROV04', 'SESS0083');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-005', 'AgenciasAPI', '/api/agencias/listar', 'GET', 200, 'OK', 
     NULL, NULL, 
     584, '192.168.5.178', 'Mozilla/5.0', NEWID(), '2025-08-22 14:28:47', 'QA', 'MOBILE', 'T034', 'ORG20', 'USR040', 'PROV08', 'SESS0166');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-006', 'PagosAPI', '/api/pagos/crear', 'GET', 201, 'Created', 
     NULL, NULL, 
     1093, '192.168.4.125', 'AndroidApp/1.0', NEWID(), '2025-08-20 17:05:47', 'UAT', 'MOBILE', 'T030', 'ORG13', 'USR045', 'PROV10', 'SESS0498');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-007', 'AgenciasAPI', '/api/agencias/crear', 'PUT', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     522, '192.168.0.45', 'iOSApp/2.3', NEWID(), '2025-08-26 22:46:47', 'DEV', 'MOBILE', 'T036', 'ORG13', 'USR187', 'PROV10', 'SESS0287');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-008', 'PagosAPI', '/api/pagos/crear', 'PUT', 502, 'Bad Gateway', 
     'GATEWAY_ERR', 'Error en gateway o balanceador', 
     1560, '192.168.4.199', 'curl/7.64.1', NEWID(), '2025-08-24 22:25:47', 'DEV', 'WEB', 'T025', 'ORG06', 'USR172', 'PROV09', 'SESS0478');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-009', 'ComerciosAPI', '/api/comercios/listar', 'DELETE', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     452, '192.168.3.199', 'curl/7.64.1', NEWID(), '2025-08-21 12:00:47', 'UAT', 'WEB', 'T012', 'ORG11', 'USR167', 'PROV04', 'SESS0365');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-010', 'UsuariosAPI', '/api/usuarios/login', 'PUT', 500, 'Internal Server Error', 
     'SRV_ERR', 'Error interno del servidor de aplicaciones', 
     516, '192.168.4.88', 'AndroidApp/1.0', NEWID(), '2025-08-24 17:26:47', 'QA', 'MOBILE', 'T049', 'ORG18', 'USR105', 'PROV04', 'SESS0157');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-011', 'ComerciosAPI', '/api/comercios/detalle', 'PUT', 404, 'Not Found', 
     'NOT_FOUND', 'Recurso solicitado no encontrado', 
     1556, '192.168.0.246', 'Mozilla/5.0', NEWID(), '2025-08-24 13:34:47', 'PROD', 'WEB', 'T002', 'ORG11', 'USR080', 'PROV05', 'SESS0302');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-012', 'UsuariosAPI', '/api/usuarios/crear', 'DELETE', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     344, '192.168.0.1', 'PostmanRuntime', NEWID(), '2025-08-23 01:10:47', 'PROD', 'API', 'T009', 'ORG14', 'USR016', 'PROV09', 'SESS0426');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-013', 'UsuariosAPI', '/api/usuarios/login', 'PUT', 200, 'OK', 
     NULL, NULL, 
     81, '192.168.2.121', 'curl/7.64.1', NEWID(), '2025-08-25 20:09:47', 'PROD', 'API', 'T033', 'ORG02', 'USR126', 'PROV08', 'SESS0471');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-014', 'AgenciasAPI', '/api/agencias/detalle', 'PUT', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     57, '192.168.4.187', 'iOSApp/2.3', NEWID(), '2025-08-22 09:49:47', 'UAT', 'API', 'T006', 'ORG10', 'USR149', 'PROV02', 'SESS0237');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-015', 'UsuariosAPI', '/api/usuarios/login', 'POST', 201, 'Created', 
     NULL, NULL, 
     597, '192.168.0.155', 'curl/7.64.1', NEWID(), '2025-08-24 23:20:47', 'DEV', 'MOBILE', 'T040', 'ORG02', 'USR143', 'PROV08', 'SESS0043');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-016', 'AgenciasAPI', '/api/agencias/listar', 'POST', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1536, '192.168.3.228', 'PostmanRuntime', NEWID(), '2025-08-23 13:23:47', 'QA', 'MOBILE', 'T047', 'ORG12', 'USR190', 'PROV09', 'SESS0236');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-017', 'UsuariosAPI', '/api/usuarios/listar', 'POST', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     231, '192.168.4.61', 'Mozilla/5.0', NEWID(), '2025-08-27 00:21:47', 'QA', 'ATM', 'T027', 'ORG18', 'USR014', 'PROV06', 'SESS0443');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-018', 'ReportesAPI', '/api/reportes/semanal', 'POST', 503, 'Service Unavailable', 
     'UNAVAILABLE', 'Servicio temporalmente no disponible', 
     1836, '192.168.0.51', 'AndroidApp/1.0', NEWID(), '2025-08-26 01:18:47', 'QA', 'ATM', 'T047', 'ORG05', 'USR080', 'PROV02', 'SESS0221');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-019', 'ComerciosAPI', '/api/comercios/listar', 'GET', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1681, '192.168.2.102', 'curl/7.64.1', NEWID(), '2025-08-25 06:36:47', 'PROD', 'API', 'T050', 'ORG04', 'USR140', 'PROV10', 'SESS0356');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-020', 'PagosAPI', '/api/pagos/cancelar', 'GET', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     1829, '192.168.2.230', 'curl/7.64.1', NEWID(), '2025-08-22 01:32:47', 'QA', 'WEB', 'T016', 'ORG19', 'USR133', 'PROV08', 'SESS0287');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-021', 'PagosAPI', '/api/pagos/cancelar', 'PUT', 404, 'Not Found', 
     'NOT_FOUND', 'Recurso solicitado no encontrado', 
     1832, '192.168.0.139', 'iOSApp/2.3', NEWID(), '2025-08-23 01:35:47', 'DEV', 'WEB', 'T049', 'ORG14', 'USR003', 'PROV05', 'SESS0497');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-022', 'ComerciosAPI', '/api/comercios/detalle', 'DELETE', 404, 'Not Found', 
     'NOT_FOUND', 'Recurso solicitado no encontrado', 
     1483, '192.168.3.250', 'Mozilla/5.0', NEWID(), '2025-08-27 11:37:47', 'DEV', 'ATM', 'T041', 'ORG13', 'USR004', 'PROV10', 'SESS0413');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-023', 'UsuariosAPI', '/api/usuarios/listar', 'DELETE', 503, 'Service Unavailable', 
     'UNAVAILABLE', 'Servicio temporalmente no disponible', 
     1481, '192.168.5.97', 'AndroidApp/1.0', NEWID(), '2025-08-23 14:15:47', 'DEV', 'WEB', 'T045', 'ORG14', 'USR066', 'PROV10', 'SESS0070');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-024', 'ComerciosAPI', '/api/comercios/crear', 'DELETE', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1601, '192.168.2.30', 'iOSApp/2.3', NEWID(), '2025-08-20 17:26:47', 'QA', 'WEB', 'T021', 'ORG14', 'USR068', 'PROV01', 'SESS0116');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-025', 'PagosAPI', '/api/pagos/crear', 'GET', 201, 'Created', 
     NULL, NULL, 
     1926, '192.168.0.56', 'iOSApp/2.3', NEWID(), '2025-08-26 22:27:47', 'PROD', 'ATM', 'T025', 'ORG09', 'USR005', 'PROV10', 'SESS0422');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-026', 'UsuariosAPI', '/api/usuarios/login', 'DELETE', 503, 'Service Unavailable', 
     'UNAVAILABLE', 'Servicio temporalmente no disponible', 
     478, '192.168.1.136', 'Mozilla/5.0', NEWID(), '2025-08-26 04:27:47', 'DEV', 'API', 'T019', 'ORG09', 'USR122', 'PROV02', 'SESS0022');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-027', 'ReportesAPI', '/api/reportes/diario', 'POST', 201, 'Created', 
     NULL, NULL, 
     1347, '192.168.5.22', 'AndroidApp/1.0', NEWID(), '2025-08-27 03:03:47', 'PROD', 'API', 'T001', 'ORG03', 'USR044', 'PROV06', 'SESS0067');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-028', 'ReportesAPI', '/api/reportes/mensual', 'PUT', 201, 'Created', 
     NULL, NULL, 
     409, '192.168.5.59', 'curl/7.64.1', NEWID(), '2025-08-24 00:57:47', 'UAT', 'ATM', 'T048', 'ORG14', 'USR049', 'PROV03', 'SESS0360');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-029', 'UsuariosAPI', '/api/usuarios/crear', 'PUT', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     1330, '192.168.3.81', 'Mozilla/5.0', NEWID(), '2025-08-23 04:09:47', 'PROD', 'MOBILE', 'T014', 'ORG10', 'USR149', 'PROV10', 'SESS0162');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-030', 'UsuariosAPI', '/api/usuarios/crear', 'POST', 502, 'Bad Gateway', 
     'GATEWAY_ERR', 'Error en gateway o balanceador', 
     256, '192.168.3.119', 'iOSApp/2.3', NEWID(), '2025-08-23 09:18:47', 'UAT', 'WEB', 'T017', 'ORG18', 'USR118', 'PROV06', 'SESS0290');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-031', 'AgenciasAPI', '/api/agencias/detalle', 'DELETE', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     1969, '192.168.5.36', 'AndroidApp/1.0', NEWID(), '2025-08-24 09:57:47', 'QA', 'ATM', 'T050', 'ORG09', 'USR172', 'PROV10', 'SESS0489');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-032', 'ReportesAPI', '/api/reportes/mensual', 'POST', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     427, '192.168.2.108', 'curl/7.64.1', NEWID(), '2025-08-24 09:56:47', 'PROD', 'MOBILE', 'T043', 'ORG15', 'USR106', 'PROV10', 'SESS0223');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-033', 'ComerciosAPI', '/api/comercios/detalle', 'PUT', 503, 'Service Unavailable', 
     'UNAVAILABLE', 'Servicio temporalmente no disponible', 
     311, '192.168.5.38', 'Mozilla/5.0', NEWID(), '2025-08-23 11:29:47', 'PROD', 'WEB', 'T044', 'ORG03', 'USR125', 'PROV01', 'SESS0088');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-034', 'ComerciosAPI', '/api/comercios/listar', 'GET', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     507, '192.168.1.30', 'Mozilla/5.0', NEWID(), '2025-08-27 14:21:47', 'DEV', 'ATM', 'T050', 'ORG20', 'USR067', 'PROV02', 'SESS0467');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-035', 'ReportesAPI', '/api/reportes/diario', 'DELETE', 201, 'Created', 
     NULL, NULL, 
     310, '192.168.5.176', 'PostmanRuntime', NEWID(), '2025-08-24 16:54:47', 'PROD', 'ATM', 'T003', 'ORG04', 'USR173', 'PROV07', 'SESS0051');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-036', 'ComerciosAPI', '/api/comercios/crear', 'POST', 503, 'Service Unavailable', 
     'UNAVAILABLE', 'Servicio temporalmente no disponible', 
     1582, '192.168.4.139', 'AndroidApp/1.0', NEWID(), '2025-08-27 00:57:47', 'DEV', 'MOBILE', 'T015', 'ORG02', 'USR028', 'PROV04', 'SESS0006');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-037', 'ComerciosAPI', '/api/comercios/detalle', 'DELETE', 502, 'Bad Gateway', 
     'GATEWAY_ERR', 'Error en gateway o balanceador', 
     1752, '192.168.0.202', 'PostmanRuntime', NEWID(), '2025-08-26 14:40:47', 'UAT', 'API', 'T038', 'ORG08', 'USR087', 'PROV02', 'SESS0459');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-038', 'PagosAPI', '/api/pagos/cancelar', 'POST', 502, 'Bad Gateway', 
     'GATEWAY_ERR', 'Error en gateway o balanceador', 
     504, '192.168.0.172', 'iOSApp/2.3', NEWID(), '2025-08-27 10:31:47', 'QA', 'MOBILE', 'T045', 'ORG20', 'USR130', 'PROV04', 'SESS0388');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-039', 'AgenciasAPI', '/api/agencias/detalle', 'PUT', 502, 'Bad Gateway', 
     'GATEWAY_ERR', 'Error en gateway o balanceador', 
     1015, '192.168.3.229', 'curl/7.64.1', NEWID(), '2025-08-20 19:46:47', 'QA', 'ATM', 'T023', 'ORG07', 'USR103', 'PROV05', 'SESS0335');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-040', 'ReportesAPI', '/api/reportes/diario', 'DELETE', 404, 'Not Found', 
     'NOT_FOUND', 'Recurso solicitado no encontrado', 
     481, '192.168.1.9', 'AndroidApp/1.0', NEWID(), '2025-08-23 20:08:47', 'UAT', 'ATM', 'T028', 'ORG12', 'USR006', 'PROV08', 'SESS0021');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-041', 'UsuariosAPI', '/api/usuarios/login', 'POST', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     1378, '192.168.2.243', 'curl/7.64.1', NEWID(), '2025-08-27 08:28:47', 'QA', 'WEB', 'T033', 'ORG12', 'USR185', 'PROV07', 'SESS0208');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-042', 'UsuariosAPI', '/api/usuarios/listar', 'POST', 503, 'Service Unavailable', 
     'UNAVAILABLE', 'Servicio temporalmente no disponible', 
     1242, '192.168.0.67', 'Mozilla/5.0', NEWID(), '2025-08-24 04:42:47', 'QA', 'API', 'T012', 'ORG20', 'USR182', 'PROV02', 'SESS0359');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-043', 'ReportesAPI', '/api/reportes/semanal', 'GET', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     461, '192.168.2.162', 'curl/7.64.1', NEWID(), '2025-08-22 18:11:47', 'UAT', 'API', 'T049', 'ORG18', 'USR177', 'PROV07', 'SESS0361');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-044', 'PagosAPI', '/api/pagos/consultar', 'DELETE', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     250, '192.168.1.181', 'AndroidApp/1.0', NEWID(), '2025-08-21 11:57:47', 'QA', 'WEB', 'T002', 'ORG09', 'USR143', 'PROV08', 'SESS0235');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-045', 'PagosAPI', '/api/pagos/cancelar', 'GET', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     545, '192.168.2.232', 'curl/7.64.1', NEWID(), '2025-08-22 07:39:47', 'UAT', 'API', 'T033', 'ORG15', 'USR069', 'PROV07', 'SESS0238');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-046', 'ReportesAPI', '/api/reportes/diario', 'GET', 200, 'OK', 
     NULL, NULL, 
     496, '192.168.2.113', 'Mozilla/5.0', NEWID(), '2025-08-25 21:22:47', 'PROD', 'MOBILE', 'T041', 'ORG10', 'USR118', 'PROV02', 'SESS0207');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-047', 'AgenciasAPI', '/api/agencias/detalle', 'DELETE', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1392, '192.168.4.222', 'curl/7.64.1', NEWID(), '2025-08-27 05:52:47', 'UAT', 'WEB', 'T044', 'ORG01', 'USR182', 'PROV10', 'SESS0202');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-048', 'UsuariosAPI', '/api/usuarios/login', 'GET', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     351, '192.168.0.237', 'Mozilla/5.0', NEWID(), '2025-08-21 14:03:47', 'QA', 'WEB', 'T041', 'ORG03', 'USR172', 'PROV01', 'SESS0366');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-049', 'PagosAPI', '/api/pagos/cancelar', 'DELETE', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1271, '192.168.0.113', 'Mozilla/5.0', NEWID(), '2025-08-23 20:11:47', 'QA', 'MOBILE', 'T050', 'ORG10', 'USR075', 'PROV06', 'SESS0222');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-050', 'ReportesAPI', '/api/reportes/mensual', 'PUT', 502, 'Bad Gateway', 
     'GATEWAY_ERR', 'Error en gateway o balanceador', 
     592, '192.168.0.163', 'PostmanRuntime', NEWID(), '2025-08-26 10:42:47', 'DEV', 'MOBILE', 'T027', 'ORG03', 'USR074', 'PROV01', 'SESS0386');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-051', 'ComerciosAPI', '/api/comercios/listar', 'GET', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1631, '192.168.3.173', 'iOSApp/2.3', NEWID(), '2025-08-25 14:15:47', 'UAT', 'API', 'T003', 'ORG08', 'USR151', 'PROV05', 'SESS0246');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-052', 'AgenciasAPI', '/api/agencias/listar', 'POST', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     1519, '192.168.4.159', 'Mozilla/5.0', NEWID(), '2025-08-26 11:34:47', 'PROD', 'ATM', 'T014', 'ORG14', 'USR182', 'PROV06', 'SESS0357');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-053', 'UsuariosAPI', '/api/usuarios/crear', 'GET', 200, 'OK', 
     NULL, NULL, 
     746, '192.168.0.36', 'AndroidApp/1.0', NEWID(), '2025-08-25 17:34:47', 'UAT', 'ATM', 'T024', 'ORG16', 'USR060', 'PROV09', 'SESS0194');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-054', 'ComerciosAPI', '/api/comercios/detalle', 'POST', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     1126, '192.168.0.167', 'iOSApp/2.3', NEWID(), '2025-08-21 15:09:47', 'QA', 'MOBILE', 'T039', 'ORG20', 'USR012', 'PROV09', 'SESS0232');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-055', 'ComerciosAPI', '/api/comercios/listar', 'POST', 404, 'Not Found', 
     'NOT_FOUND', 'Recurso solicitado no encontrado', 
     588, '192.168.5.148', 'Mozilla/5.0', NEWID(), '2025-08-22 14:25:47', 'PROD', 'API', 'T043', 'ORG18', 'USR196', 'PROV02', 'SESS0091');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-056', 'PagosAPI', '/api/pagos/cancelar', 'DELETE', 200, 'OK', 
     NULL, NULL, 
     825, '192.168.3.48', 'Mozilla/5.0', NEWID(), '2025-08-23 03:15:47', 'PROD', 'MOBILE', 'T022', 'ORG16', 'USR043', 'PROV04', 'SESS0404');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-057', 'ReportesAPI', '/api/reportes/diario', 'GET', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     1613, '192.168.0.44', 'iOSApp/2.3', NEWID(), '2025-08-24 15:48:47', 'DEV', 'WEB', 'T047', 'ORG02', 'USR153', 'PROV05', 'SESS0376');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-058', 'ComerciosAPI', '/api/comercios/detalle', 'GET', 201, 'Created', 
     NULL, NULL, 
     1275, '192.168.1.75', 'PostmanRuntime', NEWID(), '2025-08-27 07:54:47', 'DEV', 'ATM', 'T006', 'ORG07', 'USR172', 'PROV05', 'SESS0379');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-059', 'AgenciasAPI', '/api/agencias/detalle', 'DELETE', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     444, '192.168.3.166', 'PostmanRuntime', NEWID(), '2025-08-24 01:30:47', 'UAT', 'MOBILE', 'T041', 'ORG12', 'USR033', 'PROV08', 'SESS0102');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-060', 'PagosAPI', '/api/pagos/crear', 'POST', 503, 'Service Unavailable', 
     'UNAVAILABLE', 'Servicio temporalmente no disponible', 
     1539, '192.168.4.102', 'iOSApp/2.3', NEWID(), '2025-08-26 01:37:47', 'QA', 'API', 'T014', 'ORG10', 'USR173', 'PROV01', 'SESS0205');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-061', 'AgenciasAPI', '/api/agencias/listar', 'PUT', 201, 'Created', 
     NULL, NULL, 
     1095, '192.168.4.122', 'curl/7.64.1', NEWID(), '2025-08-22 03:35:47', 'QA', 'ATM', 'T041', 'ORG16', 'USR107', 'PROV08', 'SESS0042');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-062', 'PagosAPI', '/api/pagos/consultar', 'DELETE', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     189, '192.168.0.177', 'AndroidApp/1.0', NEWID(), '2025-08-25 20:45:47', 'DEV', 'MOBILE', 'T027', 'ORG09', 'USR188', 'PROV10', 'SESS0408');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-063', 'PagosAPI', '/api/pagos/consultar', 'GET', 201, 'Created', 
     NULL, NULL, 
     756, '192.168.4.13', 'Mozilla/5.0', NEWID(), '2025-08-23 01:07:47', 'UAT', 'ATM', 'T004', 'ORG13', 'USR124', 'PROV09', 'SESS0207');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-064', 'PagosAPI', '/api/pagos/cancelar', 'PUT', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     82, '192.168.3.90', 'Mozilla/5.0', NEWID(), '2025-08-27 03:50:47', 'DEV', 'MOBILE', 'T049', 'ORG09', 'USR070', 'PROV04', 'SESS0249');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-065', 'ComerciosAPI', '/api/comercios/listar', 'GET', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1124, '192.168.3.36', 'iOSApp/2.3', NEWID(), '2025-08-23 10:58:47', 'QA', 'WEB', 'T023', 'ORG09', 'USR196', 'PROV04', 'SESS0076');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-066', 'PagosAPI', '/api/pagos/consultar', 'GET', 404, 'Not Found', 
     'NOT_FOUND', 'Recurso solicitado no encontrado', 
     1105, '192.168.0.110', 'iOSApp/2.3', NEWID(), '2025-08-26 18:19:47', 'PROD', 'ATM', 'T022', 'ORG18', 'USR163', 'PROV07', 'SESS0021');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-067', 'ReportesAPI', '/api/reportes/semanal', 'PUT', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     812, '192.168.3.59', 'Mozilla/5.0', NEWID(), '2025-08-26 09:47:47', 'QA', 'API', 'T002', 'ORG05', 'USR094', 'PROV10', 'SESS0370');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-068', 'UsuariosAPI', '/api/usuarios/login', 'GET', 502, 'Bad Gateway', 
     'GATEWAY_ERR', 'Error en gateway o balanceador', 
     1480, '192.168.5.114', 'iOSApp/2.3', NEWID(), '2025-08-22 14:59:47', 'UAT', 'MOBILE', 'T020', 'ORG11', 'USR060', 'PROV02', 'SESS0399');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-069', 'UsuariosAPI', '/api/usuarios/crear', 'DELETE', 502, 'Bad Gateway', 
     'GATEWAY_ERR', 'Error en gateway o balanceador', 
     1763, '192.168.2.151', 'iOSApp/2.3', NEWID(), '2025-08-21 00:57:47', 'QA', 'MOBILE', 'T004', 'ORG15', 'USR101', 'PROV07', 'SESS0297');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-070', 'ComerciosAPI', '/api/comercios/crear', 'POST', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     435, '192.168.0.162', 'PostmanRuntime', NEWID(), '2025-08-22 19:01:47', 'QA', 'API', 'T048', 'ORG02', 'USR118', 'PROV03', 'SESS0116');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-071', 'ComerciosAPI', '/api/comercios/crear', 'PUT', 201, 'Created', 
     NULL, NULL, 
     279, '192.168.0.24', 'curl/7.64.1', NEWID(), '2025-08-25 15:21:47', 'UAT', 'API', 'T047', 'ORG06', 'USR106', 'PROV02', 'SESS0236');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-072', 'ComerciosAPI', '/api/comercios/detalle', 'POST', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     1307, '192.168.3.224', 'curl/7.64.1', NEWID(), '2025-08-24 12:58:47', 'DEV', 'WEB', 'T009', 'ORG19', 'USR012', 'PROV04', 'SESS0053');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-073', 'AgenciasAPI', '/api/agencias/crear', 'GET', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1840, '192.168.1.16', 'AndroidApp/1.0', NEWID(), '2025-08-23 02:49:47', 'UAT', 'API', 'T031', 'ORG20', 'USR103', 'PROV01', 'SESS0419');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-074', 'ReportesAPI', '/api/reportes/diario', 'PUT', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1854, '192.168.3.230', 'AndroidApp/1.0', NEWID(), '2025-08-26 06:50:47', 'QA', 'MOBILE', 'T038', 'ORG14', 'USR038', 'PROV10', 'SESS0124');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-075', 'ReportesAPI', '/api/reportes/diario', 'PUT', 201, 'Created', 
     NULL, NULL, 
     401, '192.168.1.31', 'iOSApp/2.3', NEWID(), '2025-08-25 15:10:47', 'QA', 'MOBILE', 'T020', 'ORG06', 'USR076', 'PROV02', 'SESS0065');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-076', 'ReportesAPI', '/api/reportes/diario', 'PUT', 200, 'OK', 
     NULL, NULL, 
     1466, '192.168.2.127', 'curl/7.64.1', NEWID(), '2025-08-25 06:44:47', 'UAT', 'ATM', 'T011', 'ORG12', 'USR062', 'PROV05', 'SESS0098');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-077', 'PagosAPI', '/api/pagos/cancelar', 'DELETE', 200, 'OK', 
     NULL, NULL, 
     1167, '192.168.2.218', 'curl/7.64.1', NEWID(), '2025-08-24 16:02:47', 'PROD', 'WEB', 'T017', 'ORG12', 'USR158', 'PROV04', 'SESS0073');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-078', 'ReportesAPI', '/api/reportes/diario', 'GET', 500, 'Internal Server Error', 
     'SRV_ERR', 'Error interno del servidor de aplicaciones', 
     592, '192.168.3.15', 'iOSApp/2.3', NEWID(), '2025-08-25 17:42:47', 'UAT', 'ATM', 'T032', 'ORG03', 'USR001', 'PROV03', 'SESS0158');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-079', 'ComerciosAPI', '/api/comercios/crear', 'PUT', 200, 'OK', 
     NULL, NULL, 
     1652, '192.168.1.158', 'AndroidApp/1.0', NEWID(), '2025-08-23 04:18:47', 'UAT', 'MOBILE', 'T039', 'ORG13', 'USR132', 'PROV08', 'SESS0474');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-080', 'PagosAPI', '/api/pagos/cancelar', 'PUT', 500, 'Internal Server Error', 
     'SRV_ERR', 'Error interno del servidor de aplicaciones', 
     1246, '192.168.3.179', 'Mozilla/5.0', NEWID(), '2025-08-27 08:29:47', 'QA', 'ATM', 'T046', 'ORG10', 'USR198', 'PROV01', 'SESS0401');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-081', 'ComerciosAPI', '/api/comercios/detalle', 'PUT', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     1384, '192.168.5.32', 'PostmanRuntime', NEWID(), '2025-08-27 07:36:47', 'QA', 'ATM', 'T010', 'ORG01', 'USR085', 'PROV06', 'SESS0033');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-082', 'ReportesAPI', '/api/reportes/diario', 'PUT', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     546, '192.168.4.210', 'PostmanRuntime', NEWID(), '2025-08-26 11:46:47', 'QA', 'MOBILE', 'T016', 'ORG04', 'USR106', 'PROV05', 'SESS0484');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-083', 'ComerciosAPI', '/api/comercios/listar', 'POST', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     691, '192.168.2.35', 'curl/7.64.1', NEWID(), '2025-08-27 02:09:47', 'UAT', 'MOBILE', 'T008', 'ORG09', 'USR096', 'PROV08', 'SESS0269');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-084', 'PagosAPI', '/api/pagos/consultar', 'PUT', 503, 'Service Unavailable', 
     'UNAVAILABLE', 'Servicio temporalmente no disponible', 
     163, '192.168.1.160', 'PostmanRuntime', NEWID(), '2025-08-23 23:55:47', 'UAT', 'ATM', 'T050', 'ORG09', 'USR115', 'PROV02', 'SESS0112');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-085', 'ReportesAPI', '/api/reportes/mensual', 'DELETE', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     1904, '192.168.0.71', 'iOSApp/2.3', NEWID(), '2025-08-27 10:51:47', 'PROD', 'WEB', 'T050', 'ORG09', 'USR084', 'PROV08', 'SESS0361');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-086', 'PagosAPI', '/api/pagos/cancelar', 'DELETE', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     427, '192.168.4.221', 'PostmanRuntime', NEWID(), '2025-08-23 19:34:47', 'UAT', 'MOBILE', 'T028', 'ORG12', 'USR084', 'PROV02', 'SESS0470');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-087', 'ComerciosAPI', '/api/comercios/crear', 'DELETE', 500, 'Internal Server Error', 
     'SRV_ERR', 'Error interno del servidor de aplicaciones', 
     1449, '192.168.1.174', 'iOSApp/2.3', NEWID(), '2025-08-20 20:16:47', 'DEV', 'MOBILE', 'T033', 'ORG03', 'USR020', 'PROV02', 'SESS0293');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-088', 'ComerciosAPI', '/api/comercios/listar', 'POST', 403, 'Forbidden', 
     'FORBIDDEN', 'Acceso denegado al recurso solicitado', 
     510, '192.168.2.201', 'AndroidApp/1.0', NEWID(), '2025-08-23 08:24:47', 'QA', 'MOBILE', 'T010', 'ORG07', 'USR112', 'PROV07', 'SESS0140');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-089', 'AgenciasAPI', '/api/agencias/crear', 'POST', 200, 'OK', 
     NULL, NULL, 
     1067, '192.168.1.176', 'PostmanRuntime', NEWID(), '2025-08-26 17:01:47', 'UAT', 'ATM', 'T017', 'ORG19', 'USR032', 'PROV10', 'SESS0025');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-090', 'ReportesAPI', '/api/reportes/semanal', 'GET', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     1977, '192.168.4.47', 'Mozilla/5.0', NEWID(), '2025-08-25 07:48:47', 'QA', 'ATM', 'T027', 'ORG14', 'USR130', 'PROV08', 'SESS0273');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-091', 'UsuariosAPI', '/api/usuarios/crear', 'GET', 200, 'OK', 
     NULL, NULL, 
     215, '192.168.5.71', 'Mozilla/5.0', NEWID(), '2025-08-21 23:19:47', 'DEV', 'API', 'T046', 'ORG14', 'USR195', 'PROV06', 'SESS0184');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-092', 'UsuariosAPI', '/api/usuarios/crear', 'GET', 500, 'Internal Server Error', 
     'SRV_ERR', 'Error interno del servidor de aplicaciones', 
     1524, '192.168.1.166', 'iOSApp/2.3', NEWID(), '2025-08-22 02:52:47', 'UAT', 'ATM', 'T038', 'ORG04', 'USR021', 'PROV02', 'SESS0197');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-093', 'ReportesAPI', '/api/reportes/semanal', 'GET', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     1485, '192.168.5.10', 'curl/7.64.1', NEWID(), '2025-08-25 19:50:47', 'PROD', 'API', 'T032', 'ORG14', 'USR085', 'PROV05', 'SESS0030');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-094', 'ComerciosAPI', '/api/comercios/crear', 'POST', 400, 'Bad Request', 
     'VAL_ERR', 'Error de validación en los datos enviados', 
     768, '192.168.2.65', 'curl/7.64.1', NEWID(), '2025-08-25 08:53:47', 'PROD', 'API', 'T034', 'ORG13', 'USR197', 'PROV04', 'SESS0168');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-095', 'ComerciosAPI', '/api/comercios/crear', 'POST', 404, 'Not Found', 
     'NOT_FOUND', 'Recurso solicitado no encontrado', 
     1078, '192.168.1.241', 'iOSApp/2.3', NEWID(), '2025-08-23 03:31:47', 'PROD', 'MOBILE', 'T046', 'ORG01', 'USR162', 'PROV06', 'SESS0453');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-096', 'AgenciasAPI', '/api/agencias/listar', 'PUT', 201, 'Created', 
     NULL, NULL, 
     873, '192.168.3.45', 'AndroidApp/1.0', NEWID(), '2025-08-21 15:37:47', 'PROD', 'MOBILE', 'T006', 'ORG07', 'USR143', 'PROV02', 'SESS0401');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-097', 'ReportesAPI', '/api/reportes/diario', 'GET', 200, 'OK', 
     NULL, NULL, 
     194, '192.168.1.61', 'PostmanRuntime', NEWID(), '2025-08-26 07:46:47', 'UAT', 'WEB', 'T042', 'ORG14', 'USR177', 'PROV07', 'SESS0169');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-098', 'UsuariosAPI', '/api/usuarios/login', 'POST', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     1583, '192.168.0.39', 'iOSApp/2.3', NEWID(), '2025-08-24 22:56:47', 'PROD', 'API', 'T038', 'ORG02', 'USR154', 'PROV07', 'SESS0150');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-099', 'PagosAPI', '/api/pagos/cancelar', 'POST', 200, 'OK', 
     NULL, NULL, 
     1086, '192.168.0.52', 'PostmanRuntime', NEWID(), '2025-08-20 23:38:47', 'PROD', 'API', 'T027', 'ORG15', 'USR067', 'PROV03', 'SESS0304');
INSERT INTO HNDBAPISTATUS 
    (ResponseId, ApiName, Endpoint, HttpMethod, HttpStatusCode, StatusMessage, 
     BusinessError, BusinessErrorDescription, ResponseTimeMs, ClientIp, UserAgent, 
     CorrelationId, LoggedAt, Environment, Channel, Terminal, Organization, UserId, Provider, SessionId)
VALUES 
    ('RESP-100', 'AgenciasAPI', '/api/agencias/detalle', 'POST', 401, 'Unauthorized', 
     'AUTH_ERR', 'Credenciales inválidas o token expirado', 
     1062, '192.168.4.74', 'Mozilla/5.0', NEWID(), '2025-08-21 23:03:47', 'QA', 'MOBILE', 'T004', 'ORG10', 'USR049', 'PROV09', 'SESS0209');