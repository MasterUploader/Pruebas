[Authorize]
public class VideosController : Controller
{
    private readonly IVideoService _videoService;

    public VideosController(IVideoService videoService)
    {
        _videoService = videoService;
    }

    // ============= AGREGAR VIDEO =============
    [HttpGet]
    public IActionResult Agregar()
    {
        var agencias = _videoService.ObtenerAgenciasSelectList();
        ViewBag.Agencias = agencias;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Agregar(IFormFile archivo, string codcco, string estado)
    {
        if (archivo == null || archivo.Length == 0)
        {
            ModelState.AddModelError("archivo", "Debe seleccionar un archivo.");
            return View();
        }

        var nombreArchivo = Path.GetFileName(archivo.FileName);
        string rutaContenedorBase = ConnectionManagerHelper.GetConnectionSection("AS400")?["ContenedorVideos"];

        bool guardadoOk = await _videoService.GuardarArchivoEnDisco(archivo, codcco, rutaContenedorBase, nombreArchivo);
        if (!guardadoOk)
        {
            ViewBag.Mensaje = "No se pudo guardar el archivo.";
            ViewBag.MensajeTipo = "danger";
            return View();
        }

        bool insertadoOk = _videoService.GuardarRegistroEnAs400(codcco, estado, nombreArchivo, rutaContenedorBase);
        if (!insertadoOk)
        {
            ViewBag.Mensaje = "No se pudo registrar en la base de datos.";
            ViewBag.MensajeTipo = "danger";
            return View();
        }

        // Redirección con mensaje por TempData (si prefieres persistencia)
        TempData["Mensaje"] = "Video agregado correctamente.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction("Index");
    }

    // ============= INDEX / MANTENIMIENTO =============
    [HttpGet]
    public async Task<IActionResult> Index(string codcco)
    {
        var agencias = _videoService.ObtenerAgenciasSelectList();
        var videos = await _videoService.ObtenerListaVideosAsync();

        ViewBag.Agencias = agencias;
        ViewBag.CodccoSeleccionado = codcco;

        ViewBag.Mensaje = TempData["Mensaje"];
        ViewBag.MensajeTipo = TempData["MensajeTipo"];

        return View(videos);
    }

    [HttpPost]
    public IActionResult Actualizar(int codVideo, string codcco, string Estado, int Seq)
    {
        var video = new VideoModel
        {
            CodVideo = codVideo,
            Codcco = codcco,
            Estado = Estado,
            Seq = Seq
        };

        var actualizado = _videoService.ActualizarVideo(video);

        TempData["Mensaje"] = actualizado
            ? "Registro actualizado correctamente."
            : "Error al actualizar el registro.";
        TempData["MensajeTipo"] = actualizado ? "success" : "danger";

        return RedirectToAction("Index", new { codcco });
    }

    [HttpPost]
    public IActionResult Eliminar(int codVideo, string codcco)
    {
        if (_videoService.TieneDependencias(codcco, codVideo))
        {
            TempData["Mensaje"] = "No se puede eliminar el video porque tiene dependencias.";
            TempData["MensajeTipo"] = "warning";
            return RedirectToAction("Index", new { codcco });
        }

        var lista = _videoService.ListarVideos(codcco);
        var video = lista.FirstOrDefault(v => v.CodVideo == codVideo);

        if (video == null)
        {
            TempData["Mensaje"] = "El video no fue encontrado.";
            TempData["MensajeTipo"] = "warning";
            return RedirectToAction("Index", new { codcco });
        }

        var eliminadoDb = _videoService.EliminarVideo(codVideo, codcco);
        var eliminadoArchivo = _videoService.EliminarArchivoFisico(video.RutaFisica);

        TempData["Mensaje"] = (eliminadoDb && eliminadoArchivo)
            ? "Video eliminado correctamente."
            : "Error al eliminar el video.";
        TempData["MensajeTipo"] = (eliminadoDb && eliminadoArchivo) ? "success" : "danger";

        return RedirectToAction("Index", new { codcco });
    }
}



@model List<CAUAdministracion.Models.VideoModel>
@{
    ViewData["Title"] = "Mantenimiento de Videos";
}

<h2>@ViewData["Title"]</h2>

<!-- Filtro por agencia -->
<form method="get">
    <div class="row g-2 align-items-end">
        <div class="col-md-4">
            <label for="codcco" class="form-label">Seleccione Agencia:</label>
            <select id="codcco" name="codcco" class="form-select" required>
                <option value="">--Seleccione--</option>
                @foreach (var agencia in ViewBag.Agencias as List<SelectListItem>)
                {
                    <option value="@agencia.Value" @(agencia.Value == ViewBag.CodccoSeleccionado ? "selected" : "")>
                        @agencia.Text
                    </option>
                }
            </select>
        </div>
        <div class="col-auto">
            <button type="submit" class="btn btn-primary">Filtrar</button>
        </div>
    </div>
</form>

<hr />

<!-- Mensaje dinámico según tipo -->
@if (!string.IsNullOrEmpty(ViewBag.Mensaje))
{
    var tipo = ViewBag.MensajeTipo ?? "info";
    <div class="alert alert-@tipo">
        @ViewBag.Mensaje
    </div>
}

@if (Model != null && Model.Any())
{
    <table class="table table-bordered table-hover">
        <thead class="table-dark">
            <tr>
                <th>Agencia</th>
                <th>Código</th>
                <th>Nombre</th>
                <th>Ruta</th>
                <th>Estado</th>
                <th>Secuencia</th>
                <th>Acciones</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var video in Model)
            {
                <tr>
                    <form asp-action="Actualizar" method="post">
                        <td>@video.Codcco</td>
                        <td>@video.CodVideo</td>
                        <td>@video.Nombre</td>
                        <td>@video.Ruta</td>
                        <td>
                            <select name="Estado" class="form-select">
                                <option value="A" @(video.Estado == "A" ? "selected" : "")>Activo</option>
                                <option value="I" @(video.Estado == "I" ? "selected" : "")>Inactivo</option>
                            </select>
                        </td>
                        <td>
                            <input type="number" name="Seq" value="@video.Seq" class="form-control" />
                        </td>
                        <td>
                            <input type="hidden" name="codcco" value="@video.Codcco" />
                            <input type="hidden" name="codVideo" value="@video.CodVideo" />
                            <button type="submit" class="btn btn-sm btn-success me-1">Actualizar</button>
                            <form asp-action="Eliminar" method="post" class="d-inline">
                                <input type="hidden" name="codcco" value="@video.Codcco" />
                                <input type="hidden" name="codVideo" value="@video.CodVideo" />
                                <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                            </form>
                        </td>
                    </form>
                </tr>
            }
        </tbody>
    </table>
}
else
{
    <div class="alert alert-warning">No se encontraron videos para mostrar.</div>
}


@section Scripts {
    <script>
        setTimeout(function () {
            const alertBox = document.querySelector('.alert');
            if (alertBox) {
                alertBox.classList.add('fade');
                setTimeout(() => alertBox.remove(), 300); // opcional: remueve el nodo después de desvanecer
            }
        }, 5000);
    </script>
}
