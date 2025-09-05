namespace Logging.Helpers;

public static partial class LogHelper
{
    /// <summary>
    /// Indica si el buffer por-request está activo en este <see cref="HttpContext"/>.
    /// Se usa para decidir si encolamos en la ventana dinámica o caemos al comportamiento legacy.
    /// </summary>
    public static bool HasRequestBuffer(HttpContext? ctx)
        => ctx is not null && ctx.Items.ContainsKey(BufferKey); // BufferKey ya existe en tu partial
}



using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using static Logging.Helpers.LogHelper;

namespace RestUtilities.Logging.Handlers;

/// <summary>
/// DelegatingHandler que registra HTTP saliente. Si el buffer por-request está activo,
/// encola el bloque en la ventana dinámica (queda entre 4 y 5 ordenado). Si no, usa
/// el mecanismo legacy de HttpContext.Items para no romper integraciones existentes.
/// </summary>
public sealed class HttpClientLoggingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    private const int MaxBodyChars = 20000; // Límite de seguridad para cuerpos

    /// <summary>
    /// Intercepta request/response, arma un bloque único y lo envía al buffer dinámico
    /// o a Items (legacy) según disponibilidad. No interrumpe el flujo si el logging falla.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _accessor.HttpContext; // Puede ser null fuera de pipeline
        var traceId = ctx?.TraceIdentifier ?? Guid.NewGuid().ToString();

        // ---- Preparar request ----
        string? requestBody = null;
        if (request.Content is not null)
        {
            try
            {
                var raw = await request.Content.ReadAsStringAsync(cancellationToken);
                requestBody = LogHelper.PrettyPrintAuto(Limit(raw, MaxBodyChars), request.Content.Headers.ContentType?.MediaType);
            }
            catch { requestBody = "[No disponible para logging]"; } // Streams no repetibles, etc.
        }

        var requestHeaders = RenderHeaders(request.Headers);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            // ---- Preparar response ----
            string responseBody;
            try
            {
                var raw = response.Content is null ? "[Sin contenido]" : await response.Content.ReadAsStringAsync(cancellationToken);
                responseBody = LogHelper.PrettyPrintAuto(Limit(raw, MaxBodyChars), response.Content?.Headers?.ContentType?.MediaType);
            }
            catch { responseBody = "[No disponible para logging]"; }

            var responseHeaders = RenderHeaders(response.Headers);

            // ---- Bloque final: request + response + métrica ----
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

            // === Compatibilidad: buffer dinámico si está activo; si no, legacy Items ===
            if (HasRequestBuffer(ctx))
            {
                LogHelper.AppendDynamic(ctx, filePath: null, DynamicLogKind.HttpClient, block); // Ordenado entre 4 y 5
            }
            else if (ctx is not null)
            {
                AppendHttpClientLogToContext(ctx, block); // Legacy, para que el middleware actual lo recoja
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

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
                LogHelper.AppendDynamic(ctx, null, DynamicLogKind.HttpClient, errorBlock);
            else if (ctx is not null)
                AppendHttpClientLogToContext(ctx, errorBlock);

            throw;
        }
    }

    /// <summary>Devuelve cabeceras legibles, redactando las sensibles.</summary>
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

    /// <summary>Compat: agrega el log HTTP a HttpContext.Items para el middleware legacy.</summary>
    private static void AppendHttpClientLogToContext(HttpContext context, string logEntry)
    {
        const string key = "HttpClientLogs"; // misma clave legacy
        if (!context.Items.ContainsKey(key)) context.Items[key] = new List<string>();
        if (context.Items[key] is List<string> logs) logs.Add(logEntry);
    }

    /// <summary>Limita el tamaño de texto para evitar logs desmesurados.</summary>
    private static string Limit(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Length <= maxChars) return text;
        return text[..maxChars] + "… [truncado]";
    }
}
