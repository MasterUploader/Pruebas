<# 
.SYNOPSIS
  Limpieza segura de cachés (NuGet, npm, Postman, CrashDumps) en TODOS los perfiles.

.DESCRIPTION
  - Solo borra cachés en AppData\Local y .nuget\packages por cada usuario.
  - NO borra %AppData%\Roaming (sesiones, colecciones), NI carpetas Microsoft/Google/GitHubDesktop.
  - Requiere elevación para afectar otros perfiles.
  - Por defecto corre en simulación (Dry-Run). Usa -Apply para ejecutar.

.PARAMETER Apply
  Ejecuta la limpieza real.

.PARAMETER IncludeServiceProfiles
  Incluye C:\Windows\ServiceProfiles (LocalService, NetworkService).

.PARAMETER VerboseMode
  Traza detallada.
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

Write-Header "Limpieza segura (TODOS los usuarios)"
Write-Host "Dry-Run: $DryRun  |  Verbose: $VerboseMode  |  ServiceProfiles: $IncludeServiceProfiles"
Write-Host "Solo cachés: NuGet (.nuget\\packages y Local\\NuGet\\*), npm-cache, CrashDumps, Postman(Local: Cache/GPUCache/Code Cache/logs)"
Write-Host "Protegido: NO se toca %AppData%\\Roaming ni carpetas Microsoft/Google/GitHubDesktop."

# --- Recolectar perfiles ----------------------------------------------------------
$roots = @('C:\Users')
if ($IncludeServiceProfiles) { $roots += 'C:\Windows\ServiceProfiles' }

$exclude = @('Default','Default User','Public','All Users')
$userHomes = foreach ($r in $roots) {
  if (Test-Path $r) {
    Get-ChildItem -Path $r -Directory -Force -ErrorAction SilentlyContinue |
      Where-Object { $exclude -notcontains $_.Name } |
      Select-Object -ExpandProperty FullName
  }
}

# --- Limpiar por usuario ----------------------------------------------------------
foreach ($userPath in $userHomes) {
  Write-Header "Perfil: $userPath"

  # NuGet (carpetas de cache del usuario)
  $nugetPaths = @(
    Join-Path -Path $userPath -ChildPath ".nuget\packages",
    Join-Path -Path $userPath -ChildPath "AppData\Local\NuGet\Cache",
    Join-Path -Path $userPath -ChildPath "AppData\Local\NuGet\v3-cache",
    Join-Path -Path $userPath -ChildPath "AppData\Local\NuGet\v2-cache"
  )
  $nugetPaths | ForEach-Object { Remove-Safe $_ }

  # npm cache
  $npmCache = Join-Path -Path $userPath -ChildPath "AppData\Local\npm-cache"
  Remove-Safe $npmCache

  # CrashDumps
  $crashDumps = Join-Path -Path $userPath -ChildPath "AppData\Local\CrashDumps"
  Remove-Safe $crashDumps

  # Postman (solo caches en Local)
  $postmanLocal = Join-Path -Path $userPath -ChildPath "AppData\Local\Postman"
  if (Test-Path -LiteralPath $postmanLocal) {
    foreach ($n in @('Cache','GPUCache','Code Cache','logs')) {
      $p = Join-Path -Path $postmanLocal -ChildPath $n
      Remove-Safe $p
    }
  } else {
    Write-Verbose "Postman local no existe: $postmanLocal"
  }
}

Write-Host "`nHecho. Para aplicar realmente, ejecuta con -Apply (PowerShell como Administrador)."
