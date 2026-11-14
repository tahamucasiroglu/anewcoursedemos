# Spec 0005 — Model bilgisi ve ince ayar

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0005-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0005-konseptler.md` (Req 1–5, AC-1…AC-5'in şerit/model/ince ayar kısmı, AC-34–35'in bu sayfaya düşen kısmı; S-6, G-12, G-21) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: H-1 Kimlik ve yetki — yalnızca model seçiminde yetki kontrolü (BR-13; 0013'ün model yetkisi kararı devralındı, G-21)
- Depends on: 0001, 0002, 0011, 0003, 0004, 0013

## Intent
İçerik üreticisi, Konseptler sayfasında üretime başlamadan önce seçili kanal profili için hangi online
modellerin hangi efor düzeyinde çalışacağını ve içeriğin tonunu, izleyicisini ve anlatım biçimini görmek ve
değiştirmek istiyor (`ilk_promt.txt`, Konseptler bölümü). Bu spec Konseptler sayfasının iskeletini (üst şerit +
dört alan) ve şeridin üç kontrolünü kurar: model bilgisi ve modalı, ince ayar, kelime denetimi kutusu. Her kanalın
tarzı farklı olduğu için bu ayarların tamamı **profil başına** saklanır. Başarı: kullanıcı yalnızca kullanabileceği
modelleri seçebilir, seçimleri profil profil korunur ve şeritte bir bakışta görünür. Bilinçli olarak yapılmayanlar:
konu önerisi ve konsept üretimi, ayarların talimata uygulanması ve kelime denetiminin kendisi (0014), düzenleme
döngüsü (0015), Silinenler (0016), katalog ve API key yönetimi (0004), çağrı kapısı ve model yetkisinin zorlanması (0013).

## Requirements
Kaynak: `ilk_promt.txt` (Konseptler bölümü), `docs/domain.md` (BR-1, BR-2, BR-13, BR-15), sabah karar özeti Ç-3, E-4.

**A. Sayfa iskeleti**
1. Konseptler sayfası üst barın hemen altında bir şerit ve onun altında dört alan gösterir: sol üst "Konudan üret",
   sağ üst "Fikirden üret", sol alt "Üretilenler", sağ alt "Düzenleme alanı". Bu spec'te dört alan yalnızca başlık
   ve kısa açıklamayla görünür; içerikleri 0014 ve 0015 ile gelir.
2. Seçili profil yoksa sayfa "Önce bir profil oluştur" durumunu ve üst bardaki artı (profil ekle) kontrolüne
   yönlendirmeyi gösterir; şerit ve alanlar gösterilmez.

**B. Model bilgisi ve modalı**
3. Şeridin solunda **model bilgisi** görünür: üç model rolü (konsept üretimi, anlamca tekrar denetimi, konu önerisi)
   için seçili model ve (model destekliyorsa) efor düzeyi kısa metinle gösterilir (ör. "Konsept: Claude Sonnet 5.5
   High"). Model seçilmemiş rol "seçilmedi" olarak görünür.
4. Model bilgisine tıklanınca sayfanın üstünde ayrı bir **modal** açılır (sayfa içine açılır alan değil). Modalda her
   rol için model ve, model efor destekliyorsa, yalnızca o modelin desteklediği efor düzeyleri arasından seçim yapılır;
   model seçilince efor katalogdaki varsayılan efor olur. Modal kapat düğmesiyle ya da modal dışına tıklanarak kapanır
   ve seçimler kaydedilir; şerit yeni seçimleri gösterir.
5. Modalda yalnızca kullanıcının kullanabileceği **metin** modelleri listelenir: geçerli (okunabilir) API key'i olan
   sağlayıcıların ve kullanıcının yetkili olduğu modelleri (BR-13; katalog ve key 0004'ten, yetki 0013'ten gelir).
   Yerel LLM'ler ve Ollama v1'de hiçbir rolde listelenmez ve seçilemez (E-4); 0006'nın katalogdaki yerel görsel/ses
   modelleri ve video motorları metin modeli olmadığı için bu modalda yer almaz. Yetkisiz ya da kullanılamayan bir
   modeli seçmeye yönelik istek, arayüz atlanarak gönderilse bile sunucuda reddedilir.

**C. İnce ayar**
6. Şeridin ortasında (ekranın sağ yarısının başında) **ince ayar bilgisi** görünür: seçili ton, izleyici ve anlatım
   biçimi. Tıklanınca bir seçim alanı açılır; her grupta **tek seçim** yapılır ve her grubun varsayılanı
   **"Belirtilmedi"**dir (model profil içeriğine göre karar verir):
   - Ton: gizemli, eğitici, dramatik, sakin, belgesel, esprili, ilham verici, sorgulayıcı (8).
   - İzleyici: konuya yeni başlayan, meraklı, aile ve çocuklara uygun, konuyu bilen (4).
   - Anlatım biçimi: tek bir hikâye, sıralı liste, efsane mi gerçek mi, ya olsaydı (4; liste v1'de bu dördüyle sınırlı, G-12).
   İnce ayarın konu önerisi ve konsept üretimi talimatına uygulanması 0014'ün davranışıdır.

**D. Kelime denetimi kutusu**
7. Şeridin sağında **kelime denetimi** onay kutusu vardır; yeni profilde kapalıdır. Kutunun durumu kaydedilir;
   eşik ve otomatik yeniden üretim (BR-15) 0014'ün davranışıdır.

**E. Kalıcılık ve sahiplik**
8. Model seçimleri (rol başına model + efor), ince ayar ve kelime denetimi **profil başına** saklanır (Ç-3). Profil
   değiştirilince şerit o profilin ayarlarını gösterir; bir profilde yapılan değişiklik diğer profilleri etkilemez.
   Yeni profilde tüm roller "seçilmedi", ince ayarın her grubu "Belirtilmedi", kelime denetimi kapalıdır.
9. Kullanıcı yalnızca kendi profillerinin ayarlarını görür ve değiştirir (BR-1).

## User experience
- Flow: Kullanıcı üst bardan profil seçer → Konseptler sayfası açılır → şeritte model bilgisine tıklar → modalda
  rollerin modelini ve eforunu seçer → modalı kapatır (düğme ya da dışına tıklama) → şerit yeni seçimleri gösterir →
  ince ayar bilgisine tıklar → ton/izleyici/anlatım biçimi seçer → kelime denetimi kutusunu açar ya da kapatır.
- States:
  - Sayfa: profil yok ("Önce bir profil oluştur" + artı'ya yönlendirme) · ayarlar yükleniyor · ayarlar alınamadı
    (sade Türkçe mesaj) · hazır.
  - Model bilgisi: rol seçilmedi ("seçilmedi") · seçili model ve efor.
  - Model bilgisi modalı: açık · seçilebilir model yok (açıklama + Ayarlar yönlendirmesi) · kaydediliyor · kaydedildi
    · kaydetme hatası (sade Türkçe mesaj, önceki seçim korunur).
  - İnce ayar: kapalı (özet metin; seçim yoksa "Belirtilmedi") · açık (seçim alanı).
  - Kelime denetimi: açık · kapalı.
- Design reference: `source/konseptler sayfası tasarımı.png` — commit `61cc824` sürümü. Yerleşim: üst barın altında
  şerit (solda "modeller bilgisi", ortada "ince ayar bilgisi", sağda "kelime denetimi seçimi"); altında dört alan:
  sol üst "konu üreterek", sağ üst "fikirden üreterek", sol alt "üretilenler", sağ alt "üretilenleri düzenleme
  alanı". Görseldeki üst bar "profil" bağlantısı kaldırıldı (0001/ilk_promt). Model modalı ve ince ayar seçim alanı
  için görsel yok — yukarıdaki akış tasarımdır. Renk: turuncu/kahve, açık/koyu tema değişkenleri (K-7).
- Accessibility: tüm kontroller klavyeyle erişilebilir; modal açılınca odak modalın içine gider, Esc ve kapat
  düğmesi modalı kapatır, kapanınca odak açan kontrole döner; seçim kontrollerinin etiketi rol adını içerir
  ("Konsept üretimi modeli"); WCAG AA kontrast her iki temada.

## Constraints & out of scope
- Sensitive area: H-1 yalnızca model seçiminde yetki kontrolü (BR-13). G-21 kararı: 0013 model yetkisini H-1
  sayıyor; bu spec aynı kararı devralır ve seçim ucundaki reddi (AC-3) kasıtlı bozma ile kanıtlar. Veri sahipliği
  (AC-9) normal testle kanıtlanır; sahiplik zorlamasının kasıtlı bozma kanıtı 0002/0003'tedir.
- Bu spec hiçbir online model çağrısı yapmaz (H-3 yok).
- Katalog, efor seviyeleri ve varsayılan efor 0004'ten okunur; kodda sabit model listesi olmaz (`docs/conventions.md`).
- Tüm tarayıcı istekleri Web controller'ı üzerinden `$.ajax` ile gider (K-6).
- Out of scope: konu önerisi ve konsept üretimi, ince ayarın talimata girmesi, kelime denetimi eşiği ve BR-15 (0014) ·
  düzenleme döngüsü (0015) · Silinenler ve önerileri sıfırla (0016) · video üretimi model seçimleri (0006) · yerel
  LLM'ler ve Ollama (v1'de yok) · anlatım biçimi listesinin genişletilmesi.
- Split: done — 0005 Model bilgisi ve ince ayar / kardeşler: 0014, 0015, 0016

## Acceptance criteria
- [ ] AC-1 — Seçili profille sayfa açıldığında şerit, üç rolün (konsept üretimi, anlamca tekrar denetimi, konu önerisi) seçili modelini ve (model destekliyorsa) efor düzeyini — seçim yoksa "seçilmedi" —, ince ayar özetini ve kelime denetimi kutusunu tasarımdaki yerleşimle gösterir; altında dört alan (Konudan üret, Fikirden üret, Üretilenler, Düzenleme alanı) başlıklarıyla yer alır.
- [ ] AC-2 — Model bilgisine tıklamak sayfanın üstünde bir modal açar; bir rol için model değiştirilip modal kapat düğmesiyle ya da dışına tıklanarak kapatılınca seçim kaydedilir, şerit yeni modeli gösterir ve sayfa yenilendiğinde seçim korunur. Efor seçeneği yalnızca modelin desteklediği düzeyleri listeler, model seçilince katalogdaki varsayılan efor gelir; efor desteklemeyen modelde efor kontrolü görünmez.
- [ ] AC-3 — Modalda yalnızca geçerli API key'i olan sağlayıcıların ve kullanıcının yetkili olduğu metin modelleri listelenir; "Key yok", "Key okunamıyor" ve yetkisiz modeller, görsel modeller, yerel modeller ve Ollama listelenmez (E-4). Yetkisiz ya da kullanılamayan bir modeli seçmeye yönelik doğrudan istek reddedilir ve kayıtlı seçim değişmez (BR-13). Kasıtlı bozma: sunucudaki seçim kontrolü kaldırılınca test kırmızıya döner. Seçilebilir model yoksa modal açıklama ve Ayarlar yönlendirmesi gösterir.
- [ ] AC-4 — Model seçimleri, ince ayar ve kelime denetimi profil başına saklanır: profil A'da yapılan değişiklikten sonra profil B'nin ayarları değişmez; üst bardan profil değiştirilince şerit seçilen profilin ayarlarını gösterir. Yeni oluşturulan profilde tüm roller "seçilmedi", ince ayarın her grubu "Belirtilmedi", kelime denetimi kapalıdır (Ç-3).
- [ ] AC-5 — İnce ayar alanı tonda 8, izleyicide 4, anlatım biçiminde 4 seçenek ve her grupta "Belirtilmedi" sunar; her grupta aynı anda yalnızca bir seçenek seçilidir; yapılan seçim şeritte görünür ve sayfa yenilendiğinde korunur.
- [ ] AC-6 — Kelime denetimi kutusu işaretlenip kaldırıldığında durum kaydedilir ve sayfa yenilendiğinde korunur.
- [ ] AC-7 — Seçili profil yoksa sayfa "Önce bir profil oluştur" durumunu ve artı kontrolüne yönlendirmeyi gösterir, şerit ve alanlar görünmez. Ayarlar yüklenirken yükleniyor durumu, alınamazsa sade Türkçe hata mesajı görünür; modalda kaydetme hatası olursa sade mesaj görünür ve önceki seçim korunur.
- [ ] AC-8 — Kullanıcı A, kullanıcı B'nin profiline ait model seçimini, ince ayarını ya da kelime denetimi ayarını okuma ya da değiştirme girişiminde "bulunamadı" alır ve B'nin ayarı değişmez (BR-1); girişsiz istekler reddedilir.
- [ ] AC-9 — Tüm tarayıcı istekleri Web controller'ı üzerinden `$.ajax` ile gider (K-6); modal ve ince ayar alanı yalnızca klavyeyle kullanılabilir, modal açılınca odak modala geçer, Esc ile kapanır, kapanınca odak açan kontrole döner; seçim kontrollerinin rol adını içeren erişilebilir adı vardır.

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
