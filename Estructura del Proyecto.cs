System.AggregateException
  HResult=0x80131500
  Message=Some services are not able to be constructed (Error while validating the service descriptor 'ServiceType: API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.IReporteriaService Lifetime: Scoped ImplementationType: API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.ReporteriaService': Unable to resolve service for type 'Connections.Interfaces.IDatabaseConnection' while attempting to activate 'API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.ReporteriaService'.)
  Source=Microsoft.Extensions.DependencyInjection
  StackTrace:
   at Microsoft.Extensions.DependencyInjection.ServiceProvider..ctor(ICollection`1 serviceDescriptors, ServiceProviderOptions options)
   at Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(IServiceCollection services, ServiceProviderOptions options)
   at Microsoft.Extensions.Hosting.HostApplicationBuilder.Build()
   at Microsoft.AspNetCore.Builder.WebApplicationBuilder.Build()
   at Program.<Main>$(String[] args) in C:\Git\Librerias Davivienda\Temporal\API_1_TERCEROS_REMESADORAS\Program.cs:line 118

  This exception was originally thrown at this call stack:
    [External Code]

Inner Exception 1:
InvalidOperationException: Error while validating the service descriptor 'ServiceType: API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.IReporteriaService Lifetime: Scoped ImplementationType: API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.ReporteriaService': Unable to resolve service for type 'Connections.Interfaces.IDatabaseConnection' while attempting to activate 'API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.ReporteriaService'.

Inner Exception 2:
InvalidOperationException: Unable to resolve service for type 'Connections.Interfaces.IDatabaseConnection' while attempting to activate 'API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.ReporteriaService'.
