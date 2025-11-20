# Asegura TODA la familia OWIN en la misma versión
Install-Package Owin -Version 1.0
Install-Package Microsoft.Owin -Version 4.2.2
Install-Package Microsoft.Owin.Host.SystemWeb -Version 4.2.2
Install-Package Microsoft.Owin.Security -Version 4.2.2
Install-Package Microsoft.Owin.Security.OAuth -Version 4.2.2
Install-Package Microsoft.Owin.Cors -Version 4.2.2






  # OWIN (toda la familia a la MISMA versión)
Install-Package Microsoft.Owin -Version 4.2.2 -ProjectName Presentation.RestService
Install-Package Microsoft.Owin.Host.SystemWeb -Version 4.2.2 -ProjectName Presentation.RestService
Install-Package Microsoft.Owin.Security -Version 4.2.2 -ProjectName Presentation.RestService
Install-Package Microsoft.Owin.Security.Cookies -Version 4.2.2 -ProjectName Presentation.RestService
Install-Package Microsoft.Owin.Security.OAuth -Version 4.2.2 -ProjectName Presentation.RestService
Install-Package Microsoft.Owin.Cors -Version 4.2.2 -ProjectName Presentation.RestService

# ASP.NET Identity (los 3 paquetes base, versión estable)
Install-Package Microsoft.AspNet.Identity.Core -Version 2.2.3 -ProjectName Presentation.RestService
Install-Package Microsoft.AspNet.Identity.Owin -Version 2.2.3 -ProjectName Presentation.RestService
# (si usas EF para usuarios)
Install-Package Microsoft.AspNet.Identity.EntityFramework -Version 2.2.3 -ProjectName Presentation.RestService

