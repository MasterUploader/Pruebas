Pagos_Davivienda_TNP/
├─ Controllers/
│  └─ PagosDaviviendaTnpController.cs
├─ Services/
│  ├─ Interfaces/
│  │  └─ IPaymentAuthorizationService.cs
│  └─ PaymentAuthorizationService.cs
├─ Models/
│  └─ Dtos/
│     └─ GetAuthorizationManual/
│        ├─ RequestGetauthorizationManual.cs
│        ├─ ResponseAuthorizationManualDto.cs
│        └─ ResponseEnvelope.cs
├─ Validation/
│  ├─ LuhnAttribute.cs
│  └─ AmountStringAttribute.cs
├─ Filters/
│  └─ ModelStateToErrorResponseFilter.cs
├─ Middleware/
│  └─ ExceptionHandlingMiddleware.cs
├─ Utils/
│  ├─ CardMasker.cs
│  └─ TimeUtil.cs
└─ Program.cs




using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Pagos_Davivienda_TNP.Validation;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Modelo raíz para la solicitud de autorización manual.
/// Respeta la forma:
/// {
///   "GetAuthorizationManual": { ... }
/// }
/// </summary>
public class RequestGetauthorizationManual
{
    /// <summary>Objeto con los parámetros de autorización manual.</summary>
    [JsonProperty("GetAuthorizationManual")]
    [Required]
    public GetauthorizationManualDto GetAuthorizationManual { get; set; } = new();
}

/// <summary>
/// Parámetros de entrada para autorización manual.
/// </summary>
public class GetauthorizationManualDto
{
    /// <summary>Identificador del comercio.</summary>
    [JsonRequired]
    [JsonProperty("pMerchantID")]
    [Required, StringLength(32, MinimumLength = 1)]
    public string PMerchantID { get; set; } = string.Empty;

    /// <summary>Identificador del terminal.</summary>
    [JsonRequired]
    [JsonProperty("pTerminalID")]
    [Required, StringLength(32, MinimumLength = 1)]
    public string PTerminalID { get; set; } = string.Empty;

    /// <summary>Número de tarjeta (PAN). Se valida Luhn.</summary>
    [JsonRequired]
    [JsonProperty("pPrimaryAccountNumber")]
    [Required, StringLength(19, MinimumLength = 12)]
    [Luhn] // Validación Luhn personalizada
    public string PPrimaryAccountNumber { get; set; } = string.Empty;

    /// <summary>Fecha de expiración de la tarjeta en formato MMAA.</summary>
    [JsonRequired]
    [JsonProperty("pDateExpiration")]
    [Required]
    [RegularExpression(@"^(0[1-9]|1[0-2])\d{2}$", ErrorMessage = "Formato MMAA inválido.")]
    public string PDateExpiration { get; set; } = string.Empty;

    /// <summary>CVV2 (3 o 4 dígitos).</summary>
    [JsonRequired]
    [JsonProperty("pCVV2")]
    [Required]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV2 inválido.")]
    public string PCVV2 { get; set; } = string.Empty;

    /// <summary>Monto en centavos/centésimos como string numérico positivo (ej. '10000' = 100.00).</summary>
    [JsonRequired]
    [JsonProperty("pAmount")]
    [Required, AmountString] // Validación numérica positiva
    public string PAmount { get; set; } = string.Empty;

    /// <summary>Número de traza (STAN) de 6 dígitos.</summary>
    [JsonRequired]
    [JsonProperty("pSystemsTraceAuditNumber")]
    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "pSystemsTraceAuditNumber debe tener 6 dígitos.")]
    public string PSystemsTraceAuditNumber { get; set; } = string.Empty;
}




using Newtonsoft.Json;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Datos del resultado de autorización manual.
/// </summary>
public class ResponseAuthorizationManualDto
{
    /// <summary>Código de respuesta (00 = aprobada).</summary>
    [JsonProperty("responseCode")]
    public string ResponseCode { get; set; } = string.Empty;

    /// <summary>Código de autorización si aplica.</summary>
    [JsonProperty("authorizationCode")]
    public string AuthorizationCode { get; set; } = string.Empty;

