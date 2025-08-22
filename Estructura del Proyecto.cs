#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// Respuestas JSON en duro para el segmento de mantenimiento.
    /// </summary>
    public static class HardcodedMaintenancePayloads
    {
        // -------- GET /maintenance
        public const string List_Ok = """
[
  {
    "maintenanceId": "m1",
    "scope": { "env": "PROD" },
    "startsAtUtc": "2025-08-11T03:00:00Z",
    "endsAtUtc": "2025-08-11T04:00:00Z",
    "policy": "forceDegraded",
    "note": "Deploy ventanas"
  },
  {
    "maintenanceId": "m77",
    "scope": { "serviceId": "payments-api" },
    "startsAtUtc": "2025-08-12T01:00:00Z",
    "endsAtUtc": "2025-08-12T01:30:00Z",
    "policy": "suspendChecks",
    "note": "DB patch"
  }
]
""";

        public const string List_Fail = """
{
  "traceId": "badc0ffee77",
  "code": "backend_unavailable",
  "title": "No disponible",
  "detail": "No fue posible consultar las ventanas en este momento."
}
""";

        // -------- POST /maintenance
        public const string Create_Ok = """
{
  "maintenanceId": "m99",
  "created": true
}
""";

        public const string Create_Fail = """
{
  "traceId": "0ff1ce00",
  "code": "overlap",
  "title": "Traslape de ventana",
  "detail": "Ya existe una ventana activa que se superpone con el rango solicitado."
}
""";

        // -------- DELETE /maintenance/{maintenanceId}
        public const string Delete_Ok = """
{
  "deleted": true
}
""";

        public const string Delete_Fail = """
{
  "traceId": "baadf00d",
  "code": "not_found",
  "title": "Ventana no encontrada",
  "detail": "El maintenanceId proporcionado no existe."
}
""";
    }
}


#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// Respuestas JSON en duro para el segmento de dependencias.
    /// </summary>
    public static class HardcodedDependenciesPayloads
    {
        // -------- GET /services/{serviceId}/dependencies
        public const string List_Ok = """
[
  { "dependsOnServiceId": "auth-svc", "severity": "soft", "policy": "propagateDegraded" },
  { "dependsOnServiceId": "db-core", "severity": "hard", "policy": "propagateUnhealthy" }
]
""";

        public const string List_Fail = """
{
  "traceId": "d00df00d001",
  0"code": "forbidden",
  "title": "No autorizado",
  "detail": "Tu rol no permite listar dependencias de este servicio."
}
""";

        // -------- POST /services/{serviceId}/dependencies
        public const string Create_Ok = """
{
  "created": true
}
""";

        public const string Create_Fail = """
{
  "traceId": "deadfa11",
  "code": "conflict",
  "title": "Dependencia duplicada",
  "detail": "Ya existe una relación con 'dependsOnServiceId' = 'auth-svc'."
}
""";

        // -------- DELETE /services/{serviceId}/dependencies/{dependsOnServiceId}
        public const string Delete_Ok = """
{
  "deleted": true
}
""";

        public const string Delete_Fail = """
{
  "traceId": "c0ffee00",
  "code": "not_found",
  "title": "No existe la dependencia",
  "detail": "No se encontró la relación solicitada."
}
""";
    }
}




