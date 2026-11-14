<#
.SYNOPSIS
    Geliştirme için Pi'deki PostgreSQL'e SSH tüneli açar: localhost:5433 → Pi localhost:5432.

.DESCRIPTION
    appsettings.Development.json bağlantısı Port=5433 kullanır. Tünel ön planda çalışır (Ctrl+C ile kapanır);
    -Background ile arka planda başlatılır. Port zaten dinleniyorsa yeni tünel açmaz.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\dev-tunnel.ps1
.EXAMPLE
    powershell -File scripts\dev-tunnel.ps1 -Background
#>
[CmdletBinding()]
param(
    [string]$SshHost = 'tmpi',
    [int]$LocalPort = 5433,
    [int]$RemotePort = 5432,
    [switch]$Background
)

$ErrorActionPreference = 'Stop'

function Test-LocalPort([int]$port) {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect('127.0.0.1', $port, $null, $null)
        return ($async.AsyncWaitHandle.WaitOne(500) -and $client.Connected)
    }
    catch { return $false }
    finally { $client.Close() }
}

if (Test-LocalPort $LocalPort) {
    Write-Host "localhost:$LocalPort zaten dinleniyor (tünel açık olabilir); yeni tünel açılmadı." -ForegroundColor Yellow
    exit 0
}

$sshArgs = @(
    '-N',
    '-o', 'BatchMode=yes',
    '-o', 'ExitOnForwardFailure=yes',
    '-o', 'ServerAliveInterval=30',
    '-o', 'ServerAliveCountMax=3',
    '-L', "${LocalPort}:localhost:${RemotePort}",
    $SshHost
)

if ($Background) {
    $proc = Start-Process -FilePath 'ssh' -ArgumentList $sshArgs -WindowStyle Hidden -PassThru
    for ($i = 0; $i -lt 20 -and -not (Test-LocalPort $LocalPort); $i++) { Start-Sleep -Milliseconds 500 }
    if (-not (Test-LocalPort $LocalPort)) { throw "Tünel açılamadı (ssh PID $($proc.Id))." }
    Write-Host "Tünel açık: localhost:$LocalPort → ${SshHost}:$RemotePort (ssh PID $($proc.Id); kapatmak için: Stop-Process -Id $($proc.Id))" -ForegroundColor Green
    exit 0
}

Write-Host "Tünel: localhost:$LocalPort → ${SshHost}:$RemotePort (Ctrl+C ile kapatın)" -ForegroundColor Green
& ssh @sshArgs
exit $LASTEXITCODE
