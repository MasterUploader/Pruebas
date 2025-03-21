using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using System.IO;

public class SoapService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor que inicializa el cliente HTTP.
    /// </summary>
    public SoapService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Método para enviar una solicitud SOAP basada en el objeto recibido.
    /// Convierte la solicitud JSON a XML, la envía y devuelve la respuesta en JSON.
    /// </summary>
    public async Task<string> SendSoapRequestAsync(SoapRequestDto requestDto, string soapEndpoint, string soapAction)
    {
        try
        {
            // **1️⃣ Convertir el DTO de la solicitud a XML**
            string xmlRequest = SerializeToXml(requestDto.Request);

            // **2️⃣ Construir el envelope SOAP**
            string soapEnvelope = $@"
            <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>
                <soapenv:Header/>
                <soapenv:Body>
                    {xmlRequest}
                </soapenv:Body>
            </soapenv:Envelope>";

            // **3️⃣ Configurar la solicitud HTTP**
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            var request = new HttpRequestMessage(HttpMethod.Post, soapEndpoint)
            {
                Content = content
            };
            request.Headers.Add("SOAPAction", soapAction);

            // **4️⃣ Enviar la petición SOAP**
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string xmlResponse = await response.Content.ReadAsStringAsync();

            // **5️⃣ Convertir la respuesta XML a JSON**
            var soapResponse = DeserializeFromXml<SoapResponse>(xmlResponse);
            string jsonResponse = JsonConvert.SerializeObject(soapResponse, Formatting.Indented);

            return jsonResponse;
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Método para serializar un objeto a XML.
    /// </summary>
    private string SerializeToXml<T>(T obj)
    {
        var xmlSerializer = new XmlSerializer(typeof(T));
        using (var stringWriter = new StringWriter())
        {
            xmlSerializer.Serialize(stringWriter, obj);
            return stringWriter.ToString();
        }
    }

    /// <summary>
    /// Método para deserializar XML a un objeto.
    /// </summary>
    private T DeserializeFromXml<T>(string xml)
    {
        var xmlSerializer = new XmlSerializer(typeof(T));
        using (var stringReader = new StringReader(xml))
        {
            return (T)xmlSerializer.Deserialize(stringReader);
        }
    }
}
