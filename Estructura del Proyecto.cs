/// <summary>
/// Representa una solicitud para obtener la tasa de cambio extranjera.
/// </summary>
[XmlRoot(ElementName = "REQUEST")]
public class GetForeignExchangeRateRequest : BaseRequest
{
    public GetForeignExchangeRateRequest() { Type = "FOREIGN_EXCHANGE_RATE"; }

    [XmlElement(ElementName = "AGENT_TRANS_TYPE_CODE")]
    public string AgentTransactionTypeCode { get; set; }

    [XmlElement(ElementName = "AGENT_CD")]
    public string AgentCode { get; set; }

    [XmlElement(ElementName = "CURRENCY_CD")]
    public string BaseCurrencyCode { get; set; }
}
