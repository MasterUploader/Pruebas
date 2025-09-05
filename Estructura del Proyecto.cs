using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Logging.Helpers;

/// <summary>
/// Extensión (partial) de LogHelper que agrega:
/// 1) Un buffer por-request con "ventana dinámica" entre 4) Request y 5) Response.
/// 2) Orden cronológico basado en el INICIO real de cada evento (HTTP/SQL/etc.).
/// 3) Compatibilidad: si no hay buffer, escribe directo como en el comportamiento legacy.
/// </summary>
public static partial class LogHelper
{
    // ===================== Infraestructura interna =====================

    /// <summary>
    /// Clave para guardar el buffer por-request en HttpContext.Items (detalle interno).
    /// </summary>
    private const string BufferKey = "__ReqLogBuffer";

    /// <summary>
    /// AsyncLocal que mantiene referencia al buffer del request a través de awaits/Task.Run
    /// para escenarios donde el HttpContext no está disponible en un hilo hijo.
    /// </summary>
    private static readonly AsyncLocal<object?> _ambient = new();

    /// <summary>
    /// Claves legacy usadas por el middleware antiguo para acumular bloques de HTTP en Items.
    /// Se mantienen para compatibilidad total sin cambiar a los consumidores de la librería.
    /// </summary>
    private const string HttpItemsKey      = "HttpClientLogs";       // List<string>
    private const string HttpItemsTimedKey = "HttpClientLogsTimed";  // List<object> con { DateTime TsUtc, string Content }

    // ===================== Modelo de eventos dinámicos =====================

    /// <summary>
    /// Tipos de eventos dinámicos que ocurren durante la ejecución del request.
    /// La clasificación NO altera la posición en el log (solo etiqueta).
    /// </summary>
    public enum DynamicLogKind
    {
        /// <summary>Eventos de HttpClient (salientes).</summary>
        HttpClient = 1,

        /// <summary>Ejecución de comandos SQL.</summary>
        Sql = 2,

        /// <summary>Entradas manuales de texto (AddSingleLog).</summary>
        ManualSingle = 3,

        /// <summary>Entradas manuales de objeto/estructura (AddObjLog).</summary>
        ManualObject = 4,

        /// <summary>Bloques manuales (StartLogBlock/Add/End).</summary>
        ManualBlock = 5,

        /// <summary>Reservado para extensiones personalizadas.</summary>
        Custom = 99
    }

    /// <summary>
    /// Segmento dinámico con sello temporal de INICIO (UTC) y secuencia para empate.
    /// El orden final usa: TimestampUtc ASC, luego Sequence ASC.
    /// </summary>
    private sealed class DynamicLogSegment(DynamicLogKind kind, DateTime timestampUtc, int sequence, string content)
    {
        public DynamicLogKind Kind { get; } = kind;          // Clasificación (HTTP/SQL/…)
        public DateTime TimestampUtc { get; } = timestampUtc; // Momento real de INICIO del evento
        public int Sequence { get; } = sequence;              // Empate estable en alta concurrencia
        public string Content { get; } = content;             // Texto final ya formateado

        /// <summary>Crea un segmento con sello de tiempo actual (UTC).</summary>
        public static DynamicLogSegment Create(DynamicLogKind k, int seq, string text)
            => new(k, DateTime.UtcNow, seq, text);
    }

    /// <summary>
    /// Buffer por-request que concentra:
    /// - Slots fijos 2..6 (Environment, Controller, Request, Response, Errors).
    /// - Eventos dinámicos a ubicar SIEMPRE entre 4 (Request) y 5 (Response).
    /// </summary>
    private sealed class RequestLogBuffer(string filePath)
    {
        /// <summary>Ruta final del archivo de log para este request.</summary>
        public string FilePath { get; } = filePath;

        // Secuencia incremental local para desempates en la cola dinámica.
        private int _seq;

        // Cola de segmentos dinámicos producidos durante el request.
        private readonly ConcurrentQueue<DynamicLogSegment> _dynamic = new();

        // Slots fijos (1 y 7 — Inicio/Fin — se mantienen fuera: los manejas como hoy).
        public string? FixedEnvironment { get; private set; }
        public string? FixedController  { get; private set; }
        public string? FixedRequest     { get; private set; }
        public string? FixedResponse    { get; private set; }
        public List<string> FixedErrors { get; } = [];

        /// <summary>Coloca/actualiza Environment Info (2).</summary>
        public void SetEnvironmentSlot(string content) => FixedEnvironment = content;

        /// <summary>Coloca/actualiza Controlador (3).</summary>
        public void SetControllerSlot(string content)  => FixedController  = content;

