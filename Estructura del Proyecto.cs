Así tengo un código, si el flujo se completa registra el log sql, pero si entra al if (!inserted), no registra ese unico log

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
using Swashbuckle.AspNetCore.Filters;
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

builder.Services.AddSwaggerExamplesFromAssemblyOf<
    MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Transacciones.GuardarTransaccionesRequestMultipleExamples>();

builder.Services.AddSwaggerExamplesFromAssemblyOf<
    MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.ValidarTransacciones.ValidarTransaccionesRequestExample>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Procesamiento Transacciones POS",
        Version = "v1",
        Description = "API para procesamiento de las transacciones de POS, POSRE.",
        //Contact = new OpenApiContact
        //{
        //    Name = "POSRE",
        //    Email = "soporte@api.com",
        //    Url = new Uri("https://api.com")
        //},
        //License = new OpenApiLicense
        //{
        //    Name = "License",
        //    Url = new Uri("https://api.com")
        //}
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
    options.ExampleFilters(); // <- habilita los múltiples ejemplos en Swagger
});

// Política CORS (ej. front Angular); abre orígenes/métodos/headers. Ajustar según necesidades.
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

using Microsoft.AspNetCore.Mvc;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;
using Swashbuckle.AspNetCore.Filters;
using System.Net.Mime;

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
    /// <param name="ct">Token de Cancelación</param>
    /// <returns>200/4xx/5xx con <see cref="RespuestaGuardarTransaccionesDto"/>.</returns>
    /// <remarks>
    /// <para><b>Ruta</b>:</para>
    /// <code>POST api/ProcesamientoTransaccionesPOS/Transacciones/GuardarTransacciones</code>
    /// </remarks>
    [HttpPost("GuardarTransacciones")]
    [Consumes(MediaTypeNames.Application.Json)]
    [SwaggerRequestExample(
    typeof(GuardarTransaccionesDto),
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Transacciones.GuardarTransaccionesRequestMultipleExamples))]
    [SwaggerResponseExample(
    StatusCodes.Status200OK,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransacciones200Example))]
    [SwaggerResponseExample(
    StatusCodes.Status400BadRequest,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransacciones400Example))]
    [SwaggerResponseExample(
    StatusCodes.Status409Conflict,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransacciones409Example))]
    [SwaggerResponseExample(
    StatusCodes.Status500InternalServerError,
    typeof(MS_BAN_56_ProcesamientoTransaccionesPOS.Swagger.Examples.Common.RespuestaGuardarTransacciones500Example))]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GuardarTransacciones([FromBody] GuardarTransaccionesDto guardarTransaccionesDto, CancellationToken ct = default)
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
            var respuesta = await _transaccionesService.GuardarTransaccionesAsync(guardarTransaccionesDto, ct);

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


using Connections.Abstractions;
using Connections.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BCAH96DTA;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BNKPRD01;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Utils;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using System.Globalization;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;

