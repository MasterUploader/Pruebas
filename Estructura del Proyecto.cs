<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>

</Project>



using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestUtilities.Common.Helpers;

public static class JsonHelper
{
    public static string ToJson(object obj, bool prettyPrint = false, bool useCamelCase = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(obj, options);
    }

    public static T? FromJson<T>(string json, bool useCamelCase = true)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null
        };
        return JsonSerializer.Deserialize<T>(json, options);
    }
}


using System.Text.Json;

namespace RestUtilities.Common.Helpers;

public static class JsonHelper
{
    public static string PrettyPrint(string json, JsonSerializerOptions? options = null)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, options ?? new JsonSerializerOptions { WriteIndented = true });
    }

    public static string Serialize<T>(T value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, options ?? new JsonSerializerOptions());
    }

    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(json, options ?? new JsonSerializerOptions());
    }
}


using System.Text.Json;

namespace RestUtilities.Common.Helpers;

/// <summary>
/// Métodos auxiliares para manipular y formatear JSON.
/// </summary>
public static class JsonHelper
{
    public static string PrettyPrint(string json, JsonSerializerOptions? options = null)
    {
        var parsed = JsonSerializer.Deserialize<object>(json);
        return JsonSerializer.Serialize(parsed, options ?? new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
        => JsonSerializer.Deserialize<T>(json, options);

    public static string Serialize(object obj, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(obj, options);
}
