@using System.Security.Claims
@{
    ViewData["Title"] = "Inicio";
    var tipoClaim = User.Claims.FirstOrDefault(c => c.Type == "TipoUsuario")?.Value ?? "";

    string tipoUsuario = tipoClaim switch
    {
        "1" => "Marquesina, usuarios y branch teller.",
        "2" => "Videos en Agencias.",
        "3" => "Mensajes en Agencias",
        _ => "usuario"
    };
}

<div class="text-center">
    <h1 class="display-5">Servicio de Gestion de @tipoUsuario</h1>
</div>



<div class="container mt-5">
    <h2 class="text-center mb-4">Menú Principal</h2>
    <div class="row justify-content-center">

        @* Administración Total: solo visible para usuarios tipo 1 *@
        @if (tipoClaim == "1")
        {

            <!-- Administración Videos -->
            <div class="col-md-3 mb-4">
                <div class="card text-center shadow-sm animated-box">
                    <div class="card-body">
                        <img src="~/images/agregar_video.png" alt="Videos" class="img-fluid mb-2 icon-animation" style="max-height: 64px;" />
                        <h5 class="card-title">Administración Videos</h5>
                        <a href="@Url.Action("Agregar", "Videos")" class="btn btn-sm btn-outline-primary mt-2 w-100">Agregar</a>
                        <a href="@Url.Action("Index", "Videos")" class="btn btn-sm btn-outline-secondary mt-2 w-100">Mantenimiento</a>
                    </div>
                </div>
            </div>

            <!-- Administración Mensajes -->
            <div class="col-md-3 mb-4">
                <div class="card text-center shadow-sm animated-box">
                    <div class="card-body">
                        <img src="~/images/boton_mensajes.png" alt="Mensajes" class="img-fluid mb-2 icon-animation" style="max-height: 64px;" />
                        <h5 class="card-title">Administración Mensajes</h5>
                        <a href="@Url.Action("Agregar", "Messages")" class="btn btn-sm btn-outline-success mt-2 w-100">Agregar</a>
                        <a href="@Url.Action("Index", "Messages")" class="btn btn-sm btn-outline-secondary mt-2 w-100">Mantenimiento</a>
                    </div>
                </div>
            </div>
        }

        @* Administración Video: solo visible para usuarios tipo 2 *@
        @if (tipoClaim == "2")
        {
            <!-- Administración Mensajes -->
            <!-- Administración Videos -->
            <div class="col-md-3 mb-4">
                <div class="card text-center shadow-sm animated-box">
                    <div class="card-body">
                        <img src="~/images/agregar_video.png" alt="Videos" class="img-fluid mb-2 icon-animation" style="max-height: 64px;" />
                        <h5 class="card-title">Administración Videos</h5>
                        <a href="@Url.Action("Agregar", "Videos")" class="btn btn-sm btn-outline-primary mt-2 w-100">Agregar</a>
                        <a href="@Url.Action("Index", "Videos")" class="btn btn-sm btn-outline-secondary mt-2 w-100">Mantenimiento</a>
                    </div>
                </div>
            </div>


            @* Administración Mensajes: solo visible para usuarios tipo 3 *@
            @if (tipoClaim == "3")
            {
                <!-- Administración Mensajes -->
                <div class="col-md-3 mb-4">
                    <div class="card text-center shadow-sm animated-box">
                        <div class="card-body">
                            <img src="~/images/boton_mensajes.png" alt="Mensajes" class="img-fluid mb-2 icon-animation" style="max-height: 64px;" />
                            <h5 class="card-title">Administración Mensajes</h5>
                            <a href="@Url.Action("Agregar", "Messages")" class="btn btn-sm btn-outline-success mt-2 w-100">Agregar</a>
                            <a href="@Url.Action("Index", "Messages")" class="btn btn-sm btn-outline-secondary mt-2 w-100">Mantenimiento</a>
                        </div>
                    </div>
                </div>
            }
        }

    </div>
</div>
