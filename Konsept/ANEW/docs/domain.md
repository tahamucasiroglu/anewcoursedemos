# Domain

> Bootstrap sırasında 2026-11-14'te dolduruldu (kaynak: `Promtlar/0004`–`0007`). İş ile insanlar ve agent'lar arasındaki ortak dil budur.
> If a term isn't here, expect the AI to invent its own meaning for it.

## Product

| Alan | Karar |
|---|---|
| **Ad** | ANEW Konsept |
| **Tek cümle** | Sosyal medya içerik üreticilerinin, kanal profillerine uygun video konseptlerini online LLM'lerle gerçek verilere dayanarak ürettiği; bu konseptleri kendi bilgisayarlarındaki masaüstü ajanıyla (render, seslendirme, görsel) videoya çevirdiği ve her şeyi web'den yönettiği bir üretim platformu. |
| **Kullanıcılar** | (1) **İçerik üreticisi:** birden fazla kanal profili olabilir, kendi API key'lerini girer, maliyeti kendisi öder. (2) **Admin:** istatistikleri, kullanıcıları ve hata bildirimlerini yönetir. Kod çok kullanıcılı; kullanıcılar kayıt olur, yetkileri rol ile belirlenir. Şimdilik yalnızca yerel ağda, tek kullanıcı (geliştirici) kullanıyor (`docs/security.md`). |
| **Temel değer** | Harcanan token başına alınabilecek en yüksek kalite ve performans; uydurmasız içerik. Video başına **sabit bir maliyet hedefi yok**; gerçek maliyet kullanıcı geri bildirimlerinden ve geliştiricinin testlerinden ölçülür, bunun için her online çağrının maliyeti kaydedilir (BR-12). Toplu kuyrukla çok sayıda video; kısmi yeniden üretimle (BR-9) boşa harcama yok. Kısa dikey videodan 15 dakikalık yatay videoya kadar her uzunluk desteklenir. |
| **Şimdiki kapsam (v0.x → v1.0)** | Konseptler, Konseptten Video, Üretilen Videolar, Silinenler, profil modalları, Günlük şeridi, ayarlar, hata bildirimi, admin paneli (düşük öncelik), masaüstü ajanı ile kurulum ve güncelleme. |
| **Kapsam dışı (şimdilik)** | Yayınlama (yalnızca "Çok yakında sizlerleyiz" sayfası), mobil uygulama. |
| **Başarı ölçütü** | v1.0 = tüm akış uçtan uca çalışıyor ve testleri yeşil. |

