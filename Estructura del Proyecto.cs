using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System.IO;
using System.Threading.Tasks;

namespace MyApp.Security
{
    /// <summary>
    /// Servicio para generar JWT firmados con RSA256 descargando la llave privada desde una URL externa.
    /// </summary>
    public class RsaJwtTokenServiceBouncy
    {
        private readonly RsaSecurityKey _rsaKey;

        /// <summary>
        /// Inicializa el servicio descargando la clave privada desde una URL.
        /// </summary>
        /// <param name="baseUrl">URL base donde está alojado el archivo PEM (por ejemplo: http://166.178.5.76/ProcesosTDC/IpKey)</param>
        public RsaJwtTokenServiceBouncy(string baseUrl)
        {
            string urlLlave = $"{baseUrl.TrimEnd('/')}/private_rsa_pkcs1.pem";

            string pem = DescargarContenidoDesdeUrl(urlLlave).GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(pem))
                throw new Exception($"No se pudo obtener la llave privada desde: {urlLlave}");

            var rsa = ConvertirPemAPrivateRsa(pem);
            _rsaKey = new RsaSecurityKey(rsa);
        }

        /// <summary>
        /// Genera un JWT firmado con RSA256.
        /// </summary>
        public string GenerarToken(string issuer, string audience, int expiresInMinutes = 60)
        {
            var credentials = new SigningCredentials(_rsaKey, SecurityAlgorithms.RsaSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "usuario-demo"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("rol", "admin")
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Descarga contenido desde una URL (sincrónico para compatibilidad en constructor).
        /// </summary>
        private static async Task<string> DescargarContenidoDesdeUrl(string url)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error al descargar {url}: {(int)response.StatusCode} - {response.ReasonPhrase}");

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Convierte el contenido PEM (formato PKCS#1) en objeto RSA mediante BouncyCastle.
        /// </summary>
        private static RSA ConvertirPemAPrivateRsa(string pem)
        {
            using var reader = new StringReader(pem);
            var pemReader = new PemReader(reader);
            var keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();

            return DotNetUtilities.ToRSA((RsaPrivateCrtKeyParameters)keyPair.Private);
        }
    }
}
