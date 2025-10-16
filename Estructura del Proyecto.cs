using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json.Serialization;

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
public sealed class GuardarTransaccionesDto : IValidatableObject
{
    /// <summary> Cuenta asociada (enmascarada o completa según política). </summary>
    [StringLength(34)]
    public string? NumeroCuenta { get; set; }

    /// <summary> Importe debitado como <c>string</c>; se normaliza coma/punto. </summary>
    [StringLength(32)]
    public string? MontoDebitado { get; set; }

    /// <summary> Importe acreditado como <c>string</c>; se normaliza coma/punto. </summary>
    [StringLength(32)]
    public string? MontoAcreditado { get; set; }

    /// <summary> Código único del comercio. </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(32)]
    public string? CodigoComercio { get; set; }

    /// <summary> Nombre legible del comercio. </summary>
    [StringLength(128)]
    public string? NombreComercio { get; set; }

    /// <summary> Identificador de la terminal POS. </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(64)]
    public string? Terminal { get; set; }

    /// <summary> Descripción de la operación (JSON: <c>descripción</c>). </summary>
    [JsonPropertyName("descripción")]
    [StringLength(256)]
    public string? Descripcion { get; set; }

    /// <summary> Clasificación contable (p. ej. <c>DB</c>/<c>CR</c> o código interno). </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(8)]
    public string? NaturalezaContable { get; set; }

    /// <summary> Número de corte o batch (cierres/arqueos). </summary>
    [StringLength(32)]
    public string? NumeroDeCorte { get; set; }

    /// <summary> Identificador único de idempotencia. </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(64, MinimumLength = 8)]
    public string? IdTransaccionUnico { get; set; }

    /// <summary> Estado del proceso de negocio (APROBADA, PENDIENTE, RECHAZADA...). </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(32)]
    public string? Estado { get; set; }

    /// <summary> Descripción humana del estado (no sensible). </summary>
    [StringLength(256)]
    public string? DescripcionEstado { get; set; }

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
