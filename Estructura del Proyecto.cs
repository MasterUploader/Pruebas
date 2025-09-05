using System.Threading; // AsyncLocal

namespace Logging.Helpers;

public static partial class LogHelper
{
    // Mantiene referencia al buffer del request a través de awaits/Task.Run (sin HttpContext).
    private static readonly AsyncLocal<object?> _ambient = new();

    // Intenta devolver un buffer YA EXISTENTE (del HttpContext o del ambient). No crea nada.
    private static RequestLogBuffer? TryGetExistingBuffer(HttpContext? ctx)
    {
        if (ctx is not null &&
            ctx.Items.TryGetValue(BufferKey, out var existing) &&
            existing is RequestLogBuffer ok)
            return ok;

        return _ambient.Value as RequestLogBuffer;
    }

    // Ajusta tu GetOrCreateBuffer privado para publicar el buffer en el ambient si se crea.
    private static RequestLogBuffer? GetOrCreateBuffer(HttpContext? ctx, string? filePath)
    {
        if (ctx is null)
        {
            // Sin contexto: reusa el ambient si existe (no crea)
            return _ambient.Value as RequestLogBuffer;
        }

        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath;

        if (ctx.Items.TryGetValue(BufferKey, out var existing) && existing is RequestLogBuffer ok)
        {
            _ambient.Value = ok;      // publica en ambient
            return ok;
        }

        var created = new RequestLogBuffer(path!);
        ctx.Items[BufferKey] = created;
        _ambient.Value       = created; // publica en ambient
        return created;
    }

    /// <summary>
    /// Agrega un evento dinámico con **compatibilidad hacia atrás**:
    /// - Si hay buffer por-request, se encola (quedará entre 4 y 5 ordenado por tiempo).
    /// - Si NO hay buffer, escribe de inmediato como hacía el código legacy.
    /// </summary>
    public static void AppendDynamicCompat(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text)
    {
        // 1) Caso ideal: ya hay buffer → encolar y listo.
        var buf = TryGetExistingBuffer(ctx);
        if (buf is not null)
        {
            buf.AppendDynamic(kind, text);
            return;
        }

        // 2) Legacy (sin buffer): escribir directo como antes SIN cambiar a quien consume la librería.
        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath!;
        var dir  = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;

        WriteLogToFile(dir, path, text);
        // Si quieres, aquí puedes activar el CSV legacy:
        // SaveLogAsCsv(dir, path, text);
    }
}



// … construir el bloque 'block' con request/response como ya lo tienes …

// Queda EN ORDEN cuando hay buffer; si no hay, escribe directo (legacy).
LogHelper.AppendDynamicCompat(_accessor?.HttpContext, filePath: null, 
                              LogHelper.DynamicLogKind.HttpClient, block);




LogHelper.AppendDynamicCompat(_accessor?.HttpContext, null, 
                              LogHelper.DynamicLogKind.HttpClient, errorBlock);
