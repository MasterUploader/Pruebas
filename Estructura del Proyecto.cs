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



using System;

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

using Logging.Abstractions;
using Logging.Ordering;
using Microsoft.AspNetCore.Http;

namespace Logging.Helpers;

/// <summary>
/// Extensiones de LogHelper para buffer por-request con ventana dinámica y flush único.
/// </summary>
public static partial class LogHelper
{
    private const string BufferKey = "__ReqLogBuffer";

    /// <summary>Obtiene o crea el buffer por-request, usando y almacenando el FilePath actual.</summary>
    public static RequestLogBuffer? GetOrCreateBuffer(HttpContext? ctx, string filePath)
    {
        if (ctx is null) return null;

        if (ctx.Items.TryGetValue(BufferKey, out var existing) && existing is RequestLogBuffer ok)
            return ok;

        var created = new RequestLogBuffer(filePath);
        ctx.Items[BufferKey] = created;
        return created;
    }

    /// <summary>Slot fijo 2) Environment Info.</summary>
    public static void SetEnvironment(ILoggingService svc, HttpContext ctx, string filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetEnvironment(text);

    /// <summary>Slot fijo 3) Controlador.</summary>
    public static void SetController(ILoggingService svc, HttpContext ctx, string filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetController(text);

    /// <summary>Slot fijo 4) Request Info.</summary>
    public static void SetRequest(ILoggingService svc, HttpContext ctx, string filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetRequest(text);

    /// <summary>Slot fijo 5) Response Info.</summary>
    public static void SetResponse(ILoggingService svc, HttpContext ctx, string filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetResponse(text);

    /// <summary>Slot fijo 6) Errores (acumulable).</summary>
    public static void AddError(ILoggingService svc, HttpContext ctx, string filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.AddError(text);

    /// <summary>Agrega un evento dinámico dentro de la ventana (entre Request y Response).</summary>
    public static void AppendDynamic(ILoggingService svc, HttpContext? ctx, string filePath, DynamicLogKind kind, string text)
    {
        var buf = GetOrCreateBuffer(ctx, filePath);
        if (buf is null) return; // Sin contexto → no se bufferiza (casos fuera de pipeline)

        buf.AppendDynamic(kind, text);
    }

    /// <summary>
    /// FLUSH ÚNICO: construye el bloque central ordenado y lo escribe **una sola vez** con LoggingService,
    /// dejando que este agregue “Inicio de Log” (si es nuevo) y “Fin de Log” (cuando la respuesta ya inició).
    /// </summary>
    public static void Flush(ILoggingService svc, HttpContext ctx, string filePath)
    {
        var buf = GetOrCreateBuffer(ctx, filePath);
        if (buf is null) return;

        var core = buf.BuildCore();

        // Importante: LoggingService.WriteLog agregará Begin/End automáticamente.
        svc.WriteLog(ctx, core);
    }
}






