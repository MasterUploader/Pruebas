Así estan las clases, basate en ellas:

using Microsoft.AspNetCore.Mvc;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Common;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;
using System.Net.Mime;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.Examples;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Controllers;

/// <summary>
/// Controlador de endpoints para la <b>gestión de transacciones POS</b>.
/// Estandariza validación, mapeo de <see cref="BizCodes"/> a HTTP con <see cref="BizHttpMapper"/> y manejo de errores.
/// </summary>
/// <param name="_transaccionesService">
/// Servicio de dominio que aplica reglas de negocio, idempotencia por <c>idTransaccionUnico</c> y persistencia.
/// </param>
/// <remarks>
/// <para><b>Características clave</b>:</para>
/// <list type="bullet">
///   <item><description>Produce <c>application/json</c> en todas las respuestas.</description></item>
///   <item><description>Valida <see cref="ControllerBase.ModelState"/> previo a la lógica de negocio.</description></item>
///   <item><description>Documentación enriquecida (XML docs + Examples) para Swagger/NSwag.</description></item>
/// </list>
/// <para><b>Observabilidad</b>:</para>
/// El middleware agrega/propaga <c>X-Correlation-Id</c>. Se recomienda registrar <i>TraceId</i>, <i>CorrelationId</i>, <c>codigoComercio</c>, <c>terminal</c> y <c>idTransaccionUnico</c>.
/// <para><b>Mapa referencial BizCodes → HTTP</b> (tu <see cref="BizHttpMapper"/> es la fuente de verdad):</para>
/// <code>
/// "000" (Éxito)                         → 200 OK
/// "400"/SolicitudInvalida               → 400 BadRequest
/// "409xx"/Duplicado/Conflicto           → 409 Conflict
/// "500"/ErrorInterno                    → 500 InternalServerError
/// otros códigos                         → según reglas (BizHttpMapper)
/// </code>
/// </remarks>
[Route("api/ProcesamientoTransaccionesPOS/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class TransaccionesController(ITransaccionesServices _transaccionesService) : ControllerBase
{
    /// <summary>
    /// Registra/Procesa transacciones POS en el backend.
    /// </summary>
    /// <param name="guardarTransaccionesDto">Ver <see cref="GuardarTransaccionesDto"/> para detalles, ejemplos y validaciones semánticas.</param>
    /// <returns>200 con <see cref="ApiResultDto"/>; 4xx/5xx con <see cref="RespuestaGuardarTransaccionesDto"/>.</returns>
    /// <remarks>
    /// <para><b>Ruta</b>:</para>
    /// <code>POST api/ProcesamientoTransaccionesPOS/Transacciones/GuardarTransacciones</code>
    /// </remarks>
    [HttpPost("GuardarTransacciones")]
    [Consumes(MediaTypeNames.Application.Json)]
    [SwaggerRequestExample(typeof(GuardarTransaccionesDto), typeof(Swagger.Examples.Transacciones.GuardarTransaccionesRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Swagger.Examples.Common.ApiResultDtoExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(Swagger.Examples.Common.RespuestaGuardarTransaccionesDtoExample))]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(Swagger.Examples.Common.RespuestaGuardarTransaccionesDtoExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(Swagger.Examples.Common.RespuestaGuardarTransaccionesDtoExample))]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GuardarTransacciones([FromBody] GuardarTransaccionesDto guardarTransaccionesDto)
    {
        // Validación estructural (DataAnnotations + IValidatableObject) antes de negocio
        if (!ModelState.IsValid)
        {
            // Mapeo explícito de "solicitud inválida" a HTTP mediante BizHttpMapper
            return StatusCode(BizHttpMapper.ToHttpStatusInt("400"), new
            {
                BizCodes.SolicitudInvalida,
                message = "Solicitud inválida, modelo DTO, invalido."
            });
        }

        try
        {
            // Servicio: reglas, idempotencia por idTransaccionUnico y persistencia
            var respuesta = await _transaccionesService.GuardarTransaccionesAsync(guardarTransaccionesDto);

            // Normalizar salida: code/message + HTTP vía BizHttpMapper (fuente de verdad)
            var code = respuesta.CodigoError ?? BizCodes.ErrorDesconocido;
            var http = BizHttpMapper.ToHttpStatusInt(code);

            RespuestaGuardarTransaccionesDto result = new()
            {
                CodigoError = code,
                DescripcionError = respuesta.DescripcionError
            };

            // Observabilidad: el middleware ya coloca X-Correlation-Id en la respuesta
            return StatusCode(http, result);
        }
        catch (Exception ex)
        {
            // Manejo controlado: no exponer detalles internos; log en middleware/servicio unificado
            RespuestaGuardarTransaccionesDto dto = new()
            {
                CodigoError = "400",
                DescripcionError = ex.Message
            };

            return BadRequest(dto);
        }
    }
}





using Logging.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;

/// <summary>
/// DTO para registrar/procesar una transacción POS.
/// Incluye campos de identificación, montos y estado para conciliación e idempotencia.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description>Montos como <c>string</c>; el backend normaliza coma/punto y valida formato.</description></item>
///   <item><description><c>idTransaccionUnico</c> debe mantenerse constante en reintentos (idempotencia).</description></item>
///   <item><description>El JSON <c>descripción</c> mapea a <c>Descripcion</c> en C#.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code language="json">
/// {
///   "numeroCuenta": "001234567890",
///   "montoDebitado": "125.75",
///   "montoAcreditado": "0.00",
///   "codigoComercio": "MC123",
///   "nombreComercio": "COMERCIO XYZ S.A.",
///   "terminal": "TERM-0001",
///   "descripción": "Pago POS ticket 98765",
///   "naturalezaContable": "DB",
///   "numeroDeCorte": "20251016-01",
///   "idTransaccionUnico": "6c1b1e00-6a66-4c0b-a4f7-1f77dfb9f9ef",
///   "estado": "APROBADA",
///   "descripcionEstado": "Operación aprobada por el emisor"
/// }
/// </code>
/// </example>
public sealed class GuardarTransaccionesDto
{
    /// <summary>
    /// Número de cuenta del comercio.
    /// </summary>
    [Required(ErrorMessage = "El número de cuenta es obligatorio.")]
    [JsonProperty("numeroCuenta")]
    [LogFileName("NumeroCuenta")]
    [StringLength(15)]
    public string NumeroCuenta { get; set; } = string.Empty;

    /// <summary>
    /// Importe debitado como <c>string</c>; se normaliza coma/punto.
    /// </summary>
    [Required(ErrorMessage = "El monto es obligatorio.")]
    [JsonProperty("montoDebitado")]
    [StringLength(12)]
    public string MontoDebitado { get; set; } = string.Empty;

    /// <summary>
    /// Importe acreditado como <c>string</c>; se normaliza coma/punto.
    /// </summary>
    [Required(ErrorMessage = "El monto es obligatorio.")]
    [JsonProperty("montoAcreditado")]
    [StringLength(12)]
    public string MontoAcreditado { get; set; } = string.Empty;

    /// <summary>
    /// Código único del comercio.
    /// </summary>
    [Required(ErrorMessage = "El código comercio es obligatorio.")]
    [JsonProperty("codigoComercio")]
    [LogFileName("CodigoComercio")]
    [StringLength(10)]
    public string CodigoComercio { get; set; } = string.Empty;

    /// <summary>
    /// Nombre legible del comercio.
    /// </summary>
    [Required(ErrorMessage = "El nombre del comercio es obligatorio.")]
    [JsonProperty("nombreComercio")]
    [StringLength(60)]
    public string NombreComercio { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de la terminal POS.
    /// </summary>
    [Required(ErrorMessage = "El número de terminal es obligatorio.")]
    [JsonProperty("terminal")]
    [LogFileName("Terminal")]
    [StringLength(15)]
    public string Terminal { get; set; } = string.Empty;

    /// <summary>
    ///  Descripción de la operación (JSON: <c>descripcion</c>).
    /// </summary>
    [Required(ErrorMessage = "La descripción es obligatorio.")]
    [JsonProperty("descripción")]
    [StringLength(200)]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Naturaleza contable de la transacción (p. ej. <c>D</c>/<c>C</c> u otro código interno que se pueda definir).
    /// </summary>
    [Required(ErrorMessage = "La Naturaleza contable es obligatoria.")]
    [JsonProperty("naturalezaContable")]
    [StringLength(2)]
    public string NaturalezaContable { get; set; } = string.Empty;

    /// <summary>
    /// Número de corte o batch (cierres/arqueos).
    /// </summary>
    [JsonProperty("numeroDeCorte")]
    [StringLength(2)]
    public string NumeroDeCorte { get; set; } = string.Empty;

    /// <summary>
    /// Identificador único de la transacción.
    /// </summary>
    [JsonProperty("idTransaccionUnico")]
    [StringLength(100)]
    public string IdTransaccionUnico { get; set; } = string.Empty;

    /// <summary>
    /// Estado del proceso de negocio (APROBADA, PENDIENTE, RECHAZADA, 0, 1, ...).
    /// </summary>
    [JsonProperty("estado")]
    [StringLength(100)]
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Descripción humana del estado (no sensible).
    /// </summary>
    [JsonProperty("descripcionEstado")]
    [StringLength(100)]
    public string DescripcionEstado { get; set; } = string.Empty;

    /// <summary>
    /// Validaciones semánticas transversales (complementa DataAnnotations).
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Regla opcional: evitar que ambos montos sean > 0 a la vez, y evitar ambos en cero.
        if (TryParseMoney(MontoDebitado, out var deb) && TryParseMoney(MontoAcreditado, out var cre))
        {
            if (deb > 0 && cre > 0)
                yield return new ValidationResult(
                    "montoDebitado y montoAcreditado no deben ser ambos mayores a cero.",
                    new[] { nameof(MontoDebitado), nameof(MontoAcreditado) });

            if (deb == 0 && cre == 0)
                yield return new ValidationResult(
                    "Se requiere un monto distinto de cero en montoDebitado o montoAcreditado.",
                    new[] { nameof(MontoDebitado), nameof(MontoAcreditado) });
        }

        if (!string.IsNullOrWhiteSpace(IdTransaccionUnico) && IdTransaccionUnico.Length < 8)
        {
            yield return new ValidationResult(
                "idTransaccionUnico debe tener al menos 8 caracteres.",
                new[] { nameof(IdTransaccionUnico) });
        }
    }

    /// <summary> Convierte cadena a decimal invariante aceptando ',' o '.'. </summary>
    private static bool TryParseMoney(string? value, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(value)) return true;
        var normalized = value.Replace(',', '.');
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out amount);
    }
}



