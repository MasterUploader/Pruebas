using System.Xml.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Representa la sección ADDRESSING en el encabezado SOAP.
/// </summary>
[XmlRoot(ElementName = "ADDRESSING", Namespace = "http://www.btsincusa.com/gp")]
public class Addressing
{
    [XmlElement(ElementName = "FROM", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("From")]
    public string From { get; set; }

    [XmlElement(ElementName = "TO", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("To")]
    public string To { get; set; }
}



using System.Xml.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Representa el Header de la solicitud SOAP, compuesto por Seguridad y Addressing.
/// </summary>
[XmlRoot(ElementName = "Header", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class Header
{
    [XmlElement(ElementName = "SECURITY", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("Security")]
    public Security Security { get; set; }

    [XmlElement(ElementName = "ADDRESSING", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("Addressing")]
    public Addressing Addressing { get; set; }
}



using System.Xml.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Representa la solicitud dentro del cuerpo SOAP.
/// Contiene los datos específicos de la operación.
/// </summary>
[XmlRoot(ElementName = "REQUEST", Namespace = "http://www.btsincusa.com/gp")]
public class GetDataRequest
{
    [XmlElement(ElementName = "AGENT_CD", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("AgentCode")]
    public string AgentCode { get; set; }

    [XmlElement(ElementName = "AGENT_TRANS_TYPE_CODE", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("AgentTransactionTypeCode")]
    public string AgentTransactionTypeCode { get; set; }
}




using System.Xml.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Representa la estructura del método GetData dentro del Body de la solicitud SOAP.
/// </summary>
[XmlRoot(ElementName = "GetData", Namespace = "http://www.btsincusa.com/gp")]
public class GetData
{
    [XmlElement(ElementName = "REQUEST", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("Request")]
    public GetDataRequest Request { get; set; }
}




using System.Xml.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Representa el cuerpo de la solicitud SOAP.
/// Contiene el método GetData con su respectiva solicitud.
/// </summary>
[XmlRoot(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class Body
{
    [XmlElement(ElementName = "GetData", Namespace = "http://www.btsincusa.com/gp")]
    [JsonProperty("GetData")]
    public GetData GetData { get; set; }



    using System.Xml.Serialization;
using Newtonsoft.Json;

/// <summary>
/// Representa el Envelope completo de la solicitud SOAP.
/// </summary>
[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class SoapEnvelope
{
    [XmlElement(ElementName = "Header", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [JsonProperty("Header")]
    public Header Header { get; set; }

    [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [JsonProperty("Body")]
    public Body Body { get; set; }

    [XmlAttribute(AttributeName = "xmlns:soapenv")]
    public string SoapEnv { get; set; } = "http://schemas.xmlsoap.org/soap/envelope/";

    [XmlAttribute(AttributeName = "xmlns:gp")]
    public string Gp { get; set; } = "http://www.btsincusa.com/gp";
}


    using System.IO;
using System.Xml.Serialization;

public static class XmlHelper
{
    public static string SerializeToXml<T>(T obj)
    {
        var xmlSerializer = new XmlSerializer(typeof(T));
        using var stringWriter = new StringWriter();
        xmlSerializer.Serialize(stringWriter, obj);
        return stringWriter.ToString();
    }
}

// Ejemplo de uso:
var soapRequest = new SoapEnvelope
{
    Header = new Header
    {
        Security = new Security
        {
            SessionId = "1234",
            UserName = "1111",
            UserDomain = "BTS_lkkjxk",
            UserPass = "lkljkjk@!"
        },
        Addressing = new Addressing
        {
            From = "",
            To = ""
        }
    },
    Body = new Body
    {
        GetData = new GetData
        {
            Request = new GetDataRequest
            {
                AgentCode = "HSK",
                AgentTransactionTypeCode = "USR1"
            }
        }
    }
};

string xmlRequest = XmlHelper.SerializeToXml(soapRequest);
Console.WriteLine(xmlRequest);
