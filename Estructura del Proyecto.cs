using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CAUAdministracion.Models.Video
{
    /// <summary>
    /// Representa un registro de video en el sistema.
    /// Usado para listar, editar y eliminar videos desde AS400.
    /// </summary>
    public class VideoModel
    {
        /// <summary>
        /// Código de agencia asociada al video (ej. 104)
        /// </summary>
        [DisplayName("Agencia")]
        public string Codcco { get; set; }

        /// <summary>
        /// Código único del video en AS400 (identificador primario)
        /// </summary>
        [DisplayName("ID Video")]
        public int CodVideo { get; set; }

        /// <summary>
        /// Ruta registrada en AS400 (no se usa para guardar archivo)
        /// </summary>
        [DisplayName("Ruta AS400")]
        public string Ruta { get; set; }

        /// <summary>
        /// Nombre del archivo físico de video (ej. video.mp4)
        /// </summary>
        [DisplayName("Nombre Archivo")]
        public string Nombre { get; set; }

        /// <summary>
        /// Estado del video (A = Activo, I = Inactivo)
        /// </summary>
        [DisplayName("Estado")]
        public string Estado { get; set; }

        /// <summary>
        /// Número de secuencia dentro de la agencia
        /// </summary>
        [DisplayName("Secuencia")]
        public int Seq { get; set; }

        /// <summary>
        /// Ruta física completa (solo para mostrar o eliminar)
        /// </summary>
        public string RutaFisica { get; set; }
    }
}
