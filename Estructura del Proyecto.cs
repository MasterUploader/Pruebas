# Web API 2.2 – 5.3.0
Install-Package Microsoft.AspNet.WebApi -Version 5.3.0
Install-Package Microsoft.AspNet.WebApi.Core -Version 5.3.0
Install-Package Microsoft.AspNet.WebApi.Owin -Version 5.3.0
Install-Package Microsoft.AspNet.WebApi.Client -Version 5.3.0
Install-Package Microsoft.AspNet.WebApi.Cors -Version 5.3.0

# OWIN (toda la familia en 4.2.3, como ya tienes los redirects)
Install-Package Microsoft.Owin -Version 4.2.3
Install-Package Microsoft.Owin.Host.SystemWeb -Version 4.2.3
Install-Package Microsoft.Owin.Security -Version 4.2.3
Install-Package Microsoft.Owin.Security.Cookies -Version 4.2.3
Install-Package Microsoft.Owin.Security.OAuth -Version 4.2.3
Install-Package Microsoft.Owin.Cors -Version 4.2.3

# (Opcional pero recomendado) Quita paquetes ASP.NET Core que no uses en WebApi clásico
Get-Package Microsoft.AspNetCore.* | % { Uninstall-Package $_.Id -Force -RemoveDependencies }
