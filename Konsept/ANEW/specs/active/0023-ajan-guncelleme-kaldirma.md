# Spec 0023 — ajan-guncelleme-kaldirma

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0023-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0009-ajan-kurulum-guncelleme.md` (eski 0009b — Güncelleme ve kaldırma); bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: none — yok — güncelleme ve kaldırma, davranışı bizim koda bağlı ajan işleridir (`docs/domain.md`); ancak güncelleme paketinin imza/hash doğrulaması (`docs/security.md`) AC-4 ve AC-5'te kasıtlı bozma ile kanıtlanır.
- Depends on: 0001 (ajan, tray, "Ajanı test et", sistem durum alanı, tek ürün versiyonu), 0006 (iş durumları, "Üretiliyor"), 0008 (yerel teknik log), 0009 (kurulum ekranı, kurulum yeri, bileşen doğrulaması, video klasörü), 0012 (ajanın API'ye bağlı/eşleşmiş durumu), 0017 ("Ajan bekleniyor" geçişi, ajan kopması kuralı), 0022 (ajan hata bildirimi akışı)

## Intent
Kurulu ajanın kendisi ya da bir bileşeni için yeni sürüm çıktığında kullanıcı, ilk kurulumdaki aynı profesyonel
ekranda "Şu an bunları güncelliyoruz" diye yalnızca değişen bileşenlerin güncellendiğini görür (ilk_promt.txt:
"Gerektiği zaman ileride update'lerde de bunları gösterebiliriz"). Güncelleme kullanıcının onayıyla ve
yalnızca üretim işi yokken başlar; bizim anahtarımızla imzalanmamış, hash'i tutmayan ya da kurulu sürümden eski
hiçbir paket kurulmaz; yarıda kalan ya da bozuk çıkan güncellemede ajan önceki çalışan sürüme döner. Kullanıcı
ajanı Windows "Uygulamalar" listesinden temiz biçimde kaldırabilir; emeği olan videolar sorulmadan silinmez.
Başarı: güncelleme sonrası sitedeki "Ajanı test et" yeni sürümü gösterir; sahte ya da eski paket hiçbir koşulda
kurulmaz. Bilinçli olarak yapılmayanlar: ilk kurulum ve "Ajanı indir" (0009), otomatik (onaysız) güncelleme,
birden fazla eski sürüm saklama, Windows kod imzalama sertifikası, ajan sürümlerinin Pi'ye yayınlanma süreci.

## Requirements
1. **Denetim zamanı.** Ajan açılışta ve çalıştığı sürece 6 saatte bir API'ye yeni sürüm sorar. Tray menüsündeki
   **"Güncellemeleri denetle"** öğesi denetimi hemen yapar; yeni sürüm yoksa "Ajan güncel (sürüm X)" bilgisi
   görünür.
2. **Bildirim ve onay.** Yeni sürüm varsa tray bildirimi "Yeni sürüm hazır: X" gösterir; kullanıcı **Şimdi
   güncelle** derse güncelleme başlar. Güncelleme kullanıcı onayı olmadan başlamaz (Req 4'teki zorunlu durum
   hariç; orada da başlatma düğmesine kullanıcı basar).
3. **İş sürerken başlamaz.** Kullanıcının durumu "Üretiliyor" olan bir işi varsa (iş durumları 0006) güncelleme
   başlamaz; ekranda "Üretim sürüyor; iş bitince ya da durdurulunca güncellenecek" görünür ve güncelleme, işin
   bitmesi ya da durdurulmasından sonra kendiliğinden başlar (BR-10). "Sırada" bekleyen işler, güncelleme bitene
   kadar ajana gönderilmez.
4. **Sürüm uyumsuzluğu.** Web, API ve Ajan tek ürün versiyonunu paylaşır (`docs/conventions.md`). Ajanın sürümü
   sunucununkinden farklıysa ajan API'den komut almaz, web sistem durum alanı "Ajan güncellemesi gerekli"
   gösterir ve ajanda güncelleme ekranı açılır.
5. **Güncelleme ekranı.** 0009'daki kurulum ekranı "Şu an bunları güncelliyoruz" başlığıyla açılır ve yalnızca
   güncellenecek bileşenleri (ajanın kendisi dahil) listeler; satır durumları, ilerleme göstergesi, akan Türkçe
   satırlar, ön kontrol (internet, disk "Gereken/Boş"), otomatik yeniden deneme, **Tekrar dene** ve **Hatayı
   bildir** (0022 akışı; bileşen + adım eklenir) 0009'daki kurallarla aynıdır. Bittiğinde "Güncelleme tamamlandı"
   görünür ve ajan tray'e iner.
6. **İmzalı sürüm listesi.** Ajan güncellemeleri Pi'deki API'den sunulur. Her sürümün dosya listesi (manifest)
   bizim özel anahtarımızla imzalanır (Ed25519); açık anahtar ajana gömülüdür. Listedeki her dosya SHA-256 ile
   doğrulanır. Üçüncü taraf bileşenlerin yeni sürümleri resmi kaynaklarından indirilir ve imzalı listedeki
   SHA-256 değerleriyle doğrulanır; Supertonic ağırlıkları Pi'deki aynadan sunulur. İmza ya da hash doğrulamasından
   geçmeyen paket kurulmaz (`docs/security.md`).
7. **Doğrulama başarısızsa.** Paket kurulmaz; ekranda "Güncelleme doğrulanamadı, mevcut sürümle devam ediliyor"
   görünür; ajan önceki sürümle çalışmaya ve API'ye bağlı kalmaya devam eder (sürüm uyumsuzluğu varsa Req 4
   durumu sürer); olay yerel loga Error olarak yazılır.
8. **Eski sürüme düşürme yok.** İmzası geçerli olsa bile kurulu sürümden eski ya da onunla aynı sürümü taşıyan bir
   liste kurulmaz (tekrar oynatma koruması); olay loga Warning olarak yazılır ve kullanıcıya "Ajan güncel" bilgisi
   görünür.
9. **Geri dönüş.** Güncelleme sırasında bir önceki çalışan sürüm saklanır; yalnızca bir önceki sürüm tutulur
   (daha eskisi silinir). Güncelleme yarıda kalırsa (hata, ajan çökmesi, bilgisayarın kapanması) ajan bir sonraki
   açılışta önceki çalışan sürümle açılır. Yeni sürüm ilk açılışında 60 sn içinde API'ye bağlanamazsa ajan
   kendiliğinden önceki sürüme döner ve "Yeni sürüm başlatılamadı, önceki sürüme dönüldü" bilgisini gösterir.
10. **Yönetici izni yok.** Güncelleme ve kaldırma Windows yönetici izni (UAC) istemeden tamamlanır.
11. **Kaldırma.** Ajan Windows "Uygulamalar" listesinde "ANEW Konsept" adıyla görünür ve oradan kaldırılır.
    Kaldırma `%LOCALAPPDATA%\anewconcept` kurulum yerini, tüm bileşenleri, kısayolu, başlangıç kaydını ve kaldırma
    kaydını siler. Video klasörünü silmeden önce "Videolarınız da silinsin mi?" diye sorar; varsayılan
    **Hayır**dır (videolar korunur). Ajan çalışıyorsa kaldırma önce ajanı kapatır. Bilgisayardaki başka
    Node/Python/FFmpeg/Chromium kurulumlarına ve "Konsept" adlı başka programlara dokunmaz.
12. **Tray ekleri.** Tray menüsüne "Güncellemeleri denetle" eklenir (Req 1); 0001'in ve 0009'un menü öğeleri
    değişmez.
13. **Log.** Güncelleme olayları (denetim, yeni sürüm bulundu, başladı, bileşen güncellendi, doğrulama başarısız,
    eski sürüm reddedildi, geri dönüş) yerel teknik loga Information/Warning/Error seviyesinde yazılır; loglarda
    şifre, token ya da API key bulunmaz (biçim ve saklama 0008).
14. **Dil ve erişilebilirlik.** Bu spec'in eklediği tüm metinler (bildirim, uyarılar, kaldırma sorusu) Türkçedir
    ve kaynak dosyalarından gelir; eklenen kontroller klavyeyle erişilebilirdir.
15. **Uyumsuz ajana iş gönderilmez.** Ajanın sürümü sunucununkinden farklıyken (Req 4) ona hiçbir üretim işi ya
    da yerel aşama komutu gönderilmez; üretim açısından ajan bağlı değilmiş gibi ele alınır (0017 Req 15): ilk
    yerel aşamaya gelen ya da o anda yerel aşamada olan iş **Ajan bekleniyor** durumuna geçer ve işin satırında
    neden olarak "Ajan güncellemesi gerekli" görünür; sıradaki bir sonraki iş başlamaz. Ajan güncellenip sürümü
    sunucuyla eşleşince iş kendiliğinden kaldığı aşamadan devam eder (BR-11).

## User experience
- Flow (güncelleme): Ajan açılışta / 6 saatte bir / "Güncellemeleri denetle" ile yeni sürüm bulur → tray bildirimi
  "Yeni sürüm hazır: X" → **Şimdi güncelle** → (iş "Üretiliyor" ise bekleme mesajı, iş bitince/durunca devam) →
  aynı ekran "Şu an bunları güncelliyoruz" başlığıyla açılır → yalnızca değişen bileşenler işlenir →
  "Güncelleme tamamlandı" → tray. Doğrulama başarısızsa "Güncelleme doğrulanamadı, mevcut sürümle devam ediliyor".
- Flow (zorunlu güncelleme): sürüm uyumsuz → web "Ajan güncellemesi gerekli" → ajanda güncelleme ekranı açık →
  kullanıcı başlatır.
- Flow (kaldırma): Windows Ayarlar → Uygulamalar → "ANEW Konsept" → Kaldır → "Videolarınız da silinsin mi?"
  (varsayılan Hayır) → kurulum yeri ve kayıtlar silinir.
- States:
  - Güncelleme denetimi — denetleniyor · güncel · yeni sürüm hazır · üretim sürüyor (bekliyor).
  - Güncelleme ekranı — güncelleniyor · bileşen hatası · güncelleme tamamlandı · güncelleme doğrulanamadı.
  - Geri dönüş — "Yeni sürüm başlatılamadı, önceki sürüme dönüldü".
  - Web sistem durum alanı — "Ajan güncellemesi gerekli".
  - Kaldırma — videolar sorusu (Evet / Hayır) · kaldırılıyor · kaldırıldı.
- Design reference: none — the flow above is the design. Görünüm 0009'un kurulum ekranıdır.
- Accessibility: tray bildirimi, "Şimdi güncelle", "Güncellemeleri denetle" ve kaldırma sorusu klavyeyle
  kullanılabilir; durumlar metinle verilir; 0009'daki erişilebilirlik kuralları güncelleme ekranında aynen geçerlidir.

## Constraints & out of scope
- Sensitive area: none — yok (bkz. başlık satırı). `docs/security.md` güncelleme imza/hash kuralını H-4 başlığı altında
  sayıyor; `docs/domain.md` H-4'ü LLM kodunun çalıştırılmasına daraltıyor. Bu spec "yok" der ve AC-4/AC-5'i kasıtlı
  bozma ile kanıtlar.
- Platform: yalnızca Windows 11.
- Anahtar: Ed25519 özel anahtarı repoda ve Pi'de durmaz (imzalama yayın makinesinde yapılır); açık anahtar ajana
  gömülüdür. Anahtar değişimi (rotation) v1 kapsamı dışında.
- Kanıt yöntemi: imza/hash/eski sürüm senaryoları sahte manifest ve sahte sunucuyla test edilir; geri dönüş ve
  kaldırma temiz VM ya da Windows Sandbox'ta ekran görüntüsü + klasör/kayıt karşılaştırmasıyla gözlenir.
- Out of scope: ilk kurulum, bileşen listesi, "Ajanı indir" bağlantısı, "Kurulum ve bileşenler" ekranı (0009);
  onaysız otomatik güncelleme; birden fazla eski sürüm saklama; anahtar değişimi; kod imzalama sertifikası
  (SmartScreen uyarısı kabul); ajan sürümlerinin Pi'ye yayınlanma süreci (deploy betikleri); hata bildiriminin
  içeriği ve gönderimi (0022); iş durdurma mekanizması (0006).
- Split: done — 0023 Ajan güncelleme ve kaldırma / kardeşler: 0009

## Acceptance criteria
- [ ] AC-1 — Yeni bir ajan/bileşen sürümü yayınlandığında ajan açılışta (ve sahte saatle 6 saat sonra) bunu bulur ve tray bildirimi "Yeni sürüm hazır: X" gösterir; **Şimdi güncelle** ile aynı ekran "Şu an bunları güncelliyoruz" başlığıyla açılır ve yalnızca değişen bileşenleri listeler; tamamlanınca "Güncelleme tamamlandı" görünür, ajan yeni sürümle çalışır ve sitedeki "Ajanı test et" (0001) yeni ajan sürümünü gösterir.
- [ ] AC-2 — Tray'deki "Güncellemeleri denetle" denetimi hemen yapar; yeni sürüm yoksa "Ajan güncel (sürüm X)" görünür; kullanıcı onay vermeden hiçbir güncelleme başlamaz.
- [ ] AC-3 — Kullanıcının "Üretiliyor" durumunda bir işi varken **Şimdi güncelle** güncellemeyi başlatmaz ve "Üretim sürüyor; iş bitince ya da durdurulunca güncellenecek" görünür; iş tamamlanınca ya da durdurulunca güncelleme kendiliğinden başlar; güncelleme sürerken "Sırada" işler ajana gönderilmez.
- [ ] AC-4 — İmzası geçersiz ya da listedeki SHA-256'sı tutmayan (kasıtlı bozulmuş) bir güncelleme paketi kurulmaz; ekranda "Güncelleme doğrulanamadı, mevcut sürümle devam ediliyor" görünür; ajan önceki sürümle çalışmaya ve API'ye bağlı kalmaya devam eder; olay yerel loga Error olarak yazılır.
- [ ] AC-5 — Geçerli imzalı ama kurulu sürümden eski ya da aynı sürümü taşıyan bir liste sunulduğunda (tekrar oynatma) hiçbir dosya kurulmaz, kullanıcı "Ajan güncel" bilgisini görür ve olay loga Warning olarak yazılır.
- [ ] AC-6 — Ajan sürümü sunucu sürümünden farklıyken ajan API'den gelen komutları (ör. "Ajanı test et", üretim işi) yürütmez; web sistem durum alanı "Ajan güncellemesi gerekli" gösterir; ajanda güncelleme ekranı açılır; güncelleme bitince durum alanı normale döner.
- [ ] AC-7 — Güncelleme yarıda kesilirse (süreç zorla sonlandırılır ya da bilgisayar kapanır) ajan bir sonraki açılışta önceki çalışan sürümle açılır ve API'ye bağlanır; diskte yalnızca bir önceki sürüm saklıdır.
- [ ] AC-8 — Yeni sürüm ilk açılışında 60 sn içinde API'ye bağlanamazsa (kasıtlı bozuk sürüm) ajan kendiliğinden önceki sürüme döner, "Yeni sürüm başlatılamadı, önceki sürüme dönüldü" bilgisini gösterir ve olay loga Error olarak yazılır.
- [ ] AC-9 — Güncelleme sırasında bir bileşen hata verirse satır 0009'daki gibi sade Türkçe sebeple Hata olur; **Tekrar dene** yalnızca o bileşeni yeniden dener; **Hatayı bildir** 0022 akışını bileşen adı ve adımıyla açar.
- [ ] AC-10 — Güncelleme ve kaldırma hiçbir adımda Windows yönetici izni (UAC) istemez.
- [ ] AC-11 — Windows "Uygulamalar" listesinden "ANEW Konsept" kaldırıldığında `%LOCALAPPDATA%\anewconcept`, kısayol, başlangıç kaydı ve kaldırma kaydı silinir; "Videolarınız da silinsin mi?" sorusunda varsayılan Hayır ile video klasörü ve içindeki videolar yerinde kalır, Evet ile silinir; ajan çalışıyorsa önce kapatılır.
- [ ] AC-12 — Kaldırmadan önce makinede bulunan farklı sürümde Node/Python ve "Konsept" adlı bir program/klasör, kaldırmadan sonra dosyaları, sürümleri ve kullanıcı/sistem ortam ayarları (araç yolu dahil) değişmeden durur.
- [ ] AC-13 — Denetim, yeni sürüm bulundu, güncelleme başladı, bileşen güncellendi, doğrulama başarısız, eski sürüm reddedildi ve geri dönüş olayları yerel teknik loga uygun seviyede yazılır; log dosyalarında şifre, token ya da API key geçmez.
- [ ] AC-14 — Bu spec'in eklediği tray öğesi, bildirim, güncelleme ekranı başlığı, uyarılar ve kaldırma sorusu klavyeyle kullanılabilir, erişilebilir ada sahiptir; tüm metinler Türkçe ve kaynak dosyasından gelir; 0001 ve 0009'un tray öğeleri değişmeden çalışır.
- [ ] AC-15 — Ajanın sürümü sunucudan farklıyken başlatılan işte ajana hiçbir üretim komutu gitmez (ajana giden komut kaydı boş); iş ilk yerel aşamada *Ajan bekleniyor* olur ve satırında "Ajan güncellemesi gerekli" nedeni görünür; sıradaki bir sonraki iş başlamaz; ajan güncellenip sürümü eşleşince iş kendiliğinden kaldığı aşamadan devam eder ve tamamlanmış aşamalar tekrarlanmaz (Req 15).

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise / deferred | |
| Regressions introduced | |
| Bugs escaped to production | |
