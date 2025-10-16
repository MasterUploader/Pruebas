using API_1_TERCEROS_REMESADORAS.Models;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Autenticacion.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.ConfirmacionPago.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.ConfirmacionTransaccionDirecta.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Consulta.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Pago.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.RechazoPago.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Reporteria.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Reporteria.SDEP.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Reverso.Request;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.AuthenticateService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConfirmacionPago;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConfirmacionTransaccionDirectaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConsultaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.PagoService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.RechazoPago;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.SEDP;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReversoServices;
using API_Terceros.Models;
using Microsoft.AspNetCore.Mvc;

namespace API_1_TERCEROS_REMESADORAS.Controllers;

/// <summary>
/// Controlador BTS (Remesadoras) — Orquesta endpoints para autenticación, consulta,
/// pago, reverso, confirmaciones, rechazos y reportería (incl. SDEP) contra el backend BTS.
/// </summary>
/// <remarks>
/// <para><b>Versión de API:</b> v1</para>
/// <para><b>Formato:</b> <c>application/json</c></para>
/// <para><b>Contratos:</b> Todas las operaciones reciben <see cref="RequestModel{TBody}"/> y devuelven <see cref="ResponseModel"/>.</para>
/// <para><b>Cabeceras esperadas:</b> el <c>RequestHeader</c> interno del contrato incluye metadatos (p. ej., <c>TraceId</c>, canal, etc.).</para>
/// <para><b>Seguridad:</b> la autenticación JWT y/o tokens BTS debe configurarse a nivel middleware/servicio.</para>
/// <para><b>Convenciones de estado:</b> 200 = éxito de negocio; 400 = validación/negocio fallido; 500 = error no controlado.</para>
/// </remarks>
/// <param name="authenticateService">
/// Servicio de autenticación BTS. Resuelve el proceso de emisión/validación de credenciales.
/// </param>
/// <param name="consultaService">
/// Servicio de consulta BTS. Recupera información de remesas (remitente/destinatario, estados, etc.).
/// </param>
/// <param name="pagoService">
/// Servicio de pago BTS. Ejecuta la transacción de pago conforme a reglas de negocio vigentes.
/// </param>
/// <param name="reversoService">
/// Servicio de reverso BTS. Gestiona la reversión de transacciones previamente aceptadas.
/// </param>
/// <param name="confirmacionTransaccionDirecta">
/// Servicio de confirmación directa BTS. Confirma transacciones iniciadas en otros frentes.
/// </param>
/// <param name="confirmacionPago">
/// Servicio de confirmación de pago BTS. Confirma la aplicación del pago en BTS.
/// </param>
/// <param name="rechazoPago">
/// Servicio de rechazo de pago BTS. Registra el rechazo de una transacción de pago.
/// </param>
/// <param name="reporteriaService">
/// Servicio de reportería BTS. Expone consultas de reportes parametrizados.
/// </param>
/// <param name="sEDPService">
/// Servicio SDEP (reportería específica). Consulta transacciones por cliente/canales según SDEP.
/// </param>
[Route("v1/[controller]")]
[ApiController]
#pragma warning disable S6960 // Controllers should not have mixed responsibilities
public class BtsController(
    IAuthenticateService authenticateService,
    IConsultaService consultaService,
    IPagoService pagoService,
#pragma warning restore S6960 // Controllers should not have mixed responsibilities
    IReversoService reversoService,
    IConfirmacionTransaccionDirecta confirmacionTransaccionDirecta,
    IConfirmacionPago confirmacionPago,
    IRechazoPago rechazoPago,
    IReporteriaService reporteriaService,
    ISEDPService sEDPService) : ControllerBase
{
    private readonly IAuthenticateService _authenticateService = authenticateService;
    private readonly IConsultaService _consultaService = consultaService;
    private readonly IPagoService _pagoService = pagoService;
    private readonly IReversoService _reversoService = reversoService;
    private readonly IConfirmacionTransaccionDirecta _confirmacionTransaccionDirecta = confirmacionTransaccionDirecta;
    private readonly IConfirmacionPago _confirmacionPago = confirmacionPago;
    private readonly IRechazoPago _rechazoPago = rechazoPago;
    private readonly IReporteriaService _reporteriaService = reporteriaService;
    private readonly ISEDPService _sEDPService = sEDPService;

    /// <summary>
    /// Autenticación BTS.
    /// </summary>
    /// <remarks>
    /// <para>Realiza el flujo de autenticación contra BTS y retorna datos de sesión/token.</para>
    /// <para><b>Idempotencia:</b> N/A (consulta de estado). Puede repetirse sin efectos colaterales.</para>
    /// <para><b>Ejemplo (body):</b></para>
    /// <code language="json">
    /// {
    ///   "header": { "traceId": "..." },
    ///   "body": { "user": "xxxx", "password": "****" }
    /// }
    /// </code>
    /// <para><b>HTTP</b>: 200 (éxito), 400 (credenciales inválidas / política), 500 (error interno).</para>
    /// </remarks>
    /// <param name="requestModel">
    /// Modelo de solicitud con encabezado de rastreo y <see cref="AuthenticateBody"/> para credenciales/parámetros BTS.
    /// </param>
    /// <returns>
    /// <see cref="IActionResult"/> con <see cref="ResponseModel"/>:
    /// en <c>Header</c> se refleja <c>StatusCode</c>/<c>Message</c> provistos por BTS; <c>Data</c> contiene <c>jsonResponse</c>.
    /// </returns>
    [HttpPost("Autenticacion/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AutenticacionBTS([FromBody] RequestModel<AuthenticateBody> requestModel)
    {
        var (jsonResponse, statusCode) = await _authenticateService.AuthenticateServiceRequestAsync();

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.Process_Msg
            },
            Data = new { jsonResponse }
        };
        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Consulta de remesa en BTS.
    /// </summary>
    /// <remarks>
    /// <para>Recupera detalles de la remesa (remitente, destinatario, estado, validaciones).</para>
    /// <para><b>Idempotencia:</b> Lectura pura.</para>
    /// <para><b>Ejemplo (body):</b></para>
    /// <code language="json">
    /// {
    ///   "header": { "traceId": "..." },
    ///   "body": { "remittanceId": "ABC123", "country": "HN" }
    /// }
    /// </code>
    /// <para><b>HTTP</b>: 200 (encontrado), 400 (parámetros inválidos/reglas negocio), 500 (error interno).</para>
    /// </remarks>
    /// <param name="requestModel">
    /// Modelo con encabezado de rastreo y <see cref="ConsultaBody"/> (criterios de consulta).
    /// </param>
    /// <returns>Resultado estándar <see cref="ResponseModel"/> con datos de consulta en <c>Data</c>.</returns>
    [HttpPost("Consulta/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConsultaBTS([FromBody] RequestModel<ConsultaBody> requestModel)
    {
        var (jsonResponse, statusCode) = await _consultaService.ConsultaServiceRequestAsync(requestModel.Body);

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.ProcessMsg
            },
            Data = jsonResponse
        };
        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Pago de remesa en BTS.
    /// </summary>
    /// <remarks>
    /// <para>Ejecuta la transacción de pago con validaciones antifraude, disponibilidad y reglas BTS.</para>
    /// <para><b>Idempotencia:</b> Se recomienda cabecera de idempotencia a nivel arquitectura (no incluida explícitamente en este contrato).</para>
    /// <para><b>Side-effects:</b> Puede afectar saldos/estados en BTS.</para>
    /// <para><b>HTTP</b>: 200 (aplicado), 400 (regla de negocio / validación), 500 (error interno).</para>
    /// </remarks>
    /// <param name="requestModel">
    /// Modelo con encabezado de rastreo y <see cref="PagoBody"/> (datos de pago).
    /// </param>
    /// <returns>Respuesta estándar con resultado de pago y mensajes de proceso.</returns>
    [HttpPost("Pago/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PagoBTS([FromBody] RequestModel<PagoBody> requestModel)
    {
        var (jsonResponse, statusCode) = await _pagoService.PagoServiceRequestAsync(requestModel.Body);

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.ProcessMsg
            },
            Data = jsonResponse
        };

        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Reverso de transacción en BTS.
    /// </summary>
    /// <remarks>
    /// <para>Revierte una transacción previamente aceptada bajo condiciones de negocio y ventanas de tiempo.</para>
    /// <para><b>Idempotencia:</b> Recomendado control externo por <c>TraceId</c>/clave idempotente.</para>
    /// <para><b>Side-effects:</b> Deshace efectos contables/operativos del pago.</para>
    /// <para><b>HTTP</b>: 200 (revertido), 400 (no elegible / expirado), 500 (error interno).</para>
    /// </remarks>
    /// <param name="requestModel">
    /// Modelo con <see cref="ReversoBody"/> (identificadores y causal).
    /// </param>
    /// <returns>Respuesta estándar con resultado del reverso.</returns>
    [HttpPost("Reverso/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReversoBTS([FromBody] RequestModel<ReversoBody> requestModel)
    {
        var (jsonResponse, statusCode) = await _reversoService.ReversoServiceRequestAsync(requestModel.Body);

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.ProcessMsg
            },
            Data = jsonResponse
        };
        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Confirmación directa de transacción en BTS.
    /// </summary>
    /// <remarks>
    /// <para>Confirma una transacción iniciada/registrada por otro frente (p. ej., canal externo).</para>
    /// <para><b>Idempotencia:</b> Recomendada.</para>
    /// <para><b>HTTP</b>: 200 (confirmada), 400 (inconsistencia/ya confirmada/no elegible), 500 (error interno).</para>
    /// </remarks>
    /// <param name="requestModel">
    /// Modelo con <see cref="ConfirmacionTransaccionDirectaBody"/>.
    /// </param>
    /// <returns>Resultado de confirmación en <see cref="ResponseModel"/>.</returns>
    [HttpPost("ConfirmacionTransaccionDirecta/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmacionTransaccionDirectaBTS([FromBody] RequestModel<ConfirmacionTransaccionDirectaBody> requestModel)
    {
        var (jsonResponse, statusCode) = await _confirmacionTransaccionDirecta.ConfirmacionTransaccionDirectaServiceRequestAsync(requestModel.Body);

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.ProcessMsg
            },
            Data = jsonResponse
        };
        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Confirmación de pago en BTS.
    /// </summary>
    /// <remarks>
    /// <para>Registra/valida la confirmación del pago (p. ej., liquidación final).</para>
    /// <para><b>Idempotencia:</b> Recomendada si el integrador puede reintentar.</para>
    /// <para><b>HTTP</b>: 200 (confirmado), 400 (no elegible / duplicado / validación), 500 (error interno).</para>
    /// </remarks>
    /// <param name="requestModel">
    /// Modelo con <see cref="ConfirmacionPagoBody"/>.
    /// </param>
    /// <returns>Respuesta de confirmación en <see cref="ResponseModel"/>.</returns>
    [HttpPost("ConfirmacionPago/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmacionPagoBTS([FromBody] RequestModel<ConfirmacionPagoBody> requestModel)
    {
        var (jsonResponse, statusCode) = await _confirmacionPago.ConfirmacionPagoServiceRequestAsync(requestModel.Body);

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.ProcessMsg
            },
            Data = jsonResponse
        };
        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Rechazo de pago en BTS.
    /// </summary>
    /// <remarks>
    /// <para>Registra el rechazo de una solicitud de pago con su causal documentada.</para>
    /// <para><b>Idempotencia:</b> Recomendada para evitar duplicidad de rechazos.</para>
    /// <para><b>HTTP</b>: 200 (rechazo registrado), 400 (estado no compatible / validación), 500 (error interno).</para>
    /// </remarks>
    /// <param name="requestModel">
    /// Modelo con <see cref="RechazoPagoBody"/> (motivo/causal, referencias).
    /// </param>
    /// <returns>Resultado del rechazo en <see cref="ResponseModel"/>.</returns>
    [HttpPost("RechazoPago/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RechazoPagoBTS([FromBody] RequestModel<RechazoPagoBody> requestModel)
    {
        var (jsonResponse, statusCode) = await _rechazoPago.RechazoPagoServiceRequestAsync(requestModel.Body);

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.ProcessMsg
            },
            Data = jsonResponse
        };
        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Reportería BTS (consultas generales).
    /// </summary>
    /// <remarks>
    /// <para>Ejecuta reportes parametrizados expuestos por BTS (rangos de fechas, estados, agencias, etc.).</para>
    /// <para><b>Idempotencia:</b> Lectura.</para>
    /// <para><b>HTTP</b>: 200 (ok), 400 (parámetros inválidos), 500 (error interno).</para>
    /// </remarks>
    /// <param name="requestModel">
    /// Modelo con <see cref="ReporteriaBody"/> (filtros del reporte).
    /// </param>
    /// <returns>Resultado del reporte en <see cref="ResponseModel"/>.</returns>
    [HttpPost("Reporteria/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReporteriaBTS([FromBody] RequestModel<ReporteriaBody> requestModel)
    {
        var (jsonResponse, statusCode) = await _reporteriaService.ReporteriaServiceRequestAsync(requestModel.Body);

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.ProcessMsg
            },
            Data = jsonResponse
        };
        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Reportería SDEP (por cliente/transacciones).
    /// </summary>
    /// <remarks>
    /// <para>Consulta transacciones del cliente bajo el esquema SDEP (según parámetros y políticas BTS).</para>
    /// <para><b>Idempotencia:</b> Lectura.</para>
    /// <para><b>HTTP</b>: 200 (ok), 400 (validación), 500 (error interno).</para>
    /// </remarks>
    /// <param name="requestModel">
    /// Modelo con <see cref="SdepBody"/> (parámetros SDEP).
    /// </param>
    /// <returns>Resultado SDEP en <see cref="ResponseModel"/>.</returns>
    [HttpPost("ReporteriaSDEP/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerTransaccionesClienteBts([FromBody] RequestModel<SdepBody> requestModel)
    {
        var (jsonResponse, statusCode) = await _sEDPService.SDEPServiceRequestAsync(requestModel.Body);

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.ProcessMsg
            },
            Data = jsonResponse
        };
        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }
}
