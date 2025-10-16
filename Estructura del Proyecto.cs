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
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace API_1_TERCEROS_REMESADORAS.Controllers;

/// <summary>
/// Controlador BTS (Remesadoras). Orquesta endpoints para autenticación, consulta,
/// pago, reverso, confirmaciones, rechazos y reportería (incl. SDEP) contra el backend BTS.
/// </summary>
/// <remarks>
/// <para><b>Versión:</b> v1 — <b>Formato:</b> <c>application/json</c></para>
/// <para><b>Contratos:</b> Entradas vía <see cref="RequestModel{TBody}"/> y salidas en <see cref="ResponseModel"/>.</para>
/// <para><b>Trazabilidad:</b> Utiliza <c>RequestHeader.TraceId</c> para correlación end-to-end.</para>
/// <para><b>Convención HTTP:</b> 200 = éxito negocio; 400 = validación/reglas; 500 = error inesperado.</para>
/// </remarks>
/// <param name="authenticateService">Servicio de autenticación BTS.</param>
/// <param name="consultaService">Servicio de consulta BTS.</param>
/// <param name="pagoService">Servicio de pago BTS.</param>
/// <param name="reversoService">Servicio de reverso BTS.</param>
/// <param name="confirmacionTransaccionDirecta">Servicio de confirmación directa BTS.</param>
/// <param name="confirmacionPago">Servicio de confirmación de pago BTS.</param>
/// <param name="rechazoPago">Servicio de rechazo de pago BTS.</param>
/// <param name="reporteriaService">Servicio de reportería BTS.</param>
/// <param name="sEDPService">Servicio SDEP (reportería específica).</param>
[Route("v1/[controller]")]
[ApiController]
#pragma warning disable S6960 // Controllers should not have mixed responsibilities
public class BtsController(
#pragma warning restore S6960
    IAuthenticateService authenticateService,
    IConsultaService consultaService,
    IPagoService pagoService,
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

    /// <summary>Autenticación BTS.</summary>
    /// <remarks>
    /// <para><b>Descripción ampliada:</b> Realiza el flujo de autenticación y retorna datos de sesión/token. Idempotencia de lectura; reintentos seguros.</para>
    /// <para><b>Notas del contrato (anterior):</b> Authenticates a user or system based on the provided request model.  
    /// The response includes a header with the status code and message, and a data object containing the authentication response details.</para>
    /// <para><b>Servicio utilizado:</b> <see cref="_authenticateService"/> (AuthenticateServiceRequestAsync).</para>
    /// </remarks>
    /// <param name="requestModel">Modelo con encabezado de rastreo y <see cref="AuthenticateBody"/> (credenciales/parámetros).</param>
    /// <returns>
    /// Un <see cref="IActionResult"/> con <see cref="ResponseModel"/>; el encabezado refleja <c>StatusCode</c>/<c>Message</c> de BTS y <c>Data</c> incluye el objeto de autenticación.
    /// 200 OK si la autenticación es exitosa; 400 BadRequest si falla.
    /// </returns>
    [HttpPost("Autenticacion/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation(
        OperationId = "BTS_Authenticate",
        Tags = new[] { "BTS" },
        Summary = "Autenticación",
        Description = "Valida credenciales en BTS y devuelve datos de sesión/token."
    )]
    [SwaggerRequestExample(typeof(RequestModel<AuthenticateBody>), typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.AuthenticateRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ResponseModelExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Éxito de autenticación", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Credenciales inválidas / política", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error inesperado")]
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

    /// <summary>Consulta de remesa.</summary>
    /// <remarks>
    /// <para><b>Descripción ampliada:</b> Recupera detalles (remitente, destinatario, estado, validaciones). Idempotencia de lectura.</para>
    /// <para><b>Notas del contrato (anterior):</b> Consultation of BTS remittance details, including sender and recipient information.  
    /// Employs the model RequestModel with a body of type <see cref="ConsultaBody"/> for input and returns a <see cref="ResponseModel"/> containing the consultation results.</para>
    /// <para><b>Servicio utilizado:</b> <see cref="_consultaService"/> (ConsultaServiceRequestAsync).</para>
    /// </remarks>
    /// <param name="requestModel">Encabezado y <see cref="ConsultaBody"/> con criterios de consulta.</param>
    /// <returns>Un <see cref="ResponseModel"/> con 200 OK si la consulta es exitosa o 400 BadRequest si falla.</returns>
    [HttpPost("Consulta/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation(
        OperationId = "BTS_Consulta",
        Tags = new[] { "BTS" },
        Summary = "Consulta de remesa",
        Description = "Obtiene información de la remesa y su estado."
    )]
    [SwaggerRequestExample(typeof(RequestModel<ConsultaBody>), typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ConsultaRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ResponseModelExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Consulta exitosa", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Parámetros inválidos / reglas de negocio", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error inesperado")]
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

    /// <summary>Pago de remesa.</summary>
    /// <remarks>
    /// <para><b>Descripción ampliada:</b> Ejecuta la transacción de pago con validaciones y reglas BTS. Puede tener efectos sobre saldos/estados. Recomendado control de idempotencia a nivel arquitectura.</para>
    /// <para><b>Notas del contrato (anterior):</b> Performs a payment transaction through the BTS system using the provided payment details.  
    /// Utilizes the <c>IPagoService</c> to process the request and obtain response data and status code.</para>
    /// <para><b>Servicio utilizado:</b> <see cref="_pagoService"/> (PagoServiceRequestAsync).</para>
    /// </remarks>
    /// <param name="requestModel">Encabezado y <see cref="PagoBody"/> con los datos de pago.</param>
    /// <returns><see cref="ResponseModel"/> con 200 OK si el pago es exitoso o 400 BadRequest si falla.</returns>
    [HttpPost("Pago/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation(
        OperationId = "BTS_Pago",
        Tags = new[] { "BTS" },
        Summary = "Pago de remesa",
        Description = "Aplica el pago de la remesa conforme a reglas de BTS."
    )]
    [SwaggerRequestExample(typeof(RequestModel<PagoBody>), typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.PagoRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ResponseModelExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Pago aplicado", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validación/regla de negocio fallida", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error inesperado")]
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

    /// <summary>Reverso de transacción.</summary>
    /// <remarks>
    /// <para><b>Descripción ampliada:</b> Revierte una transacción previamente aceptada dentro de ventanas y causales permitidas. Side-effects contables/operativos.</para>
    /// <para><b>Notas del contrato (anterior):</b> Performs a reversal transaction through the BTS system using the provided reversal details.  
    /// Utilizes the <c>IReversoService</c> to process the request and obtain response data and status code.</para>
    /// <para><b>Servicio utilizado:</b> <see cref="_reversoService"/> (ReversoServiceRequestAsync).</para>
    /// </remarks>
    /// <param name="requestModel">Encabezado y <see cref="ReversoBody"/> (referencias/causal).</param>
    /// <returns><see cref="ResponseModel"/> con 200 OK si el reverso es exitoso o 400 BadRequest si falla.</returns>
    [HttpPost("Reverso/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation(
        OperationId = "BTS_Reverso",
        Tags = new[] { "BTS" },
        Summary = "Reverso",
        Description = "Revierte una transacción previamente aceptada si aplica."
    )]
    [SwaggerRequestExample(typeof(RequestModel<ReversoBody>), typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ReversoRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ResponseModelExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Reverso aplicado", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "No elegible/expirado/validación", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error inesperado")]
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

    /// <summary>Confirmación directa de transacción.</summary>
    /// <remarks>
    /// <para><b>Descripción ampliada:</b> Confirma transacciones iniciadas por otro frente/canal (si son elegibles).</para>
    /// <para><b>Notas del contrato (anterior):</b> Performs a direct transaction confirmation through BTS using the provided details.  
    /// Utilizes the <c>IConfirmacionTransaccionDirecta</c> to process the request and obtain response data and status code.</para>
    /// <para><b>Servicio utilizado:</b> <see cref="_confirmacionTransaccionDirecta"/> (ConfirmacionTransaccionDirectaServiceRequestAsync).</para>
    /// </remarks>
    /// <param name="requestModel">Encabezado y <see cref="ConfirmacionTransaccionDirectaBody"/>.</param>
    /// <returns><see cref="ResponseModel"/> con 200 OK si la confirmación es exitosa o 400 BadRequest si falla.</returns>
    [HttpPost("ConfirmacionTransaccionDirecta/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation(
        OperationId = "BTS_ConfirmacionDirecta",
        Tags = new[] { "BTS" },
        Summary = "Confirmación directa",
        Description = "Confirma una transacción registrada por otro frente."
    )]
    [SwaggerRequestExample(typeof(RequestModel<ConfirmacionTransaccionDirectaBody>), typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ConfirmacionDirectaRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ResponseModelExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Confirmación exitosa", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Inconsistencia/ya confirmada/no elegible", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error inesperado")]
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

    /// <summary>Confirmación de pago.</summary>
    /// <remarks>
    /// <para><b>Descripción ampliada:</b> Registra/valida la confirmación del pago (p. ej., liquidación final). Idempotencia recomendada para reintentos.</para>
    /// <para><b>Notas del contrato (anterior):</b> Performs a payment confirmation through BTS.  
    /// Utilizes the <c>IConfirmacionPago</c> to process the request and obtain response data and status code.</para>
    /// <para><b>Servicio utilizado:</b> <see cref="_confirmacionPago"/> (ConfirmacionPagoServiceRequestAsync).</para>
    /// </remarks>
    /// <param name="requestModel">Encabezado y <see cref="ConfirmacionPagoBody"/>.</param>
    /// <returns><see cref="ResponseModel"/> con 200 OK si la confirmación es exitosa o 400 BadRequest si falla.</returns>
    [HttpPost("ConfirmacionPago/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation(
        OperationId = "BTS_ConfirmacionPago",
        Tags = new[] { "BTS" },
        Summary = "Confirmación de pago",
        Description = "Confirma la aplicación del pago en BTS."
    )]
    [SwaggerRequestExample(typeof(RequestModel<ConfirmacionPagoBody>), typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ConfirmacionPagoRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ResponseModelExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Confirmación exitosa", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "No elegible/duplicado/validación", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Error inesperado")]
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

    /// <summary>Rechazo de pago.</summary>
    /// <remarks>
    /// <para><b>Descripción ampliada:</b> Registra el rechazo de una solicitud de pago con su causal documentada.</para>
    /// <para><b>Notas del contrato (anterior):</b> Performs a payment rejection through BTS using the provided rejection details.  
    /// Utilizes the <c>IRechazoPago</c> to process the request and obtain response data and status code.</para>
    /// <para><b>Servicio utilizado:</b> <see cref="_rechazoPago"/> (RechazoPagoServiceRequestAsync).</para>
    /// </remarks>
    /// <param name="requestModel">Encabezado y <see cref="RechazoPagoBody"/> (causal, referencias).</param>
    /// <returns><see cref="ResponseModel"/> con 200 OK si el rechazo se registra o 400 BadRequest si falla.</returns>
    [HttpPost("RechazoPago/")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [SwaggerOperation(
        OperationId = "BTS_RechazoPago",
        Tags = new[] { "BTS" },
        Summary = "Rechazo de pago",
        Description = "Registra el rechazo de una transacción de pago."
    )]
    [SwaggerRequestExample(typeof(RequestModel<RechazoPagoBody>), typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.RechazoPagoRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(API_1_TERCEROS_REMESADORAS.SwaggerExamples.ResponseModelExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Rechazo registrado", typeof(ResponseModel))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "E