using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;

/// <summary>
/// Resultado canónico de la operación para respuestas exitosas o de negocio.
/// Uniforma el <c>code</c> y el <c>message</c> para clientes móviles, pasarelas y terceros.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description><c>code</c> proviene de reglas de negocio (<c>BizCodes</c>), p. ej., "0000".</description></item>
///   <item><description><c>message</c> es legible; evitar detalles internos o PII.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code language="json">
/// { "code": "0000", "message": "Transacción registrada correctamente" }
/// </code>
/// </example>
public class RespuestaGuardarTransaccionesDto
{
    /// <summary> Código de negocio estandarizado (p. ej., "0000", "40001", etc.). </summary>
    [Required(AllowEmptyStrings = false)]
    [JsonProperty("codigoError")]
    public string? CodigoError { get; set; }

    /// <summary> Mensaje humano breve y claro. </summary>
    [Required(AllowEmptyStrings = false)]
    [JsonProperty("descripcionError")]
    public string? DescripcionError { get; set; }
}



namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models;

/// <summary>
/// Catálogo centralizado de códigos de resultado/negocio.
/// Todos los códigos son numéricos string de 5 dígitos.
/// Convención general (inspirada en semántica HTTP):
/// - 00000: OK
/// - 1xxxx: Informativos/No-errores (opcionales)
/// - 2xxxx: Idempotencia/Estado benigno (opcionales)
/// - 4xxxx: Errores de validación/negocio/datos (cliente)
/// - 5xxxx: Errores técnicos/sistema/integración (servidor)
/// </summary>
internal static class BizCodes
{
    // ==========================
    // ÉXITO / INFORMATIVO
    // ==========================

