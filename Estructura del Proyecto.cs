<# 
.SYNOPSIS
  Limpieza segura de cachés (NuGet, npm, CrashDumps, Postman-cache) en TODOS los perfiles.

.DESCRIPTION
  - Solo borra cachés en AppData\Local y .nuget\packages por cada usuario.
  - NO borra %AppData%\Roaming (sesiones, colecciones) NI Microsoft/Google/GitHubDesktop.
  - Omite carpetas en C:\Users que no sean perfiles reales (sin AppData\Local).
  - Por defecto corre en simulación (Dry-Run). Usa -Apply para ejecutar.
#>

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
  [switch]$Apply,
  [switch]$IncludeServiceProfiles,
  [switch]$VerboseMode
)

$ErrorActionPreference = 'Stop'
if ($VerboseMode) { $VerbosePreference = 'Continue' } else { $VerbosePreference = 'SilentlyContinue' }
$DryRun = -not $Apply

function Write-Header([string]$text) { Write-Host "`n=== $text ===" }

function Remove-Safe([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) { Write-Verbose "No existe: $Path"; return }
  if ($DryRun) { Write-Host "[SIMULACION] Eliminar: $Path"; return }
  try {
    if ($PSCmdlet.ShouldProcess($Path, "Remove-Item")) {
      Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
      Write-Host ("OK  {0}" -f $Path)
    }
  } catch {
    Write-Warning ("No se pudo eliminar {0}: {1}" -f $Path, $_.Exception.Message)
  }
}

# Combina segmentos de ruta evitando Join-Path (compatible con PS antiguos)
function Join-Parts {
  param([Parameter(Mandatory)][string]$Base, [Parameter(Mandatory)][string[]]$Parts)
  $p = $Base
  foreach ($seg in $Parts) { $p = [System.IO.Path]::Combine($p, $seg) }
  return $p
}

Write-Header "Limpieza segura (TODOS los usuarios)"
Write-Host "Dry-Run: $DryRun  |  Verbose: $VerboseMode  |  ServiceProfiles: $IncludeServiceProfiles"
Write-Host "Solo cachés: NuGet (.nuget\\packages y Local\\NuGet\\*), npm-cache, CrashDumps, Postman(Local: Cache/GPUCache/Code Cache/logs)"
Write-Host "Protegido: NO se toca %AppData%\\Roaming ni Microsoft/Google/GitHubDesktop."

# --- Perfiles a procesar ----------------------------------------------------------
$roots = @('C:\Users'); if ($IncludeServiceProfiles) { $roots += 'C:\Windows\ServiceProfiles' }
$excludeNames = @('Default','Default User','Public','All Users')

$userHomes = foreach ($r in $roots) {
  if (Test-Path $r) {
    Get-ChildItem -Path $r -Directory -Force -ErrorAction SilentlyContinue |
      Where-Object {
        $excludeNames -notcontains $_.Name -and
        (Test-Path (Join-Parts -Base $_.FullName -Parts @('AppData','Local'))) # Solo perfiles reales
      } |
      Select-Object -ExpandProperty FullName
  }
}

foreach ($userPath in $userHomes) {
  Write-Header "Perfil: $userPath"

  # NuGet
  $nugetPaths = @(
    (Join-Parts -Base $userPath -Parts @('.nuget','packages')),
    (Join-Parts -Base $userPath -Parts @('AppData','Local','NuGet','Cache')),
    (Join-Parts -Base $userPath -Parts @('AppData','Local','NuGet','v3-cache')),
    (Join-Parts -Base $userPath -Parts @('AppData','Local','NuGet','v2-cache'))
  )
  $nugetPaths | ForEach-Object { Remove-Safe $_ }

  # npm cache
  Remove-Safe (Join-Parts -Base $userPath -Parts @('AppData','Local','npm-cache'))

  # CrashDumps
  Remove-Safe (Join-Parts -Base $userPath -Parts @('AppData','Local','CrashDumps'))

  # Postman (solo caches en Local)
  $postmanLocal = Join-Parts -Base $userPath -Parts @('AppData','Local','Postman')
  if (Test-Path -LiteralPath $postmanLocal) {
    foreach ($n in @('Cache','GPUCache','Code Cache','logs')) {
      Remove-Safe (Join-Parts -Base $postmanLocal -Parts @($n))
    }
  } else {
    Write-Verbose "Postman local no existe: $postmanLocal"
  }
}

Write-Host "`nHecho. Para aplicar realmente, ejecuta con -Apply (PowerShell como Administrador)."
