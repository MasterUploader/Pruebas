<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>

		<!-- NuGet Metadata -->
		<PackageId>RestUtilities.Common</PackageId>
		<Version>1.0.0</Version>
		<Authors>Brayan René Banegas Mejía</Authors>
		<Description>Componentes comunes reutilizables para utilidades en .NET 8: helpers, constantes y modelos base para APIs.</Description>
		<PackageTags>Common;Utilities;Helpers;Regex;JSON;XML</PackageTags>
		<GeneratePackageOnBuild>true</GeneratePackageOnBuild>
		<PlatformTarget>x64</PlatformTarget>
		<Platforms>x64</Platforms>
		<IncludeBuildOutput>true</IncludeBuildOutput>
		<RemoveUnnecessaryImports>true</RemoveUnnecessaryImports>
	</PropertyGroup>


	<ItemGroup>
		<!-- Constants -->
		<Compile Include="Constants\CommonConstants.cs" />

		<!-- Helpers -->
		<Compile Include="Helpers\DateTimeHelper.cs" />
		<Compile Include="Helpers\EnumHelper.cs" />
		<Compile Include="Helpers\EnvironmentHelper.cs" />
		<Compile Include="Helpers\HttpHelper.cs" />
		<Compile Include="Helpers\JsonHelper.cs" />
		<Compile Include="Helpers\RegexPatterns.cs" />
		<Compile Include="Helpers\RetryHelper.cs" />
		<Compile Include="Helpers\StopwatchHelper.cs" />
		<Compile Include="Helpers\StringHelper.cs" />
		<Compile Include="Helpers\TimeSpanHelper.cs" />
		<Compile Include="Helpers\ValidationHelper.cs" />
		<Compile Include="Helpers\XmlHelper.cs" />

		<!-- Models -->
		<Compile Include="Models\ApiResponse.cs" />
	</ItemGroup>
	
	

</Project>



<?xml version="1.0"?>
<package >
	<metadata>
		<id>RestUtilities.Common</id>
		<version>1.0.0</version>
		<title>RestUtilities.Common</title>
		<authors>Brayan René Banegas Mejía</authors>
		<owners>Brayan René Banegas Mejía</owners>
		<requireLicenseAcceptance>false</requireLicenseAcceptance>
		<description>Componentes comunes reutilizables para proyectos .NET 8, incluyendo helpers, constantes y modelos base.</description>
		<tags>Common Helpers Regex JSON XML Stopwatch Utilities</tags>
		<copyright>Copyright © 2025</copyright>
		<dependencies>
			<group targetFramework=".NETCoreApp8.0" />
		</dependencies>
	</metadata>

	<files>
		<!-- Incluye el ensamblado DLL en la carpeta correcta -->
		<file src="bin\Release\net8.0\Common.dll" target="lib\net8.0" />
		<file src="bin\Release\net8.0\Common.pdb" target="lib\net8.0" />
		<file src="bin\Release\net8.0\Common.xml" target="lib\net8.0" />
	</files>
</package>


