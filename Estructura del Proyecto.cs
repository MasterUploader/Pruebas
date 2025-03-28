public static class XmlJsonHelper
{
    /// <summary>
    /// Convierte un XML en una representación JSON limpia (sin xmlns, xsi, etc.)
    /// </summary>
    /// <typeparam name="T">Tipo del modelo de respuesta</typeparam>
    /// <param name="xml">XML de entrada</param>
    /// <returns>JObject con la estructura limpia del modelo</returns>
    public static JObject ToJsonFromModel<T>(string xml) where T : class
    {
        if (string.IsNullOrWhiteSpace(xml))
            return new JObject();

        // Deserializar el XML al tipo de modelo
        var serializer = new XmlSerializer(typeof(T));
        using var stringReader = new StringReader(xml);
        var deserializedObject = serializer.Deserialize(stringReader);

        // Convertir el objeto deserializado a JSON y luego a JObject
        var json = JsonConvert.SerializeObject(deserializedObject, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        });

        return JObject.Parse(json);
    }
}
