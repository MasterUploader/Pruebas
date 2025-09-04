using Logging.Helpers;
using Logging.Ordering;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace RestUtilities.Logging.Handlers;

public class HttpClientLoggingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Logging.Abstractions.ILoggingService _loggingService;

    /// <summary>Usa constructor primario; ILoggingService es opcional si lo resuelves por DI aquí.</summary>
    public HttpClientLoggingHandler(IHttpContextAccessor httpContextAccessor, Logging.Abstractions.ILoggingService loggingService)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var context = _httpContextAccessor.HttpContext;
        var filePath = _loggingService.GetCurrentLogFile();
        string traceId = context?.TraceIdentifier ?? Guid.NewGuid().ToString();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            string responseBody = response.Content != null
                ? await response.Content.ReadAsStringAsync(cancellationToken)
                : "Sin contenido";

            string formatted = LogFormatter.FormatHttpClientRequest(
                traceId: traceId,
                method: request.Method.Method,
                url: request.RequestUri?.ToString() ?? "URI no definida",
                statusCode: ((int)response.StatusCode).ToString(),
                elapsedMs: stopwatch.ElapsedMilliseconds,
                headers: request.Headers.ToString(),
                body: request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null,
                responseBody: LogHelper.PrettyPrintAuto(responseBody, response.Content?.Headers?.ContentType?.MediaType)
            );

            // 🔸 Dinámico: quedará entre Request y Response, ordenado por tiempo real.
            LogHelper.AppendDynamic(_loggingService, context, filePath, DynamicLogKind.HttpClient, formatted);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            string errorLog = LogFormatter.FormatHttpClientError(
                traceId: traceId,
                method: request.Method.Method,
                url: request.RequestUri?.ToString() ?? "URI no definida",
                exception: ex
            );

            LogHelper.AppendDynamic(_loggingService, context, filePath, DynamicLogKind.HttpClient, errorLog);
            throw;
        }
    }
}





// 1) Reemplazar AddSingleLog:
public void AddSingleLog(string message)
{
    try
    {
        string formatted = LogFormatter.FormatSingleLog(message).Indent(LogScope.CurrentLevel);
        var ctx = _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile();
        // 🔸 Dinámico (ManualSingle)
        LogHelper.AppendDynamic(this, ctx, filePath, Logging.Ordering.DynamicLogKind.ManualSingle, formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}

// 2) Reemplazar AddObjLog(nombre, objeto):
public void AddObjLog(string objectName, object logObject)
{
    try
    {
        string formatted = LogFormatter.FormatObjectLog(objectName, logObject).Indent(LogScope.CurrentLevel);
        var ctx = _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile();
        // 🔸 Dinámico (ManualObject)
        LogHelper.AppendDynamic(this, ctx, filePath, Logging.Ordering.DynamicLogKind.ManualObject, formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}

// 3) Reemplazar AddObjLog(objeto):
public void AddObjLog(object logObject)
{
    try
    {
        string objectName = logObject?.GetType()?.Name ?? "ObjetoDesconocido";
        object safeObject = logObject ?? new { };
        string formatted = LogFormatter.FormatObjectLog(objectName, safeObject).Indent(LogScope.CurrentLevel);
        var ctx = _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile();
        // 🔸 Dinámico (ManualObject)
        LogHelper.AppendDynamic(this, ctx, filePath, Logging.Ordering.DynamicLogKind.ManualObject, formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}

// 4) Reemplazar AddExceptionLog:
public void AddExceptionLog(Exception ex)
{
    try
    {
        string formatted = LogFormatter.FormatExceptionDetails(ex.ToString()).Indent(LogScope.CurrentLevel);
        var ctx = _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile();
        // 🔸 Slot 6) Errores (no dinámico)
        LogHelper.AddError(this, ctx!, filePath, formatted);
    }
    catch (Exception e)
    {
        LogInternalError(e);
    }
}

// 5) Reemplazar LogDatabaseSuccess:
public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
{
    try
    {
        var formatted = LogFormatter.FormatDbExecution(model);
        var ctx = context ?? _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile();
        // 🔸 Dinámico (SQL)
        LogHelper.AppendDynamic(this, ctx, filePath, Logging.Ordering.DynamicLogKind.Sql, formatted);
    }
    catch (Exception loggingEx)
    {
        LogInternalError(loggingEx);
    }
}

// 6) Reemplazar LogDatabaseError:
public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
{
    try
    {
        var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
        var tabla = LogHelper.ExtractTableName(command.CommandText);

        var formatted = LogFormatter.FormatDbExecutionError(
            nombreBD: connectionInfo.Database,
            ip: connectionInfo.Ip,
            puerto: connectionInfo.Port,
            biblioteca: connectionInfo.Library,
            tabla: tabla,
            sentenciaSQL: command.CommandText,
            exception: ex,
            horaError: DateTime.Now
        );

        var ctx = context ?? _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile();

        // 🔸 Dinámico (SQL) + registrar excepción en slot 6
        LogHelper.AppendDynamic(this, ctx, filePath, Logging.Ordering.DynamicLogKind.Sql, formatted);
        AddExceptionLog(ex);
    }
    catch (Exception errorAlLoguear)
    {
        LogInternalError(errorAlLoguear);
    }
}





var ctx = context ?? _httpContextAccessor.HttpContext;
var fp = GetCurrentLogFile();
var header = LogFormatter.BuildBlockHeader(title);
LogHelper.AppendDynamic(this, ctx!, fp, Logging.Ordering.DynamicLogKind.ManualBlock, header);
return new LogBlock(this, fp, title);



LogHelper.AppendDynamic(_svc, _httpContextAccessor.HttpContext, _filePath, Logging.Ordering.DynamicLogKind.ManualBlock, contenido);


