using CAUAdministracion.Services.Videos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Connections;
using Connections.Services;
using Connections.Helpers;
using CAUAdministracion.Models;

namespace CAUAdministracion.Controllers;

[Authorize]
public class VideosController : Controller
{
    private readonly IVideoService _videoService;

    public VideosController(IVideoService videoService)
    {
        _videoService = videoService;
    }

    // ===========================
    //   1. AGREGAR VIDEOS
    // ===========================
    [HttpGet]
    public IActionResult Agregar()
    {
        var agencias = _videoService.ObtenerAgenciasSelectList();
        ViewBag.Agencias = agencias;

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
        string rutaContenedorBase = ConnectionManagerHelper.GetConnectionSection("AS400")?["ContenedorVideos"];

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


    // ===========================
    //   2. MANTENIMIENTO VIDEOS
    // ===========================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var agencias =  _videoService.ObtenerAgenciasSelectList(); // o el método correcto
        var videos = await _videoService.ObtenerListaVideosAsync();

        ViewBag.Agencias = agencias; // importante
        return View(videos); // Model = lista de videos
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

        ViewBag.Mensaje = actualizado
            ? "Registro actualizado correctamente."
            : "Error al actualizar el registro.";

        return RedirectToAction("Index", new { codcco = codcco });
    }

    [HttpPost]
    public IActionResult Eliminar(int codVideo, string codcco)
    {
        // Validar dependencias
        if (_videoService.TieneDependencias(codcco, codVideo))
        {
            ViewBag.Mensaje = "No se puede eliminar el video porque tiene dependencias.";
            return RedirectToAction("Index", new { codcco = codcco });
        }

        var lista = _videoService.ListarVideos(codcco);
        var video = lista.FirstOrDefault(v => v.CodVideo == codVideo);

        if (video == null)
        {
            ViewBag.Mensaje = "El video no fue encontrado.";
            return RedirectToAction("Index", new { codcco = codcco });
        }

        var eliminadoDb = _videoService.EliminarVideo(codVideo, codcco);
        var eliminadoArchivo = _videoService.EliminarArchivoFisico(video.RutaFisica);

        ViewBag.Mensaje = eliminadoDb && eliminadoArchivo
            ? "Video eliminado correctamente."
            : "Error al eliminar el video.";

        return RedirectToAction("Index", new { codcco = codcco });
    }
}




using CAUAdministracion.Models;
using Connections.Helpers;
using Connections.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;

namespace CAUAdministracion.Services.Videos;
/// <summary>
/// Servicio para manejo de archivos y registros en la tabla MANTVIDEO del AS400.
/// Toda interacción con la base de datos se hace vía DbCommand.
/// </summary>
public class VideoService : IVideoService
{
    private readonly IDatabaseConnection _as400;
    private readonly IWebHostEnvironment _env;

    public VideoService(IDatabaseConnection as400, IWebHostEnvironment env)
    {
        _as400 = as400;
        _env = env;
    }

    /// <summary>
    /// Guarda el archivo recibido en el disco, detectando si la ruta es de red o local.
    /// Lanza errores controlados si no se puede escribir en la ruta especificada.
    /// </summary>
    /// <param name="archivo">Archivo subido (formulario)</param>
    /// <param name="codcco">Código de agencia</param>
    /// <param name="rutaBase">Ruta base (extraída de ConnectionData.json según el ambiente)</param>
    /// <param name="nombreArchivo">Nombre del archivo original</param>
    /// <returns>True si el archivo se guardó correctamente, false en caso contrario</returns>
    public async Task<bool> GuardarArchivoEnDisco(IFormFile archivo, string codcco, string rutaBase, string nombreArchivo)
    {
        try
        {
            // Asegura que la ruta base termine en backslash
            if (!rutaBase.EndsWith(Path.DirectorySeparatorChar))
                rutaBase += Path.DirectorySeparatorChar;

            // Ruta completa del archivo
            string rutaCompleta = Path.Combine(rutaBase, nombreArchivo);

            // Detectar si es ruta de red compartida (UNC) o local
            bool esRutaCompartida = rutaBase.StartsWith(@"\\");

            // Validación para rutas compartidas (no se deben crear)
            if (esRutaCompartida)
            {
                if (!Directory.Exists(rutaBase))
                    throw new DirectoryNotFoundException($"La ruta de red '{rutaBase}' no está disponible o no existe.");
            }
            else
            {
                // Crear ruta local si no existe (solo si no es red)
                if (!Directory.Exists(rutaBase))
                    Directory.CreateDirectory(rutaBase);
            }

            // Guardar el archivo físicamente en la ruta
            using var stream = new FileStream(rutaCompleta, FileMode.Create, FileAccess.Write);
            await archivo.CopyToAsync(stream);

            return true;
        }
        catch (DirectoryNotFoundException ex)
        {
            // Error específico por ruta no encontrada
          //  _logger?.LogError(ex, "Error de ruta al guardar el archivo en disco: {Ruta}", rutaBase);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permisos insuficientes en la ruta
          //  _logger?.LogError(ex, "Sin permisos para escribir en la ruta: {Ruta}", rutaBase);
            return false;
        }
        catch (Exception ex)
        {
            // Error general
          //  _logger?.LogError(ex, "Error inesperado al guardar el archivo en disco");
            return false;
        }
    }

