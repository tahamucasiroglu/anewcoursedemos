# Domain

> Bootstrap sırasında 2026-11-14'te dolduruldu (kaynak: `Promtlar/0004`–`0007`); aynı gün spec bölme sonrası onaylanan kararlarla güncellendi (`specs/active/0001`–`0025`). İş ile insanlar ve agent'lar arasındaki ortak dil budur.
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
| **Profil** (`ChannelProfile`) | Kullanıcının bir sosyal medya kanalı: ad, video dili, yatay/dikey biçim, hedef süre, kanal içeriği. Kurallar (0003): kullanıcı başına en fazla **20** profil; kanal adı 1–80 karakter ve kullanıcının profilleri arasında benzersiz (tr-TR, büyük/küçük harf duyarsız); video dili **Türkçe** ya da **İngilizce**; hedef süre **15 sn – 15 dk**; kanal içeriği 1–2000 karakter. Beş hazır şablon yalnızca yeni profil modalında sunulur. Seçili profil sunucuda hatırlanır; profil seçimi çalışan ya da sıradaki işleri etkilemez. Profil düzenlemesi yalnızca sonraki üretimleri etkiler (konsept dil, biçim ve süreyi kendisi taşır). Konsepti olan (Silinenler dahil) profil silinemez; konseptsiz profil kalıcı silinir. | Kullanıcı hesabı değil |
| **Konu önerisi** (`TopicSuggestion`) | "Konudan üret" ile gelen öneri: **1 cümlelik başlık + en fazla 3 cümlelik kısa özet** (BR-3). | Konsept değil; sahne detayı yok |
| **Konsept** (`Concept`) | Tam video kurgusu: başlık, sahneler, seslendirme metni ve ekran tarifi. Durumu BR-16'daki akışı izler. Üretildiği andaki dil, biçim ve hedef süreyi kendisi taşır; web aramasıyla üretildiyse bir kaynak listesi içerir. "Kelime tekrarı" (BR-15) ve "süre uyumsuz" (sahne süreleri toplamı hedefin ±%15'i dışında) uyarılarını taşıyabilir (0014). | Video değil |
| **Konsept durumu** (`ConceptStatus`) | Üretildi → Onaylandı → Sırada → Üretiliyor → Video oldu (BR-16). | — |
| **Onaylanan konsept** | Üretilen konseptlerden kullanıcının video üretimi için **seçtikleri** (durum: Onaylandı). Onay satırdaki ✓ ile ya da seçim kutuları + "Seçilenleri onayla" toplu eylemiyle yapılır; "Onayı geri al" konsepti Üretildi'ye döndürür (0014). | Üretilenler'deki her konsept değil; revizyon onayı (BR-6) değil |
| **Sahne** (`Scene`) | Konseptin bir parçası: başlık, ekranda gösterilecek şey, seslendirme metni, süre. | — |
| **Konsept revizyonu** (`ConceptRevision`) | Düzenleme döngüsünde üretilen aday sürüm; onaylanırsa konseptin yerini alır. | Video sürümü değil |
| **İnce ayar** (`FineTuneOptions`) | Ton, izleyici ve anlatım biçimi seçimleri. | Model fine-tuning değil |
| **Model rolü** (`ModelRole`) | Bir modelin hangi iş için seçildiği: konsept üretimi, denetim, konu önerisi, video kodu, görsel, ses. | — |
| **Sağlayıcı** (`Provider`) | Model sunan online platform. v1: metin için Anthropic, OpenAI, OpenRouter, GLM, Kimi; online görsel için yalnızca OpenAI (gpt-image) (0004); online ses için ElevenLabs (0017). Yerel görsel/ses modelleri ve video motorları katalogda "yerel" olarak yer alır (0006). Ollama ve yerel LLM v1'de hiçbir rolde yoktur (E-4). | — |
| **Video motoru** (`VideoEngine`) | Video kodunu render eden araç. v1 (Karar 5): **HyperFrames** (varsayılan); **Remotion** yalnızca ajanda isteğe bağlı bileşen olarak kuruluysa (0009) listelenir ve "Lisans uyarısı" etiketi taşır. Revideo ve Motion Canvas yok. | — |
| **Üretim sırası** (`ProductionQueue` / `QueueItem`) | Videoya çevrilecek konseptlerin sıralı listesi. Sıra profil başınadır; kullanıcı başına aynı anda tek iş çalışır (0006). Art arda 3 iş Hata olursa sıra kendiliğinden durur (0017). | — |
| **Üretim işi** (`ProductionJob`) | Bir konseptin videoya çevrilmesi ya da bir videonun yeni sürümü (0020); aşamalardan oluşur. İş *Üretiliyor*'a geçtiğinde profilin üretim ayarları işe kaydedilir (0006). | — |
| **İş durumu** (`JobStatus`) | Bir işin durumu yalnızca şunlardan biridir: **Sırada · Üretiliyor · Durduruldu · Ajan bekleniyor · Hata · Tamamlandı**. Tek kaynak 0006 Req 13 (Ç-5); Tamamlandı olan iş sıradan kalkar. | Konsept durumu değil |
| **Aşama** (`PipelineStage`) | İşin bir adımı: analiz → video kodu → görsel → seslendirme → render → birleştirme. | — |
| **Aşama çıktısı** (`StageArtifact`) | Bir aşamanın ürettiği dosya ya da veri: kod, görseller, ses, render. | — |
| **Video** (`Video`) | Üretilmiş videonun kaydı. Dosya kullanıcının bilgisayarında durur. | Konsept değil |
| **Video sürümü** (`VideoVersion`) | Aynı konseptten farklı aşama çıktılarıyla üretilmiş video (ör. yalnızca sesi değişmiş). Her sürüm üretildiği konsept metninin kopyasını, kullanılan modelleri ve motorları, altyazı/ilerleme çubuğu seçimini, AI ses beyanının durumunu ve başlangıç–bitiş saatini saklar (0017). Yeni sürüm ve yeniden üretim 0020'dedir; Sil videoyu tüm sürümleriyle siler (0019). | Konsept revizyonu değil; uygulama/ajan sürümü değil |
| **Masaüstü ajanı** (`DesktopAgent`) | Kullanıcının bilgisayarındaki tray uygulaması (Konsept.Agent): kurulum, render, yerel modeller, dosya işlemleri. v1'de kullanıcı başına **tek** ajan; yeni eşleştirme eski ajanın oturumunu düşürür (0012). | Web sitesi değil |
| **Harcama** (`UsageCost`) | Online model çağrısının token ve maliyet kaydı; aylık harcama bunların içinde bulunulan aydaki (Europe/Istanbul) toplamıdır ve profilden bağımsızdır. Her denemenin sonucu: başarılı · hata · yarım kaldı · yanıt alınamadı; token sayısı bilinmiyorsa kayıt "tahmini" işaretlidir (0013). | Limit ya da bütçe değil (BR-12) |
| **Key etiketi** | Ayarlar'daki model listesinde kullanılabilirlik: **Kullanılabilir · Key yok · Key okunamıyor** (0004) ve **Yetki yok** (0013). Kayıtlı key'in doğrulama durumu: **Doğrulandı · Doğrulanmadı** (0004). | Model yetkisi değil |
| **Üretilenler** | Konseptler sayfasındaki, üretilmiş (silinmemiş) konseptlerin listesi; düzenle/sil buradan yapılır. | Konseptten Video sayfasındaki "Onaylanan konsept listesi" değil |
| **Konseptten Video** | Onaylanmış konseptlerin üretim sırasına alınıp videoya çevrildiği sayfa (BR-16). | Üretilen Videolar sayfası değil |
| **Kelime denetimi** | Konseptte sık tekrar eden kelime var mı kontrolü (checkbox, profil başına; yeni profilde kapalı); eşik aşılırsa en fazla 1 kez yeniden üretim (BR-15). Eşik: durak kelimeler hariç aynı kelime **en az 5 kez** geçer ve toplam kelimelerin **%2'sini aşar** (tr-TR kuralıyla, 0014). | Anlam denetimi modeli değil |
| **Altyazı** | Videoda seslendirilen metnin ekranda gösterimi (aç/kapa); zamanlaması sese bağlı. Altyazı ve ilerleme çubuğu ajanın sabit şablonundan birleştirme aşamasında eklenir (Karar 5, 0017). | — |
| **Ajan bekleniyor** | Yerel aşaması olan bir işin, ajan çevrimdışıyken beklediği durum (BR-11); İş durumu listesinin bir üyesi. | Hata durumu değil |
| **Silinenler** (`Trash`) | Silinen konsept ve videoların süresiz tutulduğu alan. Ayrı modül değil (`docs/architecture.md`). | Kalıcı silme değil |
| **Günlük** (`ActivityLog`) | Sitenin alt şeridinde kullanıcıya gösterilen işlem akışı. Her olay tür, zaman, seviye, metin ve isteğe bağlı kullanıcı alanı taşır (0001); seviyeler **Bilgi · Uyarı · Hata · Kritik** (0021). | Teknik log değil |
| **Aktivite** | Admin panelinin Aktiviteler bölümünde gösterilen kayıt. Ayrı bir veri değildir: Günlük olaylarının sunucudaki kaydıdır (Ç-7). Admin'e yalnızca tür + zaman + kullanıcı görünür; Günlük metni ve kullanıcının içeriği görünmez (0010). | Günlük şeridinin kendisi değil |
| **Teknik log** (`Log`) | Web, API ve ajanın Information/Warning/Error/Critical kayıtları (Serilog). | Günlük değil |
| **Hata bildirimi** (`ErrorReport`) | Kullanıcının ya da ajanın "bildir" ile API'ye gönderdiği hata; API'nin beklenmeyen hataları da otomatik kayıt olur. Kaynak: **web · ajan · API** (0022). Durumlar: **Yeni → İnceleniyor → Çözüldü ya da Yok sayıldı**; her bildirim Yeni olarak saklanır, "açık" = Yeni + İnceleniyor (0025). | — |
| **Rol / Yetki** (`Role` / `Permission`) | Kullanıcının neleri kullanabileceği (ör. `admin.panel`, `publish.use`, `model.use:<model>`); rol, yetki paketidir. İki rol vardır: **kullanıcı** (varsayılan) ve **admin**. Admin rolü normal kullanıcı yetkilerine ek olarak `admin.panel` taşır; admin alanının giriş noktası üst barda "Ayarlar"ın yanındaki "Admin" ikonudur. İlk admin yalnızca Pi'deki ortam ayarındaki e-postayla belirlenir; kimse kendine ya da başkasına admin rolü veremez, rol atama ekranı yoktur (0011). `model.use` 0013'te (varsayılan rol tüm modellere yetkili), `publish.use` yayınlama gelince. | — |
| **Oturum** (`Session`) | Kullanıcının giriş durumu. Web oturumu son kullanımdan itibaren **14 gün** geçerlidir ve kullanıldıkça uzar (0002); ajan oturumu son kullanımdan itibaren **90 gün** geçerlidir (0012). Çıkış ve ban oturumları BR-13/BR-14'e göre düşürür. | — |

