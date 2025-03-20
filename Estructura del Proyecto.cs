using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class BaseRequestConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(BaseRequest);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        string type = jsonObject["Type"]?.ToString();

        BaseRequest request;
        switch (type)
        {
            case "GET_SERVICES":
                request = new GetServiceRequest();
                break;
            case "GET_PRODUCTS":
                request = new GetProductsRequest();
                break;
            case "GET_PAYMENT_AGENTS":
                request = new GetPaymentAgentsRequest();
                break;
            case "GET_WHOLESALE_EXCHANGE_RATE":
                request = new GetWholesaleExchangeRateRequest();
                break;
            case "FOREIGN_EXCHANGE_RATE":
                request = new GetForeignExchangeRateRequest();
                break;
            case "GET_IDENTIFICATIONS":
                request = new GetIdentificationsRequest();
                break;
            default:
                throw new Exception($"Tipo de solicitud desconocido: {type}");
        }

        serializer.Populate(jsonObject.CreateReader(), request);
        return request;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.FromObject(value);
        jsonObject.WriteTo(writer);
    }
}
