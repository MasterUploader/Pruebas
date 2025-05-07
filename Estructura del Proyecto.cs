using System.ComponentModel.DataAnnotations;

public class AgenciaModel
{
    [Required(ErrorMessage = "El centro de costo es obligatorio")]
    [Range(1, 999, ErrorMessage = "Debe ingresar un número positivo de máximo 3 dígitos")]
    public int Codcco { get; set; }

    [Required(ErrorMessage = "El nombre de la agencia es obligatorio")]
    [MaxLength(40, ErrorMessage = "Máximo 40 caracteres")]
    public string NomAge { get; set; }

    [Required]
    [Range(1, 999, ErrorMessage = "Zona debe tener máximo 3 dígitos")]
    public int Zona { get; set; }

    [MaxLength(2)]
    public string Marquesina { get; set; }  // Almacena "SI"/"NO" en lugar de bool

    [MaxLength(2)]
    public string RstBranch { get; set; }  // Almacena "SI"/"NO" en lugar de bool

    [MaxLength(20)]
    public string IpSer { get; set; }

    [MaxLength(18)]
    public string NomSer { get; set; }

    [MaxLength(20)]
    public string NomBD { get; set; }

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
}


<div class="form-check">
    <input asp-for="MarqCheck" class="form-check-input" type="checkbox" />
    <label class="form-check-label" for="MarqCheck">Aplica Marquesina</label>
</div>

<div class="form-check">
    <input asp-for="RstCheck" class="form-check-input" type="checkbox" />
    <label class="form-check-label" for="RstCheck">Aplica Reset Branch</label>
</div>
