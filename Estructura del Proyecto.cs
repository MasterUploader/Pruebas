sigue sin funcionar, te dejo el codigo antiguo que si funcionaba:

﻿using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace RestUtilities.Logging.Handlers;

/// <summary>
/// Handler personalizado para interceptar y registrar llamadas HTTP salientes realizadas mediante HttpClient.
/// Este log se integrará automáticamente con el archivo de log del Middleware.
/// </summary>
public class HttpClientLoggingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpClientLoggingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Intercepta la solicitud y la respuesta del HttpClient, y guarda su información en HttpContext.Items.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var context = _httpContextAccessor.HttpContext;
        string traceId = context?.TraceIdentifier ?? Guid.NewGuid().ToString();

        try
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            string responseBody = response.Content != null
                ? await response.Content.ReadAsStringAsync(cancellationToken)
                : "Sin contenido";

            // 🔹 Formato del log: incluye cuerpo de respuesta bien formateado
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
            if (context != null)
            {
                AppendHttpClientLogToContext(context, formatted);
            }

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

            if (context != null)
            {
                AppendHttpClientLogToContext(context, errorLog);
            }
            throw;
        }
    }

    /// <summary>
    /// Agrega el log de HttpClient a la lista en HttpContext.Items, para que luego sea procesado por el Middleware.
    /// </summary>
    private static void AppendHttpClientLogToContext(HttpContext context, string logEntry)
    {
        const string key = "HttpClientLogs";

        if (!context.Items.ContainsKey(key))
            context.Items[key] = new List<string>();

        if (context.Items[key] is List<string> logs)
            logs.Add(logEntry);
    }
}


Y te dejo el código nuevo:

using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using static Logging.Helpers.LogHelper;

namespace Logging.Handlers;

/// <summary>
/// DelegatingHandler que registra peticiones HTTP salientes y sus respuestas
/// en un bloque único y lo envía a la ventana dinámica del log (entre 4 y 5).
/// </summary>
/// <remarks>
/// - Incluye TraceId, método, URL, cabeceras, cuerpo request/response y duración.
/// - Redacta cabeceras sensibles (Authorization/Proxy-Authorization).
/// - No interrumpe el flujo si algo del logging falla.
/// </remarks>
public sealed class HttpClientLoggingHandler(IHttpContextAccessor? accessor) : DelegatingHandler
{
    // Acceso al HttpContext actual; permite ubicar el log dentro del request en curso.
    private readonly IHttpContextAccessor? _accessor = accessor;

    // Límite de caracteres para cuerpos (evita logs gigantes).
    private const int MaxBodyChars = 20000;

    /// <summary>
    /// Envía la petición, mide duración y registra un bloque único con request/response
    /// en la ventana dinámica del log (entre 4 y 5).
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _accessor?.HttpContext;                         // Puede ser null fuera de pipeline
        var traceId = ctx?.TraceIdentifier ?? Guid.NewGuid().ToString();

        // === Request: prepara cabeceras y cuerpo (si existe) ===
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
                requestBody = "[No disponible para logging]";
            }
        }
        var requestHeaders = RenderHeaders(request.Headers);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            // === Response: prepara cabeceras y cuerpo ===
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

            // === Bloque final (request + response + métrica) ===
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

            // ➜ Ventana dinámica (entre 4 y 5). filePath:null → se resuelve internamente.
            LogHelper.AppendDynamic(ctx, filePath: null, DynamicLogKind.HttpClient, block);

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

            LogHelper.AppendDynamic(ctx, filePath: null, DynamicLogKind.HttpClient, errorBlock);
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
            {
                value = "[REDACTED]";
            }

            sb.Append(key).Append(": ").AppendLine(value);
        }
        return sb.ToString();
    }

    /// <summary>Limita el tamaño de texto para evitar logs desmesurados.</summary>
    private static string Limit(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Length <= maxChars) return text;
        return text[..maxChars] + "… [truncado]";
    }
}


Compara y revisa los cambios para identificar el problema si es por los cambios, porque se empezo a presentar cuando corregimos lo del orden de los logs.
