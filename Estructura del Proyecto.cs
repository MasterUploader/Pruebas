using System;
using System.Threading.Tasks;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Define los métodos estándar para conexiones FTP/SFTP.
    /// </summary>
    public interface IFtpConnection : IDisposable
    {
        /// <summary>
        /// Sube un archivo al servidor FTP.
        /// </summary>
        /// <param name="filePath">Ruta del archivo local.</param>
        /// <param name="destinationPath">Ruta en el servidor.</param>
        Task UploadFileAsync(string filePath, string destinationPath);

        /// <summary>
        /// Descarga un archivo del servidor FTP.
        /// </summary>
        /// <param name="remotePath">Ruta en el servidor.</param>
        /// <param name="localPath">Ruta donde se guardará el archivo.</param>
        Task DownloadFileAsync(string remotePath, string localPath);
    }
}
