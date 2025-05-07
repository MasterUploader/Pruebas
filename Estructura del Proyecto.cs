using System.ComponentModel.DataAnnotations;

public class AgenciaModel
{
    [Required]
    [Range(1, 999, ErrorMessage = "Centro de Costo debe ser un número positivo de hasta 3 dígitos")]
    public int Codcco { get; set; }

    [Required]
    [MaxLength(40)]
    public string NomAge { get; set; }

    [Required]
    [MaxLength(18)]
    public string NomSer { get; set; }

    [Required]
    [MaxLength(20)]
    public string IpSer { get; set; }

    [Required]
    [MaxLength(20)]
    public string NomBD { get; set; }

    [Required]
    [Range(1, 999, ErrorMessage = "Zona debe ser un número positivo de hasta 3 dígitos")]
    public int Zona { get; set; }

    [Required]
    [MaxLength(2)]
    public string Marquesina { get; set; }

    [Required]
    [MaxLength(2)]
    public string RstBranch { get; set; }
}


<div class="mb-3">
    <label asp-for="Codcco" class="form-label"></label>
    <input asp-for="Codcco" class="form-control" maxlength="3" />
    <span asp-validation-for="Codcco" class="text-danger"></span>
</div>

<div class="mb-3">
    <label asp-for="NomAge" class="form-label"></label>
    <input asp-for="NomAge" class="form-control" maxlength="40" />
    <span asp-validation-for="NomAge" class="text-danger"></span>
</div>

<!-- Repite para NomSer, IpSer, NomBD, Zona, Marquesina, RstBranch con sus respectivos maxlength -->
