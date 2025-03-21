using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Middleware para validar que la solicitud contenga un "header" en el JSON del cuerpo de la petición.
/// Evita deserializar todo el JSON y solo revisa si la clave "header" está presente.
/// </summary>
public class HeaderValidationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructor que recibe el siguiente middleware en la cadena de ejecución.
    /// </summary>
    /// <param name="next">Delegate que representa el siguiente middleware en la cadena.</param>
    public HeaderValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Método principal que se ejecuta en cada solicitud HTTP.
    /// Valida que el "header" esté presente en el JSON del `body`.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        // **1️⃣ Habilitar el reposicionamiento del cuerpo**
        // Esto permite leer el `body` sin que se consuma completamente, 
        // para que otros middlewares y el controlador puedan acceder a él.
        context.Request.EnableBuffering();

        // **2️⃣ Leer el body sin bloquear la ejecución**
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var bodyContent = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0; // Resetear la posición del `body` para que otros lo puedan leer.

        // **3️⃣ Intentar parsear el JSON sin afectar el modelo original**
        try
        {
            using var jsonDoc = JsonDocument.Parse(bodyContent);

            // **4️⃣ Validar si el JSON contiene la propiedad "header"**
            if (!jsonDoc.RootElement.TryGetProperty("header", out var headerElement))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    Message = "El 'header' es obligatorio en el body."
                }));
                return;
            }
        }
        catch (JsonException)
        {
            // **5️⃣ Manejo de error si el JSON es inválido**
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                Message = "Formato de solicitud inválido."
            }));
            return;
        }

        // **6️⃣ Si la validación pasa, continuar con la ejecución del siguiente middleware o controlador**
        await _next(context);
    }
}

/// <summary>
/// Extensión para registrar el middleware en la aplicación.
/// </summary>
public static class HeaderValidationMiddlewareExtensions
{
    /// <summary>
    /// Método de extensión para agregar el `HeaderValidationMiddleware` en la cadena de middlewares.
    /// </summary>
    public static IApplicationBuilder UseHeaderValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HeaderValidationMiddleware>();
    }
}
