Tengo este error:

System.Reflection.ReflectionTypeLoadException
  HResult=0x80131602
  Message=Unable to load one or more of the requested types.
Method 'Apply' in type 'Swashbuckle.AspNetCore.Examples.AddHeaderOperationFilter' from assembly 'Swashbuckle.AspNetCore.Examples, Version=2.9.0.0, Culture=neutral, PublicKeyToken=aa1e9c5053bfbe95' does not have an implementation.
Method 'Apply' in type 'Swashbuckle.AspNetCore.Examples.AppendAuthorizeToSummaryOperationFilter' from assembly 'Swashbuckle.AspNetCore.Examples, Version=2.9.0.0, Culture=neutral, PublicKeyToken=aa1e9c5053bfbe95' does not have an implementation.
Method 'Apply' in type 'Swashbuckle.AspNetCore.Examples.AuthorizationInputOperationFilter' from assembly 'Swashbuckle.AspNetCore.Examples, Version=2.9.0.0, Culture=neutral, PublicKeyToken=aa1e9c5053bfbe95' does not have an implementation.
Method 'Apply' in type 'Swashbuckle.AspNetCore.Examples.DescriptionOperationFilter' from assembly 'Swashbuckle.AspNetCore.Examples, Version=2.9.0.0, Culture=neutral, PublicKeyToken=aa1e9c5053bfbe95' does not have an implementation.
Method 'Apply' in type 'Swashbuckle.AspNetCore.Examples.ExamplesOperationFilter' from assembly 'Swashbuckle.AspNetCore.Examples, Version=2.9.0.0, Culture=neutral, PublicKeyToken=aa1e9c5053bfbe95' does not have an implementation.
Method 'Apply' in type 'Swashbuckle.AspNetCore.Examples.AddFileParamTypesOperationFilter' from assembly 'Swashbuckle.AspNetCore.Examples, Version=2.9.0.0, Culture=neutral, PublicKeyToken=aa1e9c5053bfbe95' does not have an implementation.
Method 'Apply' in type 'Swashbuckle.AspNetCore.Examples.AddResponseHeadersFilter' from assembly 'Swashbuckle.AspNetCore.Examples, Version=2.9.0.0, Culture=neutral, PublicKeyToken=aa1e9c5053bfbe95' does not have an implementation.
  Source=System.Private.CoreLib
  StackTrace:
   at System.Reflection.RuntimeModule.GetTypes(RuntimeModule module)
   at Microsoft.AspNetCore.Mvc.Controllers.ControllerFeatureProvider.PopulateFeature(IEnumerable`1 parts, ControllerFeature feature)
   at Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager.PopulateFeature[TFeature](TFeature feature)
   at Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerActionDescriptorProvider.GetControllerTypes()
   at Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerActionDescriptorProvider.GetDescriptors()
   at Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerActionDescriptorProvider.OnProvidersExecuting(ActionDescriptorProviderContext context)
   at Microsoft.AspNetCore.Mvc.Infrastructure.DefaultActionDescriptorCollectionProvider.UpdateCollection()
   at Microsoft.AspNetCore.Mvc.Infrastructure.DefaultActionDescriptorCollectionProvider.Initialize()
   at Microsoft.AspNetCore.Mvc.Infrastructure.DefaultActionDescriptorCollectionProvider.GetChangeToken()
   at Microsoft.Extensions.Primitives.ChangeToken.ChangeTokenRegistration`1..ctor(Func`1 changeTokenProducer, Action`1 changeTokenConsumer, TState state)
   at Microsoft.Extensions.Primitives.ChangeToken.OnChange(Func`1 changeTokenProducer, Action changeTokenConsumer)
   at Microsoft.AspNetCore.Mvc.Routing.ActionEndpointDataSourceBase.Subscribe()
   at Microsoft.AspNetCore.Builder.ControllerEndpointRouteBuilderExtensions.GetOrCreateDataSource(IEndpointRouteBuilder endpoints)
   at Microsoft.AspNetCore.Builder.ControllerEndpointRouteBuilderExtensions.MapControllers(IEndpointRouteBuilder endpoints)
   at Program.<<Main>$>d__0.MoveNext() in C:\Git\MS_BAN_56_ProcesamientoTransaccionesPOS\MS_BAN_56_ProcesamientoTransaccionesPOS\MS_BAN_56_ProcesamientoTransaccionesPOS\Program.cs:line 172
