return new LoggingDatabaseConnectionDecorator(
    new AS400ConnectionProvider(connStr, sp.GetRequiredService<ILoggingService>()),
    sp.GetRequiredService<ILoggingService>(),
    sp.GetRequiredService<IHttpContextAccessor>()
);