/// <summary>
/// Clase de servicio para el procesamiento de transacciones POS.
/// </summary>
/// <param name="_connection">Inyección de clase IDatabaseConnection.</param>
/// <param name="_contextAccessor">Inyección de clase IHttpContextAccessor.</param>
public partial class TransaccionesServices(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : ITransaccionesServices
{
    /// <summary>
    /// Represents the response data for saving transactions.
    /// </summary>
    /// <remarks>This field is intended to store an instance of <see cref="RespuestaGuardarTransaccionesDto"/>
    /// that contains the result of a transaction-saving operation. It is protected and can be accessed  or modified by
    /// derived classes.</remarks>
    protected RespuestaGuardarTransaccionesDto _respuestaGuardarTransaccionesDto = new();

    /// <inheritdoc/>
    public async Task<RespuestaGuardarTransaccionesDto> GuardarTransaccionesAsync(GuardarTransaccionesDto guardarTransaccionesDto, CancellationToken ct = default)
    {
        //Procesos Previos

        _connection.Open(); //Abrimos la conexión a la base de datos

        // Validamos que la conexión esté activa, si no, retornamos error y no continuamos con el proceso.
        if (!_connection.IsConnected) return BuildError(BizCodes.ConexionDbFallida, "No fue posible establecer conexión con la base de datos.");

        // =================== Reserva inicial anti-duplicados ===================
        var (inserted, duplicate, insertErr) = await GuardarReservaPosre01gAsync(guardarTransaccionesDto, ct);
        if (!inserted)
        {
            _connection.Close(); //Cerramos la conexión a la base de datos
            if (duplicate)
                return BuildError(BizCodes.YaProcesado, $"Transaccion ya registrada (Numero Corte = {guardarTransaccionesDto.NumeroDeCorte}  y ID={guardarTransaccionesDto.IdTransaccionUnico})."); // 409 via BizHttpMapper
            return BuildError(BizCodes.ErrorSql, "No se pudo registrar reserva inicial: " + insertErr);
        }
        // ======================================================================

        //============================Validaciones Previas============================//

        // Normalización de importes: tolera "." o "," y espacios
        var deb = Utilities.ParseMonto(guardarTransaccionesDto.MontoDebitado);
        var cre = Utilities.ParseMonto(guardarTransaccionesDto.MontoAcreditado);

        //Validamos que al menos uno de los montos sea mayor a 0, no se puede postear ambos en 0.
        if (deb <= 0m && cre <= 0m) return BuildError(BizCodes.NoImportes, "No hay importes a postear (ambos montos son 0).");

        //Validamos que no se posteen ambos montos mayores a 0 a la vez.
        if (deb > 0m && cre > 0m) return BuildError(BizCodes.MontosIgualesMayores, "No se puede postear ambos montos (débito y crédito) mayores a cero a la vez.");

        //Obtenemos perfil transerver de la configuración global
        string perfilTranserver = GlobalConnection.Current.PerfilTranserver;

        //Validamos, si no hay perfil transerver, retornamos error porque el proceso no puede continuar.
        if (perfilTranserver.IsNullOrEmpty()) return BuildError(BizCodes.PerfilNoExiste, "No se ha configurado el perfil transerver a buscar en JSON.");

        //Validamos si existe el comercio en la tabla BCAH96DTA/IADQCOM
        var (existeComercio, codigoError, mensajeComercio) = BuscarComercio(guardarTransaccionesDto.NumeroCuenta, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeComercio) return BuildError(codigoError, mensajeComercio);

        //Validamos si existe la terminal en la tabla BCAH96DTA/ADQ03TER
        var (existeTerminal, esTerminalVirtual, codigoErrorTerminal, mensajeTerminal) = BuscarTerminal(guardarTransaccionesDto.Terminal, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeTerminal) return BuildError(codigoErrorTerminal, mensajeTerminal);

        //============================Fin Validaciones Previas============================//

        //============================Inicia Proceso Principal============================//

        // 1. Obtenemos si la terminal es virtual (e-commerce) o física.
        var respuestaPerfil = VerPerfil(perfilTranserver);

        // Si no existe el perfil, retornar error y no continuar con el proceso.
        if (!respuestaPerfil.existePerfil) return BuildError(respuestaPerfil.codigoError, respuestaPerfil.descripcionError);

        //2. Cargamos las librerías necesarias en la LIBL de la conexión actual.
        var (agregoLibrerias, codigoErrorLibrerias, descripcionErrorLibrerias) = CargaLibrerias(); // Asegura que las librerías necesarias estén en el entorno de ejecución
        //Validamos si se agregaron las librerías, si no, retornamos error y no continuamos con el proceso.
        if (!agregoLibrerias) return BuildError(codigoErrorLibrerias, descripcionErrorLibrerias);

        // 3. Resolvemos los parámetros necesarios para llamar a Int_lotes.
        var p = ResolverParametrosIntLotes(
            esEcommerce: esTerminalVirtual, // true si es e-commerce (terminal virtual)
            perfil: perfilTranserver, // Perfil transerver (CFTSKY)
            naturalezaCliente: guardarTransaccionesDto.NaturalezaContable,                 // 'C' o 'D'
            numeroCuenta: guardarTransaccionesDto.NumeroCuenta, //Del cliente/comercio
            codigoComercio: guardarTransaccionesDto.CodigoComercio, //Código del cliente/comercio
            monedaIsoNum: 0,                         // si tu RPG lo espera, envía el ISO num correspondiente
            monto: guardarTransaccionesDto.NaturalezaContable.Equals("D") ? deb : cre                         // monto a postear (mismo en ambos lados)
        );

        // 4. Ejecutamos el programa INT_LOTES con los parámetros necesarios.
        var (CodigoErrorPosteo, DescripcionErrorPosteo, NomArc) = await PosteoLoteAsync(p);

        // Actualizar el registro con el resultado del posteo
        await ActualizarResultadoPosre01gAsync(
            guardarTransaccionesDto.NumeroDeCorte,
            guardarTransaccionesDto.IdTransaccionUnico,
            CodigoErrorPosteo,
            DescripcionErrorPosteo,
            ct);

        _connection.Close(); //Cerramos la conexión a la base de datos
        //============================Fin Proceso Principal============================//
        return BuildError(CodigoErrorPosteo, DescripcionErrorPosteo);
    }

    // ============================ Utilidades ============================

    /// <summary>
    /// Método de validación de existencia de comercio en tabla BCAH96DTA/IADQCOM.
    /// </summary>
    /// <param name="cuentaRecibida">Número de cuenta recibido en la petición.</param>
    /// <param name="codigoComercioRecibido">Código de Comercio recibido en la petición.</param>
    /// <returns>Retorna un tupla
    /// /// (existeComercio: true/false, codigoError: "000001" , mensajeComercio: "Descripcioón del error")
    /// </returns>
    private (bool existeComercio, string codigoError, string mensajeComercio) BuscarComercio(string cuentaRecibida, int codigoComercioRecibido)
    {
        try
        {
            // Construimos consulta SQL con QueryBuilder para verificar existencia de perfil
            var buscarComercio = QueryBuilder.Core.QueryBuilder
                .From("IADQCOM", "BCAH96DTA")
                .Select("*")  // Solo necesitamos validar existencia
                .Where<AdqCom>(x => x.ADQCOME == codigoComercioRecibido)
                .Where<AdqCom>(x => x.ADQCTDE == cuentaRecibida) // Filtro dinámico por perfil
                .FetchNext(1)                // Solo necesitamos un registro
                .OrderBy("ADQCOME", SortDirection.Asc)
                .Build();

            using var command = _connection.GetDbCommand(buscarComercio, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (true, BizCodes.Ok, "Existe Comercio."); // Coemrcio existe
            }
            return (false, BizCodes.ComercioNoExiste, "No existe Comercio."); // Comercio no existe
        }
        catch (SqlException ex)
        {
            return (false, BizCodes.ErrorSql, ex.Message); // Indica error al consultar comercio
        }
        catch (Exception ex)
        {
            return (false, BizCodes.ErrorDesconocido, ex.Message); // Indica error desconocido al consultar comercio
        }
    }

    /// <summary>
    /// Método de validación de existencia de terminal en tabla BCAH96DTA/ADQ03TER.
    /// </summary>
    /// <param name="terminalRecibida">Número de terminal Recibida</param>
    /// <param name="codigoComercioRecibido">Código Comercio Recibido.</param>
    /// <returns></returns>
    private (bool existeTerminal, bool esTerminalvirtual, string codigoError, string mensajeTerminal) BuscarTerminal(string terminalRecibida, int codigoComercioRecibido)
    {
        try
        {
            var terminal = string.Empty;
            // Construimos consulta SQL con QueryBuilder para verificar existencia de perfil
            var buscarComercio = QueryBuilder.Core.QueryBuilder
                .From("ADQ03TER", "BCAH96DTA")
                .Select("*")  // Solo necesitamos validar existencia
                .Where<Adq03Ter>(x => x.A03COME == codigoComercioRecibido)
                .Where<Adq03Ter>(x => x.A03TERM == terminalRecibida)
                .OrderBy(("A03TERM", SortDirection.Asc), ("A03TERM", SortDirection.Asc))
                .Build();

            using var command = _connection.GetDbCommand(buscarComercio, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                if (reader.Read())
                {
                    // -- Mapeo directo desde columnas proyectadas.
                    terminal = reader.GetString(reader.GetOrdinal("A03TERM"));
                }
                return (true, EsTerminalVirtual(terminal), BizCodes.Ok, "Existe terminal."); // Terminal existe
            }
            return (false, false, BizCodes.TerminalNoExiste, "No existe terminal."); // Terminal no existe
        }
        catch (SqlException ex)
        {
            return (false, false, BizCodes.ErrorSql, ex.Message); //Indica error en SQL al Consultar Terminal
        }
        catch (Exception ex)
        {
            return (false, false, BizCodes.ErrorDesconocido, ex.Message); // Indica error desconocido al consultar terminal
        }
    }

    /// <summary>
    /// Valida si la terminal corresponde a un e-commerce (virtual).
    /// Regla: la terminal se considera virtual si el primer carácter es 'E'.
    /// </summary>
    /// <param name="terminal">Número o código de terminal recibido.</param>
    /// <returns>True si es virtual (e-commerce), False en caso contrario.</returns>
    private static bool EsTerminalVirtual(string? terminal)
    {
        if (string.IsNullOrWhiteSpace(terminal))
            return false;

        // Evaluamos únicamente el primer carácter, sin importar minúscula/mayúscula
        return terminal.Trim().StartsWith("E", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica si existe un perfil en la tabla CFP801 y ejecuta la lógica correspondiente.
    /// </summary>
    /// <param name="perfil">Clave de perfil (CFTSKY en RPGLE).</param>
    /// <returns>Tupla (bool, string,  string), true o false y descripción si existe o no el perfil</returns>
    private (bool existePerfil, string codigoError, string descripcionError) VerPerfil(string perfil)
    {
        try
        {
            // Construimos consulta SQL con QueryBuilder para verificar existencia de perfil
            var verPerfilSql = QueryBuilder.Core.QueryBuilder
                .From("CFP801", "BNKPRD01")
                .Select("CFTSBK", "CFTSKY")  // Solo necesitamos validar existencia
                .Where<Cfp801>(x => x.CFTSBK == 001)       // Condición fija
                .Where<Cfp801>(x => x.CFTSKY == perfil) // Filtro dinámico por perfil
                .FetchNext(1)                // Solo necesitamos un registro
                .Build();

            using var command = _connection.GetDbCommand(verPerfilSql, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (true, BizCodes.Ok, "Existe Perfil Transerver."); // Perfil existe
            }
            return (false, BizCodes.PerfilNoExiste, "No existe Perfil Transerver."); // Perfil no existe
        }
        catch (SqlException ex)
        {
            // Manejo de errores SQL
            return (false, BizCodes.ErrorSql, "Error SQL: " + ex.Message); //Indica error sql al consultar perfil
        }
        catch (Exception ex)
        {
            // Manejo de errores en la consulta
            return (false, BizCodes.ErrorDesconocido, "Error general: " + ex.Message); // Indica error desconocido al consultar perfil
        }
    }

    /// <summary>
    /// Resuelve todos los parámetros necesarios para INT_LOTES:
    /// - Calcula t-codes, obtiene ADQNUM y, si aplica, GL/CC en UNA sola consulta (ADQECTL o ADQCTL).
    /// - Aplica auto-balance (CFP801) si está activo para el perfil.
    /// - Arma las descripciones EXACTAS como en el RPG (40 chars).
    /// - Construye ambos movimientos: Mov1 = Débito, Mov2 = Crédito, según la naturaleza del cliente.
    ///<param name="esEcommerce">Indica si la transacción es de e-commerce (terminal virtual).</param>
    ///<param name="perfil">Perfil transerver (CFTSKY).</param>"
    ///<param name="naturalezaCliente">Naturaleza Contable a aplicar.</param>
    ///<param name="numeroCuenta">Número de cuenta del cliente.</param>
    ///<param name="codigoComercio">Código del comercio.</param>
    ///<param name="monedaIsoNum">Moneda de la cuenta.</param>
    ///<param name="monto">Valor monto acreditar o debitar.</param>
    /// </summary>
    private IntLotesParamsDto ResolverParametrosIntLotes(
        bool esEcommerce,
        string perfil,
        string naturalezaCliente, // 'C' (acreditamos cliente) o 'D' (debitamos cliente)
        string numeroCuenta,      // cuenta del cliente
        string codigoComercio,
        int monedaIsoNum,                     // 0 si no aplica ISO; de lo contrario ej. 340/840
        decimal monto                         // monto a postear (mismo en ambos lados)
    )
    {
        var dto = new IntLotesParamsDto
        {
            Perfil = perfil,
            Moneda = monedaIsoNum,
            TasaTm = 0m,
            ErrorMetodo = 0
        };

        // ---------------------------------------------------------
        // 1) Determinar t-codes (cliente vs GL) según naturaleza
        // ---------------------------------------------------------
        string tcodeCliente = naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) ? "0783" : "0784";
        string tcodeGL = naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) ? "0784" : "0783";

        // ---------------------------------------------------------
        // 2) Obtener ADQNUM + (GL/CC) en UNA consulta (control EC/GL)
        // ---------------------------------------------------------
        var (adqNumCtl, glCtl, ccCtl) = ObtenerAdqNumYGL(esEcommerce, tcodeGL);

        // ---------------------------------------------------------
        // 3) Auto-balance (CFP801) o control (ADQECTL/ADQCTL)
        // ---------------------------------------------------------
        var (enabled, glDeb, ccDeb, glCre, ccCre) = TryGetAutoBalance(perfil);
        dto.EsAutoBalance = enabled;

        string? glCuenta;
        int glCC;
        if (enabled)
        {
            // Cliente 'C' → interno debe ir a Débito; Cliente 'D' → interno a Crédito
            if (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                // Débito interno
                glCuenta = glDeb; glCC = ccDeb;
            }
            else
            {
                // Crédito interno
                glCuenta = glCre; glCC = ccCre;
            }
            dto.FuenteGL = "CFP801";
        }
        else
        {
            // Usar control EC/GL
            glCuenta = glCtl; glCC = ccCtl; dto.FuenteGL = esEcommerce ? "ADQECTL" : "ADQCTL";
        }

        // ---------------------------------------------------------
        // 4) Tipo de cuenta del cliente (Ahorros/Cheques) vía Ver_cta
        // ---------------------------------------------------------
        var infoCta = VerCta(numeroCuenta);

        decimal tipoClienteDec;

        // Validar que la cuenta exista
        if (infoCta.EsAhorro)//Es cuenta de ahorros
        {
            tipoClienteDec = 1; //Cuentas de Ahorros son tipo 1
        }
        else if (infoCta.EsCheques) //Es cuenta de cheques
        {
            tipoClienteDec = 6; // Cuentas de Cheques son tipo 6
        }
        else // Otros tipos (p. ej., contable)
        {
            tipoClienteDec = 40; // Contable (otros casos)
        }

        // ---------------------------------------------------------
        // 5) Tasa (si tu RPG la usa). De lo contrario, queda 0.
        // ---------------------------------------------------------
        try { dto.TasaTm = ObtenerTasaCompraUsd(); } catch { dto.TasaTm = 0m; }

        // ---------------------------------------------------------
        // 6) Descripciones EXACTAS del RPG (40 chars; usa ADQNUM)
        // ---------------------------------------------------------
        string concepto = "VTA";
        string fechaFormateada = DateTime.Now.ToString("yyyyMMdd");
        string ochoEspacios = "        ";
        string adqNumPadded = (adqNumCtl ?? "0").PadRight(10); // anchura usada por el RPG

        string desDb1 = Trunc("Total Neto Db liquidacion come", 40);
        string desCr1 = Trunc("Total Neto Cr liquidacion come", 40);
        string descDb2 = Trunc($"{codigoComercio}{ochoEspacios}-{fechaFormateada}-{tipoClienteDec}-{numeroCuenta}", 40);
        string descCr2 = Trunc($"{codigoComercio}{ochoEspacios}-{fechaFormateada}", 40);
        string descDb3 = Trunc($"&{concepto}&{adqNumPadded}Db Net.Liq1  ||", 40);
        string descCr3 = Trunc($"&{concepto}&{adqNumPadded}Cr Net.Liq2  ||", 40);

        // ---------------------------------------------------------
        // 7) Construir ambos movimientos (Mov1 = Débito, Mov2 = Crédito)
        //    Reglas:
        //    - Si naturalezaCliente = 'C' (acreditamos cliente):
        //        Mov1: Débito interno (GL)
        //        Mov2: Crédito cliente
        //    - Si naturalezaCliente = 'D' (debitamos cliente):
        //        Mov1: Débito cliente
        //        Mov2: Crédito interno (GL)
        // ---------------------------------------------------------
        static decimal ToDecOrZero(string? s)
            => decimal.TryParse((s ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

        dto.Perfil = perfil;
        dto.Moneda = monedaIsoNum;
        dto.TasaTm = 0m;

        if (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            // Mov1 = DB interno (GL)
            dto.TipoMov1 = 40;
            dto.CuentaMov1 = ToDecOrZero(glCuenta);
            dto.DeCr1 = "D";
            dto.CentroCosto1 = glCC;
            dto.MontoMov1 = monto;
            dto.MonedaMov1 = monedaIsoNum;

            // Mov2 = CR cliente
            dto.TipoMov2 = tipoClienteDec;
            dto.CuentaMov2 = ToDecOrZero(numeroCuenta);
            dto.DeCr2 = "C";
            dto.CentroCosto2 = 0m;
            dto.MontoMov2 = monto;
            dto.MonedaMov2 = monedaIsoNum;

            // Descripciones: DESDB* (lado debitado = interno), DESCR* (lado acreditado = cliente)
            dto.DesDB1 = desDb1; dto.DesDB2 = descDb2; dto.DesDB3 = descDb3;
            dto.DesCR1 = desCr1; dto.DesCR2 = descCr2; dto.DesCR3 = descCr3;

            // Trazabilidad
            dto.NaturalezaCliente = naturalezaCliente;
            dto.NaturalezaGL = naturalezaCliente == "C" ? 'D' : 'C';
            dto.TcodeCliente = tcodeCliente;
            dto.TcodeGL = tcodeGL;
            dto.CuentaClienteOriginal = numeroCuenta;
            dto.CuentaGLResuelta = glCuenta ?? string.Empty;
        }
        else
        {
            // Mov1 = DB cliente
            dto.TipoMov1 = tipoClienteDec;
            dto.CuentaMov1 = ToDecOrZero(numeroCuenta);
            dto.DeCr1 = "D";
            dto.CentroCosto1 = 0;
            dto.MontoMov1 = monto;
            dto.MonedaMov1 = monedaIsoNum;

            // Mov2 = CR interno (GL)
            dto.TipoMov2 = 40;
            dto.CuentaMov2 = ToDecOrZero(glCuenta);
            dto.DeCr2 = "C";
            dto.CentroCosto2 = glCC;
            dto.MontoMov2 = monto;
            dto.MonedaMov2 = monedaIsoNum;

            // Descripciones: DESDB* (lado debitado = cliente), DESCR* (lado acreditado = interno)
            dto.DesDB1 = desDb1; dto.DesDB2 = descDb2; dto.DesDB3 = descDb3;
            dto.DesCR1 = desCr1; dto.DesCR2 = descCr2; dto.DesCR3 = descCr3;
            // Trazabilidad
            dto.NaturalezaCliente = naturalezaCliente;
            dto.NaturalezaGL = naturalezaCliente == "C" ? 'D' : 'C';
            dto.TcodeCliente = tcodeCliente;
            dto.TcodeGL = tcodeGL;
            dto.CuentaClienteOriginal = numeroCuenta;
            dto.CuentaGLResuelta = glCuenta ?? string.Empty;
        }

        // ---------------------------------------------------------
        // 8) Validación mínima: GL obligatoria para el lado interno
        // ---------------------------------------------------------
        bool glFaltante =
            (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) && dto.CuentaMov1 == 0m)
         || (naturalezaCliente.Equals("D", StringComparison.OrdinalIgnoreCase) && dto.CuentaMov2 == 0m);

        if (glFaltante)
        {
            dto.ErrorMetodo = 1;
            string errorbase = $"No se encontró cuenta interna (GL) para tcode {tcodeGL} en ";

            if (dto.FuenteGL == "CFP801")
            {
                dto.DescripcionError = errorbase + "CFP801.";
                if (esEcommerce)
                {
                    dto.DescripcionError = errorbase + "ADQECTL";
                }
                else
                {
                    dto.DescripcionError = errorbase + "ADQCTL";
                }
            }
        }
        return dto;
    }

    /// <summary>
    /// Obtiene en UNA SOLA CONSULTA el ADQNUM/ADQENUM y, si corresponde,
    /// la cuenta GL (CNTk) y el centro de costo (CCOk) cuyo T-CODE (CTRk) coincide con <paramref name="tcodeGL"/>.
    /// - Si <paramref name="esEcommerce"/> = true, consulta ADQECTL (CONTROL='EC').
    /// - Si es false, consulta ADQCTL (CONTROL='GL').
    /// Devuelve: (AdqNum, GlCuenta, GlCC).
    /// <param name="esEcommerce">Indica si es Comercio virtual o tradicional.</param>
    /// <param name="tcodeGL">T-code a buscar (ej. '0784' o '0783').</param>"
    /// </summary>
    /// <returns>Tupla (AdqNum, GlCuenta, GlCC)</returns>
    private (string AdqNum, string? GlCuenta, int GlCC) ObtenerAdqNumYGL(bool esEcommerce, string tcodeGL)
    {
        // --- 1) Armar SELECT único con alias estándar (CTRn, CNTn, CCOn, ADQNUM) ---
        var q = esEcommerce
            ? QueryBuilder.Core.QueryBuilder
                .From("ADQECTL", "BCAH96DTA")
                .Select(
                    "ADQENUM AS ADQNUM",
                    "ADQECTR1 AS CTR1", "ADQECNT1 AS CNT1", "ADQECCO1 AS CCO1",
                    "ADQECTR2 AS CTR2", "ADQECNT2 AS CNT2", "ADQECCO2 AS CCO2",
                    "ADQECTR3 AS CTR3", "ADQECNT3 AS CNT3", "ADQECCO3 AS CCO3",
                    "ADQECTR4 AS CTR4", "ADQECNT4 AS CNT4", "ADQECCO4 AS CCO4",
                    "ADQECTR5 AS CTR5", "ADQECNT5 AS CNT5", "ADQECCO5 AS CCO5",
                    "ADQECTR6 AS CTR6", "ADQECNT6 AS CNT6", "ADQECCO6 AS CCO6",
                    "ADQECTR7 AS CTR7", "ADQECNT7 AS CNT7", "ADQECCO7 AS CCO7",
                    "ADQECTR8 AS CTR8", "ADQECNT8 AS CNT8", "ADQECCO8 AS CCO8",
                    "ADQECTR9 AS CTR9", "ADQECNT9 AS CNT9", "ADQECCO9 AS CCO9",
                    "ADQECTR10 AS CTR10", "ADQECNT10 AS CNT10", "ADQECC10 AS CCO10",
                    "ADQECTR11 AS CTR11", "ADQECNT11 AS CNT11", "ADQECC11 AS CCO11",
                    "ADQECTR12 AS CTR12", "ADQECNT12 AS CNT12", "ADQECC12 AS CCO12",
                    "ADQECTR13 AS CTR13", "ADQECNT13 AS CNT13", "ADQECC13 AS CCO13",
                    "ADQECTR14 AS CTR14", "ADQECNT14 AS CNT14", "ADQECC14 AS CCO14",
                    "ADQECTR15 AS CTR15", "ADQECNT15 AS CNT15", "ADQECC15 AS CCO15"
                )
                .WhereRaw(
                    "ADQECONT = 'EC' AND (" +
                    "ADQECTR1 = @T OR ADQECTR2 = @T OR ADQECTR3 = @T OR ADQECTR4 = @T OR ADQECTR5 = @T OR " +
                    "ADQECTR6 = @T OR ADQECTR7 = @T OR ADQECTR8 = @T OR ADQECTR9 = @T OR ADQECTR10 = @T OR " +
                    "ADQECTR11 = @T OR ADQECTR12 = @T OR ADQECTR13 = @T OR ADQECTR14 = @T OR ADQECTR15 = @T)"
                )
                .FetchNext(1)
                .Build()
            : QueryBuilder.Core.QueryBuilder
                .From("ADQCTL", "BCAH96DTA")
                .Select(
                    "ADQNUM AS ADQNUM",
                    "ADQCTR1 AS CTR1", "ADQCNT1 AS CNT1", "ADQCCO1 AS CCO1",
                    "ADQCTR2 AS CTR2", "ADQCNT2 AS CNT2", "ADQCCO2 AS CCO2",
                    "ADQCTR3 AS CTR3", "ADQCNT3 AS CNT3", "ADQCCO3 AS CCO3",
                    "ADQCTR4 AS CTR4", "ADQCNT4 AS CNT4", "ADQCCO4 AS CCO4",
                    "ADQCTR5 AS CTR5", "ADQCNT5 AS CNT5", "ADQCCO5 AS CCO5",
                    "ADQCTR6 AS CTR6", "ADQCNT6 AS CNT6", "ADQCCO6 AS CCO6",
                    "ADQCTR7 AS CTR7", "ADQCNT7 AS CNT7", "ADQCCO7 AS CCO7",
                    "ADQCTR8 AS CTR8", "ADQCNT8 AS CNT8", "ADQCCO8 AS CCO8",
                    "ADQCTR9 AS CTR9", "ADQCNT9 AS CNT9", "ADQCCO9 AS CCO9",
                    "ADQCTR10 AS CTR10", "ADQCNT10 AS CNT10", "ADQCC10 AS CCO10",
                    "ADQCTR11 AS CTR11", "ADQCNT11 AS CNT11", "ADQCC11 AS CCO11",
                    "ADQCTR12 AS CTR12", "ADQCNT12 AS CNT12", "ADQCC12 AS CCO12",
                    "ADQCTR13 AS CTR13", "ADQCNT13 AS CNT13", "ADQCC13 AS CCO13",
                    "ADQCTR14 AS CTR14", "ADQCNT14 AS CNT14", "ADQCC14 AS CCO14",
                    "ADQCTR15 AS CTR15", "ADQCNT15 AS CNT15", "ADQCC15 AS CCO15"
                )
                .WhereRaw(
                    " (" +
                    "ADQCTR1 = @T OR ADQCTR2 = @T OR ADQCTR3 = @T OR ADQCTR4 = @T OR ADQCTR5 = @T OR " +
                    "ADQCTR6 = @T OR ADQCTR7 = @T OR ADQCTR8 = @T OR ADQCTR9 = @T OR ADQCTR10 = @T OR " +
                    "ADQCTR11 = @T OR ADQCTR12 = @T OR ADQCTR13 = @T OR ADQCTR14 = @T OR ADQCTR15 = @T)"
                )
                .FetchNext(1)
                .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        // Si tu QueryBuilder no soporta parámetros, inyecta el valor literal una sola vez:
        cmd.CommandText = cmd.CommandText.Replace("@T", $"'{tcodeGL}'");

        using var rd = cmd.ExecuteReader();
        if (!rd.Read())
            return ("0", null, 0);

        // --- 2) ADQNUM común ---
        var adqNum = rd.IsDBNull(rd.GetOrdinal("ADQNUM"))
            ? "0"
            : Convert.ToDecimal(rd.GetValue(rd.GetOrdinal("ADQNUM"))).ToString("0");

        // --- 3) Localizar el índice k (1..15) cuyo CTRk == tcodeGL y devolver CNTk/CCOk ---
        string? gl = null; int cc = 0;
        for (int k = 1; k <= 15; k++)
        {
            var ctr = rd.GetString(rd.GetOrdinal($"CTR{k}")).Trim();
            if (string.Equals(ctr, tcodeGL, StringComparison.OrdinalIgnoreCase))
            {
                // CNTk puede ser DECIMAL en DB2; lo leemos como string "plano"
                var cntOrdinal = rd.GetOrdinal($"CNT{k}");
                gl = rd.IsDBNull(cntOrdinal) ? null : Convert.ToString(rd.GetValue(cntOrdinal))?.Trim();

                var ccoOrdinal = rd.GetOrdinal($"CCO{k}");
                cc = rd.IsDBNull(ccoOrdinal) ? 0 : Convert.ToInt32(rd.GetValue(ccoOrdinal));
                break;
            }
        }
        return (adqNum, gl, cc);
    }

    // ======================================================================
    // Helpers repuestos: VerCta, Trunc, EtiquetaConcepto, ObtenerTasaCompraUsd
    // ======================================================================



    /// <summary>
    /// Emula la lógica del procedimiento RPG <c>Ver_cta</c> para distinguir
    /// entre Ahorros/Cheques y devolver la etiqueta corta usada en AL3.
    /// 
    /// Nota: si no conoces aún el mapeo exacto por producto/tablas, esta versión
    /// aplica una heurística segura (prefijo) y, si quieres, aquí puedes
    /// reemplazar por una consulta a BNKPRD01.TAP002 cuando tengas las columnas
    /// definitivas.
    /// </summary>
    /// <param name="numeroCuenta">Cuenta del cliente/comercio.</param>
    /// <returns>Tupla (TipoCuenta, DescCorta, Descripcion, EsAhorro, EsCheques)</returns>
    private VerCtaResult VerCta(string numeroCuenta)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
            return new VerCtaResult { DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };

        //Consulta a TAP002 para obtener DMTYP (tipo de cuenta)
        var qverCtaQuery = new SelectQueryBuilder("TAP00201", "BNKPRD01")
            .Select("DMTYP")
            .WhereRaw($"DMACCT = {numeroCuenta}")
            .FetchNext(1)
            .Build();

        var cmd = _connection.GetDbCommand(qverCtaQuery, _contextAccessor.HttpContext!);
        var rd = cmd.ExecuteReader();

        if (!rd.Read()) return new VerCtaResult { TipoCuenta = 0m, DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };

        int tipoCuenta = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0));

        if (tipoCuenta == 1)
            return new VerCtaResult { TipoCuenta = 1m, DescCorta = "AHO", Descripcion = "Ahorros", EsAhorro = true, EsCheques = false };

        if (tipoCuenta == 6)
            return new VerCtaResult { TipoCuenta = 6m, DescCorta = "CHE", Descripcion = "Cheques", EsAhorro = false, EsCheques = true };

        return new VerCtaResult { TipoCuenta = 0m, DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };
    }

    /// <summary>
    /// Trunca una cadena a <paramref name="max"/> caracteres (segura para null).
    /// </summary>
    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        if (max <= 0) return string.Empty;
        return s.Length <= max ? s : s[..max];
    }


    /// <summary>
    /// Obtiene la tasa de compra USD (si aplica). Si aún no tienes
    /// la tabla/fuente, devuelve 0 sin detener el proceso.
    /// </summary>
    static private decimal ObtenerTasaCompraUsd()
    {
        try
        {
            // Si tienes tabla de tasas, reemplaza este bloque por tu SELECT real.
            // Ejemplo ilustrativo (ajusta nombres reales):
            // var q = QueryBuilder.Core.QueryBuilder
            //     .From("TASAS", "BNKPRD01")
            //     .Select("TASA_COMPRA_USD")
            //     .OrderBy("FECHA", QueryBuilder.Enums.SortDirection.Desc)
            //     .FetchNext(1)
            //     .Build()
            //
            // using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!)
            // var obj = cmd.ExecuteScalar()
            // return obj is null || obj is DBNull ? 0m : Convert.ToDecimal(obj, CultureInfo.InvariantCulture)

            return 0m; // fallback seguro
        }
        catch
        {
            return 0m; // nunca rompas el flujo por tasa
        }
    }

    /// <summary>
    /// Lee de CFP801 si el perfil genera asiento de balance (CFTSGE=1) y obtiene sus cuentas/CC.
    /// <param name="perfil">Perfil transerver.</param>
    /// </summary>
    /// <returns>Tupla (enabled, glDebito, ccDebito, glCredito, ccCredito)</returns>
    private (bool enabled, string glDebito, int ccDebito, string glCredito, int ccCredito) TryGetAutoBalance(string perfil)
    {
        // SELECT CFTSGE, CFTSGD, CFTCCD, CFTSGC, CFTCCC FROM BNKPRD01.CFP801 WHERE CFTSBK=1 AND CFTSKY=:perfil
        var q = QueryBuilder.Core.QueryBuilder
            .From("CFP801", "BNKPRD01")
            .Select("CFTSGE", "CFTSGD", "CFTCCD", "CFTSGC", "CFTCCC")
            .Where<Cfp801>(x => x.CFTSBK == 1)
            .Where<Cfp801>(x => x.CFTSKY == perfil)
            .FetchNext(1)
            .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return (false, "", 0, "", 0);

        int sge = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0));
        string glDb = rd.IsDBNull(1) ? "" : rd.GetValue(1).ToString()!.Trim();
        int ccDb = rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2));
        string glCr = rd.IsDBNull(3) ? "" : rd.GetValue(3).ToString()!.Trim();
        int ccCr = rd.IsDBNull(4) ? 0 : Convert.ToInt32(rd.GetValue(4));

        return (sge == 1, glDb, ccDb, glCr, ccCr);
    }

    /// <summary>
    /// Ejecuta INT_LOTES con dos movimientos completos (Débito y Crédito) usando un DTO ya resuelto.
    /// - Mov1 = lado DEBITADO (PMTIPO01..PMMONE01 + DESDB1..3)
    /// - Mov2 = lado ACREDITADO (PMTIPO02..PMMONE02 + DESCR1..3)
    /// OUT: CODER, DESERR, NomArc
    /// </summary>
    /// <param name="p">Parámetros ya armados para INT_LOTES (ambos movimientos, descripciones y generales).</param>
    /// <returns>Tupla (CodigoErrorPosteo, DescripcionErrorPosteo, NomArc)</returns>
    public async Task<(string CodigoErrorPosteo, string DescripcionErrorPosteo, string? NomArc)> PosteoLoteAsync(IntLotesParamsDto p)
    {
        try
        {
            var builder = ProgramCallBuilder.For(_connection, "BCAH96", "INT_LOTES")
                .UseSqlNaming()
                .WrapCallWithBraces();

            // ===================== Movimiento 1 (Débito) =====================
            builder.InDecimal("PMTIPO01", p.TipoMov1, precision: 2, scale: 0);
            builder.InDecimal("PMCTAA01", p.CuentaMov1, precision: 13, scale: 0);
            builder.InDecimal("PMVALR01", p.MontoMov1, precision: 13, scale: 2);
            builder.InChar("PMDECR01", string.IsNullOrEmpty(p.DeCr1) ? "D" : p.DeCr1, 1);
            builder.InDecimal("PMCCOS01", p.CentroCosto1, precision: 5, scale: 0);
            builder.InDecimal("PMMONE01", p.MonedaMov1 > 0 ? p.MonedaMov1 : p.Moneda, precision: 3, scale: 0);

            // ===================== Movimiento 2 (Crédito) ====================
            builder.InDecimal("PMTIPO02", p.TipoMov2, precision: 2, scale: 0);
            builder.InDecimal("PMCTAA02", p.CuentaMov2, precision: 13, scale: 0);
            builder.InDecimal("PMVALR02", p.MontoMov2, precision: 13, scale: 2);
            builder.InChar("PMDECR02", string.IsNullOrEmpty(p.DeCr2) ? "C" : p.DeCr2, 1);
            builder.InDecimal("PMCCOS02", p.CentroCosto2, precision: 5, scale: 0);
            builder.InDecimal("PMMONE02", p.MonedaMov2 > 0 ? p.MonedaMov2 : p.Moneda, precision: 3, scale: 0);

            // ===================== Movimiento 3 (vacío) ======================
            builder.InDecimal("PMTIPO03", 0m, precision: 2, scale: 0);
            builder.InDecimal("PMCTAA03", 0m, precision: 13, scale: 0);
            builder.InDecimal("PMVALR03", 0m, precision: 13, scale: 2);
            builder.InChar("PMDECR03", "", 1);
            builder.InDecimal("PMCCOS03", 0m, precision: 5, scale: 0);
            builder.InDecimal("PMMONE03", 0m, precision: 3, scale: 0);

            // ===================== Movimiento 4 (vacío) ======================
            builder.InDecimal("PMTIPO04", 0m, precision: 2, scale: 0);
            builder.InDecimal("PMCTAA04", 0m, precision: 13, scale: 0);
            builder.InDecimal("PMVALR04", 0m, precision: 13, scale: 2);
            builder.InChar("PMDECR04", "", 1);
            builder.InDecimal("PMCCOS04", 0m, precision: 5, scale: 0);
            builder.InDecimal("PMMONE04", 0m, precision: 3, scale: 0);

            // ===================== Generales/Descripciones ===================
            builder.InChar("PMPERFIL", p.Perfil, 13);
            builder.InDecimal("MONEDA", p.Moneda, precision: 3, scale: 0);

            // Lado DEBITADO → DESDBx
            builder.InChar("DESDB1", p.DesDB1 ?? "", 40);
            builder.InChar("DESDB2", p.DesDB2 ?? "", 40);
            builder.InChar("DESDB3", p.DesDB3 ?? "", 40);

            // Lado ACREDITADO → DESCRx
            builder.InChar("DESCR1", p.DesCR1 ?? "", 40);
            builder.InChar("DESCR2", p.DesCR2 ?? "", 40);
            builder.InChar("DESCR3", p.DesCR3 ?? "", 40);

            // ===================== OUT =====================
            builder.OutDecimal("CODER", 2, 0);
            builder.OutChar("DESERR", 70);
            builder.OutChar("NomArc", 10);

            var result = await builder.CallAsync(_contextAccessor.HttpContext);

            result.TryGet("CODER", out int codigoError);
            result.TryGet("DESERR", out string? descripcionError);
            result.TryGet("NomArc", out string? nomArc);

            if (codigoError != 0)
            {
                return (BizCodes.IntLotesFail, $"Error en INT_LOTES: {descripcionError}", nomArc);
            }
            else
            {
                return (BizCodes.Ok, "Posteo de Lote Exitoso", nomArc);
            }
        }
        catch (SqlException ex)
        {
            return (BizCodes.ErrorSql, "Error SQL en PosteoLoteAsync: " + ex.Message, "");
        }
        catch (Exception ex)
        {
            return (BizCodes.ErrorDesconocido, "Error general en PosteoLoteAsync: " + ex.Message, "");
        }
    }

    /// <summary>
    /// Método para agregar librerías a la LIBL de la conexión actual.
    /// </summary>
    /// <returns>Tupla (agregoLibrerias, codigoErrorLibrerias, descripcionErrorLibrerias)</returns>
    private (bool agregoLibrerias, string codigoErrorLibrerias, string descripcionErrorLibrerias) CargaLibrerias()
    {
        try
        {
            // Lista completa que quieres dejar en LIBL (ajusta el orden a tu necesidad)
            var libl = "QTEMP ICBS BCAH96 BCAH96DTA BNKPRD01 QGPL GX COVENPGMV4";

            // Comando CL en un SOLO statement
            var clCmd = $"CHGLIBL LIBL({libl})";

            // Longitud para QCMDEXC = número de caracteres del comando, con escala 5
            static decimal QcmdexcLen(string s) => Convert.ToDecimal(s.Length.ToString() + ".00000", CultureInfo.InvariantCulture);

            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = "CALL QSYS2.QCMDEXC(?, ?)";
            var p1 = cmd.CreateParameter(); p1.DbType = System.Data.DbType.String; p1.Value = clCmd; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.DbType = System.Data.DbType.Decimal; p2.Precision = 15; p2.Scale = 5; p2.Value = QcmdexcLen(clCmd); cmd.Parameters.Add(p2);

            cmd.ExecuteNonQuery();

            return (true, BizCodes.Ok, "Se agregaron las librias");
        }
        catch (SqlException ex)
        {
            return (false, BizCodes.ErrorSql, $"Error SQL al agregar librerías a LIBL. : {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, BizCodes.ErrorDesconocido, $"Error al agregar librerías a LIBL. : {ex.Message}");
        }
    }

    // ===== Normalizadores y helpers =====
    private static string OnlyDigits(string? s)
        => new([.. (s ?? string.Empty).Where(char.IsDigit)]);

    private static string EstadoOkCode(string? code)
    {
        // Acepta "0000" o "00000" y lo normaliza a 5 dígitos.
        var digits = OnlyDigits(code);
        if (string.IsNullOrEmpty(digits)) return "99999";
        return digits.PadLeft(5, '0');
    }

    private static bool IsOk(string? code)
    {
        var c = EstadoOkCode(code);
        return c == "00000";
    }

    private static (string corte6, string stan6) ClavesCorteStan(string numeroCorte, string idTransaccionUnico)
    {
        var corte6 = OnlyDigits(numeroCorte);
        var stan6 = idTransaccionUnico;
        return (corte6, stan6);
    }

    private static bool IsUniqueViolation(Exception ex)
    {
        var msg = (ex?.ToString() ?? string.Empty).ToUpperInvariant();
        // DB2 for i: SQL0803N / SQLSTATE 23505 (clave duplicada)
        return msg.Contains("SQL0803") || msg.Contains("23505") || msg.Contains("DUPLIC");
    }

    // ===== Inserta la reserva inicial en POSRE01G =====
    private async Task<(bool inserted, bool duplicate, string? errorMsg)> GuardarReservaPosre01gAsync(
        GuardarTransaccionesDto dto, CancellationToken ct)
    {
        var (corte6, stan6) = ClavesCorteStan(dto.NumeroDeCorte, dto.IdTransaccionUnico);

        var now = DateTime.Now;
        var guid = Guid.NewGuid().ToString().ToUpperInvariant();

        // Valores ya ajustados a longitudes DDS
        var FECHAPOST = now.ToString("yyyyMMdd");
        var HORAPOST = now.ToString("HHmmss");

        var NUMCUENTA = dto.NumeroCuenta;
        var MTODEBITO = dto.MontoDebitado;
        var MTOACREDI = dto.MontoAcreditado;

        var CODCOMERC = dto.CodigoComercio;
        var NOMCOMERC = dto.NombreComercio;
        var TERMINAL = dto.Terminal;
        var DESCRIPC = dto.Descripcion;

        var NATCONTA = dto.NaturalezaContable.ToUpperInvariant();
        var NUMCORTE = corte6;
        var IDTRANUNI = stan6;

        var ESTADO = "P";                             // Pendiente
        var DESCESTADO = "En proceso";
        var CODERROR = "99999";                         // ≠ éxito
        var DESCERROR = "Reserva inicial";

        var ins = new InsertQueryBuilder("IPOSRE01G1", "BCAH96DTA") //iNDICE IPOSRE01G1  TABLA  POSRE01G
            .IntoColumns(
                "GUID", "FECHAPOST", "HORAPOST", "NUMCUENTA", "MTODEBITO", "MTOACREDI",
                "CODCOMERC", "NOMCOMERC", "TERMINAL", "DESCRIPC", "NATCONTA",
                "NUMCORTE", "IDTRANUNI", "ESTADO", "DESCESTADO", "CODERROR", "DESCERROR"
            )
            .Values(
                ("GUID", guid),
                ("FECHAPOST", FECHAPOST),
                ("HORAPOST", HORAPOST),
                ("NUMCUENTA", NUMCUENTA),
                ("MTODEBITO", MTODEBITO),
                ("MTOACREDI", MTOACREDI),
                ("CODCOMERC", CODCOMERC),
                ("NOMCOMERC", NOMCOMERC),
                ("TERMINAL", TERMINAL),
                ("DESCRIPC", DESCRIPC),
                ("NATCONTA", NATCONTA),
                ("NUMCORTE", NUMCORTE),
                ("IDTRANUNI", IDTRANUNI),
                ("ESTADO", ESTADO),
                ("DESCESTADO", DESCESTADO),
                ("CODERROR", CODERROR),
                ("DESCERROR", DESCERROR)
            )
            .WithComment($"Reserva inicial anti-duplicados via LF IPOSRE01G1 ({NUMCORTE} + {IDTRANUNI})")
            .Build();

        try
        {
            using var cmd = _connection.GetDbCommand(ins, _contextAccessor.HttpContext!);
            await cmd.ExecuteNonQueryAsync(ct);
            return (true, false, null);
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            // Ya existe la combinación (NUMCORTE, IDTRANUNI) por el índice UNIQUE IPOSRE01G1
            return (false, true, ex.Message);
        }
        catch (Exception ex)
        {
            return (false, false, ex.Message);
        }
    }

    // ===== Actualiza el resultado del posteo en POSRE01G =====
    private async Task<bool> ActualizarResultadoPosre01gAsync(
        string numeroCorte, string idTransaccionUnico, string codigo, string descripcion, CancellationToken ct)
    {
        var (corte6, stan6) = ClavesCorteStan(numeroCorte, idTransaccionUnico);
        var code5 = EstadoOkCode(codigo);

        var ESTADO = IsOk(code5) ? "A" : "R";                    // A=aprobada, R=rechazada
        var DESCESTADO = IsOk(code5) ? "Aprobada" : "Rechazada";

        var upd = new UpdateQueryBuilder("POSRE01G", "BCAH96DTA", SqlDialect.Db2i)
            .Set("CODERROR", code5)
            .Set("DESCERROR", descripcion)
            .Set("ESTADO", ESTADO)
            .Set("DESCESTADO", DESCESTADO)
            .WhereRaw($"NUMCORTE = '{corte6}'")
            .WhereRaw($"IDTRANUNI = '{stan6}'")
            .Build();

        using var cmd = _connection.GetDbCommand(upd, _contextAccessor.HttpContext!);
        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }

    /// <summary>
    /// Crea un DTO de respuesta de error con metadatos consistentes.
    /// </summary>
    private static RespuestaGuardarTransaccionesDto BuildError(string code, string message)
        => new()
        {
            CodigoError = code,
            DescripcionError = message
        };
}


