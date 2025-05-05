using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CAUAdministracion.Models.Video
{
    /// <summary>
    /// Modelo para cargar nuevos archivos de video.
    /// Usado en la vista Agregar.cshtml.
    /// </summary>
    public class VideoUploadModel
    {
        /// <summary>
        /// Archivo de video subido por el usuario.
        /// </summary>
        [Required(ErrorMessage = "Debe seleccionar un archivo de video.")]
        [Display(Name = "Archivo de Video")]
        public IFormFile Video { get; set; }

        /// <summary>
        /// Código de la agencia a la que pertenece el video.
        /// </summary>
        [Required(ErrorMessage = "Debe especificar el código de agencia.")]
        [Display(Name = "Código Agencia")]
        public string Codcco { get; set; }

        /// <summary>
        /// Estado del video (A = Activo, I = Inactivo).
        /// </summary>
        [Required(ErrorMessage = "Debe seleccionar un estado.")]
        [Display(Name = "Estado")]
        public string Estado { get; set; }
    }
}
