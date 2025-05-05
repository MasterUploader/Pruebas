<div class="container mt-5">
    <h2 class="text-center mb-4">Menú Principal</h2>
    <div class="row justify-content-center">

        <!-- Administración Videos -->
        <div class="col-md-3 mb-4">
            <div class="card text-center shadow-sm animated-box">
                <div class="card-body">
                    <img src="~/images/icons/video.png" alt="Videos" class="img-fluid mb-2 icon-animation" style="max-height: 64px;" />
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
                    <img src="~/images/icons/message.png" alt="Mensajes" class="img-fluid mb-2 icon-animation" style="max-height: 64px;" />
                    <h5 class="card-title">Administración Mensajes</h5>
                    <a href="@Url.Action("Agregar", "Messages")" class="btn btn-sm btn-outline-success mt-2 w-100">Agregar</a>
                    <a href="@Url.Action("Index", "Messages")" class="btn btn-sm btn-outline-secondary mt-2 w-100">Mantenimiento</a>
                </div>
            </div>
        </div>

    </div>
</div>


.animated-box {
    transition: transform 0.3s ease, box-shadow 0.3s ease;
}

.animated-box:hover {
    transform: translateY(-5px);
    box-shadow: 0 8px 16px rgba(0,0,0,0.2);
}

.icon-animation {
    transition: transform 0.3s ease;
}

.icon-animation:hover {
    transform: scale(1.1);
}