    /// <summary>Operación exitosa.</summary>
    public const string Ok = "00000";

    /// <summary>Operación ya aplicada previamente (idempotente).</summary>
    public const string YaProcesado = "20001";

    /// <summary>No hubo cambios que aplicar.</summary>
    public const string SinCambios = "20002";

    // ==========================
    // VALIDACIÓN (4xxxx)
    // ==========================

    // ---- Entradas requeridas / formato ----
    /// <summary>Solicitud inválida o mal formada (JSON, etc.).</summary>
    public const string SolicitudInvalida = "40000";
    /// <summary>No hay importes a postear (ambos montos = 0).</summary>
    public const string NoImportes = "40001";

    /// <summary>No se configuró perfil transerver.</summary>
    public const string PerfilFaltante = "40002";

    /// <summary>Naturaleza contable inválida (debe ser 'C' o 'D').</summary>
    public const string NaturalezaInvalida = "40003";

    /// <summary>Moneda inválida o no provista.</summary>
    public const string MonedaInvalida = "40004";

    /// <summary>Monto negativo o inválido.</summary>
    public const string MontoInvalido = "40005";

    /// <summary>Cuenta del cliente vacía o inválida.</summary>
    public const string CuentaClienteVacia = "40006";

    /// <summary>Código de comercio vacío o inválido.</summary>
    public const string CodigoComercioVacio = "40007";

