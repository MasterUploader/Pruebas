using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Owin;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Cors;

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var policy = new CorsPolicy
        {
            AllowAnyHeader = true,
            AllowAnyMethod = true,
            SupportsCredentials = true
        };
        policy.Origins.Add("http://localhost:4200");
        app.UseCors(new CorsOptions
        {
            PolicyProvider = new CorsPolicyProvider { Policy = policy }
        });

        var config = new HttpConfiguration();
        WebApiConfig.Register(config);
        app.UseWebApi(config);
    }
}
