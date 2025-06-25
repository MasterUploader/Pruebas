using System;
using System.IO;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

public class JwtValidator
{
    public static void DecodificarYValidarToken(string token, string publicKeyPath)
    {
        if (!File.Exists(publicKeyPath))
        {
            Console.WriteLine("❌ Archivo de clave pública no encontrado.");
            return;
        }

        string publicKeyPem = File.ReadAllText(publicKeyPath);
        var rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(publicKeyPem.ToCharArray());
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Error al cargar la clave: {e.Message}");
            return;
        }

        var rsaKey = new RsaSecurityKey(rsa);
        var handler = new JwtSecurityTokenHandler();

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false, // Ignora expiración si solo quieres validar firma
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaKey
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out SecurityToken validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            Console.WriteLine("✅ Firma válida.");
            Console.WriteLine("\n🔎 HEADER:");
            foreach (var h in jwtToken.Header)
                Console.WriteLine($"  {h.Key}: {h.Value}");

            Console.WriteLine("\n📦 PAYLOAD:");
            foreach (var c in jwtToken.Claims)
                Console.WriteLine($"  {c.Type}: {c.Value}");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            Console.WriteLine("❌ Firma inválida. La clave pública no corresponde.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error: {ex.Message}");
        }
    }
}
