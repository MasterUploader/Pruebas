/// <summary>
/// Representa una solicitud para obtener agentes de pago permitidos.
/// </summary>
[XmlRoot(ElementName = "REQUEST")]
public class GetPaymentAgentsRequest : BaseRequest
{
    public GetPaymentAgentsRequest() { Type = "GET_PAYMENT_AGENTS"; }

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

    [XmlElement(ElementName = "DEST_COUNTRY_CD")]
    public string DestinationCountryCode { get; set; }

    [XmlElement(ElementName = "DEST_CURRENCY_CD")]
    public string DestinationCurrencyCode { get; set; }

    [XmlElement(ElementName = "PAYMENT_TYPE_CD")]
    public string PaymentTypeCode { get; set; }
}
