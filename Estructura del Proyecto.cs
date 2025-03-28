using System.Xml;
using Newtonsoft.Json;

public static class XmlJsonHelper
{
    /// <summary>
    /// Convierte el nodo RESPONSE (u otro nodo raíz deseado) de un XML en un JObject limpio.
    /// </summary>
    /// <param name="rawXml">El XML completo como string.</param>
    /// <param name="targetNodeName">El nombre del nodo que se desea convertir, por defecto "RESPONSE".</param>
    /// <returns>JObject con los datos del nodo convertido.</returns>
    public static JObject ExtractNodeAsJson(string rawXml, string targetNodeName = "RESPONSE")
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(rawXml);

        var node = xmlDoc.GetElementsByTagName(targetNodeName)[0];

        if (node == null)
            throw new Exception($"No se encontró el nodo '{targetNodeName}' en el XML.");

        string json = JsonConvert.SerializeXmlNode(node, Formatting.None, true);

        return JObject.Parse(json);
    }
}
