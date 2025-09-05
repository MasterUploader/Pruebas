using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Http;

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
    private const string HttpItemsKey      = "HttpClientLogs";       // List<string>
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
        public string? FixedController  { get; private set; }
        public string? FixedRequest     { get; private set; }
        public string? FixedResponse    { get; private set; }
        public List<string> FixedErrors { get; } = [];

        /// <summary>Coloca Environment Info (2).</summary>
        public void SetEnvironmentSlot(string content) => FixedEnvironment = content;

        /// <summary>Coloca Controlador (3).</summary>
        public void SetControllerSlot(string content)  => FixedController  = content;

        /// <summary>Coloca Request Info (4).</summary>
        public void SetRequestSlot(string content)     => FixedRequest     = content;

        /// <summary>Coloca Response Info (5).</summary>
        public void SetResponseSlot(string content)    => FixedResponse    = content;

        /// <summary>Agrega Error (6).</summary>
        public void AddErrorSlot(string content)       => FixedErrors.Add(content);

        /// <summary>
        /// Inserta evento dinámico con sello de tiempo actual (UTC).
        /// </summary>
        public void AppendDynamic(DynamicLogKind kind, string content)
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
            if (FixedController  is not null) sb.Append(FixedController);
            if (FixedRequest     is not null) sb.Append(FixedRequest);

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
        => GetOrCreateBuffer(ctx, filePath)?.AppendDynamic(kind, text);

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
        var dir  = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;
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
        var dir  = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;

        WriteLogToFile(dir, path, core);
        // Si quieres CSV automático, descomenta:
        // SaveLogAsCsv(dir, path, core);
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
                var tx = (string?)  o.GetType().GetProperty("Content")?.GetValue(o);
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
                buf.AppendDynamic(DynamicLogKind.HttpClient, s);

            ctx.Items.Remove(HttpItemsKey); // evitar duplicados
        }
    }
}



using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using static Logging.Helpers.LogHelper;

namespace RestUtilities.Logging.Handlers;

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



// Ejemplo ilustrativo (ajústalo a tu clase real)
public int ExecuteNonQueryLogged(DbCommand cmd, IHttpContextAccessor accessor)
{
    var startedUtc = DateTime.UtcNow; // ⬅️ sello de INICIO

    try
    {
        var rows = cmd.ExecuteNonQuery();

        var block = LogFormatter.FormatDbExecution(new SqlLogModel
        {
            CommandText = cmd.CommandText,
            // ... completa tu modelo como ya lo haces
        });

        LogHelper.AppendDynamicCompatAt(
            accessor.HttpContext, filePath: null,
            LogHelper.DynamicLogKind.Sql,
            block, startedUtc);

        return rows;
    }
    catch (Exception ex)
    {
        var errorBlock = LogFormatter.FormatDbExecutionError(
            nombreBD: "Desconocida",
            ip: "-", puerto: 0, biblioteca: "-",
            tabla: LogHelper.ExtractTableName(cmd.CommandText),
            sentenciaSQL: cmd.CommandText,
            exception: ex,
            horaError: DateTime.Now);

        LogHelper.AppendDynamicCompatAt(
            accessor.HttpContext, null,
            LogHelper.DynamicLogKind.Sql,
            errorBlock, startedUtc);

        throw;
    }
}



