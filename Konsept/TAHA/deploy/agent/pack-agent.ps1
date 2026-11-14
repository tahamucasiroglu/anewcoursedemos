<#
.SYNOPSIS
  Taha Konsept masaüstü ajanını (Avalonia, Windows) Velopack ile paketler ve kurulum dosyasını web'in indirme klasörüne kopyalar.
.DESCRIPTION
  1) dotnet publish -c Release -r win-x64 --self-contained  → artifacts/agent/publish
  2) dotnet tool restore + vpk pack (packId TahaKonsept.Agent, sürüm kökteki VERSION dosyasından, ikon Assets/app.ico)
     → artifacts/agent/releases (Setup.exe, full nupkg, releases.win.json; önceki sürüm varsa delta)
  3) Kopyalar → src/Hosts/TahaKonsept.Web/wwwroot/downloads/
       TahaKonsept.Agent-win-Setup.exe  (Ayarlar → Masaüstü Programı → İndir)
       releases.win.json + *.nupkg      (uygulama içi güncelleme kaynağı: <web>/downloads/)
  Bu dosyalar .gitignore ile depo dışında tutulur.
.PARAMETER Version
  Paket sürümü (varsayılan: VERSION dosyası).
.PARAMETER SkipPublish
  Yayınlamayı atla (artifacts/agent/publish hazırsa).
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File deploy/agent/pack-agent.ps1
#>
param(
    [string]$Version,
    [switch]$SkipPublish
)
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$project = Join-Path $root 'src\Desktop\TahaKonsept.Agent\TahaKonsept.Agent.csproj'
$icon = Join-Path $root 'src\Desktop\TahaKonsept.Agent\Assets\app.ico'
$publishDir = Join-Path $root 'artifacts\agent\publish'
$releaseDir = Join-Path $root 'artifacts\agent\releases'
$downloadsDir = Join-Path $root 'src\Hosts\TahaKonsept.Web\wwwroot\downloads'
$packId = 'TahaKonsept.Agent'

if (-not $Version) { $Version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim() }
Write-Host "Taha Konsept ajanı paketleniyor: $packId $Version"

if (-not (Test-Path $icon)) {
    Write-Host 'İkon yok, geçici ikon üretiliyor…'
    & (Join-Path $PSScriptRoot 'make-icon.ps1')
}

Push-Location $root
try {
    if (-not $SkipPublish) {
        if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
        dotnet publish $project -c Release -r win-x64 --self-contained true -o $publishDir -p:Version=$Version
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish başarısız ($LASTEXITCODE)" }
    }

    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore başarısız ($LASTEXITCODE)" }

    New-Item -ItemType Directory -Force $releaseDir | Out-Null
    dotnet vpk pack `
        --packId $packId `
        --packVersion $Version `
        --runtime win-x64 `
        --packDir $publishDir `
        --mainExe 'TahaKonsept.Agent.exe' `
        --packTitle 'Taha Konsept' `
        --packAuthors 'Taha Konsept' `
        --icon $icon `
        --outputDir $releaseDir
    if ($LASTEXITCODE -ne 0) { throw "vpk pack başarısız ($LASTEXITCODE)" }

    $setup = Join-Path $releaseDir "$packId-win-Setup.exe"
    if (-not (Test-Path $setup)) { throw "Setup.exe bulunamadı: $setup" }

    New-Item -ItemType Directory -Force $downloadsDir | Out-Null
    Copy-Item $setup (Join-Path $downloadsDir "$packId-win-Setup.exe") -Force
    Get-ChildItem $releaseDir -File | Where-Object { $_.Name -like 'releases.*.json' -or $_.Name -like 'assets.*.json' -or $_.Name -like 'RELEASES*' -or $_.Name -like "$packId-$Version-*.nupkg" } |
        ForEach-Object { Copy-Item $_.FullName (Join-Path $downloadsDir $_.Name) -Force }

    $size = (Get-Item (Join-Path $downloadsDir "$packId-win-Setup.exe")).Length
    Write-Host ("Kurulum hazır: {0} ({1:N1} MB)" -f (Join-Path $downloadsDir "$packId-win-Setup.exe"), ($size / 1MB))
}
finally {
    Pop-Location
}