## Business rules
Her kural en az bir testle kanıtlanır (`docs/testing.md`).

- **BR-1:** Her kayıt bir kullanıcıya aittir. Kullanıcı yalnızca kendi verisini görür. Admin yalnızca istatistik (aktiviteler dahil: yalnızca tür, zaman, kullanıcı), kullanıcı ve hata bildirimi verilerini görür. (Zorlama: EF global query filter by `UserId` + API yetki kontrolü, `docs/security.md`.)
- **BR-2:** Her konsept tam olarak bir profile aittir. Sayfalar, üst bardan seçili profilin verisini listeler.
- **BR-3:** "Konudan üret" yalnızca konu önerisi üretir: 1 cümlelik başlık + en fazla 3 cümlelik kısa özet; adet 1–100. Tam sahne detayı yalnızca seçilen öneriler için üretilir.
- **BR-4:** Aynı profilde; daha önce üretilmiş, videosu yapılmış, silinmiş ya da önerilip seçilmemiş başlıklar tekrar önerilmez. "Önerileri sıfırla" **yalnızca önerilip seçilmemiş** başlıkların hafızasını siler; üretilmiş ve silinmiş konseptler engel olmaya devam eder.
- **BR-5:** "Fikirden üret" 1–100 arası adet alır, sonuçlar doğrudan Üretilenler'e düşer.
- **BR-6:** Düzenleme sonucu bir revizyon olarak modalda gösterilir. Onaylanırsa konseptin yerini alır; silinirse konsept aynen kalır. "Tekrar düzenle" döngüsünün sınırı yoktur.
- **BR-7:** Silinen konsept ya da video Silinenler'e gider ve süresiz saklanır (soft delete, `DeletedAt`). Silinenler'den silinen kayıt, dosyalarıyla birlikte kalıcı olarak gider ve geri alınamaz. v1'de Silinenler'den geri yükleme yoktur. Video silme, videoyu tüm sürümleriyle siler (0019).
- **BR-8:** Silinenler, konu önerisi üretiminde negatif sinyal olarak kullanılır; silinen video "bu video beğenilmedi" sinyalidir (0019). Kalıcı silinen kayıt sinyal olmaktan çıkar (0016, 0019).
- **BR-9:** Aşama çıktıları ayrı saklanır. Bir ayar değiştiğinde yalnızca o aşama ve ona bağlı aşamalar yeniden üretilir (yeni sürüm tablosu: 0020 Req 1):
  - Ses değişirse → seslendirme + birleştirme; render çalışmaz. Altyazı birleştirmede yeni sese göre zamanlanır (Karar 5); altyazının açık olması render gerektirmez (0020 Req 1).
  - Video motoru değişirse → video kodu + render + birleştirme; ses korunur.
  - Görsel modeli değişirse → görsel + render + birleştirme. Online çağrı yalnızca online görsel modeli (OpenAI gpt-image) seçilirse yapılır.
  - Altyazı ya da ilerleme çubuğu açılıp kapanırsa → yalnızca birleştirme.
  - Sessiz videoya geçilirse → yalnızca birleştirme (altyazı kaldırılır); render çalışmaz (0020 Req 1).
  - Birden fazla ayar değişirse → etkilenen aşamaların birleşimi.
  - Konsept metni değişirse → her şey baştan.
