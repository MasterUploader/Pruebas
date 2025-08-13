Por ejemplo si tuviese un clase dto así

public class IniciarSesionDto
{
    [Required(ErrorMessage = "El Correo es Obligatorio.")]
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "La Contraseña es obligatoria.")]
    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;


}

Pero le agrego algo como:


[Library("Data"), TableName("USER")]
public class IniciarSesionDto
{
    [Required(ErrorMessage = "El Correo es Obligatorio.")]
    [JsonProperty("email")]
	[ParameterName("MAIL")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "La Contraseña es obligatoria.")]
    [JsonProperty("password")]
	[ParameterName("PASS")]
    public string Password { get; set; } = string.Empty;


}


O similar si lo quiero con detalle

[Library("Data"), TableName("USER")]
public class IniciarSesionDto
{
    [Required(ErrorMessage = "El Correo es Obligatorio.")]
    [JsonProperty("email")]
	[ParameterName("MAIL", Type.char, Size:50)]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "La Contraseña es obligatoria.")]
    [JsonProperty("password")]
	[ParameterName("PASS"), Type.decimal, Size:10, Precision:2]
    public string Password { get; set; } = string.Empty;


}

Es posible usarlo de tal manera que un select pueda ser:

{
    var q = new SelectQueryBuilder(IniciarSesionDto)
        .Select("*")
        .Build();
}

O tambien como:

{
    var q = new SelectQueryBuilder(IniciarSesionDto, x)
        .Select(x.Email, x.Password)
        .Build();
}

Si fuesen varias tablas a trabajar algo como:

var q = new SelectQueryBuilder([IniciarSesionDto, a], [objeto2, b])

Esto amplialo para no tener que escribir directamente el campo, sino seleccionarlo en la tabla.

Dime si es posible, aun no modifiques ni crees codigo