        /// <summary>Coloca/actualiza Request Info (4).</summary>
        public void SetRequestSlot(string content)     => FixedRequest     = content;

        /// <summary>Coloca/actualiza Response Info (5).</summary>
        public void SetResponseSlot(string content)    => FixedResponse    = content;

        /// <summary>Agrega un Error (6). Se acumulan en orden de llegada.</summary>
        public void AddErrorSlot(string content)       => FixedErrors.Add(content);

        /// <summary>
        /// Inserta evento dinámico con sello de tiempo actual. Útil cuando no tienes un "startedUtc".
        /// </summary>
        public void AppendDynamic(DynamicLogKind kind, string content)
        {
            var seq = Interlocked.Increment(ref _seq);
            _dynamic.Enqueue(DynamicLogSegment.Create(kind, seq, content));
        }

        /// <summary>
        /// Inserta evento dinámico con sello de tiempo de INICIO explícito (UTC).
        /// Permite ordenar según "cuándo comenzó" (ideal para HTTP/SQL).
        /// </summary>
        public void AppendDynamicAt(DynamicLogKind kind, string content, DateTime timestampUtc)
        {
            var seq = Interlocked.Increment(ref _seq);
            _dynamic.Enqueue(new DynamicLogSegment(kind, timestampUtc, seq, content));
        }

