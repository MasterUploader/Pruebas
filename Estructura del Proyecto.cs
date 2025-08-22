#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// Respuestas JSON en duro para el segmento de Checks.
    /// </summary>
    public static class HardcodedChecksPayloads
    {
        // -------- GET /services/{serviceId}/checks
        public const string List_Ok = """
[
  {
    "checkId": "c1",
    "probeType": "GET",
    "operation": "/health",
    "assertions": { "expectStatus": 200 },
    "retries": 0,
    "warnLatencyMs": 300,
    "errorLatencyMs": 1000
  },
  {
    "checkId": "c42",
    "probeType": "SOAP_ACTION",
    "operation": "urn:Auth#Login",
    "assertions": { "xpath": "//ns:Status='OK'" },
    "retries": 1,
    "warnLatencyMs": 400,
    "errorLatencyMs": 1200
  }
]
""";

        public const string List_Fail = """
{
  "traceId": "f00dbabe111",
  "code": "forbidden",
  "title": "No autorizado",
  "detail": "No tienes permisos para ver los checks de este servicio."
}
""";

        // -------- GET /services/{serviceId}/checks/{checkId}
        public const string Get_Ok = """
{
  "checkId": "c42",
  "probeType": "SOAP_ACTION",
  "operation": "urn:Auth#Login",
  "headers": { "SOAPAction": "urn:Auth#Login" },
  "payload": "<soapenv:Envelope>...</soapenv:Envelope>",
  "assertions": { "xpath": "//ns:Status='OK'" },
  "retries": 1,
  "backoffMs": 200,
  "warnLatencyMs": 300,
  "errorLatencyMs": 1000
}
""";

        public const string Get_Fail = """
{
  "traceId": "a11deadbee",
  "code": "check_not_found",
  "title": "Check no encontrado",
  "detail": "No existe un check con el ID solicitado para este servicio."
}
""";

        // -------- POST /services/{serviceId}/checks
        public const string Create_Ok = """
{
  "checkId": "c99",
  "created": true
}
""";

        public const string Create_Fail = """
{
  "traceId": "baddad00",
  "code": "validation_error",
  "title": "Datos inválidos",
  "detail": "El campo 'probeType' es requerido."
}
""";

        // -------- PUT /services/{serviceId}/checks/{checkId}
        public const string Update_Ok = """
{
  "checkId": "c42",
  "updated": true
}
""";

        public const string Update_Fail = """
{
  "traceId": "c0deba5e",
  "code": "check_not_found",
  "title": "Check no encontrado",
  "detail": "No se puede actualizar; el check no existe."
}
""";

        // -------- DELETE /services/{serviceId}/checks/{checkId}
        public const string Delete_Ok = """
{
  "checkId": "c42",
  "deleted": true
}
""";

        public const string Delete_Fail = """
{
  "traceId": "de1e7e",
  "code": "in_use",
  "title": "No se puede eliminar",
  "detail": "El check se usa en una política activa."
}
""";

        // -------- POST /services/{serviceId}/checks/{checkId}/dry-run
        public const string DryRun_Ok = """
{
  "result": { "status": "Healthy", "latencyMs": 180, "statusCode": 200 },
  "logs": ["Resolved DNS", "Connected in 50ms", "HTTP 200 OK"]
}
""";

        public const string DryRun_Fail = """
{
  "traceId": "5ca1ab1e",
  "code": "connect_timeout",
  "title": "Tiempo de conexión agotado",
  "detail": "No fue posible conectar al endpoint remoto en 3 segundos."
}
""";
    }
}


