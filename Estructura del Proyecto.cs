@* Administración para tipo 1: acceso completo *@
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

@* Solo Videos para tipo 2 *@
@if (tipoClaim == "2")
{
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
}

@* Solo Mensajes para tipo 3 *@
@if (tipoClaim == "3")
{
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