<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<!-- Metadatos para NuGet -->
		<PackageId>RestUtilities.Connections</PackageId>
		<Version>1.0.0</Version>
		<Authors>Brayan René Banegas Mejía</Authors>
		<Description>Biblioteca para gestionar conexiones a bases de datos, servicios externos y mensajería en .NET 8.</Description>
		<PackageTags>Conexiones;As400;SQL</PackageTags>
		<!-- Generar el paquete NuGet automáticamente al compilar -->
		<GeneratePackageOnBuild>true</GeneratePackageOnBuild>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
		<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
		<PlatformTarget>x64</PlatformTarget>
		<IncludeBuildOutput>true</IncludeBuildOutput>
		<Platforms>x64</Platforms>
		<RemoveUnnecessaryImports>true</RemoveUnnecessaryImports>
		<DisableFastUpToDateCheck>true</DisableFastUpToDateCheck>
	</PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentFTP" Version="52.1.0" />
    <PackageReference Include="Grpc.Net.Client" Version="2.71.0" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.2" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.6" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.6" />
    <PackageReference Include="Oracle.ManagedDataAccess.Core" Version="23.8.0" />
    <PackageReference Include="RabbitMQ.Client" Version="7.1.2" />
    <PackageReference Include="StackExchange.Redis" Version="2.8.41" />
    <PackageReference Include="System.Data.OleDb" Version="8.0.1" />
  </ItemGroup>

	<ItemGroup>
		<FrameworkReference Include="Microsoft.AspNetCore.App" />
	</ItemGroup>


	<ItemGroup>
		<!-- Helpers-->
		<Compile Include="Helpers\ConnectionManagerHelper.cs" />
		<Compile Include="Helpers\EncryptionHelper.cs" />
		
		
		<!-- Interfaces -->
		<Compile Include="Interfaces\IExternalServiceConnection.cs" />
		<Compile Include="Interfaces\IConnectionManager.cs" />
		<Compile Include="Interfaces\IWebSocketConnection.cs" />
		<Compile Include="Interfaces\IGrpcConnection.cs" />
		<Compile Include="Interfaces\IFtpConnection.cs" />
		<Compile Include="Interfaces\IServiceConnectionFactory.cs" />
		<Compile Include="Interfaces\IMessageQueueConnection.cs" />
		<Compile Include="Interfaces\ISoapServiceConnection.cs" />
		<Compile Include="Managers\LoggingDatabaseConnection.cs" />

		<!-- Providers/Database -->
		<Compile Include="Providers\Database\AS400ConnectionProvider.cs" />
		<Compile Include="Providers\Database\ExternalDbContextConnectionProvider.cs" />
		<Compile Include="Providers\Database\MSSQLConnectionProvider.cs" />
		<Compile Include="Providers\Database\OracleConnectionProvider.cs" />
		<Compile Include="Providers\Database\MySQLConnectionProvider.cs" />
		<Compile Include="Providers\Database\PostgreSQLConnectionProvider.cs" />
		<Compile Include="Providers\Database\MongoDBConnectionProvider.cs" />
		<Compile Include="Providers\Database\RedisConnectionProvider.cs" />
		<Compile Include="Providers\Database\DatabaseConnectionFactory.cs" />

		<!-- Providers/Services -->
		<Compile Include="Providers\Services\RestServiceClient.cs" />
		<Compile Include="Providers\Services\SoapServiceClient.cs" />
		<Compile Include="Providers\Services\WebSocketConnectionProvider.cs" />
		<Compile Include="Providers\Services\GrpcConnectionProvider.cs" />
		<Compile Include="Providers\Services\RabbitMQConnectionProvider.cs" />
		<Compile Include="Providers\Services\FtpConnectionProvider.cs" />
		<Compile Include="Providers\Services\ServiceConnectionFactory.cs" />
		
		<!--Logging-->

		<!-- MAnagers -->
		<Compile Include="Managers\ConnectionManager.cs" />
		<Compile Include="Managers\DatabaseManager.cs" />
		<Compile Include="Managers\ServiceManager.cs" />
		<Compile Include="Managers\WebSocketManager.cs" />
		<Compile Include="Managers\GrpcManager.cs" />

		<!-- Models -->
		<Compile Include="Models\ConnectionInfo.cs" />
		<Compile Include="Models\DatabaseSettings.cs" />
		<Compile Include="Models\ServiceSettings.cs" />
		<Compile Include="Models\WebSocketSettings.cs" />
		<Compile Include="Models\GrpcSettings.cs" />
		<Compile Include="Models\RedisSettings.cs" />

		<!-- Services -->
		<Compile Include="Services\ConnectionSettings.cs" />

	</ItemGroup>


	<ItemGroup>
	  <ProjectReference Include="..\Connections.Abstractions\Connections.Abstractions.csproj" />
	  <ProjectReference Include="..\Logging\Logging.csproj" />
	</ItemGroup>


</Project>


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
		<authors>Brayan René Banegas Mejía</authors>

		<!-- Empresa u organización (opcional) -->
		<owners>Davivienda Honduras</owners>

		<!-- Etiquetas para mejorar la búsqueda en NuGet -->
		<tags>REST API Connections SQL RabbitMQ Redis gRPC AS400</tags>

		<!-- Indica que el paquete es estable -->
		<releaseNotes>Versión inicial con soporte para múltiples conexiones.</releaseNotes>

		<!-- Indica que el paquete requiere aceptar términos -->
		<requireLicenseAcceptance>false</requireLicenseAcceptance>

		<!-- Framework Target -->
		<dependencies>
			<group targetFramewrok="net8.0">
				<dependency id="FluentFTP" Version="[52.1.0]" />
				<dependency id="IBM.EntityFrameworkCore" Version="[8.0.0.300]" />
				<dependency id="Microsoft.Extensions.Configuration" version="[8.0.0]" />
				<dependency id="Microsoft.Extensions.Configuration.Json" version="[8.0.0]" />
				<dependency id="Microsoft.Extensions.Configuration.Binder" Version="[9.0.3]" />
				<dependency id="Microsoft.Data.SqlClient" version="[5.0.0]" />
				<dependency id="Oracle.ManagedDataAccess.Core" Version="[23.7.0]" />
				<dependency id="RabbitMQ.Client" version="[7.0.0]" />
				<dependency id="StackExchange.Redis" version="[3.0.0]" />
				<dependency id="Grpc.Net.Client" version="[2.50.0]" />
				<dependency id="StackExchange.Redis" Version="[2.8.31]" />
			</group>
		</dependencies>


	</metadata>
</package>



