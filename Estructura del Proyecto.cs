System.AggregateException
  HResult=0x80131500
  Message=Some services are not able to be constructed (Error while validating the service descriptor 'ServiceType: Microsoft.AspNetCore.Authorization.IAuthorizationService Lifetime: Transient ImplementationType: Microsoft.AspNetCore.Authorization.DefaultAuthorizationService': Cannot consume scoped service 'MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService.ISessionManagerService' from singleton 'Microsoft.AspNetCore.Authorization.IAuthorizationHandler'.) (Error while validating the service descriptor 'ServiceType: Microsoft.AspNetCore.Authorization.IAuthorizationHandlerProvider Lifetime: Transient ImplementationType: Microsoft.AspNetCore.Authorization.DefaultAuthorizationHandlerProvider': Cannot consume scoped service 'MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService.ISessionManagerService' from singleton 'Microsoft.AspNetCore.Authorization.IAuthorizationHandler'.) (Error while validating the service descriptor 'ServiceType: Microsoft.AspNetCore.Authorization.Policy.IPolicyEvaluator Lifetime: Transient ImplementationType: Microsoft.AspNetCore.Authorization.Policy.PolicyEvaluator': Cannot consume scoped service 'MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService.ISessionManagerService' from singleton 'Microsoft.AspNetCore.Authorization.IAuthorizationHandler'.) (Error while validating the service descriptor 'ServiceType: Microsoft.AspNetCore.Authorization.IAuthorizationHandler Lifetime: Singleton ImplementationType: MS_BAN_43_Embosado_Tarjetas_Debito.Authorization.ActiveSessionHandler': Cannot consume scoped service 'MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService.ISessionManagerService' from singleton 'Microsoft.AspNetCore.Authorization.IAuthorizationHandler'.)
  Source=Microsoft.Extensions.DependencyInjection
  StackTrace:
   at Microsoft.Extensions.DependencyInjection.ServiceProvider..ctor(ICollection`1 serviceDescriptors, ServiceProviderOptions options)
   at Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(IServiceCollection services, ServiceProviderOptions options)
   at Microsoft.Extensions.Hosting.HostApplicationBuilder.Build()
   at Microsoft.AspNetCore.Builder.WebApplicationBuilder.Build()
   at Program.<<Main>$>d__0.MoveNext() in C:\Git\MS_BAN_43_EmbosadoTarjetasDebito\BACKEND\MS_BAN_43_Embosado_Tarjetas_Debito\Program.cs:line 212

  This exception was originally thrown at this call stack:
    [External Code]

Inner Exception 1:
InvalidOperationException: Error while validating the service descriptor 'ServiceType: Microsoft.AspNetCore.Authorization.IAuthorizationService Lifetime: Transient ImplementationType: Microsoft.AspNetCore.Authorization.DefaultAuthorizationService': Cannot consume scoped service 'MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService.ISessionManagerService' from singleton 'Microsoft.AspNetCore.Authorization.IAuthorizationHandler'.

Inner Exception 2:
InvalidOperationException: Cannot consume scoped service 'MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService.ISessionManagerService' from singleton 'Microsoft.AspNetCore.Authorization.IAuthorizationHandler'.
