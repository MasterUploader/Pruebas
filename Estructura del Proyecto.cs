using Logging.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Logging.Extensions
{
    /// <summary>
    /// Métodos de extensión para configurar el servicio de logging.
    /// </summary>
    public static class LoggingExtensions
    {
        public static void AddLoggingServices(this IServiceCollection services)
        {
            services.AddSingleton<LoggingService>();
        }

        public static void UseLoggingMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<LoggingMiddleware>();
        }
    }
}
