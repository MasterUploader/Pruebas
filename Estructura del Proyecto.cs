public static class CertificateLoader
{
    public static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
    {
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var cert = store.Certificates
            .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
            .OfType<X509Certificate2>()
            .FirstOrDefault();

        if (cert == null || !cert.HasPrivateKey)
            throw new Exception("Certificado no encontrado o sin clave privada.");

        return cert;
    }
}




public class JwtGenerator
{
    private readonly X509Certificate2 _certificate;

    public JwtGenerator(X509Certificate2 certificate)
    {
        _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
    }

    public string GenerateToken(string issuer, string audience, TimeSpan expiresIn)
    {
        var securityKey = new X509SecurityKey(_certificate);

        var signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.RsaSha256 // Puede ser RsaSha384 o RsaSha512 según el certificado
        );

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "usuario@ejemplo.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("rol", "Administrador")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(expiresIn),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}


dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
