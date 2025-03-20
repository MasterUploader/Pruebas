using System.Xml.Serialization;

/// <summary>
/// Clase base para todas las solicitudes SOAP.
/// Permite manejar diferentes tipos de REQUEST de forma genérica.
/// </summary>
[XmlInclude(typeof(GetServiceRequest))]
[XmlInclude(typeof(GetProductsRequest))]
[XmlInclude(typeof(GetPaymentAgentsRequest))]
[XmlInclude(typeof(GetWholesaleExchangeRateRequest))]
[XmlInclude(typeof(GetForeignExchangeRateRequest))]
[XmlInclude(typeof(GetIdentificationsRequest))]
public abstract class BaseRequest
{
    /// <summary>
    /// Define el tipo de la solicitud (Ej: "GET_SERVICES", "GET_PRODUCTS", etc.).
    /// Este atributo se usa para la serialización en XML.
    /// </summary>
    [XmlAttribute(AttributeName = "xsi:type")]
    public string Type { get; set; }
}
