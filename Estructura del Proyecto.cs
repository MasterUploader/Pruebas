using System;
using System.Security.Cryptography;
using System.Text;

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
        public static string Decrypt(string encrypted, string method, string? key = null)
        {
            if (string.IsNullOrWhiteSpace(encrypted))
                return string.Empty;

            if (!IsEncrypted(encrypted))
                return encrypted;

            return method.ToUpperInvariant() switch
            {
                "AES" => DecryptAES(encrypted, key ?? string.Empty),
                "BASE64" => DecryptBase64(encrypted),
                "MODIFICADO" => DecryptCustom(encrypted),
                _ => throw new NotSupportedException($"Método de desencriptación no soportado: {method}")
            };
        }

        /// <summary>
        /// Valida si el valor parece estar encriptado (por ejemplo, en formato Base64).
        /// </summary>
        private static bool IsEncrypted(string value)
        {
            try
            {
                var buffer = Convert.FromBase64String(value);
                return buffer.Length > 8;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Desencripta usando algoritmo AES con clave proporcionada.
        /// Requiere que la clave esté en Base64 y sea de 16, 24 o 32 bytes.
        /// </summary>
        public static string DecryptAES(string encrypted, string base64Key)
        {
            if (string.IsNullOrEmpty(base64Key))
                throw new ArgumentException("La clave AES es requerida para desencriptar.");

            byte[] key = Convert.FromBase64String(base64Key);
            byte[] iv = new byte[16]; // IV de 16 bytes (puede ser ajustado si lo defines externamente)

            byte[] cipherText = Convert.FromBase64String(encrypted);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] result = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

            return Encoding.UTF8.GetString(result);
        }

        /// <summary>
        /// Desencripta texto codificado en Base64 estándar.
        /// </summary>
        public static string DecryptBase64(string encrypted)
        {
            byte[] buffer = Convert.FromBase64String(encrypted);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Desencriptación modificada (implementa tu lógica heredada aquí).
        /// </summary>
        public static string DecryptCustom(string encrypted)
        {
            // Por ahora solo retorno el valor simulado. Reemplázalo con tu lógica personalizada real.
            return $"[MODIFICADO]: {encrypted}";
        }
    }
}
