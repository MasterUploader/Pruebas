using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

public class TokenValidator
{
    public ClaimsPrincipal? ValidarToken(string token, string publicKeyPath)
    {
        if (!File.Exists(publicKeyPath))
        {
            Console.WriteLine("❌ Archivo de clave pública no encontrado.");
            return null;
        }

        string publicKeyPem = File.ReadAllText(publicKeyPath);
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(publicKeyPem.ToCharArray());
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Error al importar la clave pública: {e.Message}");
            return null;
        }

        var rsaKey = new RsaSecurityKey(rsa);

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = rsaKey
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Token inválido: {ex.Message}");
            return null;
        }
    }
}





var token = "tu_token_aquí"; // Puedes copiarlo desde Postman o consola
var publicKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "keys", "jwtRSA256.pem.pub");

var validador = new TokenValidator();
var claims = validador.ValidarToken(token, publicKeyPath);

if (claims != null)
{
    Console.WriteLine("✅ Token válido. Claims:");
    foreach (var claim in claims.Claims)
    {
        Console.WriteLine($"  {claim.Type}: {claim.Value}");
    }
}
else
{
    Console.WriteLine("❌ Token inválido.");
}
