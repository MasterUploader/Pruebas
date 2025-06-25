using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

public class JwtValidator
{
    public static JwtValidationResult ValidarTokenRS256(string token, string publicKeyPath)
    {
        var result = new JwtValidationResult();

        if (!File.Exists(publicKeyPath))
        {
            result.Success = false;
            result.ErrorMessage = "Archivo de clave pública no encontrado.";
            return result;
        }

        string publicKeyPem = File.ReadAllText(publicKeyPath);
        var rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(publicKeyPem.ToCharArray());
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error al cargar la clave pública: {ex.Message}";
            return result;
        }

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false, // para validación de firma solamente
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out SecurityToken validatedToken);

            var jwtToken = validatedToken as JwtSecurityToken;
            if (jwtToken == null || jwtToken.Header.Alg != SecurityAlgorithms.RsaSha256)
            {
                result.Success = false;
                result.ErrorMessage = "El token no es RS256.";
                return result;
            }

            result.Success = true;
            result.Header = new Dictionary<string, object>(jwtToken.Header);
            result.Claims = new Dictionary<string, string>();
            foreach (var claim in jwtToken.Claims)
            {
                result.Claims[claim.Type] = claim.Value;
            }

            return result;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            result.Success = false;
            result.ErrorMessage = "Firma inválida: la clave pública no corresponde al token.";
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error al validar el token: {ex.Message}";
            return result;
        }
    }
}


string token = "tu.jwt.aqui";
string keyPath = "./keys/jwtRSA256.pem.pub";

var resultado = JwtValidator.ValidarTokenRS256(token, keyPath);

if (resultado.Success)
{
    Console.WriteLine("✅ Firma válida. Claims:");
    foreach (var kv in resultado.Claims)
    {
        Console.WriteLine($"  {kv.Key}: {kv.Value}");
    }
}
else
{
    Console.WriteLine($"❌ Error: {resultado.ErrorMessage}");
}
