using System.Xml.Serialization;

namespace Models.DTO.BTS.Common
{
    /// <summary>
    /// Representa la sección de seguridad del encabezado SOAP (HEADER).
    /// </summary>
    [XmlRoot(ElementName = "SECURITY", Namespace = "http://www.btsincusa.com/gp")]
    public class Security
    {
        /// <summary>
        /// Código del usuario autenticado.
        /// </summary>
        [XmlElement(ElementName = "UserCode")]
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña del usuario.
        /// </summary>
        [XmlElement(ElementName = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}





using System.Xml.Serialization;

namespace Models.DTO.BTS.Common
{
    /// <summary>
    /// Representa los parámetros base requeridos en cualquier solicitud SOAP.
    /// </summary>
    public class RequestBase
    {
        /// <summary>
        /// Código del agente que realiza la solicitud.
        /// </summary>
        [XmlElement(ElementName = "AGENT_CD")]
        public string AgentCode { get; set; } = string.Empty;

        /// <summary>
        /// Código del tipo de transacción que se solicita ejecutar.
        /// </summary>
        [XmlElement(ElementName = "AGENT_TRANS_TYPE_CODE")]
        public string AgentTransactionTypeCode { get; set; } = string.Empty;

        /// <summary>
        /// Contenedor de datos adicionales de la solicitud.
        /// Puede omitirse si no se requiere.
        /// </summary>
        [XmlElement(ElementName = "DATA")]
        public string? Data { get; set; }
    }
}





using Models.DTO.BTS.Common;
using System.Xml.Serialization;

namespace Models.DTO.BTS.ExecTR
{
    /// <summary>
    /// Representa una solicitud de tipo ExecTR enviada en el cuerpo del mensaje SOAP.
    /// </summary>
    [XmlRoot(ElementName = "ExecTR", Namespace = "http://www.btsincusa.com/gp")]
    public class ExecTRRequest
    {
        /// <summary>
        /// Parámetros base requeridos por ExecTR.
        /// </summary>
        [XmlElement(ElementName = "Request")]
        public RequestBase Request { get; set; } = new();
    }
}


using Models.DTO.BTS.Common;
using System.Xml.Serialization;

namespace Models.DTO.BTS.GetData
{
    /// <summary>
    /// Representa una solicitud de tipo GetData enviada en el cuerpo del mensaje SOAP.
    /// </summary>
    [XmlRoot(ElementName = "GetData", Namespace = "http://www.btsincusa.com/gp")]
    public class GetDataRequest
    {
        /// <summary>
        /// Parámetros base requeridos por GetData.
        /// </summary>
        [XmlElement(ElementName = "Request")]
        public RequestBase Request { get; set; } = new();
    }
}


using System.Xml.Serialization;

namespace Models.DTO.BTS.Common
{
    /// <summary>
    /// Representa el encabezado de la solicitud SOAP.
    /// </summary>
    public class Header
    {
        /// <summary>
        /// Información de autenticación.
        /// </summary>
        [XmlElement(ElementName = "SECURITY", Namespace = "http://www.btsincusa.com/gp")]
        public Security Security { get; set; } = new();
    }
}


using System.Xml.Serialization;

namespace Models.DTO.BTS.Common
{
    /// <summary>
    /// Representa el sobre SOAP completo, incluyendo Header y Body.
    /// Esta clase es genérica para admitir diferentes tipos de cuerpo como ExecTR o GetData.
    /// </summary>
    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Envelope<TBody>
    {
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Xmlns { get; set; } = new();

        public Envelope()
        {
            Xmlns.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
            Xmlns.Add("gp", "http://www.btsincusa.com/gp");
        }

        /// <summary>
        /// Encabezado SOAP con autenticación.
        /// </summary>
        [XmlElement(ElementName = "Header", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public Header Header { get; set; } = new();

        /// <summary>
        /// Cuerpo SOAP, parametrizable por tipo de operación (ExecTR, GetData, etc.).
        /// </summary>
        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public TBody Body { get; set; } = default!;
    }
}
