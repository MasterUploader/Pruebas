public static class JsonHelper
{
    // Opciones individuales reutilizables
    public static JsonNamingPolicy CamelCase => JsonNamingPolicy.CamelCase;
    public static JsonNamingPolicy PascalCase => null; // null = no aplicar naming policy
    public static JsonIgnoreCondition IgnoreNull => JsonIgnoreCondition.WhenWritingNull;
    public static JsonIgnoreCondition IncludeAll => JsonIgnoreCondition.Never;
    public static JsonNumberHandling AllowNumbersAsString => JsonNumberHandling.AllowReadingFromString;

    // Métodos generadores de opciones completas
    public static JsonSerializerOptions CreateOptions(
        bool indented = true,
        JsonNamingPolicy? namingPolicy = null,
        JsonIgnoreCondition ignoreCondition = JsonIgnoreCondition.Never,
        JsonNumberHandling? numberHandling = null)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = namingPolicy,
            DefaultIgnoreCondition = ignoreCondition,
            NumberHandling = numberHandling ?? JsonNumberHandling.Strict
        };
    }

    // Ejemplos predefinidos reutilizables
    public static JsonSerializerOptions PrettyPrintCamelCase =>
        CreateOptions(indented: true, namingPolicy: CamelCase, ignoreCondition: IgnoreNull);

    public static JsonSerializerOptions PrettyPrintPascalCase =>
        CreateOptions(indented: true, namingPolicy: PascalCase, ignoreCondition: IgnoreNull);

    public static JsonSerializerOptions CompactCamelCase =>
        CreateOptions(indented: false, namingPolicy: CamelCase, ignoreCondition: IgnoreNull);
}
