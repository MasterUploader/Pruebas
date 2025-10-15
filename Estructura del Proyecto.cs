Este es el código:


using Microsoft.AspNetCore.Mvc;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.AutenticacionDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Repository.IRepository.Autenticacion;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.Controllers;

/// <summary>
/// Clase Login, requeridad para autenticar el usuario con Ginih.
/// </summary>
/// <param name="_loginRepository">Instancia de Clase de Repositorio LoginRepository.</param>
[Route("[controller]")]
[ApiController]
public class LoginController(ILoginRepository _loginRepository) : ControllerBase
{

    /// <summary>
    /// Acciones relacionadas con los usuarios habilitados para realizar acciones API y en el portal
    /// Importante saber: Las operaciones realizadas por el API son realizadas con un JWT que tiene una duración de 5 minutos.
    /// La empresa proporcionará acceso al portal para tener el usuario y contraseña que le permitirá obtener un refresh-token que debe ser usado para obtener los tokens de corta duración.
    /// </summary>
    /// <returns>Retorna un Objeto PostLoginResponseDto</returns>
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login()
    {
        PostUsuarioLoginDto usuarioLoginDto = new()
        {
            UserName = GlobalConnection.Current.GinihUser,
            Password = GlobalConnection.Current.GinihPassword
        };

        ResponseHandler responseHandler = new();
        var postLoginResponseDto = await _loginRepository.Login(usuarioLoginDto);
        return responseHandler.HandleResponse(postLoginResponseDto, postLoginResponseDto.Status);
    }
}


using Connections.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.AutenticacionDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Repository.IRepository.Autenticacion;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.Repository;

