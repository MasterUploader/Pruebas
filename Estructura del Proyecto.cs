// AUTORIZACIÓN
builder.Services.AddAuthorization(options =>
{
    // Política "ActiveSession" que delega la validación al handler
    options.AddPolicy("ActiveSession", policy =>
        policy.AddRequirements(new ActiveSessionRequirement()));
});

// 👇 Importante: el handler debe ser Scoped (o Transient), NO Singleton
builder.Services.AddScoped<IAuthorizationHandler, ActiveSessionHandler>();

// Asegura también que tu ISessionManagerService sea Scoped (recomendado)
builder.Services.AddScoped<ISessionManagerService, SessionManagerService>();