    /// <summary>Terminal vacía o inválida.</summary>
    public const string TerminalVacia = "40008";

    /// <summary>Descripciones exceden longitud máxima permitida.</summary>
    public const string DescripcionesLargas = "40009";

    /// <summary>No se pudo obtener fecha del sistema.</summary>
    public const string FechaSistemaNoDisponible = "40010";

    /// <summary>No se recibió identificador único de transacción.</summary>
    public const string IdUnicoFaltante = "40011";

    // ---- Autenticación / Autorización ----
    /// <summary>Usuario no autenticado.</summary>
    public const string UsuarioNoAutenticado = "40101";

    /// <summary>Permisos insuficientes para la operación.</summary>
    public const string PermisosInsuficientes = "40301";

    /// <summary>Operación bloqueada por políticas de negocio.</summary>
    public const string PoliticasBloquean = "40302";

    // ---- No encontrados ----
    /// <summary>Comercio no existe.</summary>
    public const string ComercioNoExiste = "40401";

    /// <summary>Terminal no existe.</summary>
    public const string TerminalNoExiste = "40402";

    /// <summary>Perfil transerver no existe.</summary>
    public const string PerfilNoExiste = "40403";

    /// <summary>Cuenta (cliente) no existe.</summary>
    public const string CuentaNoExiste = "40404";

    /// <summary>Cuenta interna (GL) no existe.</summary>
    public const string CuentaInternaNoExiste = "40405";

    // ---- Conflictos / Estados ----
    /// <summary>Id de transacción duplicado.</summary>
    public const string DuplicadoIdUnico = "40901";

    /// <summary>No fue posible reservar número de lote (conflicto).</summary>
    public const string LoteNoDisponible = "40902";

    /// <summary>Secuencia de lote agotada o inconsistente.</summary>
    public const string SecuenciaAgotada = "40903";

    /// <summary>Cuenta en estado que impide operación (bloqueo, restricción, etc.).</summary>
    public const string EstadoCuentaInvalido = "40904";

    // ---- Semántica / Reglas de negocio (422xx) ----
    /// <summary>No se encontraron reglas/definiciones aplicables.</summary>
    public const string ReglasNoEncontradas = "42200";

    /// <summary>No se encontró contrapartida GL/CC válida para el asiento.</summary>
    public const string ResolverGLFalta = "42201";

    /// <summary>Tipo de cuenta no reconocido (se esperaba 1/6/40).</summary>
    public const string TipoCuentaNoReconocido = "42202";

    /// <summary>Descripciones obligatorias faltantes para la operación.</summary>
    public const string DescripcionesObligatoriasFaltantes = "42203";

    /// <summary>Auto-balance deshabilitado o inconsistente contra el perfil.</summary>
    public const string AutoBalanceDeshabilitado = "42204";

    /// <summary>Reglas de e-commerce inconsistentes o incompletas.</summary>
    public const string ReglasEcommerceInconsistentes = "42205";

    /// <summary>Tasa de cambio faltante para moneda recibida.</summary>
    public const string TasaCambioFaltante = "42206";

    /// <summary>Cuentas de control inconsistentes con el t-code.</summary>
    public const string CuentasControlInconsistentes = "42207";

    /// <summary>Centro de costo inválido o no permitido.</summary>
    public const string CentroCostoNoValido = "42208";

    /// <summary>Error de validación específico de la cuenta.</summary>
    public const string ErrorValidacionCuenta = "42210";

    // ---- Otros (opcionales) ----
    /// <summary>Saldo insuficiente (si aplica en otros flujos).</summary>
    public const string SaldoInsuficiente = "40201";

    // ==========================
    // SISTEMA / INTEGRACIÓN (5xxxx)
    // ==========================

