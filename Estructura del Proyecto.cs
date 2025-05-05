@switch (tipoUsuario)
{
    case "1": // Super usuario
        <div class="d-flex justify-content-center flex-wrap gap-4">
            <partial name="_MenuIcono" model="new MenuItem('/Videos', 'Administración Videos', 'boton_videos.png')" />
            <partial name="_MenuIcono" model="new MenuItem('/Mensajes', 'Administración Mensajes', 'boton_mensajes.png')" />
            <partial name="_MenuIcono" model="new MenuItem('/Configuracion', 'Configuración', 'configuracion.png')" />
        </div>
        break;

    case "2": // Solo videos
        <div class="d-flex justify-content-center">
            <partial name="_MenuIcono" model="new MenuItem('/Videos', 'Administración Videos', 'boton_videos.png')" />
        </div>
        break;

    case "3": // Solo mensajes
        <div class="d-flex justify-content-center">
            <partial name="_MenuIcono" model="new MenuItem('/Mensajes', 'Administración Mensajes', 'boton_mensajes.png')" />
        </div>
        break;
}
