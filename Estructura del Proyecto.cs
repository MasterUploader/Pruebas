using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger opcional para probar rápido (puedes quitarlo)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Inyección de dependencias (Status)
builder.Services.AddScoped<Services.IStatusService, Services.StatusService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();



#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Controlador para exponer el estado on-demand de servicios/microservicios.
    /// Devuelve JSON literal (simulado) para "ok" o "fail" según el parámetro de demo.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class StatusController : ControllerBase
    {
        private readonly IStatusService _service;

        public StatusController(IStatusService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista el estado actual normalizado de los servicios.
        /// </summary>
        /// <param name="env">Filtro por entorno (DEV/UAT/PROD...). No aplica en el mock.</param>
        /// <param name="tag">Filtro por etiqueta. No aplica en el mock.</param>
        /// <param name="include">checks,dependencies,history. No aplica en el mock.</param>
        /// <param name="history">Tamaño de historial si include=history. No aplica en el mock.</param>
        /// <param name="demo">Modo demo: "ok" (por defecto) o "fail" para forzar JSON de error simulado.</param>
        /// <returns>JSON literal con la lista de estados.</returns>
        /// <remarks>
        /// Ejemplos:
        /// GET /api/v1/status?demo=ok
        /// GET /api/v1/status?demo=fail
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetStatusList([FromQuery] string? env, [FromQuery] string? tag,
            [FromQuery] string? include, [FromQuery] int? history, [FromQuery] string? demo = "ok")
        {
            var json = _service.GetStatusList(demo ?? "ok");
            return Content(json, "application/json");
        }

        /// <summary>
        /// Devuelve el estado detallado de un servicio concreto.
        /// </summary>
        /// <param name="serviceId">Identificador lógico del servicio.</param>
        /// <param name="include">checks,dependencies,history. No aplica en el mock.</param>
        /// <param name="history">Cantidad de puntos de historial. No aplica en el mock.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal del estado del servicio.</returns>
        /// <remarks>
        /// Ejemplos:
        /// GET /api/v1/status/payments-api?demo=ok
        /// GET /api/v1/status/payments-api?demo=fail
        /// </remarks>
        [HttpGet("{serviceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetStatusById([FromRoute] string serviceId,
            [FromQuery] string? include, [FromQuery] int? history, [FromQuery] string? demo = "ok")
        {
            var json = _service.GetStatusById(serviceId, demo ?? "ok");
            return Content(json, "application/json");
        }

        /// <summary>
        /// Devuelve un resumen agregado de estados (conteos y percentiles).
        /// </summary>
        /// <param name="groupBy">env, tag, owner, kind. No aplica en el mock.</param>
        /// <param name="from">Inicio de ventana. No aplica en el mock.</param>
        /// <param name="to">Fin de ventana. No aplica en el mock.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con resumen.</returns>
        /// <remarks>
        /// Ejemplos:
        /// GET /api/v1/status/summary?demo=ok
        /// GET /api/v1/status/summary?demo=fail
        /// </remarks>
        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetSummary([FromQuery] string? groupBy, [FromQuery] string? from,
            [FromQuery] string? to, [FromQuery] string? demo = "ok")
        {
            var json = _service.GetStatusSummary(demo ?? "ok");
            return Content(json, "application/json");
        }

        /// <summary>
        /// Fuerza el refresco del estado de uno o varios servicios.
        /// </summary>
        /// <param name="request">Cuerpo con serviceIds y bandera coalesce.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal indicando servicios refrescados o error simulado.</returns>
        /// <remarks>
        /// POST /api/v1/status/refresh?demo=ok
        /// Body:
        /// {
        ///   "serviceIds": ["payments-api", "auth-svc"],
        ///   "coalesce": true
        /// }
        /// </remarks>
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Refresh([FromBody] Models.RefreshRequest request, [FromQuery] string? demo = "ok")
        {
            var json = _service.RefreshStatus(request, demo ?? "ok");
            return Content(json, "application/json");
        }
    }
}



#nullable enable
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato de servicio para proveer respuestas simuladas de los endpoints de Status.
    /// </summary>
    public interface IStatusService
    {
        /// <summary>
        /// Devuelve el JSON literal de la lista de estados.
        /// </summary>
        /// <param name="demo">"ok" para respuesta buena, "fail" para respuesta mala.</param>
        string GetStatusList(string demo);

        /// <summary>
        /// Devuelve el JSON literal del estado por ID.
        /// </summary>
        /// <param name="serviceId">ID del servicio.</param>
        /// <param name="demo">"ok" o "fail".</param>
        string GetStatusById(string serviceId, string demo);

        /// <summary>
        /// Devuelve el JSON literal del resumen agregado.
        /// </summary>
        /// <param name="demo">"ok" o "fail".</param>
        string GetStatusSummary(string demo);

        /// <summary>
        /// Devuelve el JSON literal del resultado de refrescar estado.
        /// </summary>
        /// <param name="request">Petición de refresco (no se usa en el mock).</param>
        /// <param name="demo">"ok" o "fail".</param>
        string RefreshStatus(RefreshRequest request, string demo);
    }
}



