# 0016 — Bilgisayarda kalanları kaldır; sıfırdan test hazır mı, Pi'de site çalışıyor mu

## İstek
bilgisayarda yüklü bir şey kaldıysa kaldır sıfırdan üyelik deneyeceğim ve indirip test edeceğim bu süreç hazır mı?
  raspide site çalışıyor mu

## Cevap
Bilgisayarda Taha Konsept'ten kurulu bir şey yok, kontrol edildi:
- Kurulu program yok: program klasörü, veri klasörü (%LOCALAPPDATA%\TahaKonsept), Başlat menüsü, masaüstü, açılışta başlama ve kaldırma kaydı boş.
- Çalışan bir süreç de yok.
- Geliştirme sırasında kalan tek iz Velopack'in log dosyasıydı (%LOCALAPPDATA%\velopack), silindi.
- Repo içindeki .tools-dev (benim test ortamım) kurulu programla paylaşılmıyor, yerinde bırakıldı.
Pi çalışıyor (0.3.0, veritabanı hazır), kontrol edildi:
- /Kurulum 200 dönüyor; hiç hesap yok, ilk üyelik sana açık.
- Kurulum dosyası 200 dönüyor (91 MB), releases.win.json da 200.
Akış: http://192.168.1.102:7110 → hesap oluştur → Başlangıç → indir, kur → programdaki kodu Başlangıç'a yaz → anahtar, profil, konsept, video.
Not: kurulum dosyası imzasız, Windows SmartScreen "Ek bilgi → Yine de çalıştır" diyebilir. İlk açılışta araçlar (ffmpeg, node, Chrome, ses ve görsel modelleri) indirileceği için ilk video uzun sürer.
