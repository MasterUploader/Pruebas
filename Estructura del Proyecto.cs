using System;
using System.Text;
using System.Security.Cryptography;

namespace SitiosIntranet.Web.Helpers
{
    /// <summary>
    /// Clase utilitaria para realizar operaciones generales como desencriptar cadenas.
    /// </summary>
    public static class OperacionesVarias
    {
        /// <summary>
        /// Método que desencripta una cadena encriptada.
        /// Aquí se utiliza una lógica simulada. Puedes reemplazarla por AES, TripleDES, etc.
        /// </summary>
        /// <param name="cadenaEncriptada">Texto encriptado recibido desde la base de datos.</param>
        /// <returns>Texto desencriptado.</returns>
        public static string DesencriptarCadena(string cadenaEncriptada)
        {
            // Esta implementación es solo simbólica. Debes reemplazar por tu algoritmo real de desencriptación.
            // Si usas AES, puede ser algo como esto:

            // Clave y vector de inicialización (ejemplo, deben ser seguros y coincidir con lo usado al encriptar)
            string clave = "claveSecreta1234"; // 16 caracteres para AES-128
            string iv = "vectorInicial1234";   // 16 caracteres también

            try
            {
                using Aes aesAlg = Aes.Create();
                aesAlg.Key = Encoding.UTF8.GetBytes(clave);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                byte[] buffer = Convert.FromBase64String(cadenaEncriptada);

                string resultado;
                using (var ms = new System.IO.MemoryStream(buffer))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new System.IO.StreamReader(cs))
                {
                    resultado = reader.ReadToEnd();
                }

                return resultado;
            }
            catch
            {
                // Si falla la desencriptación, devuelve la misma cadena o un error
                return string.Empty;
            }
        }
    }
}
