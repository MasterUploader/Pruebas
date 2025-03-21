using System.Xml.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Representa la sección de seguridad en el encabezado de la solicitud SOAP.
/// Contiene credenciales de sesión, usuario y dominio.
/// </summary>
[XmlRoot(ElementName = "SECURITY", Namespace = "http://www.btsincusa.com/gp")]
public class Security
{
    [XmlElement(ElementName = "SESSION_ID", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("SessionId")]
    public string SessionId { get; set; }

    [XmlElement(ElementName = "USER_NAME", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("UserName")]
    public string UserName { get; set; }

    [XmlElement(ElementName = "USER_DOMAIN", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("UserDomain")]
    public string UserDomain { get; set; }

    [XmlElement(ElementName = "USER_PASS", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("UserPass")]
    public string UserPass { get; set; }
}
