using System.Text.Json.Serialization;
using JsonConverters;

public class MiRespuestaDto
{
    /// <summary>
    /// Se trunca a máx. 8 caracteres al deserializar JSON.
    /// </summary>
    [JsonConverter(typeof(MaxLengthStringJsonConverter), 8 /* maxLength */, true /* trim */, false /* graphemes */)]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Otro ejemplo con tamaño distinto (máx. 12) y sin Trim previo.
    /// </summary>
    [JsonConverter(typeof(MaxLengthStringJsonConverter), 12, false)]
    public string Descripcion { get; set; } = string.Empty;
}
