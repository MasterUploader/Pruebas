using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Convierte strings truncándolos a 8 caracteres.</summary>
public sealed class StringMaxLen8Converter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString() ?? string.Empty;
        return s.Length > 8 ? s[..8] : s;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
