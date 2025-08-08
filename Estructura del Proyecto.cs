using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Logging.Services
{
    /// <summary>
    /// Servicio de logging. Genera rutas de log con estructura Api/Endpoint/AAAA-MM-DD y extensión .txt,
    /// soportando sufijos personalizados mediante el atributo <c>[LogFileName]</c>.
    /// </summary>
    public class LoggingService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _basePath;

        // === Ajustes explícitos de formato/estructura ===
        private const string LogExtension = ".txt"; // Fuerza .txt
        private const string FallbackFileName = "GlobalManualLogs.txt";

        public LoggingService(IHttpContextAccessor httpContextAccessor, string basePath)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        /// <summary>
        /// Obtiene la ruta completa del archivo de log para la petición actual con estructura:
        /// <c>{BasePath}/{Api}/{Endpoint}/{yyyy-MM-dd}/{Endpoint}_{HHmmssffff}[_{Custom}].txt</c>.
        /// Omite Swagger, usa hora local America/Tegucigalpa y cachea el resultado en <c>HttpContext.Items["LogFileName"]</c>.
        /// </summary>
        public string GetCurrentLogFile()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
                return Path.Combine(_basePath, FallbackFileName);

            // Reutiliza si ya existía, pero normaliza a .txt y estructura definida.
            if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
                existingObj is string existingPath &&
                !string.IsNullOrWhiteSpace(existingPath))
            {
                var normalized = NormalizeToTxtAndStructure(existingPath, context);
                context.Items["LogFileName"] = normalized;
                return normalized;
            }

            if (IsSwaggerRequest(context))
            {
                var ignored = Path.Combine(_basePath, "Ignored", "SwaggerIgnored.txt");
                EnsureDirectoryExists(Path.GetDirectoryName(ignored)!);
                context.Items["LogFileName"] = ignored;
                return ignored;
            }

            var (apiName, endpointName) = GetRouteNames(context);
            var now = GetLocalNow(); // America/Tegucigalpa
            var dateFolder = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Sufijo opcional desde [LogFileName] o Items["LogCustomPart"]
            string? customPart = null;
            if (context.Items.TryGetValue("LogFileNameObject", out var dto1) && dto1 is not null)
                customPart = GetLogFileNameValue(dto1);
            if (string.IsNullOrWhiteSpace(customPart) &&
                context.Items.TryGetValue("RequestBodyObject", out var dto2) && dto2 is not null)
                customPart = GetLogFileNameValue(dto2);
            if (string.IsNullOrWhiteSpace(customPart) &&
                context.Items.TryGetValue("LogCustomPart", out var partObj) && partObj is string partStr)
                customPart = partStr;

            var timeStamp = now.ToString("HHmmssffff", CultureInfo.InvariantCulture);
            var safeApi = SanitizeFileName(apiName);
            var safeEndpoint = SanitizeFileName(endpointName);

            // Nombre de archivo: Endpoint_HHmmssffff[_Custom].txt
            var fileName = string.IsNullOrWhiteSpace(customPart)
                ? $"{safeEndpoint}_{timeStamp}{LogExtension}"
                : $"{safeEndpoint}_{timeStamp}_{SanitizeFileName(customPart)}{LogExtension}";

            // Carpeta: Base/Api/Endpoint/yyyy-MM-dd
            var folder = Path.Combine(_basePath, safeApi, safeEndpoint, dateFolder);
            EnsureDirectoryExists(folder);

            var fullPath = Path.Combine(folder, fileName);
            context.Items["LogFileName"] = fullPath;
            return fullPath;
        }

        /// <summary>
        /// Fuerza la extensión .txt y (si detecta que la ruta no sigue la estructura base)
        /// reconstruye la ruta con la estructura Api/Endpoint/AAAA-MM-DD.
        /// </summary>
        private string NormalizeToTxtAndStructure(string currentPath, HttpContext context)
        {
            // 1) Forzar extensión .txt
            var txtPath = Path.ChangeExtension(currentPath, LogExtension);

            // 2) Verificar estructura (…/Api/Endpoint/AAAA-MM-DD/Archivo.txt).
            var (apiName, endpointName) = GetRouteNames(context);
            var now = GetLocalNow();
            var dateFolder = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var expectedFolder = Path.Combine(_basePath,
                                              SanitizeFileName(apiName),
                                              SanitizeFileName(endpointName),
                                              dateFolder);

            // Si currentPath ya está dentro de expectedFolder y termina en .txt, úsalo.
            var folder = Path.GetDirectoryName(txtPath) ?? string.Empty;
            if (folder.Replace('\\', '/').EndsWith(expectedFolder.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)
                && txtPath.EndsWith(LogExtension, StringComparison.OrdinalIgnoreCase))
            {
                EnsureDirectoryExists(folder);
                return txtPath;
            }

            // 3) Si no cumple, reconstruir con nombre válido.
            var safeEndpoint = SanitizeFileName(endpointName);
            var timeStamp = now.ToString("HHmmssffff", CultureInfo.InvariantCulture);

            // Intenta mantener algún identificador del archivo original como sufijo (sin extensión).
            var originalName = Path.GetFileNameWithoutExtension(txtPath);
            var safeOriginal = string.IsNullOrWhiteSpace(originalName) ? null : SanitizeFileName(originalName);

            var newFileName = safeOriginal is null
                ? $"{safeEndpoint}_{timeStamp}{LogExtension}"
                : $"{safeEndpoint}_{timeStamp}_{safeOriginal}{LogExtension}";

            EnsureDirectoryExists(expectedFolder);
            return Path.Combine(expectedFolder, newFileName);
        }

        /// <summary>
        /// Extrae un valor de una propiedad con <c>[LogFileName]</c> en un objeto,
        /// solo considerando propiedades públicas de instancia sin parámetros (no indexers).
        /// </summary>
        private string? GetLogFileNameValue(object obj)
        {
            if (obj is null) return null;

            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (prop.GetIndexParameters().Length != 0) continue;
                var hasAttr = prop.GetCustomAttribute(typeof(LogFileNameAttribute), inherit: true) is not null;
                if (!hasAttr) continue;

                try
                {
                    var raw = prop.GetValue(obj);
                    if (raw is null) continue;
                    var str = Convert.ToString(raw, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(str))
                        return SanitizeFileName(str!.Trim());
                }
                catch
                {
                    // Ignorar y continuar
                }
            }
            return null;
        }

        /// <summary>Determina si es una petición de Swagger.</summary>
        private static bool IsSwaggerRequest(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            return path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/swagger/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Obtiene nombres Api y Endpoint desde RouteData (controller/action) o desde la URL.
        /// </summary>
        private static (string ApiName, string EndpointName) GetRouteNames(HttpContext context)
        {
            var controller = context.Request.RouteValues.TryGetValue("controller", out var c) ? c?.ToString() : null;
            var action = context.Request.RouteValues.TryGetValue("action", out var a) ? a?.ToString() : null;

            if (!string.IsNullOrWhiteSpace(controller) && !string.IsNullOrWhiteSpace(action))
                return (controller!, action!);

            var segments = (context.Request.Path.Value ?? string.Empty)
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            var api = segments.Length > 0 ? segments[0] : "UnknownApi";
            var endpoint = segments.Length > 1 ? segments[1] : "UnknownEndpoint";
            return (api, endpoint);
        }

        /// <summary>Devuelve fecha/hora local America/Tegucigalpa (fallback: UTC).</summary>
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

        /// <summary>Crea el directorio si no existe.</summary>
        private static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// Sanea un nombre para uso en el sistema de archivos.
        /// Reemplaza caracteres inválidos por <c>_</c> y recorta extremos.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return sanitized.Trim('_');
        }
    }

    /// <summary>
    /// Atributo para indicar qué propiedad del modelo debe usarse como parte del nombre del archivo de log.
    /// Debe aplicarse únicamente a propiedades públicas sin parámetros (no indexers).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class LogFileNameAttribute : Attribute {}
}
