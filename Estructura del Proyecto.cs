#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// Respuestas JSON en duro para el segmento de Catálogo de Servicios.
    /// </summary>
    public static class HardcodedServicesPayloads
    {
        // -------- GET /services
        public const string List_Ok = """
{
  "items": [
    {
      "serviceId": "payments-api",
      "name": "Payments API",
      "env": "PROD",
      "kind": "HTTP",
      "endpoint": "https://api.example.com/health",
      "ttlSec": 30,
      "timeoutSec": 3,
      "enabled": true,
      "tags": ["payments","critical"]
    },
    {
      "serviceId": "invoice-worker",
      "name": "Invoice Worker",
      "env": "PROD",
      "kind": "MQ",
      "endpoint": "topic:payments",
      "ttlSec": 30,
      "timeoutSec": 3,
      "enabled": true,
      "tags": ["billing"]
    }
  ],
  "meta": { "page": 1, "size": 50, "total": 120 }
}
""";

        public const string List_Fail = """
{
  "traceId": "bada55aa1100",
  "code": "forbidden",
  "title": "No autorizado",
  "detail": "La llave provista no tiene permisos para listar servicios."
}
""";

        // -------- GET /services/{serviceId}
        public const string Get_Ok = """
{
  "serviceId": "payments-api",
  "name": "Payments API",
  "env": "PROD",
  "kind": "HTTP",
  "endpoint": "https://api.example.com/health",
  "ttlSec": 30,
  "timeoutSec": 3,
  "expectedHttp": 200,
  "enabled": true,
  "tags": ["payments","critical"]
}
""";

        public const string Get_Fail = """
{
  "traceId": "c0ffee123456",
  "code": "service_not_found",
  "title": "Servicio no encontrado",
  "detail": "El serviceId solicitado no existe."
}
""";

        // -------- POST /services
        public const string Create_Ok = """
{
  "serviceId": "new-svc",
  "created": true
}
""";

        public const string Create_Fail = """
{
  "traceId": "deadbeef2024",
  "code": "conflict",
  "title": "ID duplicado",
  "detail": "Ya existe un servicio con 'serviceId' = 'new-svc'."
}
""";

        // -------- PUT /services/{serviceId}
        public const string Update_Ok = """
{
  "serviceId": "payments-api",
  "updated": true
}
""";

        public const string Update_Fail = """
{
  "traceId": "badc0de77",
  "code": "validation_error",
  "title": "Datos inválidos",
  "detail": "El campo 'ttlSec' debe ser > 0."
}
""";

        // -------- PATCH /services/{serviceId}/enable
        public const string Enable_Ok = """
{
  "serviceId": "payments-api",
  "enabled": false
}
""";

        public const string Enable_Fail = """
{
  "traceId": "faded00d42",
  "code": "not_allowed",
  "title": "Operación no permitida",
  "detail": "No se puede deshabilitar un servicio marcado como 'critical'."
}
""";

        // -------- DELETE /services/{serviceId}
        public const string Delete_Ok = """
{
  "serviceId": "old-svc",
  "deleted": true
}
""";

        public const string Delete_Fail = """
{
  "traceId": "b16b00b5",
  "code": "in_use",
  "title": "No se puede eliminar",
  "detail": "El servicio tiene dependencias activas."
}
""";
    }
}


#nullable enable
using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.Models
{
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
}




#nullable enable
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato del servicio del Catálogo (respuestas en duro).
    /// </summary>
    public interface IServicesCatalogService
    {
        /// <summary>JSON literal de GET /services.</summary>
        string GetList(string demo);

        /// <summary>JSON literal de GET /services/{serviceId}.</summary>
        string GetById(string serviceId, string demo);

        /// <summary>JSON literal de POST /services.</summary>
        string Create(CreateServiceRequest request, string demo);

        /// <summary>JSON literal de PUT /services/{serviceId}.</summary>
        string Update(string serviceId, UpdateServiceRequest request, string demo);

        /// <summary>JSON literal de PATCH /services/{serviceId}/enable.</summary>
        string Enable(string serviceId, EnableServiceRequest request, string demo);

        /// <summary>JSON literal de DELETE /services/{serviceId}.</summary>
        string Delete(string serviceId, string demo);
    }
}



