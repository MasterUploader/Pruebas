using Logging.Middleware;
using Logging.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Logging.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar e integrar el servicio de logging en la aplicación.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Agrega los servicios de logging a la colección de servicios de la aplicación.
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación.</param>
        public static void AddLoggingServices(this IServiceCollection services)
        {
            // Registra el servicio de acceso al contexto HTTP para obtener datos de las peticiones.
            services.AddHttpContextAccessor();

            // Registra el servicio de logging como singleton para mantener una única instancia en la aplicación.
            services.AddSingleton<LoggingService>();
        }

        /// <summary>
        /// Habilita el Middleware de logging en la aplicación para capturar todas las solicitudes HTTP.
        /// </summary>
        /// <param name="app">Aplicación de ASP.NET Core.</param>
        public static void UseLoggingMiddleware(this IApplicationBuilder app)
        {
            // Agrega el middleware de logging al pipeline de la aplicación.
            app.UseMiddleware<LoggingMiddleware>();
        }
    }
}