    // ---- Infraestructura / DB ----
    /// <summary>Error al cargar/agregar librerías a la LIBL.</summary>
    public const string LibreriasFail = "50010";

    /// <summary>Fallo de conexión a base de datos.</summary>
    public const string ConexionDbFallida = "50020";

    /// <summary>Timeout ejecutando comando en base de datos.</summary>
    public const string TimeoutDb = "50021";

    /// <summary>Deadlock o contención en base de datos.</summary>
    public const string DeadlockDb = "50022";

    /// <summary>Error SQL genérico (sintaxis/constraint/etc.).</summary>
    public const string ErrorSql = "50023";

    /// <summary>Error desconocido no categorizado.</summary>
    public const string ErrorDesconocido = "50099";

    // ---- Programas RPG/ILE (INT_LOTES y similares) ----
    /// <summary>El programa INT_LOTES devolvió error (CODER != 0).</summary>
    public const string IntLotesFail = "51001";

    /// <summary>Timeout llamando al programa INT_LOTES.</summary>
    public const string IntLotesTimeout = "51002";

    /// <summary>Parámetros inválidos para INT_LOTES (longitud/escala).</summary>
    public const string IntLotesParametroInvalido = "51003";

    /// <summary>Programa RPG/ILE no disponible o no encontrado.</summary>
    public const string ProgramaNoDisponible = "51004";

    /// <summary>Error de conversión de tipos al invocar programa (packed/decimal).</summary>
    public const string ErrorConversionTipos = "51005";

    /// <summary>Tamaño de parámetro fuera de especificación del programa.</summary>
    public const string TamanioParamInvalido = "51006";

    // ---- Entorno IBM i (LIBL/paths) ----
    /// <summary>No se pudo establecer LIBL (CHGLIBL/Addlible falló).</summary>
    public const string CargaLiblFail = "52001";

    /// <summary>No se pudo establecer esquema/corriente de librerías.</summary>
    public const string SetPathLiblFail = "52002";

    // ---- Transaccionalidad ----
    /// <summary>Error al confirmar la transacción (COMMIT).</summary>
    public const string CommitFail = "53001";

    /// <summary>Error al revertir la transacción (ROLLBACK).</summary>
    public const string RollbackFail = "53002";

    /// <summary>Bloqueo de tabla/registro impide la operación.</summary>
    public const string LockTabla = "53003";

    // ---- IO / Sistemas externos ----
    /// <summary>Error de IO o de filesystem.</summary>
    public const string IOError = "54001";

    /// <summary>Servicio externo no disponible.</summary>
    public const string ServicioExternoNoDisponible = "55001";

    /// <summary>Timeout llamando servicio externo.</summary>
    public const string ServicioExternoTimeout = "55002";

    /// <summary>Error devuelto por servicio externo.</summary>
    public const string ServicioExternoError = "55003";
}



using Connections.Abstractions;
using Connections.Helpers;
using Connections.Providers.Database;
using Connections.Services;
using Logging.Abstractions;
using Logging.Filters;
using Logging.Handlers;
using Logging.Middleware;
using Logging.Services;
using Microsoft.OpenApi.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Services.ValidarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Utils;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ===============================  Conexiones  ===============================
// Publica la configuración de conexiones y deja disponible un helper global
// usado por componentes existentes (compatibilidad con código legacy).
ConnectionSettings connectionSettings = new(builder.Configuration);
ConnectionManagerHelper.ConnectionConfig = connectionSettings;

// Registrar ConnectionSettings para lectura dinámica en tiempo de ejecución
builder.Services.AddSingleton<ConnectionSettings>();

// ===============================  Infra Logging  ===============================
// Accessor del HttpContext para:
//  - Propagar correlación (ExecutionId, etc.)
//  - Permitir que SQL/HTTP se encolen en la **misma cola “timed”** y se ordenen por INICIO real.
builder.Services.AddHttpContextAccessor();

// Filtro que anota Controller/Action y otros metadatos MVC en los bloques fijos del log.
builder.Services.AddScoped<LoggingActionFilter>();

// Servicio central de logging (singleton: configuración y E/S de archivos compartidos).
builder.Services.AddSingleton<ILoggingService, LoggingService>();

// Opciones de logging (rutas, switches .txt/.csv) mapeadas desde appsettings.
builder.Services.Configure<Logging.Configuration.LoggingOptions>(
    builder.Configuration.GetSection("LoggingOptions"));

