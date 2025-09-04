using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Logging.Helpers;

/// <summary>
/// Extensión (partial) que agrega una "ventana dinámica" de logging entre
/// Request Info (4) y Response Info (5), preservando el orden real de ejecución.
/// </summary>
public static partial class LogHelper
{
    // Clave interna para guardar el buffer por-request en HttpContext.Items.
    private const string BufferKey = "__ReqLogBuffer";

    /// <summary>
    /// Tipos de eventos dinámicos que pueden ocurrir entre 4) y 5).
    /// La clasificación NO altera la posición fija, solo etiqueta el segmento.
    /// </summary>
    public enum DynamicLogKind
    {
        /// <summary>Eventos de HttpClient (salientes).</summary>
        HttpClient = 1,

        /// <summary>Eventos de SQL (SELECT/DML/SP/CL/PGM).</summary>
        Sql = 2,

        /// <summary>Entradas de texto manuales (AddSingleLog).</summary>
        ManualSingle = 3,

        /// <summary>Entradas de objeto/estructura (AddObjLog).</summary>
        ManualObject = 4,

        /// <summary>Bloques agregados con StartLogBlock/Add/End.</summary>
        ManualBlock = 5,

        /// <summary>Reservado para extensiones personalizadas.</summary>
        Custom = 99
    }

    /// <summary>
    /// Segmento dinámico atómico con sello temporal y secuencia incremental.
    /// Se ordena por tiempo de creación (UTC) y luego por secuencia local.
    /// </summary>
    private sealed class DynamicLogSegment(DynamicLogKind kind, DateTime timestampUtc, int sequence, string content)
    {
        public DynamicLogKind Kind { get; } = kind;          // Clasificación (HTTP/SQL/etc.)
        public DateTime TimestampUtc { get; } = timestampUtc; // Momento real de generación del segmento
        public int Sequence { get; } = sequence;              // Empate estable en alta concurrencia
        public string Content { get; } = content;             // Texto ya renderizado (no se altera)
        public static DynamicLogSegment Create(DynamicLogKind k, int seq, string text)
            => new(k, DateTime.UtcNow, seq, text);
    }

    /// <summary>
    /// Buffer por-request que mantiene los slots FIJOS 2-6 (Environment, Controller, Request, Response, Errores)
    /// y una cola de eventos DINÁMICOS que siempre se ubican ENTRE Request(4) y Response(5).
    /// </summary>
    private sealed class RequestLogBuffer(string filePath)
    {
        public string FilePath { get; } = filePath; // Archivo de destino para el flush único

        // Secuencia incremental local para desempates en dinámica.
        private int _seq;

        // Segmentos dinámicos; su orden final depende de TimestampUtc y Sequence.
        private readonly ConcurrentQueue<DynamicLogSegment> _dynamic = new();

        // Slots fijos 2-6 (1 y 7 — Inicio/Fin — los puedes seguir escribiendo como hoy).
        public string? FixedEnvironment { get; private set; }
        public string? FixedController { get; private set; }
        public string? FixedRequest { get; private set; }
        public string? FixedResponse { get; private set; }
        public List<string> FixedErrors { get; } = [];

        /// <summary>Coloca/actualiza el bloque fijo "Environment Info" (2).</summary>
        public void SetEnvironment(string content) => FixedEnvironment = content;

        /// <summary>Coloca/actualiza el bloque fijo "Controlador" (3).</summary>
        public void SetController(string content) => FixedController = content;

        /// <summary>Coloca/actualiza el bloque fijo "Request Info" (4).</summary>
        public void SetRequest(string content) => FixedRequest = content;

        /// <summary>Coloca/actualiza el bloque fijo "Response Info" (5).</summary>
        public void SetResponse(string content) => FixedResponse = content;

        /// <summary>Agrega un bloque fijo de "Error" (6). Puede haber varios.</summary>
        public void AddError(string content) => FixedErrors.Add(content);

        /// <summary>
        /// Inserta un evento dinámico dentro de la ventana (entre 4 y 5).
        /// El orden final respeta hora real de ejecución y secuencia local.
        /// </summary>
        public void AppendDynamic(DynamicLogKind kind, string content)
        {
            var seq = Interlocked.Increment(ref _seq);
            _dynamic.Enqueue(DynamicLogSegment.Create(kind, seq, content));
        }