/// <summary>
/// Clase Login Repository.
/// </summary>
public class LoginRepository(IHttpClientFactory _httpClientFactory, IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : ILoginRepository
{

    /// <summary>
    /// Para obtener un refresh-token necesitas hacer una llamada al endpoint de login. La respuesta a esta llamada obtendrá un secret token que necesitarás para obtener JWT válidos para los request. 
    /// El JWT dura 5 minutos y el refresh-token tiene una duración por defecto de 2 años (Recibirás una notificación para que puedas renovarlo antes de vencer). Si ya tienes acceso al portal de integración, puedes acceder o volver a generar uno nuevo en la vista developer -> secret.
    /// </summary>
    /// <param name="usuarioLoginDto">Objeto de Transferencia Dto</param>
    /// <returns>Retorna un Objeto de Tipo PostLoginResponseDto</returns>

    [HttpPost]
    public async Task<PostLoginResponseDto> Login(PostUsuarioLoginDto usuarioLoginDto)
    {
        try
        {
            //Obtenemos las variables globales
            string _baseUrl = GlobalConnection.Current.Host;

            PostLoginResponseDto _postLoginResponseDto = new();

            using var client = _httpClientFactory.CreateClient("GINIH");
            if (!string.IsNullOrEmpty(_baseUrl) && Uri.IsWellFormedUriString(_baseUrl, UriKind.RelativeOrAbsolute))
            {
                client.BaseAddress = new Uri(_baseUrl);
            }

            var content = new StringContent(JsonConvert.SerializeObject(usuarioLoginDto), Encoding.UTF8);

            using HttpResponseMessage response = await client.PostAsync(client.BaseAddress + "/users/login", content);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var deserialized = JsonConvert.DeserializeObject<PostLoginResponseDto>(responseContent);

            if (response.IsSuccessStatusCode && deserialized is not null)
            {
                _postLoginResponseDto = deserialized;
                _postLoginResponseDto.Status = response.StatusCode.ToString();
                var saveToken = new Token(deserialized, _connection, _contextAccessor);
                var badResponse = new PostLoginResponseDto
                {
                    Status = HttpStatusCode.NotFound.ToString(),
                    Message = "No se pudo guardar datos en la tabla de token"
                };
                return saveToken.SavenTokenUTH() ? _postLoginResponseDto : badResponse;
            }
            else if (!response.IsSuccessStatusCode && deserialized is not null)
            {
                _postLoginResponseDto = deserialized;
                _postLoginResponseDto.Status = response.StatusCode.ToString();
                return _postLoginResponseDto;
            }
            _postLoginResponseDto.Status = response.StatusCode.ToString();
            _postLoginResponseDto.Message = deserialized!.Message ?? "No devolvio valores la consulta";
            return _postLoginResponseDto;
        }
        catch (Exception ex)
        {
            PostLoginResponseDto _postLoginResponseDto = new()
            {
                Status = HttpStatusCode.NotFound.ToString(),
                Message = ex.Message
            };

            return _postLoginResponseDto;
        }
    }

    using Connections.Abstractions;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.AutenticacionDtos;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using System.Data.Common;
using System.Globalization;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;

/// <summary>
/// Clase utilitaria para manipular tokens de Ginih sobre AS/400 (DB2 for i),
/// utilizando RestUtilities.QueryBuilder para construir y ejecutar SQL de forma segura.
/// </summary>
public class Token
{
    private readonly IDatabaseConnection _connection;
    private readonly IHttpContextAccessor _contextAccessor;

    // Tabla/Library destino (AS/400)
    private readonly string _tableName = "TOKENUTH";
    private readonly string _library = "BCAH96DTA";

    // Campos que llegan del DTO (con valores por defecto para evitar nulls en INSERT/UPDATE)
    private readonly string _status = string.Empty;
    private readonly string _message = string.Empty;
    private readonly string _rToken = string.Empty;
    private readonly string _createdAt = string.Empty;
    private readonly string _timeStamp = string.Empty;
    private readonly string _value = string.Empty;
    private readonly string _name = string.Empty;

    // Campos calculados/recuperados
    private string _vence = string.Empty;
    private string _creado = string.Empty;
    private string _token = string.Empty;

    /// <summary>
    /// Constructor principal: inyecta DTO base, conexión y contexto.
    /// </summary>
    /// <param name="tokenStructure">Respuesta de login con datos de token.</param>
    /// <param name="connection">Conexión a base de datos.</param>
    /// <param name="contextAccessor">Accessor de HttpContext (para logging/decorators).</param>
    public Token(PostLoginResponseDto tokenStructure, IDatabaseConnection connection, IHttpContextAccessor contextAccessor)
    {
        _status = tokenStructure?.Status ?? string.Empty;
        _message = tokenStructure?.Message ?? string.Empty;
        _rToken = tokenStructure?.Data?.RefreshToken ?? string.Empty;
        _createdAt = tokenStructure?.Data?.CreatedAt ?? string.Empty;
        _timeStamp = tokenStructure?.TimeStamp ?? string.Empty;
        _value = tokenStructure?.Code?.Value ?? string.Empty;
        _name = tokenStructure?.Code?.Name ?? string.Empty;

        _connection = connection;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Constructor alterno: solo inyecta conexión y contexto (para lecturas).
    /// </summary>
    public Token(IDatabaseConnection connection, IHttpContextAccessor contextAccessor)
    {
        _connection = connection;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Guarda/actualiza el token en la tabla AS/400.
    /// Si no existe la fila ID=1, la inserta; caso contrario, actualiza sus campos.
    /// </summary>
    /// <remarks>
    /// Se calcula <c>_vence</c> sumando 2 años a <c>_createdAt</c> (formato ISO-UTC "yyyy-MM-ddTHH:mm:ss.fffZ").
    /// </remarks>
    /// <returns><c>true</c> si la operación fue exitosa; en caso contrario <c>false</c>.</returns>
    public bool SavenTokenUTH()
    {
        try
        {
            // 1) Calcular fecha de vencimiento a partir de CreatedAt (+2 años)
            if (DateTime.TryParseExact(_createdAt, "yyyy-MM-ddTHH:mm:ss.fffZ",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                        out var createdAtDt))
            {
                var venceDt = createdAtDt.AddYears(2);
                _vence = venceDt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            }
            else
            {
                // Si viene en formato inesperado, persiste lo recibido para no romper flujo
                _vence = _createdAt;
            }

            _connection.Open();
            if (!_connection.IsConnected) return false;

            // 2) Verificar si existe fila ID = 1
            var existsQuery = new SelectQueryBuilder(_tableName, _library)
                .Select("1")
                .WhereRaw("ID = 1")
                .Limit(1)
                .Build();

            using var cmdExists = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmdExists.CommandText = existsQuery.Sql;
            cmdExists.CommandType = System.Data.CommandType.Text;

            using var rdr = cmdExists.ExecuteReader();
            var exists = rdr.HasRows;

            // 3) Insertar o Actualizar según exista
            if (!exists)
            {
                // INSERT parametrizado (DB2 i → placeholders ? + Parameters)
                var insert = new InsertQueryBuilder(_tableName, _library, SqlDialect.Db2i)
                    .IntoColumns("ID", "STATUS", "MESSAGE", "RTOKEN", "CREATEDAT", "TIMESTAMP", "VALUE", "NAME", "VENCE")
                    .Row(
                        1,
                        _status ?? string.Empty,
                        _message ?? string.Empty,
                        _rToken ?? string.Empty,
                        _createdAt ?? string.Empty,
                        _timeStamp ?? string.Empty,
                        _value ?? string.Empty,
                        _name ?? string.Empty,
                        _vence ?? string.Empty
                    )
                    .Build();

                using var cmdIns = _connection.GetDbCommand(insert, _contextAccessor.HttpContext!);
                var aff = cmdIns.ExecuteNonQuery();
                return aff > 0;
            }
            else
            {
                // UPDATE parametrizado
                var update = new UpdateQueryBuilder(_tableName, _library, SqlDialect.Db2i)
                    .Set("STATUS", _status ?? string.Empty)
                    .Set("MESSAGE", _message ?? string.Empty)
                    .Set("RTOKEN", _rToken ?? string.Empty)
                    .Set("CREATEDAT", _createdAt ?? string.Empty)
                    .Set("TIMESTAMP", _timeStamp ?? string.Empty)
                    .Set("VALUE", _value ?? string.Empty)
                    .Set("NAME", _name ?? string.Empty)
                    .Set("VENCE", _vence ?? string.Empty)
                    .WhereRaw("ID = 1")
                    .Build();

                using var cmdUpd = _connection.GetDbCommand(update, _contextAccessor.HttpContext!);
                var aff = cmdUpd.ExecuteNonQuery();
                return aff > 0;
            }
        }
        catch (Exception ex)
        {
            // TOD: integrar con tu servicio de logging estructurado
            Console.WriteLine(ex.Message);
            return false;
        }
        finally
        {
            _connection.Close();
        }
    }

    /// <summary>
    /// Obtiene el token registrado (fila ID=1) y valida vigencia según la diferencia entre VENCE y CREATEDAT.
    /// </summary>
    /// <param name="rToken">Token de salida cuando la condición de vigencia se cumple.</param>
    /// <returns><c>true</c> si hay token válido; en caso contrario <c>false</c>.</returns>
    public bool GetToken(out string rToken)
    {
        rToken = string.Empty;

        try
        {
            _connection.Open();
            if (!_connection.IsConnected) return false;

            // SELECT VENCE, CREATEDAT, RTOKEN FROM BCAH96DTA.TOKENUTH WHERE ID = 1 FETCH FIRST 1 ROW ONLY
            var select = new SelectQueryBuilder(_tableName, _library)
                .Select("VENCE", "CREATEDAT", "RTOKEN")
                .WhereRaw("ID = 1")
                .Limit(1)
                .Build();

            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = select.Sql;
            cmd.CommandType = System.Data.CommandType.Text;

            using DbDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows) return false;

            while (reader.Read())
            {
                _vence = reader.IsDBNull(reader.GetOrdinal("VENCE")) ? string.Empty : reader.GetString(reader.GetOrdinal("VENCE"));
                _creado = reader.IsDBNull(reader.GetOrdinal("CREATEDAT")) ? string.Empty : reader.GetString(reader.GetOrdinal("CREATEDAT"));
                _token = reader.IsDBNull(reader.GetOrdinal("RTOKEN")) ? string.Empty : reader.GetString(reader.GetOrdinal("RTOKEN"));
            }

            // Parseo de fechas con el mismo formato ISO-UTC usado en SavenTokenUTH()
            if (DateTime.TryParseExact(_vence, "yyyy-MM-ddTHH:mm:ss.fffZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var venceDt)
                &&
                DateTime.TryParseExact(_creado, "yyyy-MM-ddTHH:mm:ss.fffZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var creadoDt))
            {
                var diferencia = venceDt - creadoDt;

                // Mantengo tu regla original (Days > 0). Si quieres validar contra DateTime.UtcNow:
                // si (DateTime.UtcNow <= venceDt) ...
                if (diferencia.Days > 0 && !string.IsNullOrWhiteSpace(_token))
                {
                    rToken = _token;
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            // TOD: integrar con tu servicio de logging estructurado
            Console.WriteLine(ex.Message);
            return false;
        }
        finally
        {
            _connection.Close();
        }
    }
}
}

Porque se guarda primero el SQL y luego el http en el log, si primero se ejecuto el http antes que el log.
Este es el codigo del log, hay algunas clases que no las agregue, no las cree de nuevo, solo nota como funcionan en el codigo actual:

using Logging.Abstractions;
using Logging.Configuration;
using Logging.Extensions;
using Logging.Helpers;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Data.Common;
using System.Text;

namespace Logging.Services;

/// <summary>
/// Servicio de logging que captura y almacena eventos en archivos de log.
/// - Calcula y cachea la ruta de archivo por-request.
/// - Escribe bloques fijos y entradas dinámicas sin bloquear el hilo principal.
/// - Mantiene utilidades para logs de objeto, texto y excepciones.
/// - Expone helpers para logging de SQL (éxito y error).
/// - Permite bloques manuales (StartLogBlock).
/// </summary>
public class LoggingService(
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment hostEnvironment,
    IOptions<LoggingOptions> loggingOptions) : ILoggingService
{
    // ===================== Dependencias y configuración (constructor primario) =====================

    /// <summary>Accessor del contexto HTTP para derivar el archivo de log por-request.</summary>
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <summary>Opciones de logging (rutas base y switches de .txt/.csv).</summary>
    private readonly LoggingOptions _loggingOptions = loggingOptions.Value;

    /// <summary>Directorio base de logs para la API actual: BaseLogDirectory/ApplicationName.</summary>
    private readonly string _logDirectory =
        Path.Combine(loggingOptions.Value.BaseLogDirectory,
                     !string.IsNullOrWhiteSpace(hostEnvironment.ApplicationName) ? hostEnvironment.ApplicationName : "Desconocido");

    // ===================== API pública =====================

    /// <summary>
    /// Obtiene el archivo de log de la petición actual, garantizando que toda la información
    /// se guarde en el mismo archivo. Organiza por API, controlador, endpoint (desde Path) y fecha.
    /// Respeta <c>Items["LogCustomPart"]</c> si está presente. Usa hora local.
    /// </summary>
    public string GetCurrentLogFile()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
                return BuildErrorFilePath(kind: "manual", context: null); // Fallback sin contexto

            // Si hay un path cacheado y apareció/cambió el sufijo custom, invalidamos el cache.
            if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
                existingObj is string existingPath &&
                context.Items.TryGetValue("LogCustomPart", out var partObj) &&
                partObj is string part && !string.IsNullOrWhiteSpace(part) &&
                !existingPath.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                context.Items.Remove("LogFileName");
            }

            // Reutilizar si ya está cacheado (guardamos SIEMPRE el path completo).
            if (context.Items.TryGetValue("LogFileName", out var cached) &&
                cached is string cachedPath && !string.IsNullOrWhiteSpace(cachedPath))
            {
                return cachedPath;
            }

            // Nombre del endpoint (último segmento del Path) y Controller (si existe metadata MVC).
            var endpoint = context.Request.Path.Value?.Trim('/').Split('/').LastOrDefault() ?? "UnknownEndpoint";
            var cad = context.GetEndpoint()
                             ?.Metadata
                             .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                             .FirstOrDefault();
            var controllerName = cad?.ControllerName ?? "UnknownController";

            // Identificadores y fecha/hora local para componer nombre de archivo.
            var fecha = DateTime.Now.ToString("yyyy-MM-dd");
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

            // Sufijo custom opcional (inyectado por tu middleware/extractor).
            var customPart = context.Items.TryGetValue("LogCustomPart", out var partValue) &&
                             partValue is string partStr && !string.IsNullOrWhiteSpace(partStr)
                             ? $"_{partStr}"
                             : "";

            // Carpeta final: <base>/<controller>/<endpoint>/<fecha>
            var finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
            Directory.CreateDirectory(finalDirectory); // Garantiza existencia (crea toda la jerarquía)

            // Nombre final y path completo
            var fileName = $"{endpoint}_{executionId}{customPart}_{timestamp}.txt";
            var fullPath = Path.Combine(finalDirectory, fileName);

            // Cachear el path para el resto del ciclo de vida del request.
            context.Items["LogFileName"] = fullPath;
            return fullPath;
        }
        catch (Exception ex)
        {
            LogInternalError(ex);
            return BuildErrorFilePath(kind: "manual", context: _httpContextAccessor.HttpContext);
        }
    }

    /// <summary>
    /// Escribe un log en el archivo correspondiente de la petición actual (.txt)
    /// y en su respectivo archivo .csv. Si el contenido excede cierto tamaño,
    /// se delega a <c>Task.Run</c> para no bloquear el hilo de la API.
    /// </summary>
    /// <param name="context">Contexto HTTP actual (opcional, para reglas de cabecera/pie).</param>
    /// <param name="logContent">Contenido del log a registrar.</param>
    public void WriteLog(HttpContext? context, string logContent)
    {
        try
        {
            var filePath = GetCurrentLogFile();
            var isNewFile = !File.Exists(filePath);

            StringBuilder logBuilder = new();

            // Cabecera automática solo en el primer write de ese archivo.
            if (isNewFile) logBuilder.AppendLine(LogFormatter.FormatBeginLog());

            // Contenido del log aportado por el llamador.
            logBuilder.AppendLine(logContent);

            // Pie automático si la respuesta ya inició (headers enviados).
            if (context is not null && context.Response.HasStarted)
                logBuilder.AppendLine(LogFormatter.FormatEndLog());

            var fullText = logBuilder.ToString();

            // Si el log supera ~128KB, escribir en background para no bloquear.
            var isLargeLog = fullText.Length > (128 * 1024);
            if (isLargeLog)
            {
                Task.Run(() =>
                {
                    if (_loggingOptions.GenerateTxt) LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
                    if (_loggingOptions.GenerateCsv) LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
                });
            }
            else
            {
                if (_loggingOptions.GenerateTxt) LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
                if (_loggingOptions.GenerateCsv) LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
            }
        }
        catch (Exception ex)
        {
            // El logging nunca debe interrumpir el flujo del request.
            LogInternalError(ex);
        }
    }

    /// <summary>
    /// Agrega un log manual de texto en el archivo de log actual.
    /// </summary>
    public void AddSingleLog(string message)
    {
        try
        {
            var formatted = LogFormatter.FormatSingleLog(message).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Registra un objeto en los logs con un nombre descriptivo.
    /// </summary>
    public void AddObjLog(string objectName, object logObject)
    {
        try
        {
            var formatted = LogFormatter.FormatObjectLog(objectName, logObject).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Registra un objeto en los logs sin necesidad de un nombre específico.
    /// Se utiliza el nombre del tipo del objeto si está disponible.
    /// </summary>
    public void AddObjLog(object logObject)
    {
        try
        {
            var objectName = logObject?.GetType()?.Name ?? "ObjetoDesconocido";
            var safeObject = logObject ?? new { }; // evita null en el serializador
            var formatted = LogFormatter.FormatObjectLog(objectName, safeObject).Indent(LogScope.CurrentLevel);

            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Registra excepciones en los logs (canal transversal para diagnósticos).
    /// </summary>
    public void AddExceptionLog(Exception ex)
    {
        try
        {
            var formatted = LogFormatter.FormatExceptionDetails(ex.ToString()).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception e) { LogInternalError(e); }
    }

    /// <summary>
    /// Registra un log de SQL exitoso y lo encola con el INICIO real para ordenar cronológicamente
    /// entre (4) Request Info y (5) Response Info.
    /// </summary>
    public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
    {
        try
        {
            var formatted = LogFormatter.FormatDbExecution(model); // respeta tu formato visual

            if (context is not null)
            {
                // 1) Preferir el INICIO real propagado por el wrapper
                DateTime? fromItems = context.Items.TryGetValue("__SqlStartedUtc", out var o) && o is DateTime dt ? dt : null;

                // 2) Si no existe, usar el StartTime del modelo (cuando lo cargues correctamente)
                DateTime? fromModel;
                if (model.StartTime.Kind == DateTimeKind.Utc)
                {
                    fromModel = model.StartTime != default ? (model.StartTime) : null;
                }
                else
                {
                    fromModel = model.StartTime != default ? (model.StartTime.ToUniversalTime()) : null;
                }

                // 3) Último recurso: ahora (no ideal, pero nunca dejamos null)
                var startedUtc = fromItems ?? fromModel ?? DateTime.UtcNow;

                if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
                if (context.Items["HttpClientLogsTimed"] is List<object> timed)
                    timed.Add(new { TsUtc = startedUtc, Seq = NextSeq(context), Content = formatted });
            }
            else
            {
                // Sin contexto: escribir directo para no perder el evento
                WriteLog(context, formatted);
            }
        }
        catch (Exception loggingEx)
        {
            LogInternalError(loggingEx);
        }
    }



    /// <summary>
    /// Registra un log de SQL con error y lo encola con el INICIO real para mantener el orden cronológico.
    /// </summary>
    public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
    {
        try
        {
            var info = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
            var tabla = LogHelper.ExtractTableName(command.CommandText);

            var formatted = LogFormatter.FormatDbExecutionError(
                nombreBD: info.Database,
                ip: info.Ip,
                puerto: info.Port,
                biblioteca: info.Library,
                tabla: tabla,
                sentenciaSQL: command.CommandText,
                exception: ex,
                horaError: DateTime.Now
            );

            if (context is not null)
            {
                // Preferimos el INICIO real que puso el wrapper; si no, ahora (UTC).
                var startedUtc = context.Items.TryGetValue("__SqlStartedUtc", out var o) && o is DateTime dt ? dt : DateTime.UtcNow;

                if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
                if (context.Items["HttpClientLogsTimed"] is List<object> timed)
                    timed.Add(new { TsUtc = startedUtc, Seq = NextSeq(context), Content = formatted });
            }
            else
            {
                WriteLog(context, formatted); // fallback sin contexto
            }

            AddExceptionLog(ex); // rastro transversal
        }
        catch (Exception fail)
        {
            LogInternalError(fail);
        }
    }

    // ===================== Bloques manuales =====================

    #region Métodos para AddSingleLog en bloque

    /// <summary>
    /// Inicia un bloque de log. Escribe una cabecera común y permite ir agregando filas
    /// con <see cref="ILogBlock.Add(string)"/>. Al finalizar, llamar <see cref="ILogBlock.End()"/>
    /// o disponer el objeto (using) para escribir el cierre del bloque.
    /// </summary>
    /// <param name="title">Título o nombre del bloque (ej. "Proceso de conciliación").</param>
    /// <param name="context">Contexto HTTP (opcional). Si es null, se usa el contexto actual si existe.</param>
    /// <returns>Instancia del bloque para agregar filas.</returns>
    public ILogBlock StartLogBlock(string title, HttpContext? context = null)
    {
        context ??= _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile(); // asegura que compartimos el mismo archivo de la request

        // Cabecera del bloque
        var header = LogFormatter.BuildBlockHeader(title);
        LogHelper.SafeWriteLog(_logDirectory, filePath, header);

        return new LogBlock(this, filePath, title);
    }

    /// <summary>
    /// Implementación concreta de un bloque de log.
    /// </summary>
    private sealed class LogBlock(LoggingService svc, string filePath, string title) : ILogBlock
    {
        private readonly LoggingService _svc = svc;
        private readonly string _filePath = filePath;
        private readonly string _title = title;
        private int _ended; // 0 no, 1 sí (para idempotencia)

        /// <inheritdoc />
        public void Add(string message, bool includeTimestamp = false)
        {
            // cada "Add" es una fila en el mismo archivo, dentro del bloque
            var line = includeTimestamp
                ? $"[{DateTime.Now:HH:mm:ss}]•{message}"
                : $"• {message}";
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, line + Environment.NewLine);
        }

        /// <inheritdoc />
        public void AddObj(string name, object obj)
        {
            var formatted = LogFormatter.FormatObjectLog(name, obj);
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, formatted);
        }

        /// <inheritdoc />
        public void AddException(Exception ex)
        {
            var formatted = LogFormatter.FormatExceptionDetails(ex.ToString());
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, formatted);
        }

        /// <inheritdoc />
        public void End()
        {
            if (Interlocked.Exchange(ref _ended, 1) == 1) return; // ya finalizado
            var footer = LogFormatter.BuildBlockFooter();
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, footer);
        }

        public void Dispose() => End();
    }

    #endregion


    // ===================== Utilidades privadas =====================

    /// <summary>
    /// Devuelve un nombre seguro para usar en rutas/archivos (quita caracteres inválidos).
    /// </summary>
    private static string Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";
        var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
        var cleaned = new string([.. name.Where(c => !invalid.Contains(c))]).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Unknown" : cleaned;
    }

    /// <summary>
    /// Obtiene un nombre de endpoint seguro desde el <see cref="HttpContext"/>.
    /// </summary>
    private static string GetEndpointSafe(HttpContext? context)
    {
        if (context is null) return "NoContext";

        var cad = context.GetEndpoint()?.Metadata
            .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            .FirstOrDefault();

        var endpoint = cad?.ActionName
                       ?? (context.Request.Path.Value ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                       ?? "UnknownEndpoint";

        return Sanitize(endpoint);
    }

    /// <summary>
    /// Carpeta de errores por fecha local: &lt;base&gt;/Errores/&lt;yyyy-MM-dd&gt;.
    /// </summary>
    private string GetErrorsDirectory(DateTime nowLocal)
    {
        var dir = Path.Combine(_logDirectory, "Errores", nowLocal.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Construye un path de archivo de error con ExecutionId, Endpoint y timestamp local.
    /// Sufijo: "internal" para errores internos; "manual" para global manual logs.
    /// </summary>
    private string BuildErrorFilePath(string kind, HttpContext? context)
    {
        var now = DateTime.Now;
        var dir = GetErrorsDirectory(now);

        var executionId = context?.Items?["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();
        var endpoint = GetEndpointSafe(context);
        var timestamp = now.ToString("yyyyMMdd_HHmmss");

        var suffix = string.Equals(kind, "internal", StringComparison.OrdinalIgnoreCase) ? "_internal" : "";
        var fileName = $"{executionId}_{endpoint}_{timestamp}{suffix}.txt";

        return Path.Combine(dir, fileName);
    }

    /// <summary>
    /// Clave de secuencia por-request para desempatar eventos con el mismo TsUtc.
    /// </summary>
    private const string TimedSeqKey = "__TimedSeq";

    /// <summary>
    /// Devuelve un número incremental por-request. Se almacena en Items[TimedSeqKey].
    /// </summary>
    private static long NextSeq(HttpContext ctx)
    {
        // Como Items es por-request, no necesitamos sincronización pesada aquí.
        var curr = ctx.Items.TryGetValue(TimedSeqKey, out var obj) && obj is long c ? c : 0L;
        curr++;
        ctx.Items[TimedSeqKey] = curr;
        return curr;
    }

    /// <summary>
    /// Registra errores internos del propio servicio en la carpeta de errores.
    /// Nunca interrumpe la solicitud en curso.
    /// </summary>
    public void LogInternalError(Exception ex)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var errorPath = BuildErrorFilePath(kind: "internal", context: context);

            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error en LoggingService: {ex}{Environment.NewLine}";
            File.AppendAllText(errorPath, msg);
        }
        catch
        {
            // Evita bucles de error del propio logger
        }
    }
}

using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;

namespace Connections.Helpers;

/// <summary>
/// Especifica un parámetro OUT/INOUT, incluyendo metadatos como tamaño y precisión/escala.
/// </summary>
public readonly struct OutSpec(string name, DbType type, int? size = null, byte? precision = null, byte? scale = null, object? initial = null)
{
    public string Name { get; } = name;
    public DbType Type { get; } = type;
    public int? Size { get; } = size;
    public byte? Precision { get; } = precision;
    public byte? Scale { get; } = scale;
    public object? Initial { get; } = initial;
}

/// <summary>
/// Builder fluido para invocar programas CLLE/RPGLE mediante <c>{CALL LIB.PROG(?, ?, ...)}</c>
/// con parámetros **posicionales** y helpers que exigen el **nombre lógico** de cada parámetro
/// (solo para trazabilidad; el proveedor sigue usando el orden).
/// </summary>
public sealed class ProgramCallBuilder
{
    // Dependencias / estado
    private readonly IDatabaseConnection? _connection;             // overload con interfaz
    private readonly Func<HttpContext?, DbCommand>? _getCmd;       // overload con delegado

    private string _library;
    private readonly string _program;

    // IN: lista de fábricas de parámetros en el orden en que se agregan
    private readonly List<Func<DbCommand, DbParameter>> _paramFactories = [];
    // OUT: especificaciones con metadatos
    private readonly List<OutSpec> _bulkOuts = [];

    // Configuración operativa
    private int? _commandTimeoutSeconds;
    private int _retryAttempts = 0;
    private TimeSpan _retryBackoff = TimeSpan.Zero;
    private string? _traceId;

    // SQL emitido
    private enum Naming { SqlDot, SystemSlash }
    private Naming _naming = Naming.SqlDot;     // LIB.PROG
    private bool _wrapWithBraces = true;        // {CALL ...}

    // Normalización de OUTs
    private bool _trimOutStringPadding = true;  // recorta '\0' y ' '
    private bool _emptyStringAsNull = false;    // "" -> null
    private bool _forceUnspecifiedDateTime = true;

    #region Creación

    private ProgramCallBuilder(IDatabaseConnection connection, string library, string program)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _library = EnsureText(library);
        _program = EnsureText(program);
    }

    private ProgramCallBuilder(Func<HttpContext?, DbCommand> getDbCommand, string library, string program)
    {
        _getCmd = getDbCommand ?? throw new ArgumentNullException(nameof(getDbCommand));
        _library = EnsureText(library);
        _program = EnsureText(program);
    }

    private static string EnsureText(string v)
        => string.IsNullOrWhiteSpace(v) ? throw new ArgumentNullException(nameof(v)) : v.Trim();

    /// <summary>Crea el builder usando una conexión de la librería (recomendado).</summary>
    public static ProgramCallBuilder For(IDatabaseConnection connection, string library, string program)
        => new(connection, library, program);

    /// <summary>
    /// Crea el builder usando un delegado que devuelve <see cref="DbCommand"/>.
    /// Útil si quieres evitar referenciar la interfaz directamente.
    /// </summary>
    public static ProgramCallBuilder For(Func<HttpContext?, DbCommand> getDbCommand, string library, string program)
        => new(getDbCommand, library, program);

    #endregion

    #region Configuración general

    /// <summary>Override puntual de la librería (schema).</summary>
    public ProgramCallBuilder OnLibrary(string library) { _library = EnsureText(library); return this; }

    /// <summary>Define el timeout del comando en segundos.</summary>
    public ProgramCallBuilder WithTimeout(int seconds)
    {
        _commandTimeoutSeconds = seconds >= 0 ? seconds : throw new ArgumentOutOfRangeException(nameof(seconds));
        return this;
    }

    /// <summary>Asigna un TraceId visible en <see cref="HttpContext.Items"/> con la clave <c>"TraceId"</c>.</summary>
    public ProgramCallBuilder WithTraceId(string? traceId) { _traceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId; return this; }

    /// <summary>Configura reintentos ante errores transitorios con backoff.</summary>
    public ProgramCallBuilder WithRetry(int attempts, TimeSpan backoff)
    {
        _retryAttempts = Math.Max(0, attempts);
        _retryBackoff = backoff < TimeSpan.Zero ? TimeSpan.Zero : backoff;
        return this;
    }

    /// <summary>Usa convención SQL (LIB.PROG). Es la opción por defecto.</summary>
    public ProgramCallBuilder UseSqlNaming() { _naming = Naming.SqlDot; return this; }
    /// <summary>Usa convención de sistema (LIB/PROG).</summary>
    public ProgramCallBuilder UseSystemNaming() { _naming = Naming.SystemSlash; return this; }
    /// <summary>Envuelve el CALL en llaves ODBC: <c>{CALL ...}</c>. Activado por defecto.</summary>
    public ProgramCallBuilder WrapCallWithBraces(bool enable = true) { _wrapWithBraces = enable; return this; }

    /// <summary>Recorta '\0' y espacios a la derecha en OUTs de texto.</summary>
    public ProgramCallBuilder TrimOutStringPadding(bool enabled = true) { _trimOutStringPadding = enabled; return this; }
    /// <summary>Convierte cadenas vacías a <c>null</c> en OUTs.</summary>
    public ProgramCallBuilder EmptyStringAsNull(bool enabled = true) { _emptyStringAsNull = enabled; return this; }
    /// <summary>Fuerza <see cref="DateTimeKind.Unspecified"/> en OUTs de fecha/hora.</summary>
    public ProgramCallBuilder ForceUnspecifiedDateTime(bool enabled = true) { _forceUnspecifiedDateTime = enabled; return this; }

    #endregion

    #region Parámetros IN (helpers con nombre obligatorio)

    /// <summary>
    /// Agrega un parámetro IN genérico (posicional). El nombre es solo para trazas.
    /// </summary>
    public ProgramCallBuilder In(
        string name,
        object? value,
        DbType? dbType = null,
        int? size = null,
        byte? precision = null,
        byte? scale = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

        _paramFactories.Add(cmd =>
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;                  // etiqueta para logging; el binding sigue siendo posicional
            p.Direction = ParameterDirection.Input;

            if (dbType.HasValue) p.DbType = dbType.Value;
            if (size.HasValue) p.Size = size.Value;
            if (precision.HasValue) p.Precision = precision.Value;
            if (scale.HasValue) p.Scale = scale.Value;

            p.Value = value ?? DBNull.Value;
            return p;
        });
        return this;
    }

    /// <summary>IN string (VARCHAR/NVARCHAR) con nombre obligatorio.</summary>
    public ProgramCallBuilder InString(string name, string? value, int? size = null)
        => In(name, value, DbType.String, size: size);

    /// <summary>IN carácter fijo (CHAR(n)) con nombre obligatorio.</summary>
    public ProgramCallBuilder InChar(string name, string? value, int size)
        => In(name, value, DbType.AnsiStringFixedLength, size: size);

    /// <summary>IN decimal con nombre obligatorio (precisión/escala opcionales).</summary>
    public ProgramCallBuilder InDecimal(string name, decimal? value, byte? precision = null, byte? scale = null)
        => In(name, value, DbType.Decimal, precision: precision, scale: scale);

    /// <summary>IN entero de 32 bits con nombre obligatorio.</summary>
    public ProgramCallBuilder InInt32(string name, int? value)
        => In(name, value, DbType.Int32);

    /// <summary>IN fecha/hora con nombre obligatorio.</summary>
    public ProgramCallBuilder InDateTime(string name, DateTime? value)
        => In(name, value, DbType.DateTime);

    #endregion

    #region Parámetros OUT/INOUT (helpers con nombre obligatorio)

    /// <summary>
    /// Declara un parámetro OUT/INOUT. Si <paramref name="initialValue"/> se define, el parámetro actuará como INOUT.
    /// </summary>
    public ProgramCallBuilder Out(
        string name,
        DbType dbType,
        int? size = null,
        byte? precision = null,
        byte? scale = null,
        object? initialValue = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        _bulkOuts.Add(new OutSpec(name, dbType, size, precision, scale, initialValue));
        return this;
    }

    /// <summary>OUT string (VARCHAR/NVARCHAR) con tamaño y nombre obligatorios.</summary>
    public ProgramCallBuilder OutString(string name, int size, object? initialValue = null)
        => Out(name, DbType.String, size: size, initialValue: initialValue);

    /// <summary>OUT carácter fijo (CHAR(n)) con nombre obligatorio.</summary>
    public ProgramCallBuilder OutChar(string name, int size, object? initialValue = null)
        => Out(name, DbType.AnsiStringFixedLength, size: size, initialValue: initialValue);

    /// <summary>OUT decimal con precisión/escala y nombre obligatorios.</summary>
    public ProgramCallBuilder OutDecimal(string name, byte precision, byte scale, object? initialValue = null)
        => Out(name, DbType.Decimal, precision: precision, scale: scale, initialValue: initialValue);

    /// <summary>OUT entero de 32 bits con nombre obligatorio.</summary>
    public ProgramCallBuilder OutInt32(string name, int? initialValue = null)
        => Out(name, DbType.Int32, initialValue: initialValue);

    /// <summary>OUT fecha/hora con nombre obligatorio.</summary>
    public ProgramCallBuilder OutDateTime(string name, DateTime? initialValue = null)
        => Out(name, DbType.DateTime, initialValue: initialValue);

    #endregion

    #region Mapeo desde DTO (IN)

    /// <summary>
    /// Agrega parámetros IN posicionales a partir de un objeto,
    /// siguiendo el orden de nombres de propiedades proporcionado.
    /// Útil para llamadas con muchos IN.
    /// </summary>
    public ProgramCallBuilder FromObject(object source, IEnumerable<string> order)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(order);

        var type = source.GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var name in order)
        {
            if (!props.TryGetValue(name, out var pi))
                throw new ArgumentException($"La propiedad '{name}' no existe en {type.Name}.");

            var value = pi.GetValue(source);
            In(name, value); // reusa la API IN con nombre
        }
        return this;
    }

    #endregion

    #region Ejecución

    /// <summary>Ejecuta el <c>CALL</c> y devuelve filas afectadas + OUT/INOUT.</summary>
    public Task<ProgramCallResult> CallAsync(HttpContext? httpContext = null, CancellationToken cancellationToken = default)
        => ExecuteInternalAsync(httpContext, readerCallback: null, cancellationToken);

    /// <summary>Ejecuta el <c>CALL</c> y permite leer un result set si el programa hace <c>SELECT</c>.</summary>
    public Task<ProgramCallResult> CallAndReadAsync(HttpContext? httpContext, Func<DbDataReader, Task> readerCallback, CancellationToken cancellationToken = default)
        => ExecuteInternalAsync(httpContext, readerCallback, cancellationToken);

    private async Task<ProgramCallResult> ExecuteInternalAsync(HttpContext? httpContext, Func<DbDataReader, Task>? readerCallback, CancellationToken ct)
    {
        var sql = BuildSql();
        int attempt = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Crea el comando (según overload usado al construir el builder)
                using var command = _getCmd is not null ? _getCmd(httpContext) : _connection!.GetDbCommand(httpContext);

                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                if (_commandTimeoutSeconds.HasValue) command.CommandTimeout = _commandTimeoutSeconds.Value;

                // Enviar TraceId a logging (si lo usas en tu wrapper)
                if (httpContext != null && !string.IsNullOrWhiteSpace(_traceId))
                    httpContext.Items["TraceId"] = _traceId;

                // 1) IN en orden de registro
                foreach (var factory in _paramFactories)
                    command.Parameters.Add(factory(command));

                // 2) OUT/INOUT (van después de IN, también posicionales)
                foreach (var o in _bulkOuts)
                {
                    var p = command.CreateParameter();
                    p.ParameterName = o.Name;
                    p.Direction = o.Initial is null ? ParameterDirection.Output : ParameterDirection.InputOutput;
                    p.DbType = o.Type;
                    if (o.Size.HasValue) p.Size = o.Size.Value;
                    if (o.Precision.HasValue) p.Precision = o.Precision.Value;
                    if (o.Scale.HasValue) p.Scale = o.Scale.Value;
                    if (o.Initial is not null) p.Value = o.Initial;
                    command.Parameters.Add(p);
                }

                var sw = Stopwatch.StartNew();

                var result = new ProgramCallResult();

                if (readerCallback is null)
                {
                    var rows = await ExecuteNonQueryAsync(command, ct).ConfigureAwait(false);
                    result.RowsAffected = rows;
                }
                else
                {
                    using var reader = await ExecuteReaderAsync(command, ct).ConfigureAwait(false);
                    await readerCallback(reader).ConfigureAwait(false);
                    result.RowsAffected = reader.RecordsAffected;
                }

                sw.Stop();
                if (httpContext != null) httpContext.Items["ProgramCallDurationMs"] = sw.ElapsedMilliseconds;

                // Captura de OUT/INOUT con normalización
                foreach (DbParameter p in command.Parameters)
                {
                    if (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue)
                    {
                        var key = string.IsNullOrWhiteSpace(p.ParameterName)
                            ? $"out_{result.OutValues.Count + 1}"
                            : p.ParameterName!;
                        result.AddOut(key, NormalizeOutValue(p.Value));
                    }
                }

                return result;
            }
            catch (DbException ex) when (attempt < _retryAttempts && IsTransient(ex))
            {
                attempt++;
                if (_retryBackoff > TimeSpan.Zero)
                    await Task.Delay(_retryBackoff, ct).ConfigureAwait(false);
                // reintentar
            }
        }
    }

    /// <summary>
    /// Normaliza valores OUT: 
    /// - string/char[]: quita NUL (0x00) y espacios a la derecha; opcionalmente ""→null
    /// - DateTime: fuerza Kind=Unspecified si está activado
    /// - DBNull → null
    /// </summary>
    private object? NormalizeOutValue(object? raw)
    {
        if (raw is null || raw is DBNull) return null;

        switch (raw)
        {
            case string s:
                if (_trimOutStringPadding) s = s.TrimEnd('\0', ' ');
                if (_emptyStringAsNull && s.Length == 0) return null;
                return s;

            case char[] chars:
                var s2 = new string(chars);
                if (_trimOutStringPadding) s2 = s2.TrimEnd('\0', ' ');
                if (_emptyStringAsNull && s2.Length == 0) return null;
                return s2;

            case DateTime dt:
                return _forceUnspecifiedDateTime && dt.Kind != DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dt, DateTimeKind.Unspecified)
                    : dt;

            default:
                return raw;
        }
    }

    private string BuildSql()
    {
        // Total de placeholders = IN + OUT/INOUT
        int paramCount = _paramFactories.Count + _bulkOuts.Count;
        var placeholders = paramCount == 0 ? "" : string.Join(", ", Enumerable.Repeat("?", paramCount));

        var sep = _naming == Naming.SqlDot ? "." : "/";
        var target = $"{_library}{sep}{_program}".ToUpperInvariant();
        var core = paramCount == 0 ? $"CALL {target}()" : $"CALL {target}({placeholders})";

        return _wrapWithBraces ? "{" + core + "}" : core;
    }

    private static bool IsTransient(DbException ex)
    {
        // Ajusta con tus SQLSTATE/SQLCODE según necesites
        var msg = ex.Message?.ToLowerInvariant() ?? "";
        return msg.Contains("deadlock") || msg.Contains("timeout") || msg.Contains("temporar")
               || msg.Contains("lock") || msg.Contains("08001") || msg.Contains("08004")
               || msg.Contains("40001") || msg.Contains("57033") || msg.Contains("57014") || msg.Contains("57016");
    }

    private static async Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken ct)
    {
        try { return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
        catch (NotSupportedException) { return await Task.Run(() => command.ExecuteNonQuery(), ct).ConfigureAwait(false); }
    }

    private static async Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken ct)
    {
        try { return await command.ExecuteReaderAsync(ct).ConfigureAwait(false); }
        catch (NotSupportedException) { return await Task.Run(() => command.ExecuteReader(), ct).ConfigureAwait(false); }
    }

    #endregion
}



