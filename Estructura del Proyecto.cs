@switch (tipoUsuario)
{
    case "1": // Super usuario
    {
        var item1 = new CAUAdministracion.Models.MenuItem("/Mensajes", "Mensajes", "boton_mensajes.png");
        var item2 = new CAUAdministracion.Models.MenuItem("/Videos", "Videos", "boton_videos.png");
        var item3 = new CAUAdministracion.Models.MenuItem("/Configuracion", "Configuración", "configuracion.png");

        <partial name="_MenuIcono" model="item1" />
        <partial name="_MenuIcono" model="item2" />
        <partial name="_MenuIcono" model="item3" />
        break;
    }

    case "2": // Solo videos
    {
        var item = new CAUAdministracion.Models.MenuItem("/Videos", "Videos", "boton_videos.png");
        <partial name="_MenuIcono" model="item" />
        break;
    }

    case "3": // Solo mensajes
    {
        var item = new CAUAdministracion.Models.MenuItem("/Mensajes", "Mensajes", "boton_mensajes.png");
        <partial name="_MenuIcono" model="item" />
        break;
    }
}
