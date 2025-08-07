public async Task InvokeAsync(HttpContext context)
{
    var path = context.Request.Path.Value;
    if (!string.IsNullOrEmpty(path) &&
        (path.Contains("swagger", StringComparison.OrdinalIgnoreCase) ||
         path.Contains("favicon.ico", StringComparison.OrdinalIgnoreCase)))
    {
        await _next(context); // No loguear estas rutas
        return;
    }

    // Lógica de logging normal
    await _next(context);
}
