/// <summary>
/// Representa una solicitud para obtener productos disponibles según código de servicio.
/// </summary>
[XmlRoot(ElementName = "REQUEST")]
public class GetProductsRequest : BaseRequest
{
    public GetProductsRequest() { Type = "GET_PRODUCTS"; }

    [XmlElement(ElementName = "AGENT_TRANS_TYPE_CODE")]
    public string AgentTransactionTypeCode { get; set; }

    [XmlElement(ElementName = "AGENT_CD")]
    public string AgentCode { get; set; }

    [XmlElement(ElementName = "ORIG_COUNTRY_CD")]
    public string OriginCountryCode { get; set; }

    [XmlElement(ElementName = "ORIG_CURRENCY_CD")]
    public string OriginCurrencyCode { get; set; }

    [XmlElement(ElementName = "SERVICE_CD")]
    public string ServiceCode { get; set; }
}