## Ubiquitous language
Aynı kelime her yerde (kod, spec, sohbet) aynı anlamdadır. Kod adları `docs/conventions.md` isimlendirme kurallarına uyar.

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| **Profil** (`ChannelProfile`) | Kullanıcının bir sosyal medya kanalı: ad, video dili, yatay/dikey biçim, hedef süre, kanal içeriği. Bir kullanıcının birden çok profili olabilir; hazır şablonlardan başlatılabilir. | Kullanıcı hesabı değil |
| **Konu önerisi** (`TopicSuggestion`) | "Konudan üret" ile gelen öneri: **1 cümlelik başlık + en fazla 3 cümlelik kısa özet** (BR-3). | Konsept değil; sahne detayı yok |
| **Konsept** (`Concept`) | Tam video kurgusu: başlık, sahneler, seslendirme metni ve ekran tarifi. Durumu BR-16'daki akışı izler. | Video değil |
| **Konsept durumu** (`ConceptStatus`) | Üretildi → Onaylandı → Sırada → Üretiliyor → Video oldu (BR-16). | — |
| **Onaylanan konsept** | Üretilen konseptlerden kullanıcının video üretimi için **seçtikleri** (durum: Onaylandı). Seçimi yapan arayüz kontrolü Konseptler spec'inde belirlenecek. | Üretilenler'deki her konsept değil; revizyon onayı (BR-6) değil |
| **Sahne** (`Scene`) | Konseptin bir parçası: başlık, ekranda gösterilecek şey, seslendirme metni, süre. | — |
| **Konsept revizyonu** (`ConceptRevision`) | Düzenleme döngüsünde üretilen aday sürüm; onaylanırsa konseptin yerini alır. | Video sürümü değil |
| **İnce ayar** (`FineTuneOptions`) | Ton, izleyici ve anlatım biçimi seçimleri. | Model fine-tuning değil |
| **Model rolü** (`ModelRole`) | Bir modelin hangi iş için seçildiği: konsept üretimi, denetim, konu önerisi, video kodu, görsel, ses. | — |
| **Sağlayıcı** (`Provider`) | Model sunan platform: Anthropic, OpenAI, OpenRouter, GLM, Kimi, Ollama… | — |
| **Video motoru** (`VideoEngine`) | Video kodunu render eden araç (ör. Hyperframes, Remotion, Revideo). Kesin liste araştırma raporuna bağlı → `specs/backlog.md`. | — |
| **Üretim sırası** (`ProductionQueue` / `QueueItem`) | Videoya çevrilecek konseptlerin sıralı listesi. | — |
| **Üretim işi** (`ProductionJob`) | Bir konseptin videoya çevrilmesi; aşamalardan oluşur. | — |
| **Aşama** (`PipelineStage`) | İşin bir adımı: analiz → video kodu → görsel → seslendirme → render → birleştirme. | — |
| **Aşama çıktısı** (`StageArtifact`) | Bir aşamanın ürettiği dosya ya da veri: kod, görseller, ses, render. | — |
| **Video** (`Video`) | Üretilmiş videonun kaydı. Dosya kullanıcının bilgisayarında durur. | Konsept değil |
| **Video sürümü** (`VideoVersion`) | Aynı konseptten farklı aşama çıktılarıyla üretilmiş video (ör. yalnızca sesi değişmiş). | Konsept revizyonu değil |
| **Masaüstü ajanı** (`DesktopAgent`) | Kullanıcının bilgisayarındaki tray uygulaması (Konsept.Agent): kurulum, render, yerel modeller, dosya işlemleri. | Web sitesi değil |
| **Harcama** (`UsageCost`) | Online model çağrısının token ve maliyet kaydı; aylık harcama bunların içinde bulunulan aydaki toplamıdır. | Limit ya da bütçe değil (BR-12) |
| **Üretilenler** | Konseptler sayfasındaki, üretilmiş (silinmemiş) konseptlerin listesi; düzenle/sil buradan yapılır. | Konseptten Video sayfasındaki "Onaylanan konsept listesi" değil |
| **Konseptten Video** | Onaylanmış konseptlerin üretim sırasına alınıp videoya çevrildiği sayfa (BR-16). | Üretilen Videolar sayfası değil |
| **Kelime denetimi** | Konseptte sık tekrar eden kelime var mı kontrolü (checkbox); eşik aşılırsa en fazla 1 kez yeniden üretim (BR-15). Eşik değeri spec'te netleşecek. | Anlam denetimi modeli değil |
| **Altyazı** | Videoda seslendirilen metnin ekranda gösterimi (aç/kapa); zamanlaması sese bağlı (BR-9). | — |
| **Ajan bekleniyor** | Yerel aşaması olan bir işin, ajan çevrimdışıyken beklediği durum (BR-11). İş durumlarının tam listesi spec'te netleşecek. | Hata durumu değil |
| **Silinenler** (`Trash`) | Silinen konsept ve videoların süresiz tutulduğu alan. Ayrı modül değil (`docs/architecture.md`). | Kalıcı silme değil |
| **Günlük** (`ActivityLog`) | Sitenin alt şeridinde kullanıcıya gösterilen işlem akışı. | Teknik log değil |
| **Teknik log** (`Log`) | Web, API ve ajanın Information/Warning/Error/Critical kayıtları (Serilog). | Günlük değil |
| **Hata bildirimi** (`ErrorReport`) | Kullanıcının ya da ajanın "bildir" ile API'ye gönderdiği hata. | — |
| **Rol / Yetki** (`Role` / `Permission`) | Kullanıcının neleri kullanabileceği (ör. `admin.panel`, `publish.use`, `model.use:<model>`); rol, yetki paketidir. | — |

