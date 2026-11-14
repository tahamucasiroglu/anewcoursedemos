# Kararlar Günlüğü

| Tarih | Karar | Kaynak |
|---|---|---|
| 2026-11-14 | Proje adı **Taha Konsept**; çalışma klasörü `Konsept/TAHA`. | prompt 0001 |
| 2026-11-14 | Klasörler: `promptlar/`, `source/`, `memory/`, `raporlar/`. | prompt 0001 |
| 2026-11-14 | Sunucu: yerel Raspberry Pi 5 8 GB (Debian 13 arm64; PostgreSQL 17 kurulu, .NET/Docker yok). | prompt 0001 + kontrol |
| 2026-11-14 | Veritabanı: PostgreSQL (kullanıcı tavsiyesi; Pi'de zaten kurulu). | prompt 0001 |
| 2026-11-14 | Stack: .NET — Avalonia masaüstü ajanı, ASP.NET Core MVC web (jQuery Ajax, fetch yok), ASP.NET Core Web API. | prompt 0001 |
| 2026-11-14 | Mimari: modüler monolit + modül başına Clean Architecture; interface→abstract→concrete; dosya başına tek nesne. | prompt 0001 |
| 2026-11-14 | Versiyon 0.1.0'dan başlar; 1.0.0 = testleriyle tam çalışır. | prompt 0001 |
| 2026-11-14 | Profil sayfası üst menüden kalktı → ✏️/➕ modal; yerine "Yayınlama" (çok yakında). | prompt 0001 |
| 2026-11-14 | Açık kaynak, **ticari değil**; herkes kendi Pi'sine kurar; Pi dışarı açılmaz (yerel ağ). Çok kullanıcılı yapı yine var. | prompt 0001 soru-cevap |
| 2026-11-14 | BYOK: her kullanıcı kendi API key'i; tüm online çağrılar Pi'deki API'den. | prompt 0001 soru-cevap |
| 2026-11-14 | Videolar kullanıcının PC'sinde; web oynatıcı ajandan oynatır. | prompt 0001 soru-cevap |
| 2026-11-14 | Chatterbox (ses klonlama) ilk sürümde var (isteğe bağlı gömülü Python+CUDA bileşeni). | prompt 0001 soru-cevap |
| 2026-11-14 | Kapsam: "hepsi tam olacak" — tüm özellikler 1.0.0'da; fazlar sadece sıra. Plan: `PLAN.md`. | prompt 0001 soru-cevap |
| 2026-11-14 | `source/` git'e girer (TAHA/.gitignore `!source/`), büyük ikililer hariç. | prompt 0001 soru-cevap |
| 2026-11-14 | Varsayılan teknik seçimler (rapor 0001): HyperFrames, ffmpeg ASS altyazı aşaması, Supertonic 3, sd.cpp, SignalR (ajan), jQuery polling (tarayıcı), Velopack, .NET 10, MediatR yok. Portlar 7110 web / 7111 API. | rapor 0001 |
| 2026-11-14 | **Tek kullanıcı (sadece ben), şahsi, ticari değil.** Pi sadece yerelde. Kişisel kullanıma izin veren tüm ses ve görsel modelleri eklenecek. Kayıt kapalı, kurulumda tek hesap. | prompt 0002 |
| 2026-11-14 | Türkçe okunuş düzeltme: Normalize aşaması (yazılış ≠ okunuş; sözlük, sayı, tarih, romen, kısaltma, LLM önerisi). Örn. Google → Gugıl. | prompt 0002 |
| 2026-11-14 | Pi deploy: **self-contained linux-arm64** (Pi'de .NET runtime yok; mevcut projelerle aynı kalıp) → `/opt/tahakonsept`, veri `/var/lib/tahakonsept`, kullanıcı `tahakonsept`, DB/rol `tahakonsept` (oluşturuldu). DB parolası `TAHA/.env` (git dışı). Geliştirmede PG tüneli `ssh -L 5433:localhost:5432 tmpi`. | prompt 0003 |
| 2026-11-14 | Bağlayıcı teknik sözleşme: `docs/MIMARI.md`. | prompt 0003 |
| 2026-11-14 | Anlatım hızı ses modeline göre: katalogda `wordsPerSecond` (Türkçe; Supertonic 1,7 = e2e ölçümü 50 kelime / 29,23 sn; Chatterbox ve diğer yereller 2,0; online 2,2). Concepts `INarrationPaceSource` portunu Production adaptörü doldurur; video bitince gerçek hız (model+ses+dil) `production.speech_rate_stats`'a yazılır ve varsa o kullanılır. ScenePlan tahmini hedefi %15'ten fazla aşarsa işlem günlüğüne uyarı (ses kesilmez). | e2e raporu |
