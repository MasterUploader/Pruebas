using System;
using System.Text;
using System.Security.Cryptography;

namespace RestUtilities.Helpers
{
    /// <summary>
    /// Proporciona métodos de desencriptación dinámicos según configuración.
    /// Soporta AES, Base64, Modificado y detección automática de texto plano.
    /// </summary>
    public static class EncryptionHelper
    {
        /// <summary>
        /// Desencripta un valor usando el método especificado, o lo devuelve si ya está en texto plano.
        /// </summary>
        /// <param name="encrypted">Texto posiblemente encriptado</param>
        /// <param name="method">Método a aplicar: AES, Base64, Modificado</param>
        public static string Decrypt(string encrypted, string method)
        {
            if (string.IsNullOrWhiteSpace(encrypted))
                return string.Empty;

            if (!IsEncrypted(encrypted))
                return encrypted;

            return method.ToUpperInvariant() switch
            {
                "AES" => DecryptAES(encrypted),
                "BASE64" => DecryptBase64(encrypted),
                "MODIFICADO" => DecryptCustom(encrypted),
                _ => throw new NotSupportedException($"Método de desencriptación no soportado: {method}")
            };
        }

        /// <summary>
        /// Intenta determinar si el valor está encriptado.
        /// Esta validación se puede ajustar según reglas de tu proyecto.
        /// </summary>
        private static bool IsEncrypted(string value)
        {
            // Ejemplo simple: validar si es Base64 válido y suficientemente largo
            try
            {
                var buffer = Convert.FromBase64String(value);
                return buffer.Length > 8; // puede ajustarse
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Desencripta un valor encriptado en AES (ajustable con clave real).
        /// </summary>
        private static string DecryptAES(string encrypted)
        {
            // Este método es un stub. Aquí iría la lógica real AES.
            return "[AES] Desencriptado: " + encrypted;
        }

        /// <summary>
        /// Desencripta un valor Base64 estándar.
        /// </summary>
        private static string DecryptBase64(string encrypted)
        {
            var bytes = Convert.FromBase64String(encrypted);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Desencriptación personalizada (modificada) según tu lógica heredada.
        /// </summary>
        private static string DecryptCustom(string encrypted)
        {
            // Aquí pondrás el algoritmo modificado real.
            return "[MOD] Desencriptado: " + encrypted;
        }
    }
}