using System.Reflection;

namespace Connections.Helpers;

/// <summary>
/// Resultado de invocar un programa CLLE/RPGLE mediante <c>CALL</c>.
/// Contiene el número de filas afectadas y los valores de parámetros OUT/INOUT devueltos por el programa.
/// </summary>
public sealed class ProgramCallResult
{
    /// <summary>
    /// Obtiene o establece el número de filas afectadas reportado por la ejecución del comando.
    /// Para llamadas <c>CALL</c> suele ser 0, pero si el programa realiza operaciones DML podría ser &gt; 0.
    /// </summary>
    public int RowsAffected { get; internal set; }

    /// <summary>
    /// Diccionario inmutable con los valores finales de los parámetros de salida (OUT/INOUT),
    /// indexados por la clave lógica (normalmente el nombre de parámetro declarado).
    /// </summary>
    public IReadOnlyDictionary<string, object?> OutValues => _outValues;
    private readonly Dictionary<string, object?> _outValues = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Agrega o reemplaza un valor OUT/INOUT en el resultado. Uso interno del builder.
    /// </summary>
    /// <param name="name">Nombre lógico del parámetro (clave).</param>
    /// <param name="value">Valor devuelto por el motor; puede ser <see cref="DBNull"/> o <c>null</c>.</param>
    internal void AddOut(string name, object? value) => _outValues[name] = value;