    /// <summary>Identificador único de la transacción.</summary>
    [JsonProperty("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Mensaje legible.</summary>
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Marca de tiempo ISO 8601.</summary>
    [JsonProperty("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}



using Newtonsoft.Json;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Envoltura para cumplir el contrato:
/// {
///   "GetAuthorizationManualResponse": {
///     "GetAuthorizationManualResult": { ... }
///   }
/// }
/// </summary>
public class GetAuthorizationManualResultEnvelope
{
    [JsonProperty("GetAuthorizationManualResponse")]
    public GetAuthorizationManualResponseContainer Response { get; set; } = new();
}

public class GetAuthorizationManualResponseContainer
{
    [JsonProperty("GetAuthorizationManualResult")]
    public ResponseAuthorizationManualDto Result { get; set; } = new();
}



using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Pagos_Davivienda_TNP.Validation;

/// <summary>Valida PAN por algoritmo de Luhn.</summary>
public sealed class LuhnAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var pan = value as string;
        if (string.IsNullOrWhiteSpace(pan)) return new ValidationResult("PAN requerido.");

        if (!pan.All(char.IsDigit)) return new ValidationResult("PAN debe ser numérico.");

        int sum = 0;
        bool alt = false;
        for (int i = pan.Length - 1; i >= 0; i--)
        {
            int n = pan[i] - '0';
            if (alt)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alt = !alt;
        }
        return (sum % 10 == 0) ? ValidationResult.Success : new ValidationResult("PAN no supera validación Luhn.");
    }
}



using System.ComponentModel.DataAnnotations;

namespace Pagos_Davivienda_TNP.Validation;

/// <summary>
/// Valida que el monto sea un string numérico positivo sin signo ni separadores, p.ej. "10000".
/// </summary>
public sealed class AmountStringAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return new ValidationResult("Monto requerido.");
        foreach (var c in s)
        {
            if (c < '0' || c > '9') return new ValidationResult("Monto debe ser numérico positivo en formato string.");
        }
        if (s.TrimStart('0').Length == 0) return new ValidationResult("Monto debe ser mayor a cero.");
        return ValidationResult.Success;
    }
}



namespace Pagos_Davivienda_TNP.Utils;

/// <summary>Utilidades para ofuscación de datos sensibles.</summary>
public static class CardMasker
{
    /// <summary>Ej.: 411111******1111</summary>
    public static string MaskPan(string pan)
    {
        if (string.IsNullOrEmpty(pan) || pan.Length < 10) return "************";
        var prefix = pan[..6];
        var suffix = pan[^4..];
        return $"{prefix}{new string('*', pan.Length - 10)}{suffix}";
    }
}


using System;

namespace Pagos_Davivienda_TNP.Utils;

public static class TimeUtil
{
    public static long ToUnixMillis(DateTime utc) => new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    public static string IsoNowUtc() => DateTime.UtcNow.ToString("O"); // ISO 8601
}




using System.Threading;
using System.Threading.Tasks;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

namespace Pagos_Davivienda_TNP.Services.Interfaces;

/// <summary>Contrato del servicio de autorización.</summary>
public interface IPaymentAuthorizationService
{
    Task<ResponseAuthorizationManualDto> AuthorizeManualAsync(GetauthorizationManualDto request, CancellationToken ct = default);
}



using System;
using System.Threading;
using System.Threading.Tasks;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Services;

/// <summary>
/// Implementación demo. Sustituir por integración real.
/// </summary>
public class PaymentAuthorizationService : Interfaces.IPaymentAuthorizationService
{
    public Task<ResponseAuthorizationManualDto> AuthorizeManualAsync(GetauthorizationManualDto request, CancellationToken ct = default)
    {
        // TODO: Llamar a conector DaviviendaTNP aquí. Manejar timeouts/reintentos.
        var txnId = $"TXN-{Guid.NewGuid():N}".ToUpperInvariant();
        var auth = new ResponseAuthorizationManualDto
        {
            ResponseCode = "00",
            AuthorizationCode = "123456",
            TransactionId = txnId,
            Message = "Transacción aprobada",
            Timestamp = TimeUtil.IsoNowUtc()
        };
        return Task.FromResult(auth);
    }
}



using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Filters;

/// <summary>
/// Convierte validaciones fallidas (400) al formato de error requerido.
/// </summary>
public sealed class ModelStateToErrorResponseFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var firstError = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .FirstOrDefault()?.ErrorMessage ?? "Solicitud inválida.";