#nullable enable
using MonitoringApi.Data;
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Implementación simulada de IStatusService que retorna JSON literal desde constantes.
    /// </summary>
    public class StatusService : IStatusService
    {
        public string GetStatusList(string demo)
            => demo?.ToLowerInvariant() == "fail"
                ? HardcodedStatusPayloads.StatusList_Fail
                : HardcodedStatusPayloads.StatusList_Ok;

        public string GetStatusById(string serviceId, string demo)
            => demo?.ToLowerInvariant() == "fail"
                ? HardcodedStatusPayloads.StatusById_Fail
                : HardcodedStatusPayloads.StatusById_Ok;

        public string GetStatusSummary(string demo)
            => demo?.ToLowerInvariant() == "fail"
                ? HardcodedStatusPayloads.StatusSummary_Fail
                : HardcodedStatusPayloads.StatusSummary_Ok;

        public string RefreshStatus(RefreshRequest request, string demo)
            => demo?.ToLowerInvariant() == "fail"
                ? HardcodedStatusPayloads.StatusRefresh_Fail
                : HardcodedStatusPayloads.StatusRefresh_Ok;
    }
}




#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// Respuestas JSON en duro (literal) para el módulo Status.
    /// </summary>
    public static class HardcodedStatusPayloads
    {
        /// <summary>Lista de estados (OK).</summary>
        public const string StatusList_Ok = """
[
  {
    "serviceId": "payments-api",
    "env": "PROD",
    "interface": "HTTP",
    "endpoint": "/payments/{id}",
    "method": "GET",
    "status": "Healthy",
    "latencyMs": 120,
    "checkedAtUtc": "2025-08-11T16:59:58Z",
    "expiresAtUtc": "2025-08-11T17:00:28Z",
    "details": { "reason": "OK" }
  },
  {
    "serviceId": "auth-svc",
    "env": "PROD",
    "interface": "SOAP",
    "endpoint": "https://idp.example.com/Auth.svc",
    "method": "CALL",
    "status": "Degraded",
    "latencyMs": 320,
    "checkedAtUtc": "2025-08-11T16:59:52Z",
    "expiresAtUtc": "2025-08-11T17:00:22Z",
    "details": { "note": "XPath OK; latencia alta" }
  }
]
""";

        /// <summary>Lista de estados (FAIL).</summary>
        public const string StatusList_Fail = """
{
  "traceId": "7b2c7f2d9d2e4a1a",
  "code": "backend_unavailable",
  "title": "No se pudo obtener el estado",
  "detail": "Tiempo de espera agotado al consultar 3 servicios."
}
""";

        /// <summary>Estado por serviceId (OK).</summary>
        public const string StatusById_Ok = """
{
  "serviceId": "payments-api",
  "env": "PROD",
  "status": "Healthy",
  "latencyMs": 118,
  "checkedAtUtc": "2025-08-11T16:59:58Z",
  "expiresAtUtc": "2025-08-11T17:00:28Z",
  "checks": [
    { "checkId": "c1", "probeType": "GET", "status": "Healthy", "latencyMs": 118, "statusCode": 200 }
  ],
  "dependencies": [
    { "dependsOnServiceId": "db-core", "status": "Healthy" }
  ],
  "history": [
    { "timestampUtc": "2025-08-11T16:58:00Z", "status": "Healthy", "latencyMs": 150 }
  ]
}
""";

        /// <summary>Estado por serviceId (FAIL).</summary>
        public const string StatusById_Fail = """
{
  "traceId": "a13f0c29dd5c4b0e",
  "code": "service_not_found",
  "title": "Servicio no registrado",
  "detail": "El serviceId solicitado no existe en catálogo."
}
""";

        /// <summary>Resumen (OK).</summary>
        public const string StatusSummary_Ok = """
{
  "groups": [
    {
      "key": { "env": "PROD" },
      "counts": { "healthy": 25, "degraded": 3, "unhealthy": 1, "unknown": 0 },
      "latency": { "p50Ms": 110, "p95Ms": 420, "p99Ms": 800 }
    }
  ]
}
""";

        /// <summary>Resumen (FAIL).</summary>
        public const string StatusSummary_Fail = """
{
  "traceId": "de0adbee5a5f0042",
  "code": "invalid_range",
  "title": "Rango de tiempo no válido",
  "detail": "'from' debe ser anterior a 'to'."
}
""";

        /// <summary>Refresh (OK).</summary>
        public const string StatusRefresh_Ok = """
{
  "refreshed": ["payments-api", "auth-svc"],
  "skipped": []
}
""";

        /// <summary>Refresh (FAIL).</summary>
        public const string StatusRefresh_Fail = """
{
  "traceId": "ff12aa90c0ffee77",
  "code": "coalesce_lock",
  "title": "Operación bloqueada",
  "detail": "Hay un refresco en curso para uno o más servicios."
}
""";
    }
}



#nullable enable
using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.Models
{
    /// <summary>
    /// Modelo de entrada para el endpoint de refresh de estado.
    /// </summary>
    public class RefreshRequest
    {
        /// <summary>
        /// Identificadores lógicos de servicios a refrescar.
        /// </summary>
        [Required]
        public List<string> ServiceIds { get; set; } = new();

        /// <summary>
        /// Si es true, colapsa peticiones concurrentes para los mismos servicios (sugerido).
        /// En el mock no cambia el resultado.
        /// </summary>
        public bool Coalesce { get; set; } = true;
    }
}


