<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
		<!-- Metadatos para NuGet -->
		<PackageId>RestUtilities.Connections</PackageId>
		<Version>1.0.1</Version>
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