    /// <summary>
    /// Intenta obtener un valor OUT/INOUT fuertemente tipado.
    /// </summary>
    /// <typeparam name="T">Tipo de destino (por ejemplo, <c>int</c>, <c>decimal</c>, <c>string</c>).</typeparam>
    /// <param name="key">Nombre lógico del parámetro OUT/INOUT.</param>
    /// <param name="value">Valor convertido a <typeparamref name="T"/> si existe y puede convertirse.</param>
    /// <returns><c>true</c> si la clave existe y la conversión fue exitosa; de lo contrario, <c>false</c>.</returns>
    public bool TryGet<T>(string key, out T? value)
    {
        if (_outValues.TryGetValue(key, out var raw))
        {
            value = TypeCoercion.ChangeType<T>(raw);
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Mapea los valores OUT/INOUT a un DTO de salida, mediante un mapeo declarativo OUT→Propiedad.
    /// </summary>
    /// <typeparam name="T">Tipo del DTO de destino. Debe tener un constructor público sin parámetros.</typeparam>
    /// <param name="map">
    /// Acción que configura las asociaciones entre claves OUT y propiedades del DTO usando <see cref="OutputMapBuilder{T}"/>.
    /// </param>
    /// <returns>Instancia de <typeparamref name="T"/> con las propiedades asignadas desde los OUT encontrados.</returns>
    /// <remarks>
    /// Solo se asignan las claves OUT que existan en <see cref="OutValues"/> y tengan una propiedad asociada.
    /// Las conversiones usan <see cref="Convert.ChangeType(object, Type)"/> con manejo básico para enums y nullables.
    /// </remarks>
    public T MapTo<T>(Action<OutputMapBuilder<T>> map) where T : new()
    {
        ArgumentNullException.ThrowIfNull(map);

        var builder = new OutputMapBuilder<T>();
        map(builder);

        var target = new T();

        foreach (var kv in builder.Bindings)
        {
            var outKey = kv.Key;
            var prop = kv.Value;

            if (_outValues.TryGetValue(outKey, out var raw))
            {
                var converted = TypeCoercion.ChangeType(raw, prop.PropertyType);
                prop.SetValue(target, converted);
            }
        }

        return target;
    }

    /// <summary>
    /// Utilidades internas para conversión de tipos comunes, incluyendo nullables y enums.
    /// </summary>
    private static class TypeCoercion
    {
        public static T? ChangeType<T>(object? value)
        {
            if (value is null || value is DBNull) return default;
            var target = typeof(T);
            return (T?)ChangeType(value, target);
        }

        public static object? ChangeType(object? value, Type destinationType)
        {
            if (value is null || value is DBNull) return null;

            var nonNullable = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

            // Enum: soporta fuente numérica o string
            if (nonNullable.IsEnum)
            {
                if (value is string s)
                    return Enum.Parse(nonNullable, s, ignoreCase: true);

                var underlying = Enum.GetUnderlyingType(nonNullable);
                var numeric = Convert.ChangeType(value, underlying);
                return Enum.ToObject(nonNullable, numeric!);
            }

            // Convert.ChangeType maneja la mayoría de casos básicos
            return Convert.ChangeType(value, nonNullable);
        }
    }
}

/// <summary>
/// Builder para declarar el mapeo entre claves OUT/INOUT devueltas por el programa
/// y propiedades del DTO de salida al usar <see cref="ProgramCallResult.MapTo{T}(Action{OutputMapBuilder{T}})"/>.
/// </summary>
/// <typeparam name="T">Tipo del DTO de salida.</typeparam>
public sealed class OutputMapBuilder<T> where T : new()
{
    /// <summary>
    /// Colección interna de asociaciones OUT→Propiedad.
    /// </summary>
    internal Dictionary<string, PropertyInfo> Bindings { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Asocia la clave OUT/INOUT <paramref name="outName"/> a una propiedad del DTO <typeparamref name="T"/>.
    /// </summary>
    /// <param name="outName">Nombre lógico del parámetro OUT/INOUT (clave en <see cref="ProgramCallResult.OutValues"/>).</param>
    /// <param name="propertyName">
    /// Nombre de la propiedad pública del DTO a la que se asignará el valor.
    /// Use esta sobrecarga cuando no quiera usar expresiones lambda.
    /// </param>
    /// <returns>El mismo builder para encadenar llamadas.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="outName"/> o <paramref name="propertyName"/> es nulo o vacío.</exception>
    /// <exception cref="ArgumentException">Si la propiedad no existe o no es legible/escribible.</exception>
    public OutputMapBuilder<T> Bind(string outName, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(outName)) throw new ArgumentNullException(nameof(outName));
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException(nameof(propertyName));

        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop is null || !prop.CanWrite)
            throw new ArgumentException($"La propiedad '{propertyName}' no existe o no es asignable en el tipo {typeof(T).Name}.");

        Bindings[outName] = prop;
        return this;
    }

    /// <summary>
    /// Asocia la clave OUT/INOUT <paramref name="outName"/> a una propiedad del DTO mediante expresión lambda.
    /// </summary>
    /// <param name="outName">Nombre lógico del parámetro OUT/INOUT.</param>
    /// <param name="selector">
    /// Expresión que selecciona la propiedad del DTO, por ejemplo: <c>x =&gt; x.Codigo</c>.
    /// </param>
    /// <returns>El mismo builder para encadenar llamadas.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="outName"/> o <paramref name="selector"/> es nulo.</exception>
    /// <exception cref="InvalidOperationException">Si la expresión no apunta a una propiedad válida.</exception>
    public OutputMapBuilder<T> Bind(string outName, System.Linq.Expressions.Expression<Func<T, object?>> selector)
    {
        if (string.IsNullOrWhiteSpace(outName)) throw new ArgumentNullException(nameof(outName));
        ArgumentNullException.ThrowIfNull(selector);

        var member = selector.Body as System.Linq.Expressions.MemberExpression
                     ?? (selector.Body as System.Linq.Expressions.UnaryExpression)?.Operand as System.Linq.Expressions.MemberExpression;

        if (member?.Member is not PropertyInfo pi || !pi.CanWrite)
            throw new InvalidOperationException("El selector debe apuntar a una propiedad pública asignable del DTO.");

        Bindings[outName] = pi;
        return this;
    }
}



using System.Collections.Concurrent;
using System.Text;

namespace Logging.Ordering;

/// <summary>
/// Buffer por-request que asegura el orden canónico:
/// 1) Inicio → 2) Environment → 3) Controlador → 4) Request → [DINÁMICOS] → 5) Response → 6) Errores → 7) Fin.
/// Los DINÁMICOS se ordenan por tiempo real de ejecución.
/// </summary>
public sealed class RequestLogBuffer(string filePath)
{
    /// <summary>Ruta final del archivo de log de este request.</summary>
    public string FilePath { get; } = filePath;

