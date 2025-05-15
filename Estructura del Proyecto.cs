builder.Services.AddSingleton(sp =>
{
    var cert = CertificateLoader.LoadFromStore("‎ABC123..."); // Thumbprint sin espacios
    return new JwtGenerator(cert);
});
