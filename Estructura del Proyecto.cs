using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Http;

public partial class LoggingService
{
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
        var header = BuildBlockHeader(title);
        LogHelper.SafeWriteLog(_logDirectory, filePath, header);

        return new LogBlock(this, filePath, title);
    }

    /// <summary>
    /// Construye el texto de cabecera para un bloque de log.
    /// </summary>
    private static string BuildBlockHeader(string title)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sb = new StringBuilder();
        sb.AppendLine($"======================== [BEGIN BLOCK] ========================");
        sb.AppendLine($"Título     : {title}");
        sb.AppendLine($"Inicio     : {now}");
        sb.AppendLine($"===============================================================");
        return sb.ToString();
    }

    /// <summary>
    /// Construye el texto de cierre para un bloque de log.
    /// </summary>
    private static string BuildBlockFooter()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sb = new StringBuilder();
        sb.AppendLine($"===============================================================");
        sb.AppendLine($"Fin        : {now}");
        sb.AppendLine($"========================= [END BLOCK] =========================");
        return sb.ToString();
    }

    /// <summary>
    /// Implementación concreta de un bloque de log.
    /// </summary>
    private sealed class LogBlock : ILogBlock
    {
        private readonly LoggingService _svc;
        private readonly string _filePath;
        private readonly string _title;
        private int _ended; // 0 no, 1 sí (para idempotencia)

        public LogBlock(LoggingService svc, string filePath, string title)
        {
            _svc = svc;
            _filePath = filePath;
            _title = title;
        }

        /// <inheritdoc />
        public void Add(string message)
        {
            // cada "Add" es una fila en el mismo archivo, dentro del bloque
            var line = $"• {message}";
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
            var footer = BuildBlockFooter();
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, footer);
        }

        public void Dispose() => End();
    }
}

/// <summary>
/// Contrato para un bloque de log que agrupa múltiples filas con un encabezado y un pie comunes.
/// </summary>
public interface ILogBlock : IDisposable
{
    /// <summary>Agrega una fila de texto al bloque.</summary>
    void Add(string message);

    /// <summary>Agrega una fila logueando un objeto formateado.</summary>
    void AddObj(string name, object obj);

    /// <summary>Agrega una fila con detalle de excepción.</summary>
    void AddException(Exception ex);

    /// <summary>Finaliza el bloque (escribe el pie). Idempotente.</summary>
    void End();
}

// Patrón using: garantiza cierre del bloque aunque haya excepciones
using (var block = _loggingService.StartLogBlock("Proceso de pagos"))
{
    block.Add("Validando entrada");
    block.AddObj("Request", requestDto);
    block.Add("Llamando servicio externo");
    // ...
    block.Add("Proceso finalizado OK");
}

// O manual:
var b = _loggingService.StartLogBlock("Conciliación");
b.Add("Paso 1 completado");
b.Add("Paso 2 completado");
// ...
b.End();
