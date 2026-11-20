# Yerel ses -> metin servisini başlatır (spec 0012, AC-10). Ön planda çalışır; Ctrl+C ile durur.
# Varsayılan yalnızca bu bilgisayar (127.0.0.1:8765). Yerel ağ demosu: -ListenHost 0.0.0.0 (tools/ai/README.md —
# Windows güvenlik duvarında yalnızca Pi'nin adresine izin verin).
param(
    [string]$ListenHost = "127.0.0.1",
    [int]$Port = 8765,
    [string]$Model = "medium",
    [ValidateSet("auto", "cuda", "cpu")][string]$Device = "auto"
)

$ErrorActionPreference = "Stop"
$python = Join-Path $PSScriptRoot ".venv\Scripts\python.exe"
if (-not (Test-Path $python)) {
    Write-Error "tools/ai/.venv bulunamadı — kurulum: tools/ai/README.md"
    exit 1
}

$env:STT_HOST = $ListenHost
$env:STT_PORT = "$Port"
$env:STT_MODEL = $Model
$env:STT_DEVICE = $Device
$env:PYTHONIOENCODING = "utf-8"
& $python -I (Join-Path $PSScriptRoot "stt_server.py")
exit $LASTEXITCODE