        /// <summary>
        /// Construye el bloque central 2→6 con la ventana dinámica en medio:
        /// 2) Environment → 3) Controlador → 4) Request → [DINÁMICOS] → 5) Response → 6) Errores.
        /// </summary>
        public string BuildCore()
        {
            // Drenamos y ordenamos la porción dinámica por tiempo real y secuencia.
            List<DynamicLogSegment> dyn = [];
            while (_dynamic.TryDequeue(out var seg)) dyn.Add(seg);

            var dynOrdered = dyn
                .OrderBy(s => s.TimestampUtc)
                .ThenBy(s => s.Sequence)
                .ToList();

            // Ensamblado del bloque central.
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

    // ===================== API PÚBLICA DEL BUFFER =====================

    /// <summary>
    /// Obtiene (o crea) el buffer por-request. Si <paramref name="filePath"/> viene vacío,
    /// se deriva automáticamente desde el <see cref="HttpContext"/> con <c>GetPathFromContext</c>.
    /// </summary>
    public static object? GetOrCreateBuffer(HttpContext? ctx, string? filePath, out RequestLogBuffer? buffer)
    {
        buffer = null;
        if (ctx is null) return null;

        // Determina ruta del archivo final (usa tu lógica existente si no la pasan).
        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath;

        if (ctx.Items.TryGetValue(BufferKey, out var existing) && existing is RequestLogBuffer ok)
        {
            buffer = ok;
            return ok;
        }

        var created = new RequestLogBuffer(path);
        ctx.Items[BufferKey] = created;
        buffer = created;
        return created;
    }

    /// <summary>Slot 2) Environment Info.</summary>
    public static void SetEnvironment(HttpContext ctx, string? filePath, string text)
    {
        GetOrCreateBuffer(ctx, filePath, out var buf);
        buf?.SetEnvironment(text);
    }

    /// <summary>Slot 3) Controlador.</summary>
    public static void SetController(HttpContext ctx, string? filePath, string text)
    {
        GetOrCreateBuffer(ctx, filePath, out var buf);
        buf?.SetController(text);
    }

    /// <summary>Slot 4) Request Info.</summary>
    public static void SetRequest(HttpContext ctx, string? filePath, string text)
    {
        GetOrCreateBuffer(ctx, filePath, out var buf);
        buf?.SetRequest(text);
    }

    /// <summary>Slot 5) Response Info.</summary>
    public static void SetResponse(HttpContext ctx, string? filePath, string text)
    {
        GetOrCreateBuffer(ctx, filePath, out var buf);
        buf?.SetResponse(text);
    }

    /// <summary>Slot 6) Errores (acumulable).</summary>
    public static void AddError(HttpContext ctx, string? filePath, string text)
    {
        GetOrCreateBuffer(ctx, filePath, out var buf);
        buf?.AddError(text);
    }

    /// <summary>
    /// Agrega un evento dinámico que SIEMPRE quedará entre 4) Request y 5) Response,
    /// ordenado por momento de ejecución real.
    /// </summary>
    public static void AppendDynamic(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text)
    {
        if (ctx is null) return;
        GetOrCreateBuffer(ctx, filePath, out var buf);
        buf?.AppendDynamic(kind, text);
    }

    /// <summary>
    /// FLUSH ÚNICO: construye el bloque 2→6 (con ventana dinámica) y lo escribe una sola vez
    /// en el archivo final. Puedes seguir escribiendo Inicio(1) y Fin(7) como hoy.
    /// </summary>
    public static void Flush(HttpContext ctx, string? filePath)
    {
        if (ctx is null) return;
        GetOrCreateBuffer(ctx, filePath, out var buf);
        if (buf is null) return;

        var core = buf.BuildCore();

        // Escribe usando tu propia primitiva (ya maneja creación de carpeta, share, etc.)
        var path = buf.FilePath;
        var dir = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;

        // Escritura TXT
        WriteLogToFile(dir, path, core);

        // Opcional: activar CSV automático
        // SaveLogAsCsv(dir, path, core);
    }
}
