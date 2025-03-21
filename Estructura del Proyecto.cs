using System.Xml.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Clase base para las respuestas SOAP.
/// Define un tipo general que se especializa en clases hijas.
/// </summary>
[XmlInclude(typeof(GetServiceResponse))]
[XmlInclude(typeof(GetProductsResponse))]
[XmlInclude(typeof(GetPaymentAgentsResponse))]
[XmlInclude(typeof(GetWholesaleExchangeRateResponse))]
[XmlInclude(typeof(GetForeignExchangeRateResponse))]
[XmlInclude(typeof(GetIdentificationsResponse))]
public abstract class BaseSoapResponse
{
    /// <summary>
    /// Tipo de respuesta recibida, por ejemplo, "GET_SERVICES".
    /// </summary>
    [XmlAttribute(AttributeName = "xsi:type")]
    [JsonProperty("type")]
    public string Type { get; set; }
}



[XmlRoot(ElementName = "RESPONSE", Namespace = "http://www.btsincusa.com/gp")]
public class GetServiceResponse : BaseSoapResponse
{
    public GetServiceResponse() { Type = "GET_SERVICES"; }

    /// <summary>
    /// Código de operación (4 caracteres).
    /// </summary>
    [XmlElement(ElementName = "OPCODE", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("OperationalCode")]
    public string OperationalCode { get; set; }

    /// <summary>
    /// Mensaje del proceso (hasta 255 caracteres).
    /// </summary>
    [XmlElement(ElementName = "PROCESS_MSG", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("ProcessMessage")]
    public string ProcessMessage { get; set; }
}




[XmlRoot(ElementName = "RESPONSE", Namespace = "http://www.btsincusa.com/gp")]
public class GetProductsResponse : BaseSoapResponse
{
    public GetProductsResponse() { Type = "GET_PRODUCTS"; }

    [XmlElement(ElementName = "OPCODE", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("OperationalCode")]
    public string OperationalCode { get; set; }

    [XmlElement(ElementName = "PROCESS_MSG", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("ProcessMessage")]
    public string ProcessMessage { get; set; }

    /// <summary>
    /// Código de servicio relacionado.
    /// </summary>
    [XmlElement(ElementName = "SERVICE_CD", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("ServiceCode")]
    public string ServiceCode { get; set; }
}



[XmlRoot(ElementName = "RESPONSE", Namespace = "http://www.btsincusa.com/gp")]
public class GetPaymentAgentsResponse : BaseSoapResponse
{
    public GetPaymentAgentsResponse() { Type = "GET_PAYMENT_AGENTS"; }

    [XmlElement(ElementName = "OPCODE", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("OperationalCode")]
    public string OperationalCode { get; set; }

    [XmlElement(ElementName = "PROCESS_MSG", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("ProcessMessage")]
    public string ProcessMessage { get; set; }

    /// <summary>
    /// Código del agente de pago.
    /// </summary>
    [XmlElement(ElementName = "PAY_AGENT_CD", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("PaymentAgentCode")]
    public string PaymentAgentCode { get; set; }
}

[XmlRoot(ElementName = "RESPONSE", Namespace = "http://www.btsincusa.com/gp")]
public class GetWholesaleExchangeRateResponse : BaseSoapResponse
{
    public GetWholesaleExchangeRateResponse() { Type = "GET_WHOLESALE_EXCHANGE_RATE"; }

    [XmlElement(ElementName = "OPCODE", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("OperationalCode")]
    public string OperationalCode { get; set; }

    [XmlElement(ElementName = "WHOLESALE_FX", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("WholesaleExchangeRate")]
    public string WholesaleExchangeRate { get; set; }
}


[XmlRoot(ElementName = "RESPONSE", Namespace = "http://www.btsincusa.com/gp")]
public class GetForeignExchangeRateResponse : BaseSoapResponse
{
    public GetForeignExchangeRateResponse() { Type = "FOREIGN_EXCHANGE_RATE"; }

    [XmlElement(ElementName = "OPCODE", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("OperationalCode")]
    public string OperationalCode { get; set; }

    [XmlElement(ElementName = "EXCH_RATE_FX", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("ExchangeRate")]
    public string ExchangeRate { get; set; }
}


[XmlRoot(ElementName = "RESPONSE", Namespace = "http://www.btsincusa.com/gp")]
public class GetIdentificationsResponse : BaseSoapResponse
{
    public GetIdentificationsResponse() { Type = "GET_IDENTIFICATIONS"; }

    [XmlElement(ElementName = "OPCODE", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("OperationalCode")]
    public string OperationalCode { get; set; }

    [XmlElement(ElementName = "ISSUER_CD", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("IdentificationIssuerCode")]
    public string IdentificationIssuerCode { get; set; }

    [XmlElement(ElementName = "TYPE_CD", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("IdentificationTypeCode")]
    public string IdentificationTypeCode { get; set; }
}


[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class SoapResponseEnvelope
{
    [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [JsonProperty("Body")]
    public SoapResponseBody Body { get; set; }
}

public class SoapResponseBody
{
    [XmlElement(ElementName = "RESPONSE", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("Response")]
    [JsonConverter(typeof(BaseSoapResponseConverter))] // Usa el convertidor dinámico
    public BaseSoapResponse Response { get; set; }
}


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class BaseSoapResponseConverter : JsonConverter<BaseSoapResponse>
{
    public override BaseSoapResponse ReadJson(JsonReader reader, Type objectType, BaseSoapResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        string type = jsonObject["type"]?.ToString();

        BaseSoapResponse response = type switch
        {
            "GET_SERVICES" => new GetServiceResponse(),
            "GET_PRODUCTS" => new GetProductsResponse(),
            "GET_PAYMENT_AGENTS" => new GetPaymentAgentsResponse(),
            "GET_WHOLESALE_EXCHANGE_RATE" => new GetWholesaleExchangeRateResponse(),
            "FOREIGN_EXCHANGE_RATE" => new GetForeignExchangeRateResponse(),
            "GET_IDENTIFICATIONS" => new GetIdentificationsResponse(),
            _ => throw new JsonSerializationException($"Tipo de respuesta desconocido: {type}")
        };

        serializer.Populate(jsonObject.CreateReader(), response);
        return response;
    }

    public override void WriteJson(JsonWriter writer, BaseSoapResponse value, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.FromObject(value, serializer);
        jsonObject.WriteTo(writer);
    }
}










