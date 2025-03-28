public static class XmlJsonHelper
{
    public static JObject ToCleanJson(string xml, string targetNode = null, bool returnAsString = false, out string json)
    {
        var doc = XDocument.Parse(xml);

        // Eliminar todos los atributos xmlns, xsi, etc.
        foreach (var el in doc.Descendants())
        {
            el.ReplaceAttributes(el.Attributes().Where(a =>
                !a.IsNamespaceDeclaration &&
                !a.Name.LocalName.StartsWith("xmlns") &&
                !a.Name.LocalName.StartsWith("xsi") &&
                !a.Name.LocalName.StartsWith("type")));
        }

        // Buscar el nodo deseado
        XElement selectedNode = doc.Root;
        if (!string.IsNullOrEmpty(targetNode))
        {
            selectedNode = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == targetNode) ?? doc.Root;
        }

        // Convertir a XmlDocument
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(selectedNode.ToString(SaveOptions.DisableFormatting));

        // Convertir a JSON
        json = JsonConvert.SerializeXmlNode(xmlDoc.DocumentElement, Formatting.Indented, true);
        var jObj = JObject.Parse(json);

        // Limpieza final de cualquier @xmlns o @xsi aún presente
        CleanJsonNamespaces(jObj);

        json = jObj.ToString(Formatting.Indented);
        return jObj;
    }

    private static void CleanJsonNamespaces(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var propsToRemove = ((JObject)token).Properties()
                .Where(p => p.Name.StartsWith("@xmlns") || p.Name.StartsWith("@xsi") || p.Name.StartsWith("@"))
                .ToList();

            foreach (var prop in propsToRemove)
                prop.Remove();

            foreach (var child in ((JObject)token).Properties())
                CleanJsonNamespaces(child.Value);
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in (JArray)token)
                CleanJsonNamespaces(item);
        }
    }
}
