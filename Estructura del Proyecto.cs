@using System.Security.Claims
@{
    ViewData["Title"] = "Inicio";
    var tipoClaim = User.FindFirst("TipoUsuario")?.Value;
    string tipoUsuario = tipoClaim switch
    {
        "1" => "Administrador",
        "2" => "Agente",
        "3" => "Supervisor",
        _ => "usuario"
    };
}

<div class="text-center">
    <h1 class="display-5">Bienvenido al Servicio de Administración de @tipoUsuario</h1>
    <p class="lead">Desde aquí puedes acceder a los módulos disponibles según tu perfil.</p>
</div>
