using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Filters;           // <-- necesario para HostAuthenticationFilter
using Microsoft.Owin.Security.OAuth;     // <-- OAuthDefaults

namespace Presentation.RestService
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // ===== Autenticación global: sólo OAuth bearer =====
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

            // Rutas por atributo y fallback
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // CORS
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

            // Forzar JSON
            var appXml = config.Formatters.XmlFormatter.SupportedMediaTypes
                .FirstOrDefault(t => t.MediaType == "application/xml");
            if (appXml != null)
                config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXml);

            config.EnableSystemDiagnosticsTracing();
        }
    }
}



