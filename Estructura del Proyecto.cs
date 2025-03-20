/// <summary>
/// Representa una solicitud para obtener servicios permitidos en el sistema.
/// </summary>
[XmlRoot(ElementName = "REQUEST")]
public class GetServiceRequest : BaseRequest
{
    public GetServiceRequest() { Type = "GET_SERVICES"; }

    [XmlElement(ElementName = "AGENT_TRANS_TYPE_CODE")]
    public string AgentTransactionTypeCode { get; set; }

    [XmlElement(ElementName = "AGENT_CD")]
    public string AgentCode { get; set; }

    [XmlElement(ElementName = "ORIG_COUNTRY_CD")]
    public string OriginCountryCode { get; set; }
}
