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
