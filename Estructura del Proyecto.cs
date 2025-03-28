public static class XmlJsonHelper
{
    /// <summary>
    /// Convierte un XML deserializado (ya sea objeto o string) directamente a un JObject limpio.
    /// </summary>
    /// <param name="deserializedObject">Objeto deserializado desde XML</param>
    /// <param name="includeOnlyData">Si se desea extraer solo el nodo RESPONSE</param>
    public static JObject ToCleanJson(object deserializedObject, bool includeOnlyData = false)
    {
        if (deserializedObject == null)
            return JObject.FromObject(new { error = "Objeto nulo" });

        var fullJson = JObject.FromObject(deserializedObject);

        // Opcional: limpiar @xmlns, @xsi:type, etc.
        RemoveXmlMetadata(fullJson);

        // Si se desea devolver únicamente el contenido de RESPONSE
        if (includeOnlyData)
        {
            var response = fullJson
                .SelectToken("Body.ExecTRResponse.ExecTRResult.RESPONSE");

            return response != null
                ? JObject.FromObject(response)
                : fullJson;
        }

        return fullJson;
    }

    private static void RemoveXmlMetadata(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var propsToRemove = ((JObject)token)
                .Properties()
                .Where(p => p.Name.StartsWith("@"))
                .ToList();

            foreach (var prop in propsToRemove)
                prop.Remove();

            foreach (var child in token.Children())
                RemoveXmlMetadata(child);
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
                RemoveXmlMetadata(item);
        }
    }
}
