public static string PrettyPrintXml(string xml)
{
    try
    {
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(xml);

        var stringBuilder = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        };

        using (var writer = XmlWriter.Create(stringBuilder, settings))
        {
            doc.Save(writer);
        }

        return stringBuilder.ToString();
    }
    catch
    {
        // Si el XML es inválido o viene mal, lo devolvemos como está
        return xml;
    }
}


string responseBody = await response.Content.ReadAsStringAsync();
responseBody = LogFormatter.PrettyPrintXml(responseBody);
