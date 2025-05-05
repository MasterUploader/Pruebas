@{
    ViewData["Title"] = "Menú Principal";
    var tipoUsuario = ViewData["TipoUsuario"]?.ToString();
}

<div class="d-flex justify-content-between align-items-center bg-primary text-white p-3 rounded">
    <h2 class="mb-0">Menú Principal</h2>

    <div>
        @switch (tipoUsuario)
        {
            case "1":
                <span class="me-3">Perfil: Super Usuario</span>
                break;
            case "2":
                <span class="me-3">Perfil: Solo Videos</span>
                break;
            case "3":
                <span class="me-3">Perfil: Solo Mensajes</span>
                break;
        }

        <a asp-controller="Account" asp-action="Logout" class="btn btn-light">Cerrar Sesión</a>
    </div>
</div>

<!-- Contenedor centrado -->
<div class="d-flex justify-content-center align-items-center" style="height: 70vh;">
    <div class="text-center">
        <div class="row row-cols-1 row-cols-md-3 g-4 justify-content-center">
            @switch (tipoUsuario)
            {
                case "1": // Super usuario
                    <partial name="_MenuIcono" model="new MenuItem("/Mensajes", "Mensajes", "boton_mensajes.png")" />
                    <partial name="_MenuIcono" model="new MenuItem("/Videos", "Videos", "boton_videos.png")" />
                    <partial name="_MenuIcono" model="new MenuItem("/Configuracion", "Configuración", "configuracion.png")" />
                    break;

                case "2": // Solo videos
                    <partial name="_MenuIcono" model="new MenuItem("/Videos", "Videos", "boton_videos.png")" />
                    break;

                case "3": // Solo mensajes
                    <partial name="_MenuIcono" model="new MenuItem("/Mensajes", "Mensajes", "boton_mensajes.png")" />
                    break;
            }
        </div>
    </div>
</div>




@model MenuItem

<div class="col text-center">
    <a href="@Model.Url" class="text-decoration-none">
        <img src="~/Images/@Model.Icono" alt="@Model.Titulo" class="img-fluid rounded shadow" style="max-width: 150px;" />
        <h5 class="mt-2 text-dark">@Model.Titulo</h5>
    </a>
</div>



public class MenuItem
{
    public string Url { get; set; }
    public string Titulo { get; set; }
    public string Icono { get; set; }

    public MenuItem(string url, string titulo, string icono)
    {
        Url = url;
        Titulo = titulo;
        Icono = icono;
    }
}
