Mejora todos los comentarios, que esten modernos y no omitas nada util.



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
/// Controlador BTS
/// </summary>
/// <remarks>
/// Contructor de BTS Controller
/// </remarks>
/// <param name="authenticateService">Instancia de Authenticate Service.</param>
/// <param name="consultaService">Instancia de Consulta Service.</param>
/// <param name="pagoService">Instancia de Pago Service.</param>
/// <param name="reversoService">Instancia de Reverso Service.</param>
/// <param name="confirmacionTransaccionDirecta">Instancia de Confirmación Transaccion Directa Service.</param>
/// <param name="confirmacionPago">Instancia de Confirmación Pago Service.</param>
/// <param name="rechazoPago">Instancia de Rechazo Pago Service.</param>
/// <param name="reporteriaService">Instancia de Reporteria Service.</param>
/// <param name="sEDPService">Instaacia de ISEDPService.</param>
[Route("v1/[controller]")]
[ApiController]
#pragma warning disable S6960 // Controllers should not have mixed responsibilities
public class BtsController(IAuthenticateService authenticateService, IConsultaService consultaService, IPagoService pagoService,
#pragma warning restore S6960 // Controllers should not have mixed responsibilities
    IReversoService reversoService, IConfirmacionTransaccionDirecta confirmacionTransaccionDirecta, IConfirmacionPago confirmacionPago,
    IRechazoPago rechazoPago, IReporteriaService reporteriaService, ISEDPService sEDPService) : ControllerBase
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
    /// Authenticates a user or system based on the provided request model. 
    /// </summary>
    /// <remarks>The response includes a header with the status code and message, and a data object containing
    /// the authentication response details.</remarks>
    /// <param name="requestModel">The request model containing the authentication details. This includes the request header and the body with
    /// authentication data.</param>
    /// <returns>An <see cref="IActionResult"/> containing a <see cref="ResponseModel"/> with the authentication result.  Returns
    /// a 200 OK response if the authentication is successful, or a 400 Bad Request response if the authentication
    /// fails.</returns>
    [HttpPost("Autenticacion/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(typeof(ResponseModel), 400)]
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
            Data = new
            {
                jsonResponse
            }
        };
        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Consultation of BTS remittance details, including sender and recipient information.
    /// Employs the model RequestModel with a body of type ConsultaBody for input and returns a ResponseModel containing the consultation results.
    /// </summary>
    /// <param name="requestModel">The request model containing the remittance details. This includes the request header and the body with
    /// remittance data.</param>
    /// <returns>An <see cref="IActionResult"/> containing a <see cref="ResponseModel"/> with the remittance result.  Returns
    /// a 200 OK response if the consultation is successful, or a 400 Bad Request response if the consultation
    /// fails.</returns>
    [HttpPost("Consulta/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(typeof(ResponseModel), 400)]
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
    /// Performs a payment transaction through the BTS system using the provided payment details.
    /// </summary>
    /// <param name="requestModel">The request model containing the Payment details. This includes the request header and the body with
    /// Payment data.</param>
    /// <returns>An <see cref="IActionResult"/> containing a <see cref="ResponseModel"/> with the payment result.  Returns
    /// a 200 OK response if the payment is successful, or a 400 Bad Request response if the payment
    /// fails.</returns>
    /// <remarks>Utilizes the IPagoService to process the payment request and obtain the response data and status code.</remarks>
    [HttpPost("Pago/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(typeof(ResponseModel), 400)]
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
    /// Performs a reversal transaction through the BTS system using the provided reversal details.
    /// </summary>
    /// <param name="requestModel">The request model containing the Reverso details. This includes the request header and the body with
    /// Reverso data.</param>
    /// <returns>An <see cref="IActionResult"/> containing a <see cref="ResponseModel"/> with the reversal result.  Returns
    /// a 200 OK response if the reversal is successful, or a 400 Bad Request response if the reversal
    /// fails.</returns>
    /// <remarks>Utilizes the IReversoService to process the reversal request and obtain the response data and status code.</remarks>
    [HttpPost("Reverso/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(typeof(ResponseModel), 400)]
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
    /// Performs a direct transaction confirmation through the BTS system using the provided confirmation details.
    /// </summary>
    /// <param name="requestModel">The request model containing the ConfirmacionTransaccionDirecta details. This includes the request header and the body with
    ///  confirmation data.</param>
    ///  <returns>An <see cref="IActionResult"/> containing a <see cref="ResponseModel"/> with the confirmation result.  Returns
    ///  a 200 OK response if the confirmation is successful, or a 400 Bad Request response if the confirmation
    ///  fails.</returns>
    ///  <remarks>Utilizes the IConfirmacionTransaccionDirecta to process the confirmation request and obtain the response data and status code.</remarks>
    [HttpPost("ConfirmacionTransaccionDirecta/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(typeof(ResponseModel), 400)]
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
    /// Performs a payment confirmation through the BTS system using the provided confirmation details.
    /// </summary>
    /// <param name="requestModel">The request model containing the ConfirmacionPago details. This includes the request header and the body with
    /// data.</param>
    /// <returns>An <see cref="IActionResult"/> containing a <see cref="ResponseModel"/> with the confirmation result.  Returns
    /// 200 OK response if the confirmation is successful, or a 400 Bad Request response if the confirmation
    /// fails.</returns>
    /// <remarks>Utilizes the IConfirmacionPago to process the confirmation request and obtain the response data and status code.</remarks>
    [HttpPost("ConfirmacionPago/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(typeof(ResponseModel), 400)]
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
    /// Performs a payment rejection through the BTS system using the provided rejection details.
    /// </summary>
    /// <param name="requestModel">The request model containing the RechazoPago details. This includes the request header and the body with
    ///  rejection data.</param>
    ///  <returns>An <see cref="IActionResult"/> containing a <see cref="ResponseModel"/> with the rejection result.  Returns
    ///  a 200 OK response if the rejection is successful, or a 400 Bad Request response if the rejection
    ///  fails.</returns>
    ///  <remarks>Utilizes the IRechazoPago to process the rejection request and obtain the response data and status code.</remarks>
    [HttpPost("RechazoPago/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(typeof(ResponseModel), 400)]
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
    /// Performs a reporting operation through the BTS system using the provided reporting details.
    /// </summary>
    /// <param name="requestModel">The request model containing the Reporteria details. This includes the request header and the body with
    /// reporting data.</param>
    /// <returns>An <see cref="IActionResult"/> containing a <see cref="ResponseModel"/> with the reporting result.  Returns
    /// a 200 OK response if the reporting is successful, or a 400 Bad Request response if the reporting
    /// fails.</returns>    
    [HttpPost("Reporteria/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(typeof(ResponseModel), 400)]
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
    /// Performs a SDEP reporting operation through the BTS system using the provided SDEP reporting details.
    /// </summary>
    /// <param name="requestModel">The request model containing the ReporteriaSDEP details. This includes the request header and the body with
    /// a SDEP reporting data.</param>
    ///  <returns>An <see cref="IActionResult"/> containing a <see cref="ResponseModel"/> with the SDEP reporting result.  Returns
    ///  a 200 OK response if the SDEP reporting is successful, or a 400 Bad Request response if the SDEP reporting
    ///  fails.</returns>
    ///  <remarks>Utilizes the ISEDPService to process the SDEP reporting request and obtain the response data and status code.</remarks>
    [HttpPost("ReporteriaSDEP/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(typeof(ResponseModel), 400)]
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
