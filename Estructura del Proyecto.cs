/// <summary>
/// DTO global para recibir cualquier tipo de solicitud en la API REST.
/// </summary>
public class SoapRequestDto
{
    /// <summary>
    /// Tipo de transacción (Ej: GSRV, GPRD, GPAA, etc.).
    /// </summary>
    public string TransactionType { get; set; }

    /// <summary>
    /// Código del agente solicitante.
    /// </summary>
    public string AgentCode { get; set; }

    /// <summary>
    /// Contiene la solicitud específica, puede ser de diferentes tipos.
    /// Se convierte dinámicamente con un `JsonConverter`.
    /// </summary>
    [JsonConverter(typeof(BaseRequestConverter))]
    public BaseRequest Request { get; set; }
}
