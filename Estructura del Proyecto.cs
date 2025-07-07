using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;

[XmlRoot("RESPONSE", Namespace = "http://www.btsincusa.com/gp/")]
public class DepositsResponse
{
    /// <summary>
    /// Código de operación.
    /// </summary>
    [XmlElement("OPCODE")]
    [JsonProperty("opCode")]
    public string OpCode { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje del proceso.
    /// </summary>
    [XmlElement("PROCESS_MSG")]
    [JsonProperty("processMessage")]
    public string ProcessMsg { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del parámetro con error.
    /// </summary>
    [XmlElement("ERROR_PARAM_FULL_NAME")]
    [JsonProperty("errorParamFullName")]
    public string ErrorParamFullName { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de procesamiento.
    /// </summary>
    [XmlElement("PROCESS_DT")]
    [JsonProperty("processDate")]
    public string ProcessDate { get; set; } = string.Empty;

    /// <summary>
    /// Hora de procesamiento.
    /// </summary>
    [XmlElement("PROCESS_TM")]
    [JsonProperty("processTime")]
    public string ProcessTime { get; set; } = string.Empty;

    /// <summary>
    /// Lista de depósitos.
    /// </summary>
    [XmlArray("DEPOSITS")]
    [XmlArrayItem("DEPOSIT")]
    [JsonProperty("deposits")]
    public List<Deposit> Deposits { get; set; } = new();
}

public class Deposit
{
    /// <summary>
    /// Datos del depósito.
    /// </summary>
    [XmlElement("DATA")]
    [JsonProperty("data")]
    public DepositData Data { get; set; } = new();
}
public class DepositData
{
    /// <summary>
    /// Número de Confirmación.
    /// </summary>
    [XmlElement("CONFIRMATION_NM")]
    [JsonProperty("confirmationNumber")]
    public string ConfirmationNumber { get; set; } = string.Empty;

    /// <summary>
    /// ID del movimiento de venta.
    /// </summary>
    [XmlElement("SALE_MOVEMENT_ID")]
    [JsonProperty("saleMovementId")]
    public string SaleMovementId { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de la venta.
    /// </summary>
    [XmlElement("SALE_DT")]
    [JsonProperty("saleDate")]
    public string SaleDate { get; set; } = string.Empty;

    /// <summary>
    /// Hora de la venta.
    /// </summary>
    [XmlElement("SALE_TM")]
    [JsonProperty("saleTime")]
    public string SaleTime { get; set; } = string.Empty;

    /// <summary>
    /// Código del servicio.
    /// </summary>
    [XmlElement("SERVICE_CD")]
    [JsonProperty("serviceCode")]
    public string ServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de pago.
    /// </summary>
    [XmlElement("PAYMENT_TYPE_CD")]
    [JsonProperty("paymentTypeCode")]
    public string PaymentTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// País de origen.
    /// </summary>
    [XmlElement("ORIG_COUNTRY_CD")]
    [JsonProperty("originCountryCode")]
    public string OriginCountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Moneda de origen.
    /// </summary>
    [XmlElement("ORIG_CURRENCY_CD")]
    [JsonProperty("originCurrencyCode")]
    public string OriginCurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// País de destino.
    /// </summary>
    [XmlElement("DEST_COUNTRY_CD")]
    [JsonProperty("destinationCountryCode")]
    public string DestinationCountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Moneda de destino.
    /// </summary>
    [XmlElement("DEST_CURRENCY_CD")]
    [JsonProperty("destinationCurrencyCode")]
    public string DestinationCurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Monto de origen.
    /// </summary>
    [XmlElement("ORIGIN_AM")]
    [JsonProperty("originAmount")]
    public string OriginAmount { get; set; } = string.Empty;

    /// <summary>
    /// Monto de destino.
    /// </summary>
    [XmlElement("DESTINATION_AM")]
    [JsonProperty("destinationAmount")]
    public string DestinationAmount { get; set; } = string.Empty;

    /// <summary>
    /// Tasa de cambio.
    /// </summary>
    [XmlElement("EXCH_RATE_FX")]
    [JsonProperty("exchangeRateFx")]
    public string ExchangeRateFx { get; set; } = string.Empty;

    /// <summary>
    /// Moneda de referencia del mercado.
    /// </summary>
    [XmlElement("MARKET_REF_CURRENCY_CD")]
    [JsonProperty("marketRefCurrencyCode")]
    public string MarketRefCurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Tasa de cambio del mercado de referencia.
    /// </summary>
    [XmlElement("MARKET_REF_CURRENCY_FX")]
    [JsonProperty("marketRefCurrencyFx")]
    public string MarketRefCurrencyFx { get; set; } = string.Empty;

    /// <summary>
    /// Monto en moneda de referencia del mercado.
    /// </summary>
    [XmlElement("MARKET_REF_CURRENCY_AM")]
    [JsonProperty("marketRefCurrencyAmount")]
    public string MarketRefCurrencyAmount { get; set; } = string.Empty;

    /// <summary>
    /// Código del agente origen.
    /// </summary>
    [XmlElement("S_AGENT_CD")]
    [JsonProperty("senderAgentCode")]
    public string SenderAgentCode { get; set; } = string.Empty;

    /// <summary>
    /// País del agente origen.
    /// </summary>
    [XmlElement("S_COUNTRY_CD")]
    [JsonProperty("senderCountryCode")]
    public string SenderCountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Estado del agente origen.
    /// </summary>
    [XmlElement("S_STATE_CD")]
    [JsonProperty("senderStateCode")]
    public string SenderStateCode { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de cuenta del receptor.
    /// </summary>
    [XmlElement("R_ACCOUNT_TYPE_CD")]
    [JsonProperty("recipientAccountTypeCode")]
    public string RecipientAccountTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Número de cuenta del receptor.
    /// </summary>
    [XmlElement("R_ACCOUNT_NM")]
    [JsonProperty("recipientAccountNumber")]
    public string RecipientAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Código del agente receptor.
    /// </summary>
    [XmlElement("R_AGENT_CD")]
    [JsonProperty("recipientAgentCode")]
    public string RecipientAgentCode { get; set; } = string.Empty;

    /// <summary>
    /// Información del remitente.
    /// </summary>
    [XmlElement("SENDER")]
    [JsonProperty("sender")]
    public Person Sender { get; set; } = new();

    /// <summary>
    /// Información del receptor.
    /// </summary>
    [XmlElement("RECIPIENT")]
    [JsonProperty("recipient")]
    public Person Recipient { get; set; } = new();
}

public class Person
{
    /// <summary>
    /// Primer nombre.
    /// </summary>
    [XmlElement("FIRST_NAME")]
    [JsonProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Segundo nombre.
    /// </summary>
    [XmlElement("MIDDLE_NAME")]
    [JsonProperty("middleName")]
    public string MiddleName { get; set; } = string.Empty;

    /// <summary>
    /// Apellido paterno.
    /// </summary>
    [XmlElement("LAST_NAME")]
    [JsonProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Apellido materno.
    /// </summary>
    [XmlElement("MOTHER_M_NAME")]
    [JsonProperty("motherMaidenName")]
    public string MotherMaidenName { get; set; } = string.Empty;

    /// <summary>
    /// Dirección.
    /// </summary>
    [XmlElement("ADDRESS")]
    [JsonProperty("address")]
    public Address Address { get; set; } = new();
}
public class Address
{
    /// <summary>
    /// Dirección exacta.
    /// </summary>
    [XmlElement("ADDRESS")]
    [JsonProperty("addressLine")]
    public string AddressLine { get; set; } = string.Empty;

    /// <summary>
    /// Ciudad.
    /// </summary>
    [XmlElement("CITY")]
    [JsonProperty("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Estado.
    /// </summary>
    [XmlElement("STATE_CD")]
    [JsonProperty("stateCode")]
    public string StateCode { get; set; } = string.Empty;

    /// <summary>
    /// País.
    /// </summary>
    [XmlElement("COUNTRY_CD")]
    [JsonProperty("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Código postal.
    /// </summary>
    [XmlElement("ZIP_CODE")]
    [JsonProperty("zipCode")]
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// Teléfono.
    /// </summary>
    [XmlElement("PHONE")]
    [JsonProperty("phone")]
    public string Phone { get; set; } = string.Empty;
}
