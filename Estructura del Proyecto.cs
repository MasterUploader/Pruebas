Sigue sin colocar en orden el log, te muestro todo el código tal como lo tengo actualmente, por favor analiza el motivo del porque el SQL sigue colocandose antes del HTTP, para el caso que estoy probando donde primero se ejecutan 2 consultas HTTP, y luego una SQL.

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



using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using static Logging.Helpers.LogHelper;

namespace Logging.Handlers;

/// <summary>
/// DelegatingHandler que registra HTTP saliente con compatibilidad total:
/// - Si hay buffer por-request: encola entre 4 y 5, ordenado por INICIO.
/// - Si no hay buffer: acumula en Items (legacy) para que el middleware lo escriba,
///   y además guarda variante "timed" para permitir orden cuando se use Flush.
/// </summary>
public sealed class HttpClientLoggingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    private const int MaxBodyChars = 20000; // Límite de seguridad para cuerpos grandes

    /// <summary>
    /// Intercepta la petición/respuesta, arma el bloque y lo publica por buffer o por Items.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _accessor.HttpContext; // Puede ser null fuera del pipeline
        var traceId = ctx?.TraceIdentifier ?? Guid.NewGuid().ToString();

        // Sello del INICIO del request HTTP — fundamental para el orden correcto.
        var startedUtc = DateTime.UtcNow;

        // ===== Preparar datos de la petición =====
        string? requestBody = null;
        if (request.Content is not null)
        {
            try
            {
                var raw = await request.Content.ReadAsStringAsync(cancellationToken);
                requestBody = LogHelper.PrettyPrintAuto(Limit(raw, MaxBodyChars), request.Content.Headers.ContentType?.MediaType);
            }
            catch
            {
                // Cuerpos no re-leibles o streams cerrados no deben romper la llamada real.
                requestBody = "[No disponible para logging]";
            }
        }

        var requestHeaders = RenderHeaders(request.Headers);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            // ===== Preparar datos de la respuesta =====
            string responseBody;
            try
            {
                var raw = response.Content is null
                    ? "[Sin contenido]"
                    : await response.Content.ReadAsStringAsync(cancellationToken);

                responseBody = LogHelper.PrettyPrintAuto(Limit(raw, MaxBodyChars), response.Content?.Headers?.ContentType?.MediaType);
            }
            catch
            {
                responseBody = "[No disponible para logging]";
            }

            var responseHeaders = RenderHeaders(response.Headers);

            // ===== Construir bloque final (request + response + métrica) =====
            var block = new StringBuilder(capacity: 2048)
                .AppendLine("============== INICIO HTTP CLIENT ==============")
                .Append("TraceId        : ").AppendLine(traceId)
                .Append("Fecha/Hora     : ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append("Método         : ").AppendLine(request.Method.Method)
                .Append("URL            : ").AppendLine(request.RequestUri?.ToString() ?? "URI no definida")
                .AppendLine("---- Request Headers ----")
                .AppendLine(requestHeaders)
                .AppendLine("---- Request Body ----")
                .AppendLine(string.IsNullOrWhiteSpace(requestBody) ? "[Sin cuerpo]" : requestBody)
                .AppendLine("---- Response ----")
                .Append("Status Code    : ").Append((int)response.StatusCode).Append(' ').AppendLine(response.StatusCode.ToString())
                .AppendLine("---- Response Headers ----")
                .AppendLine(responseHeaders)
                .AppendLine("---- Response Body ----")
                .AppendLine(responseBody)
                .Append("Duración (ms)  : ").AppendLine(sw.ElapsedMilliseconds.ToString())
                .AppendLine("=============== FIN HTTP CLIENT ================")
                .AppendLine()
                .ToString();

            // Publicación con compatibilidad (buffer o Items).
            LogHelper.AppendDynamicCompatAt(ctx, filePath: null, DynamicLogKind.HttpClient, block, startedUtc);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            // Bloque de error con contexto de la petición.
            var errorBlock = new StringBuilder(capacity: 1024)
                .AppendLine("============== INICIO HTTP CLIENT (ERROR) ==============")
                .Append("TraceId        : ").AppendLine(traceId)
                .Append("Fecha/Hora     : ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append("Método         : ").AppendLine(request.Method.Method)
                .Append("URL            : ").AppendLine(request.RequestUri?.ToString() ?? "URI no definida")
                .AppendLine("---- Request Headers ----")
                .AppendLine(requestHeaders)
                .AppendLine("---- Request Body ----")
                .AppendLine(string.IsNullOrWhiteSpace(requestBody) ? "[Sin cuerpo]" : requestBody)
                .Append("Duración (ms)  : ").AppendLine(sw.ElapsedMilliseconds.ToString())
                .AppendLine("Excepción:")
                .AppendLine(ex.ToString())
                .AppendLine("=============== FIN HTTP CLIENT (ERROR) ================")
                .AppendLine()
                .ToString();

            LogHelper.AppendDynamicCompatAt(ctx, filePath: null, DynamicLogKind.HttpClient, errorBlock, startedUtc);
            throw;
        }
    }

    /// <summary>
    /// Redacta cabeceras sensibles y produce una representación legible para el log.
    /// </summary>
    private static string RenderHeaders(HttpHeaders headers)
    {
        var sb = new StringBuilder(capacity: 512);
        foreach (var h in headers)
        {
            var key = h.Key;
            var value = string.Join(",", h.Value);

            // Redacción de secretos: evita exponer tokens.
            if (key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase))
                value = "[REDACTED]";

            sb.Append(key).Append(": ").AppendLine(value);
        }
        return sb.ToString();
    }

    /// <summary>Limita tamaño de texto para evitar logs desmesurados.</summary>
    private static string Limit(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Length <= maxChars) return text;
        return text[..maxChars] + "… [truncado]";
    }
}




using Logging.Extensions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace Logging.Helpers;

/// <summary>
/// Clase estática encargada de formatear los logs con la estructura pre definida.
/// </summary>
public static class LogFormatter
{
    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Formato de Log para FormatBeginLog.
    /// </summary>
    /// <returns>Un string con el formato de Log para FormatBeginLog.</returns>
    public static string FormatBeginLog()
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Inicio de Log-------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formato de Log para FormatEndLog.
    /// </summary>
    /// <returns>Un string con el formato de Log para FormatEndLog.</returns>
    public static string FormatEndLog()
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Fin de Log-------------------------");
        sb.AppendLine($"Final: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();

    }

