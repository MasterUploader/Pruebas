using System;
using System.Xml.Serialization;

/// <summary>
/// Modelo que representa los datos del nodo RESPONSE de la respuesta MONEY_TRANSFER_QUERY_RESPONSE.
/// </summary>
[Serializable]
[XmlRoot("RESPONSE", Namespace = "http://www.btsincusa.com/gp/")]
public class ConsultaResponseData
{
    [XmlElement("OPCODE")]
    public string OpCode { get; set; }

    [XmlElement("PROCESS_MSG")]
    public string ProcessMsg { get; set; }

    [XmlElement("ERROR_PARAM_FULL_NAME")]
    public string ErrorParamFullName { get; set; }

    [XmlElement("TRANS_STATUS_CD_ONP")]
    public string TransStatusCdOnp { get; set; }

    [XmlElement("TRANS_STATUS_CD")]
    public string TransStatusCd { get; set; }

    [XmlElement("PROCESS_DT")]
    public string ProcessDt { get; set; }

    [XmlElement("PROCESS_TM")]
    public string ProcessTm { get; set; }

    [XmlElement("DATA")]
    public ConsultaData Data { get; set; }
}

/// <summary>
/// Modelo que representa los datos dentro del nodo DATA.
/// </summary>
public class ConsultaData
{
    [XmlElement("SALE_DT")]
    public string SaleDt { get; set; }

    [XmlElement("SALE_TM")]
    public string SaleTm { get; set; }

    [XmlElement("SERVICE_CD")]
    public string ServiceCd { get; set; }

    [XmlElement("PAYMENT_TYPE_CD")]
    public string PaymentTypeCd { get; set; }

    [XmlElement("ORIG_COUNTRY_CD")]
    public string OrigCountryCd { get; set; }

    [XmlElement("ORIG_CURR_CD")]
    public string OrigCurrencyCd { get; set; }

    [XmlElement("ORIG_AM")]
    public string OrigAmount { get; set; }

    [XmlElement("DEST_COUNTRY_CD")]
    public string DestCountryCd { get; set; }

    [XmlElement("DEST_CURR_CD")]
    public string DestCurrencyCd { get; set; }

    [XmlElement("DEST_AM")]
    public string DestAmount { get; set; }

    [XmlElement("EX_RATE_CURR_CD")]
    public string ExchangeRateCurrencyCd { get; set; }

    [XmlElement("EX_RATE")]
    public string ExchangeRate { get; set; }

    [XmlElement("MARKET_REF_CURRENCY_CD")]
    public string MarketRefCurrencyCd { get; set; }

    [XmlElement("MARKET_REF_CURRENCY_FX")]
    public string MarketRefCurrencyFx { get; set; }

    [XmlElement("MARKET_REF_CURRENCY_AM")]
    public string MarketRefCurrencyAm { get; set; }

    [XmlElement("CS_AGENT_CD")]
    public string CsAgentCd { get; set; }

    [XmlElement("CS_ACCOUNT_NO")]
    public string CsAccountNo { get; set; }

    [XmlElement("CS_ACCOUNT_TYPE_CD")]
    public string CsAccountTypeCd { get; set; }

    [XmlElement("R_ACCOUNT_NO")]
    public string RAccountNo { get; set; }

    [XmlElement("R_ACCOUNT_TYPE_CD")]
    public string RAccountTypeCd { get; set; }

    [XmlElement("R_ACCOUNT_NM")]
    public string RAccountName { get; set; }

    [XmlElement("R_AGENT_CD")]
    public string RAgentCd { get; set; }

    [XmlElement("R_AGENT_REGION_SD")]
    public string RAgentRegionSd { get; set; }

    [XmlElement("R_AGENT_BRANCH_SD")]
    public string RAgentBranchSd { get; set; }

    [XmlElement("SENDER")]
    public ConsultaPersona Sender { get; set; }

    [XmlElement("RECIPIENT")]
    public ConsultaPersona Recipient { get; set; }

    [XmlElement("RECIPIENT_IDENTIFICATION")]
    public ConsultaIdentificacion RecipientIdentification { get; set; }

    [XmlElement("SENDER_IDENTIFICATION")]
    public ConsultaIdentificacion SenderIdentification { get; set; }
}

/// <summary>
/// Modelo común para remitente o destinatario.
/// </summary>
public class ConsultaPersona
{
    [XmlElement("FIRST_NAME")]
    public string FirstName { get; set; }

    [XmlElement("MIDDLE_NAME")]
    public string MiddleName { get; set; }

    [XmlElement("LAST_NAME")]
    public string LastName { get; set; }

    [XmlElement("MOTHER_M_NAME")]
    public string MotherMName { get; set; }

    [XmlElement("ADDRESS")]
    public string Address { get; set; }

    [XmlElement("CITY")]
    public string City { get; set; }

    [XmlElement("STATE_CD")]
    public string StateCd { get; set; }

    [XmlElement("COUNTRY_CD")]
    public string CountryCd { get; set; }

    [XmlElement("ZIP_CODE")]
    public string ZipCode { get; set; }

    [XmlElement("PHONE")]
    public string Phone { get; set; }
}

/// <summary>
/// Modelo común para identificaciones del remitente o destinatario.
/// </summary>
public class ConsultaIdentificacion
{
    [XmlElement("TYPE_CD")]
    public string TypeCd { get; set; }

    [XmlElement("ISSUER_CD")]
    public string IssuerCd { get; set; }

    [XmlElement("ISSUER_STATE_CD")]
    public string IssuerStateCd { get; set; }

    [XmlElement("ISSUER_COUNTRY_CD")]
    public string IssuerCountryCd { get; set; }

    [XmlElement("IDENT_FNUM")]
    public string IdentFnum { get; set; }

    [XmlElement("EXPIRATION_DT")]
    public string ExpirationDt { get; set; }
}



