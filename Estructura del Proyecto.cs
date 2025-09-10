using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.Auth;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.MachineInformationService;
using QueryBuilder.Core; // Punto de entrada del builder (ajústalo si tu namespace difiere)
using System.Data.Common;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.Auth;

/// <summary>
/// Repositorio para registrar eventos de autenticación (log general y log por usuario),
/// optimizado para minimizar viajes a la BD:
/// - El correlativo se resuelve en el mismo INSERT vía SELECT (MAX + 1).
/// - El log por usuario se resuelve con MERGE (upsert) en una sola instrucción.
/// </summary>
/// <param name="_machineInfoService">Servicio para obtener huella de máquina/cliente.</param>
/// <param name="_connection">Proveedor de conexión abstracto hacia AS400 (DB2 for i).</param>
/// <param name="_contextAccessor">Acceso a <see cref="HttpContext"/> para trazabilidad/logging.</param>
public class AuthServiceRepository(IMachineInfoService _machineInfoService, IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor)
{
    /// <summary>
    /// Registra los logs de autenticación en dos pasos minimizados:
    /// 1) <b>INSERT ... SELECT</b> a <c>BCAH96DTA.ETD01LOG</c> calculando el correlativo como <c>COALESCE(MAX(LOGA01AID),0)+1</c>.
    /// 2) <b>MERGE</b> sobre <c>BCAH96DTA.ETD02LOG</c> para hacer UPSERT del “log personal” del usuario.
    /// </summary>
    /// <param name="_getAuthResponseDto">DTO de respuesta a completar.</param>
    /// <param name="userID">Identificador de usuario.</param>
    /// <param name="motivo">Motivo del registro.</param>
    /// <param name="exitoso">“1” si la autenticación fue exitosa; “0” si falló.</param>
    /// <param name="idSesion">Identificador de sesión/token.</param>
    /// <param name="success">Etiqueta de estado (“success”, “Unauthorized”, etc.).</param>
    /// <returns>DTO con el resultado del proceso.</returns>
    public GetAuthResponseDto RegistraLogsUsuario(GetAuthResponseDto _getAuthResponseDto, string userID, string motivo, string exitoso, string idSesion, string success)
    {
        try
        {
            // -- Sello de tiempo local para DB2 (se usa en ambas operaciones).
            var now = DateTime.Now;

            // -- Huella de máquina/cliente para trazabilidad (IP, device, browser, etc.).
            var machine = _machineInfoService.GetMachineInfo();

            // =========================================================================
            // 1) INSERT ... SELECT (QueryBuilder) → BCAH96DTA.ETD01LOG
            //    - Resuelve correlativo en el mismo INSERT (sin “preconsulta”).
            //    - Deja el contador de intentos en 0 (LOGB09ACO en tu diseño original).
            // =========================================================================

            // SELECT que produce la fila a insertar (con columnas en el mismo orden del INSERT)
            //  [LOGA01AID, LOGA02UID, LOGA03TST, LOGA04SUC, LOGA05IPA, LOGA06MNA, LOGA07SID,
            //   LOGA08FRE, LOGA09ACO, LOGA10UAG, LOGA11BRO, LOGA12SOP, LOGA13DIS]
            //  MAX + 1 se calcula contra la misma tabla destino.
            var selectInsert = QueryBuilder
                .From("ETD01LOG", "BCAH96DTA").As("T")
                .SelectRaw("COALESCE(MAX(T.LOGA01AID), 0) + 1")      // Correlativo calculado
                .SelectRaw($"'{userID.Replace("'", "''")}'")          // LOGA02UID
                .SelectRaw($"TIMESTAMP('{now:yyyy-MM-dd-HH.mm.ss}')" )// LOGA03TST (formato timestamp DB2)
                .SelectRaw($"'{exitoso.Replace("'", "''")}'")          // LOGA04SUC
                .SelectRaw($"'{machine.ClientIPAddress?.Replace("'", "''") ?? ""}'") // LOGA05IPA
                .SelectRaw($"'{machine.HostName?.Replace("'", "''") ?? ""}'")        // LOGA06MNA
                .SelectRaw($"'{idSesion.Replace("'", "''")}'")         // LOGA07SID
                .SelectRaw($"'{motivo.Replace("'", "''")}'")           // LOGA08FRE
                .SelectRaw("0")                                        // LOGA09ACO (Conteo Intentos para log general)
                .SelectRaw($"'{machine.UserAgent?.Replace("'", "''") ?? ""}'") // LOGA10UAG
                .SelectRaw($"'{machine.Browser?.Replace("'", "''") ?? ""}'")   // LOGA11BRO
                .SelectRaw($"'{machine.OS?.Replace("'", "''") ?? ""}'")        // LOGA12SOP
                .SelectRaw($"'{machine.Device?.Replace("'", "''") ?? ""}'")    // LOGA13DIS
                .Build();

            // Construcción del INSERT con FromSelect (todas las columnas explícitas)
            var insertLogGeneral = new QueryBuilder.Builders.InsertQueryBuilder(typeof(void))
                .Into("ETD01LOG", "BCAH96DTA")
                .IntoColumns(new[]
                {
                    "LOGA01AID","LOGA02UID","LOGA03TST","LOGA04SUC","LOGA05IPA","LOGA06MNA","LOGA07SID",
                    "LOGA08FRE","LOGA09ACO","LOGA10UAG","LOGA11BRO","LOGA12SOP","LOGA13DIS"
                })
                .FromSelect(selectInsert.Sql) // Nota: si tu InsertQueryBuilder acepta SelectQueryBuilder directamente, pásalo sin .Sql
                .WithComment("INSERT general de login con correlativo MAX+1 en una sola operación")
                .Build();

            using var cmd1 = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd1.CommandText = insertLogGeneral.Sql;
            cmd1.CommandType = System.Data.CommandType.Text;
            var aff1 = cmd1.ExecuteNonQuery(); // Debe ser 1 si insertó ok

            // =========================================================================
            // 2) MERGE (UPSERT) → BCAH96DTA.ETD02LOG
            //
            //    Lógica de “intentos”:
            //       - Si exitoso = '1' → intentos = intentos(previos) + 1
            //       - Si exitoso = '0' → intentos = 0
            //
            //    Campos:
            //     - LOGB02UIL (último login)      ← now
            //     - LOGB03TIL (intentos)          ← CASE basado en exitoso
            //     - LOGB04SEA (sesión activa)     ← exitoso
            //     - LOGB05UDI (IP)                ← machine.ClientIPAddress
            //     - LOGB06UTD (Device)            ← machine.Device
            //     - LOGB07UNA (Browser)           ← machine.Browser
            //     - LOGB08CBI (Bloqueo intento)   ← '' (vacío en tu inserción)
            //     - LOGB09UIF (último intento)    ← COALESCE(previo, now)  (si no hay previo, cae en now)
            //     - LOGB10TOK (token/sesión)      ← idSesion
            //
            //    Nota: Usamos VALUES(...) como tabla fuente “S” para MERGE.
            // =========================================================================

            // Escapes simples para literales:
            string esc(string? s) => (s ?? "").Replace("'", "''");

            var mergeUpsert = $@"
MERGE INTO BCAH96DTA.ETD02LOG AS T
USING (VALUES(
    '{esc(userID)}',
    TIMESTAMP('{now:yyyy-MM-dd-HH.mm.ss}'),
    '{esc(exitoso)}',
    '{esc(machine.ClientIPAddress)}',
    '{esc(machine.Device)}',
    '{esc(machine.Browser)}',
    '{esc(idSesion)}'
)) AS S(UID, NOWTS, EXI, IP, DEV, BRO, TOK)
ON T.LOGB01UID = S.UID

WHEN MATCHED THEN UPDATE SET
    T.LOGB02UIL = S.NOWTS,
    T.LOGB03TIL = CASE WHEN S.EXI = '1' THEN COALESCE(T.LOGB03TIL, 0) + 1 ELSE 0 END,
    T.LOGB04SEA = S.EXI,
    T.LOGB05UDI = S.IP,
    T.LOGB06UTD = S.DEV,
    T.LOGB07UNA = S.BRO,
    T.LOGB09UIF = COALESCE(T.LOGB02UIL, S.NOWTS),
    T.LOGB10TOK = S.TOK

WHEN NOT MATCHED THEN INSERT
    (LOGB01UID, LOGB02UIL, LOGB03TIL, LOGB04SEA, LOGB05UDI, LOGB06UTD, LOGB07UNA, LOGB08CBI, LOGB09UIF, LOGB10TOK)
VALUES
    (S.UID, S.NOWTS, CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END, S.EXI, S.IP, S.DEV, S.BRO, '', S.NOWTS, S.TOK);
";

            using var cmd2 = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd2.CommandText = mergeUpsert;
            cmd2.CommandType = System.Data.CommandType.Text;
            var aff2 = cmd2.ExecuteNonQuery(); // ≥1 si hizo UPDATE o INSERT

            // =========================================================================
            // Respuesta homogénea
            // =========================================================================
            string statusCode = success switch
            {
                "success" => "200",
                "Unauthorized" => "401",
                _ => "400"
            };

            if (aff1 > 0 && aff2 >= 0)
            {
                _getAuthResponseDto.Codigo.Message = motivo;
                _getAuthResponseDto.Codigo.Error = statusCode;
                _getAuthResponseDto.Codigo.Status = success;
                _getAuthResponseDto.Codigo.TimeStamp = $"{DateTime.Now:HH:mm:ss tt}";
                return _getAuthResponseDto;
            }

            var fail = new GetAuthResponseDto();
            fail.Codigo.Message = "No se pudo guardar datos de log";
            fail.Codigo.Error = "400";
            fail.Codigo.Status = "BadRequest";
            fail.Codigo.TimeStamp = $"{DateTime.Now:HH:mm:ss tt}";
            return fail;
        }
        catch (Exception ex)
        {
            var resp = new GetAuthResponseDto();
            resp.Codigo.Message = ex.Message;
            resp.Codigo.Error = "400";
            resp.Codigo.Status = "BadRequest";
            resp.Codigo.TimeStamp = $"{DateTime.Now:HH:mm:ss tt}";
            return resp;
        }
    }
                           }
