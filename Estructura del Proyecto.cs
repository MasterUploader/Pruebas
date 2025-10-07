Así tengo las clases

using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Common.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Common.Response;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Consulta.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Consulta.Response;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.AuthenticateService;
using API_1_TERCEROS_REMESADORAS.Utilities;
using System.Text;
using System.Xml.Serialization;

namespace API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConsultaService;

/// <summary>
/// Clase de servicio para método de consulta de BTS.
/// </summary>
public class ConsultaService : IConsultaService
{
    private readonly IAuthenticateService _authenticateService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Constructor de clase ConsultaService.
    /// </summary>
    /// <param name="authenticateService">Instancia de la Clase AuthenticateService.</param>
    /// <param name="httpContextAccessor">Instancia de IHttpContextAccesor</param>
    /// <param name="httpClientFactory">Instancia de httpClientFactory. </param>
    public ConsultaService(IAuthenticateService authenticateService, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
    {
        _authenticateService = authenticateService;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Método de ConsultaServiceRequestAsync con BTS.
    /// </summary>
    /// <returns>Retorna objeto ConsultaResponseData.</returns>
    public async Task<(ConsultaResponseData consultaResponseData, int statusCode)> ConsultaServiceRequestAsync(ConsultaBody consultaModel)
    {
        var respuesta = new ConsultaResponseData();
        try
        {
            //Realizamos la consulta para obtener el token ya que es requerido para cada petición.
            var (responseAutenticacion, statusCodeA) = await _authenticateService.AuthenticateServiceRequestAsync();
            string session = responseAutenticacion.Session_Id;

            if (!responseAutenticacion.OpCode.Equals("S010") && !responseAutenticacion.OpCode.Equals("S000"))
            {
                respuesta.ProcessDt = responseAutenticacion.Proces_DT;
                respuesta.ProcessTm = responseAutenticacion.Process_Tm;
                respuesta.OpCode = responseAutenticacion.OpCode;
                respuesta.ProcessMsg = responseAutenticacion.Process_Msg;

                return (respuesta, statusCodeA);
            }

            //Extraemos los campos Estaticos para Cada Petición
            string user = GlobalConnection.Current.BTSUser;
            string password = GlobalConnection.Current.BTSPassword;
            string host = GlobalConnection.Current.Host + "/GPTS/transactionservice.asmx"; ;
            string domain = GlobalConnection.Current.Domain;

            var request = new GetRequestConsultaEnvelope<GetDataRequest<GetConsultaDataRequest>>
            {
                Header = new GetHeader
                {
                    Security = new GetSecurity
                    {
                        UserDomain = domain,
                        UserName = user,
                        UserPass = password,
                        SessionId = session

                    },
                    Addressing = new GetAddressing
                    {
                        From = "",
                        To = ""
                    }
                },
                Body = consultaModel
            };

            var xmlPost = XmlHelper.SerializeToXml(request);

            var content = new StringContent(xmlPost, Encoding.UTF8, "text/xml");

            HttpClientHandler handler = new ()
            {
                ServerCertificateCustomValidationCallback = (sender, certificate, chain, SslPolicyErrors) => true

            };

            var client = _httpClientFactory.CreateClient("BTS");

            //using var client = new HttpClient(loggingHandler);
            var response = await client.PostAsync(host, content);

            int statusCode = (int)response.StatusCode;
            string responseXml = await response.Content.ReadAsStringAsync();

            var serializer = new XmlSerializer(typeof(GetResponseEnvelope<GetResponseBody<ExecTRResponseConsulta>>));
            using var reader = new StringReader(responseXml);
            GetResponseEnvelope<GetResponseBody<ExecTRResponseConsulta>> result = (GetResponseEnvelope<GetResponseBody<ExecTRResponseConsulta>>)serializer.Deserialize(reader)!;

            return (result.Body.ExectTRResponse.ExecTRResult.RESPONSE, statusCode);
        }
        catch (Exception ex)
        {
            respuesta.ProcessDt = DateTime.Now.ToString("yyyyMMdd");
            respuesta.ProcessTm = DateTime.Now.ToString("hhmmss");
            respuesta.OpCode = "S099";
            respuesta.ProcessMsg = ex.Message;

            return (respuesta, 500);
        }
    }
}


using Newtonsoft.Json;
using System.Xml.Serialization;

namespace API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Consulta.Response;

/// <summary>
/// Modelo que representa los datos del nodo RESPONSE de la respuesta MONEY_TRANSFER_QUERY_RESPONSE.
/// </summary>
[XmlType(TypeName = "MONEY_TRANSFER_QUERY_RESPONSE", Namespace = "http://www.btsincusa.com/gp/")]
public class ConsultaResponseData
{
    /// <summary>
    /// Código de Operación.
    /// </summary>
    [XmlElement("OPCODE")]
    [JsonProperty("opCode")]
    public string OpCode { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje del Proceso.
    /// </summary>
    [XmlElement("PROCESS_MSG")]
    [JsonProperty("processMsg")]
    public string ProcessMsg { get; set; } = string.Empty;

    /// <summary>
    /// Error en Parametro.
    /// </summary>
    [XmlElement("ERROR_PARAM_FULL_NAME")]
    [JsonProperty("errorParamFullName")]
    public string ErrorParamFullName { get; set; } = string.Empty;

    /// <summary>
    /// Código estado de la transacción.
    /// </summary>
    [XmlElement("TRANS_STATUS_CD")]
    [JsonProperty("transStatusCd")]
    public string TransStatusCd { get; set; } = string.Empty;

    /// <summary>
    /// Fecha del Estatus de la transacción.
    /// </summary>
    [XmlElement("TRANS_STATUS_DT")]
    [JsonProperty("transStatusDt")]
    public string TransStatusDt { get; set; } = string.Empty;

    /// <summary>
    /// Fecha del Proceso.
    /// </summary>
    [XmlElement("PROCESS_DT")]
    [JsonProperty("processDt")]
    public string ProcessDt { get; set; } = string.Empty;

    /// <summary>
    /// Hora del Proceso.
    /// </summary>
    [XmlElement("PROCESS_TM")]
    [JsonProperty("processTm")]
    public string ProcessTm { get; set; } = string.Empty;

    /// <summary>
    /// Objeto Data de la respuesta.
    /// </summary>
    [XmlElement("DATA")]
    [JsonProperty("data")]
    public ConsultaData Data { get; set; } = new();
}


using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Common.Response;
using API_1_TERCEROS_REMESADORAS.Utilities;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Consulta.Response;

/// <summary>
/// Modelo que representa los datos dentro del nodo DATA.
/// </summary>
public class ConsultaData
{
    /// <summary>
    /// Parametro Sale Date Time.
    /// </summary>
    [XmlElement("SALE_DT")]
    [JsonProperty("saleDt")]
    public string SaleDt { get; set; } = string.Empty;

    /// <summary>
    /// Parametro Sale Time
    /// </summary>
    [XmlElement("SALE_TM")]
    [JsonProperty("saleTm")]
    public string SaleTm { get; set; } = string.Empty;

    /// <summary>
    /// Parametro Service CD.
    /// </summary>
    [XmlElement("SERVICE_CD")]
    [JsonProperty("serviceCd")]
    public string ServiceCd { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de Pago.
    /// </summary>
    [XmlElement("PAYMENT_TYPE_CD")]
    [JsonProperty("paymentTypeCd")]
    public string PaymentTypeCd { get; set; } = string.Empty;

    /// <summary>
    /// Pais de Origen.
    /// </summary>
    [XmlElement("ORIG_COUNTRY_CD")]
    [JsonProperty("origCountryCd")]
    public string OrigCountryCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("ORIG_CURRENCY_CD")]
    [JsonProperty("origCurrencyCd")]
    public string OrigCurrencyCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("DEST_COUNTRY_CD")]
    [JsonProperty("destCountryCd")]
    public string DestCountryCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("destCurrencyCd")]
    public string DestCurrencyCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("DESTINATION_AM")]

