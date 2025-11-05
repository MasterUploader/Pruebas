<# Limpieza segura de cachés (NuGet, npm, CrashDumps, Postman-cache) en TODOS los perfiles.
   - Solo AppData\Local y .nuget\packages de cada usuario
   - NO toca %AppData%\Roaming ni Microsoft/Google/GitHubDesktop
   - Simulación por defecto; usa -Apply para ejecutar real
#>

[CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='Low')]
param(
  [switch]$Apply,
  [switch]$IncludeServiceProfiles,
  [switch]$VerboseMode
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = $VerboseMode ? 'Continue' : 'SilentlyContinue'
$DryRun = -not $Apply

function Write-Header([string]$t){ Write-Host "`n=== $t ===" }

function Remove-Safe([string]$Path){
  if (-not [System.IO.Directory]::Exists($Path) -and -not [System.IO.File]::Exists($Path)) {
    Write-Verbose "No existe o no accesible: $Path"; return
  }
  if ($DryRun){ Write-Host "[SIMULACION] Eliminar: $Path"; return }
  try{
    if ($PSCmdlet.ShouldProcess($Path,"Remove-Item")){
      Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
      Write-Host ("OK  {0}" -f $Path)
    }
  }catch{
    Write-Warning ("No se pudo eliminar {0}: {1}" -f $Path, $_.Exception.Message)
  }
}

# Combinar rutas (evita Join-Path con arrays y PS antiguos)
function Join-Parts{ param([string]$Base,[string[]]$Parts)
  $p=$Base; foreach($seg in $Parts){ $p=[System.IO.Path]::Combine($p,$seg) }; return $p
}

# ---------- Detección de perfiles reales ----------
$roots=@('C:\Users'); if($IncludeServiceProfiles){ $roots+='C:\Windows\ServiceProfiles' }
$ex=@('Default','Default User','Public','All Users')

$users=@()
foreach($r in $roots){
  if(-not [System.IO.Directory]::Exists($r)){ continue }
  foreach($d in (Get-ChildItem -Path $r -Directory -Force -ErrorAction SilentlyContinue)){
    if($ex -contains $d.Name){ continue }
    $local = Join-Parts -Base $d.FullName -Parts @('AppData','Local')
    # Usa Directory.Exists → devuelve False si no hay acceso o no existe
    if([System.IO.Directory]::Exists($local)){
      $users += $d.FullName
    } else {
      Write-Verbose "Omitido (no perfil real o sin acceso): $($d.FullName)"
    }
  }
}

Write-Header "Limpieza segura (TODOS los usuarios)"
Write-Host "Dry-Run: $DryRun | Verbose: $VerboseMode | ServiceProfiles: $IncludeServiceProfiles"
Write-Host "Acciones: NuGet (.nuget\\packages, Local\\NuGet\\*), npm-cache, CrashDumps, Postman(Local cache)"
Write-Host "Protegido: NO Roaming ni Microsoft/Google/GitHubDesktop."

# ---------- Limpieza por usuario ----------
foreach($userPath in $users){
  Write-Header "Perfil: $userPath"

  # NuGet
  $nuget=@(
    (Join-Parts -Base $userPath -Parts @('.nuget','packages')),
    (Join-Parts -Base $userPath -Parts @('AppData','Local','NuGet','Cache')),
    (Join-Parts -Base $userPath -Parts @('AppData','Local','NuGet','v3-cache')),
    (Join-Parts -Base $userPath -Parts @('AppData','Local','NuGet','v2-cache'))
  ); $nuget | ForEach-Object { Remove-Safe $_ }

  # npm cache
  Remove-Safe (Join-Parts -Base $userPath -Parts @('AppData','Local','npm-cache'))

  # CrashDumps
  Remove-Safe (Join-Parts -Base $userPath -Parts @('AppData','Local','CrashDumps'))

  # Postman (solo cache/logs en Local)
  $pmLocal = Join-Parts -Base $userPath -Parts @('AppData','Local','Postman')
  if([System.IO.Directory]::Exists($pmLocal)){
    foreach($n in @('Cache','GPUCache','Code Cache','logs')){
      Remove-Safe (Join-Parts -Base $pmLocal -Parts @($n))
    }
  } else {
    Write-Verbose "Postman local no existe/accesible: $pmLocal"
  }
}

Write-Host "`nHecho. Para ejecutar realmente, usa:  .\ClearNuGetCaches_SAFE.ps1 -Apply  (como Administrador)."
