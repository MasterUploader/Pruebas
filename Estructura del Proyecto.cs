#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// Respuestas JSON en duro para el segmento de Histórico.
    /// </summary>
    public static class HardcodedHistoryPayloads
    {
        // -------- GET /api/v1/history (vía query serviceId)
        public const string Query_Ok = """
{
  "points": [
    { "t": "2025-08-11T16:00:00Z", "status": "Healthy",  "latencyMs": 150 },
    { "t": "2025-08-11T16:05:00Z", "status": "Degraded", "latencyMs": 420 },
    { "t": "2025-08-11T16:10:00Z", "status": "Healthy",  "latencyMs": 130 }
  ],
  "meta": {
    "serviceId": "payments-api",
    "from": "2025-08-11T16:00:00Z",
    "to":   "2025-08-11T17:00:00Z",
    "downsample": "1m",
    "note": "El parámetro 'format' es ignorado en el mock (siempre JSON)."
  }
}
""";

        public const string Query_Fail = """
{
  "traceId": "histq-bad0001",
  "code": "invalid_range",
  "title": "Rango de tiempo no válido",
  "detail": "'from' debe ser anterior a 'to'."
}
""";

        // -------- GET /api/v1/history/{serviceId}
        public const string ById_Ok = """
{
  "points": [
    { "t": "2025-08-11T16:00:00Z", "status": "Healthy", "latencyMs": 150 },
    { "t": "2025-08-11T16:30:00Z", "status": "Healthy", "latencyMs": 170 },
    { "t": "2025-08-11T17:00:00Z", "status": "Healthy", "latencyMs": 160 }
  ],
  "meta": {
    "serviceId": "payments-api",
    "from": "2025-08-11T16:00:00Z",
    "to":   "2025-08-11T17:00:00Z",
    "downsample": "1m"
  }
}
""";

        public const string ById_Fail = """
{
  "traceId": "histid-deadbeef",
  "code": "service_not_found",
  "title": "Servicio no encontrado",
  "detail": "El serviceId solicitado no existe en el catálogo."
}
""";
    }
}




#nullable enable
namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato para obtener histórico (mock, JSON en duro).
    /// </summary>
    public interface IHistoryService
    {
        /// <summary>JSON literal de GET /api/v1/history (vía query serviceId).</summary>
        string GetHistory(string? serviceId, string demo);

        /// <summary>JSON literal de GET /api/v1/history/{serviceId}.</summary>
        string GetHistoryById(string serviceId, string demo);
    }
}




#nullable enable
using MonitoringApi.Data;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Implementación mock que devuelve JSON literal desde constantes.
    /// </summary>
    public class HistoryService : IHistoryService
    {
        public string GetHistory(string? serviceId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedHistoryPayloads.Query_Fail
                : HardcodedHistoryPayloads.Query_Ok;

        public string GetHistoryById(string serviceId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedHistoryPayloads.ById_Fail
                : HardcodedHistoryPayloads.ById_Ok;
    }
}




#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Endpoints para consultar histórico de resultados por servicio (series de puntos).
    /// MOCK: Respuestas en duro controladas por el query param ?demo=ok|fail.
    /// </summary>
    [ApiController]
    [Route("api/v1/history")]
    [Produces("application/json")]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryService _service;

        public HistoryController(IHistoryService service)
        {
            _service = service;
        }

        /// <summary>
        /// Histórico por serviceId vía querystring (atajo flexible).
        /// </summary>
        /// <param name="serviceId">Identificador lógico del servicio.</param>
        /// <param name="from">Inicio de ventana (ISO-8601 UTC). No se valida en el mock.</param>
        /// <param name="to">Fin de ventana (ISO-8601 UTC). No se valida en el mock.</param>
        /// <param name="downsample">1m, 5m, 1h (no aplicado en el mock).</param>
        /// <param name="format">ndjson|csv (ignorado en el mock; siempre JSON).</param>
        /// <param name="demo">"ok" (default) o "fail".</param>
        /// <returns>JSON literal con puntos y meta, o error simulado.</returns>
        /// <remarks>
        /// GET /api/v1/history?serviceId=payments-api&amp;from=2025-08-11T16:00:00Z&amp;to=2025-08-11T17:00:00Z&amp;downsample=1m&amp;demo=ok
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetViaQuery(
            [FromQuery] string? serviceId,
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] string? downsample,
            [FromQuery] string? format,
            [FromQuery] string? demo = "ok")
            => Content(_service.GetHistory(serviceId, demo ?? "ok"), "application/json");

        /// <summary>
        /// Histórico por serviceId (ruta dedicada).
        /// </summary>
        /// <param name="serviceId">Identificador lógico del servicio.</param>
        /// <param name="from">Inicio de ventana (ISO-8601 UTC). No se valida en el mock.</param>
        /// <param name="to">Fin de ventana (ISO-8601 UTC). No se valida en el mock.</param>
        /// <param name="downsample">1m, 5m, 1h (no aplicado en el mock).</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con puntos y meta, o error simulado.</returns>
        /// <remarks>
        /// GET /api/v1/history/payments-api?from=2025-08-11T16:00:00Z&amp;to=2025-08-11T17:00:00Z&amp;downsample=1m&amp;demo=ok
        /// </remarks>
        [HttpGet("{serviceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetByServiceId(
            [FromRoute] string serviceId,
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] string? downsample,
            [FromQuery] string? demo = "ok")
            => Content(_service.GetHistoryById(serviceId, demo ?? "ok"), "application/json");
    }
}



builder.Services.AddScoped<MonitoringApi.Services.IHistoryService, MonitoringApi.Services.HistoryService>();