        /// <summary>
        /// Compone el bloque central 2→6 con la ventana dinámica entre 4 y 5.
        /// </summary>
        public string BuildCore()
        {
            // 1) Drenar y ordenar la porción dinámica por inicio real y secuencia.
            List<DynamicLogSegment> dyn = [];
            while (_dynamic.TryDequeue(out var seg)) dyn.Add(seg);

            var dynOrdered = dyn
                .OrderBy(s => s.TimestampUtc)
                .ThenBy(s => s.Sequence)
                .ToList();

            // 2) Ensamblar el bloque central.
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
    /// Devuelve el buffer existente desde HttpContext.Items o desde AsyncLocal (si fue publicado).
    /// No crea uno nuevo (uso interno para compatibilidad/lecturas).
    /// </summary>
    private static RequestLogBuffer? TryGetExistingBuffer(HttpContext? ctx)
    {
        if (ctx is not null &&
            ctx.Items.TryGetValue(BufferKey, out var existing) &&
            existing is RequestLogBuffer ok)
            return ok;

        return _ambient.Value as RequestLogBuffer;
    }

    /// <summary>
    /// Obtiene o crea el buffer por-request y lo publica en AsyncLocal para hilos hijos.
    /// Si no hay HttpContext, retorna el buffer ambient si existe (no crea).
    /// </summary>
    private static RequestLogBuffer? GetOrCreateBuffer(HttpContext? ctx, string? filePath)
    {
        // Sin contexto → reusar ambient si existe (evita crear buffers huérfanos).
        if (ctx is null) return _ambient.Value as RequestLogBuffer;

        // Determinar ruta final del archivo con tu helper existente si no se pasa.
        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath;

        if (ctx.Items.TryGetValue(BufferKey, out var existing) && existing is RequestLogBuffer ok)
        {
            _ambient.Value = ok; // Publica en AsyncLocal para tareas hijas (Task.Run/await)
            return ok;
        }

        var created = new RequestLogBuffer(path!);
        ctx.Items[BufferKey] = created;
        _ambient.Value = created; // Publica en ambient
        return created;
    }

    // ===================== API pública de slots fijos =====================

    /// <summary>Coloca Environment Info (2) en el buffer actual.</summary>
    public static void SetEnvironment(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetEnvironmentSlot(text);

    /// <summary>Coloca Controlador (3) en el buffer actual.</summary>
    public static void SetController(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetControllerSlot(text);

    /// <summary>Coloca Request Info (4) en el buffer actual.</summary>
    public static void SetRequest(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetRequestSlot(text);

    /// <summary>Coloca Response Info (5) en el buffer actual.</summary>
    public static void SetResponse(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetResponseSlot(text);

    /// <summary>Agrega Error (6) en el buffer actual.</summary>
    public static void AddError(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.AddErrorSlot(text);

    /// <summary>
    /// Agrega un evento dinámico con sello de tiempo ACTUAL. Queda entre 4 y 5.
    /// </summary>
    public static void AppendDynamic(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text)
        => GetOrCreateBuffer(ctx, filePath)?.AppendDynamic(kind, text);

    // ===================== API pública de compatibilidad =====================

    /// <summary>
    /// ¿El buffer por-request está activo en este HttpContext? Útil para compatibilidad dual.
    /// </summary>
    public static bool HasRequestBuffer(HttpContext? ctx)
        => ctx is not null && ctx.Items.ContainsKey(BufferKey);

    /// <summary>
    /// Agrega un evento dinámico con timestamp explícito (UTC) con compatibilidad:
    /// - Si hay buffer, encola y respetará el orden por INICIO.
    /// - Si no hay buffer, escribe directo como legacy (sin romper al consumidor).
    /// </summary>
    public static void AppendDynamicCompatAt(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text, DateTime startedUtc)
    {
        var buf = TryGetExistingBuffer(ctx);
        if (buf is not null)
        {
            buf.AppendDynamicAt(kind, text, startedUtc);
            return;
        }

        // Legacy: sin buffer → escritura directa al archivo (misma ruta de siempre).
        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath!;
        var dir  = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;
        WriteLogToFile(dir, path, text);
    }

    /// <summary>
    /// Agrega un evento dinámico con compatibilidad:
    /// - Si hay buffer, encola (sello de tiempo actual).
    /// - Si no hay buffer, escribe directo como legacy.
    /// </summary>
    public static void AppendDynamicCompat(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text)
    {
        var buf = TryGetExistingBuffer(ctx);
        if (buf is not null)
        {
            buf.AppendDynamic(kind, text);
            return;
        }

        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath!;
        var dir  = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;
        WriteLogToFile(dir, path, text);
    }

    // ===================== Flush central con import legacy =====================

    /// <summary>
    /// FLUSH único: importa logs HTTP legacy de Items (si existen), compone el bloque 2→6
    /// y lo escribe en el archivo final. Mantén Inicio(1) y Fin(7) como hoy.
    /// </summary>
    public static void Flush(HttpContext ctx, string? filePath)
    {
        if (ctx is null) return;

        var buf = GetOrCreateBuffer(ctx, filePath);
        if (buf is null) return;

        // Importar HTTP legacy (si se acumularon en Items) antes de ordenar.
        ImportLegacyHttpItems(ctx, buf);

        var core = buf.BuildCore();
        var path = buf.FilePath;
        var dir  = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;

        // Escritura TXT (reutiliza tus primitivas robustas).
        WriteLogToFile(dir, path, core);

        // Si deseas CSV automático, descomenta:
        // SaveLogAsCsv(dir, path, core);
    }

    /// <summary>
    /// Importa bloques HTTP guardados en Items (legacy) para que también participen
    /// del ordenado en la ventana dinámica entre 4 y 5.
    /// </summary>
    private static void ImportLegacyHttpItems(HttpContext ctx, RequestLogBuffer buf)
    {
        // 1) Lista "timed" (preferida): cada entrada trae TsUtc + Content.
        if (ctx.Items.TryGetValue(HttpItemsTimedKey, out var obj) && obj is List<object> timed && timed.Count > 0)
        {
            foreach (var o in timed)
            {
                // Late-binding: admite el tipo PrivateEntry del handler sin acoplar referencias.
                var tsProp = o.GetType().GetProperty("TsUtc");
                var ctProp = o.GetType().GetProperty("Content");
                if (tsProp is null || ctProp is null) continue;

                var ts = (DateTime)(tsProp.GetValue(o) ?? DateTime.UtcNow);
                var tx = (string)(ctProp.GetValue(o) ?? string.Empty);

                buf.AppendDynamicAt(DynamicLogKind.HttpClient, tx, ts);
            }

            // Limpiar para evitar doble escritura.
            ctx.Items.Remove(HttpItemsTimedKey);
        }

        // 2) Lista antigua de strings: se importa sin timestamp (quedará ordenada por fin).
        if (ctx.Items.TryGetValue(HttpItemsKey, out var raw) && raw is List<string> oldList && oldList.Count > 0)
        {
            foreach (var s in oldList)
                buf.AppendDynamic(DynamicLogKind.HttpClient, s);

            ctx.Items.Remove(HttpItemsKey);
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
/// DelegatingHandler que registra HTTP saliente. Mantiene compatibilidad:
/// - Si el buffer por-request está activo, encola el bloque en la ventana dinámica
///   (queda entre 4 y 5, ordenado por el INICIO real del request).
/// - Si no hay buffer, acumula en HttpContext.Items (legacy) para que el middleware
///   lo recoja como siempre (sin cambios para el consumidor de la librería).
/// </summary>
public sealed class HttpClientLoggingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    private const int MaxBodyChars = 20000; // Límite opcional para evitar logs gigantes

    /// <summary>
    /// Intercepta la petición/respuesta, arma un bloque único y lo publica
    /// en el buffer dinámico o en Items (legacy) según disponibilidad.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _accessor.HttpContext; // Puede ser null fuera del pipeline
        var traceId = ctx?.TraceIdentifier ?? Guid.NewGuid().ToString();

        // Sello del INICIO del request HTTP — clave para el orden correcto.
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
                // Cuerpos no re-leibles o flujos cerrados no deben romper el logging.
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

            // ===== Compatibilidad dual =====
            if (HasRequestBuffer(ctx))
            {
                // En buffer: quedará entre 4 y 5 y se ordenará por "startedUtc".
                LogHelper.AppendDynamicCompatAt(ctx, null, DynamicLogKind.HttpClient, block, startedUtc);
            }
            else if (ctx is not null)
            {
                // Legacy: acumula en Items con timestamp para que Flush lo importe y ordene.
                AppendHttpClientLogToContextTimed(ctx, startedUtc, block);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            // Bloque de error: conserva contexto de la petición y tiempo transcurrido.
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

            if (HasRequestBuffer(ctx))
            {
                LogHelper.AppendDynamicCompatAt(ctx, null, DynamicLogKind.HttpClient, errorBlock, startedUtc);
            }
            else if (ctx is not null)
            {
                AppendHttpClientLogToContextTimed(ctx, startedUtc, errorBlock);
            }

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

    /// <summary>
    /// Compatibilidad: agrega un registro a Items con timestamp de INICIO para que
    /// el Flush pueda importarlo y ordenar correctamente entre 4 y 5.
    /// </summary>
    private static void AppendHttpClientLogToContextTimed(HttpContext context, DateTime startedUtc, string logEntry)
    {
        // Tipo local privado para no acoplar al helper (se importará por reflexión).
        var entry = new PrivateEntry(startedUtc, logEntry);

        const string keyTimed = "HttpClientLogsTimed";
        if (!context.Items.ContainsKey(keyTimed)) context.Items[keyTimed] = new List<object>();
        if (context.Items[keyTimed] is List<object> list) list.Add(entry);
    }

    /// <summary>
    /// Tipo privado con TsUtc/Content para almacenar entradas temporales en Items.
    /// El helper lo importará mediante reflexión (sin necesidad de referenciar este tipo).
    /// </summary>
    private sealed class PrivateEntry(DateTime tsUtc, string content)
    {
        public DateTime TsUtc { get; } = tsUtc;
        public string Content { get; } = content;
    }

    /// <summary>Limita el tamaño de texto para evitar logs desmesurados.</summary>
    private static string Limit(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Length <= maxChars) return text;
        return text[..maxChars] + "… [truncado]";
    }
}



using System.Data.Common;
using Microsoft.AspNetCore.Http;

namespace Logging.Decorators;

/// <summary>
/// Wrapper mínimo para DbCommand que agrega logging SQL ordenado por el INICIO de la ejecución.
/// Solo ilustra el patrón de sellar con startedUtc y publicar el bloque en la ventana dinámica.
/// </summary>
public sealed class LoggingDbCommandWrapper(DbCommand inner, IHttpContextAccessor accessor)
{
    private readonly DbCommand _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly IHttpContextAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    /// <summary>
    /// Ejecuta NonQuery y registra el bloque SQL en la ventana dinámica (o legacy) anclado al INICIO.
    /// </summary>
    public int ExecuteNonQueryLogged()
    {
        var startedUtc = DateTime.UtcNow; // ⬅️ sello de INICIO para el orden
        try
        {
            var rows = _inner.ExecuteNonQuery();

            var block = LogFormatter.FormatDbExecution(new SqlLogModel
            {
                CommandText = _inner.CommandText,
                ElapsedMs = 0, // si lo mides, agrega el tiempo real
                // ... completa con tus datos actuales (DB, tabla, etc.)
            });

            Logging.Helpers.LogHelper.AppendDynamicCompatAt(
                _accessor.HttpContext, null,
                Logging.Helpers.LogHelper.DynamicLogKind.Sql,
                block, startedUtc);

            return rows;
        }
        catch (Exception ex)
        {
            var errorBlock = LogFormatter.FormatDbExecutionError(
                nombreBD: "Desconocida",
                ip: "-",
                puerto: 0,
                biblioteca: "-",
                tabla: Logging.Helpers.LogHelper.ExtractTableName(_inner.CommandText),
                sentenciaSQL: _inner.CommandText,
                exception: ex,
                horaError: DateTime.Now
            );

            Logging.Helpers.LogHelper.AppendDynamicCompatAt(
                _accessor.HttpContext, null,
                Logging.Helpers.LogHelper.DynamicLogKind.Sql,
                errorBlock, startedUtc);

            throw;
        }
    }
}






