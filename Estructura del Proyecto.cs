@using System.Security.Claims
@{
    var usuario = User.Identity?.Name;
    var tipoUsuario = User.Claims.FirstOrDefault(c => c.Type == "TipoUsuario")?.Value ?? "";
    var esAutenticado = User.Identity?.IsAuthenticated ?? false;
}
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>@ViewData["Title"] - Sitios Intranet</title>
    <link rel="stylesheet" href="~/css/bootstrap.min.css" />
</head>
<body>
    @if (esAutenticado)
    {
        <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
            <div class="container-fluid">
                <a class="navbar-brand" href="/Home/Index">Sitios Intranet</a>
                <div class="collapse navbar-collapse">
                    <ul class="navbar-nav me-auto">
                        @* Menú de configuración *@
                        @if (tipoUsuario == "Admin" || tipoUsuario == "Configuracion")
                        {
                            <li class="nav-item dropdown">
                                <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">Configuración</a>
                                <ul class="dropdown-menu">
                                    <li><a class="dropdown-item" href="/Configuracion/Usuarios">Administración General</a></li>
                                    <li><a class="dropdown-item" href="/Configuracion/Agencias">Administración de Agencias</a></li>
                                </ul>
                            </li>
                        }

                        @* Menú de mensajes *@
                        @if (tipoUsuario == "Admin" || tipoUsuario == "Mensajes")
                        {
                            <li class="nav-item">
                                <a class="nav-link" href="/Mensajes/Agregar">Agregar Mensajes</a>
                            </li>
                        }

                        @* Menú de videos *@
                        @if (tipoUsuario == "Admin" || tipoUsuario == "Videos")
                        {
                            <li class="nav-item">
                                <a class="nav-link" href="/Videos/Agregar">Agregar Videos</a>
                            </li>
                        }
                    </ul>
                    <ul class="navbar-nav ms-auto">
                        <li class="nav-item">
                            <span class="navbar-text text-white me-3">@usuario</span>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="/Account/Logout">Cerrar sesión</a>
                        </li>
                    </ul>
                </div>
            </div>
        </nav>
    }

    <div class="container mt-4">
        @RenderBody()
    </div>

    <script src="~/js/bootstrap.bundle.min.js"></script>
</body>
</html>
