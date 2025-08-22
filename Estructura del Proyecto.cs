ServiceDtos

/// <summary>
    /// DTO para crear un servicio en el catálogo (mock).
    /// </summary>
    public class CreateServiceRequest
    {
        /// <summary>ID lógico único del servicio.</summary>
        [Required]
        public string ServiceId { get; set; } = string.Empty;

        /// <summary>Nombre visible.</summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>Entorno (DEV/UAT/PROD...).</summary>
        [Required]
        public string Env { get; set; } = "DEV";

        /// <summary>Tipo: HTTP, SOAP, GRPC, MQ, JOB, SFTP, TCP, CUSTOM.</summary>
        [Required]
        public string Kind { get; set; } = "HTTP";

        /// <summary>Endpoint o destino lógico (url, cola, etc.).</summary>
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>TTL en segundos para cachear el estado (mock, no usado).</summary>
        public int TtlSec { get; set; } = 30;

        /// <summary>Timeout de probe en segundos (mock, no usado).</summary>
        public int TimeoutSec { get; set; } = 3;
    }

    /// <summary>
    /// DTO para actualizar metadatos de un servicio.
    /// </summary>
    public class UpdateServiceRequest
    {
        /// <summary>Nombre visible.</summary>
        public string? Name { get; set; }

        /// <summary>Etiquetas operativas.</summary>
        public List<string>? Tags { get; set; }

        /// <summary>Criticidad (Low/Medium/High).</summary>
        public string? Criticality { get; set; }

        /// <summary>TTL en segundos para cachear el estado.</summary>
        public int? TtlSec { get; set; }

        /// <summary>Timeout de probe en segundos.</summary>
        public int? TimeoutSec { get; set; }
    }

    /// <summary>
    /// DTO para habilitar/deshabilitar un servicio.
    /// </summary>
    public class EnableServiceRequest
    {
        /// <summary>Indica si el servicio queda habilitado.</summary>
        public bool Enabled { get; set; } = true;
    }
