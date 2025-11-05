Me hicieron falta mostrarte estas dos clases que ya tengo:

using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Datos del resultado de autorización manual.
/// </summary>
public class ResponseAuthorizationManualDto
{
    /// <summary>Código de respuesta (00 = aprobada).</summary>
    [JsonPropertyName("responseCode")]
    public string ResponseCode { get; set; } = string.Empty;

    /// <summary>Código de autorización si aplica.</summary>
    [JsonPropertyName("authorizationCode")]
    public string AuthorizationCode { get; set; } = string.Empty;

    /// <summary>Identificador único de la transacción.</summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Mensaje legible.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Marca de tiempo ISO 8601.</summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}



using System.Text.Json.Serialization;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Envoltura para cumplir el contrato:
/// {
///   "GetAuthorizationManualResponse": {
///     "GetAuthorizationManualResult": { ... }
///   }
/// }
/// </summary>
public class GetAuthorizationManualResultEnvelope
{
    [JsonPropertyName("GetAuthorizationManualResponse")]
    public GetAuthorizationManualResponseContainer GetAuthorizationManualResponse { get; set; } = new();
}

public class GetAuthorizationManualResponseContainer
{
    [JsonPropertyName("GetAuthorizationManualResult")]
    public ResponseAuthorizationManualDto GetAuthorizationManualResult { get; set; } = new();
}
