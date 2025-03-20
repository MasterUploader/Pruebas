using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

/// <summary>
/// Convertidor de JSON a la clase base BaseRequest.
/// Permite la deserialización automática de JSON en diferentes subclases de BaseRequest.
/// </summary>
public class BaseRequestConverter : JsonConverter<BaseRequest>
{
    /// <summary>
    /// Convierte un objeto JSON en una instancia de una subclase de BaseRequest.
    /// </summary>
    /// <param name="reader">El lector JSON que contiene los datos.</param>
    /// <param name="objectType">El tipo del objeto a convertir.</param>
    /// <param name="existingValue">Valor existente (ignorado en este caso).</param>
    /// <param name="serializer">El serializador JSON a usar.</param>
    /// <returns>Una instancia de la subclase correcta de BaseRequest.</returns>
    public override BaseRequest? ReadJson(JsonReader reader, Type objectType, BaseRequest? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // Cargar el objeto JSON en un JObject
        JObject jsonObject = JObject.Load(reader);

        // Obtener el tipo de solicitud desde el JSON
        string? type = jsonObject["Type"]?.ToString();
        if (string.IsNullOrEmpty(type))
        {
            throw new JsonSerializationException("El campo 'Type' es obligatorio para determinar el tipo de solicitud.");
        }

        // Determinar la subclase correcta de BaseRequest según el valor de "Type"
        BaseRequest? request = type switch
        {
            "GET_SERVICES" => new GetServiceRequest(),
            "GET_PRODUCTS" => new GetProductsRequest(),
            "GET_PAYMENT_AGENTS" => new GetPaymentAgentsRequest(),
            "GET_WHOLESALE_EXCHANGE_RATE" => new GetWholesaleExchangeRateRequest(),
            "FOREIGN_EXCHANGE_RATE" => new GetForeignExchangeRateRequest(),
            "GET_IDENTIFICATIONS" => new GetIdentificationsRequest(),
            _ => throw new JsonSerializationException($"Tipo de solicitud desconocido: {type}")
        };

        // Poblar la instancia con los datos del JSON
        serializer.Populate(jsonObject.CreateReader(), request);

        return request;
    }

    /// <summary>
    /// Convierte una instancia de BaseRequest en un objeto JSON.
    /// </summary>
    /// <param name="writer">El escritor JSON donde se escribirá la salida.</param>
    /// <param name="value">El objeto BaseRequest a serializar.</param>
    /// <param name="serializer">El serializador JSON a usar.</param>
    public override void WriteJson(JsonWriter writer, BaseRequest? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        // Serializar la instancia como un JObject
        JObject jsonObject = JObject.FromObject(value, serializer);
        jsonObject.WriteTo(writer);
    }
}
