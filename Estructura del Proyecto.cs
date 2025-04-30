using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class StringToDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Permitir cadenas que representan números decimales
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (decimal.TryParse(str, out var result))
                return result;
            else
                throw new JsonException($"Valor decimal inválido: {str}");
        }

        // También permitir números directos como fallback
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetDecimal();

        throw new JsonException("Tipo de token no válido para decimal");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        // Siempre escribir como string en JSON
        writer.WriteStringValue(value.ToString());
    }
}
