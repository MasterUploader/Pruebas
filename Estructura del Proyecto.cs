<# 
.SYNOPSIS
  Limpieza segura de cachés de NuGet/npm/Postman/CrashDumps para el USUARIO ACTUAL.
.DESCRIPTION
  - No borra perfiles, sesiones ni datos en %AppData%\Roaming.
  - No toca carpetas "Microsoft", "Google", "GitHubDesktop".
  - Postman: solo Cache/GPUCache/Code Cache/logs en AppData\Local.
  - NuGet: usa "dotnet nuget locals all --clear".
  - npm: usa "npm cache clean --force".
.PARAMETER Apply
  Ejecuta la limpieza real. Si se omite, corre en modo simulación (Dry-Run).
.PARAMETER VerboseMode
  Muestra más detalle durante la ejecución.
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
  [switch]$Apply,
  [switch]$VerboseMode
)

$ErrorActionPreference = 'Stop'
if ($VerboseMode) { $VerbosePreference = 'Continue' } else { $VerbosePreference = 'SilentlyContinue' }
$DryRun = -not $Apply

function Write-Header($text) { Write-Host "`n=== $text ===" }

function Remove-Safe([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { 
        Write-Verbose "No existe: $Path"
        return 
    }
    if ($DryRun) { 
        Write-Host "[SIMULACION] Eliminar: $Path"
        return 
    }
    try {
        if ($PSCmdlet.ShouldProcess($Path, "Remove-Item")) {
            Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            Write-Host ("OK  {0}" -f $Path)
        }
    } catch {
        Write-Warning ("No se pudo eliminar {0}: {1}" -f $Path, $_.Exception.Message)
    }
}

Write-Header "Limpieza segura (usuario actual)"
Write-Host "Dry-Run: $DryRun  |  Verbose: $VerboseMode"
Write-Host "Acciones: NuGet (oficial), npm cache, CrashDumps, Postman(Local: Cache/GPUCache/Code Cache/logs)"
Write-Host "Resguardo: NO se tocan perfiles, sesiones, %AppData%\Roaming ni carpetas Microsoft/Google/GitHubDesktop."

# --- NuGet (oficial) --------------------------------------------------------------
Write-Header "NuGet"
try {
    $cmdArgs = @("nuget","locals","all","--clear")
    if ($DryRun) {
        Write-Host "[SIMULACION] dotnet $($cmdArgs -join ' ')"
    } else {
        & dotnet @cmdArgs | Out-Null
        Write-Host "OK  NuGet cache limpiada mediante 'dotnet nuget locals all --clear'"
    }
} catch {
    Write-Verbose "dotnet CLI no disponible; intento limpiar directorios de cache NuGet del usuario"
    $nugetPaths = @(
        Join-Path -Path $env:USERPROFILE -ChildPath ".nuget\packages",
        Join-Path -Path $env:LOCALAPPDATA -ChildPath "NuGet\Cache",
        Join-Path -Path $env:LOCALAPPDATA -ChildPath "NuGet\v3-cache",
        Join-Path -Path $env:LOCALAPPDATA -ChildPath "NuGet\v2-cache"
    )
    $nugetPaths | ForEach-Object { Remove-Safe $_ }
}

# --- npm cache --------------------------------------------------------------------
Write-Header "npm cache"
try {
    $null = Get-Command npm -ErrorAction Stop
    if ($DryRun) {
        Write-Host "[SIMULACION] npm cache clean --force"
    } else {
        npm cache clean --force | Out-Null
        Write-Host "OK  npm cache limpiada."
    }
} catch {
    Write-Host "npm no encontrado; omitido."
}

# --- CrashDumps -------------------------------------------------------------------
Write-Header "CrashDumps"
Remove-Safe (Join-Path -Path $env:LOCALAPPDATA -ChildPath "CrashDumps")

# --- Postman (SOLO cache/logs en Local) ------------------------------------------
Write-Header "Postman (Local: cache y logs)"
$postmanLocal = Join-Path -Path $env:LOCALAPPDATA -ChildPath "Postman"

if (Test-Path -LiteralPath $postmanLocal) {
    # Construye rutas de forma individual para evitar arrays en -ChildPath
    $pmNames   = @('Cache','GPUCache','Code Cache','logs')
    $pmTargets = foreach ($n in $pmNames) { Join-Path -Path $postmanLocal -ChildPath $n }
    $pmTargets | ForEach-Object { Remove-Safe $_ }
} else {
    Write-Verbose "Postman local no existe en: $postmanLocal"
}

Write-Host "`nHecho. Si quieres aplicar cambios reales, ejecuta con -Apply."



@echo off
SETLOCAL ENABLEDELAYEDEXPANSION
rem Ejecuta la simulación (no borra nada). No requiere elevación.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ClearNuGetCaches_SAFE.ps1"
if errorlevel 1 (
  echo.
  echo [!] Ocurrio un error durante la simulacion.
  pause
)
ENDLOCAL



@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

:: Elevar a Administrador si no lo somos
net session >nul 2>&1
if %errorlevel% NEQ 0 (
  echo Solicitando privilegios de administrador...
  powershell -NoProfile -Command "Start-Process -Verb RunAs -FilePath '%~f0'"
  goto :eof
)

echo.
echo Ejecutando LIMPIEZA REAL (usuario actual). No se borran sesiones ni perfiles.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ClearNuGetCaches_SAFE.ps1" -Apply

if errorlevel 1 (
  echo.
  echo [!] Ocurrio un error durante la ejecucion.
  pause
)
ENDLOCAL


    
