using System.Text.Json;
using System.Text.Json.Serialization;

namespace Logging.Helpers
{
    public static class JsonHelper
    {
        public static readonly JsonSerializerOptions PrettyPrint = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static readonly JsonSerializerOptions Compact = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static readonly JsonSerializerOptions PreserveCase = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Conserva PascalCase
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
    }
}
