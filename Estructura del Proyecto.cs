using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonConverters
{
    /// <summary>
    /// Convierte strings truncándolos a un tamaño máximo al DESERIALIZAR.
    /// - No modifica el valor al serializar (Write): se escribe tal cual esté en el DTO.
    /// - Opcionalmente hace Trim antes de truncar.
    /// - Opcionalmente respeta "grapheme clusters" (emojis/acentos compuestos).
    /// </summary>
    public sealed class MaxLengthStringJsonConverter : JsonConverter<string>
    {
        private readonly int _maxLength;
        private readonly bool _trimBeforeTruncate;
        private readonly bool _useTextElements;

        /// <param name="maxLength">Tamaño máximo a conservar (>= 0).</param>
        /// <param name="trimBeforeTruncate">Si true, aplica Trim() antes de truncar.</param>
        /// <param name="useTextElements">
        /// Si true, usa <see cref="StringInfo"/> para no cortar emojis/acentos combinados.
        /// Recomiendo dejarlo en false salvo que lo necesites.
        /// </param>
        public MaxLengthStringJsonConverter(int maxLength, bool trimBeforeTruncate = true, bool useTextElements = false)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength debe ser >= 0.");

            _maxLength = maxLength;
            _trimBeforeTruncate = trimBeforeTruncate;
            _useTextElements = useTextElements;
        }

        /// <inheritdoc/>
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString() ?? string.Empty;

            if (_trimBeforeTruncate)
                s = s.Trim();

            if (_maxLength == 0 || s.Length == 0)
                return _maxLength == 0 ? string.Empty : s;

            // Truncado normal (rápido) o por "text elements" (emojis/acentos)
            return _useTextElements
                ? TruncateByTextElements(s, _maxLength)
                : (s.Length > _maxLength ? s[.._maxLength] : s);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            // No tocamos el valor al serializar; se escribe como esté en el DTO.
            writer.WriteStringValue(value);
        }

        private static string TruncateByTextElements(string s, int maxTextElements)
        {
            var si = new StringInfo(s);
            return si.LengthInTextElements <= maxTextElements
                ? s
                : si.SubstringByTextElements(0, maxTextElements);
        }
    }
}



