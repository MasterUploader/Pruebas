using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;

public static class SoapXmlToJsonHelper
{
    public static string ConvertXmlToJsonAuto(string xmlContent)
    {
        try
        {
            var document = XDocument.Parse(xmlContent);
            XElement execNode = FindRelevantNode(document.Root);

            if (execNode == null)
                throw new InvalidOperationException("No se encontró un nodo ExecTR, GetData, ExecTRResponse o GetDataResponse.");

            string json = JsonConvert.SerializeXNode(execNode, Formatting.Indented, omitRootObject: false);
            return json;
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = ex.Message });
        }
    }

    private static XElement FindRelevantNode(XElement element)
    {
        if (element == null)
            return null;

        string localName = element.Name.LocalName.ToLower();
        if (localName == "exectr" || localName == "getdata" ||
            localName == "exectrresponse" || localName == "getdataresponse")
        {
            return element;
        }

        foreach (var child in element.Elements())
        {
            var result = FindRelevantNode(child);
            if (result != null)
                return result;
        }

        return null;
    }
}