    /// <summary>
    /// Guarda el registro del video en AS400 con la ruta formateada como C:\Vid{codcco}Marq
    /// </summary>
    /// <param name="codcco">Código de agencia</param>
    /// <param name="estado">Estado del video (A/I)</param>
    /// <param name="nombreArchivo">Nombre del archivo de video</param>
    /// <param name="rutaServidor">Ruta del servidor físico (no se usa en el insert, solo para guardar el archivo)</param>
    /// <returns>True si el registro se insertó correctamente, false si falló</returns>
    public bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaServidor)
    {
        try
        {
            // Abrir conexión a AS400 desde la librería RestUtilities.Connections
            _as400.Open();

            if (_as400.IsConnected())
            {
                // Obtener comando para ejecutar SQL
                using var command = _as400.GetDbCommand();

                //if (command.Connection.State != System.Data.ConnectionState.Open)
                //    command.Connection.Open();

                // Obtener nuevo ID para el video
                int codVideo = GetUltimoId(command);

                // Obtener secuencia correlativa
                int sec = GetSecuencia(command, codcco);

                // Construir la ruta en el formato que espera AS400: C:\Vid{codcco}Marq\
                string rutaAs400 = $"C:\\Vid{codcco}Marq\\";

                // Construir query de inserción SQL
                command.CommandText = $@"INSERT INTO BCAH96DTA.MANTVIDEO (CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ) 
                                      VALUES('{codcco}', {codVideo}, '{rutaAs400}', '{nombreArchivo}', '{estado}', {sec})";

                // Ejecutar inserción
                int result = command.ExecuteNonQuery();

                return result > 0;

            }
            return false;
        }
        catch (Exception ex)
        {
            // Loguear o manejar el error según tu sistema (puedes usar RestUtilities.Logging aquí si lo deseas)
            // Por ahora, devolvemos false controladamente
            return false;
        }
        finally
        {
            // Cerrar conexión correctamente
            _as400.Close();
        }
    }

    /// <summary>
    /// Obtiene la lista de agencias activas desde RSAGE01.
    /// </summary>
    public List<string> ObtenerAgencias(DbCommand command)
    {
        var agencias = new List<string>();
        try
        {
            command.CommandText = "SELECT CODCCO FROM BCAH96DTA.RSAGE01";

            using var reader = command.ExecuteReader();
            while (reader.Read())
                agencias.Add(reader["CODCCO"].ToString());

            reader.Close();
        }
        catch
        {

        }

        return agencias;
    }

    /// <summary>
    /// Obtiene las agencias en formato SelectListItem para desplegar en el formulario
    /// </summary>
    public List<SelectListItem> ObtenerAgenciasSelectList()
    {
        var agencias = new List<SelectListItem>();

        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();
            command.CommandText = "SELECT CODCCO, NOMAGE FROM BCAH96DTA.RSAGE01 ORDER BY NOMAGE";

            if (command.Connection.State == ConnectionState.Closed)
                command.Connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                agencias.Add(new SelectListItem
                {
                    Value = reader["CODCCO"].ToString(),
                    Text = reader["NOMAGE"].ToString()
                });
            }
        }
        catch (Exception ex)
        {
            // Puedes loguearlo si tienes un sistema de logging, por ahora solo devolvemos una opción informativa
            agencias.Clear();
            agencias.Add(new SelectListItem
            {
                Value = "",
                Text = "Error al obtener agencias: " + ex.Message
            });
        }

        return agencias;
    }

    /// <summary>
    /// Retorna el siguiente valor de CODVIDEO (MAX + 1)
    /// </summary>
    public int GetUltimoId(DbCommand command)
    {
        try
        {
            command.CommandText = "SELECT MAX(CODVIDEO) FROM BCAH96DTA.MANTVIDEO";
            var result = command.ExecuteScalar();

            return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
        }
        catch
        {
            return 1; //or defecto
        }

    }

    /// <summary>
    /// Retorna el siguiente valor de SEQ por agencia (MAX + 1)
    /// </summary>
    public int GetSecuencia(DbCommand command, string codcco)
    {
        try
        {
            command.CommandText = $"SELECT MAX(SEQ) FROM BCAH96DTA.MANTVIDEO WHERE CODCCO = '{codcco}'";
            var result = command.ExecuteScalar();

            return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
        }
        catch
        {
            return 1; //Por defecto
        }
    }

    /// <summary>
    /// Devuelve la ruta personalizada desde la tabla RSAGE01 según el código de agencia.
    /// </summary>
    private string GetRutaServer(string codcco)
    {
        try
        {
            using var command = _as400.GetDbCommand();
            command.CommandText = $"SELECT NOMSER FROM BCAH96DTA.RSAGE01 WHERE CODCCO = '{codcco}'";

            if (command.Connection.State == ConnectionState.Closed)
                command.Connection.Open();

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var ruta = reader["NONSER"]?.ToString();
                if (!string.IsNullOrWhiteSpace(ruta))
                    return $"\\\\{ruta}\\";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener ruta de servidor: {ex.Message}");
        }

        return null; // Si falla, retornará null y se usará ContenedorVideos
    }






    /// <summary>
    /// Obtiene la lista completa de videos registrados desde AS400.
    /// </summary>
    /// <returns>Lista de objetos VideoModel con la información cargada desde la tabla MANTVIDEO.</returns>
    public async Task<List<VideoModel>> ObtenerListaVideosAsync()
    {
        var videos = new List<VideoModel>();

        try
        {
            // Abrir conexión a AS400
            _as400.Open();

            // Crear comando SQL para obtener los registros de la tabla MANTVIDEO
            using var command = _as400.GetDbCommand();
            command.CommandText = @"
            SELECT CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ 
            FROM BCAH96DTA.MANTVIDEO
            ORDER BY CODCCO, SEQ";

            using var reader = await command.ExecuteReaderAsync();

            // Recorrer los resultados y mapear a objetos VideoModel
            while (await reader.ReadAsync())
            {
                var video = new VideoModel
                {
                    Codcco = reader["CODCCO"]?.ToString(),
                    CodVideo = Convert.ToInt32(reader["CODVIDEO"]),
                    Ruta = reader["RUTA"]?.ToString(),
                    Nombre = reader["NOMBRE"]?.ToString(),
                    Estado = reader["ESTADO"]?.ToString(),
                    Seq = Convert.ToInt32(reader["SEQ"])
                };

                videos.Add(video);
            }
        }
        catch (Exception ex)
        {
            // Manejo de errores controlado (puedes usar logging aquí)
            Console.WriteLine($"Error al obtener lista de videos: {ex.Message}");
        }
        finally
        {
            // Asegurar cierre de conexión
            _as400.Close();
        }

        return videos;
    }



    /// <summary>
    /// Lista los videos activos e inactivos de una agencia desde AS400.
    /// </summary>
    public List<VideoModel> ListarVideos(string codcco)
    {
        var lista = new List<VideoModel>();

        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            command.CommandText = $@"
                    SELECT CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ
                    FROM BCAH96DTA.MANTVIDEO
                    WHERE CODCCO = '{codcco}'
                    ORDER BY SEQ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new VideoModel
                {
                    Codcco = reader["CODCCO"].ToString(),
                    CodVideo = Convert.ToInt32(reader["CODVIDEO"]),
                    Ruta = reader["RUTA"].ToString(),
                    Nombre = reader["NOMBRE"].ToString(),
                    Estado = reader["ESTADO"].ToString(),
                    Seq = Convert.ToInt32(reader["SEQ"]),
                    RutaFisica = Path.Combine(ConnectionManagerHelper.GetConnectionSection("AS400")?["ContenedorVideos"], reader["NOMBRE"].ToString())
                });
            }
        }
        catch
        {
            // Manejo opcional con logger
        }
        finally
        {
            _as400.Close();
        }

        return lista;
    }

    /// <summary>
    /// Actualiza el estado y secuencia de un video.
    /// </summary>
    public bool ActualizarVideo(VideoModel video)
    {
        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            command.CommandText = $@"
                    UPDATE BCAH96DTA.MANTVIDEO
                    SET ESTADO = '{video.Estado}',
                        SEQ = {video.Seq}
                    WHERE CODCCO = '{video.Codcco}'
                      AND CODVIDEO = {video.CodVideo}";

            return command.ExecuteNonQuery() > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <summary>
    /// Elimina el registro del video en AS400.
    /// </summary>
    public bool EliminarVideo(int codVideo, string codcco)
    {
        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            command.CommandText = $@"
                    DELETE FROM BCAH96DTA.MANTVIDEO
                    WHERE CODCCO = '{codcco}'
                      AND CODVIDEO = {codVideo}";

            return command.ExecuteNonQuery() > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <summary>
    /// Verifica si existen dependencias del video antes de eliminarlo.
    /// </summary>
    public bool TieneDependencias(string codcco, int codVideo)
    {
        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            command.CommandText = $@"
                    SELECT COUNT(*)
                    FROM BCAH96DTA.OTRATABLA
                    WHERE CODCCO = '{codcco}'
                      AND CODVIDEO = {codVideo}";

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
        catch
        {
            return true; // Si hay error, asumimos que tiene dependencias para prevenir borrado
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <summary>
    /// Elimina el archivo físico del video del disco.
    /// </summary>
    public bool EliminarArchivoFisico(string rutaArchivo)
    {
        try
        {
            if (File.Exists(rutaArchivo))
            {
                File.Delete(rutaArchivo);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

}
