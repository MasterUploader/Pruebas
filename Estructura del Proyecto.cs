using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.RegistraImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.RegistraImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using System;
using System.Net.Mime;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers;

/// <summary>
/// Controlador que expone endpoints para registrar la impresión de tarjetas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class RegistraImpresionController : ControllerBase
{
    private readonly IRegistraImpresionService _registraImpresionService;
    private readonly ILogger<RegistraImpresionController> _logger;
    private readonly ResponseHandler _responseHandler = new();

    /// <summary>
    /// Crea una nueva instancia del controlador.
    /// </summary>
    /// <param name="registraImpresionService">Servicio de dominio para registro de impresión.</param>
    /// <param name="logger">Logger para trazabilidad y diagnóstico.</param>
    public RegistraImpresionController(
        IRegistraImpresionService registraImpresionService,
        ILogger<RegistraImpresionController> logger)
    {
        _registraImpresionService = registraImpresionService;
        _logger = logger;
    }

    /// <summary>
    /// Registra la impresión de una tarjeta (fecha, hora y usuario de impresión).
    /// </summary>
    /// <param name="postRegistraImpresionDto">Datos necesarios para registrar la impresión.</param>
    /// <returns>Respuesta con el resultado del registro.</returns>
    /// <remarks>
    /// Devuelve:
    /// - <b>200</b> con estado <c>success</c> cuando el registro se actualiza correctamente.
    /// - <b>304</b> con estado <c>NotModified</c> cuando no se actualiza ningún registro.
    /// - <b>400</b> con estado <c>BadRequest</c> cuando la solicitud es inválida o ocurre un error controlado.
    /// </remarks>
    [HttpPost] // endpoint canónico: POST api/RegistraImpresion
    [HttpPost("RegistraImpresion")] // alias para compatibilidad: POST api/RegistraImpresion/RegistraImpresion
    [ProducesResponseType(typeof(PostRegistraImpresionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PostRegistraImpresionResponseDto), StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(PostRegistraImpresionResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegistraImpresion([FromBody] PostRegistraImpresionDto postRegistraImpresionDto)
    {
        // Validación de cuerpo nulo (aunque [ApiController] ya maneja ModelState, esto nos da mensaje consistente).
        if (postRegistraImpresionDto is null)
        {
            var bad = BuildResponse(false, "El cuerpo de la solicitud es nulo.", "400", "BadRequest");
            return _responseHandler.HandleResponse(bad, bad.Codigo.Status);
        }

        // Validación de modelo por data annotations.
        if (!ModelState.IsValid)
        {
            var msg = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var bad = BuildResponse(false, string.IsNullOrWhiteSpace(msg) ? "Solicitud inválida." : msg, "400", "BadRequest");
            return _responseHandler.HandleResponse(bad, bad.Codigo.Status);
        }

        try
        {
            _logger.LogInformation("Iniciando registro de impresión para tarjeta {Tarjeta} por usuario {Usuario}.",
                                   postRegistraImpresionDto.NumeroTarjeta, postRegistraImpresionDto.UsuarioICBS);

            var exito = await _registraImpresionService.RegistraImpresion(postRegistraImpresionDto);

            if (exito)
            {
                var ok = BuildResponse(true, "Se registró correctamente la impresión.", "200", "success");
                _logger.LogInformation("Registro de impresión exitoso para tarjeta {Tarjeta}.", postRegistraImpresionDto.NumeroTarjeta);
                return _responseHandler.HandleResponse(ok, ok.Codigo.Status);
            }
            else
            {
                // No se modificó ningún registro (p. ej., no encontró la tarjeta)
                var notMod = BuildResponse(false, "No se pudo ingresar fecha, hora y usuario de impresión.", "304", "NotModified");
                _logger.LogWarning("No se modificó ningún registro para tarjeta {Tarjeta}.", postRegistraImpresionDto.NumeroTarjeta);
                return _responseHandler.HandleResponse(notMod, notMod.Codigo.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar impresión para tarjeta {Tarjeta}.", postRegistraImpresionDto.NumeroTarjeta);

            var bad = BuildResponse(false, ex.Message, "400", "BadRequest");
            return _responseHandler.HandleResponse(bad, bad.Codigo.Status);
        }
    }

    /// <summary>
    /// Construye el DTO de respuesta con metadatos consistentes.
    /// </summary>
    private static PostRegistraImpresionResponseDto BuildResponse(bool exito, string mensaje, string codigo, string status)
    {
        return new PostRegistraImpresionResponseDto
        {
            ImpresionExitosa = exito,
            Codigo = new PostRegistraImpresionResponseDto.CodigoRespuesta
            {
                Message = mensaje,
                Error = codigo,
                Status = status,
                // Formato de hora legible; si prefieres UTC cambialo a DateTime.UtcNow
                TimeStamp = DateTime.Now.ToString("HH:mm:ss tt")
            }
        };
    }
}


