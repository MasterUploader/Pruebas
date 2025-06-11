using System.Text.Json;

namespace Logging.Helpers
{
    /// <summary>
    /// Configuraciones comunes de serialización JSON reutilizables.
    /// </summary>
    public static class JsonOptions
    {
        /// <summary>
        /// Opciones para serializar JSON con indentación (pretty print).
        /// </summary>
        public static readonly JsonSerializerOptions PrettyPrint = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        /// <summary>
        /// Opciones compactas (sin espacios ni saltos de línea).
        /// </summary>
        public static readonly JsonSerializerOptions Compact = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        // Puedes agregar otras configuraciones reutilizables aquí si lo deseas
    }
}
