// ===============================  HTTP Saliente  ===============================
// Handler que intercepta TODAS las solicitudes/respuestas HTTP salientes para loguearlas
builder.Services.AddTransient<HttpClientLoggingHandler>();

// Cliente HTTP **nombrado** con el handler de logging encadenado.
// Nota: no configuro BaseAddress porque ya armas la URL con GlobalConnection.Current.Host.
// Si prefieres, puedes asignar BaseAddress y luego usar rutas relativas en los servicios.
builder.Services
    .AddHttpClient("TNP", (sp, http) =>
    {
        http.Timeout = TimeSpan.FromSeconds(30);
        http.DefaultRequestHeaders.ExpectContinue = false;

        // Evitar cualquier rastro de credenciales/Authorization por defecto
        http.DefaultRequestHeaders.Authorization = null;
        // Si llegases a activar DefaultRequestHeaders.ProxyAuthorization, anúlalo también
        // http.DefaultRequestHeaders.ProxyAuthorization = null;
    })
    // Mantén tu logger de HTTP (no rompe los logs existentes)
    .AddHttpMessageHandler<HttpClientLoggingHandler>()
    // Configura el handler principal según el ambiente
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var env = sp.GetRequiredService<IHostEnvironment>();

        var handler = new SocketsHttpHandler
        {
            // Rendimiento & sane defaults
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AllowAutoRedirect = false,
            UseCookies = false,

            // Importante: sin credenciales/NTLM implícitas
            PreAuthenticate = false,
            Credentials = null
        };

        if (env.IsDevelopment())
        {
            // Solo DEV: aceptar cualquier certificado (equivalente a curl -k)
            handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = static (_, _, _, _) => true
            };
        }
        // En UAT/PROD NO tocar SslOptions => validación estricta de certificado

        return handler;
    });
