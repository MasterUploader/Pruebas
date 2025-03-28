public static class XmlJsonHelper
{
    public static JObject ToCleanJson(string xml, string targetNode = null, bool includeOnlyData = false)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xml);

        // Elimina atributos como xmlns, xsi
        RemoveAllAttributes(xmlDoc.DocumentElement);

        // Selecciona el nodo deseado si se indicó
        XmlNode nodeToConvert = xmlDoc.DocumentElement;
        if (!string.IsNullOrWhiteSpace(targetNode))
        {
            nodeToConvert = xmlDoc.GetElementsByTagName(targetNode)?.Item(0);
        }

        if (nodeToConvert == null)
            throw new Exception($"No se encontró el nodo '{targetNode}' en el XML.");

        // Convierte el nodo a JSON
        var json = JsonConvert.SerializeXmlNode(nodeToConvert, Formatting.None, true);

        // Convierte a JObject y remueve atributos extras (si aplica)
        var jObj = JObject.Parse(json);

        if (includeOnlyData)
        {
            // Retorna directamente el contenido del nodo
            return jObj.First?.First as JObject ?? jObj;
        }

        return jObj;
    }

    private static void RemoveAllAttributes(XmlNode node)
    {
        if (node.Attributes != null)
        {
            for (int i = node.Attributes.Count - 1; i >= 0; i--)
            {
                var attr = node.Attributes[i];
                if (attr.Name.StartsWith("xmlns") || attr.Name.StartsWith("xsi") || attr.Name.StartsWith("xsd"))
                    node.Attributes.Remove(attr);
            }
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            RemoveAllAttributes(child);
        }
    }
}