    [JsonProperty("destAmount")]
    public string DestAmount { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("ORIGIN_AM")]
    [JsonProperty("origAmount")]
    public string OrigAmount { get; set; } = string.Empty;

    private string _exchangeRateFx = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("EXCH_RATE_FX")]
    [JsonProperty("exchangeRateFx")]
    public string ExchangeRateFx { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    //[XmlElement("EXCH_RATE_FX")]
    //[JsonProperty("exchangeRateFx")]
    //public string ExchangeRateFx
    //{
    //    get => _exchangeRateFx is null ? string.Empty
    //           : (_exchangeRateFx.Length > 10 ? _exchangeRateFx[..10] : _exchangeRateFx);
    //    set => _exchangeRateFx = value?.Trim() ?? string.Empty;
    //}

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("MARKET_REF_CURRENCY_CD")]
    [JsonProperty("marketRefCurrencyCd")]
    public string MarketRefCurrencyCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("MARKET_REF_CURRENCY_FX")]
    [JsonProperty("marketRefCurrencyFx")]
    public string MarketRefCurrencyFx { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("MARKET_REF_CURRENCY_AM")]
    [JsonProperty("marketRefCurrencyAm")]
    public string MarketRefCurrencyAm { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("S_AGENT_CD")]
    [JsonProperty("sAgentCd")]
    public string SAgentCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("S_PAYMENT_TYPE_CD")]
    [JsonProperty("sPaymentTypeCd")]
    public string SPaymentTypeCd { get; set; } = string.Empty;
    /// <summary>
    /// 
    /// </summary>
    [XmlElement("S_ACCOUNT_TYPE_CD")]
    [JsonProperty("sAccountTypeCd")]
    public string SAccountTypeCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("S_ACCOUNT_NM")]
    [JsonProperty("sAccountNm")]
    public string SAccountNm { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("S_BANK_CD")]
    [JsonProperty("sBankCd")]
    public string SBankCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("S_BANK_REF_NM")]
    [JsonProperty("sBankRefNm")]
    public string SBankRefNm { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("R_ACCOUNT_TYPE_CD")]
    [JsonProperty("rAccountTypeCd")]
    public string RAccountTypeCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("R_ACCOUNT_NM")]
    [JsonProperty("rAccountName")]
    public string RAccountName { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("R_AGENT_CD")]
    [JsonProperty("rAgentCd")]
    public string RAgentCd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("R_AGENT_REGION_SD")]
    [JsonProperty("rAgentRegionSd")]
    public string RAgentRegionSd { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("R_AGENT_BRANCH_SD")]
    [JsonProperty("rAgentBranchSd")]
    public string RAgentBranchSd { get; set; } = string.Empty;



    /// <summary>
    /// 
    /// </summary>
    [XmlElement("BANK_REF_NM")]
    [JsonProperty("bankRefNm")]
    public string BankRefNm { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("PROMOTION_CODE")]
    [JsonProperty("promotionCode")]
    public string PromotionCode { get; set; } = string.Empty;





    /// <summary>
    /// 
    /// </summary>
    [XmlElement("SENDER")]
    [JsonProperty("sender")]
    public ResponseSender Sender { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("RECIPIENT")]
    [JsonProperty("recipient")]
    public ConsultaRecipent Recipient { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("RECIPIENT_IDENTIFICATION")]
    [JsonProperty("recipientIdentification")]
    public SenderIdentification RecipientIdentification { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    [XmlElement("SENDER_IDENTIFICATION")]
    [JsonProperty("senderIdentification")]
    public SenderIdentification SenderIdentification { get; set; } = new();
}



