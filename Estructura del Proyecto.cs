using System.Xml.Serialization;
using Newtonsoft.Json;

/// <summary>
/// DTO que representa la solicitud SOAP genérica.
/// </summary>
[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class SoapRequestDto
{
    [XmlElement(ElementName = "AgentCode")]
    [JsonProperty("AgentCode")]
    public string AgentCode { get; set; }

    /// <summary>
    /// Define la estructura de la solicitud, que puede variar (GetServiceRequest, GetProductsRequest, etc.)
    /// </summary>
    [XmlElement(ElementName = "Request")]
    [JsonProperty("Request")]
    [XmlInclude(typeof(GetServiceRequest))]
    [XmlInclude(typeof(GetProductsRequest))]
    [XmlInclude(typeof(GetPaymentAgentsRequest))]
    public BaseRequest Request { get; set; } // ⬅️ Solución: BaseRequest con XmlInclude

    public SoapRequestDto() { }
}




/// <summary>
/// Clase base para todas las solicitudes específicas (GetServiceRequest, GetProductsRequest, etc.)
/// </summary>
[XmlInclude(typeof(GetServiceRequest))]
[XmlInclude(typeof(GetProductsRequest))]
[XmlInclude(typeof(GetPaymentAgentsRequest))]
public abstract class BaseRequest
{
    [XmlAttribute(AttributeName = "xsi:type")]
    [JsonProperty("Type")]
    public string Type { get; set; }
}
