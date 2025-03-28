public static class XmlJsonHelper
{
    public static JObject ToCleanJson(string xml, string targetNode = null, bool asString = false, out string jsonString)
    {
        var doc = XDocument.Parse(xml);

        // Eliminar atributos xmlns, xsi, etc.
        foreach (var el in doc.Descendants())
        {
            el.Attributes().Where(a =>
                a.IsNamespaceDeclaration ||
                a.Name.LocalName.StartsWith("xmlns") ||
                a.Name.LocalName.StartsWith("xsi") ||
                a.Name.LocalName == "type").Remove();
        }

        // Buscar nodo deseado
        XElement selectedNode = doc.Root;
        if (!string.IsNullOrEmpty(targetNode))
        {
            selectedNode = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == targetNode) ?? doc.Root;
        }

        // Convertir a JSON
        string cleanedXml = selectedNode.ToString(SaveOptions.DisableFormatting);
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(cleanedXml);
        jsonString = JsonConvert.SerializeXmlNode(xmlDoc.DocumentElement, Newtonsoft.Json.Formatting.Indented, true);

        return JObject.Parse(jsonString);
    }
}
