No, ahora el request sera así:


using System.ComponentModel.DataAnnotations;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

public class RequestModelAuthorization
{
    [Required]
    public RequestHeader Header { get; set; } = new();

    [Required]
    public RequestModelAuthorization Body { get; set; } = new();
}


using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos;

public class RequestHeader
{
    [Required]
    [JsonPropertyName("h-request-id")]
    public string HRequestId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("h-channel")]
    public string HChannel { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("h-terminal")]
    public string HTerminal { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("h-organization")]
    public string HOrganization { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("h-user-id")]
    public string HUserId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("h-provider")]
    public string HProvider { get; set; } = string.Empty;


    [JsonPropertyName("h-session-id")]
    public string HSessionId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [JsonPropertyName("h-client-ip")]
    public string HClientIp { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("h-timestamp")]
    public string HTimestamp { get; set; } = string.Empty;
}




using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

namespace Pagos_Davivienda_TNP.Models.Dtos;

public class RequestBody
{
    public RequestModelAuthorization RequestModelAuthorization { get; set; } = new();
}