- **BR-10:** Kuyruk yukarıdan aşağı işlenir. Durdurulan işin tamamlanmış aşamaları korunur. Devam edildiğinde yarım kalan aşama baştan, kalan aşamalar sırayla çalışır.
- **BR-11:** Masaüstü ajanı çevrimdışıyken yerel aşamalar başlamaz, iş "ajan bekleniyor" durumunda kalır.
- **BR-12:** Her online model çağrısının token sayısı ve maliyeti kaydedilir. Aylık harcama **yalnızca bilgi amaçlıdır**: kullanıcının içinde bulunulan aya ait toplamı gösterilir; limit ya da engelleme yoktur.
- **BR-13:** Kullanıcı, yetkisi olmayan bir modeli ya da özelliği kullanamaz. Banlanan kullanıcı giriş yapamaz, ajanı da çalışmaz (tüm token'ları anında iptal edilir). Ban süresizdir ve admin tarafından kaldırılabilir; admin hesapları banlanamaz; ban anında işler çıkıştaki gibi durur (BR-14, 0024).
- **BR-14:** Web'den çıkış yapılınca kullanıcının tüm oturumları, ajan dahil, geçersiz olur; sunucu ajanın SignalR bağlantısını kapatır. Çıkış ya da ban anında (ve kullanıcının son geçerli web oturumunun süresi dolunca; başka tarayıcıda geçerli oturum varken bir oturumun süresinin dolması durdurmaz — 0006 Req 21) çalışan iş *Durduruldu* olur; *Sırada* işler *Sırada* kalır ve sıra durur (0006 Req 16); süren konsept/öneri üretim partileri de durur (0014 Req 20).
- **BR-15:** Kelime denetimi açıksa ve tekrar eşiği aşılırsa konsept otomatik olarak **en fazla 1 kez** yeniden üretilir; yine aşılırsa konsept bir uyarıyla Üretilenler'e düşer.
- **BR-16:** Konsept durumu şu sırayla ilerler: **Üretildi** → (kullanıcı video için seçer) **Onaylandı** → **Sırada** → **Üretiliyor** → **Video oldu**. Konseptten Video sayfasındaki "Onaylanan konsept listesi"nde yalnızca *Onaylandı* durumundakiler görünür. Onay satırdaki ✓ ya da "Seçilenleri onayla" ile verilir; "Onayı geri al" Onaylandı → Üretildi geçişidir (0014). Sıradaki bir işin konsepti düzenlenemez ve silinemez (0006). Tek istisna 0020'dir: "Yeniden üret" bağlamında *Video oldu* konsept düzenleme döngüsüyle düzenlenebilir ve sürüm işi konsept *Video oldu* kalarak sıraya alınır.

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
| H-4 | **LLM'in ürettiği video kodunun kullanıcı bilgisayarında çalıştırılması** (daraltılmış) | Yalnızca ajanın online modelin yazdığı video kodunu (ör. Hyperframes/Remotion JS) render için çalıştırması | Prompt injection ile modelin ürettiği koda sızan bir talimat, kullanıcının emri olmadan dosya okuyabilir ya da ağ isteği yapabilir. Render, yalnızca kendi çalışma klasörüne erişebilen ve ağ erişimi kapalı bir ortamda yapılır; yalıtım yöntemi Karar 5 ile seçildi ve 0018'de tanımlı (araştırma raporu `raporlar/b5-video-pipeline-arastirmasi`). |

**Bilinçli olarak hassas sayılmayanlar:**
- **Ajanın kendi işleri:** kurulum, dosya silme, klasör açma gibi web'den verilen emirlerle çalışan, davranışı bizim kodumuza bağlı ajan işleri (H-4 kapsamı dışında).
- **Kalıcı silme:** geri alınamaz ama BR-7 ve normal testler yeterli.
- **Kişisel veri:** e-posta gibi az miktarda veri; H-1 kapsamında korunuyor.
