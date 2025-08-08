using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Logging.Services
{
    /// <summary>
    /// Servicio de logging con generación de ruta:
    /// {Base}/{Api}/{Endpoint}/{yyyy-MM-dd}/{Endpoint}_{TraceId}_{Partes}_{yyyyMMdd_HHmmss}.txt
    /// </summary>
    public class LoggingService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _basePath;

        private const string LogExtension = ".txt";
        private const string FallbackFileName = "GlobalManualLogs.txt";

        public LoggingService(IHttpContextAccessor httpContextAccessor, string basePath)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        /// <summary>
        /// Obtiene la ruta completa del archivo de log para la petición actual con estructura:
        /// {Base}/{Api}/{Endpoint}/{yyyy-MM-dd}/{Endpoint}_{TraceId}_{Partes}_{yyyyMMdd_HHmmss}.txt
        /// </summary>
        public string GetCurrentLogFile()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
                return Path.Combine(_basePath, FallbackFileName);

            // Cache por petición
            if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
                existingObj is string existingPath &&
                !string.IsNullOrWhiteSpace(existingPath))
            {
                var normalized = Path.ChangeExtension(existingPath, LogExtension);
                EnsureDirectoryExists(Path.GetDirectoryName(normalized)!);
                return (string)(context.Items["LogFileName"] = normalized);
            }

            if (IsSwaggerRequest(context))
            {
                var ignored = Path.Combine(_basePath, "Ignored", "SwaggerIgnored.txt");
                EnsureDirectoryExists(Path.GetDirectoryName(ignored)!);
                return (string)(context.Items["LogFileName"] = ignored);
            }

            // 1) Resolver Api/Endpoint (preferir plantilla de ruta)
            var (apiName, endpointName) = GetRouteNamesPreferringTemplate(context);

            // 2) Fecha local Tegucigalpa para carpeta/archivo
            var now = GetLocalNow();
            var dateFolder = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // 3) Partes para el nombre
            var traceId = GetTraceId(context); // GUID estable de la petición
            var parts = BuildCustomParts(context); // p.ej. ["Remesa-12345678-20250807_152641"]
            var timeSuffix = now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

            var safeApi = SanitizeFileName(apiName);
            var safeEndpoint = SanitizeFileName(endpointName);

            // Nombre: Endpoint_{TraceId}[_{Parte1}_{Parte2}_... ]_{yyyyMMdd_HHmmss}.txt
            var fileCore = parts.Count > 0
                ? $"{safeEndpoint}_{traceId}_{string.Join("_", parts.Select(SanitizeFileName))}_{timeSuffix}"
                : $"{safeEndpoint}_{traceId}_{timeSuffix}";

            var fileName = $"{fileCore}{LogExtension}";
            var folder = Path.Combine(_basePath, safeApi, safeEndpoint, dateFolder);
            EnsureDirectoryExists(folder);

            var fullPath = Path.Combine(folder, fileName);
            context.Items["LogFileName"] = fullPath;
            return fullPath;
        }

        #region Partes del nombre

        /// <summary>
        /// Construye la lista de partes personalizadas:
        /// 1) Items["LogFileNameParts"] (IEnumerable&lt;string&gt;)
        /// 2) Varias propiedades con [LogFileName] en el DTO (todas, no solo la primera)
        /// 3) Heurísticas del DTO (Remesa-..., etc.)
        /// 4) Items["LogCustomPart"]
        /// </summary>
        private List<string> BuildCustomParts(HttpContext context)
        {
            var parts = new List<string>();

            // (1) Lista explícita desde Items
            if (context.Items.TryGetValue("LogFileNameParts", out var listObj) && listObj is IEnumerable<string> listParts)
                parts.AddRange(listParts.Where(s => !string.IsNullOrWhiteSpace(s)));

            // (2) Varias propiedades con [LogFileName]
            object? dto = null;
            if (context.Items.TryGetValue("LogFileNameObject", out var o1) && o1 is not null) dto = o1;
            else if (context.Items.TryGetValue("RequestBodyObject", out var o2) && o2 is not null) dto = o2;

            if (dto is not null)
            {
                var annotated = GetAllLogFileNameValues(dto);
                if (annotated.Count > 0)
                    parts.AddRange(annotated);

                // (3) Heurísticas: intenta formar "Remesa-<valor>" si existen propiedades conocidas
                var remesa = TryBuildRemesaPart(dto, GetLocalNow());
                if (!string.IsNullOrWhiteSpace(remesa))
                    parts.Add(remesa!);
            }

            // (4) Parte libre
            if (context.Items.TryGetValue("LogCustomPart", out var partObj) && partObj is string part && !string.IsNullOrWhiteSpace(part))
                parts.Add(part);

            // Devolver sin vacíos
            return parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        }

        /// <summary>
        /// Devuelve todas las propiedades públicas sin parámetros marcadas con [LogFileName],
        /// convertidas a string.
        /// </summary>
        private List<string> GetAllLogFileNameValues(object obj)
        {
            var result = new List<string>();
            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                if (prop.GetIndexParameters().Length != 0) continue; // ignora indexers
                var hasAttr = prop.GetCustomAttribute(typeof(LogFileNameAttribute), inherit: true) is not null;
                if (!hasAttr) continue;

                try
                {
                    var raw = prop.GetValue(obj);
                    var str = Convert.ToString(raw, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(str))
                        result.Add(str!.Trim());
                }
                catch { /* ignorar */ }
            }

            return result;
        }

        /// <summary>
        /// Intenta construir un componente "Remesa-XXXX[ -yyyyMMdd_HHmmss ]"
        /// buscando propiedades típicas en el DTO: Remesa, RemesaId, NumeroRemesa, etc.
        /// Si el DTO ya trae su propia fecha/hora, úsala; de lo contrario se usa "now".
        /// </summary>
        private string? TryBuildRemesaPart(object dto, DateTime nowLocal)
        {
            var t = dto.GetType();
            string? remesaVal = GetStringProp(t, dto, ["Remesa", "RemesaId", "NumeroRemesa", "NoRemesa", "IdRemesa"]);
            if (string.IsNullOrWhiteSpace(remesaVal)) return null;

            // Opcional: si el DTO tiene fecha/hora propias
            var fechaStr = GetStringProp(t, dto, ["Fecha", "FechaRemesa", "Date", "FechaOperacion"]);
            var horaStr  = GetStringProp(t, dto, ["Hora", "HoraRemesa", "Time", "HoraOperacion"]);

            string fechaHora;
            if (DateTime.TryParse($"{fechaStr} {horaStr}", out var fhParsed))
            {
                var local = DateTime.SpecifyKind(fhParsed, DateTimeKind.Unspecified);
                fechaHora = local.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            }
            else
            {
                fechaHora = nowLocal.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            }

            return $"Remesa-{remesaVal}-{fechaHora}";
        }

        private static string? GetStringProp(Type t, object obj, IEnumerable<string> names)
        {
            foreach (var n in names)
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p is null || p.GetIndexParameters().Length != 0) continue;
                try
                {
                    var raw = p.GetValue(obj);
                    var str = Convert.ToString(raw, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(str))
                        return str!.Trim();
                }
                catch { /* ignorar */ }
            }
            return null;
        }

        /// <summary>
        /// TraceId estable: usa HttpContext.TraceIdentifier si es GUID; si no, genera uno.
        /// </summary>
        private static string GetTraceId(HttpContext context)
        {
            var id = context.TraceIdentifier;
            if (Guid.TryParse(id, out _)) return id;
            return Guid.NewGuid().ToString();
        }

        #endregion

        #region Resolución de ruta

        /// <summary>
        /// Obtiene Api y Endpoint priorizando el segmento literal de la plantilla de ruta:
        /// - Endpoint = último segmento literal de RoutePattern (p.ej. "Consulta")
        /// - Api = controller (RouteValues["controller"]) o 1er segmento de URL
        /// </summary>
        private static (string ApiName, string EndpointName) GetRouteNamesPreferringTemplate(HttpContext context)
        {
            string? api = context.Request.RouteValues.TryGetValue("controller", out var c) ? c?.ToString() : null;

            // Intentar obtener el texto crudo de la plantilla de la ruta: /Bts/Consulta, etc.
            string? endpointFromTemplate = null;
            if (context.GetEndpoint() is RouteEndpoint re && re.RoutePattern is not null)
            {
                // Tomar el último segmento literal (no parámetro)
                var lastLiteral = re.RoutePattern.PathSegments
                    .Select(seg => seg.Parts.FirstOrDefault(p => p.IsLiteral)?.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .LastOrDefault();

                endpointFromTemplate = lastLiteral; // p.ej. "Consulta"
            }

            // Fallbacks si la plantilla no está disponible (minimal APIs o rutas dinámicas):
            if (string.IsNullOrWhiteSpace(endpointFromTemplate))
                endpointFromTemplate = context.Request.RouteValues.TryGetValue("action", out var a) ? a?.ToString() : null;

            if (string.IsNullOrWhiteSpace(api) || string.IsNullOrWhiteSpace(endpointFromTemplate))
            {
                var segments = (context.Request.Path.Value ?? string.Empty).Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                api ??= segments.Length > 0 ? segments[0] : "UnknownApi";
                endpointFromTemplate ??= segments.Length > 1 ? segments[1] : "UnknownEndpoint";
            }

            return (api!, endpointFromTemplate!);
        }

        #endregion

        #region Utilidades

        private static bool IsSwaggerRequest(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            return path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/swagger/", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime GetLocalNow()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Tegucigalpa");
                return TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
            }
            catch
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                    return TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
                }
                catch
                {
                    return DateTime.UtcNow;
                }
            }
        }

        private static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return sanitized.Trim('_');
        }

        #endregion
    }

    /// <summary>
    /// Atributo para marcar propiedades a incluir en el nombre del archivo de log.
    /// Se permiten múltiples propiedades en el mismo DTO.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class LogFileNameAttribute : Attribute { }
}