<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<!-- Target .NET version -->
		<TargetFramework>net8.0</TargetFramework>

		<!-- NuGet metadata -->
		<PackageId>RestUtilities.Connections.Abstractions</PackageId>
		<Version>1.0.0</Version>
		<Authors>Brayan René Banegas Mejía</Authors>
		<Description>Interfaces base para el sistema de conexión modular de RestUtilities.</Description>
		<PackageTags>Conexiones;Abstracciones;SQL;AS400</PackageTags>

		<!-- Configuración del build -->
		<GeneratePackageOnBuild>true</GeneratePackageOnBuild>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
		<PlatformTarget>x64</PlatformTarget>

		<Platforms>x64</Platforms>
		<IncludeBuildOutput>true</IncludeBuildOutput>
	</PropertyGroup>

	<ItemGroup>
	  <PackageReference Include="Microsoft.AspNetCore.Http" Version="2.3.0" />
	</ItemGroup>

</Project>


<?xml version="1.0"?>
<package >
	<metadata>
		<id>RestUtilities.Connections.Abstractions</id>
		<version>1.0.0</version>
		<authors>Brayan René Banegas Mejía</authors>
		<owners>Brayan René Banegas Mejía</owners>
		<requireLicenseAcceptance>false</requireLicenseAcceptance>
		<description>Interfaces base para el sistema de conexión modular de RestUtilities, compatible con múltiples motores SQL y servicios externos.</description>
		<tags>RestUtilities Conexiones Abstracciones AS400 SQL</tags>
		<copyright>Copyright © 2025</copyright>
	</metadata>
</package>


<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<!-- Metadatos para NuGet -->
		<PackageId>RestUtilities.Logging</PackageId>
		<Version>1.0.0</Version>
		<Authors>Brayan René Banegas Mejía</Authors>
		<Description>Librería de logging para APIs ASP.NET Core que captura información de request/response y el flujo de ejecución.</Description>
		<!-- Generar el paquete NuGet automáticamente al compilar -->
		<GeneratePackageOnBuild>true</GeneratePackageOnBuild>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>
		<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
		<PlatformTarget>x64</PlatformTarget>
		<IncludeBuildOutput>true</IncludeBuildOutput>
		<Platforms>x64</Platforms>
		<RemoveUnnecessaryImports>true</RemoveUnnecessaryImports>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Castle.Core" Version="5.2.1" />
		<PackageReference Include="Scrutor" Version="6.1.0" />
	</ItemGroup>

	<ItemGroup>
		<FrameworkReference Include="Microsoft.AspNetCore.App" />		
	</ItemGroup>

	<ItemGroup>
		<!-- Interfaces -->
		<Compile Include="Abstractions\ILoggingService.cs" />

		<!-- Configuración -->
		<Compile Include="Configuration\LoggingOptions.cs" />
		<Compile Include="Decorators\LoggingDatabaseConnectionDecorator.cs" />
		<Compile Include="Extensions\LoggingExtensions.cs" />

		<!-- Extensiones -->
		<Compile Include="Extensions\StringExtensions.cs" />

		<!-- Filtros -->
		<Compile Include="Filters\LoggingActionFilter.cs" />
		<Compile Include="Filters\LogMethodExecutionAttribute.cs" />
		<Compile Include="Handlers\HttpClientLoggingHandler.cs" />
		<Compile Include="Helpers\JsonHelper.cs" />
		<Compile Include="Helpers\LoggingDbCommand.cs">
		  <SubType>Component</SubType>
		</Compile>
		<Compile Include="Helpers\LogHelper.cs" />
		<Compile Include="Helpers\QueryExecutionLogger.cs" />

		<!-- Middleware -->
		<Compile Include="Middleware\LoggingMiddleware.cs" />

		<!-- Servicios -->
		<Compile Include="Services\LoggingService.cs" />

		<!-- Helpers -->
		<Compile Include="Helpers\LogFormatter.cs" />
		<Compile Include="Helpers\LogScope.cs" />
	</ItemGroup>

	<ItemGroup>
	  <ProjectReference Include="..\Common\Common.csproj" />
	  <ProjectReference Include="..\Connections.Abstractions\Connections.Abstractions.csproj" />
	</ItemGroup>
	
</Project>



<?xml version="1.0"?>
<package>
	<metadata>
		<id>RestUtilities.Logging</id>
		<version>1.0.1</version>
		<authors>Brayan René Banegas Mejía</authors>
		<owners>Davivienda Honduras</owners>
		<requireLicenseAcceptance>false</requireLicenseAcceptance>
		<description>Librería de logging para APIs ASP.NET Core que captura información de request/response y el flujo de ejecución.</description>
		<dependencies>
			<group targetFramewrok="net8.0">
				<dependency Id="Scrutor" Version="[6.0.1]" />
				<dependency Id="Castle.Core" Version="[5.1.1]" />
			</group>
		</dependencies>
	</metadata>
</package>
