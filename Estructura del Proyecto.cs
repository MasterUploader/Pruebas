#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// Payloads en duro para /health/* y /metrics.
    /// </summary>
    public static class HardcodedHealthPayloads
    {
        // -------- GET /api/v1/health/live
        public const string Live_Ok = """
{
  "status": "Live"
}
""";

        public const string Live_Fail = """
{
  "traceId": "live-err-0001",
  "code": "app_initializing",
  "title": "Inicializando",
  "detail": "El proceso aún no está listo para atender solicitudes."
}
""";

        // -------- GET /api/v1/health/ready
        public const string Ready_Ok = """
{
  "status": "Ready",
  "checks": {
    "database": "ok",
    "cache": "ok"
  }
}
""";

        public const string Ready_Fail = """
{
  "traceId": "ready-err-0002",
  "code": "dependency_down",
  "title": "Dependencia caída",
  "detail": "Fallo de conectividad hacia la base de datos."
}
""";

        // -------- GET /api/v1/metrics (text/plain)
        public const string Metrics_Text = """
# HELP api_request_duration_seconds Request duration
# TYPE api_request_duration_seconds histogram
api_request_duration_seconds_bucket{le="0.1"} 120
api_request_duration_seconds_bucket{le="0.3"} 320
api_request_duration_seconds_bucket{le="1"}  480
api_request_duration_seconds_bucket{le="+Inf"} 500
api_request_duration_seconds_sum 85.6
api_request_duration_seconds_count 500

# HELP api_requests_total Total requests
# TYPE api_requests_total counter
api_requests_total 500
""";

        public const string Metrics_Fail = """
{
  "traceId": "metrics-err-0003",
  "code": "exporter_unavailable",
  "title": "Exportador no disponible",
  "detail": "No se pudieron recolectar métricas en este momento."
}
""";
    }
}

#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// Payloads en duro para /config, /validate, /discovery/http y /discovery/tcp.
    /// </summary>
    public static class HardcodedUtilitiesPayloads
    {
        // -------- GET /api/v1/config
        public const string Config_Ok = """
{
  "defaults": { "ttlSec": 30, "timeoutSec": 3 },
  "limits": { "liveStreamsPerKey": 10, "maxGroupByFields": 4, "maxWindowDays": 7 },
  "featureFlags": { "enableDryRun": true, "enableDiscovery": true }
}
""";

        public const string Config_Fail = """
{
  "traceId": "cfg-deadbeef",
  "code": "backend_unavailable",
  "title": "No disponible",
  "detail": "No fue posible leer la configuración operativa."
}
""";

        // -------- POST /api/v1/validate
        public const string Validate_Ok = """
{
  "valid": true,
  "warnings": [],
  "errors": []
}
""";

        public const string Validate_Fail = """
{
  "valid": false,
  "warnings": ["El endpoint no especifica esquema (http/https); usando https por defecto."],
  "errors": ["ProbeType inválido: 'XGET'. Valores soportados: GET, POST, SOAP_ACTION, GRPC_CALL, TCP_CONNECT."]
}
""";

        // -------- POST /api/v1/discovery/http
        public const string DiscoveryHttp_Ok = """
{
  "proposals": [
    { "serviceId": "api", "endpoint": "/health", "probeType": "GET" },
    { "serviceId": "auth", "endpoint": "/.well-known/openid-configuration", "probeType": "GET" }
  ],
  "meta": { "bases": ["https://api.example.com","https://auth.example.com"] }
}
""";

        public const string DiscoveryHttp_Fail = """
{
  "traceId": "disc-http-err01",
  "code": "not_allowed",
  "title": "No permitido",
  "detail": "La base 'http://interna.local' no está en la allow-list."
}
""";

        // -------- POST /api/v1/discovery/tcp
        public const string DiscoveryTcp_Ok = """
{
  "open": [
    { "host": "svc1.local", "port": 443 },
    { "host": "svc2.local", "port": 8443 }
  ],
  "meta": { "hosts": ["svc1.local"], "ports": [443,8443] }
}
""";

        public const string DiscoveryTcp_Fail = """
{
  "traceId": "disc-tcp-err01",
  "code": "scan_blocked",
  "title": "Escaneo bloqueado",
  "detail": "El rango solicitado se encuentra en la deny-list."
}
""";
    }
}




