using System;
using System.Globalization;
using System.Text;
using System.Globalization;
using Newtonsoft.Json;

namespace JsonNetConverters
{
    /// <summary>
    /// Trunca strings a un tamaño máximo al DESERIALIZAR con Newtonsoft.Json.
    /// - No modifica el valor al serializar (Write).
    /// - Opcionalmente hace Trim antes de truncar.
    /// - Opcionalmente respeta "text elements" (emojis/acentos compuestos).
    /// </summary>
    public sealed class MaxLengthStringJsonConverter : JsonConverter
    {
        private readonly int _maxLength;
        private readonly bool _trimBeforeTruncate;
        private readonly bool _useTextElements;

        /// <param name="maxLength">Longitud máxima a conservar (>= 0).</param>
        /// <param name="trimBeforeTruncate">Si true, aplica Trim() antes de truncar.</param>
        /// <param name="useTextElements">
        /// Si true, usa <see cref="StringInfo"/> para no partir emojis/acentos combinados
        /// (más costoso; úsalo solo si lo necesitas).
        /// </param>
        public MaxLengthStringJsonConverter(int maxLength, bool trimBeforeTruncate = true, bool useTextElements = false)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength debe ser >= 0.");

            _maxLength = maxLength;
            _trimBeforeTruncate = trimBeforeTruncate;
            _useTextElements = useTextElements;
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null; // respeta nulos si vienen en el JSON

            // Toma el valor como string; si viene un número, lo convierte.
            var s = reader.Value?.ToString() ?? string.Empty;

            if (_trimBeforeTruncate)
                s = s.Trim();

            if (_maxLength == 0 || s.Length == 0)
                return _maxLength == 0 ? string.Empty : s;

            if (_useTextElements)
            {
                var si = new StringInfo(s);
                return si.LengthInTextElements <= _maxLength
                    ? s
                    : si.SubstringByTextElements(0, _maxLength);
            }

            return s.Length > _maxLength ? s.Substring(0, _maxLength) : s;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((string?)value);
        }
    }
}
using Newtonsoft.Json;
using JsonNetConverters;

public class MiRespuestaDto
{
    /// <summary>Se trunca a máx. 8 caracteres al deserializar.</summary>
    [JsonConverter(typeof(MaxLengthStringJsonConverter), 8 /* maxLength */, true /* trim */, false /* graphemes */)]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Ejemplo con 12 y sin Trim previo.</summary>
    [JsonConverter(typeof(MaxLengthStringJsonConverter), 12, false)]
    public string Descripcion { get; set; } = string.Empty;
}