## Business rules
Her kural en az bir testle kanıtlanır (`docs/testing.md`).

- **BR-1:** Her kayıt bir kullanıcıya aittir. Kullanıcı yalnızca kendi verisini görür. Admin yalnızca istatistik, kullanıcı ve hata bildirimi verilerini görür. (Zorlama: EF global query filter by `UserId` + API yetki kontrolü, `docs/security.md`.)
- **BR-2:** Her konsept tam olarak bir profile aittir. Sayfalar, üst bardan seçili profilin verisini listeler.
- **BR-3:** "Konudan üret" yalnızca konu önerisi üretir: 1 cümlelik başlık + en fazla 3 cümlelik kısa özet; adet 1–100. Tam sahne detayı yalnızca seçilen öneriler için üretilir.
- **BR-4:** Aynı profilde; daha önce üretilmiş, videosu yapılmış, silinmiş ya da önerilip seçilmemiş başlıklar tekrar önerilmez. "Önerileri sıfırla" **yalnızca önerilip seçilmemiş** başlıkların hafızasını siler; üretilmiş ve silinmiş konseptler engel olmaya devam eder.
- **BR-5:** "Fikirden üret" 1–100 arası adet alır, sonuçlar doğrudan Üretilenler'e düşer.
- **BR-6:** Düzenleme sonucu bir revizyon olarak modalda gösterilir. Onaylanırsa konseptin yerini alır; silinirse konsept aynen kalır. "Tekrar düzenle" döngüsünün sınırı yoktur.
- **BR-7:** Silinen konsept ya da video Silinenler'e gider ve süresiz saklanır (soft delete, `DeletedAt`). Silinenler'den silinen kayıt, dosyalarıyla birlikte kalıcı olarak gider ve geri alınamaz.
- **BR-8:** Silinenler, konu önerisi üretiminde negatif sinyal olarak kullanılır.
- **BR-9:** Aşama çıktıları ayrı saklanır. Bir ayar değiştiğinde yalnızca o aşama ve ona bağlı aşamalar yeniden üretilir:
  - Ses değişirse → seslendirme + birleştirme. Altyazı açıksa render da yeniden yapılır (altyazı zamanlaması sese bağlı).
  - Video motoru değişirse → video kodu + render + birleştirme; ses korunur.
  - Konsept metni değişirse → her şey baştan.
