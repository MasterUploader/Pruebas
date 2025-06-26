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
    <IncludeBuildOutput>true</IncludeBuildOutput>
    <RemoveUnnecessaryImports>true</RemoveUnnecessaryImports>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Constants\CommonConstants.cs" />
    
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
    
    <Compile Include="Models\ApiResponse.cs" />
  </ItemGroup>

</Project>

<?xml version="1.0"?>
<package >
  <metadata>
    <id>RestUtilities.Common</id>
    <version>1.0.0</version>
    <authors>Brayan René Banegas Mejía</authors>
    <owners>Brayan René Banegas Mejía</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Componentes comunes reutilizables para proyectos .NET 8, incluyendo helpers, constantes y modelos base.</description>
    <tags>Common Helpers Regex JSON XML Utilities .NET8</tags>
    <copyright>Copyright © 2025</copyright>
  </metadata>
</package>
