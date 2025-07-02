<?xml version="1.0"?>
<package >
  <metadata>
    <id>RestUtilities.QueryBuilder</id>
    <version>1.0.0</version>
    <authors>RestUtilities Team</authors>
    <owners>RestUtilities</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Librería modular para la construcción dinámica, segura y multiplataforma de consultas SQL (SELECT, INSERT, UPDATE, JOIN, WHERE, etc.) basada en modelos, expresiones lambda y compatibilidad con múltiples motores (SQL Server, Oracle, AS400, etc.).</description>
    <copyright>Copyright © 2025</copyright>
    <tags>sql querybuilder lambda orm dbcontext dynamic safe generator restutilities</tags>
    <repository type="git" url="https://github.com/RestUtilities/QueryBuilder" />
    <dependencies>
      <dependency id="Microsoft.Extensions.DependencyInjection.Abstractions" version="8.0.0" />
    </dependencies>
  </metadata>
</package>


<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <Version>1.0.0</Version>
    <Authors>RestUtilities Team</Authors>
    <Company>RestUtilities</Company>
    <Description>Librería modular para generar consultas SQL dinámicas multiplataforma (compatible con SQL Server, Oracle, AS400, etc.) con soporte para modelos, atributos, expresiones lambda y validaciones de tipo.</Description>
    <PackageId>RestUtilities.QueryBuilder</PackageId>
    <RepositoryUrl>https://github.com/RestUtilities/QueryBuilder</RepositoryUrl>
    <PackageTags>sql querybuilder lambda expressions orm dbcontext restutilities</PackageTags>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
  </ItemGroup>

</Project>