- **BR-10:** Kuyruk yukarıdan aşağı işlenir. Durdurulan işin tamamlanmış aşamaları korunur. Devam edildiğinde yarım kalan aşama baştan, kalan aşamalar sırayla çalışır.
- **BR-11:** Masaüstü ajanı çevrimdışıyken yerel aşamalar başlamaz, iş "ajan bekleniyor" durumunda kalır.
- **BR-12:** Her online model çağrısının token sayısı ve maliyeti kaydedilir. Aylık harcama **yalnızca bilgi amaçlıdır**: kullanıcının içinde bulunulan aya ait toplamı gösterilir; limit ya da engelleme yoktur.
- **BR-13:** Kullanıcı, yetkisi olmayan bir modeli ya da özelliği kullanamaz. Banlanan kullanıcı giriş yapamaz, ajanı da çalışmaz (tüm token'ları anında iptal edilir).
- **BR-14:** Web'den çıkış yapılınca kullanıcının tüm oturumları, ajan dahil, geçersiz olur; sunucu ajanın SignalR bağlantısını kapatır.
- **BR-15:** Kelime denetimi açıksa ve tekrar eşiği aşılırsa konsept otomatik olarak **en fazla 1 kez** yeniden üretilir; yine aşılırsa konsept bir uyarıyla Üretilenler'e düşer.
- **BR-16:** Konsept durumu şu sırayla ilerler: **Üretildi** → (kullanıcı video için seçer) **Onaylandı** → **Sırada** → **Üretiliyor** → **Video oldu**. Konseptten Video sayfasındaki "Onaylanan konsept listesi"nde yalnızca *Onaylandı* durumundakiler görünür. Seçimi yapan arayüz kontrolü Konseptler spec'inde, insan onayıyla belirlenecek.

## Key domain invariants
- Şifreler ve API key'ler hiçbir yerde düz metin olarak saklanmaz ve loglanmaz: şifreler ASP.NET Core Identity ile hash'lenir (hiç loglanmaz); API key'ler Data Protection ile şifrelenir ve loglarda maskelenir (`sk-…a3f`; `docs/security.md`).
- Bir kullanıcı, başka bir kullanıcının verisine hiçbir yoldan erişemez (BR-1).
- Konsept içeriği gerçek bilgiye dayanır, uydurma olmaz; üretim promptları doğruluk talimatı içerir.
- Online LLM API'lerini çağıran ve çözülmüş API key'i gören tek yer API'deki AiGateway modülüdür; API key'ler Pi'den hiç çıkmaz (Y-7, Y-8, `docs/architecture.md`).
- LLM'in ürettiği video kodu kullanıcı bilgisayarında yalnızca kısıtlı render ortamında çalışır (H-4).

## Sensitive areas

Bu alanlardan birine dokunan spec, Constraints bölümünde `Sensitive area: <ad>` yazar; lite modda da clarify sorulur, yalnızca doküman değiştiren turlar dışında her düzeltme turu yeniden review edilir ve her kriter bir kasıtlı bozma ile kırmızıya döndürülerek kanıtlanır (workflows/README.md, Modes).

| # | Alan | Kapsam | Neden hassas (bir hatanın bedeli) |
|---|---|---|---|
| H-1 | **Kimlik ve yetki** | Login, kayıt, roller ve yetkiler, ban, oturumlar, ajan cihaz eşleştirme ve web'den çıkınca ajan oturumunun düşmesi | Bir kullanıcı başkasının verisine erişebilir ya da banlı biri sistemi kullanmaya devam edebilir. |
| H-2 | **Gizli bilgiler** | Şifrelerin hash'lenmesi; API key'lerin şifrelenmesi, saklanması, loglarda maskelenmesi; Data Protection anahtarları | Sızan bir API key, kullanıcının parasını doğrudan başkasına harcatır. |
| H-3 | **Maliyet doğuran çağrılar** | Online model çağrıları, yeniden deneme ve döngüler (BR-15), token limitleri, toplu kuyruk, maliyet kaydı (BR-12) | Bir döngü hatası ya da yanlış token limiti, büyük bir kuyrukta kullanıcının bütçesini dakikalar içinde yakabilir. |
| H-4 | **LLM'in ürettiği video kodunun kullanıcı bilgisayarında çalıştırılması** (daraltılmış) | Yalnızca ajanın online modelin yazdığı video kodunu (ör. Hyperframes/Remotion JS) render için çalıştırması | Prompt injection ile modelin ürettiği koda sızan bir talimat, kullanıcının emri olmadan dosya okuyabilir ya da ağ isteği yapabilir. Render, yalnızca kendi çalışma klasörüne erişebilen ve ağ erişimi kapalı bir ortamda yapılır; sandbox yöntemi araştırma raporuna bağlı → `specs/backlog.md`. |

**Bilinçli olarak hassas sayılmayanlar:**
- **Ajanın kendi işleri:** kurulum, dosya silme, klasör açma gibi web'den verilen emirlerle çalışan, davranışı bizim kodumuza bağlı ajan işleri (H-4 kapsamı dışında).
- **Kalıcı silme:** geri alınamaz ama BR-7 ve normal testler yeterli.
- **Kişisel veri:** e-posta gibi az miktarda veri; H-1 kapsamında korunuyor.
