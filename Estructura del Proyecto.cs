using System.ComponentModel.DataAnnotations;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Common;

/// <summary>
/// Resultado canónico de la operación para respuestas exitosas o de negocio.
/// Uniforma el <c>code</c> y el <c>message</c> para clientes móviles, pasarelas y terceros.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description><c>code</c> proviene de reglas de negocio (<c>BizCodes</c>), p. ej., "000".</description></item>
///   <item><description><c>message</c> es legible; evitar detalles internos o PII.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code language="json">
/// { "code": "000", "message": "Transacción registrada correctamente" }
/// </code>
/// </example>
public sealed class ApiResultDto
{
    /// <summary> Código de negocio estandarizado (p. ej., "000", "40001", etc.). </summary>
    [Required(AllowEmptyStrings = false)]
    public string? Code { get; set; }

    /// <summary> Mensaje humano breve y claro. </summary>
    [Required(AllowEmptyStrings = false)]
    public string? Message { get; set; }
}