CheckDtos
#nullable enable
using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.Models
{
    /// <summary>
    /// Modelo para crear un check (HTTP/SOAP/gRPC/MQ/TCP/etc.).
    /// Campos específicos pueden omitirse en el mock.
    /// </summary>
    public class CreateCheckRequest
    {
        /// <summary>Tipo de probe (GET, POST, SOAP_ACTION, GRPC_CALL, TCP_CONNECT, etc.).</summary>
        [Required]
        public string ProbeType { get; set; } = "GET";

        /// <summary>Operación lógica (endpoint/acción/rpc/cola/ruta).</summary>
        public string? Operation { get; set; }

        /// <summary>Cabeceras a utilizar (si aplica).</summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>Payload de la solicitud (si aplica).</summary>
        public string? Payload { get; set; }

        /// <summary>Reglas de validación/assertions (status esperado, xpath, jsonpath, etc.).</summary>
        public CheckAssertions? Assertions { get; set; }

        /// <summary>Reintentos ante fallo.</summary>
        public int Retries { get; set; } = 0;

        /// <summary>Backoff entre reintentos en milisegundos.</summary>
        public int? BackoffMs { get; set; }

        /// <summary>Umbral de alerta (warning) de latencia (ms).</summary>
        public int? WarnLatencyMs { get; set; }

        /// <summary>Umbral de error de latencia (ms).</summary>
        public int? ErrorLatencyMs { get; set; }
    }

    /// <summary>
    /// Modelo para actualizar un check (todos opcionales).
    /// </summary>
    public class UpdateCheckRequest
    {
        public string? ProbeType { get; set; }
        public string? Operation { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string? Payload { get; set; }
        public CheckAssertions? Assertions { get; set; }
        public int? Retries { get; set; }
        public int? BackoffMs { get; set; }
        public int? WarnLatencyMs { get; set; }
        public int? ErrorLatencyMs { get; set; }
    }

    /// <summary>
    /// Assertions/validaciones del check.
    /// </summary>
    public class CheckAssertions
    {
        /// <summary>Código HTTP esperado (si aplica).</summary>
        public int? ExpectStatus { get; set; }

        /// <summary>Expresión JSONPath esperada que debe evaluar a True o valor no nulo.</summary>
        public string? JsonPath { get; set; }

        /// <summary>Expresión XPath esperada (para XML/SOAP).</summary>
        public string? Xpath { get; set; }
    }
}

#nullable enable
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato de servicio para manejar definiciones de checks (mock).
    /// </summary>
    public interface IChecksService
    {
        /// <summary>JSON literal de GET /services/{serviceId}/checks.</summary>
        string GetList(string serviceId, string demo);

        /// <summary>JSON literal de GET /services/{serviceId}/checks/{checkId}.</summary>
        string GetOne(string serviceId, string checkId, string demo);

        /// <summary>JSON literal de POST /services/{serviceId}/checks.</summary>
        string Create(string serviceId, CreateCheckRequest request, string demo);

        /// <summary>JSON literal de PUT /services/{serviceId}/checks/{checkId}.</summary>
        string Update(string serviceId, string checkId, UpdateCheckRequest request, string demo);

        /// <summary>JSON literal de DELETE /services/{serviceId}/checks/{checkId}.</summary>
        string Delete(string serviceId, string checkId, string demo);

        /// <summary>JSON literal de POST /services/{serviceId}/checks/{checkId}/dry-run.</summary>
        string DryRun(string serviceId, string checkId, string demo);
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
    public class ChecksService : IChecksService
    {
        public string GetList(string serviceId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedChecksPayloads.List_Fail
                : HardcodedChecksPayloads.List_Ok;

        public string GetOne(string serviceId, string checkId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedChecksPayloads.Get_Fail
                : HardcodedChecksPayloads.Get_Ok;

        public string Create(string serviceId, CreateCheckRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedChecksPayloads.Create_Fail
                : HardcodedChecksPayloads.Create_Ok;

        public string Update(string serviceId, string checkId, UpdateCheckRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedChecksPayloads.Update_Fail
                : HardcodedChecksPayloads.Update_Ok;

        public string Delete(string serviceId, string checkId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedChecksPayloads.Delete_Fail
                : HardcodedChecksPayloads.Delete_Ok;

        public string DryRun(string serviceId, string checkId, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedChecksPayloads.DryRun_Fail
                : HardcodedChecksPayloads.DryRun_Ok;
    }
}




#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;
using MonitoringApi.Models;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Endpoints para administrar y probar definiciones de checks de un servicio.
    /// MOCK: Respuestas en duro usando el parámetro de query ?demo=ok|fail.
    /// </summary>
    [ApiController]
    [Route("api/v1/services/{serviceId}/checks")]
    [Produces("application/json")]
    public class ChecksController : ControllerBase
    {
        private readonly IChecksService _service;

        public ChecksController(IChecksService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista las definiciones de checks asociadas a un servicio.
        /// </summary>
        /// <param name="serviceId">ID lógico del servicio.</param>
        /// <param name="demo">"ok" (default) o "fail" para simular error.</param>
        /// <returns>JSON literal con el arreglo de checks o error simulado.</returns>
        /// <remarks>GET /api/v1/services/{serviceId}/checks?demo=ok</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetChecks([FromRoute] string serviceId, [FromQuery] string? demo = "ok")
            => Content(_service.GetList(serviceId, demo ?? "ok"), "application/json");

        /// <summary>
        /// Devuelve la definición de un check por su identificador.
        /// </summary>
        /// <param name="serviceId">ID del servicio.</param>
        /// <param name="checkId">ID del check.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con el check o error simulado.</returns>
        /// <remarks>GET /api/v1/services/{serviceId}/checks/{checkId}?demo=ok</remarks>
        [HttpGet("{checkId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetCheck([FromRoute] string serviceId, [FromRoute] string checkId, [FromQuery] string? demo = "ok")
            => Content(_service.GetOne(serviceId, checkId, demo ?? "ok"), "application/json");

        /// <summary>
        /// Crea una nueva definición de check para el servicio indicado.
        /// </summary>
        /// <param name="serviceId">ID del servicio.</param>
        /// <param name="request">Definición del check.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con el ID del check creado o error simulado.</returns>
        /// <remarks>POST /api/v1/services/{serviceId}/checks?demo=ok</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Create([FromRoute] string serviceId, [FromBody] CreateCheckRequest request, [FromQuery] string? demo = "ok")
            => Content(_service.Create(serviceId, request, demo ?? "ok"), "application/json");

        /// <summary>
        /// Actualiza una definición de check existente.
        /// </summary>
        /// <param name="serviceId">ID del servicio.</param>
        /// <param name="checkId">ID del check a actualizar.</param>
        /// <param name="request">Campos a modificar.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con updated=true o error simulado.</returns>
        /// <remarks>PUT /api/v1/services/{serviceId}/checks/{checkId}?demo=ok</remarks>
        [HttpPut("{checkId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Update([FromRoute] string serviceId, [FromRoute] string checkId, [FromBody] UpdateCheckRequest request, [FromQuery] string? demo = "ok")
            => Content(_service.Update(serviceId, checkId, request, demo ?? "ok"), "application/json");

        /// <summary>
        /// Elimina (o deshabilita) una definición de check.
        /// </summary>
        /// <param name="serviceId">ID del servicio.</param>
        /// <param name="checkId">ID del check.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con deleted=true o error simulado.</returns>
        /// <remarks>DELETE /api/v1/services/{serviceId}/checks/{checkId}?demo=ok</remarks>
        [HttpDelete("{checkId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Delete([FromRoute] string serviceId, [FromRoute] string checkId, [FromQuery] string? demo = "ok")
            => Content(_service.Delete(serviceId, checkId, demo ?? "ok"), "application/json");

        /// <summary>
        /// Ejecuta el check en caliente sin persistir ni cachear (dry-run).
        /// </summary>
        /// <param name="serviceId">ID del servicio.</param>
        /// <param name="checkId">ID del check a probar.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con resultado y logs o error simulado.</returns>
        /// <remarks>
        /// POST /api/v1/services/{serviceId}/checks/{checkId}/dry-run?demo=ok
        /// </remarks>
        [HttpPost("{checkId}/dry-run")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DryRun([FromRoute] string serviceId, [FromRoute] string checkId, [FromQuery] string? demo = "ok")
            => Content(_service.DryRun(serviceId, checkId, demo ?? "ok"), "application/json");
    }
}



// Agrega esta línea a tu Program.cs (junto con las de Status/Exec/Services)
builder.Services.AddScoped<MonitoringApi.Services.IChecksService, MonitoringApi.Services.ChecksService>();







