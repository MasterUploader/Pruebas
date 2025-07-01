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
