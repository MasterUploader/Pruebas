using System;
using System.IO;

namespace Logging.Helpers
{
    /// <summary>
    /// Proporciona métodos auxiliares para la gestión y almacenamiento de logs en archivos.
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Escribe un log en un archivo, asegurando que no interrumpa la ejecución si ocurre un error.
        /// </summary>
        /// <param name="logDirectory">Directorio donde se almacenará el archivo de log.</param>
        /// <param name="fileName">Nombre del archivo de log.</param>
        /// <param name="logContent">Contenido del log a escribir.</param>
        public static void WriteLogToFile(string logDirectory, string fileName, string logContent)
        {
            try
            {
                // Asegura que el directorio de logs exista
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Define la ruta completa del archivo de log
                string logFilePath = Path.Combine(logDirectory, fileName);

                // Escribe el contenido en el archivo (append para evitar sobrescribir)
                File.AppendAllText(logFilePath, logContent + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // En caso de error, guarda un log interno para depuración
                LogInternalError(logDirectory, ex);
            }
        }

        /// <summary>
        /// Registra un error interno en un archivo separado ("InternalErrorLog.txt") sin afectar la API.
        /// </summary>
        /// <param name="logDirectory">Directorio donde se almacenará el archivo de errores internos.</param>
        /// <param name="ex">Excepción capturada.</param>
        private static void LogInternalError(string logDirectory, Exception ex)
        {
            try
            {
                // Define la ruta del archivo de errores internos
                string errorLogPath = Path.Combine(logDirectory, "InternalErrorLog.txt");

                // Mensaje de error con timestamp
                string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error en LogHelper: {ex}{Environment.NewLine}";

                // Guarda el error sin interrumpir la ejecución de la API
                File.AppendAllText(errorLogPath, errorMessage);
            }
            catch
            {
                // Evita bucles de error si la escritura en el log interno también falla
            }
        }
    }
}
