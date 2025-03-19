using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Services
{
    /// <summary>
    /// Cliente para conexiones FTP/SFTP.
    /// </summary>
    public class FtpConnectionProvider : IFtpConnection
    {
        private readonly string _server;
        private readonly NetworkCredential _credentials;

        public FtpConnectionProvider(string server, string user, string password)
        {
            _server = server;
            _credentials = new NetworkCredential(user, password);
        }

        public async Task UploadFileAsync(string filePath, string destinationPath)
        {
            var request = (FtpWebRequest)WebRequest.Create($"{_server}/{destinationPath}");
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = _credentials;

            using var fileStream = File.OpenRead(filePath);
            using var requestStream = await request.GetRequestStreamAsync();
            await fileStream.CopyToAsync(requestStream);
        }

        public async Task DownloadFileAsync(string remotePath, string localPath)
        {
            var request = (FtpWebRequest)WebRequest.Create($"{_server}/{remotePath}");
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = _credentials;

            using var response = (FtpWebResponse)await request.GetResponseAsync();
            using var responseStream = response.GetResponseStream();
            using var fileStream = File.Create(localPath);
            await responseStream.CopyToAsync(fileStream);
        }
    }
}
