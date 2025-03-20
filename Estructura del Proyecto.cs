<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <!-- Identificador del paquete NuGet -->
    <id>RestUtilities.Connections</id>

    <!-- Versión del paquete (Actualizar según corresponda) -->
    <version>1.0.0</version>

    <!-- Nombre del paquete visible en NuGet -->
    <title>RestUtilities.Connections</title>

    <!-- Breve descripción del paquete -->
    <description>Biblioteca para gestionar conexiones a bases de datos, servicios externos y mensajería en .NET 8.</description>

    <!-- Autor(es) del paquete -->
    <authors>TuNombre</authors>

    <!-- Empresa u organización (opcional) -->
    <owners>TuEmpresa</owners>

    <!-- Licencia (MIT en este caso) -->
    <license type="expression">MIT</license>

    <!-- URL de la licencia -->
    <licenseUrl>https://opensource.org/licenses/MIT</licenseUrl>

    <!-- URL de la página del proyecto -->
    <projectUrl>https://github.com/TuRepositorio</projectUrl>

    <!-- URL para reportar problemas -->
    <bugTrackerUrl>https://github.com/TuRepositorio/issues</bugTrackerUrl>

    <!-- Etiquetas para mejorar la búsqueda en NuGet -->
    <tags>REST API Connections SQL RabbitMQ Redis gRPC AS400</tags>

    <!-- Indica que el paquete es estable -->
    <releaseNotes>Versión inicial con soporte para múltiples conexiones.</releaseNotes>

    <!-- Indica que el paquete requiere aceptar términos -->
    <requireLicenseAcceptance>false</requireLicenseAcceptance>

    <!-- Framework Target -->
    <dependencies>
      <group targetFramework=".NET8.0">
        <dependency id="Microsoft.Extensions.Configuration" version="8.0.0" />
        <dependency id="Microsoft.Extensions.Configuration.Json" version="8.0.0" />
        <dependency id="Microsoft.Data.SqlClient" version="5.0.0" />
        <dependency id="RabbitMQ.Client" version="7.0.0" />
        <dependency id="StackExchange.Redis" version="3.0.0" />
        <dependency id="Grpc.Net.Client" version="2.50.0" />
        <dependency id="System.Text.Json" version="8.0.0" />
      </group>
    </dependencies>

  </metadata>

  <!-- Especifica qué archivos incluir en el paquete -->
  <files>
    <file src="bin\Release\net8.0\RestUtilities.Connections.dll" target="lib\net8.0\" />
    <file src="bin\Release\net8.0\RestUtilities.Connections.pdb" target="lib\net8.0\" />
    <file src="README.md" target="docs\" />
    <file src="LICENSE" target="docs\" />
  </files>
</package>