// ===============================  BD Provider  ===============================
// Proveedor AS400 con soporte de logging estructurado y **HttpContext**.
// Se pasa IHttpContextAccessor para que el wrapper SQL:
//  - Selle Items["__SqlStartedUtc"] (ancla de orden por INICIO real)
//  - Encole el bloque en HttpClientLogsTimed (mezcla/orden junto con HTTP).
builder.Services.AddScoped<IDatabaseConnection>(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    ConnectionSettings settings = new(config);
    string connStr = settings.GetAS400ConnectionString("AS400");

    return new AS400ConnectionProvider(
        connStr,
        sp.GetRequiredService<ILoggingService>(),
        sp.GetRequiredService<IHttpContextAccessor>()); // ? imprescindible para orden cronológico correcto
});

// ===============================  HTTP Saliente  ===============================
// Handler que intercepta TODAS las solicitudes/respuestas HTTP salientes para loguearlas
// en la cola “timed”. Los servicios deben usar IHttpClientFactory con el cliente **"Embosado"**.
builder.Services.AddTransient<HttpClientLoggingHandler>();

// Cliente HTTP **nombrado** con el handler de logging encadenado.
builder.Services.AddHttpClient("POS").AddHttpMessageHandler<HttpClientLoggingHandler>();

// ===============================  Config general / archivos  ===============================
// Carga de archivos de configuración de la API.
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ConnectionData.json", optional: false, reloadOnChange: true);

ConfigurationManager configuration = builder.Configuration;

// Resolver nodo de conexión por ambiente y publicarlo globalmente (compatibilidad).
string enviroment = configuration["ApiSettings:Enviroment"] ?? "DEV";
IConfigurationSection connectionSection = configuration.GetSection(enviroment);
ConnectionConfig? connectionConfig = connectionSection.Get<ConnectionConfig>();
GlobalConnection.Current = connectionConfig!; // intencional: debe existir configuración válida

// ===============================  JWT / Seguridad  ===============================
// Carga de clave secreta dinámica y configuración de autenticación JWT.


// ===============================  Servicios de dominio  ===============================
// Se registran como Scoped (no typed clients). Cada servicio debe obtener el HttpClient
// via IHttpClientFactory.CreateClient("Embosado") para que el handler capture los logs HTTP.
//builder.Services.AddHttpClient<ITransaccionesServices, TransaccionesServices>()
//builder.Services.AddHttpClient<IValidarTransaccionService, ValidarTransaccionService>()

builder.Services.AddScoped<ITransaccionesServices, TransaccionesServices>();
builder.Services.AddScoped<IValidarTransaccionService, ValidarTransaccionService>();
// Si en el futuro conviertes alguno a typed client, su clase debe exponer ctor(HttpClient, ...).

// ===============================  MVC / Swagger / CORS  ===============================
// Inserta el filtro de logging y expone documentación con comentarios XML.
builder.Services.AddControllers(options =>
{
    // Inserta filtro para capturar Controller/Action en los bloques fijos del log.
    options.Filters.Add<LoggingActionFilter>();
});

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Procesamiento Transacciones POS",
        Version = "v1",
        Description = "API para procesamiento de las transacciones de POS, POSRE.",
        Contact = new OpenApiContact
        {
            Name = "POSRE",
            Email = "soporte@api.com",
            Url = new Uri("https://api.com")
        },
        License = new OpenApiLicense
        {
            Name = "License",
            Url = new Uri("https://api.com")
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
});

// Política CORS (ej. front Angular); abre orígenes/métodos/headers.


builder.Services.AddSwaggerGenNewtonsoftSupport();


var app = builder.Build();

// ===============================  Pipeline HTTP  ===============================
// Activa CORS, seguridad, middleware de logging y Swagger en Dev/Prod.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Procesamiento Transacciones POS");
    });
}

app.UseHttpsRedirection();

//app.UseAuthentication() // valida JWT en endpoints protegidos
app.UseAuthorization();  // aplica políticas/atributos

// Middleware de logging central:
// - Escribe los 7 bloques fijos (Inicio/Env/Controlador/Request/Dinámicos/Errores/Fin).
// - Mezcla HTTP/SQL entre (4) Request y (5) Response, ordenados por INICIO real (TsUtc) y Seq.
app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();

await app.RunAsync();