UtilitiesDtos
#nullable enable
using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.Models
{
    /// <summary>
    /// Solicitud para validación sintáctica semántica (sin persistir/ejecutar).
    /// </summary>
    public class ValidateRequest
    {
        /// <summary>Definición de servicio (mínima) a validar.</summary>
        public ServiceDefinitionLite? Service { get; set; }

        /// <summary>Checks propuestos para validar.</summary>
        public List<CheckDefinitionLite>? Checks { get; set; }
    }

    /// <summary>
    /// Definición mínima de servicio para fines de validación.
    /// </summary>
    public class ServiceDefinitionLite
    {
        [Required] public string ServiceId { get; set; } = string.Empty;
        [Required] public string Env { get; set; } = "DEV";
        [Required] public string Kind { get; set; } = "HTTP";
        [Required] public string Endpoint { get; set; } = string.Empty;
        public int? TtlSec { get; set; }
        public int? TimeoutSec { get; set; }
    }

    /// <summary>
    /// Definición mínima de check para validación (similar a CreateCheckRequest).
    /// </summary>
    public class CheckDefinitionLite
    {
        [Required] public string ProbeType { get; set; } = "GET";
        public string? Operation { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string? Payload { get; set; }
        public CheckAssertionsLite? Assertions { get; set; }
    }

    /// <summary>Assertions mínimas para validar.</summary>
    public class CheckAssertionsLite
    {
        public int? ExpectStatus { get; set; }
        public string? JsonPath { get; set; }
        public string? Xpath { get; set; }
    }

    /// <summary>
    /// Solicitud de descubrimiento HTTP.
    /// </summary>
    public class DiscoveryHttpRequest
    {
        /// <summary>Lista de URLs base (https://...)</summary>
        [Required] public List<string> Bases { get; set; } = new();
    }

    /// <summary>
    /// Solicitud de descubrimiento TCP.
    /// </summary>
    public class DiscoveryTcpRequest
    {
        /// <summary>Hosts a evaluar (DNS/IPv4/IPv6).</summary>
        [Required] public List<string> Hosts { get; set; } = new();

        /// <summary>Puertos a verificar.</summary>
        [Required] public List<int> Ports { get; set; } = new();
    }
}



#nullable enable
namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato para salud del API y métricas (mock).
    /// </summary>
    public interface IHealthService
    {
        /// <summary>JSON literal de GET /api/v1/health/live.</summary>
        string GetLive(string demo);

        /// <summary>JSON literal de GET /api/v1/health/ready.</summary>
        string GetReady(string demo);

        /// <summary>Texto de métricas (Prometheus) para ok.</summary>
        string GetMetricsOk();

        /// <summary>JSON de error para métricas (fail).</summary>
        string GetMetricsFail();
    }
}





