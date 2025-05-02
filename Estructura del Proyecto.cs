using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SitiosIntranet.Web.Services;

namespace SitiosIntranet.Web.Controllers
{
    [Authorize]
    public class VideosController : Controller
    {
        private readonly IVideoService _videoService;

        public VideosController(IVideoService videoService)
        {
            _videoService = videoService;
        }

        // GET: Mostrar formulario para subir video
        [HttpGet]
        public IActionResult Agregar()
        {
            return View();
        }

        // POST: Recibe el video, guarda el archivo en disco y lo registra en AS400
        [HttpPost]
        public async Task<IActionResult> Agregar(IFormFile archivo, string codcco, string estado)
        {
            if (archivo == null || archivo.Length == 0)
            {
                ModelState.AddModelError("archivo", "Debe seleccionar un archivo.");
                return View();
            }

            var nombreArchivo = Path.GetFileName(archivo.FileName);
            string rutaBase = @"C:\W1\"; // Ruta base configurable si se requiere

            // Guardar el archivo en disco
            bool guardadoOk = await _videoService.GuardarArchivoEnDisco(archivo, codcco, rutaBase, nombreArchivo);

            if (!guardadoOk)
            {
                ModelState.AddModelError("", "No se pudo guardar el archivo.");
                return View();
            }

            // Insertar el registro en AS400
            bool insertadoOk = _videoService.GuardarRegistroEnAs400(codcco, estado, nombreArchivo, rutaBase);

            if (!insertadoOk)
            {
                ModelState.AddModelError("", "No se pudo registrar en la base de datos.");
                return View();
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
