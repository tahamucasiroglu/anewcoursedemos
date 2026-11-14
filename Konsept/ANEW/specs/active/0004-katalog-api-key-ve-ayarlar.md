# Spec 0004 — katalog-api-key-ve-ayarlar

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0004-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0004-ai-gateway-ve-harcama.md` (bölündü 2026-11-14, onay: tahamucasiroglu 'önerilerin uygun'); Katalog, API key yönetimi ve Ayarlar temeli kısmı (eski Req 1–8, 20)
- Sensitive area: H-2 Gizli bilgiler (API key saklama, maskeleme, doğrulama, loglama)
- Depends on: 0001, 0008, 0002, 0011

## Intent
İçerik üreticisi, konsept ve video üretiminde kendi API key'leriyle ve kendi parasıyla online modelleri
(Anthropic, OpenAI, OpenRouter, GLM, Kimi) kullanır (`ilk_promt.txt`, `docs/domain.md` Kullanıcılar). Bu spec
bunun ilk zeminini kuruyor: sistemin tanıdığı sağlayıcı ve model kataloğu (metin/görsel ayrımı, efor seviyeleri,
çıktı token limiti, fiyat), kullanıcının API key'lerini Ayarlar'dan güvenle girip doğrulatıp güncelleyip
silebilmesi ve Ayarlar sayfasının kendisi. Başarı: kullanıcı key'ini girer, yalnızca maskeli halini görür; hiçbir
key düz metin olarak saklanmaz, loglanmaz ya da kullanıcıya geri gösterilmez; yanlış yapıştırılan key girildiği
anda, para harcamadan yakalanır. Bilinçli olarak yapılmayanlar: çağrı kapısı, model yetkisi, token/efor/yeniden
deneme kuralları, harcama kaydı ve aylık gösterge (0013); model seçim ekranları (0005, 0006); yerel LLM'ler ve
Ollama (v1'de hiçbir rolde seçilemez); "ses" ve "video motoru" katalog türleri ile yerel görsel/ses modelleri ve
video motorlarının katalog girişleri (0006); online ses girişi ElevenLabs ve karakter başı fiyat (0017).

## Requirements
**Katalog**
1. Sistem bir sağlayıcı ve model kataloğu tutar. İlk sürümdeki online sağlayıcılar **Anthropic, OpenAI,
   OpenRouter, GLM ve Kimi**'dir (`ilk_promt.txt`). Her metin sağlayıcısından geliştiricinin test ettiği **1–3
   model** katalogda yer alır (ör. Claude Opus / Sonnet / Haiku). Kesin model listesi seed verisindedir.
2. Katalog türleri bu spec'te **metin** ve **görsel**dir. Online görsel sağlayıcısı olarak v1'de yalnızca OpenAI
   (gpt-image) bulunur; görsel üretiminin varsayılanı yereldir (Z-Image-Turbo, ikinci FLUX.2 klein 4B — Karar 5 (`raporlar/b5-video-pipeline-arastirmasi`),
   ajan tarafı; bu yerel modellerin katalog girişlerini 0006 ekler). Başka bulut görsel sağlayıcıları ve modelleri (fal
   Z-Image-Turbo, Google Nano Banana) v1'de yoktur, backlog'dadır. "ses" ve "video motoru" türlerini 0006 ekler;
   online ses girişi (ElevenLabs, karakter başı fiyat) bu "ses" türüne 0017'de eklenir. OpenAI TTS katalogda yoktur.
3. Katalogda yerel LLM (Ollama) yoktur; Ollama v1'de hiçbir rolde seçilemez (E-4). Bu spec'in seed verisi yalnızca
   online modelleri içerir; yerel görsel/ses modelleri ve video motorları kataloğa "yerel" girişler olarak 0006
   tarafından eklenir (Ç-8 düzeni).
4. Katalogdaki her model için şunlar bilinir: bağlı olduğu sağlayıcı, görünen adı, türü (metin / görsel),
   desteklediği efor seviyeleri (low'dan max'a; hiç desteklemeyebilir), varsayılan efor seviyesi, **çıktı token
   limiti** (yalnızca metin), **çağrı zaman aşımı** (varsayılan 10 dk) ve maliyet hesaplamaya yeten birim fiyatı
   (metin: giriş ve çıkış token başına; görsel: üretilen görsel başına), USD. Token limitleri, zaman aşımları ve
   fiyatlar katalogdadır; davranışa gömülü sabit değer yoktur (`docs/conventions.md`, Data rules).
5. Katalog, sistemle gelen sürümlenmiş bir seed verisidir; fiyat ya da limit değişikliğini geliştirici yapar
   (patch sürüm). Admin'in katalog/fiyat düzenleme ekranı v1'de yoktur.
6. Kullanıcı kataloğu Ayarlar'da salt okunur **"Modeller"** listesi olarak görür; tür (metin/görsel) ve sağlayıcıya
   göre filtreleyebilir. Her modelin yanında kullanılabilirlik etiketi görünür: **Kullanılabilir** (sağlayıcının key'i
   kayıtlı) · **Key yok** · **Key okunamıyor**. Model yetkisine dayanan **Yetki yok** etiketi 0013'te eklenir.
   Bu liste sonraki spec'lerin seçim ekranlarının kaynağıdır.

**API key yönetimi (H-2)**
7. Kullanıcı Ayarlar'da her online sağlayıcı için **bir** API key girebilir, mevcut key'i yenisiyle değiştirebilir ve
   silebilir. Key'ler kullanıcıya aittir; başka kullanıcı (admin dahil) göremez ve değiştiremez (BR-1).
8. Girilen key baştaki/sondaki boşluklardan arındırılır. Boş, yalnızca boşluk, **8 karakterden kısa, 512 karakterden
   uzun** ya da kontrol karakteri içeren değer reddedilir ve alanın yanında Türkçe hata gösterilir. Sağlayıcıya özgü
   önek kontrolü (`sk-`, `sk-ant-` …) yapılmaz.
9. Kaydedilen key bir daha **tam haliyle hiçbir yerde gösterilmez**: Ayarlar'da yalnızca maskeli hali (`sk-…a3f`
   biçiminde: ilk en fazla 3 ve son 3 karakter), doğrulama etiketi ve son güncellenme zamanı görünür. Key düz metin
   olarak saklanmaz; hiçbir log, hata mesajı, Günlük satırı ya da yanıt içinde düz metin yer almaz
   (`docs/domain.md` invariants). Maskeleme mekanizması 0008'dedir; bu spec yalnızca kendi alanının maskeli
   olduğunu kanıtlar.
10. **Doğrulama:** key kaydedilirken sağlayıcıda **maliyetsiz** bir yolla (token harcamayan bir uç, ör. model listesi)
    doğrulanır. Doğrulama tek denemedir, tekrar edilmez, en fazla 10 sn bekler ve harcama üretmez.
    - Sağlayıcı key'i reddederse (401) key **kaydedilmez**, alanın yanında "Sağlayıcı bu key'i reddetti" görünür,
      varsa mevcut key değişmez.
    - Doğrulama başarılıysa key **"Doğrulandı"** etiketiyle kaydedilir.
    - Sağlayıcının maliyetsiz doğrulama yolu yoksa ya da doğrulama 401 dışında bir nedenle sonuçlanamazsa (zaman
      aşımı, 429, 5xx, bağlantı hatası) key **"Doğrulanmadı"** etiketiyle kaydedilir.
11. Key silinmeden önce onay istenir. Onay metni: "Bu key silinecek; <Sağlayıcı> modelleri kullanılamaz olacak.
    Sıradaki işler bu sağlayıcıyı kullanıyorsa duracak." Onaylanınca key kalıcı olarak gider ve o sağlayıcının
    modelleri "Key yok" olarak görünür. Sıradaki işin bu durumda durması (harcama olmadan "API key yok — Ayarlar"
    nedeniyle Hata) 0017'nin çalışma anı davranışıdır.
12. Kayıtlı bir key şifre çözme anahtarları kaybolduğu için çözülemiyorsa (`docs/security.md`) satır **"Key okunamıyor —
    lütfen yeniden girin"** durumunu gösterir ve o sağlayıcının modelleri "Key okunamıyor" etiketini alır;
    kullanıcı "Değiştir" ile yeni key girerek durumu düzeltir.
13. Düzenleme alanının yanında **"Vazgeç"** vardır (Esc de aynı işi yapar); vazgeçince alan temizlenip kapanır, mevcut
    key değişmez. Yazılmış ama kaydedilmemiş key hiçbir yerde taslak olarak saklanmaz (sayfadan ayrılınca kaybolur).

**Ayarlar sayfasının temeli**
14. Üst barın sağındaki "Ayarlar" düğmesi artık çalışır ve Ayarlar'ı açar (0001'de pasifti). Bu spec'te Ayarlar'ın
    bölümleri **"API key'ler"** (her online sağlayıcı bir satırda; durumu, doğrulama etiketi ve ekle/değiştir/sil
    eylemleri) ve salt okunur **"Modeller"** listesidir. Ayarlar, sonraki spec'lerin bölüm ekleyebileceği bir yapıda
    kurulur (ör. önerileri sıfırla 0016, ajan 0009).

## User experience
- Flow:
  1. Giriş yapmış kullanıcı üst bardaki **Ayarlar**'a tıklar → Ayarlar açılır, "API key'ler" bölümünde sağlayıcı
     satırları görünür; hiç key yoksa her satırda "Girilmemiş" yazar.
  2. Anthropic satırında **Ekle**'ye tıklar → gizli (şifre tipi) giriş alanına key'i yapıştırır → **Kaydet** → key
     doğrulanır → satır "sk-…a3f · Doğrulandı · az önce güncellendi" olur; giriş alanı temizlenir. Sağlayıcı key'i
     reddederse alanın yanında "Sağlayıcı bu key'i reddetti" görünür ve key kaydedilmez.
  3. **Değiştir** ile yeni key girer → eski key'in yerine geçer; **Vazgeç** ya da Esc ile alan kapanır, key değişmez.
     **Sil** → onay penceresi → onaylarsa satır "Girilmemiş"e döner.
  4. "Modeller" listesinde Anthropic modelleri artık "Kullanılabilir", key'i olmayan sağlayıcılarınki "Key yok"
     olarak görünür; tür ve sağlayıcı filtresiyle liste daraltılır.
- States:
  - Ayarlar / API key'ler: yükleniyor · boş (hiç key yok, her satır "Girilmemiş") · key kayıtlı (maskeli + doğrulama
    etiketi + zaman) · key okunamıyor ("Key okunamıyor — lütfen yeniden girin") · düzenleniyor (giriş alanı, Kaydet,
    Vazgeç) · kaydediliyor/doğrulanıyor (düğme pasif) · doğrulama hatası (alan yanında mesaj; biçim ya da sağlayıcı
    reddi) · kaydetme/silme hatası (genel hata mesajı + korelasyon no) · silme onayı · başarılı (kısa bildirim).
  - Modeller listesi: yükleniyor · liste (Kullanılabilir / Key yok / Key okunamıyor etiketleriyle) · seçilen filtrede
    hiç model yok (boş durum metni) · hata.
  - Yetkisiz: giriş yapmamış kullanıcı Ayarlar'a gidemez, giriş sayfasına yönlenir (0002).
- Design reference: `source/konseptler sayfası tasarımı.png` — commit `61cc824` sürümü: sağ üstteki "ayarlar"
  düğmesi. Ayarlar sayfasının içi için tasarım yok: none — the flow above is the design. Renk: turuncu/kahve tema
  değişkenleri (K-7).
- Accessibility: Ayarlar'daki tüm eylemler klavyeyle yapılabilir (Tab, Enter/Space; onay penceresinde ve düzenleme
  alanında Esc = vazgeç, odak onay penceresinde tutulur); key giriş alanının etiketi sağlayıcı adını içerir ve şifre
  tipindedir; hata mesajları alana bağlıdır (`aria-describedby`); kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Mimari sınırlar: çözülmüş key'i gören ve online sağlayıcıyı (doğrulama dahil) çağıran tek yer AiGateway'dir (Y-7);
  key'ler Pi'den hiç çıkmaz, ajana gönderilmez (Y-8). Testlerde gerçek sağlayıcı çağrısı yapılmaz (stub); gerçek
  doğrulama yalnızca elle başlatılan smoke testle (`docs/testing.md`).
- Maskeleme ve log tarama mekanizması 0008'dedir (Ç-9); bu spec yalnızca kendi alanlarını kanıtlar.
- Para: yalnızca USD (E-3); fiyatlar katalogda sentin kesirlerini kaybetmeden tutulur.
- Hassas alan kuralı gereği H-2 kriterleri (AC-3, AC-4, AC-5, AC-9, AC-10) kasıtlı bozma kanıtıyla doğrulanır.
- Out of scope: çağrı kapısı, kullanılabilirlik zorlaması, model yetkisi (`model.use:<model>`) ve "Yetki yok" etiketi,
  token limiti/efor/yeniden deneme kuralları, harcama kaydı ve aylık gösterge (0013) · model bilgisi modalı ve
  model seçimleri (0005; seçimler profil başına) · video kodu/görsel/ses model seçimi ve video motoru (0006, 0017) ·
  "ses" ve "video motoru" türleri (0006) · online ses girişi ElevenLabs ve karakter başı fiyat (0017) · yerel LLM'ler ve Ollama (v1'de yok) · yerel görsel/ses modelleri ve video
  motorlarının katalog girişleri (0006) · key'i silinmiş sağlayıcıyı kullanan sıradaki işin Hata olması (0017) · admin'in katalog, fiyat ve yetki
  düzenleme ekranları (v1'de yok) · sağlayıcı başına birden fazla key / key rotasyonu · fal gibi ek bulut görsel
  sağlayıcıları ve modelleri (fal Z-Image-Turbo, Google Nano Banana — backlog) · TL gösterimi (ileride) · fatura/ödeme.
- Split: done — 0004 Katalog, API key yönetimi ve Ayarlar temeli / kardeşler: 0013

## Acceptance criteria
**Katalog**
- [ ] AC-1 — Katalog listesi Anthropic, OpenAI, OpenRouter, GLM ve Kimi'yi; her metin sağlayıcısı için 1–3 modeli ve online görsel model olarak yalnızca OpenAI'nin görsel modelini (gpt-image; fal ya da Google görsel modeli yok) döndürür; her model için sağlayıcı, ad, tür (metin/görsel), desteklenen efor seviyeleri, varsayılan efor, çıktı token limiti (metin), çağrı zaman aşımı ve USD birim fiyatı vardır; katalogda yerel LLM (Ollama), ses türü ve OpenAI TTS yoktur (bu spec'in seed'inde yerel giriş bulunmaz; yerel görsel/ses/video girişleri 0006 ile eklenir).
- [ ] AC-2 — Bir modelin çıktı token limiti, zaman aşımı ya da fiyatı yalnızca seed verisi değiştirilerek değişir ve katalog listesi yeni değeri gösterir (davranışa gömülü sabit yok — test, katalog değeri değiştirilerek kanıtlanır; çağrının yeni değeri kullanması 0013 AC-4 ve AC-12).

**API key (H-2)**
- [ ] AC-3 — Kullanıcı bir sağlayıcı için geçerli bir key kaydeder; Ayarlar'da yalnızca maskeli hali (`ilk ≤3 … son 3`), doğrulama etiketi ve güncellenme zamanı görünür; giriş alanı temizlenir; sayfa yenilense de tam key hiçbir yanıtta dönmez.
- [ ] AC-4 — Veritabanında key düz metin olarak bulunmaz (kayıtlı değerde girilen key'in hiçbir 8+ karakterlik parçası geçmez); şifreleme anahtarları olmadan değer çözülemez.
- [ ] AC-5 — Key kaydetme, doğrulama (başarılı ve 401), değiştirme ve silme akışlarında üretilen tüm teknik loglar, Günlük satırları ve hata yanıtları 0008'in tarama yöntemiyle taranır; girilen key'in düz hali hiçbirinde yoktur, varsa yalnızca maskeli hali vardır.
- [ ] AC-6 — Boş, yalnızca boşluk, 8 karakterden kısa, 512 karakterden uzun ya da kontrol karakteri içeren key reddedilir, alanın yanında Türkçe mesaj görünür ve mevcut key değişmez; baş/son boşluklu key kırpılarak kaydedilir; sağlayıcı öneki olmayan 8–512 karakterlik key biçim nedeniyle reddedilmez.
- [ ] AC-7 — "Değiştir" yeni key'i eskisinin yerine koyar (maskeli hal yeni key'e döner, sonraki doğrulama yeni key'le yapılır); bir sağlayıcı için aynı anda en fazla bir key vardır.
- [ ] AC-8 — "Sil" önce Req 11'deki onay metnini gösterir; vazgeçilirse key kalır; onaylanırsa key kalıcı silinir, satır "Girilmemiş" olur ve o sağlayıcının modelleri "Key yok" olarak görünür.
- [ ] AC-9 — Kullanıcı A (admin rolündeki kullanıcı dahil, BR-1), kullanıcı B'nin key'ini — maskeli hali dahil — hiçbir yoldan göremez, değiştiremez, silemez; giriş yapmamış istek Ayarlar'a ve key işlemlerine erişemez (giriş sayfasına yönlenir / yetkisiz yanıt). Kasıtlı bozma: sahiplik kontrolü kaldırılınca test kırmızıya döner.
- [ ] AC-10 — Doğrulama: stub sağlayıcı maliyetsiz uçta 200 dönerse key "Doğrulandı" ile kaydedilir; 401 dönerse key kaydedilmez, "Sağlayıcı bu key'i reddetti" görünür ve önceki key değişmez; zaman aşımı (10 sn), 429, 5xx ya da maliyetsiz yolu olmayan sağlayıcıda key "Doğrulanmadı" ile kaydedilir; her durumda stub tam 1 kez çağrılır, token harcayan uç hiç çağrılmaz ve harcama oluşmaz.
- [ ] AC-11 — Şifre çözme anahtarı değiştirilmiş (çözülemeyen) bir key'in satırı "Key okunamıyor — lütfen yeniden girin" gösterir, o sağlayıcının modelleri "Key okunamıyor" etiketini alır; "Değiştir" ile girilen yeni key satırı normale döndürür.
- [ ] AC-12 — Düzenleme alanında "Vazgeç" ya da Esc alanı temizleyip kapatır ve mevcut key'i değiştirmez; kaydedilmemiş key sunucuya gönderilmez ve tarayıcı depolamasında saklanmaz.

**Ayarlar**
- [ ] AC-13 — Üst bardaki "Ayarlar" düğmesi (0001'de pasif) Ayarlar'ı açar; "API key'ler" bölümünde katalogdaki her online sağlayıcı bir satır olarak görünür; hiç key yoksa her satır "Girilmemiş" yazar (boş durum); "Modeller" bölümü aynı sayfadadır.
- [ ] AC-14 — Ayarlar verisi gelene kadar yükleniyor durumu görünür; "Ekle"/"Değiştir" ile düzenleniyor durumunda şifre tipinde giriş alanı, "Kaydet" ve "Vazgeç" görünür; kaydederken/doğrularken "Kaydet" pasiftir ve art arda tıklama tek kayıt ve tek doğrulama üretir; başarıda kısa bir Türkçe bildirim görünür.
- [ ] AC-15 — Kaydetme ya da silme sunucu hatasıyla bitince Türkçe genel hata mesajı ve korelasyon numarası görünür, satırın önceki durumu (maskeli key ya da "Girilmemiş") değişmez ve mesajda girilen key yer almaz.
- [ ] AC-16 — Modeller listesi: yükleniyor durumu görünür; her model "Kullanılabilir" / "Key yok" / "Key okunamıyor" etiketlerinden biriyle gösterilir; tür ve sağlayıcı filtresi yalnızca eşleşen modelleri getirir; eşleşen yoksa boş durum metni, katalog alınamazsa Türkçe hata mesajı görünür.
- [ ] AC-17 — Ayarlar'daki tüm eylemler yalnızca klavyeyle tamamlanabilir; key alanı şifre tipindedir ve sağlayıcı adını içeren bir etikete sahiptir; hata mesajları alana bağlıdır; onay penceresinde Esc vazgeçer ve odak pencerede kalır; renkler yalnızca tema değişkenlerinden gelir (K-7), kontrast her iki temada WCAG AA.

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
