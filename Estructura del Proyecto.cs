using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace Logging.Helpers
{
    public static class LogHelper
    {
        /// <summary>
        /// Guarda un log estructurado en un archivo de texto, utilizando el contexto HTTP si está disponible.
        /// </summary>
        /// <param name="formattedLog">El contenido del log ya formateado (por ejemplo, SQL estructurado, logs HTTP, etc.).</param>
        /// <param name="context">
        /// Opcional: contexto HTTP de la solicitud actual. Si se proporciona, se usará para nombrar el archivo de log con TraceId, endpoint, etc.
        /// </param>
        public static void SaveStructuredLog(string formattedLog, HttpContext? context)
        {
            try
            {
                // Obtener ruta del log dinámicamente
                var path = GetPathFromContext(context);

                // Asegurar que el directorio exista
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory!);

                // Guardar el log estructurado
                File.AppendAllText(path, formattedLog + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Temporal: manejo silencioso en caso de error de escritura
                Console.WriteLine($"[LogHelper Error] {ex.Message}");
            }
        }

        /// <summary>
        /// Construye la ruta dinámica para guardar logs basada en el contexto HTTP.
        /// Si no hay contexto, se genera una ruta genérica con timestamp.
        /// </summary>
        /// <param name="context">Contexto HTTP actual (puede ser null).</param>
        /// <returns>Ruta absoluta del archivo de log.</returns>
        private static string GetPathFromContext(HttpContext? context)
        {
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            if (context != null)
            {
                var traceId = context.TraceIdentifier;
                var endpoint = context.Request.Path.Value?.Trim('/').Replace("/", "_") ?? "endpoint";
                var date = DateTime.UtcNow.ToString("yyyyMMdd");

                var filename = $"{traceId}_{endpoint}_{date}.txt";
                return Path.Combine(basePath, filename);
            }

            // Sin contexto: log general
            var genericName = $"GeneralLog_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.txt";
            return Path.Combine(basePath, genericName);
        }
    }
}
