# 0021 — Bilgisayardan ve Pi'den kurulumları kaldır

**Tarih:** 2026-11-16 11:00

## İstek
> bilgisayardan ve raspiden kurulumları kaldırır mısın

Kapsam soruldu → cevap: bilgisayarda **her şeyi sil**, Pi'de **her şeyi sil**.

## Yapılan
**Bilgisayar:** Velopack kaldırıcı (`Update.exe --uninstall --silent`, çıkış 0); `%LOCALAPPDATA%\TahaKonsept` (ayarlar, eşleme, loglar, videolar, modeller, Python ortamları, araçlar), velopack logu silindi. Kaldırıcının bıraktığı `HKCU\...\Run\TahaKonsept.Agent` başlangıç kaydı elle silindi. Kontrol: klasör/kısayol/kayıt kalmadı.

**Pi:** servisler kapatıldı + unit'ler silindi, `/opt/tahakonsept`, `/var/lib/tahakonsept`, `tahakonsept` + `tahakonsept_e2e` DB'leri, `tahakonsept` rolü, sistem kullanıcısı/grubu silindi. Kontrol: 7110/7111 boş, DB/rol/kullanıcı yok; PostgreSQL çalışıyor.

## Bulunan hata ve düzeltme
Kaldırma, Windows başlangıç kaydını bırakıyordu (silinmiş exe'yi gösteren kayıt). `Program.cs`: `VelopackApp.Build().OnBeforeUninstallFastCallback(...)` ile kaldırma öncesi `RegistryStartupRegistration.SetEnabled(false)`. Derleme 0 hata. Bir sonraki ajan paketinde çıkar.