    // Secuencia incremental local para desempates.
    private int _seq;

    // Cola concurrente para segmentos dinámicos.
    private readonly ConcurrentQueue<DynamicLogSegment> _dynamic = new();

    // Slots fijos (uno cada uno). Errores puede acumular varios.
    public string? FixedEnvironment { get; private set; }
    public string? FixedController { get; private set; }
    public string? FixedRequest { get; private set; }
    public string? FixedResponse { get; private set; }
    public List<string> FixedErrors { get; } = [];

    /// <summary>Coloca/actualiza Environment Info.</summary>
    public void SetEnvironment(string content) => FixedEnvironment = content;

    /// <summary>Coloca/actualiza Controlador.</summary>
    public void SetController(string content) => FixedController = content;

    /// <summary>Coloca/actualiza Request Info.</summary>
    public void SetRequest(string content) => FixedRequest = content;

    /// <summary>Coloca/actualiza Response Info.</summary>
    public void SetResponse(string content) => FixedResponse = content;

    /// <summary>Agrega un bloque de error (se listan al final, antes del fin).</summary>
    public void AddError(string content) => FixedErrors.Add(content);

    /// <summary>Agrega un segmento dinámico (HTTP, SQL, manual, etc.).</summary>
    public void AppendDynamic(DynamicLogKind kind, string content)
    {
        var seq = Interlocked.Increment(ref _seq);
        _dynamic.Enqueue(DynamicLogSegment.Create(kind, seq, content));
    }

