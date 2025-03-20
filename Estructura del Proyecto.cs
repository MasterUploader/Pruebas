/// <summary>
/// Representa una solicitud para obtener tipos de identificación permitidos.
/// </summary>
[XmlRoot(ElementName = "REQUEST")]
public class GetIdentificationsRequest : BaseRequest
{
    public GetIdentificationsRequest() { Type = "GET_IDENTIFICATIONS"; }

    [XmlElement(ElementName = "AGENT_TRANS_TYPE_CODE")]
    public string AgentTransactionTypeCode { get; set; }

    [XmlElement(ElementName = "AGENT_CD")]
    public string AgentCode { get; set; }

    [XmlElement(ElementName = "ORIG_COUNTRY_CD")]
    public string OriginCountryCode { get; set; }
}
