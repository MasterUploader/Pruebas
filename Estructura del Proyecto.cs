Asi tengo el constructor:

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
[Route("v1/[controller]")]
[ApiController]
public class BtsController : ControllerBase
{
    private readonly IAuthenticateService _authenticateService;
    private readonly IConsultaService _consultaService;
    private readonly IPagoService _pagoService;
    private readonly IReversoService _reversoService;
    private readonly IConfirmacionTransaccionDirecta _confirmacionTransaccionDirecta;
    private readonly IConfirmacionPago _confirmacionPago;
    private readonly IRechazoPago _rechazoPago;
    private readonly IReporteriaService _reporteriaService;
    private readonly ISEDPService _sEDPService;


    /// <summary>
    /// Contructor de BTS Controller
    /// </summary>
    /// <param name="authenticateService">Instancia de Authenticate Service.</param>
    /// <param name="consultaService">Instancia de Consulta Service.</param>
    /// <param name="pagoService">Instancia de Pago Service.</param>
    /// <param name="reversoService">Instancia de Reverso Service.</param>
    /// <param name="confirmacionTransaccionDirecta">Instancia de Confirmación Transaccion Directa Service.</param>
    /// <param name="confirmacionPago">Instancia de Confirmación Pago Service.</param>
    /// <param name="rechazoPago">Instancia de Rechazo Pago Service.</param>
    /// <param name="reporteriaService">Instancia de Reporteria Service.</param>
    /// <param name="sEDPService">Instnacia de ISEDPService.</param>
    public BtsController(IAuthenticateService authenticateService, IConsultaService consultaService, IPagoService pagoService, 
        IReversoService reversoService, IConfirmacionTransaccionDirecta confirmacionTransaccionDirecta, IConfirmacionPago confirmacionPago, 
        IRechazoPago rechazoPago, IReporteriaService reporteriaService, ISEDPService sEDPService)
    {
        _authenticateService = authenticateService;
        _consultaService = consultaService;
        _pagoService = pagoService;
        _reversoService = reversoService;
        _confirmacionTransaccionDirecta = confirmacionTransaccionDirecta;
        _confirmacionPago = confirmacionPago;
        _rechazoPago = rechazoPago;
        _reporteriaService = reporteriaService;
        _sEDPService = sEDPService;
    }

    /// <summary>
    /// Enpoint Autenticación.
    /// </summary>
    /// <param name="requestModel"></param>
    /// <returns>Un Acción HTTP</returns>
    [HttpPost("Autenticacion/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AutenticacionBTS([FromBody] RequestModel<AuthenticateBody> requestModel)
    {
        var (jsonResponse, statusCode)  = await _authenticateService.AuthenticateServiceRequestAsync();

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
    /// Endpoint GetData
    /// </summary>
    /// <param name="requestModel"></param>
    /// <returns>Http Response</returns>
    [HttpPost("Consulta/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(400)]
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
    /// Endpoint GetData
    /// </summary>
    /// <param name="requestModel"></param>
    /// <returns>Http Response</returns>
    [HttpPost("Pago/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> PagoBTS([FromBody] RequestModel<PagoBody> requestModel)
    {
        var (jsonResponse, statusCode)  = await _pagoService.PagoServiceRequestAsync(requestModel.Body);

        var resp = new ResponseModel
        {
            Header = new ResponseHeader
            {
                RequestHeader = requestModel.Header,
                StatusCode = jsonResponse.OpCode,
                Message = jsonResponse.ProcessMsg
            },
            Data =  jsonResponse                
        };

        return statusCode == 200 ? Ok(resp) : BadRequest(resp);
    }

    /// <summary>
    /// Endpoint ReversoBTS
    /// </summary>
    /// <param name="requestModel"></param>
    /// <returns>Http Response</returns>
    [HttpPost("Reverso/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ReversoBTS([FromBody] RequestModel<ReversoBody> requestModel)
    {
        var (jsonResponse, statusCode)  = await _reversoService.ReversoServiceRequestAsync(requestModel.Body);

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
    /// Endpoint ConfirmacionTransaccionDirectaBTS "CDEP"
    /// </summary>
    /// <param name="requestModel">Objeto DTO de tipo RequestModel ->ConfirmacionTransaccionDirectaBody</param>
    /// <returns>Retorna un objeto dentro de una respuesta HTTP.</returns>
    [HttpPost("ConfirmacionTransaccionDirecta/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ConfirmacionTransaccionDirectaBTS([FromBody] RequestModel<ConfirmacionTransaccionDirectaBody> requestModel)
    {
        var (jsonResponse, statusCode)  = await _confirmacionTransaccionDirecta.ConfirmacionTransaccionDirectaServiceRequestAsync(requestModel.Body);

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
    /// Endpoint ConfirmacionPagoBTS "PAYC"
    /// </summary>
    /// <param name="requestModel">Objeto DTO de tipo RequestModel ->ConfirmacionTransaccionDirectaBody</param>
    /// <returns>Retorna un objeto dentro de una respuesta HTTP.</returns>
    [HttpPost("ConfirmacionPago/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ConfirmacionPagoBTS([FromBody] RequestModel<ConfirmacionPagoBody> requestModel)
    {
        var (jsonResponse, statusCode)  = await _confirmacionPago.ConfirmacionPagoServiceRequestAsync(requestModel.Body);

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
    /// Endpoint RechazoPagoBTS "PAYJ"
    /// </summary>
    /// <param name="requestModel">Objeto DTO de tipo RequestModel ->RechazoPagoBody</param>
    /// <returns>Retorna un objeto dentro de una respuesta HTTP.</returns>
    [HttpPost("RechazoPago/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(400)]
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
    /// Endpoint para Reporteria.
    /// </summary>
    /// <param name="requestModel">Objeto Dto de tipo Request Model -> Reporteria Body</param>
    /// <returns>Retorna un objeto dentro de una respuesta Http.</returns>
    [HttpPost("Reporteria/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(400)]
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
    /// Endpoint para Reporteria.
    /// </summary>
    /// <param name="requestModel">Objeto Dto de tipo Request Model -> Reporteria Body</param>
    /// <returns>Retorna un objeto dentro de una respuesta Http.</returns>
    [HttpPost("ReporteriaSDEP/")]
    [ProducesResponseType(typeof(ResponseModel), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ObtenerTransaccionesClienteBts([FromBody] RequestModel<SDEPBody> requestModel)
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
