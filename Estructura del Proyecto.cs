using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Contrato común para cualquier conexión gestionada por la librería.
    /// Soporta tanto ejecución directa de comandos SQL como acceso por DbContext.
    /// </summary>
    public interface IDatabaseConnection : IDisposable
    {
        /// <summary>
        /// Abre la conexión si aún no está activa.
        /// </summary>
        void Open();

        /// <summary>
        /// Cierra la conexión y libera recursos.
        /// </summary>
        void Close();

        /// <summary>
        /// Verifica si la conexión está activa o accesible.
        /// </summary>
        /// <returns>True si está conectada, false si no lo está.</returns>
        bool IsConnected();

        /// <summary>
        /// Retorna una instancia del DbContext si se usa Entity Framework.
        /// </summary>
        /// <returns>Instancia de DbContext configurada.</returns>
        DbContext GetDbContext();

        /// <summary>
        /// Retorna una instancia de DbCommand para ejecutar SQL directo.
        /// </summary>
        /// <returns>Comando SQL nativo de la conexión (ej. OleDbCommand).</returns>
        DbCommand GetDbCommand();
    }
}