    /// <summary>
    /// Compone SOLO la porción ordenada desde 2) Environment hasta 6) Errores (sin Inicio/Fin).
    /// El “Inicio de Log” y “Fin de Log” los agrega LoggingService.WriteLog automáticamente.
    /// </summary>
    public string BuildCore()
    {
        // Ordena los dinámicos por timestamp y secuencia.
        List<DynamicLogSegment> dyn = [];
        while (_dynamic.TryDequeue(out var seg)) dyn.Add(seg);

        var dynOrdered = dyn
            .OrderBy(s => s.TimestampUtc)
            .ThenBy(s => s.Sequence)
            .ToList();

        // Ensambla en el orden fijo + ventana dinámica.
        var sb = new StringBuilder(capacity: 64 * 1024);

        if (FixedEnvironment is not null) sb.Append(FixedEnvironment);
        if (FixedController is not null) sb.Append(FixedController);
        if (FixedRequest is not null) sb.Append(FixedRequest);

        foreach (var d in dynOrdered) sb.Append(d.Content);

        if (FixedResponse is not null) sb.Append(FixedResponse);

        if (FixedErrors.Count > 0)
            foreach (var e in FixedErrors) sb.Append(e);

        return sb.ToString();
    }
}


namespace Logging.Ordering;

/// <summary>
/// Segmento dinámico (HTTP/SQL/Manual...) con sello temporal y secuencia incremental.
/// Se ordena por tiempo de creación y sequence para empate.
/// </summary>
public sealed class DynamicLogSegment(DynamicLogKind kind, DateTime timestampUtc, int sequence, string content)
{
    /// <summary>Clasificación del segmento (no afecta la posición fija, solo clasificación).</summary>
    public DynamicLogKind Kind { get; } = kind;

