// Controllers/VideosController.cs using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;

namespace SitiosIntranet.Web.Controllers { [Authorize] public class VideosController : Controller { public IActionResult Agregar() { return View(); } } }

// Views/Videos/Agregar.cshtml @{ ViewData["Title"] = "Agregar Video"; }

<h2 class="mb-4">Agregar nuevo video</h2><form method="post" enctype="multipart/form-data">
    <div class="mb-3">
        <label for="titulo" class="form-label">Título del video</label>
        <input type="text" id="titulo" name="titulo" class="form-control" required />
    </div><div class="mb-3">
    <label for="descripcion" class="form-label">Descripción</label>
    <textarea id="descripcion" name="descripcion" class="form-control" rows="4"></textarea>
</div>

<div class="mb-3">
    <label for="archivo" class="form-label">Archivo de video</label>
    <input type="file" id="archivo" name="archivo" class="form-control" accept="video/*" required />
</div>

<button type="submit" class="btn btn-primary">Subir Video</button>

</form>
