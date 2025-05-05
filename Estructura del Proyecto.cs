<!-- Dentro de tu navbar, agrega esto en el área de navegación -->
<li class="nav-item dropdown">
    <a class="nav-link dropdown-toggle" href="#" id="menuMensajes" role="button" data-bs-toggle="dropdown" aria-expanded="false">
        Administración Mensajes
    </a>
    <ul class="dropdown-menu" aria-labelledby="menuMensajes">
        <li>
            <a class="dropdown-item" asp-controller="Messages" asp-action="Agregar">Agregar Mensajes</a>
        </li>
        <li>
            <a class="dropdown-item" asp-controller="Messages" asp-action="Index">Mantenimiento de Mensajes</a>
        </li>
    </ul>
</li>