#nullable enable
using MonitoringApi.Data;
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Implementación mock que devuelve JSON literal desde constantes.
    /// </summary>
    public class ServicesCatalogService : IServicesCatalogService
    {
        public string GetList(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedServicesPayloads.List_Fail
                : HardcodedServicesPayloads.List_Ok;

        public string GetById(string serviceId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedServicesPayloads.Get_Fail
                : HardcodedServicesPayloads.Get_Ok;

        public string Create(CreateServiceRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedServicesPayloads.Create_Fail
                : HardcodedServicesPayloads.Create_Ok;

        public string Update(string serviceId, UpdateServiceRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedServicesPayloads.Update_Fail
                : HardcodedServicesPayloads.Update_Ok;

        public string Enable(string serviceId, EnableServiceRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedServicesPayloads.Enable_Fail
                : HardcodedServicesPayloads.Enable_Ok;

        public string Delete(string serviceId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedServicesPayloads.Delete_Fail
                : HardcodedServicesPayloads.Delete_Ok;
    }
}


#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;
using MonitoringApi.Models;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Catálogo de servicios/microservicios registrados para monitoreo.
    /// MOCK: Respuestas en duro controladas por el query param `demo=ok|fail`.
    /// </summary>
    [ApiController]
    [Route("api/v1/services")]
    [Produces("application/json")]
    public class ServicesController : ControllerBase
    {
        private readonly IServicesCatalogService _svc;

        public ServicesController(IServicesCatalogService svc)
        {
            _svc = svc;
        }

        /// <summary>
        /// Lista los servicios registrados (metadatos).
        /// </summary>
        /// <param name="env">Filtro por entorno (DEV/UAT/PROD...). No aplica en el mock.</param>
        /// <param name="enabled">Filtra por habilitados/inhabilitados. No aplica en el mock.</param>
        /// <param name="demo">"ok" (default) o "fail" para simular error.</param>
        /// <returns>JSON literal con items y meta.</returns>
        /// <remarks>GET /api/v1/services?demo=ok</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetMany([FromQuery] string? env, [FromQuery] bool? enabled, [FromQuery] string? demo = "ok")
            => Content(_svc.GetList(demo ?? "ok"), "application/json");

        /// <summary>
        /// Devuelve los metadatos de un servicio por su ID.
        /// </summary>
        /// <param name="serviceId">Identificador lógico.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con el servicio o error simulado.</returns>
        /// <remarks>GET /api/v1/services/payments-api?demo=ok</remarks>
        [HttpGet("{serviceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetOne([FromRoute] string serviceId, [FromQuery] string? demo = "ok")
            => Content(_svc.GetById(serviceId, demo ?? "ok"), "application/json");

        /// <summary>
        /// Crea un nuevo servicio en el catálogo.
        /// </summary>
        /// <param name="request">Datos mínimos del servicio.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal indicando creación o error simulado.</returns>
        /// <remarks>
        /// POST /api/v1/services?demo=ok
        /// Body mínimo:
        /// { "serviceId":"new-svc","name":"New Svc","env":"UAT","kind":"HTTP","endpoint":"https://.../health" }
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Create([FromBody] CreateServiceRequest request, [FromQuery] string? demo = "ok")
            => Content(_svc.Create(request, demo ?? "ok"), "application/json");

        /// <summary>
        /// Actualiza metadatos de un servicio existente.
        /// </summary>
        /// <param name="serviceId">ID del servicio a actualizar.</param>
        /// <param name="request">Campos a modificar.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con resultado.</returns>
        /// <remarks>PUT /api/v1/services/{serviceId}?demo=ok</remarks>
        [HttpPut("{serviceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Update([FromRoute] string serviceId, [FromBody] UpdateServiceRequest request, [FromQuery] string? demo = "ok")
            => Content(_svc.Update(serviceId, request, demo ?? "ok"), "application/json");

        /// <summary>
        /// Habilita o deshabilita (baja lógica) un servicio.
        /// </summary>
        /// <param name="serviceId">ID del servicio.</param>
        /// <param name="request">Estado deseado.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con enabled final.</returns>
        /// <remarks>PATCH /api/v1/services/{serviceId}/enable?demo=ok</remarks>
        [HttpPatch("{serviceId}/enable")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Enable([FromRoute] string serviceId, [FromBody] EnableServiceRequest request, [FromQuery] string? demo = "ok")
            => Content(_svc.Enable(serviceId, request, demo ?? "ok"), "application/json");

        /// <summary>
        /// Elimina (o marca eliminado) un servicio del catálogo.
        /// </summary>
        /// <param name="serviceId">ID del servicio.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con deleted=true o error simulado.</returns>
        /// <remarks>DELETE /api/v1/services/{serviceId}?demo=ok</remarks>
        [HttpDelete("{serviceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Delete([FromRoute] string serviceId, [FromQuery] string? demo = "ok")
            => Content(_svc.Delete(serviceId, demo ?? "ok"), "application/json");
    }
}



builder.Services.AddScoped<MonitoringApi.Services.IServicesCatalogService, MonitoringApi.Services.ServicesCatalogService>();