MaintenanceDtos
#nullable enable
using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.Models
{
    /// <summary>
    /// Solicitud para crear una ventana de mantenimiento.
    /// </summary>
    public class CreateMaintenanceRequest
    {
        /// <summary>Ámbito de la ventana (por serviceId, env o tag).</summary>
        [Required]
        public MaintenanceScope Scope { get; set; } = new();

        /// <summary>Inicio de la ventana (UTC, ISO-8601).</summary>
        [Required]
        public string StartsAtUtc { get; set; } = string.Empty;

        /// <summary>Fin de la ventana (UTC, ISO-8601).</summary>
        [Required]
        public string EndsAtUtc { get; set; } = string.Empty;

        /// <summary>Política: forceDegraded, suspendChecks, etc.</summary>
        [Required]
        public string Policy { get; set; } = "suspendChecks";

        /// <summary>Nota descriptiva.</summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// Define el alcance objetivo de la ventana de mantenimiento.
    /// Proporciona uno de los campos (serviceId, env o tag).
    /// </summary>
    public class MaintenanceScope
    {
        /// <summary>Identificador del servicio afectado (si aplica).</summary>
        public string? ServiceId { get; set; }

        /// <summary>Entorno afectado (DEV/UAT/PROD...).</summary>
        public string? Env { get; set; }

        /// <summary>Etiqueta/Tag afectada (p.ej. "payments").</summary>
        public string? Tag { get; set; }
    }
}



DependencyDtos
#nullable enable
using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.Models
{
    /// <summary>
    /// Datos para crear una relación de dependencia entre servicios.
    /// </summary>
    public class CreateDependencyRequest
    {
        /// <summary>ID del servicio del cual depende el actual.</summary>
        [Required]
        public string DependsOnServiceId { get; set; } = string.Empty;

        /// <summary>Severidad de la dependencia: soft o hard.</summary>
        [Required]
        public string Severity { get; set; } = "soft";

        /// <summary>
        /// Política de propagación: p. ej. propagateDegraded, propagateUnhealthy.
        /// </summary>
        [Required]
        public string Policy { get; set; } = "propagateDegraded";
    }
}


#nullable enable
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato de negocio para ventanas de mantenimiento (mock).
    /// </summary>
    public interface IMaintenanceService
    {
        /// <summary>JSON literal de GET /maintenance.</summary>
        string GetList(string demo);

        /// <summary>JSON literal de POST /maintenance.</summary>
        string Create(CreateMaintenanceRequest request, string demo);

        /// <summary>JSON literal de DELETE /maintenance/{maintenanceId}.</summary>
        string Delete(string maintenanceId, string demo);
    }
}


