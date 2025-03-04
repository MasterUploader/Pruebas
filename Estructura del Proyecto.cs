using System;
using System.Text;

namespace Logging.Extensions
{
    /// <summary>
    /// Métodos de extensión para manipulación de cadenas en el logging.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Indenta cada línea de un texto con un número de espacios determinado.
        /// </summary>
        /// <param name="text">Texto a formatear.</param>
        /// <param name="level">Nivel de indentación (número de espacios).</param>
        /// <returns>Texto indentado.</returns>
        public static string Indent(this string text, int level = 4)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Crea el prefijo de indentación basado en el nivel
            string indentation = new string(' ', level);
            
            // Aplica la indentación a cada línea del texto
            return indentation + text.Replace("\n", "\n" + indentation);
        }

        /// <summary>
        /// Normaliza los espacios en blanco dentro de una cadena eliminando espacios extra.
        /// </summary>
        /// <param name="text">Texto a limpiar.</param>
        /// <returns>Texto sin espacios en blanco excesivos.</returns>
        public static string NormalizeWhitespace(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Reemplaza múltiples espacios por un solo espacio
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        }

        /// <summary>
        /// Convierte una cadena en formato JSON a una versión legible con sangría.
        /// </summary>
        /// <param name="json">Cadena JSON sin formato.</param>
        /// <returns>Cadena JSON formateada.</returns>
        public static string FormatJson(this string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;

            try
            {
                var jsonObject = System.Text.Json.JsonDocument.Parse(json);
                return System.Text.Json.JsonSerializer.Serialize(jsonObject, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                return json; // Si el formato es incorrecto, retorna el JSON sin cambios.
            }
        }

        /// <summary>
        /// Convierte una cadena XML en una versión legible con sangría.
        /// </summary>
        /// <param name="xml">Cadena XML sin formato.</param>
        /// <returns>Cadena XML formateada.</returns>
        public static string FormatXml(this string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return string.Empty;

            try
            {
                var xmlDocument = new System.Xml.XmlDocument();
                xmlDocument.LoadXml(xml);
                using var stringWriter = new StringWriter();
                using var xmlTextWriter = new System.Xml.XmlTextWriter(stringWriter) { Formatting = System.Xml.Formatting.Indented };
                xmlDocument.WriteContentTo(xmlTextWriter);
                return stringWriter.ToString();
            }
            catch
            {
                return xml; // Si el formato es incorrecto, retorna el XML sin cambios.
            }
        }
    }
}
