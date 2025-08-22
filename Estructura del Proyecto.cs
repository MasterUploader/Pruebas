#nullable enable
namespace MonitoringApi.Data
{
    /// <summary>
    /// Respuestas JSON en duro para el segmento de Seguridad / Referencias.
    /// </summary>
    public static class HardcodedAuthRefsPayloads
    {
        // -------- GET /api/v1/auth/refs
        public const string List_Ok = """
[
  {
    "authRef": "vault:kv/app/payments",
    "type": "ApiKey",
    "owner": "Payments",
    "meta": { "rotation": "30d", "note": "Clave de integraciones externas" }
  },
  {
    "authRef": "vault:kv/app/auth-svc",
    "type": "Bearer",
    "owner": "Security",
    "meta": { "rotation": "90d", "audience": "idp", "scopes": ["openid","profile"] }
  },
  {
    "authRef": "vault:pki/mtls/gateway",
    "type": "MTLS",
    "owner": "Core",
    "meta": { "rotation": "cert:365d", "cn": "gw.internal", "san": ["gw.internal"] }
  }
]
""";

        public const string List_Fail = """
{
  "traceId": "authrefs-list-err01",
  "code": "forbidden",
  "title": "No autorizado",
  "detail": "Tu credencial no puede listar referencias de autenticación."
}
""";

        // -------- POST /api/v1/auth/refs (upsert)
        public const string Upsert_Ok = """
{
  "authRef": "vault:kv/app/auth-svc",
  "saved": true,
  "updatedAtUtc": "2025-08-11T17:05:00Z"
}
""";

        public const string Upsert_Fail = """
{
  "traceId": "authrefs-upsert-err01",
  "code": "validation_error",
  "title": "Datos inválidos",
  "detail": "El campo 'authRef' es requerido y debe iniciar con 'vault:' o 'azurekeyvault:' o 'aws:'."
}
""";
    }
}




#nullable enable
using System.ComponentModel.DataAnnotations;

namespace MonitoringApi.Models
{
    /// <summary>
    /// Solicitud de upsert para una referencia a credencial (solo metadatos).
    /// </summary>
    public class UpsertAuthRefRequest
    {
        /// <summary>
        /// Ruta o identificador de la credencial en el gestor de secretos (p.ej. "vault:kv/app/payments").
        /// </summary>
        [Required]
        public string AuthRef { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de autenticación: ApiKey, Basic, Bearer, MTLS, OAuth2CC (client credentials) o Custom.
        /// </summary>
        [Required]
        public string Type { get; set; } = "ApiKey";

        /// <summary>
        /// Dueño/equipo responsable de la credencial (p.ej., "Security", "Payments").
        /// </summary>
        public string? Owner { get; set; }

        /// <summary>
        /// Metadatos no sensibles (rotación, audiencia, scopes, descripción, etc.).
        /// </summary>
        public Dictionary<string, object>? Meta { get; set; }
    }
}




#nullable enable
using MonitoringApi.Models;

namespace MonitoringApi.Services
{
    /// <summary>
    /// Contrato para gestionar referencias de autenticación (metadatos, mock).
    /// </summary>
    public interface IAuthRefsService
    {
        /// <summary>JSON literal para GET /api/v1/auth/refs.</summary>
        string GetList(string demo);

        /// <summary>JSON literal para POST /api/v1/auth/refs (upsert).</summary>
        string Upsert(UpsertAuthRefRequest request, string demo);
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
    public class AuthRefsService : IAuthRefsService
    {
        public string GetList(string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedAuthRefsPayloads.List_Fail
                : HardcodedAuthRefsPayloads.List_Ok;

        public string Upsert(UpsertAuthRefRequest request, string demo)
            => (demo?.ToLowerInvariant() == "fail")
                ? HardcodedAuthRefsPayloads.Upsert_Fail
                : HardcodedAuthRefsPayloads.Upsert_Ok;
    }
}




#nullable enable
using Microsoft.AspNetCore.Mvc;
using MonitoringApi.Services;
using MonitoringApi.Models;

namespace MonitoringApi.Controllers
{
    /// <summary>
    /// Administra referencias a credenciales/secretos (metadatos).
    /// MOCK: respuestas en duro con ?demo=ok|fail. No almacena secretos reales.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth/refs")]
    [Produces("application/json")]
    public class AuthRefsController : ControllerBase
    {
        private readonly IAuthRefsService _service;

        public AuthRefsController(IAuthRefsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista referencias a credenciales (solo metadatos, nunca el secreto).
        /// </summary>
        /// <param name="type">Filtro opcional por tipo (ApiKey, Basic, Bearer, MTLS, OAuth2CC, Custom). No aplica en el mock.</param>
        /// <param name="owner">Filtro opcional por dueño/equipo. No aplica en el mock.</param>
        /// <param name="demo">"ok" (default) o "fail" para forzar la respuesta simulada de error.</param>
        /// <returns>JSON literal con el inventario de referencias o error simulado.</returns>
        /// <remarks>GET /api/v1/auth/refs?demo=ok</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetMany([FromQuery] string? type, [FromQuery] string? owner, [FromQuery] string? demo = "ok")
            => Content(_service.GetList(demo ?? "ok"), "application/json");

        /// <summary>
        /// Crea o actualiza (upsert) una referencia (metadatos) a una credencial.
        /// </summary>
        /// <param name="request">Datos de la referencia. <b>No enviar secretos reales aquí</b>.</param>
        /// <param name="demo">"ok" o "fail".</param>
        /// <returns>JSON literal con saved=true y marca temporal o error simulado.</returns>
        /// <remarks>
        /// POST /api/v1/auth/refs?demo=ok
        /// Body mínimo:
        /// { "authRef": "vault:kv/app/auth-svc", "type": "Bearer", "owner": "Security" }
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Upsert([FromBody] UpsertAuthRefRequest request, [FromQuery] string? demo = "ok")
            => Content(_service.Upsert(request, demo ?? "ok"), "application/json");
    }
}





builder.Services.AddScoped<MonitoringApi.Services.IAuthRefsService, MonitoringApi.Services.AuthRefsService>();