#nullable enable
using MonitoringApi.Data;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Implementación mock para salud y métricas del propio API.
    /// </summary>
    public class HealthService : IHealthService
    {
        public string GetLive(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedHealthPayloads.Live_Fail
                : HardcodedHealthPayloads.Live_Ok;

        public string GetReady(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedHealthPayloads.Ready_Fail
                : HardcodedHealthPayloads.Ready_Ok;

        public string GetMetricsOk() => HardcodedHealthPayloads.Metrics_Text;
        public string GetMetricsFail() => HardcodedHealthPayloads.Metrics_Fail;
    }
}



#nullable enable
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato del módulo de utilidades (mock).
    /// </summary>
    public interface IUtilitiesService
    {
        /// <summary>JSON literal de GET /api/v1/config.</summary>
        string GetConfig(string demo);

        /// <summary>JSON literal de POST /api/v1/validate.</summary>
        string Validate(ValidateRequest request, string demo);

        /// <summary>JSON literal de POST /api/v1/discovery/http.</summary>
        string DiscoveryHttp(DiscoveryHttpRequest request, string demo);

        /// <summary>JSON literal de POST /api/v1/discovery/tcp.</summary>
        string DiscoveryTcp(DiscoveryTcpRequest request, string demo);
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
    public class UtilitiesService : IUtilitiesService
    {
        public string GetConfig(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedUtilitiesPayloads.Config_Fail
                : HardcodedUtilitiesPayloads.Config_Ok;

        public string Validate(ValidateRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedUtilitiesPayloads.Validate_Fail
                : HardcodedUtilitiesPayloads.Validate_Ok;

        public string DiscoveryHttp(DiscoveryHttpRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedUtilitiesPayloads.DiscoveryHttp_Fail
                : HardcodedUtilitiesPayloads.DiscoveryHttp_Ok;

        public string DiscoveryTcp(DiscoveryTcpRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedUtilitiesPayloads.DiscoveryTcp_Fail
                : HardcodedUtilitiesPayloads.DiscoveryTcp_Ok;
    }
}



#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Exposición de métricas del propio API (formato Prometheus o texto plano).
    /// MOCK: Devuelve texto en duro; en fail devuelve JSON de error.
    /// </summary>
    [ApiController]
    public class MetricsController : ControllerBase
    {
        private readonly IHealthService _svc;

        public MetricsController(IHealthService svc)
        {
            _svc = svc;
        }

        /// <summary>
        /// Métricas del API (Prometheus exposition format simulado).
        /// </summary>
        /// <param name="demo">"ok" (default) para texto de métricas; "fail" para error JSON.</param>
        /// <returns>text/plain en ok; application/json en fail.</returns>
        /// <remarks>GET /api/v1/metrics?demo=ok</remarks>
        [HttpGet]
        [Route("api/v1/metrics")]
        public IActionResult Metrics([FromQuery] string? demo = "ok")
        {
            if ((demo ?? "ok").ToLowerInvariant() == "fail")
                return Content(_svc.GetMetricsFail(), "application/json");

            return Content(_svc.GetMetricsOk(), "text/plain");
        }
    }
}


#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Endpoints de salud del propio API (liveness/readiness).
    /// MOCK: Respuestas en duro con ?demo=ok|fail.
    /// </summary>
    [ApiController]
    [Route("api/v1/health")]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly IHealthService _svc;

        public HealthController(IHealthService svc)
        {
            _svc = svc;
        }

        /// <summary>Liveness: solo verifica que el proceso esté arriba.</summary>
        /// <param name="demo">"ok" (default) o "fail".</param>
        /// <returns>JSON literal {"status":"Live"} o error simulado.</returns>
        /// <remarks>GET /api/v1/health/live?demo=ok</remarks>
        [HttpGet("live")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Live([FromQuery] string? demo = "ok")
            => Content(_svc.GetLive(demo ?? "ok"), "application/json");

        /// <summary>
        /// Readiness: verifica conectividad a BD/caché y dependencias mínimas del API.
        /// </summary>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal {"status":"Ready"} o error simulado.</returns>
        /// <remarks>GET /api/v1/health/ready?demo=ok</remarks>
        [HttpGet("ready")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Ready([FromQuery] string? demo = "ok")
            => Content(_svc.GetReady(demo ?? "ok"), "application/json");
    }
}



#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;
using MonitoringApi.Models;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Utilidades generales: configuración visible, validación de definiciones
    /// y descubrimiento asistido de endpoints/puertos.
    /// MOCK: Respuestas en duro usando ?demo=ok|fail.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Produces("application/json")]
    public class UtilitiesController : ControllerBase
    {
        private readonly IUtilitiesService _svc;

        public UtilitiesController(IUtilitiesService svc)
        {
            _svc = svc;
        }

        /// <summary>
        /// Devuelve configuración operativa no sensible del API.
        /// </summary>
        /// <param name="demo">"ok" (default) o "fail".</param>
        /// <returns>JSON literal con defaults/limits o error simulado.</returns>
        /// <remarks>GET /api/v1/config?demo=ok</remarks>
        [HttpGet("config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetConfig([FromQuery] string? demo = "ok")
            => Content(_svc.GetConfig(demo ?? "ok"), "application/json");

        /// <summary>
        /// Valida definiciones (linting) sin persistir ni ejecutar.
        /// </summary>
        /// <param name="request">Definición de servicio y checks (parcial).</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con valid=true/false y warnings/errors.</returns>
        /// <remarks>POST /api/v1/validate?demo=ok</remarks>
        [HttpPost("validate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Validate([FromBody] ValidateRequest request, [FromQuery] string? demo = "ok")
            => Content(_svc.Validate(request, demo ?? "ok"), "application/json");

        /// <summary>
        /// Descubre endpoints HTTP públicos/permitidos a partir de bases.
        /// </summary>
        /// <param name="request">Listado de URLs base para escanear con reglas de allow-list.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con proposals (no se persiste).</returns>
        /// <remarks>POST /api/v1/discovery/http?demo=ok</remarks>
        [HttpPost("discovery/http")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DiscoveryHttp([FromBody] DiscoveryHttpRequest request, [FromQuery] string? demo = "ok")
            => Content(_svc.DiscoveryHttp(request, demo ?? "ok"), "application/json");

        /// <summary>
        /// Descubre puertos/servicios TCP en hosts permitidos.
        /// </summary>
        /// <param name="request">Hosts y puertos a verificar (con allow/deny-lists).</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con open[].</returns>
        /// <remarks>POST /api/v1/discovery/tcp?demo=ok</remarks>
        [HttpPost("discovery/tcp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DiscoveryTcp([FromBody] DiscoveryTcpRequest request, [FromQuery] string? demo = "ok")
            => Content(_svc.DiscoveryTcp(request, demo ?? "ok"), "application/json");
    }
}



builder.Services.AddScoped<MonitoringApi.Services.IUtilitiesService, MonitoringApi.Services.UtilitiesService>();
builder.Services.AddScoped<MonitoringApi.Services.IHealthService, MonitoringApi.Services.HealthService>();