            var payload = new
            {
                error = firstError,
                status = 400,
                timestamp = TimeUtil.ToUnixMillis(DateTime.UtcNow)
            };

            context.Result = new BadRequestObjectResult(payload);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}



using System.Net;
using System.Text.Json;
using Pagos_Davivienda_TNP.Utils;

namespace Pagos_Davivienda_TNP.Middleware;

/// <summary>
/// Manejo global de excepciones → formato de error estándar.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            // Timeout/cancelaciones → 504
            await WriteError(context, HttpStatusCode.GatewayTimeout, "La operación fue cancelada por timeout.");
        }
        catch (Exception ex)
        {
            // Nunca exponer detalles sensibles en producción
            await WriteError(context, HttpStatusCode.InternalServerError, "Error interno del servidor.");
            // TODO: Log interno con detalles (sin PAN/CVV2).
        }
    }

    private static Task WriteError(HttpContext ctx, HttpStatusCode code, string message)
    {
        ctx.Response.StatusCode = (int)code;
        ctx.Response.ContentType = "application/json";

        var payload = new
        {
            error = message,
            status = (int)code,
            timestamp = TimeUtil.ToUnixMillis(DateTime.UtcNow)
        };

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}




using Microsoft.AspNetCore.Mvc;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;
using Pagos_Davivienda_TNP.Services.Interfaces;

namespace Pagos_Davivienda_TNP.Controllers;

/// <summary>
/// API de Pagos DaviviendaTNP (v1).
/// Base URL: /davivienda-tnp/api/v1
/// </summary>
[ApiController]
[Route("davivienda-tnp/api/v1")]
[Produces("application/json")]
public class PagosDaviviendaTnpController : ControllerBase
{
    private readonly IPaymentAuthorizationService _paymentService;

    public PagosDaviviendaTnpController(IPaymentAuthorizationService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>Verifica salud del servicio.</summary>
    /// <returns>Estado UP y nombre de servicio.</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        var response = new
        {
            status = "UP",
            service = "DaviviendaTNP Payment API"
        };
        return Ok(response);
    }

    /// <summary>Procesa una autorización manual.</summary>
    /// <param name="request">Payload de autorización manual.</param>
    /// <returns>Envoltura GetAuthorizationManualResponse/GetAuthorizationManualResult.</returns>
    [HttpPost("authorization/manual")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(GetAuthorizationManualResultEnvelope), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuthorizationManual([FromBody] RequestGetauthorizationManual request, CancellationToken ct)
    {
        // Validación de modelo: el filtro global convertirá errores a formato {error,status,timestamp}.
        var dto = request.GetAuthorizationManual;

        // Llamada al dominio/servicio (simulado).
        var result = await _paymentService.AuthorizeManualAsync(dto, ct);

        // Envolver según contrato del documento.
        var envelope = new GetAuthorizationManualResultEnvelope
        {
            Response = new GetAuthorizationManualResponseContainer
            {
                Result = result
            }
        };

        return Ok(envelope);
    }
}



using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using Pagos_Davivienda_TNP.Filters;
using Pagos_Davivienda_TNP.Middleware;
using Pagos_Davivienda_TNP.Services;
using Pagos_Davivienda_TNP.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Newtonsoft.Json (para respetar tus atributos)
builder.Services
    .AddControllers(options =>
    {
        // Usaremos nuestro filtro para uniformar errores 400
        options.Filters.Add<ModelStateToErrorResponseFilter>();
    })
    .AddNewtonsoftJson(o =>
    {
        // Mantener nombres exactos según JsonProperty
        o.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new DefaultNamingStrategy()
        };
        o.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });

// Desactivar el filtro automático de ModelState para usar el nuestro
builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.SuppressModelStateInvalidFilter = true;
});

// DI servicios
builder.Services.AddScoped<IPaymentAuthorizationService, PaymentAuthorizationService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API de Pagos DaviviendaTNP",
        Version = "v1",
        Description = "API REST ligera para procesar autorizaciones de pago."
    });
});

var app = builder.Build();

// Middleware de excepciones → {error,status,timestamp}
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRouting();

// HTTPS recomendado en prod (agrega UseHttpsRedirection si corresponde)
app.UseAuthorization();

app.MapControllers();

// Swagger en DEV/UAT
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DaviviendaTNP v1");
});

app.Run();


































