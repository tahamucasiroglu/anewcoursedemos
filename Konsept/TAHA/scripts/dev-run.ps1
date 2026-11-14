<#
.SYNOPSIS
    Api (7111) ve Web (7110) host'larını yerelde Development ortamında başlatır.

.DESCRIPTION
    - Gerekirse PostgreSQL tünelini (scripts/dev-tunnel.ps1 -Background) açar (-NoTunnel ile atlanır).
    - Her host'u ayrı bir pencerede 'dotnet run' ile çalıştırır (pencereyi kapatmak host'u durdurur).
    - -Wait verilirse bu pencerede bekler ve Ctrl+C ile iki host'u da kapatır.
    Gizli değerler TAHA/.env dosyasından (TAHAKONSEPT_DB_PASSWORD) Development'ta otomatik okunur.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\dev-run.ps1
.EXAMPLE
    powershell -File scripts\dev-run.ps1 -Wait -NoTunnel
#>
[CmdletBinding()]
param(
    [switch]$NoTunnel,
    [switch]$Wait,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

if (-not $NoTunnel) {
    & (Join-Path $PSScriptRoot 'dev-tunnel.ps1') -Background
}

$shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { 'pwsh' } else { 'powershell' }
$hosts = @(
    @{ Name = 'Api'; Project = 'src\Hosts\TahaKonsept.Api\TahaKonsept.Api.csproj'; Url = 'http://localhost:7111/api/health' },
    @{ Name = 'Web'; Project = 'src\Hosts\TahaKonsept.Web\TahaKonsept.Web.csproj'; Url = 'http://localhost:7110/' }
)

$processes = @()
foreach ($h in $hosts) {
    $project = Join-Path $root $h.Project
    $command = "`$host.UI.RawUI.WindowTitle = 'Taha Konsept $($h.Name)'; `$env:ASPNETCORE_ENVIRONMENT = 'Development'; dotnet run --project '$project' -c $Configuration --launch-profile 'TahaKonsept.$($h.Name)'"
    $processes += Start-Process -FilePath $shell -ArgumentList @('-NoExit', '-NoProfile', '-Command', $command) -WorkingDirectory $root -PassThru
    Write-Host "$($h.Name) başlatıldı (PID $($processes[-1].Id)) → $($h.Url)" -ForegroundColor Green
}

if ($Wait) {
    Write-Host 'Çalışıyor. Ctrl+C ile iki host da kapatılır.' -ForegroundColor Cyan
    try {
        while ($true) { Start-Sleep -Seconds 1 }
    }
    finally {
        foreach ($p in $processes) {
            if (-not $p.HasExited) {
                & taskkill /PID $p.Id /T /F | Out-Null
            }
        }
        Write-Host 'Host pencereleri kapatıldı.' -ForegroundColor Yellow
    }
}
