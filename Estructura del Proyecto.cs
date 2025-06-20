using System;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace MyApp.Security
{
    /// <summary>
    /// Generador de JWT firmado con RS256 usando clave privada RSA en formato PKCS#1 (.pem).
    /// Carga las llaves desde una ruta externa al proyecto.
    /// </summary>
    public class RsaJwtTokenServiceBouncy
    {
        private readonly RsaSecurityKey _rsaKey;

        /// <summary>
        /// Inicializa el servicio cargando la clave privada desde un archivo PEM externo.
        /// </summary>
        /// <param name="externalKeysPath">Ruta absoluta de la carpeta que contiene el archivo 'private_rsa_pkcs1.pem'</param>
        public RsaJwtTokenServiceBouncy(string externalKeysPath)
        {
            if (string.IsNullOrWhiteSpace(externalKeysPath) || !Directory.Exists(externalKeysPath))
                throw new DirectoryNotFoundException($"Directorio de llaves no válido: {externalKeysPath}");

            var fullKeyPath = Path.Combine(externalKeysPath, "private_rsa_pkcs1.pem");

            if (!File.Exists(fullKeyPath))
                throw new FileNotFoundException("Archivo de clave privada no encontrado", fullKeyPath);

            var rsa = CargarRsaDesdePkcs1(fullKeyPath);
            _rsaKey = new RsaSecurityKey(rsa);
        }

        /// <summary>
        /// Genera un JWT firmado con la clave privada cargada.
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
        /// Carga el objeto RSA desde un archivo PEM en formato PKCS#1 usando BouncyCastle.
        /// </summary>
        private RSA CargarRsaDesdePkcs1(string pemFilePath)
        {
            using var reader = File.OpenText(pemFilePath);
            var pemReader = new PemReader(reader);
            var keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();

            return DotNetUtilities.ToRSA((RsaPrivateCrtKeyParameters)keyPair.Private);
        }
    }
}
