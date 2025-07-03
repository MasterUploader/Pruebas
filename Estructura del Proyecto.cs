using System;
using System.Xml.Serialization;
using System.Collections.Generic;

[XmlRoot("DATA")]
public class Data
{
    /// <summary>
    /// Lista de actividades detalladas.
    /// </summary>
    [XmlElement("DET_ACT")]
    public List<DetAct> DetActList { get; set; }
}

public class DetAct
{
    [XmlAttribute("ACTIVITY_DT")]
    public string ActivityDate { get; set; }

    [XmlAttribute("AGENT_CD")]
    public string AgentCode { get; set; }

    [XmlAttribute("SERVICE_CD")]
    public string ServiceCode { get; set; }

    [XmlAttribute("ORIG_COUNTRY_CD")]
    public string OriginCountryCode { get; set; }

    [XmlAttribute("ORIG_CURRENCY_CD")]
    public string OriginCurrencyCode { get; set; }

    [XmlAttribute("DEST_COUNTRY_CD")]
    public string DestinationCountryCode { get; set; }

    [XmlAttribute("DEST_CURRENCY_CD")]
    public string DestinationCurrencyCode { get; set; }

    [XmlAttribute("PAYMENT_TYPE_CD")]
    public string PaymentTypeCode { get; set; }

    [XmlAttribute("O_AGENT_CD")]
    public string OriginAgentCode { get; set; }

    [XmlElement("DETAILS")]
    public Details Details { get; set; }
}

public class Details
{
    [XmlElement("DETAIL")]
    public List<Detail> DetailList { get; set; }
}

public class Detail
{
    [XmlAttribute("MOVEMENT_TYPE_CODE")]
    public string MovementTypeCode { get; set; }

    [XmlAttribute("CONFIRMATION_NM")]
    public string ConfirmationNumber { get; set; }

    [XmlAttribute("AGENT_ORDER_NM")]
    public string AgentOrderNumber { get; set; }

    [XmlAttribute("ORIGIN_AM")]
    public decimal OriginAmount { get; set; }

    [XmlAttribute("DESTINATION_AM")]
    public decimal DestinationAmount { get; set; }

    [XmlAttribute("FEE_AM")]
    public decimal FeeAmount { get; set; }

    [XmlAttribute("DISCOUNT_AM")]
    public decimal DiscountAmount { get; set; }
}