    /// <summary>
    /// Formatea la información del entorno, incluyendo datos adicionales si están disponibles.
    /// </summary>
    /// <param name="application">Nombre de la aplicación.</param>
    /// <param name="env">Nombre del entorno (Development, Production, etc.).</param>
    /// <param name="contentRoot">Ruta raíz del contenido.</param>
    /// <param name="executionId">Identificador único de la ejecución.</param>
    /// <param name="clientIp">Dirección IP del cliente.</param>
    /// <param name="userAgent">Agente de usuario del cliente.</param>
    /// <param name="machineName">Nombre de la máquina donde corre la aplicación.</param>
    /// <param name="os">Sistema operativo del servidor.</param>
    /// <param name="host">Host del request recibido.</param>
    /// <param name="distribution">Distribución personalizada u origen (opcional).</param>
    /// <param name="extras">Diccionario con información adicional opcional.</param>
    /// <returns>Texto formateado con la información del entorno.</returns>
    public static string FormatEnvironmentInfo(
        string application, string env, string contentRoot, string executionId,
        string clientIp, string userAgent, string machineName, string os,
        string host, string distribution, Dictionary<string, string>? extras = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Enviroment Info-------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");
        sb.AppendLine($"Application: {application}");
        sb.AppendLine($"Environment: {env}");
        sb.AppendLine($"ContentRoot: {contentRoot}");
        sb.AppendLine($"Execution ID: {executionId}");
        sb.AppendLine($"Client IP: {clientIp}");
        sb.AppendLine($"User Agent: {userAgent}");
        sb.AppendLine($"Machine Name: {machineName}");
        sb.AppendLine($"OS: {os}");
        sb.AppendLine($"Host: {host}");
        sb.AppendLine($"Distribución: {distribution}");

        if (extras is not null && extras.Count != 0)
        {
            sb.AppendLine("  -- Extras del HttpContext --");
            foreach (var kvp in extras)
            {
                sb.AppendLine($"    {kvp.Key,-20}: {kvp.Value}");
            }
        }

        sb.AppendLine(new string('-', 70));
        sb.AppendLine("---------------------------Enviroment Info-------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formatea los parámetros de entrada de un método antes de guardarlos en el log.
    /// </summary>
    public static string FormatInputParameters(IDictionary<string, object> parameters)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-----------------------Parámetros de Entrada-----------------------------------");

        if (parameters == null || parameters.Count == 0)
        {
            sb.AppendLine("Sin parámetros de entrada.");
        }
        else
        {
            foreach (var param in parameters)
            {
                if (param.Value == null)
                {
                    sb.AppendLine($"{param.Key} = null");
                }
                else if (param.Value.GetType().IsPrimitive || param.Value is string)
                {
                    sb.AppendLine($"{param.Key} = {param.Value}");
                }
                else
                {
                    string json = JsonSerializer.Serialize(param.Value, s_writeOptions);
                    sb.AppendLine($"Objeto {param.Key} =\n{json}");
                }
            }
        }

        sb.AppendLine("-----------------------Parámetros de Entrada-----------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formato de Log para Request.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición.</param>
    /// <param name="method">Endpoint.</param>
    /// <param name="path">Ruta del Endpoint.</param>
    /// <param name="queryParams">Parametros del Query.</param>
    /// <param name="body">Cuerpo de la petición.</param>
    /// <returns>uString con el Log Formateado.</returns>
    public static string FormatRequestInfo(HttpContext context, string method, string path, string queryParams, string body)
    {
        string formattedJson = string.IsNullOrWhiteSpace(body) ? "  (Sin cuerpo en la solicitud)" : StringExtensions.FormatJson(body, 30); // Aplica indentación controlada
        var routeData = context.GetRouteData();
        string controllerName = routeData?.Values["controller"]?.ToString() ?? "Desconocido";
        string actionName = routeData?.Values["action"]?.ToString() ?? "Desconocido";

        var sb = new StringBuilder();

        sb.AppendLine(FormatControllerBegin(controllerName, actionName));
        sb.AppendLine("----------------------------------Request Info---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Método: {method}");
        sb.AppendLine($"URL: {path}{queryParams}");
        sb.AppendLine($"Cuerpo:");
        sb.AppendLine($"{formattedJson}");
        sb.AppendLine("----------------------------------Request Info---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formato de la información de Respuesta.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición.</param>
    /// <param name="statusCode">Codigo de Estádo de la respuesta.</param>
    /// <param name="headers">Cabeceras de la respuesta.</param>
    /// <param name="body">Cuerpo de la Respuesta.</param>
    /// <returns>String con el Log Formateado.</returns>
    public static string FormatResponseInfo(HttpContext context, string statusCode, string headers, string body)
    {
        string formattedJson = string.IsNullOrWhiteSpace(body) ? "        (Sin cuerpo en la respuesta)" : StringExtensions.FormatJson(body, 30); // Aplica indentación controlada
        var routeData = context.GetRouteData();
        string controllerName = routeData?.Values["controller"]?.ToString() ?? "Desconocido";
        string actionName = routeData?.Values["action"]?.ToString() ?? "Desconocido";

        var sb = new StringBuilder();

        sb.AppendLine("----------------------------------Response Info---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Código Estado: {statusCode}");
        sb.AppendLine($"Headers: {headers}");
        sb.AppendLine($"Cuerpo:");
        sb.AppendLine($"{formattedJson}");
        sb.AppendLine("----------------------------------Response Info---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine(FormatControllerEnd(controllerName, actionName));

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el Inicio del Log del Controlador.
    /// </summary>
    /// <param name="controllerName">Nombre del controlador.</param>
    /// <param name="actionName">Tipo de Acción.</param>
    /// <returns>String con el log formateado.</returns>
    private static string FormatControllerBegin(string controllerName, string actionName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Controlador: {controllerName}");
        sb.AppendLine($"Action: {actionName}");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el fin del Log del Controlador.
    /// </summary>
    /// <param name="controllerName">Nombre del Controlador.</param>
    /// <param name="actionName">Tipo de Acción.</param>
    /// <returns>String con el log formateado.</returns>
    private static string FormatControllerEnd(string controllerName, string actionName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Controlador: {controllerName}");
        sb.AppendLine($"Action: {actionName}");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea la estructura de inicio un método.
    /// </summary>
    /// <param name="methodName">Nombre del Método.</param>
    /// <param name="parameters">Parametros del metodo.</param>
    /// <returns>String con el log formateado.</returns>
    public static string FormatMethodEntry(string methodName, string parameters)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Método: {methodName}");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine("Parámetros de Entrada:");
        sb.AppendLine($"{parameters}");

        return sb.ToString();

    }

    /// <summary>
    /// Método que formatea la estructura de salida de un método.
    /// </summary>
    /// <param name="methodName">Nombre del Método.</param>
    /// <param name="returnValue">Valores de Retorno.</param>
    /// <returns>String con el Log Formateado.</returns>
    public static string FormatMethodExit(string methodName, string returnValue)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Método: {methodName}");
        sb.AppendLine($"Valores de Retorno: {returnValue}");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();

    }

    /// <summary>
    /// Método de formatea un Log Simple.
    /// </summary>
    /// <param name="message">Cuerpo del texto del Log.</param>
    /// <returns>String con el Log formateado.</returns>
    public static string FormatSingleLog(string message)
    {
        var sb = new StringBuilder();

        sb.AppendLine("----------------------------------Single Log-----------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"{message}");
        sb.AppendLine("----------------------------------Single Log-----------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el Log de un Objeto
    /// </summary>
    /// <param name="objectName">Nombre del Objeto.</param>
    /// <param name="obj">Objeto.</param>
    /// <returns>String con el log formateado.</returns>
    public static string FormatObjectLog(string objectName, object obj)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"---------------------- Object -> {objectName}---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"{StringExtensions.ConvertObjectToString(obj)}");
        sb.AppendLine($"---------------------- Object -> {objectName}---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el Log de una Excepción.
    /// </summary>
    /// <param name="exceptionMessage">Mensaje de la Excepción.</param>
    /// <returns>String con el log formateado.</returns>
    public static string FormatExceptionDetails(string exceptionMessage)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-----------------------------Exception Details---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"{exceptionMessage}");
        sb.AppendLine("-----------------------------Exception Details---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();

    }

    /// <summary>
    /// Formatea la información de una solicitud HTTP externa realizada mediante HttpClient.
    /// </summary>
    public static string FormatHttpClientRequest(
    string traceId,
    string method,
    string url,
    string statusCode,
    long elapsedMs,
    string headers,
    string? body,
    string? responseBody // <-- nuevo
)
    {
        var builder = new StringBuilder();
        builder.AppendLine("============= INICIO HTTP CLIENT =============");
        builder.AppendLine($"TraceId       : {traceId}");
        builder.AppendLine($"Método        : {method}");
        builder.AppendLine($"URL           : {url}");
        builder.AppendLine($"Código Status : {statusCode}");
        builder.AppendLine($"Duración (ms) : {elapsedMs}");
        builder.AppendLine($"Encabezados   :\n{headers}");

        if (!string.IsNullOrWhiteSpace(body))
        {
            builder.AppendLine("Cuerpo:");
            builder.AppendLine(body);
        }

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            builder.AppendLine("Respuesta:");
            builder.AppendLine(responseBody);
        }

        builder.AppendLine("============= FIN HTTP CLIENT =============");
        return builder.ToString();
    }

    /// <summary>
    /// Formatea la información de error ocurrida durante una solicitud con HttpClient.
    /// </summary>
    public static string FormatHttpClientError(
        string traceId,
        string method,
        string url,
        Exception exception)
    {
        var builder = new StringBuilder();

        builder.AppendLine();
        builder.AppendLine("======= ERROR HTTP CLIENT =======");
        builder.AppendLine($"TraceId     : {traceId}");
        builder.AppendLine($"Método      : {method}");
        builder.AppendLine($"URL         : {url}");
        builder.AppendLine($"Excepción   : {exception.Message}");
        builder.AppendLine($"StackTrace  : {exception.StackTrace}");
        builder.AppendLine("=================================");

        return builder.ToString();
    }

    /// <summary>
    /// Formatea un log detallado de una ejecución de base de datos exitosa.
    /// Incluye motor, servidor, base de datos, comando SQL y parámetros.
    /// </summary>
    /// <param name="command">Comando ejecutado (DbCommand).</param>
    /// <param name="elapsedMs">Milisegundos que tomó la ejecución.</param>
    /// <param name="context">Contexto HTTP opcional para enlazar trazabilidad.</param>
    /// <param name="customMessage">Mensaje adicional que puede incluir el log.</param>
    /// <returns>Cadena formateada para log de éxito en base de datos.</returns>
    public static string FormatDatabaseSuccess(DbCommand command, long elapsedMs, HttpContext? context = null, string? customMessage = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("📘 [Base de Datos] Consulta ejecutada exitosamente:");
        sb.AppendLine($"→ Motor: {command.Connection?.GetType().Name ?? "Desconocido"}");
        sb.AppendLine($"→ Servidor: {command.Connection?.DataSource ?? "Desconocido"}");
        sb.AppendLine($"→ Base de Datos: {command.Connection?.Database ?? "Desconocido"}");
        sb.AppendLine($"→ Tipo de Comando: {command.CommandType}");
        sb.AppendLine($"→ Texto SQL: {command.CommandText}");

        if (command.Parameters.Count > 0)
        {
            sb.AppendLine("→ Parámetros:");
            foreach (DbParameter param in command.Parameters)
            {
                sb.AppendLine($"   • {param.ParameterName} = {param.Value} ({param.DbType})");
            }
        }

        sb.AppendLine($"→ Tiempo de ejecución: {elapsedMs} ms");

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            sb.AppendLine($"→ Mensaje adicional: {customMessage}");
        }

        if (context != null)
        {
            sb.AppendLine($"→ TraceId: {context.TraceIdentifier}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formatea un log detallado de un error durante la ejecución de una consulta a base de datos.
    /// Incluye información del motor, SQL ejecutado y excepción.
    /// </summary>
    /// <param name="command">Comando que produjo el error.</param>
    /// <param name="exception">Excepción generada.</param>
    /// <param name="context">Contexto HTTP opcional.</param>
    /// <returns>Cadena formateada para log de error en base de datos.</returns>
    public static string FormatDatabaseError(DbCommand command, Exception exception, HttpContext? context = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("❌ [Base de Datos] Error en la ejecución de una consulta:");
        sb.AppendLine($"→ Motor: {command.Connection?.GetType().Name ?? "Desconocido"}");
        sb.AppendLine($"→ Servidor: {command.Connection?.DataSource ?? "Desconocido"}");
        sb.AppendLine($"→ Base de Datos: {command.Connection?.Database ?? "Desconocido"}");
        sb.AppendLine($"→ Tipo de Comando: {command.CommandType}");
        sb.AppendLine($"→ Texto SQL: {command.CommandText}");

        if (command.Parameters.Count > 0)
        {
            sb.AppendLine("→ Parámetros:");
            foreach (DbParameter param in command.Parameters)
            {
                sb.AppendLine($"   • {param.ParameterName} = {param.Value} ({param.DbType})");
            }
        }

        sb.AppendLine($"→ Excepción: {exception.GetType().Name} - {exception.Message}");

        if (context != null)
        {
            sb.AppendLine($"→ TraceId: {context.TraceIdentifier}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Da formato al log estructurado de una ejecución SQL para fines de almacenamiento en log de texto plano.
    /// </summary>
    /// <param name="model">Modelo de log SQL estructurado.</param>
    /// <returns>Cadena con formato estándar para logging de SQL.</returns>
    public static string FormatDbExecution(SqlLogModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("====================== INICIO LOG DE EJECUCIÓN SQL ======================");
        sb.AppendLine($"Fecha y Hora      : {model.StartTime:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Duración          : {model.Duration.TotalMilliseconds} ms");
        sb.AppendLine($"Base de Datos     : {model.DatabaseName}");
        sb.AppendLine($"IP                : {model.Ip}");
        sb.AppendLine($"Puerto            : {model.Port}");
        sb.AppendLine($"Esquema           : {model.Schema}");
        sb.AppendLine($"Tabla             : {model.TableName}");
        sb.AppendLine($"Veces Ejecutado   : {model.ExecutionCount}");
        sb.AppendLine($"Filas Afectadas   : {model.TotalAffectedRows}");
        sb.AppendLine("SQL:");
        sb.AppendLine(model.Sql);
        sb.AppendLine("====================== FIN LOG DE EJECUCIÓN SQL ======================");

        return sb.ToString();
    }

    /// <summary>
    /// Formatea un bloque de log para errores en ejecución SQL, incluyendo contexto y detalles de excepción.
    /// </summary>
    /// <param name="nombreBD">Nombre de la base de datos.</param>
    /// <param name="ip">IP del servidor de base de datos.</param>
    /// <param name="puerto">Puerto utilizado en la conexión.</param>
    /// <param name="biblioteca">Biblioteca o esquema objetivo.</param>
    /// <param name="tabla">Tabla afectada por la operación fallida.</param>
    /// <param name="sentenciaSQL">Sentencia SQL que generó el error.</param>
    /// <param name="exception">Excepción lanzada por el proveedor de datos.</param>
    /// <param name="horaError">Hora en la que ocurrió el error.</param>
    /// <returns>Texto formateado para almacenar como log de error estructurado.</returns>
    public static string FormatDbExecutionError(
        string nombreBD,
        string ip,
        int puerto,
        string biblioteca,
        string tabla,
        string sentenciaSQL,
        Exception exception,
        DateTime horaError)
    {
        var sb = new StringBuilder();

        sb.AppendLine("============= DB ERROR =============");
        sb.AppendLine($"Nombre BD: {nombreBD}");
        sb.AppendLine($"IP: {ip}");
        sb.AppendLine($"Puerto: {puerto}");
        sb.AppendLine($"Biblioteca: {biblioteca}");
        sb.AppendLine($"Tabla: {tabla}");
        sb.AppendLine($"Hora del error: {horaError:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("Sentencia SQL:");
        sb.AppendLine(sentenciaSQL);
        sb.AppendLine();
        sb.AppendLine("Excepción:");
        sb.AppendLine(exception.Message);
        sb.AppendLine("StackTrace:");
        sb.AppendLine(exception.StackTrace ?? "Sin detalles de stack.");
        sb.AppendLine("============= END DB ERROR ===================");

        return sb.ToString();
    }

    /// <summary>
    /// Construye el texto de cabecera para un bloque de log.
    /// </summary>
    public static string BuildBlockHeader(string title)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sb = new StringBuilder();
        sb.AppendLine($"======================== [BEGIN BLOCK] ========================");
        sb.AppendLine($"Título     : {title}");
        sb.AppendLine($"Inicio     : {now}");
        sb.AppendLine($"===============================================================");
        sb.AppendLine("");
        return sb.ToString();
    }

    /// <summary>
    /// Construye el texto de cierre para un bloque de log.
    /// </summary>
    public static string BuildBlockFooter()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sb = new StringBuilder();
        sb.AppendLine($"===============================================================");
        sb.AppendLine($"Fin        : {now}");
        sb.AppendLine($"========================= [END BLOCK] =========================");
        sb.AppendLine("");
        return sb.ToString();
    }
}


using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Logging.Helpers;

/// <summary>
/// Proporciona métodos auxiliares para la gestión y almacenamiento de logs en archivos.
/// </summary>
public static partial class LogHelper
{
    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Escribe un log en un archivo, asegurando que no interrumpa la ejecución si ocurre un error.
    /// </summary>
    /// <param name="logDirectory">Directorio donde se almacenará el archivo de log.</param>
    /// <param name="fileName">Nombre del archivo de log.</param>
    /// <param name="logContent">Contenido del log a escribir.</param>
    public static void WriteLogToFile(string logDirectory, string filePath, string logContent)
    {
        try
        {
            // Asegura que la carpeta del archivo exista
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(fileStream, Encoding.UTF8);
            writer.Write(logContent);
        }
        catch (Exception ex)
        {
            LogInternalError(logDirectory, ex);
        }
    }

    /// <summary>
    /// Escribe un log en un archivo, asegurando que no interrumpa la ejecución si ocurre un error.
    /// </summary>
    /// <param name="logDirectory">Directorio donde se almacenará el archivo de log.</param>
    /// <param name="fileName">Nombre del archivo de log.</param>
    /// <param name="logContent">Contenido del log a escribir.</param>
    public static void WriteLogToFile2(string logDirectory, string fileName, string logContent)
    {
        try
        {
            // Asegura que el directorio de logs exista
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Define la ruta completa del archivo de log
            string logFilePath = Path.Combine(logDirectory, fileName);

            //Usamos FileStream con FileShare para permitir accesos concurrentes
            using var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(fileStream, Encoding.UTF8);
            writer.Write(logContent);

        }
        catch (Exception ex)
        {
            // En caso de error, guarda un log interno para depuración
            LogInternalError(logDirectory, ex);
        }
    }

    /// <summary>
    /// Registra un error interno en un archivo separado ("InternalErrorLog.txt") sin afectar la API.
    /// </summary>
    /// <param name="logDirectory">Directorio donde se almacenará el archivo de errores internos.</param>
    /// <param name="ex">Excepción capturada.</param>
    private static void LogInternalError(string logDirectory, Exception ex)
    {
        try
        {
            // Define la ruta del archivo de errores internos
            string errorLogPath = Path.Combine(logDirectory, "InternalErrorLog.txt");

            // Mensaje de error con timestamp
            string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error en LogHelper: {ex}{Environment.NewLine}";

            // Guarda el error sin interrumpir la ejecución de la API
            File.AppendAllText(errorLogPath, errorMessage);
        }
        catch
        {
            // Evita bucles de error si la escritura en el log interno también falla
        }
    }

    /// <summary>
    /// Guarda una entrada de log en formato CSV (una línea por log con campos separados por coma).
    /// Utiliza el mismo nombre base del archivo .txt pero con extensión .csv.
    /// </summary>
    /// <param name="logDirectory">Directorio donde se almacenan los logs.</param>
    /// <param name="txtFilePath">Ruta del archivo .txt original (para extraer nombre base).</param>
    /// <param name="logContent">Contenido del log a registrar en CSV.</param>
    public static void SaveLogAsCsv(string logDirectory, string txtFilePath, string logContent)
    {
        try
        {
            // Obtener el nombre base sin extensión (ej. "Log_trace123_Controller_20250408_150000")
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(txtFilePath);
            var csvFilePath = Path.Combine(Path.GetDirectoryName(txtFilePath) ?? logDirectory, fileNameWithoutExtension + ".csv");

            // Extraer los campos obligatorios para el CSV
            var traceId = fileNameWithoutExtension.Split('_').FirstOrDefault() ?? "Desconocido";
            var fecha = DateTime.Now.ToString("yyyy-MM-dd");
            var hora = DateTime.Now.ToString("HH:mm:ss");
            var apiName = AppDomain.CurrentDomain.FriendlyName;
            var endpoint = fileNameWithoutExtension.Contains('_') ? fileNameWithoutExtension.Split('_').Skip(1).FirstOrDefault() ?? "Desconocido" : "Desconocido";

            // Convertir el contenido del log en una sola línea
            string singleLineLog = ConvertLogToCsvLine(logContent);

            // Crear la línea CSV completa
            string csvLine = $"{traceId},{fecha},{hora},{apiName},{endpoint},\"{singleLineLog}\"";

            // Guardar en el archivo .csv
            WriteCsvLog(csvFilePath, csvLine);
        }
        catch
        {
            // Silenciar cualquier error para no afectar al API
        }
    }

    /// <summary>
    /// Escribe una línea en un archivo CSV. Si el archivo no existe, lo crea con cabecera.
    /// </summary>
    /// <param name="csvFilePath">Ruta del archivo CSV.</param>
    /// <param name="csvLine">Línea a escribir.</param>
    public static void WriteCsvLog(string csvFilePath, string csvLine)
    {
        try
        {
            var directory = Path.GetDirectoryName(csvFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bool fileExists = File.Exists(csvFilePath);
            using var writer = new StreamWriter(csvFilePath, append: true, encoding: Encoding.UTF8);

            if (!fileExists)
            {
                writer.WriteLine("TraceId,Fecha,Hora,ApiName,Endpoint,LogCompleto");
            }

            writer.WriteLine(csvLine);
        }
        catch
        {
            // Silenciar para no afectar la ejecución
        }
    }

    /// <summary>
    /// Escribe una línea en un archivo CSV. Si el archivo no existe, lo crea con cabecera.
    /// </summary>
    /// <param name="csvFilePath">Ruta del archivo CSV.</param>
    /// <param name="csvLine">Línea a escribir.</param>
    public static void WriteCsvLog2(string csvFilePath, string csvLine)
    {
        try
        {
            bool fileExists = File.Exists(csvFilePath);

            using var writer = new StreamWriter(csvFilePath, append: true, encoding: Encoding.UTF8);

            // Escribir cabecera si el archivo no existe
            if (!fileExists)
            {
                writer.WriteLine("TraceId,Fecha,Hora,ApiName,Endpoint,LogCompleto");
            }

            writer.WriteLine(csvLine);
        }
        catch
        {
            // Silenciar para no afectar la ejecución
        }
    }



    /// <summary>
    /// Convierte el contenido de un log multilinea a una sola línea, separando líneas con un símbolo (ej. '|').
    /// También escapa caracteres especiales para evitar errores en CSV.
    /// </summary>
    /// <param name="logContent">Contenido del log en texto plano.</param>
    /// <returns>Log transformado en una sola línea.</returns>
    private static string ConvertLogToCsvLine(string logContent)
    {
        if (string.IsNullOrWhiteSpace(logContent)) return "Sin contenido";

        return logContent
            .Replace("\r\n", "|")
            .Replace("\n", "|")
            .Replace("\r", "|")
            .Replace("\"", "'") // Escapar comillas dobles
            .Trim();
    }
    /// <summary>
    /// Devuelve el cuerpo formateado automáticamente como JSON o XML si es posible.
    /// </summary>
    /// <param name="body">El contenido de la respuesta.</param>
    /// <param name="contentType">El tipo de contenido (Content-Type).</param>
    public static string PrettyPrintAuto(string? body, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "[Sin contenido]";

        contentType = contentType?.ToLowerInvariant();

        try
        {
            if (contentType != null && contentType.Contains("json"))
                return PrettyPrintJson(body);

            if (contentType != null && (contentType.Contains("xml") || contentType.Contains("text/xml")))
                return PrettyPrintXml(body);

            return body;
        }
        catch
        {
            return body; // Si el formateo falla, devolver el cuerpo original
        }
    }

    /// <summary>
    /// Da formato legible a un string JSON.
    /// Si no es un JSON válido, devuelve el texto original.
    /// </summary>
    /// <param name="json">Contenido en formato JSON.</param>
    /// <returns>JSON formateado con sangrías o el texto original si falla el parseo.</returns>
    private static string PrettyPrintJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "[Sin contenido JSON]";

        try
        {
            using var jdoc = JsonDocument.Parse(json);
            var options = s_writeOptions;

            return JsonSerializer.Serialize(jdoc.RootElement, options);
        }
        catch
        {
            return json; // Si no es JSON válido, devolverlo sin cambios
        }
    }

    /// <summary>
    /// Mejora la estructura del XML para que no quede en una sola linea.
    /// </summary>
    /// <param name="xml">Xml sin formatear.</param>
    /// <returns>Devuelve XML fromateado.</returns>
    private static string PrettyPrintXml(string xml)
    {
        try
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml);

            var stringBuilder = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using (var writer = XmlWriter.Create(stringBuilder, settings))
            {
                doc.Save(writer);
            }

            return stringBuilder.ToString();
        }
        catch
        {
            // Si el XML es inválido o viene mal, lo devolvemos como está
            return xml;
        }
    }

    /// <summary>
    /// Extrae los datos de IP, puerto, base de datos y biblioteca desde una cadena de conexión.
    /// </summary>
    public static DbConnectionInfo ExtractDbConnectionInfo(string? connectionString)
    {
        var info = new DbConnectionInfo();

        if (string.IsNullOrWhiteSpace(connectionString))
            return info;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim().ToLowerInvariant();
            var value = kv[1].Trim();

            switch (key)
            {
                case "data source":
                case "server":
                    if (value.Contains(':'))
                    {
                        var ipPort = value.Split(':');          // ip:puerto
                        info.Ip = ipPort[0];                    // IP siempre disponible

                        // ✔ Solo asigna puerto si el parseo fue exitoso
                        if (ipPort.Length > 1 &&
                            int.TryParse(ipPort[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                        {
                            info.Port = port;                   // Puerto válido detectado
                        }
                        // else: se mantiene el valor actual (por defecto 0) para evitar datos incorrectos
                    }
                    else
                    {
                        info.Ip = value;                        // Solo IP, sin puerto
                    }
                    break;

                case "port":
                    // ✔ Solo asigna si el valor es un entero válido
                    if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPort))
                    {
                        info.Port = parsedPort;
                    }
                    // else: ignora y conserva el valor actual (0 u otro previamente establecido)
                    break;

                case "initial catalog":
                case "database":
                    info.Database = value;
                    break;

                case "default collection":
                case "library":
                    info.Library = value;
                    break;
            }
        }

        return info;
    }

    /// <summary>
    /// Extrae el nombre de la tabla desde una sentencia SQL básica (INSERT, UPDATE, DELETE, SELECT).
    /// </summary>
    /// <param name="sql">Sentencia SQL.</param>
    /// <returns>Nombre de la tabla o "Desconocida".</returns>
    public static string ExtractTableName(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return "Desconocida";

        string lowerSql = sql.ToLowerInvariant();

        var patterns = new[]
        {
        @"insert\s+into\s+([a-zA-Z0-9_\.]+)",
        @"update\s+([a-zA-Z0-9_\.]+)",
        @"delete\s+from\s+([a-zA-Z0-9_\.]+)",
        @"from\s+([a-zA-Z0-9_\.]+)"
    };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(lowerSql, pattern);
            if (match.Success && match.Groups.Count > 1)
                return match.Groups[1].Value;
        }

        return "Desconocida";
    }

    /// <summary>
    /// Escribe contenido en un archivo de log `.txt`, eligiendo automáticamente
    /// entre escritura síncrona o asincrónica según el tamaño del contenido.
    /// Esto evita bloquear la ejecución de la API en logs grandes.
    /// </summary>
    /// <param name="directory">Directorio base donde se guardará el archivo.</param>
    /// <param name="filePath">Ruta completa del archivo de log .txt.</param>
    /// <param name="content">Contenido a escribir en el archivo.</param>
    /// <param name="forceAsyncThresholdBytes">
    /// Umbral en bytes a partir del cual se usará Task.Run para escritura asincrónica (por defecto 128 KB).
    /// </param>
    public static void SafeWriteLog(string directory, string filePath, string content, int forceAsyncThresholdBytes = 128 * 1024)
    {
        try
        {
            if (content.Length > forceAsyncThresholdBytes)
            {
                Task.Run(() => WriteLogToFile(directory, filePath, content));
            }
            else
            {
                WriteLogToFile(directory, filePath, content);
            }
        }
        catch
        {
            // Falla silenciosa para no interrumpir el flujo de ejecución principal
        }
    }


    /// <summary>
    /// Escribe contenido en el archivo de log `.csv`, eligiendo entre modo síncrono
    /// o asincrónico dependiendo del tamaño del contenido. Esta función es útil
    /// para garantizar rendimiento en logs muy extensos sin bloquear el hilo principal.
    /// </summary>
    /// <param name="directory">Directorio donde se guarda el archivo CSV.</param>
    /// <param name="logFilePath">Ruta base del archivo de log (de donde se deriva el nombre del .csv).</param>
    /// <param name="content">Contenido del log a escribir en una línea del archivo CSV.</param>
    /// <param name="forceAsyncThresholdBytes">
    /// Umbral en bytes a partir del cual se usará escritura asincrónica (por defecto 128 KB).
    /// </param>
    public static void SafeWriteCsv(string directory, string logFilePath, string content, int forceAsyncThresholdBytes = 128 * 1024)
    {
        try
        {
            if (content.Length > forceAsyncThresholdBytes)
            {
                Task.Run(() => SaveLogAsCsv(directory, logFilePath, content));
            }
            else
            {
                SaveLogAsCsv(directory, logFilePath, content);
            }
        }
        catch
        {
            // Silenciar errores de escritura en CSV para evitar interrupciones
        }
    }

    /// <summary>
    /// Guarda un log estructurado en un archivo de texto, utilizando el contexto HTTP si está disponible.
    /// </summary>
    /// <param name="formattedLog">El contenido del log ya formateado (por ejemplo, SQL estructurado, logs HTTP, etc.).</param>
    /// <param name="context">
    /// Opcional: contexto HTTP de la solicitud actual. Si se proporciona, se usará para nombrar el archivo de log con TraceId, endpoint, etc.
    /// </param>
    public static void SaveStructuredLog(string formattedLog, HttpContext? context)
    {
        try
        {
            // Obtener ruta del log dinámicamente
            var path = GetPathFromContext(context);

            // Asegurar que el directorio exista
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            // Guardar el log estructurado
            File.AppendAllText(path, formattedLog + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Temporal: manejo silencioso en caso de error de escritura
            Console.WriteLine($"[LogHelper Error] {ex.Message}");
        }
    }

    /// <summary>
    /// Construye la ruta dinámica para guardar logs basada en el contexto HTTP.
    /// Si no hay contexto, se genera una ruta genérica con timestamp.
    /// </summary>
    /// <param name="context">Contexto HTTP actual (puede ser null).</param>
    /// <returns>Ruta absoluta del archivo de log.</returns>
    private static string GetPathFromContext(HttpContext? context)
    {
        var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        if (context != null)
        {
            var traceId = context.TraceIdentifier;
            var endpoint = context.Request.Path.Value?.Trim('/').Replace("/", "_") ?? "endpoint";
            var date = DateTime.UtcNow.ToString("yyyyMMdd");

            var filename = $"{traceId}_{endpoint}_{date}.txt";
            return Path.Combine(basePath, filename);
        }

        // Sin contexto: log general
        var genericName = $"GeneralLog_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.txt";
        return Path.Combine(basePath, genericName);
    }
}


using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Text;

namespace Logging.Helpers;

/// <summary>
/// Extensión (partial) de LogHelper que agrega una "ventana dinámica" entre
/// 4) Request Info y 5) Response Info, y ordena eventos por el INICIO real.
/// Incluye compatibilidad: si no hay buffer por-request, se comporta como antes.
/// </summary>
public static partial class LogHelper
{
    // ===================== Infraestructura interna =====================

    /// <summary>
    /// Clave interna para guardar el buffer por-request en HttpContext.Items.
    /// </summary>
    private const string BufferKey = "__ReqLogBuffer";

    /// <summary>
    /// Claves legacy para HTTP: listas en HttpContext.Items usadas por el middleware antiguo.
    /// Mantener ambas para compatibilidad.
    /// </summary>
    private const string HttpItemsKey = "HttpClientLogs";       // List<string>
    private const string HttpItemsTimedKey = "HttpClientLogsTimed";  // List<object> con TsUtc/Content

    // ===================== Modelo de eventos dinámicos =====================

    /// <summary>
    /// Tipos de eventos dinámicos (solo etiqueta, no altera la posición fija).
    /// </summary>
    public enum DynamicLogKind
    {
        /// <summary>Eventos de clientes HTTP salientes.</summary>
        HttpClient = 1,

        /// <summary>Ejecuciones de comandos SQL.</summary>
        Sql = 2,

        /// <summary>Entradas manuales simples (AddSingleLog).</summary>
        ManualSingle = 3,

        /// <summary>Entradas manuales de objeto/estructura (AddObjLog).</summary>
        ManualObject = 4,

        /// <summary>Bloques manuales (StartLogBlock/Add/End).</summary>
        ManualBlock = 5,

        /// <summary>Reservado.</summary>
        Custom = 99
    }

    /// <summary>
    /// Segmento dinámico con sello de INICIO (UTC) y secuencia para ordenar de forma estable.
    /// </summary>
    private sealed class DynamicLogSegment(DynamicLogKind kind, DateTime timestampUtc, int sequence, string content)
    {
        /// <summary>Clasificación del segmento (HTTP/SQL/etc.).</summary>
        public DynamicLogKind Kind { get; } = kind;

        /// <summary>Instante UTC en que comenzó el evento (se usa para ordenar correctamente).</summary>
        public DateTime TimestampUtc { get; } = timestampUtc;

        /// <summary>Secuencia incremental por-request para desempate.</summary>
        public int Sequence { get; } = sequence;

        /// <summary>Contenido final del bloque (ya formateado por el productor).</summary>
        public string Content { get; } = content;

        /// <summary>Fábrica con sello UTC actual (para casos sin “startedUtc” explícito).</summary>
        public static DynamicLogSegment Create(DynamicLogKind k, int seq, string text)
            => new(k, DateTime.UtcNow, seq, text);
    }

    /// <summary>
    /// Buffer por-request que concentra:
    /// - Slots fijos 2..6 (Environment, Controller, Request, Response, Errors).
    /// - Eventos dinámicos siempre entre 4 y 5, ordenados por inicio real.
    /// </summary>
    private sealed class RequestLogBuffer(string filePath)
    {
        /// <summary>Ruta del archivo final de este request.</summary>
        public string FilePath { get; } = filePath;

        // Secuencia incremental local para desempates y estabilidad.
        private int _seq;

        // Cola de segmentos dinámicos producidos durante el request.
        private readonly ConcurrentQueue<DynamicLogSegment> _dynamic = new();

        // Slots FIJOS 2..6 (1: Inicio y 7: Fin siguen fuera, como hoy).
        public string? FixedEnvironment { get; private set; }
        public string? FixedController { get; private set; }
        public string? FixedRequest { get; private set; }
        public string? FixedResponse { get; private set; }
        public List<string> FixedErrors { get; } = [];

        /// <summary>Coloca Environment Info (2).</summary>
        public void SetEnvironmentSlot(string content) => FixedEnvironment = content;

        /// <summary>Coloca Controlador (3).</summary>
        public void SetControllerSlot(string content) => FixedController = content;

        /// <summary>Coloca Request Info (4).</summary>
        public void SetRequestSlot(string content) => FixedRequest = content;

        /// <summary>Coloca Response Info (5).</summary>
        public void SetResponseSlot(string content) => FixedResponse = content;

        /// <summary>Agrega Error (6).</summary>
        public void AddErrorSlot(string content) => FixedErrors.Add(content);

        /// <summary>
        /// Inserta evento dinámico con sello de tiempo actual (UTC).
        /// </summary>
        public void AppendDynamicSlot(DynamicLogKind kind, string content)
        {
            var seq = Interlocked.Increment(ref _seq);
            _dynamic.Enqueue(DynamicLogSegment.Create(kind, seq, content));
        }

        /// <summary>
        /// Inserta evento dinámico con sello de INICIO explícito (UTC) — ideal para HTTP/SQL.
        /// </summary>
        public void AppendDynamicAt(DynamicLogKind kind, string content, DateTime timestampUtc)
        {
            var seq = Interlocked.Increment(ref _seq);
            _dynamic.Enqueue(new DynamicLogSegment(kind, timestampUtc, seq, content));
        }

        /// <summary>
        /// Construye el bloque central 2→6 con la ventana dinámica en medio (entre 4 y 5).
        /// </summary>
        public string BuildCore()
        {
            // 1) Ordenar la porción dinámica por INICIO real y por secuencia.
            List<DynamicLogSegment> dyn = [];
            while (_dynamic.TryDequeue(out var seg)) dyn.Add(seg);

            var dynOrdered = dyn
                .OrderBy(s => s.TimestampUtc)
                .ThenBy(s => s.Sequence)
                .ToList();

            // 2) Ensamblar el bloque central en el orden fijo.
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

    // ===================== Helpers internos del buffer =====================

    /// <summary>
    /// Devuelve el buffer si ya existe en Items. No crea uno nuevo.
    /// </summary>
    private static RequestLogBuffer? TryGetExistingBuffer(HttpContext? ctx)
    {
        if (ctx is not null &&
            ctx.Items.TryGetValue(BufferKey, out var existing) &&
            existing is RequestLogBuffer ok)
            return ok;

        return null;
    }

    /// <summary>
    /// Crea u obtiene el buffer por-request desde Items. Si no hay HttpContext, devuelve null.
    /// </summary>
    private static RequestLogBuffer? GetOrCreateBuffer(HttpContext? ctx, string? filePath)
    {
        if (ctx is null) return null;

        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath;

        if (ctx.Items.TryGetValue(BufferKey, out var existing) && existing is RequestLogBuffer ok)
            return ok;

        var created = new RequestLogBuffer(path!);
        ctx.Items[BufferKey] = created;
        return created;
    }

    // ===================== API pública de slots fijos =====================

    /// <summary>Coloca Environment Info (2) en el buffer.</summary>
    public static void SetEnvironment(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetEnvironmentSlot(text);

    /// <summary>Coloca Controlador (3) en el buffer.</summary>
    public static void SetController(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetControllerSlot(text);

    /// <summary>Coloca Request Info (4) en el buffer.</summary>
    public static void SetRequest(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetRequestSlot(text);

    /// <summary>Coloca Response Info (5) en el buffer.</summary>
    public static void SetResponse(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetResponseSlot(text);

    /// <summary>Agrega Error (6) en el buffer.</summary>
    public static void AddError(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.AddErrorSlot(text);

    /// <summary>
    /// Agrega un evento dinámico con sello de tiempo ACTUAL. Queda entre 4 y 5.
    /// </summary>
    public static void AppendDynamic(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text)
        => GetOrCreateBuffer(ctx, filePath)?.AppendDynamicSlot(kind, text);

    // ===================== API pública de compatibilidad =====================

    /// <summary>
    /// ¿El buffer por-request está activo en este HttpContext?
    /// </summary>
    public static bool HasRequestBuffer(HttpContext? ctx)
        => ctx is not null && ctx.Items.ContainsKey(BufferKey);

    /// <summary>
    /// Agrega un evento dinámico con timestamp explícito (UTC) con compatibilidad:
    /// - Si hay buffer, encola (orden por INICIO).
    /// - Si no hay buffer:
    ///   - HTTP → Items legacy (con lista "timed" y lista simple).
    ///   - Otros (ej. SQL) → escritura directa a archivo (comportamiento clásico).
    /// </summary>
    public static void AppendDynamicCompatAt(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text, DateTime startedUtc)
    {
        var buf = TryGetExistingBuffer(ctx);
        if (buf is not null)
        {
            buf.AppendDynamicAt(kind, text, startedUtc);
            return;
        }

        // Sin buffer → compatibilidad total
        if (kind == DynamicLogKind.HttpClient && ctx is not null)
        {
            // 1) Lista “timed” para que Flush pueda importar y ordenar si más tarde hay buffer.
            if (!ctx.Items.ContainsKey(HttpItemsTimedKey)) ctx.Items[HttpItemsTimedKey] = new List<object>();
            if (ctx.Items[HttpItemsTimedKey] is List<object> timed) timed.Add(new LegacyHttpEntry(startedUtc, text));

            // 2) Lista antigua (string) para middleware legacy que aún lee esta clave.
            if (!ctx.Items.ContainsKey(HttpItemsKey)) ctx.Items[HttpItemsKey] = new List<string>();
            if (ctx.Items[HttpItemsKey] is List<string> raw) raw.Add(text);

            return;
        }

        // Para SQL u otros: escritura directa (no dependemos del middleware).
        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath!;
        var dir = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;
        WriteLogToFile(dir, path, text);
    }

    /// <summary>
    /// Agrega un evento dinámico con compatibilidad usando el sello de tiempo actual.
    /// </summary>
    public static void AppendDynamicCompat(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text)
        => AppendDynamicCompatAt(ctx, filePath, kind, text, DateTime.UtcNow);

    // ===================== Flush central con import legacy =====================

    /// <summary>
    /// FLUSH único: importa HTTP legacy desde Items (si existe), compone 2→6 y lo escribe.
    /// Mantén Inicio(1) y Fin(7) como ya los manejas.
    /// </summary>
    public static void Flush(HttpContext ctx, string? filePath)
    {
        if (ctx is null) return;

        var buf = GetOrCreateBuffer(ctx, filePath);
        if (buf is null) return;

        // Importar HTTP legacy antes de ordenar.
        ImportLegacyHttpItems(ctx, buf);

        var core = buf.BuildCore();
        var path = buf.FilePath;
        var dir = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;

        WriteLogToFile(dir, path, core);
        // Si quieres CSV automático, descomenta:
        // SaveLogAsCsv(dir, path, core)
    }

    /// <summary>
    /// Entrada temporal para Items “timed”. No se expone fuera.
    /// </summary>
    private sealed class LegacyHttpEntry(DateTime tsUtc, string content)
    {
        public DateTime TsUtc { get; } = tsUtc;
        public string Content { get; } = content;
    }

    /// <summary>
    /// Importa entradas de Items "timed" y antiguas (string) para que también se ordenen.
    /// </summary>
    private static void ImportLegacyHttpItems(HttpContext ctx, RequestLogBuffer buf)
    {
        // 1) Lista “timed”: TsUtc + Content (preferida)
        if (ctx.Items.TryGetValue(HttpItemsTimedKey, out var obj) && obj is List<object> timed && timed.Count > 0)
        {
            foreach (var o in timed)
            {
                // Late-binding para aceptar cualquier tipo con TsUtc/Content (p.ej. del handler).
                var ts = (DateTime?)o.GetType().GetProperty("TsUtc")?.GetValue(o);
                var tx = (string?)o.GetType().GetProperty("Content")?.GetValue(o);
                if (ts is null || tx is null) continue;

                buf.AppendDynamicAt(DynamicLogKind.HttpClient, tx, ts.Value);
            }

            ctx.Items.Remove(HttpItemsTimedKey); // evitar duplicados
        }

        // 2) Lista antigua de strings (sin timestamp): se importan con el tiempo de importación
        //    — último recurso por compatibilidad.
        if (ctx.Items.TryGetValue(HttpItemsKey, out var raw) && raw is List<string> oldList && oldList.Count > 0)
        {
            foreach (var s in oldList)
                buf.AppendDynamicSlot(DynamicLogKind.HttpClient, s);

            ctx.Items.Remove(HttpItemsKey); // evitar duplicados
        }
    }
}

namespace Logging.Abstractions;

/// <summary>
/// Contrato para un bloque de log que agrupa múltiples filas con un encabezado y un pie comunes.
/// </summary>
public interface ILogBlock : IDisposable
{
    /// <summary>Agrega una fila de texto al bloque.</summary>
    void Add(string message, bool includeTimestamp = false);

    /// <summary>Agrega una fila logueando un objeto formateado.</summary>
    void AddObj(string name, object obj);

    /// <summary>Agrega una fila con detalle de excepción.</summary>
    void AddException(Exception ex);

    /// <summary>Finaliza el bloque (escribe el pie). Idempotente.</summary>
    void End();
}


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
using Logging.Abstractions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Logging.Decorators;

/// <summary>
/// Decorador de DbCommand que:
/// - Mide cada ejecución (Reader/NonQuery/Scalar) y construye un bloque estructurado.
/// - Sustituye parámetros por valores reales en el SQL sin romper literales.
/// - Propaga el INICIO (UTC) al contexto para ordenar cronológicamente en el middleware.
/// </summary>
public class LoggingDbCommandWrapper(
    DbCommand innerCommand,
    ILoggingService? loggingService = null,
    IHttpContextAccessor? httpContextAccessor = null) : DbCommand
{
    private readonly DbCommand _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
    private readonly ILoggingService? _loggingService = loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;

    // Clave compartida con el servicio para propagar el INICIO del SQL
    // (el servicio la usa si ocurre error para mantener el orden correcto).
    private const string SqlStartedKey = "__SqlStartedUtc";

    #region Ejecuciones (bloque por ejecución)

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var startedLocal = DateTime.Now;
        var startedUtc = startedLocal.ToUniversalTime();
        var ctx = _httpContextAccessor?.HttpContext;
        if (ctx is not null) ctx.Items["__SqlStartedUtc"] = startedUtc;

        var sw = Stopwatch.StartNew();
        try
        {
            var reader = _innerCommand.ExecuteReader(behavior);
            sw.Stop();

            // SELECT: filas afectadas = 0
            LogOneExecution(
                startedAt: startedLocal,
                duration: sw.Elapsed,
                affectedRows: 0,
                sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
            );

            return reader;
        }
        catch (Exception ex)
        {
            _loggingService?.LogDatabaseError(_innerCommand, ex, ctx);
            throw;
        }
    }

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        var startedLocal = DateTime.Now;
        var startedUtc = startedLocal.ToUniversalTime();
        var ctx = _httpContextAccessor?.HttpContext;
        if (ctx is not null) ctx.Items["__SqlStartedUtc"] = startedUtc;

        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteNonQuery();

        sw.Stop();
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: result,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        var startedLocal = DateTime.Now;
        var startedUtc = startedLocal.ToUniversalTime();
        var ctx = _httpContextAccessor?.HttpContext;
        if (ctx is not null) ctx.Items["__SqlStartedUtc"] = startedUtc;

        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteScalar();

        sw.Stop();
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0, // Scalar no afecta filas
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    #endregion

    #region Logging por ejecución (con agregación cronológica)

    /// <summary>
    /// Construye el modelo de log SQL y lo envía al servicio para que lo agregue a la lista “timed”.
    /// </summary>
    private void LogOneExecution(DateTime startedAt, TimeSpan duration, int affectedRows, string sqlRendered)
    {
        if (_loggingService is null) return; // Resiliencia: sin servicio, no interfiere.

        try
        {
            var conn = _innerCommand.Connection;

            var model = new SqlLogModel
            {
                Sql = sqlRendered,
                ExecutionCount = 1,               // bloque por ejecución
                TotalAffectedRows = affectedRows, // 0 para SELECT/SCALAR
                StartTime = startedAt,            // ⇦ INICIO real de la ejecución
                Duration = duration,
                DatabaseName = conn?.Database ?? "Desconocida",
                Ip = conn?.DataSource ?? "Desconocida",
                Port = 0, // Si puedes inferirlo, complétalo.
                TableName = ExtraerNombreTablaDesdeSql(_innerCommand.CommandText),
                Schema = ExtraerEsquemaDesdeSql(_innerCommand.CommandText)
            };

            _loggingService.LogDatabaseSuccess(model, _httpContextAccessor?.HttpContext);
        }
        catch (Exception logEx)
        {
            // El logging no debe detener la app: registramos en el mismo archivo como fallback.
            try
            {
                _loggingService?.WriteLog(_httpContextAccessor?.HttpContext,
                    $"[LoggingDbCommandWrapper] Error al escribir el log SQL: {logEx.Message}");
            }
            catch { /* silencio para evitar loops */ }
        }
    }

    #endregion

    #region Render de SQL con parámetros (respetando literales)

    /// <summary>
    /// Devuelve el SQL con parámetros sustituidos por sus valores reales (comillas en literales).
    /// Ignora coincidencias dentro de literales '...'.
    /// </summary>
    private static string RenderSqlWithParametersSafe(string? sql, DbParameterCollection parameters)
    {
        if (string.IsNullOrEmpty(sql) || parameters.Count == 0) return sql ?? string.Empty;

        var literalRanges = ComputeSingleQuoteRanges(sql);

        if (sql.Contains('?'))
            return ReplacePositionalIgnoringLiterals(sql, parameters, literalRanges);

        return ReplaceNamedIgnoringLiterals(sql, parameters, literalRanges);
    }

    /// <summary>
    /// Identifica rangos [start,end] dentro de comillas simples (maneja escape '').
    /// </summary>
    private static List<(int start, int end)> ComputeSingleQuoteRanges(string sql)
    {
        var ranges = new List<(int, int)>();
        bool inString = false;
        int start = -1;

        int idx = 0;
        while (idx < sql.Length)
        {
            char c = sql[idx];
            if (c == '\'')
            {
                bool isEscaped = (idx + 1 < sql.Length) && sql[idx + 1] == '\'';

                if (!inString) { inString = true; start = idx; }
                else if (!isEscaped) { inString = false; ranges.Add((start, idx)); }

                idx += isEscaped ? 2 : 1;
                continue;
            }

            idx++;
        }

        return ranges;
    }

    /// <summary>Indica si un índice está dentro de alguno de los rangos de literales.</summary>
    private static bool IsInsideAnyRange(int index, List<(int start, int end)> ranges)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            var (s, e) = ranges[i];
            if (index >= s && index <= e) return true;
        }
        return false;
    }

    /// <summary>
    /// Reemplaza '?' por valores en orden, ignorando las que caen dentro de literales.
    /// </summary>
    private static string ReplacePositionalIgnoringLiterals(string sql, DbParameterCollection parameters, List<(int start, int end)> literalRanges)
    {
        var sb = new StringBuilder(sql.Length + parameters.Count * 10);
        int paramIndex = 0;

        for (int i = 0; i < sql.Length; i++)
        {
            char c = sql[i];
            if (c == '?' && !IsInsideAnyRange(i, literalRanges) && paramIndex < parameters.Count)
            {
                var p = parameters[paramIndex++];
                sb.Append(FormatParameterValue(p));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Reemplazo de parámetros nombrados (@name, :name) ignorando literales.
    /// </summary>
    private static string ReplaceNamedIgnoringLiterals(string sql, DbParameterCollection parameters, List<(int start, int end)> literalRanges)
    {
        if (parameters.Count == 0) return sql;

        string result = sql;
        foreach (DbParameter p in parameters)
        {
            var name = p.ParameterName?.Trim();
            if (string.IsNullOrEmpty(name)) continue;

            foreach (var token in new[] { "@" + name, ":" + name })
            {
                var rx = new Regex($@"(?<!\w){Regex.Escape(token)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                result = rx.Replace(result, m =>
                {
                    if (IsInsideAnyRange(m.Index, literalRanges)) return m.Value;
                    return FormatParameterValue(p);
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Formatea un parámetro a literal SQL. Maneja IEnumerable, binarios y tipos escalar.
    /// </summary>
    private static string FormatParameterValue(DbParameter p)
    {
        var value = p.Value;

        if (value is null || value == DBNull.Value) return "NULL";

        if (value is IEnumerable enumerable && value is not string && value is not byte[])
        {
            var parts = new List<string>();
            foreach (var item in enumerable) parts.Add(FormatScalar(item));
            return "(" + string.Join(", ", parts) + ")";
        }

        return FormatScalar(value, p.DbType);
    }

    /// <summary>Formateo escalar con heurística por DbType/tipo CLR.</summary>
    private static string FormatScalar(object? value, DbType? hinted = null)
    {
        if (value is null || value == DBNull.Value) return "NULL";
        if (value is byte[] bytes) return $"<binary {bytes.Length} bytes>";

        if (hinted.HasValue)
        {
            switch (hinted.Value)
            {
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.String:
                case DbType.StringFixedLength:
                case DbType.Xml:
                    return "'" + EscapeSqlString(Convert.ToString(value) ?? string.Empty) + "'";

                case DbType.Date:
                case DbType.Time:
                case DbType.DateTime:
                case DbType.DateTime2:
                case DbType.DateTimeOffset:
                    return $"'{ToDateTime(value):yyyy-MM-dd HH:mm:ss}'";

                case DbType.Boolean:
                    return Convert.ToBoolean(value) ? "1" : "0";

                case DbType.Byte:
                case DbType.SByte:
                case DbType.Int16:
                case DbType.Int32:
                case DbType.Int64:
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                case DbType.Single:
                case DbType.Double:
                case DbType.Decimal:
                case DbType.VarNumeric:
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";

                case DbType.Guid:
                    return "'" + Convert.ToString(value) + "'";

                case DbType.Object:
                default:
                    break;
            }
        }

        return value switch
        {
            string s => "'" + EscapeSqlString(s) + "'",
            char ch => "'" + EscapeSqlString(ch.ToString()) + "'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss}'",
            bool b => b ? "1" : "0",
            float or double or decimal => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0",
            sbyte or byte or short or int or long or ushort or uint or ulong => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0",
            Guid guid => "'" + guid.ToString() + "'",
            IEnumerable e => "(" + string.Join(", ", EnumerateFormatted(e)) + ")",
            _ => "'" + EscapeSqlString(Convert.ToString(value) ?? string.Empty) + "'"
        };
    }

    private static IEnumerable<string> EnumerateFormatted(IEnumerable e)
    {
        foreach (var it in e) yield return FormatScalar(it);
    }

    private static string EscapeSqlString(string s) => s.Replace("'", "''");

    private static DateTime ToDateTime(object value)
    {
        if (value is DateTime d) return d;
        if (value is DateTimeOffset dto) return dto.LocalDateTime;
        if (DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                              DateTimeStyles.AllowWhiteSpaces, out var parsed))
            return parsed;
        return DateTime.MinValue;
    }

    #endregion

    #region Heurísticas para nombre de tabla/esquema

    private static string ExtraerNombreTablaDesdeSql(string sql)
    {
        try
        {
            var tokens = sql.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var idx = Array.FindIndex(tokens, t => t is "into" or "from" or "update");
            return idx >= 0 && tokens.Length > idx + 1 ? tokens[idx + 1] : "Desconocida";
        }
        catch { return "Desconocida"; }
    }

    private static string ExtraerEsquemaDesdeSql(string sql)
    {
        var tabla = ExtraerNombreTablaDesdeSql(sql);
        var partes = tabla.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length > 1 ? partes[0] : "Desconocida";
    }

    #endregion

    #region Delegación transparente al comando interno

    public override string? CommandText { get => _innerCommand.CommandText; set => _innerCommand.CommandText = value; }
    public override int CommandTimeout { get => _innerCommand.CommandTimeout; set => _innerCommand.CommandTimeout = value; }
    public override CommandType CommandType { get => _innerCommand.CommandType; set => _innerCommand.CommandType = value; }
    public override bool DesignTimeVisible { get => _innerCommand.DesignTimeVisible; set => _innerCommand.DesignTimeVisible = value; }
    public override UpdateRowSource UpdatedRowSource { get => _innerCommand.UpdatedRowSource; set => _innerCommand.UpdatedRowSource = value; }
    protected override DbConnection? DbConnection { get => _innerCommand.Connection; set => _innerCommand.Connection = value; }
    protected override DbTransaction? DbTransaction { get => _innerCommand.Transaction; set => _innerCommand.Transaction = value; }
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;
    public override void Cancel() => _innerCommand.Cancel();
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();
    public override void Prepare() => _innerCommand.Prepare();

    #endregion

    /// <summary>El decorador no imprime resumen en Dispose; los bloques son por ejecución.</summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _innerCommand.Dispose();
    }
}

