using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace Logging.Services
{
    /// <summary>
    /// Servicio de logging. Genera el nombre de archivo de log por petición,
    /// soportando un valor personalizado obtenido mediante el atributo <c>[LogFileName]</c>
    /// sobre una propiedad (sin parámetros) del DTO del request.
    /// </summary>
    public class LoggingService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _basePath;

        public LoggingService(IHttpContextAccessor httpContextAccessor, string basePath)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        /// <summary>
        /// Obtiene el archivo de log de la petición actual, garantizando que toda la información
        /// se guarde en el mismo archivo. Se organiza por API, endpoint y fecha local (America/Tegucigalpa).
        /// Si existe un valor marcado con <c>[LogFileName]</c> en el body del request (u objeto similar),
        /// se concatena al nombre del archivo para mejorar la trazabilidad.
        /// </summary>
        /// <remarks>
        /// - Omite peticiones a Swagger.<br/>
        /// - Usa <c>HttpContext.Items["LogFileName"]</c> como cache para no recalcular varias veces.<br/>
        /// - Sanea nombres para evitar caracteres inválidos en el sistema de archivos.
        /// </remarks>
        /// <returns>Ruta completa del archivo de log para la petición actual.</returns>
        public string GetCurrentLogFile()
        {
            var context = _httpContextAccessor.HttpContext;

            // Fallback global si no hay HttpContext (escenarios fuera de pipeline HTTP).
            if (context is null)
                return Path.Combine(_basePath, "GlobalManualLogs.txt");

            // 1) Reutiliza si ya fue calculado en esta petición.
            if (context.Items.TryGetValue("LogFileName", out var existing) && existing is string existingPath && !string.IsNullOrWhiteSpace(existingPath))
                return existingPath;

            // 2) Evita generar logs para Swagger.
            if (IsSwaggerRequest(context))
            {
                // Opcional: si deseas NO crear archivo, retorna una ruta fija fuera de tus carpetas de negocio.
                var ignored = Path.Combine(_basePath, "Ignored", "SwaggerIgnored.txt");
                EnsureDirectoryExists(Path.GetDirectoryName(ignored)!);
                context.Items["LogFileName"] = ignored;
                return ignored;
            }

            // 3) Componentes de ruta: ApiName, EndpointName y fecha local.
            var (apiName, endpointName) = GetRouteNames(context);
            var now = GetLocalNow(); // Hora local America/Tegucigalpa
            var dateFolder = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // 4) Parte opcional desde atributo [LogFileName] en el DTO u objeto del body.
            //    - Busca por un objeto previamente guardado en Items (idealmente por tu middleware de lectura del body).
            //    - Si no existe, intenta con algún otro objeto que tengas almacenado.
            string? customPart = null;

            if (context.Items.TryGetValue("LogFileNameObject", out var dtoObj) && dtoObj is not null)
            {
                customPart = GetLogFileNameValue(dtoObj);
            }
            else if (context.Items.TryGetValue("RequestBodyObject", out var bodyObj) && bodyObj is not null)
            {
                customPart = GetLogFileNameValue(bodyObj);
            }

            // 5) También permite forzar una parte personalizada vía Items["LogCustomPart"] (si tu pipeline lo setea).
            if (string.IsNullOrWhiteSpace(customPart) &&
                context.Items.TryGetValue("LogCustomPart", out var partObj) &&
                partObj is string partStr && !string.IsNullOrWhiteSpace(partStr))
            {
                customPart = partStr;
            }

            // 6) Construye nombre de archivo: {Endpoint}_{HHmmssffff}[_{Custom}].log
            var timeStamp = now.ToString("HHmmssffff", CultureInfo.InvariantCulture);
            var sanitizedEndpoint = SanitizeFileName(endpointName);
            var fileName = string.IsNullOrWhiteSpace(customPart)
                ? $"{sanitizedEndpoint}_{timeStamp}.log"
                : $"{sanitizedEndpoint}_{timeStamp}_{SanitizeFileName(customPart)}.log";

            // 7) Ruta completa: BasePath/ApiName/Endpoint/yyyy-MM-dd/fileName
            var fullPath = Path.Combine(_basePath,
                                        SanitizeFileName(apiName),
                                        SanitizeFileName(endpointName),
                                        dateFolder,
                                        fileName);

            EnsureDirectoryExists(Path.GetDirectoryName(fullPath)!);

            // 8) Cachea en Items para reutilizar durante toda la petición.
            context.Items["LogFileName"] = fullPath;

            return fullPath;
        }

        /// <summary>
        /// Obtiene el valor marcado con <c>[LogFileName]</c> desde las propiedades públicas
        /// SIN parámetros (evita indexers) del objeto indicado. Devuelve <c>null</c> si no encuentra una propiedad válida.
        /// </summary>
        /// <param name="obj">Objeto del cual extraer el valor (ej. DTO del request).</param>
        /// <returns>Valor de la propiedad anotada, o <c>null</c> si no existe o es vacío.</returns>
        private string? GetLogFileNameValue(object obj)
        {
            if (obj is null) return null;

            var type = obj.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                // Ignora indexers o propiedades con parámetros.
                if (prop.GetIndexParameters().Length != 0)
                    continue;

                // Verifica el atributo [LogFileName] (ajusta el nombre del atributo a tu espacio de nombres real).
                var attr = prop.GetCustomAttribute(typeof(LogFileNameAttribute), inherit: true);
                if (attr is null) continue;

                try
                {
                    var raw = prop.GetValue(obj);
                    if (raw is null) continue;

                    // Convierte a string y sanea.
                    var str = Convert.ToString(raw, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(str))
                        return str!.Trim();
                }
                catch
                {
                    // Evita romper el flujo si alguna propiedad lanza excepción.
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Determina si la petición actual corresponde a Swagger.
        /// </summary>
        /// <param name="context">Contexto HTTP actual.</param>
        /// <returns><c>true</c> si es Swagger; en caso contrario <c>false</c>.</returns>
        private static bool IsSwaggerRequest(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            return path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/swagger/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resuelve nombres de API y Endpoint a partir de la ruta y/o metadatos de endpoint.
        /// </summary>
        /// <param name="context">Contexto HTTP.</param>
        /// <returns>Tupla (ApiName, EndpointName).</returns>
        private static (string ApiName, string EndpointName) GetRouteNames(HttpContext context)
        {
            // Intenta desde RouteData (MVC): controller y action.
            var controller = context.Request.RouteValues.TryGetValue("controller", out var c) ? c?.ToString() : null;
            var action = context.Request.RouteValues.TryGetValue("action", out var a) ? a?.ToString() : null;

            if (!string.IsNullOrWhiteSpace(controller) && !string.IsNullOrWhiteSpace(action))
                return (controller!, action!);

            // Fallback: primeros segmentos de la URL (/Api/Endpoint/...)
            var segments = (context.Request.Path.Value ?? string.Empty)
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            var api = segments.Length > 0 ? segments[0] : "UnknownApi";
            var endpoint = segments.Length > 1 ? segments[1] : "UnknownEndpoint";

            return (api, endpoint);
        }

        /// <summary>
        /// Obtiene la fecha y hora local para Honduras (America/Tegucigalpa).
        /// Intenta IANA y si no está disponible, utiliza el ID de zona horaria de Windows.
        /// </summary>
        /// <returns>Fecha y hora local de Tegucigalpa.</returns>
        private static DateTime GetLocalNow()
        {
            try
            {
                // Linux/containers: IANA
                var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Tegucigalpa");
                return TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
            }
            catch
            {
                try
                {
                    // Windows: ID de zona horaria de Windows
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                    return TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
                }
                catch
                {
                    // Fallback: UTC si no se encontró zona.
                    return DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Crea el directorio si no existe.
        /// </summary>
        /// <param name="dir">Ruta del directorio.</param>
        private static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// Sanea un nombre para uso seguro en sistema de archivos (reemplaza caracteres inválidos por guión bajo).
        /// </summary>
        /// <param name="name">Nombre original.</param>
        /// <returns>Nombre saneado.</returns>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.Join("_", sanitized.Split(Path.GetInvalidPathChars(), StringSplitOptions.RemoveEmptyEntries))
                         .Trim('_');
        }
    }

    /// <summary>
    /// Atributo para indicar qué propiedad del modelo debe usarse como parte del nombre del archivo de log.
    /// Debe aplicarse únicamente a propiedades públicas sin parámetros (no indexers).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class LogFileNameAttribute : Attribute
    {
    }
}
