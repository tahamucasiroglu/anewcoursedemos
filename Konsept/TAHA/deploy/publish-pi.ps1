<#
.SYNOPSIS
    Taha Konsept'i Raspberry Pi'ye yayınlar (Api + Web, self-contained linux-arm64).

.DESCRIPTION
    1. VERSION dosyasını okur, sürüm adı üretir: <sürüm>-<yyyyMMdd-HHmmss>.
    2. Her iki host'u 'dotnet publish -c Release -r linux-arm64 --self-contained' ile yayınlar
       (ayrı --artifacts-path: diğer derlemelerin obj/bin klasörlerine dokunmaz).
    3. api/ + web/ + pi/ (kurulum betiği, unit'ler, rollback) klasörlerini tek tar.gz yapar.
    4. scp ile Pi'ye kopyalar, deploy/pi/install-release.sh'ı sudo ile çalıştırır:
       /opt/tahakonsept/releases/<sürüm-adı>, current sembolik bağı, önceki sürüm geri dönüş için saklanır.
    5. Bu bilgisayardan http://<PiAddress>:7111/api/health ve http://<PiAddress>:7110/ doğrular.

.EXAMPLE
    pwsh -File deploy/publish-pi.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File deploy\publish-pi.ps1 -Source Head      # son commit'i yayınla
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File deploy\publish-pi.ps1 -SkipPublish   # son yayını yeniden paketle/kur
#>
[CmdletBinding()]
param(
    # ~/.ssh/config içindeki Pi takma adı (BatchMode, parolasız sudo).
    [string]$SshHost = 'tmpi',
    # Bu bilgisayardan doğrulama için Pi'nin LAN adresi.
    [string]$PiAddress = '192.168.1.102',
    # Yayınlamayı atla, deploy/.out/last altındaki son çıktıyı kullan.
    [switch]$SkipPublish,
    # Kurulumdan sonra uzaktan doğrulamayı atla.
    [switch]$SkipVerify,
    # Kaynak: WorkingTree (çalışma klasörü, varsayılan) ya da Head (son commit; 'git archive' ile temiz kopya —
    # paralel yarım kalmış değişiklikler yayını bozmasın).
    [ValidateSet('WorkingTree', 'Head')]
    [string]$Source = 'WorkingTree'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$outRoot = Join-Path $PSScriptRoot '.out'
$version = (Get-Content -Raw (Join-Path $root 'VERSION')).Trim()
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$release = "$version-$stamp"
$bundle = Join-Path $outRoot 'last'
$tarName = "tahakonsept-$release.tar.gz"
$tarPath = Join-Path $outRoot $tarName

function Write-Step([string]$message) { Write-Host "==> $message" -ForegroundColor Cyan }

function Invoke-Native([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { throw "$exe başarısız (çıkış kodu $LASTEXITCODE): $($arguments -join ' ')" }
}

New-Item -ItemType Directory -Force $outRoot | Out-Null

$srcRoot = $root
if (-not $SkipPublish -and $Source -eq 'Head') {
    $srcRoot = Join-Path $outRoot 'src'
    Write-Step "Kaynak: HEAD ($(git -C $root rev-parse --short HEAD)) → $srcRoot"
    if (Test-Path $srcRoot) { Remove-Item -Recurse -Force $srcRoot }
    New-Item -ItemType Directory -Force $srcRoot | Out-Null
    $archive = Join-Path $outRoot 'head.tar'
    Invoke-Native 'git' @('-C', $root, 'archive', '--format=tar', '-o', $archive, 'HEAD:Konsept/TAHA')
    Invoke-Native 'tar' @('-xf', $archive, '-C', $srcRoot)
    Remove-Item -Force $archive
}

if (-not $SkipPublish) {
    if (Test-Path $bundle) { Remove-Item -Recurse -Force $bundle }
    $hosts = @(
        @{ Name = 'api'; Project = 'src/Hosts/TahaKonsept.Api/TahaKonsept.Api.csproj' },
        @{ Name = 'web'; Project = 'src/Hosts/TahaKonsept.Web/TahaKonsept.Web.csproj' }
    )
    foreach ($h in $hosts) {
        Write-Step "dotnet publish $($h.Name) ($release, linux-arm64, self-contained)"
        Invoke-Native 'dotnet' @(
            'publish', (Join-Path $srcRoot $h.Project),
            '-c', 'Release', '-r', 'linux-arm64', '--self-contained', 'true',
            '--artifacts-path', (Join-Path $outRoot 'build'),
            '-o', (Join-Path $bundle $h.Name),
            '-p:DebugType=None', '-p:DebugSymbols=false',
            '-nologo', '-v', 'minimal')
    }
}
else {
    if (-not (Test-Path (Join-Path $bundle 'api'))) { throw "Önceki yayın yok: $bundle" }
    Write-Step "Yayınlama atlandı, $bundle kullanılıyor"
}

# Yerel geliştirme kalıntıları pakete girmesin (logs, App_Data anahtarları).
foreach ($name in 'api', 'web') {
    foreach ($junk in 'logs', 'App_Data') {
        $p = Join-Path (Join-Path $bundle $name) $junk
        if (Test-Path $p) { Remove-Item -Recurse -Force $p }
    }
}

# Pi dosyaları: satır sonlarını LF'ye çevirerek kopyala (bash CRLF ile bozulur).
Write-Step 'Pi betikleri ve unit dosyaları hazırlanıyor'
$piOut = Join-Path $bundle 'pi'
if (Test-Path $piOut) { Remove-Item -Recurse -Force $piOut }
New-Item -ItemType Directory -Force $piOut | Out-Null
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
Get-ChildItem (Join-Path $PSScriptRoot 'pi') -File | ForEach-Object {
    $text = [System.IO.File]::ReadAllText($_.FullName) -replace "`r`n", "`n"
    [System.IO.File]::WriteAllText((Join-Path $piOut $_.Name), $text, $utf8NoBom)
}

Write-Step "Paket: $tarPath"
Get-ChildItem $outRoot -Filter 'tahakonsept-*.tar.gz' | Remove-Item -Force
Invoke-Native 'tar' @('-czf', $tarPath, '-C', $bundle, 'api', 'web', 'pi')
$sizeMb = [math]::Round((Get-Item $tarPath).Length / 1MB, 1)
Write-Host "    $sizeMb MB"

Write-Step "Pi'ye kopyalanıyor ($SshHost)"
$remoteTar = "/tmp/$tarName"
$remoteDir = "/tmp/tahakonsept-deploy-$release"
Invoke-Native 'scp' @('-q', '-o', 'BatchMode=yes', $tarPath, "${SshHost}:$remoteTar")

Write-Step 'Uzak kurulum (install-release.sh)'
# Tek satır (ssh argümanı olarak satır sonu/CRLF sorunu olmasın); yalnız tek tırnak kullanılır.
$remoteScript = (
    "set -u",
    "rm -rf '$remoteDir' && mkdir -p '$remoteDir' && tar -xzf '$remoteTar' -C '$remoteDir' || exit 1",
    "sudo bash '$remoteDir/pi/install-release.sh' '$release' '$remoteDir'",
    'rc=$?',
    "sudo rm -rf '$remoteDir' '$remoteTar'",
    'exit $rc'
) -join '; '
Invoke-Native 'ssh' @('-o', 'BatchMode=yes', $SshHost, $remoteScript)

if (-not $SkipVerify) {
    Write-Step "Bu bilgisayardan doğrulama ($PiAddress)"
    $health = Invoke-RestMethod -Uri "http://${PiAddress}:7111/api/health" -TimeoutSec 15
    Write-Host ("    api/health: status={0} version={1} environment={2} database={3}" -f $health.status, $health.version, $health.environment, $health.database)
    $web = Invoke-WebRequest -Uri "http://${PiAddress}:7110/" -TimeoutSec 15 -UseBasicParsing -MaximumRedirection 5
    Write-Host "    web /: HTTP $($web.StatusCode)"
}

Write-Step "Tamam: $release"