    /// <summary>Instante UTC de creación del segmento (momento real del evento).</summary>
    public DateTime TimestampUtc { get; } = timestampUtc;

    /// <summary>Secuencia incremental por-request para desempate.</summary>
    public int Sequence { get; } = sequence;

    /// <summary>Contenido ya formateado que se escribirá tal cual en el archivo.</summary>
    public string Content { get; } = content;

    /// <summary>Fábrica que usa DateTime.UtcNow para el sello temporal.</summary>
    public static DynamicLogSegment Create(DynamicLogKind kind, int seq, string content)
        => new(kind, DateTime.UtcNow, seq, content);
}

namespace Logging.Ordering;

/// <summary>
/// Tipos de eventos dinámicos que ocurren durante el ciclo de vida del request.
/// Se registran SIEMPRE entre Request Info y Response Info.
/// </summary>
public enum DynamicLogKind
{
    /// <summary>Peticiones salientes por HttpClient u otros clientes HTTP.</summary>
    HttpClient = 1,

    /// <summary>Ejecución de comandos SQL (SELECT/DML/SP/CL/PGM).</summary>
    Sql = 2,

    /// <summary>Entradas manuales de texto (AddSingleLog).</summary>
    ManualSingle = 3,

    /// <summary>Entradas manuales de objeto/estructura (AddObjLog).</summary>
    ManualObject = 4,

    /// <summary>Bloques de log agregados con StartLogBlock/Add/End.</summary>
    ManualBlock = 5,

    /// <summary>Reservado para extensiones personalizadas.</summary>
    Custom = 99
}

using Logging.Abstractions;
using Logging.Attributes;
using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Logging.Middleware;


/// <summary>
/// Middleware que captura Environment (2), Request (4), bloques dinámicos ordenados (HTTP/SQL/…)
/// y Response (5). Mantiene compatibilidad con Items existentes y no rompe integraciones.
/// </summary>
public class LoggingMiddleware(RequestDelegate next, ILoggingService loggingService)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILoggingService _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

    /// <summary>
    /// Cronómetro por-request para medir tiempo total.
    /// </summary>
    private Stopwatch _stopwatch = new();

