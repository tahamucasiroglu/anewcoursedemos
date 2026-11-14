# e2e temizliği: "Sistem oynatıcısı" ve "Klasörde aç" komutlarının açtığı pencereleri kapatır (masaüstünde iz bırakmamak için).
Get-Process Microsoft.Media.Player, Video.UI, vlc, wmplayer -ErrorAction SilentlyContinue | Stop-Process -Force
$sh = New-Object -ComObject Shell.Application
foreach ($w in @($sh.Windows())) {
    try { if ($w.LocationURL -like '*e2e-agent*Videos*' -or $w.LocationName -eq 'Videos') { $w.Quit() } } catch { }
}