#nullable enable
using MonitoringApi.Data;
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Implementación mock que retorna JSON literal desde constantes.
    /// </summary>
    public class MaintenanceService : IMaintenanceService
    {
        public string GetList(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedMaintenancePayloads.List_Fail
                : HardcodedMaintenancePayloads.List_Ok;

        public string Create(CreateMaintenanceRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedMaintenancePayloads.Create_Fail
                : HardcodedMaintenancePayloads.Create_Ok;

        public string Delete(string maintenanceId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedMaintenancePayloads.Delete_Fail
                : HardcodedMaintenancePayloads.Delete_Ok;
    }
}


#nullable enable
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato de negocio para dependencias de servicios (mock).
    /// </summary>
    public interface IDependenciesService
    {
        /// <summary>JSON literal de GET /services/{serviceId}/dependencies.</summary>
        string GetList(string serviceId, string demo);

        /// <summary>JSON literal de POST /services/{serviceId}/dependencies.</summary>
        string Create(string serviceId, CreateDependencyRequest request, string demo);

        /// <summary>JSON literal de DELETE /services/{serviceId}/dependencies/{dependsOnServiceId}.</summary>
        string Delete(string serviceId, string dependsOnServiceId, string demo);
    }
}


#nullable enable
using MonitoringApi.Data;
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Implementación mock que retorna JSON literal desde constantes.
    /// </summary>
    public class DependenciesService : IDependenciesService
    {
        public string GetList(string serviceId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedDependenciesPayloads.List_Fail
                : HardcodedDependenciesPayloads.List_Ok;

        public string Create(string serviceId, CreateDependencyRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedDependenciesPayloads.Create_Fail
                : HardcodedDependenciesPayloads.Create_Ok;

        public string Delete(string serviceId, string dependsOnServiceId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedDependenciesPayloads.Delete_Fail
                : HardcodedDependenciesPayloads.Delete_Ok;
    }
}


#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;
using MonitoringApi.Models;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Administra ventanas de mantenimiento que afectan el estado reportado.
    /// MOCK: respuestas en duro controladas con ?demo=ok|fail.
    /// </summary>
    [ApiController]
    [Route("api/v1/maintenance")]
    [Produces("application/json")]
    public class MaintenanceController : ControllerBase
    {
        private readonly IMaintenanceService _service;

        public MaintenanceController(IMaintenanceService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista ventanas de mantenimiento (activas o programadas).
        /// </summary>
        /// <param name="active">Si es true, solo activas. (No aplicado en mock)</param>
        /// <param name="from">Inicio de ventana de consulta. (No aplicado en mock)</param>
        /// <param name="to">Fin de ventana de consulta. (No aplicado en mock)</param>
        /// <param name="demo">"ok" (default) o "fail".</param>
        /// <returns>JSON literal con arreglo de ventanas o error simulado.</returns>
        /// <remarks>GET /api/v1/maintenance?demo=ok</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetMany([FromQuery] bool? active, [FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? demo = "ok")
            => Content(_service.GetList(demo ?? "ok"), "application/json");

        /// <summary>
        /// Crea una ventana de mantenimiento (por serviceId, env o tag).
        /// </summary>
        /// <param name="request">Ámbito (scope) y periodo de la ventana.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con maintenanceId o error simulado.</returns>
        /// <remarks>POST /api/v1/maintenance?demo=ok</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Create([FromBody] CreateMaintenanceRequest request, [FromQuery] string? demo = "ok")
            => Content(_service.Create(request, demo ?? "ok"), "application/json");

        /// <summary>
        /// Elimina/cancela una ventana de mantenimiento.
        /// </summary>
        /// <param name="maintenanceId">Identificador de la ventana.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con deleted=true o error simulado.</returns>
        /// <remarks>DELETE /api/v1/maintenance/{maintenanceId}?demo=ok</remarks>
        [HttpDelete("{maintenanceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Delete([FromRoute] string maintenanceId, [FromQuery] string? demo = "ok")
            => Content(_service.Delete(maintenanceId, demo ?? "ok"), "application/json");
    }
}


#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;
using MonitoringApi.Models;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Administra dependencias declaradas para un servicio (upstream/downstream).
    /// MOCK: las respuestas se simulan con JSON literal según ?demo=ok|fail.
    /// </summary>
    [ApiController]
    [Route("api/v1/services/{serviceId}/dependencies")]
    [Produces("application/json")]
    public class DependenciesController : ControllerBase
    {
        private readonly IDependenciesService _service;

        public DependenciesController(IDependenciesService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista las dependencias del servicio indicado.
        /// </summary>
        /// <param name="serviceId">ID lógico del servicio.</param>
        /// <param name="demo">"ok" (default) para respuesta buena, "fail" para error simulado.</param>
        /// <returns>JSON literal con el arreglo de dependencias o error.</returns>
        /// <remarks>GET /api/v1/services/{serviceId}/dependencies?demo=ok</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetMany([FromRoute] string serviceId, [FromQuery] string? demo = "ok")
            => Content(_service.GetList(serviceId, demo ?? "ok"), "application/json");

        /// <summary>
        /// Crea una dependencia para el servicio indicado.
        /// </summary>
        /// <param name="serviceId">ID del servicio dueño de la relación.</param>
        /// <param name="request">Detalle de la dependencia.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con created=true o error simulado.</returns>
        /// <remarks>POST /api/v1/services/{serviceId}/dependencies?demo=ok</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Create([FromRoute] string serviceId, [FromBody] CreateDependencyRequest request, [FromQuery] string? demo = "ok")
            => Content(_service.Create(serviceId, request, demo ?? "ok"), "application/json");

        /// <summary>
        /// Elimina una dependencia específica.
        /// </summary>
        /// <param name="serviceId">ID del servicio dueño de la relación.</param>
        /// <param name="dependsOnServiceId">ID del servicio del que depende.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con deleted=true o error simulado.</returns>
        /// <remarks>DELETE /api/v1/services/{serviceId}/dependencies/{dependsOnServiceId}?demo=ok</remarks>
        [HttpDelete("{dependsOnServiceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Delete([FromRoute] string serviceId, [FromRoute] string dependsOnServiceId, [FromQuery] string? demo = "ok")
            => Content(_service.Delete(serviceId, dependsOnServiceId, demo ?? "ok"), "application/json");
    }
}


builder.Services.AddScoped<MonitoringApi.Services.IDependenciesService, MonitoringApi.Services.DependenciesService>();
builder.Services.AddScoped<MonitoringApi.Services.IMaintenanceService, MonitoringApi.Services.MaintenanceService>();



