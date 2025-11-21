# Asegura el proyecto correcto
$proj = "Presentation.RestService"

# 1) Web API 5.3.0 (instala/realinea la versión exacta)
Install-Package Microsoft.AspNet.WebApi         -Version 5.3.0 -ProjectName $proj -Source nuget.org
Install-Package Microsoft.AspNet.WebApi.Core    -Version 5.3.0 -ProjectName $proj -Source nuget.org
Install-Package Microsoft.AspNet.WebApi.Owin    -Version 5.3.0 -ProjectName $proj -Source nuget.org
Install-Package Microsoft.AspNet.WebApi.Client  -Version 5.3.0 -ProjectName $proj -Source nuget.org
Install-Package Microsoft.AspNet.WebApi.Cors    -Version 5.3.0 -ProjectName $proj -Source nuget.org

# 2) OWIN 4.2.3 (toda la familia en la misma versión)
Install-Package Microsoft.Owin                   -Version 4.2.3 -ProjectName $proj -Source nuget.org
Install-Package Microsoft.Owin.Host.SystemWeb    -Version 4.2.3 -ProjectName $proj -Source nuget.org
Install-Package Microsoft.Owin.Security          -Version 4.2.3 -ProjectName $proj -Source nuget.org
Install-Package Microsoft.Owin.Security.Cookies  -Version 4.2.3 -ProjectName $proj -Source nuget.org
Install-Package Microsoft.Owin.Security.OAuth    -Version 4.2.3 -ProjectName $proj -Source nuget.org
Install-Package Microsoft.Owin.Cors              -Version 4.2.3 -ProjectName $proj -Source nuget.org

# 3) (Opcional) Reinstala sin cambiar versión para corregir HintPath si quedó al GAC
Update-Package Microsoft.AspNet.WebApi.Core   -ProjectName $proj -Reinstall
Update-Package Microsoft.AspNet.WebApi.Owin   -ProjectName $proj -Reinstall
Update-Package Microsoft.AspNet.WebApi.Client -ProjectName $proj -Reinstall
