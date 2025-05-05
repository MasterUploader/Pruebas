using System.ComponentModel.DataAnnotations;

namespace CAUAdministracion.Models
{
    /// <summary>
    /// Modelo que representa un mensaje de la tabla MANTMSG del AS400.
    /// </summary>
    public class MensajeModel
    {
        /// <summary>
        /// Código único del mensaje.
        /// </summary>
        public int Codigo { get; set; }

        /// <summary>
        /// Código del centro de costo (agencia).
        /// </summary>
        [Required(ErrorMessage = "Debe seleccionar una agencia.")]
        public string Codcco { get; set; }

        /// <summary>
        /// Nombre de la agencia. Solo para visualización.
        /// </summary>
        public string NombreAgencia { get; set; }

        /// <summary>
        /// Número de secuencia para orden.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Secuencia debe ser mayor a 0.")]
        public int Seq { get; set; }

        /// <summary>
        /// Contenido del mensaje.
        /// </summary>
        [Required(ErrorMessage = "Debe ingresar un mensaje.")]
        public string Mensaje { get; set; }

        /// <summary>
        /// Estado del mensaje: 'A' (Activo) o 'I' (Inactivo).
        /// </summary>
        public string Estado { get; set; }

        /// <summary>
        /// Propiedad para mostrar el texto legible del estado (Activo/Inactivo).
        /// </summary>
        public string EstadoDescripcion
        {
            get
            {
                return Estado == "A" ? "Activo" : "Inactivo";
            }
        }
    }
}





using CAUAdministracion.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CAUAdministracion.Services.Mensajes
{
    /// <summary>
    /// Interfaz que define las operaciones de mantenimiento para mensajes (MANTMSG).
    /// </summary>
    public interface IMensajeService
    {
        /// <summary>
        /// Obtiene la lista de agencias habilitadas para mensajes.
        /// </summary>
        List<SelectListItem> ObtenerAgenciasSelectList();

        /// <summary>
        /// Obtiene todos los mensajes filtrados por agencia si se especifica.
        /// </summary>
        Task<List<MensajeModel>> ObtenerMensajesAsync(string codcco = null);

        /// <summary>
        /// Actualiza un mensaje existente (secuencia, contenido, estado).
        /// </summary>
        bool ActualizarMensaje(MensajeModel mensaje);

        /// <summary>
        /// Elimina un mensaje por su código único.
        /// </summary>
        bool EliminarMensaje(int codMsg);

        /// <summary>
        /// Verifica si el mensaje tiene dependencias (si aplica).
        /// </summary>
        bool TieneDependencias(int codMsg);
    }
}
