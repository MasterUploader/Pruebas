namespace RestUtilities.Connections.Models
{
    /// <summary>
    /// Configuración para servicios externos REST y SOAP.
    /// </summary>
    public class ServiceSettings
    {
        /// <summary>
        /// URL base del servicio.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Tipo de servicio (Ejemplo: "REST", "SOAP").
        /// </summary>
        public string ServiceType { get; set; }

        /// <summary>
        /// API Key si aplica.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Indica si debe usarse autenticación OAuth.
        /// </summary>
        public bool UseOAuth { get; set; }
    }
}



namespace RestUtilities.Connections.Models
{
    /// <summary>
    /// Configuración para conexiones WebSocket.
    /// </summary>
    public class WebSocketSettings
    {
        /// <summary>
        /// URL del servidor WebSocket.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Indica si la conexión debe mantenerse abierta.
        /// </summary>
        public bool KeepAlive { get; set; }
    }
}




namespace RestUtilities.Connections.Models
{
    /// <summary>
    /// Configuración para conexiones gRPC.
    /// </summary>
    public class GrpcSettings
    {
        /// <summary>
        /// Dirección del servidor gRPC.
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// Indica si debe usarse autenticación TLS.
        /// </summary>
        public bool UseTLS { get; set; }
    }
}
