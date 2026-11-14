# Sabah karar özeti — 2026-11-14 gecesi

Durum: hiçbir kod yazılmadı. Hiçbir spec onaylı değil; hepsi `Status: Draft`. Kural 2'ye göre kod,
spec ve plan onaylarından sonra başlayacak. Bu özet senin kararlarını kolaylaştırmak için hazırlandı;
her şey öneri, karar senin.

## Gece yapılanlar (commit'ler, yalnız Konsept/ANEW, main, push yok)
- `4dc742b` spec 0001 taslağı, PostgreSQL 17 kararı, ikon promptu (`source/ikon-promptu.md`)
- `32a6c06` B-5 araştırma raporu → `raporlar/b5-video-pipeline-arastirmasi/index.html`
- (bu commit) spec 0002–0010 taslakları + bu özet

Her spec bir ajan tarafından yazıldı. Ardından yazandan bağımsız, "düşmanca okuyan" başka bir ajan
spec'i `ilk_promt.txt` ve onay kayıtlarınla karşılaştırıp düzeltti. En sonda da bir kapsam ajanı
`ilk_promt.txt`'yi cümle cümle tarayıp spec'lerle karşılaştırdı.

## Spec'ler bir bakışta
| Spec | Konu | AC | Hassas alan | Not |
|---|---|---|---|---|
| 0001 | Walking skeleton | 10 | yok | D1–D4 işlendi; 6 açık nokta |
| 0002 | Kimlik | 25 | H-1 | ban 0010'a taşındı; bölme önerisi |
| 0003 | Profiller | 15 | H-1 (sahiplik) | bölme önerisi (öneri: tek kalsın) |
| 0004 | AiGateway + harcama | 28 | H-2, H-3, H-1 | bölme önerisi |
| 0005 | Konseptler | 35 | H-3 | bölme önerisi (4 parça) |
| 0006 | Üretim / Konseptten Video | 37 | H-3, H-4 | bölme önerisi (0011, 0012) |
| 0007 | Üretilen videolar | 25 | H-3 (+H-1 akış) | bölme önerisi |
| 0008 | Günlük + hata bildirimi + log | 16 | H-2 | bölme önerisi |
| 0009 | Ajan kurulum/güncelleme | 16 | yok (tartışmalı) | B-5'e bağlı |
| 0010 | Admin paneli | 18 | H-1 | ban burada |

Spec'lerin çoğu büyük çıktı; 0001 hariç hepsi ~10 AC sınırını aşıyor. ANEW kuralı bu durumda bölmeyi öneriyor.

## Karar 1 — Bölme numaralandırması
Spec'lerin bazıları bölme için harf eki öneriyor (0002a, 0005b…). Doctor spec numarasını dosya adından
okuyor (`0002a` ayrı bir numara gibi görünür) ve plan dosyaları `NNNN-plan.md` kalıbında.
**Öneri:** bölünen her parça yeni bir sıra numarası alsın (0011, 0012, …). Harf eki kullanılmasın.
**Gerekçe:** ANEW'in "NNNN = bir iş" kuralına ve doctor'ın kontrollerine birebir uyuyor.

## Karar 2 — Önerilen sıra (bağımlılıklara göre düzeltilmiş)
Kapsam ajanı, yol haritasındaki sıranın spec'lerin kendi bağımlılıklarıyla çeliştiğini buldu.
Örneğin maskeleme 0002'de lazım ama 0008'de tanımlı; render araçlarının kurulumu 0006'da lazım ama 0009'da tanımlı.
**Öneri:**
1. 0001 walking skeleton
2. 0008'in loglama + maskeleme dilimi (0002 ve 0004 bunu kullanıyor)
3. 0002 kimlik → 0003 profiller → 0004 AiGateway
4. 0008'in Günlük kaydı dilimi (0005 ve 0006 olay yazıyor)
5. 0005 konseptler
6. 0009'un ilk dilimi (render araçlarının kurulumu + "Ajanı indir" bağlantısı)
7. 0006 üretim (bölünmüş halleriyle) → 0007 üretilen videolar
8. 0009'un kalan dilimi (güncelleme ekranı) → 0010 admin

