builder.Services.AddSingleton<IDatabaseConnection>(sp =>
{
    var settings = sp.GetRequiredService<ConnectionSettings>();
    var logger = sp.GetRequiredService<ILogger>();

    string connectionString = settings.GetAS400ConnectionString("AS400");

    logger.LogInformation($"[AS400] Cadena de conexión construida dinámicamente.");

    return new AS400ConnectionProvider(connectionString);
});
