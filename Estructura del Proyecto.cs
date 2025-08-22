// DI de servicios
builder.Services.AddScoped<MonitoringApi.Services.IStatusService, MonitoringApi.Services.StatusService>();
builder.Services.AddScoped<MonitoringApi.Services.IExecService, MonitoringApi.Services.ExecService>();



#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;
using System.Text;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Endpoints de analítica de ejecuciones (APIs y microservicios).
    /// Todas las respuestas son simuladas con JSON literal (modo demo).
    /// </summary>
    [ApiController]
    [Route("api/v1/exec")]
    [Produces("application/json")]
    public class ExecController : ControllerBase
    {
        private readonly IExecService _service;

        public ExecController(IExecService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista ejecuciones individuales (crudo reciente) con paginación.
        /// </summary>
        /// <param name="demo">"ok" (default) para respuesta buena, "fail" para respuesta de error simulada.</param>
        /// <returns>JSON literal con items y meta de paginación.</returns>
        /// <remarks>GET /api/v1/exec/recent?demo=ok</remarks>
        [HttpGet("recent")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetRecent([FromQuery] string? demo = "ok")
            => Content(_service.GetRecent(demo ?? "ok"), "application/json");

        /// <summary>
        /// Serie temporal agregada por intervalos (buckets).
        /// </summary>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con buckets y meta (from/to/step).</returns>
        /// <remarks>GET /api/v1/exec/timeseries?demo=ok</remarks>
        [HttpGet("timeseries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetTimeseries([FromQuery] string? demo = "ok")
            => Content(_service.GetTimeseries(demo ?? "ok"), "application/json");

        /// <summary>
        /// KPI agregados por grupo (serviceId, interface, operation, verb, env).
        /// </summary>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con arreglo de grupos y sus métricas.</returns>
        /// <remarks>GET /api/v1/exec/summary?demo=ok</remarks>
        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetSummary([FromQuery] string? demo = "ok")
            => Content(_service.GetSummary(demo ?? "ok"), "application/json");

        /// <summary>
        /// Top N por métrica (latencia, errorRate, throughput).
        /// </summary>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con lista ordenada por la métrica solicitada.</returns>
        /// <remarks>GET /api/v1/exec/top?demo=ok</remarks>
        [HttpGet("top")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetTop([FromQuery] string? demo = "ok")
            => Content(_service.GetTop(demo ?? "ok"), "application/json");

        /// <summary>
        /// Distribución de latencias/duración (histograma).
        /// </summary>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con bins.</returns>
        /// <remarks>GET /api/v1/exec/distribution?demo=ok</remarks>
        [HttpGet("distribution")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetDistribution([FromQuery] string? demo = "ok")
            => Content(_service.GetDistribution(demo ?? "ok"), "application/json");

        /// <summary>
        /// Stream SSE (Server-Sent Events) de estadísticas en vivo.
        /// Produce 2 eventos de muestra según el modo (agg o raw).
        /// </summary>
        /// <param name="mode">"agg" (default) o "raw".</param>
        /// <param name="demo">"ok" o "fail". En fail emite un evento "error".</param>
        /// <returns>Stream SSE con 1 línea de retry y 2 eventos.</returns>
        /// <remarks>
        /// GET /api/v1/exec/live?mode=agg&amp;demo=ok  
        /// Header: Accept: text/event-stream
        /// </remarks>
        [HttpGet("live")]
        public async Task Live([FromQuery] string? mode = "agg", [FromQuery] string? demo = "ok")
        {
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            Response.ContentType = "text/event-stream; charset=utf-8";

            // retry para auto-reconexión del cliente
            await Response.WriteAsync("retry: 3000\n\n");

            if ((demo ?? "ok").ToLowerInvariant() == "fail")
            {
                await Response.WriteAsync(Data.HardcodedExecPayloads.SSE_Error);
                await Response.Body.FlushAsync();
                return;
            }

            if ((mode ?? "agg").Equals("raw", StringComparison.OrdinalIgnoreCase))
            {
                await Response.WriteAsync(Data.HardcodedExecPayloads.SSE_Raw_1);
                await Response.WriteAsync(Data.HardcodedExecPayloads.SSE_Raw_2);
            }
            else
            {
                await Response.WriteAsync(Data.HardcodedExecPayloads.SSE_Agg_1);
                await Response.WriteAsync(Data.HardcodedExecPayloads.SSE_Agg_2);
            }

            await Response.Body.FlushAsync();
        }
    }
}




#nullable enable
namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato para proveer respuestas simuladas de los endpoints /exec/*.
    /// </summary>
    public interface IExecService
    {
        /// <summary>JSON literal de /exec/recent.</summary>
        string GetRecent(string demo);

        /// <summary>JSON literal de /exec/timeseries.</summary>
        string GetTimeseries(string demo);

        /// <summary>JSON literal de /exec/summary.</summary>
        string GetSummary(string demo);

        /// <summary>JSON literal de /exec/top.</summary>
        string GetTop(string demo);

        /// <summary>JSON literal de /exec/distribution.</summary>
        string GetDistribution(string demo);
    }
}



#nullable enable
using MonitoringApi.Data;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Implementación mock de IExecService que retorna JSON literal desde constantes.
    /// </summary>
    public class ExecService : IExecService
    {
        public string GetRecent(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedExecPayloads.Recent_Fail
                : HardcodedExecPayloads.Recent_Ok;

        public string GetTimeseries(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedExecPayloads.Timeseries_Fail
                : HardcodedExecPayloads.Timeseries_Ok;

        public string GetSummary(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedExecPayloads.Summary_Fail
                : HardcodedExecPayloads.Summary_Ok;

        public string GetTop(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedExecPayloads.Top_Fail
                : HardcodedExecPayloads.Top_Ok;

        public string GetDistribution(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedExecPayloads.Distribution_Fail
                : HardcodedExecPayloads.Distribution_Ok;
    }
}



#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// JSON literal y eventos SSE simulados para los endpoints /exec/*.
    /// </summary>
    public static class HardcodedExecPayloads
    {
        // ----------------- /exec/recent -----------------
        public const string Recent_Ok = """
{
  "items": [
    {
      "timestampUtc": "2025-08-11T16:59:58Z",
      "serviceId": "invoice-worker",
      "env": "PROD",
      "interface": "MQ",
      "operation": "topic:payments",
      "verb": "CONSUME",
      "statusCode": 0,
      "success": true,
      "latencyMs": 45,
      "lag": 12,
      "traceId": "a13f0c29dd5c4b0e"
    },
    {
      "timestampUtc": "2025-08-11T16:59:32Z",
      "serviceId": "payments-api",
      "env": "PROD",
      "interface": "HTTP",
      "endpoint": "/payments/{id}",
      "method": "GET",
      "statusCode": 200,
      "success": true,
      "latencyMs": 142,
      "traceId": "7b2c7f2d9d2e4a1a"
    }
  ],
  "meta": { "page": 1, "size": 50, "total": 980 }
}
""";

        public const string Recent_Fail = """
{
  "traceId": "deadbeefcafef00d",
  "code": "invalid_request",
  "title": "Parámetro inválido",
  "detail": "El filtro 'from' debe ser anterior a 'to'."
}
""";

        // ----------------- /exec/timeseries -----------------
        public const string Timeseries_Ok = """
{
  "series": [
    {
      "bucketStartUtc": "2025-08-11T16:00:00Z",
      "bucketEndUtc": "2025-08-11T16:01:00Z",
      "count": 1820,
      "success": 1779,
      "errors": 41,
      "errorRate": 0.0225,
      "latency": { "avgMs": 210, "p50Ms": 120, "p95Ms": 480, "p99Ms": 920 },
      "throughputRps": 30.33
    }
  ],
  "meta": { "from": "2025-08-11T16:00:00Z", "to": "2025-08-11T17:00:00Z", "step": "1m" }
}
""";

        public const string Timeseries_Fail = """
{
  "traceId": "ff12aa90c0ffee77",
  "code": "range_too_wide",
  "title": "Ventana temporal muy grande",
  "detail": "La ventana máxima permitida es de 7 días."
}
""";

        // ----------------- /exec/summary -----------------
        public const string Summary_Ok = """
{
  "groups": [
    {
      "key": { "serviceId": "auth-soap", "interface": "SOAP", "operation": "urn:Auth#Login", "verb": "CALL" },
      "count": 3200,
      "success": 3187,
      "errors": 13,
      "errorRate": 0.0041,
      "latency": { "avgMs": 180, "p50Ms": 110, "p95Ms": 420, "p99Ms": 800 },
      "firstSeenUtc": "2025-08-11T15:00:12Z",
      "lastSeenUtc": "2025-08-11T16:59:58Z"
    }
  ],
  "meta": { "from": "2025-08-11T15:00:00Z", "to": "2025-08-11T17:00:00Z", "groupBy": ["serviceId","interface","operation","verb"] }
}
""";

        public const string Summary_Fail = """
{
  "traceId": "beadfeedabcd0001",
  "code": "too_many_groups",
  "title": "Exceso de cardinalidad",
  "detail": "Reduce groupBy o aplica más filtros."
}
""";

        // ----------------- /exec/top -----------------
        public const string Top_Ok = """
{
  "items": [
    {
      "rank": 1,
      "key": { "serviceId": "payments-api", "endpoint": "/payments/{id}" },
      "metric": { "name": "latency", "stat": "p95", "value": 720 },
      "supporting": { "count": 5320, "errorRate": 0.007, "throughputRps": 14.0 }
    },
    {
      "rank": 2,
      "key": { "serviceId": "auth-svc", "endpoint": "/token" },
      "metric": { "name": "latency", "stat": "p95", "value": 680 },
      "supporting": { "count": 8070, "errorRate": 0.003, "throughputRps": 22.4 }
    }
  ],
  "meta": { "from": "2025-08-11T16:00:00Z", "to": "2025-08-11T17:00:00Z", "by": "latency", "stat": "p95", "limit": 5 }
}
""";

        public const string Top_Fail = """
{
  "traceId": "c001d00d4444",
  "code": "invalid_scope",
  "title": "Scope no soportado",
  "detail": "Use 'service', 'endpoint', 'method', 'service-endpoint', 'operation' o 'verb'."
}
""";

        // ----------------- /exec/distribution -----------------
        public const string Distribution_Ok = """
{
  "histogram": [
    { "binStartMs": 0, "binEndMs": 50, "count": 820 },
    { "binStartMs": 50, "binEndMs": 100, "count": 1320 },
    { "binStartMs": 100, "binEndMs": 150, "count": 980 }
  ],
  "meta": { "from": "2025-08-11T16:00:00Z", "to": "2025-08-11T17:00:00Z", "bins": 20, "maxLatencyMs": 3000 }
}
""";

        public const string Distribution_Fail = """
{
  "traceId": "0badf00dfacefeed",
  "code": "missing_required",
  "title": "Parámetros requeridos ausentes",
  "detail": "Los parámetros 'from' y 'to' son obligatorios."
}
""";

        // ----------------- /exec/live (SSE) -----------------
        public const string SSE_Agg_1 = """
event: stats
data: {"windowStartUtc":"2025-08-11T16:05:00Z","windowEndUtc":"2025-08-11T16:05:05Z","count":210,"success":208,"errors":2,"errorRate":0.0095,"latency":{"avgMs":190,"p95Ms":410},"throughputRps":42.0}

""";

        public const string SSE_Agg_2 = """
event: stats
data: {"windowStartUtc":"2025-08-11T16:05:05Z","windowEndUtc":"2025-08-11T16:05:10Z","count":195,"success":193,"errors":2,"errorRate":0.0103,"latency":{"avgMs":200,"p95Ms":430},"throughputRps":39.0}

""";

        public const string SSE_Raw_1 = """
event: exec
data: {"timestampUtc":"2025-08-11T16:05:02Z","serviceId":"payments-api","endpoint":"/payments/{id}","method":"GET","statusCode":200,"latencyMs":142,"traceId":"7b2c7f2d9d2e4a1a"}

""";

        public const string SSE_Raw_2 = """
event: exec
data: {"timestampUtc":"2025-08-11T16:05:03Z","serviceId":"payments-api","endpoint":"/payments","method":"POST","statusCode":500,"latencyMs":910,"traceId":"a13f0c29dd5c4b0e"}

""";

        public const string SSE_Error = """
event: error
data: {"traceId":"feedbeef1234","code":"stream_unavailable","title":"Stream no disponible","detail":"Simulación de error en modo demo=fail."}

""";
    }
}