## Karar 3 — Spec'ler arası çelişkiler (her biri için önerim)
| # | Çelişki | Önerim |
|---|---|---|
| Ç-1 | Yerel video akışı hem 0006'da hem 0007'de tanımlı | Sahibi 0006 (üretim) olsun, H-1 etiketi de oraya geçsin; 0007 kullanır. Mesaj tek: "Videoyu izlemek için ajan bağlı olmalı". |
| Ç-2 | Silinenler sayfasının bütününün sahibi yok (konsept 0005, video 0007) | Sayfanın kendisi 0005'te kurulsun (iki sekme: Konseptler / Videolar, sekme başına boş durum); 0007 video sekmesini doldursun. Geri yükleme her iki tarafta da v1'de yok. |
| Ç-3 | Model seçimlerinin kapsamı: kullanıcı başına mı (0005), profil başına mı (0006)? | **Profil başına.** Her kanalın tarzı farklı; kullanıcı da kanal kanal çalışıyor. |
| Ç-4 | Çıkış/ban anında çalışan işler: 0002 "sunucu aşamaları sürer", 0006/0010 "durdurulur" | **Durdurulur** ("Durduruldu", kaldığı yerden devam edilebilir). Sahibi 0006. Gerekçe: oturum yokken para harcanmasın (H-3). |
| Ç-5 | İş durum listesi 0006 ile 0007'de farklı | Tek liste 0006'da: Sırada · Üretiliyor · Durduruldu · Ajan bekleniyor · Hata · Tamamlandı. 0007 bu listeye referans verir. |
| Ç-6 | Hata bildirimi kaynakları: 0010 API'yi de sayıyor, 0008 saymıyor | 0008 API'nin beklenmeyen hatalarını da (Error/Critical) otomatik hata kaydı olarak toplasın; 0010'daki filtre de anlamlı olur. |
| Ç-7 | Admin "aktiviteler" verisini kim üretiyor? | Aktivite = Günlük olaylarının sunucudaki kaydı (aynı veri). 0002 giriş, çıkış, başarısız giriş ve eşleştirme olaylarını yazsın (kendi AC'siyle); 0005 ve 0006 üretim olaylarını yazsın. |
| Ç-8 | Online ses/görsel fiyatları için katalog genişlemesinin sahibi yok | 0006 kendi kapsamında katalogu genişletsin ("ses" türü, karakter başı fiyat); 0004 temel katalog olarak kalsın. |
| Ç-9 | Maskeleme 3 spec'te kanıtlanıyor | Mekanizma ve kanıt 0008 dilim 1'de. 0002 ve 0004 yalnızca "kendi alanlarında maskeli" olduğunu birer satırla kanıtlar. |
| Ç-10 | Admin iskeleti ve yetkisiz sayfa durumu 0002 ve 0010'da çift | 0002 yetki iskeletini kursun; 0010 AC-1 buna dayansın; admin giriş noktası tek soru (0010). |
| Ç-11 | Konsept düzenleme döngüsü 2 sayfada (0005, 0007) | Döngü yalnızca 0005'te. 0007'deki "Yeniden üret" kullanıcıyı Konseptler sayfasındaki düzenlemeye götürsün. |
| Ç-12 | 0008 AC-15 ban'a dayanıyor ama ban 0010'da geliyor | Bu kriter 0010'a taşınsın. |

## Karar 4 — Kapsamda eksik kalanlar (ilk_promt'tan)
| # | Eksik | Önerim |
|---|---|---|
| E-1 | İş bitti/hata olaylarının süre ve başlangıç/bitiş saatiyle Günlük'e yazılması (örn. "57 sn") | 0006'ya AC eklensin. |
| E-2 | Silinen **videonun** konu önerisinde "beğenilmedi" sinyali olması | 0007'ye eklensin (BR-8 genişler). |
| E-3 | "Sentleri de kuruşları da": TL gösterimi de mi isteniyor? | Şimdilik yalnızca USD ve sent; TL ileride. 0004'e soru olarak eklendi sayılsın. |
| E-4 | Ollama (yerel LLM) hangi rolde seçilebilir? | v1'de hiçbir rolde. Ollama yalnızca B-5'te gerekiyorsa kurulsun. |
| E-5 | İkonun ürün içinde kullanımı (üst bar, favicon, tray, kurulum) | 0001'de yer tutucu, ikon gelince değiştirilir (0001 açık nokta 5). |
| E-6 | "Ajanı indir" bağlantısı | 0009'un ilk dilimine (0001 açık nokta 6). |
| E-7 | Versiyonun sabit metin olması | Düzeltildi: 0001 AC-4 artık tek ürün versiyonunu okuyor. |

## Karar 5 — B-5 raporundaki teknik öneriler (ayrıntı: rapor sayfası)
- **Video motoru:** HyperFrames (Apache-2.0, sürüm sabit). Remotion opsiyonel ve varsayılan kapalı (lisans, telemetri, sandbox). Revideo ve Motion Canvas bırakılır.
- **Altyazı ve ilerleme çubuğu:** LLM üretmez, ajanın sabit şablonundan gelir (ASS karaoke, FFmpeg overlay).
- **Yerel TTS:** Supertonic 3, .NET içinde ONNX ile çalışır, Türkçe dahil. Lisansı AI beyanı istiyor, beyan varsayılan açık. Chatterbox, ses klonlama için ayrı bir Python sidecar'da.
- **Görsel:** Z-Image-Turbo (GGUF, stable-diffusion.cpp). FLUX.2 klein 4B ikinci seçenek; 9B lisans nedeniyle engellenir.
- **Sandbox (H-4):** admin gerektirmeyen katmanlar ve 11 kasıtlı bozma testi. Önce 2 spike gerekiyor (AppContainer + Chrome davranışı belirsiz).
- **Dikkat:** OpenAI TTS 2027-01-06'da kapanıyor; üzerine yeni iş yapılmamalı.

## Sabah onay akışı (önerim)
1. Bu sayfadaki Karar 1–4'ü tek seferde onayla ("hepsini kabul" ya da madde numarasıyla değişiklik).
2. Ben bu kararları spec'lere işleyip bölmeleri uygularım. Spec'ler daha küçük ve net hale gelir.
3. Spec'leri sırayla tek tek onaylarsın, her mesajda bir spec, 0001'den başlayarak. Her spec'in sonundaki
   "Open questions" bölümünde önerilerim var; "önerilerin hepsini kabul" diyebilirsin.
4. Spec'lerin hepsi onaylanınca planlar yazılır, plan onaylarıyla birlikte kodlama başlar ve her adımda commit alınır.
5. İkon: `source/ikon-promptu.md` içindeki promptu Claude Web'e ver, sonucu `source/` klasörüne koy.
6. Docker: istediğin zaman (yönetici yetkisi + yeniden başlatma). O zamana kadar full profil Docker'sız yöntemle çalışacak (plan 0001'de önerilecek).
