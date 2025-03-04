using System;
using System.IO;

namespace Logging.Helpers
{
    /// <summary>
    /// Métodos auxiliares para manejar validaciones y formato de logs.
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Verifica si una ruta de archivo es válida.
        /// </summary>
        public static bool IsValidFilePath(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) && filePath.IndexOfAny(Path.GetInvalidPathChars()) < 0;
        }

        /// <summary>
        /// Maneja excepciones sin detener la ejecución.
        /// </summary>
        public static void SafeExecute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG ERROR]: {ex.Message}");
            }
        }
    }
}
