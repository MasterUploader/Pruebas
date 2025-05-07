using System.ComponentModel.DataAnnotations;

namespace CAUAdministracion.Models;

/// <summary>
/// Modelo que representa una agencia para gestión desde AS400.
/// </summary>
public class AgenciaModel
{
    /// <summary>
    /// Código de centro de costo (identificador único).
    /// </summary>
    [Required]
    [Range(0, 999, ErrorMessage = "Centro de Costo debe ser un número positivo de hasta 3 dígitos")]
    public int Codcco { get; set; }

    /// <summary>
    /// Nombre de la agencia.
    /// </summary>
    [Required]
    [MaxLength(40)]
    public string NomAge { get; set; } = string.Empty;

    /// <summary>
    /// Zona geográfica a la que pertenece la agencia (1: CENTRO SUR, 2: NOR OCCIDENTE, 3: NOR ORIENTE).
    /// </summary>
    [Required]
    [Range(1, 3, ErrorMessage = "Zona Debe 1: CENTRO SUR, 2: NOR OCCIDENTE, 3: NOR ORIENTE")]
    public int Zona { get; set; }

    /// <summary>
    /// Indica si aplica marquesina ("SI" o "NO").
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string Marquesina { get; set; } = string.Empty;


    // ✅ Propiedades auxiliares para el checkbox en Razor
    public bool MarqCheck
    {
        get => Marquesina == "SI";
        set => Marquesina = value ? "SI" : "NO";
    }

    public bool RstCheck
    {
        get => RstBranch == "SI";
        set => RstBranch = value ? "SI" : "NO";
    }

    /// <summary>
    /// Indica si aplica reinicio de Branch ("SI" o "NO").
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string RstBranch { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del servidor configurado para la agencia.
    /// </summary>
   // [Required]
    [MaxLength(18)]
    public string NomSer { get; set; } = string.Empty;

    /// <summary>
    /// Dirección IP del servidor configurado para la agencia.
    /// </summary>
   // [Required]
    [MaxLength(20)]
    public string IpSer { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la base de datos asociada.
    /// </summary>
   // [Required]
    [MaxLength(20)]
    public string NomBD { get; set; } = string.Empty;
}
