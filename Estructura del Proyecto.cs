si quito el Scoped da este error:

-----------------------------Exception Details---------------------------------
Inicio: 2025-09-09 10:19:23
-------------------------------------------------------------------------------
System.InvalidOperationException: A suitable constructor for type 'API_1_TERCEROS_REMESADORAS.Services.BTSServices.AuthenticateService.AuthenticateService' could not be located. Ensure the type is concrete and all parameters of a public constructor are either registered as services or passed as arguments. Also ensure no extraneous arguments are provided.
   at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.FindApplicableConstructor(Type instanceType, Type[] argumentTypes, ConstructorInfo& matchingConstructor, Nullable`1[]& matchingParameterMap)
   at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactoryInternal(Type instanceType, Type[] argumentTypes, ParameterExpression& provider, ParameterExpression& argumentArray, Expression& factoryExpressionBody)
   at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory(Type instanceType, Type[] argumentTypes)
   at System.Threading.LazyInitializer.EnsureInitializedCore[T](T& target, Boolean& initialized, Object& syncLock, Func`1 valueFactory)
   at Microsoft.Extensions.Http.DefaultTypedHttpClientFactory`1.Cache.get_Activator()
   at Microsoft.Extensions.Http.DefaultTypedHttpClientFactory`1.CreateClient(HttpClient httpClient)
   at Microsoft.Extensions.DependencyInjection.HttpClientBuilderExtensions.AddTransientHelper[TClient,TImplementation](IServiceProvider s, IHttpClientBuilder builder)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitDisposeCache(ServiceCallSite transientCallSite, RuntimeResolverContext context)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.DynamicServiceProviderEngine.<>c__DisplayClass2_0.<RealizeService>b__0(ServiceProviderEngineScope scope)
   at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope)
   at Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceProviderEngineScope.GetService(Type serviceType)
   at lambda_method10(Closure, IServiceProvider, Object[])
   at Microsoft.AspNetCore.Mvc.Controllers.ControllerFactoryProvider.<>c__DisplayClass6_0.<CreateControllerFactory>g__CreateController|0(ControllerContext controllerContext)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.InvokeInnerFilterAsync()
--- End of stack trace from previous location ---
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeFilterPipelineAsync>g__Awaited|20_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Logged|17_1(ResourceInvoker invoker)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Logged|17_1(ResourceInvoker invoker)
   at Logging.Middleware.LoggingMiddleware.InvokeAsync(HttpContext context)
-----------------------------Exception Details---------------------------------

    Lo dejare solo con esto para que funcione:

builder.Services.AddScoped<IDatabaseConnection>(sp =>
{
    // Obtener configuración general
    var config = sp.GetRequiredService<IConfiguration>();
    var settings = new ConnectionSettings(config); // Tu clase para acceder a settings

    // Obtener cadena de conexión desencriptada para AS400
    var connStr = settings.GetAS400ConnectionString("AS400");

    // Instanciar proveedor interno
    return new AS400ConnectionProvider(
        connStr,
        sp.GetRequiredService<ILoggingService>(),
        sp.GetRequiredService<IHttpContextAccessor>()); // Constructor de AS400ConnectionProvider       
});