    /// <summary>
    /// Intercepta la request, escribe bloques fijos y agrega los dinámicos en orden por INICIO.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Filtrado básico (swagger/favicon).
            var path = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(path) &&
                (path.Contains("swagger", StringComparison.OrdinalIgnoreCase) ||
                 path.Contains("favicon.ico", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            _stopwatch = Stopwatch.StartNew();

            // ExecutionId por-request para correlación.
            if (!context.Items.ContainsKey("ExecutionId")) context.Items["ExecutionId"] = Guid.NewGuid().ToString();

            // Intento temprano de extraer LogCustomPart (DTO/Query).
            await ExtractLogCustomPartFromBody(context);

            // (2) Environment Info — bloque fijo (se escribe en el archivo ahora).
            _loggingService.WriteLog(context, await CaptureEnvironmentInfoAsync(context));

            // (4) Request Info — bloque fijo (se escribe en el archivo ahora).
            _loggingService.WriteLog(context, await CaptureRequestInfoAsync(context));

            // Interceptar body de respuesta en memoria para no iniciar headers todavía.
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Ejecutar el pipeline.
            await _next(context);

            // === BLOQUES DINÁMICOS ORDENADOS (entre 4 y 5) ===
            // 1) Lista “timed” (preferida: trae TsUtc/Content para ordenar)
            if (context.Items.TryGetValue("HttpClientLogsTimed", out var timedObj) &&
                timedObj is List<object> timedList && timedList.Count > 0)
            {
                // Ordena primero por TsUtc (instante real de inicio) y luego por Seq (desempate estable).
                var ordered = timedList
                        .Select(o =>
                        {
                            var t = o.GetType();
                            var ts = t.GetProperty("TsUtc")?.GetValue(o);
                            var sq = t.GetProperty("Seq")?.GetValue(o);
                            var tx = t.GetProperty("Content")?.GetValue(o);

                            DateTime tsUtc = ts is DateTime d ? d : DateTime.UtcNow; // fallback
                            long seq = sq is long l ? l : long.MaxValue;             // legacy sin Seq va al final en empates
                            string content = tx as string ?? string.Empty;

                            return new { Ts = tsUtc, Seq = seq, Tx = content };
                        })
                        .OrderBy(x => x.Ts)    // primero por inicio real
                        .ThenBy(x => x.Seq)    // luego por secuencia estable
                        .ToList();

                foreach (var e in ordered)
                    _loggingService.WriteLog(context, e.Tx);

                context.Items.Remove("HttpClientLogsTimed");
            }
            else
            {
                // 2) Fallback: lista antigua (solo strings, sin timestamp)
                if (context.Items.TryGetValue("HttpClientLogs", out var clientLogsObj) &&
                    clientLogsObj is List<string> clientLogs && clientLogs.Count > 0)
                {
                    foreach (var log in clientLogs)
                        _loggingService.WriteLog(context, log);
                }
            }

            // (5) Response Info — bloque fijo (todavía no hemos enviado headers).
            _loggingService.WriteLog(context, await CaptureResponseInfoAsync(context));

            // Restaurar el stream original y enviar realmente la respuesta al cliente.
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            // Si se acumuló alguna Exception en Items, persiste su detalle.
            if (context.Items.ContainsKey("Exception") && context.Items["Exception"] is Exception ex)
                _loggingService.AddExceptionLog(ex);
        }
        catch (Exception ex)
        {
            _loggingService.AddExceptionLog(ex); // El logging no debe romper el request
        }
        finally
        {
            _stopwatch.Stop();

            // Registro final: tiempo total. En este punto, lo usual es que HasStarted sea true,
            // por lo que WriteLog añadirá el Fin de Log (7) junto con esta línea.
            _loggingService.WriteLog(context, $"[Tiempo Total de Ejecución]: {_stopwatch.ElapsedMilliseconds} ms");
        }
    }

    /// <summary>
    /// Extrae un valor “custom” para el nombre del archivo de log desde el DTO
    /// o desde Query/Route (GET). Lo deja en Items["LogCustomPart"] si existe.
    /// </summary>
    private static async Task ExtractLogCustomPartFromBody(HttpContext context)
    {
        string? bodyString = null;

        // Soporte JSON de entrada: habilita rebobinado (buffering) para no interferir con el pipeline.
        if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            bodyString = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        try
        {
            var customPart = StrongTypedLogFileNameExtractor.Extract(context, bodyString);
            if (!string.IsNullOrWhiteSpace(customPart))
                context.Items["LogCustomPart"] = customPart;
        }
        catch
        {
            // La extracción no debe interrumpir el request.
        }
    }

    /// <summary>
    /// Busca recursivamente en un objeto cualquier propiedad marcada con [LogFileName].
    /// </summary>
    private static string? GetLogFileNameValue(object? obj)
    {
        if (obj is null) return null;

        var type = obj.GetType();
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal)) return null;

        // Búsqueda directa en propiedades marcadas
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(prop => Attribute.IsDefined(prop, typeof(LogFileNameAttribute))))
        {
            var value = prop.GetValue(obj)?.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        // Búsqueda en propiedades anidadas
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(obj);
            var nested = GetLogFileNameValue(value);
            if (!string.IsNullOrWhiteSpace(nested)) return nested;
        }

        return null;
    }

    /// <summary>
    /// Construye el bloque “Environment Info (2)”.
    /// </summary>
    private static async Task<string> CaptureEnvironmentInfoAsync(HttpContext context)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1)); // mantener firma async sin bloquear

        var request = context.Request;
        var connection = context.Connection;
        var hostEnvironment = context.RequestServices.GetService<IHostEnvironment>();

        // Origen de "distribution" preferente: Header → Claim → Subdominio.
        var distributionFromHeader = context.Request.Headers["Distribucion"].FirstOrDefault();
        var distributionFromClaim = context.User?.Claims?.FirstOrDefault(c => c.Type == "distribution")?.Value;
        var host = context.Request.Host.Host;
        var distributionFromSubdomain = !string.IsNullOrWhiteSpace(host) && host.Contains('.')
            ? host.Split('.')[0]
            : null;

        var distribution = distributionFromHeader ?? distributionFromClaim ?? distributionFromSubdomain ?? "N/A";

        // Metadatos de host
        string application = hostEnvironment?.ApplicationName ?? "Desconocido";
        string env = hostEnvironment?.EnvironmentName ?? "Desconocido";
        string contentRoot = hostEnvironment?.ContentRootPath ?? "Desconocido";
        string executionId = context.TraceIdentifier ?? "Desconocido";
        string clientIp = connection?.RemoteIpAddress?.ToString() ?? "Desconocido";
        string userAgent = request.Headers.UserAgent.ToString() ?? "Desconocido";
        string machineName = Environment.MachineName;
        string os = Environment.OSVersion.ToString();
        var fullHost = request.Host.ToString() ?? "Desconocido";

        // Extras compactos
        var extras = new Dictionary<string, string>
        {
            { "Scheme", request.Scheme },
            { "Protocol", request.Protocol },
            { "Method", request.Method },
            { "Path", request.Path },
            { "Query", request.QueryString.ToString() },
            { "ContentType", request.ContentType ?? "N/A" },
            { "ContentLength", request.ContentLength?.ToString() ?? "N/A" },
            { "ClientPort", connection?.RemotePort.ToString() ?? "Desconocido" },
            { "LocalIp", connection?.LocalIpAddress?.ToString() ?? "Desconocido" },
            { "LocalPort", connection?.LocalPort.ToString() ?? "Desconocido" },
            { "ConnectionId", connection?.Id ?? "Desconocido" },
            { "Referer", request.Headers.Referer.ToString() ?? "N/A" }
        };

        return LogFormatter.FormatEnvironmentInfo(
            application: application,
            env: env,
            contentRoot: contentRoot,
            executionId: executionId,
            clientIp: clientIp,
            userAgent: userAgent,
            machineName: machineName,
            os: os,
            host: fullHost,
            distribution: distribution,
            extras: extras
        );
    }

    /// <summary>
    /// Construye el bloque “Request Info (4)”.
    /// </summary>
    private static async Task<string> CaptureRequestInfoAsync(HttpContext context)
    {
        context.Request.EnableBuffering(); // lectura sin consumir el stream

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        // Extrae y deja un posible valor para el nombre del archivo.
        var customPart = LogFileNameExtractor.ExtractLogFileNameFromContext(context, body);
        if (!string.IsNullOrWhiteSpace(customPart))
            context.Items["LogCustomPart"] = customPart;

        return LogFormatter.FormatRequestInfo(context,
            method: context.Request.Method,
            path: context.Request.Path,
            queryParams: context.Request.QueryString.ToString(),
            body: body
        );
    }

    /// <summary>
    /// Construye el bloque “Response Info (5)” sin forzar el envío de headers.
    /// </summary>
    private static async Task<string> CaptureResponseInfoAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        string formattedResponse;

        if (context.Items.ContainsKey("ResponseObject"))
        {
            var responseObject = context.Items["ResponseObject"];
            formattedResponse = LogFormatter.FormatResponseInfo(context,
                statusCode: context.Response.StatusCode.ToString(),
                headers: string.Join("; ", context.Response.Headers),
                body: responseObject is not null
                    ? JsonSerializer.Serialize(responseObject, JsonHelper.PrettyPrintCamelCase)
                    : "null"
            );
        }
        else
        {
            formattedResponse = LogFormatter.FormatResponseInfo(context,
                statusCode: context.Response.StatusCode.ToString(),
                headers: string.Join("; ", context.Response.Headers),
                body: body
            );
        }

        return formattedResponse;
    }
}

Me indicas si requieres otra clase para el analisis.


