using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CAUAdministracion.Services.Videos;
using Microsoft.AspNetCore.Http;
using RestUtilities.Connection; // Librería personalizada de conexión
using System.Threading.Tasks;
using System;

namespace CAUAdministracion.Controllers
{
    [Authorize]
    public class VideosController : Controller
    {
        private readonly IVideoService _videoService;

        public VideosController(IVideoService videoService)
        {
            _videoService = videoService;
        }

        /// <summary>
        /// GET: Muestra el formulario para subir un nuevo video.
        /// </summary>
        [HttpGet]
        public IActionResult Agregar()
        {
            return View();
        }

        /// <summary>
        /// POST: Recibe el archivo de video, lo guarda en disco y lo registra en AS400.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Agregar(IFormFile archivo, string codcco, string estado)
        {
            // Validación básica del archivo
            if (archivo == null || archivo.Length == 0)
            {
                ModelState.AddModelError("archivo", "Debe seleccionar un archivo.");
                return View();
            }

            // Obtener nombre del archivo y ruta base del archivo de configuración
            var nombreArchivo = Path.GetFileName(archivo.FileName);
            string rutaContenedorBase = GlobalConnection.ConnectionConfig.ContenedorVideos;

            // Paso 1: Guardar archivo en disco
            bool guardadoOk = await _videoService.GuardarArchivoEnDisco(archivo, codcco, rutaContenedorBase, nombreArchivo);
            if (!guardadoOk)
            {
                ModelState.AddModelError("", "No se pudo guardar el archivo.");
                return View();
            }

            // Paso 2: Registrar información en AS400
            bool insertadoOk = _videoService.GuardarRegistroEnAs400(codcco, estado, nombreArchivo, rutaContenedorBase);
            if (!insertadoOk)
            {
                ModelState.AddModelError("", "No se pudo registrar en la base de datos.");
                return View();
            }

            // Redirige al menú principal después de éxito
            return RedirectToAction("Index", "Home");
        }
    }
}
