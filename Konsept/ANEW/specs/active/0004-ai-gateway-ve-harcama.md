# Spec 0004 — ai-gateway-ve-harcama

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0004-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
İçerik üreticisi, konsept ve video üretiminde kendi API key'leriyle ve kendi parasıyla online modelleri
(Anthropic, OpenAI, OpenRouter, GLM, Kimi…) kullanır (`ilk_promt.txt`, `docs/domain.md` Kullanıcılar). Bu spec
bunun zeminini kuruyor: sistemin tanıdığı sağlayıcı ve model kataloğu (metin/görsel ayrımı, efor seviyeleri,
çıktı token limiti, fiyat), kullanıcının API key'lerini Ayarlar'dan güvenle girip güncelleyip silebilmesi,
yalnızca yetkili ve key'i girilmiş modellerin kullanılabilmesi, her online çağrının token ve maliyetinin
kaydedilmesi ve üst bardaki aylık harcama göstergesi. Başarı: sonraki spec'ler (0005 Konseptler, 0006 Üretim)
online modeli yalnızca bu kapıdan çağırır; harcama sent dahil doğru görünür; hiçbir key düz metin olarak
saklanmaz, loglanmaz ya da kullanıcıya geri gösterilmez; bir hata durumunda çağrı kontrolsüz tekrarlanmaz.
Bilinçli olarak yapılmayanlar: model seçim ekranları (model bilgisi modalı 0005'te, video model seçimleri
0006'da), harcama limiti ya da bütçe (BR-12: yalnızca bilgi), yerel modeller (Ollama vb., ajan tarafı), admin'in
katalog/yetki yönetim ekranları (0010), online ses ve görsel sağlayıcılarının kesin listesi (B-5 raporu kararı bekliyor).

## Requirements
**Katalog**
1. Sistem bir sağlayıcı ve model kataloğu tutar. Her sağlayıcının adı vardır; ilk sürümdeki online sağlayıcılar
   Anthropic, OpenAI, OpenRouter, GLM ve Kimi'dir (`ilk_promt.txt`; kesin liste ve ilk model seti → Open questions S-1).
2. Katalogdaki her model için şunlar bilinir: bağlı olduğu sağlayıcı, görünen adı, türü (**metin** ya da **görsel**),
   desteklediği efor seviyeleri (low'dan max'a; hiç desteklemeyebilir), varsayılan efor seviyesi, **çıktı token
   limiti** (yalnızca metin modelleri) ve maliyet hesaplamaya yeten birim fiyatı (metin: giriş ve çıkış token başına;
   görsel: üretilen görsel başına). Token limitleri ve fiyatlar katalogdadır; davranışa gömülü sabit değer yoktur
   (`docs/conventions.md`, Data rules).
3. Kullanıcı, kataloğu tür (metin/görsel) ve sağlayıcıya göre listeleyebilir; her modelin yanında kullanılabilir olup
   olmadığı ve kullanılamıyorsa nedeni (key yok / yetki yok) görünür. Bu liste sonraki spec'lerin seçim ekranlarının
   kaynağıdır.

**API key yönetimi (H-2)**
4. Kullanıcı Ayarlar'da her online sağlayıcı için **bir** API key girebilir, mevcut key'i yenisiyle değiştirebilir ve
   silebilir. Key'ler kullanıcıya aittir; başka kullanıcı göremez ve kullanamaz (BR-1).
5. Girilen key baştaki/sondaki boşluklardan arındırılır; boş, yalnızca boşluk ya da izin verilen uzunluk dışında
   (önerilen sınır 8–512 karakter; kesin sınır Open questions SC-3) bir değer reddedilir ve alanın yanında Türkçe hata gösterilir.
6. Kaydedilen key bir daha **tam haliyle hiçbir yerde gösterilmez**: Ayarlar'da yalnızca maskeli hali (`sk-…a3f`
   biçiminde: ilk en fazla 3 ve son 3 karakter) ve son güncellenme zamanı görünür. Key düz metin olarak saklanmaz,
   hiçbir log, hata mesajı, Günlük satırı ya da yanıt içinde düz metin yer almaz (`docs/domain.md` invariants).
7. Key silinmeden önce onay istenir; onaylanınca key kalıcı olarak gider ve o sağlayıcının modelleri "key yok"
   nedeniyle kullanılamaz hale gelir. Geçmiş harcama kayıtları silinmez.
8. Key kaydedilirken sağlayıcıya doğrulanıp doğrulanmayacağı → Open questions S-3 (öneri: maliyetsiz bir doğrulama).

**Kullanılabilirlik ve yetki (BR-13)**
9. Bir model ancak (a) kullanıcının o modeli kullanma yetkisi varsa ve (b) modelin sağlayıcısı için kullanıcının
   kayıtlı key'i varsa kullanılabilir. Kullanılamayan bir model için yapılan çağrı **sağlayıcıya hiç gitmeden**
   reddedilir, maliyet oluşmaz ve neden Türkçe bildirilir ("Anthropic için API key girilmemiş — Ayarlar" /
   "Bu modeli kullanma yetkiniz yok"). Karar her zaman sunucudadır; arayüzün gizlemesi yeterli sayılmaz.
10. Varsayılan kullanıcı rolünün hangi modellere yetkili olduğu → Open questions S-4.

**Çağrı kuralları (H-3)**
11. Bir metin çağrısının çıktı token üst sınırı, seçilen modelin katalogdaki limitini asla aşmaz. Çağıran özellik
    daha yüksek bir sınır isterse istek katalog limitine indirilir. Çıktı bu sınıra takılıp yarım kalırsa sonuç
    "yarım kaldı" olarak işaretlenir ve çağıran özelliğe bildirilir; sessizce tam sonuç gibi kabul edilmez.
12. Seçilen efor seviyesi modelin desteklediklerinden biri değilse çağrı sağlayıcıya gitmeden reddedilir; efor
    belirtilmezse modelin katalogdaki varsayılanı kullanılır.
13. Yeniden deneme yalnızca sağlayıcı **429** ya da **5xx** döndürdüğünde yapılır: en fazla **2 tekrar** (toplam en fazla
    3 deneme), her tekrarda bekleme artar. Diğer 4xx hatalarında (geçersiz key, yetersiz bakiye, hatalı istek) hiç
    tekrar yapılmaz (`docs/security.md`). Ağ zaman aşımında davranış → Open questions S-5.
14. Hata kullanıcıya Türkçe, sade bir mesajla döner (ör. "Sağlayıcı API key'i reddetti", "Sağlayıcı şu an yoğun,
    tekrar denendi ama olmadı"); sağlayıcının ham yanıtı, key ya da iç ayrıntı gösterilmez.

**Harcama kaydı (BR-12)**
15. Sağlayıcıya ulaşan **her deneme** (başarılı, başarısız ya da yeniden deneme olsun) için bir harcama kaydı tutulur:
    zaman, kullanıcı, sağlayıcı, model, efor, amaç (model rolü: konsept üretimi, denetim, konu önerisi, video kodu,
    görsel, ses), varsa ilgili profil, giriş/çıkış token sayısı (görselde görsel adedi), sonuç (başarılı / hata /
    yarım kaldı) ve maliyet. Sağlayıcıya hiç gitmeyen (reddedilen) istek kayıt ve maliyet üretmez.
16. Maliyet, çağrı anındaki katalog fiyatıyla ve sentin kesirlerini kaybetmeden USD olarak hesaplanır; katalog
    fiyatı sonradan değişse de eski kayıtlar değişmez. Sağlayıcı token sayısını bildirmezse → Open questions SC-4.

**Aylık harcama göstergesi**
17. Üst barda, logo ve "ANEW Konsept" adının hemen yanında aylık harcama görünür (tasarım): kullanıcının içinde
    bulunulan takvim ayındaki tüm harcama kayıtlarının toplamı, **tüm profiller dahil**, iki ondalık haneyle ve sent
    dahil (ör. "Bu ay: $2,37"). Ayın ilk gününde sıfırdan başlar. Ay sınırının hangi saat dilimine göre belirlendiği
    → Open questions S-6.
18. Gösterge yalnızca bilgi amaçlıdır: hiçbir tutar bir çağrıyı engellemez, uyarı ya da limit yoktur (BR-12).
19. Yeni bir harcama kaydı oluştuğunda gösterge sayfa yenilenmeden güncellenir. Brief süre vermiyor; üst sınır
    → Open questions SC-9 (öneri: en geç 10 saniye).

**Ayarlar sayfasının temeli**
20. Üst barın sağındaki "Ayarlar" düğmesi artık çalışır ve Ayarlar'ı açar (0001'de pasifti). Bu spec'te Ayarlar'ın tek
    bölümü "API key'ler"dir: her online sağlayıcı bir satırda, durumu (girilmemiş / maskeli key + güncellenme zamanı)
    ve ekle/değiştir/sil eylemleriyle. Ayarlar, sonraki spec'lerin bölüm ekleyebileceği (ör. "Önerileri sıfırla"
    0005, ajan 0009) bir yapıda kurulur.

## User experience
- Flow:
  1. Giriş yapmış kullanıcı üst bardaki **Ayarlar**'a tıklar → Ayarlar açılır, "API key'ler" bölümünde sağlayıcı satırları
     görünür; hiç key yoksa her satırda "Girilmemiş" yazar.
  2. Anthropic satırında **Ekle**'ye tıklar → gizli (şifre tipi) giriş alanına key'i yapıştırır → **Kaydet** → satır
     "sk-…a3f · az önce güncellendi" olur; giriş alanı temizlenir.
  3. **Değiştir** ile yeni key girer → eski key'in yerine geçer. **Sil** → "Bu key silinecek; Anthropic modelleri
     kullanılamaz olacak" onayı → onaylarsa satır "Girilmemiş"e döner.
  4. Model listesinde (Ayarlar'da salt okunur katalog görünümü, Open questions S-2) Anthropic modelleri artık
     "Kullanılabilir", key'i olmayan sağlayıcılarınki "Key yok" olarak görünür.
  5. Sonraki spec'lerde bir üretim yapıldıkça üst bardaki "Bu ay: $0,00" tutarı birkaç saniye içinde artar.
- States:
  - Ayarlar / API key'ler: yükleniyor · boş (hiç key yok, her satır "Girilmemiş") · key kayıtlı (maskeli + zaman) ·
    düzenleniyor (giriş alanı açık) · kaydediliyor (düğme pasif) · doğrulama hatası (alan yanında mesaj) ·
    kaydetme/silme hatası (genel hata mesajı + korelasyon no) · silme onayı · başarılı (kısa bildirim).
  - Katalog görünümü: yükleniyor · liste (Kullanılabilir / Key yok / Yetki yok etiketleriyle) · tür filtresinde hiç
    model yok (boş durum metni) · hata.
  - Aylık harcama göstergesi: yükleniyor · hiç kayıt yok ("$0,00") · tutar · okunamadı ("—" ve "Harcama alınamadı" ipucu).
  - Yetkisiz: giriş yapmamış kullanıcı Ayarlar'a gidemez, giriş sayfasına yönlenir (0002); gösterge yalnızca girişliyken görünür.
- Design reference: `source/konseptler sayfası tasarımı.png` — commit `61cc824` sürümü: üst barda "Aylık harcama"
  alanının yeri (logo + program adının sağı) ve sağ üstteki "ayarlar" düğmesi. Ayarlar sayfasının içi için tasarım
  yok: none — the flow above is the design. Renk: turuncu/kahve tema değişkenleri (K-7).
- Accessibility: Ayarlar'daki tüm eylemler klavyeyle yapılabilir (Tab, Enter/Space; onay penceresinde Esc = vazgeç,
  odak onay penceresinde tutulur); key giriş alanının etiketi sağlayıcı adını içerir ve şifre tipindedir (ekran okuyucu
  ve omuz üstü okumaya karşı); hata mesajları alana bağlıdır (`aria-describedby`); harcama göstergesinin erişilebilir
  adı "Bu ayki harcama: 2,37 dolar" biçimindedir; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-2 Gizli bilgiler (API key saklama, maskeleme, loglama).
- Sensitive area: H-3 Maliyet doğuran çağrılar (çağrı kapısı, token limiti, yeniden deneme, harcama kaydı).
- Sensitive area: H-1 Kimlik ve yetki (yalnızca model kullanma yetkisi kontrolü, BR-13 — `model.use:<model>`; ayrıntı SC-1).
- Mimari sınırlar: çözülmüş key'i gören ve online sağlayıcıyı çağıran tek yer AiGateway'dir (Y-7); key'ler Pi'den
  hiç çıkmaz, ajana gönderilmez (Y-8). Testlerde gerçek sağlayıcı çağrısı yapılmaz (stub); gerçek deneme yalnızca elle
  başlatılan smoke testle (`docs/testing.md`).
- Bağımlılık: 0002 Kimlik (kullanıcı, rol, yetki) ve 0003 Profiller (harcama kaydındaki profil) tamamlanmış olmalı.
- Para: USD, sentin kesirleri korunur, ekranda 2 hane ve Türkçe ondalık virgül (`$2,37`).
- Out of scope: model bilgisi modalı ve konsept modeli seçimi (0005) · video kodu/görsel/ses model seçimi ve video
  motoru (0006) · yerel modeller ve Ollama (ajan, 0006/0009) · harcama limiti, bütçe, uyarı (BR-12 gereği hiç yok) ·
  ayrıntılı harcama raporu/grafiği · admin'in katalog, fiyat ve yetki düzenleme ekranları ve tüm kullanıcıların
  harcama istatistiği (0010) · online TTS/görsel sağlayıcılarının (ElevenLabs, fal, Google…) eklenmesi (B-5 raporu
  kararından sonra; Open questions S-1) · birden fazla key / sağlayıcı başına key rotasyonu · fatura/ödeme.
- Split: proposed **0004a** Katalog + API key yönetimi + Ayarlar temeli (H-2; Req 1–8, 20) → ardından **0004b** Çağrı
  kapısı: kullanılabilirlik/yetki, token limiti, efor, yeniden deneme, harcama kaydı ve aylık gösterge (H-3; Req 9–19).
  Gerekçe: iki ayrı kullanıcı yeteneği, iki ayrı hassas alan ve 20+ kriter; 0004a tek başına gösterilebilir (key
  girilir, maskeli görünür), 0004b ise 0005'in ilk gerçek çağrısından önce bitmeli. Karar insanda (Open questions S-7).

## Acceptance criteria
**Katalog**
- [ ] AC-1 — Katalog listesi ilk sürüm sağlayıcılarını (S-1 kararı) ve her model için sağlayıcı, ad, tür (metin/görsel), desteklenen efor seviyeleri, varsayılan efor, çıktı token limiti (metin) ve birim fiyatı döndürür; tür ve sağlayıcı filtresi yalnızca eşleşen modelleri getirir; eşleşen yoksa boş liste ve boş durum metni görünür.
- [ ] AC-2 — Bir modelin çıktı token limiti ya da fiyatı yalnızca katalog verisi değiştirilerek değişir; çağrı davranışı yeni değeri kullanır (davranışa gömülü sabit yok — test, katalog değeri değiştirilerek kanıtlanır).

**API key (H-2)**
- [ ] AC-3 — Kullanıcı bir sağlayıcı için geçerli bir key kaydeder; Ayarlar'da yalnızca maskeli hali (`ilk ≤3 … son 3`) ve güncellenme zamanı görünür; giriş alanı temizlenir; sayfa yenilense de tam key hiçbir yanıtta dönmez.
- [ ] AC-4 — Veritabanında key düz metin olarak bulunmaz (kayıtlı değerde girilen key'in hiçbir 8+ karakterlik parçası geçmez); şifreleme anahtarları olmadan değer çözülemez.
- [ ] AC-5 — Key kaydetme, değiştirme, silme ve key kullanan bir çağrının hata aldığı akışlarda üretilen tüm teknik loglar, Günlük satırları ve hata yanıtları taranır; girilen key'in düz hali hiçbirinde yoktur, varsa yalnızca maskeli hali vardır.
- [ ] AC-6 — Boş, yalnızca boşluk, 8 karakterden kısa ya da 512'den uzun key (sınırlar SC-3 kararına bağlı) reddedilir, alanın yanında Türkçe mesaj görünür ve mevcut key değişmez; baş/son boşluklu key kırpılarak kaydedilir.
- [ ] AC-7 — "Değiştir" yeni key'i eskisinin yerine koyar (sonraki çağrı yeni key'i kullanır); bir sağlayıcı için aynı anda en fazla bir key vardır.
- [ ] AC-8 — "Sil" önce onay ister; vazgeçilirse key kalır; onaylanırsa key kalıcı silinir, satır "Girilmemiş" olur, o sağlayıcının modelleri "Key yok" olarak görünür ve geçmiş harcama kayıtları yerinde kalır.
- [ ] AC-9 — Kullanıcı A (admin rolündeki kullanıcı dahil, BR-1), kullanıcı B'nin key'ini — maskeli hali dahil — hiçbir yoldan göremez, değiştiremez, silemez ve B'nin key'iyle çağrı yaptıramaz; giriş yapmamış istek Ayarlar'a ve key işlemlerine erişemez (giriş sayfasına yönlenir / yetkisiz yanıt).

**Kullanılabilirlik ve çağrı kuralları (H-3)**
- [ ] AC-10 — Key'i girilmemiş sağlayıcının modeliyle yapılan çağrı sağlayıcıya gitmeden "… için API key girilmemiş" ile reddedilir; harcama kaydı ve maliyet oluşmaz (stub sağlayıcı hiç çağrılmamış olmalı).
- [ ] AC-11 — Kullanıcının yetkisi olmayan modelle yapılan çağrı, key kayıtlı olsa bile, sağlayıcıya gitmeden "Bu modeli kullanma yetkiniz yok" ile reddedilir; arayüzü atlayan doğrudan istek de reddedilir; katalog listesinde model "Yetki yok" görünür.
- [ ] AC-12 — Çıktı sınırı istenmeyen ya da katalog limitinden yüksek istenen çağrı sağlayıcıya katalog limitiyle gider; limitin altındaki istek olduğu gibi gider; çıktı sınıra takılırsa sonuç "yarım kaldı" olarak işaretlenir ve kayıtta bu sonuç görünür.
- [ ] AC-13 — Modelin desteklemediği efor seviyesiyle çağrı sağlayıcıya gitmeden reddedilir; efor belirtilmezse katalogdaki varsayılan efor kullanılır.
- [ ] AC-14 — Stub sağlayıcı 429 ya da 5xx döndürdüğünde çağrı en fazla 2 kez tekrarlanır (toplam ≤3 deneme), ardışık beklemeler artar (sabit saatle ölçülür); 2. tekrarda başarılı olursa sonuç başarılıdır; 3 denemede de başarısızsa Türkçe hata döner ve 4. deneme yapılmaz.
- [ ] AC-15 — Stub sağlayıcı 400, 401, 402, 403 ya da 404 döndürdüğünde hiç tekrar yapılmaz (tam 1 deneme) ve kullanıcıya ham yanıt ya da key içermeyen Türkçe mesaj döner (401 → "Sağlayıcı API key'i reddetti").

**Harcama kaydı (BR-12)**
- [ ] AC-16 — Başarılı bir metin çağrısından sonra bir harcama kaydı oluşur: zaman, sağlayıcı, model, efor, amaç, profil, giriş/çıkış token ve maliyet = giriş token × giriş fiyatı + çıkış token × çıkış fiyatı; sent altı kesirler korunur (ör. 1 234 giriş + 567 çıkış token için beklenen tutar 6 hane hassasiyetle eşleşir).
- [ ] AC-17 — Yeniden denemeli bir çağrıda sağlayıcıya ulaşan her deneme ayrı kayıt olur (ör. 429 + 429 + başarı → 3 kayıt, sonuçlarıyla); görsel çağrısının maliyeti görsel adedi × görsel fiyatıdır.
- [ ] AC-18 — Katalog fiyatı değiştirildikten sonra eski kayıtların maliyeti değişmez, yeni çağrılar yeni fiyatla kaydedilir.

**Aylık harcama göstergesi**
- [ ] AC-19 — Üst bar, girişli kullanıcının bu takvim ayındaki tüm profillerine ait kayıtların toplamını iki haneyle gösterir (ör. 1,234567 + 1,131 → "$2,37"); önceki aya ve başka kullanıcılara ait kayıtlar toplama girmez; hiç kayıt yoksa "$0,00" görünür.
- [ ] AC-20 — Ay sınırı: S-6'da seçilen saat dilimine göre ayın son günü 23:59:59'daki kayıt o aya, ertesi ayın ilk anındaki kayıt yeni aya sayılır (sabit saatle test edilir).
- [ ] AC-21 — Yeni bir kayıt oluştuktan sonra gösterge sayfa yenilenmeden en geç 10 sn (SC-9 kararına bağlı) içinde yeni toplamı gösterir; gösterge verisi alınamazsa "—" ve "Harcama alınamadı" ipucu görünür, sayfanın geri kalanı çalışmaya devam eder.
- [ ] AC-22 — Toplam ne kadar yüksek olursa olsun hiçbir çağrı harcama yüzünden engellenmez ve uyarı gösterilmez (BR-12).

**Ayarlar**
- [ ] AC-23 — Üst bardaki "Ayarlar" düğmesi (0001'de pasif) Ayarlar'ı açar; "API key'ler" bölümünde katalogdaki her online sağlayıcı bir satır olarak görünür; hiç key yoksa her satır "Girilmemiş" yazar (boş durum).
- [ ] AC-24 — Ayarlar verisi gelene kadar yükleniyor durumu görünür; "Ekle"/"Değiştir" ile düzenleniyor durumunda şifre tipinde giriş alanı ve "Kaydet" görünür; kaydederken "Kaydet" pasiftir ve art arda tıklama tek kayıt üretir; başarıda kısa bir Türkçe bildirim görünür.
- [ ] AC-25 — Kaydetme ya da silme sunucu hatasıyla bitince Türkçe genel hata mesajı ve korelasyon numarası görünür, satırın önceki durumu (maskeli key ya da "Girilmemiş") değişmez ve mesajda girilen key yer almaz.
- [ ] AC-26 — Katalog görünümü: yükleniyor durumu görünür; liste her modeli "Kullanılabilir" / "Key yok" / "Yetki yok" etiketlerinden biriyle gösterir; seçilen tür/sağlayıcı filtresinde model yoksa boş durum metni, katalog alınamazsa Türkçe hata mesajı görünür.
- [ ] AC-27 — Harcama göstergesi veri gelene kadar yükleniyor durumu gösterir; giriş yapmamış kullanıcıya gösterge hiç görünmez.
- [ ] AC-28 — Ayarlar'daki tüm eylemler yalnızca klavyeyle tamamlanabilir; key alanı şifre tipindedir ve sağlayıcı adını içeren bir etikete sahiptir; onay penceresinde Esc vazgeçer; harcama göstergesinin erişilebilir adı vardır; renkler yalnızca tema değişkenlerinden gelir (K-7).

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir.

**(a) Clarify soruları (etkiye göre sıralı)**
1. **S-1 — İlk katalog içeriği ve online ses/görsel sağlayıcıları.** Hangi sağlayıcılar ve modeller ilk sürümde katalogda olsun; B-5 raporundaki online TTS (ElevenLabs) ve bulut görsel (fal Z-Image-Turbo, Nano Banana 2 Lite) bu spec'e mi girsin?
   - Öneri: Metin için Anthropic, OpenAI, OpenRouter, GLM, Kimi; her birinden sizin test ettiğiniz 1–3 model (ör. Claude Opus/Sonnet/Haiku). Katalog türü şimdilik **metin + görsel**; görsel sağlayıcı olarak yalnızca OpenAI (gpt-image) ve, B-5 kararı "evet" çıkarsa, fal. Online ses (ElevenLabs) için katalogda üçüncü bir tür (**ses**, karakter başı fiyat) 0006 ile eklensin.
   - Gerekçe: Brief yalnızca bu beş metin sağlayıcısını adıyla sayıyor; B-5 önerileri henüz kararınızı bekliyor. Ses türünü şimdi eklemek, fiyat modeli (karakter) ve zaman damgası gibi henüz karar verilmemiş ayrıntıları bu spec'e taşır.
2. **S-2 — Katalog ve fiyatları kim günceller, kullanıcı kataloğu nerede görür?**
   - Öneri: Katalog, sürümlenen bir seed verisi olarak sistemle gelir; fiyat/limit değişikliğini geliştirici yapar (patch sürüm); admin düzenleme ekranı 0010'a. Kullanıcı kataloğu Ayarlar'da salt okunur "Modeller" listesi olarak görür. OpenRouter gibi yanıtında gerçek maliyeti bildiren sağlayıcılarda bildirilen maliyet esas alınsın.
   - Gerekçe: Tek kullanıcı ve LAN için en ucuz ve test edilebilir yol; fiyatların bayatlama riski sağlayıcı-bildirimli maliyetle azalır.
3. **S-3 — Key kaydedilirken sağlayıcıda doğrulansın mı?**
   - Öneri: Evet, ama **maliyetsiz** bir yolla (sağlayıcının model listesi gibi token harcamayan bir uç); maliyetsiz yolu olmayan sağlayıcıda doğrulama yapılmasın, key "doğrulanmadı" etiketiyle kaydedilsin. Doğrulama 401 dönerse key kaydedilmez.
   - Gerekçe: Yanlış yapıştırılan key'i kuyrukta 200 videolu bir işin ortasında değil, girildiği anda yakalar; H-3 gereği doğrulama para harcamamalı.
4. **S-4 — Varsayılan rolün model yetkileri.**
   - Öneri: Varsayılan "kullanıcı" rolü katalogdaki tüm modellere yetkili (`model.use:*` karşılığı); kısıtlama yeteneği bu spec'te kurulur ve testlenir, kısıtlayan ekran 0010'da gelir.
   - Gerekçe: Brief "herkes her modeli kullanamayacak" diyor ama bugün tek kullanıcı var; kısıtlama mekanizması şimdi kurulup varsayılanı açık tutmak sizi kendi modelinizden kilitlemez.
5. **S-5 — Ağ zaman aşımı ve bağlantı kopması yeniden deneme sayılsın mı; çağrı zaman aşımı ne olsun?**
   - Öneri: Bağlantı kurulamadıysa (istek sağlayıcıya ulaşmadıysa) 5xx gibi tekrar edilsin (≤2); istek gönderilip yanıt beklenirken zaman aşımı olursa **tekrar edilmesin** (sağlayıcı işlemiş ve ücretlendirmiş olabilir), "yanıt alınamadı" kaydı düşülsün. Zaman aşımı modelin çıktı limitine göre katalogda tutulsun (varsayılan 10 dk).
   - Gerekçe: `docs/security.md` yalnızca 429/5xx diyor; yanıt beklerken tekrar, çift ücret riski taşır (H-3).
6. **S-6 — "Bu ay" hangi saat dilimine göre?**
   - Öneri: Kullanıcının saat dilimi; şimdilik tek kullanıcı için **Europe/Istanbul** sabit ayar, ileride kullanıcı ayarı.
   - Gerekçe: Veri UTC tutuluyor (`docs/conventions.md`); UTC ay sınırı Türkiye'de ayın ilk gecesi 03:00'e kadar olan harcamayı önceki aya yazar ve kullanıcının sağlayıcı faturasıyla tutmaz.
7. **S-7 — Split.** 0004'ü 0004a (Katalog + key + Ayarlar, H-2) ve 0004b (çağrı kapısı + harcama + gösterge, H-3) olarak bölelim mi?
   - Öneri: Bölelim; sıra 0004a → 0004b, ikisi de 0005'ten önce.
   - Gerekçe: 28 kriter, iki yetenek, iki hassas alan; hassas alan kuralı her kriter için kasıtlı bozma kanıtı istediğinden küçük dilimler review ve doğrulamayı belirgin biçimde kısaltır.

**(b) Self-critique boşlukları (düşmanca okuyan geliştirici gözüyle)**
- SC-1 — Model yetki kontrolü (BR-13) H-1 kapsamındaki "roller ve yetkiler"e dokunuyor; bağımsız incelemede Constraints'e "Sensitive area: H-1" satırı eklendi. Öneri: H-1 kapsamı yalnızca model yetki kontrolüyle sınırlı kalsın (rol/yetki tanımları 0002'de); AC-11 ve AC-9 kasıtlı bozmayla kanıtlansın.
- SC-2 — Harcama göstergesinin detayı yok: tıklanınca ne olur? 0005 görüşmesinde "profil bazlı kırılım" önerilmişti, kararınız yalnızca "bilgi olarak" idi. Öneri: Bu spec'te gösterge tıklanmaz; profil/model kırılımı ileride ayrı bir küçük spec.
- SC-3 — Key uzunluk sınırı (8–512) ve sağlayıcıya özgü önek kontrolü (`sk-`, `sk-ant-`…) belirsiz. Öneri: Önek kontrolü yapılmasın (sağlayıcılar önekleri değiştiriyor), yalnızca 8–512 karakter ve kontrol karakteri yasak; 8 karakterden kısa key zaten maskelemeyi anlamsız kılar.
- SC-4 — Sağlayıcı yanıtında token sayısı yoksa (hata yanıtları, bazı akış modları) maliyet nasıl yazılır? Öneri: Token bilinmiyorsa kayıt "token bilinmiyor" işaretiyle ve 0 maliyetle değil, gönderilen giriş tokenının tahminiyle yazılsın ve "tahmini" olarak işaretlensin; 429 gibi hiç işlenmemiş istekler 0 maliyetle kaydedilsin.
- SC-5 — Key silinirken o sağlayıcıyı kullanan, kuyrukta bekleyen ya da çalışan işler ne olur? Öneri: Çalışan çağrı tamamlanır; sonraki çağrılar AC-10 ile reddedilir; kuyruğun bu durumda duraklaması 0006'da belirlenir. Silme onay metni "kuyruktaki işler bu sağlayıcıyı kullanıyorsa duracak" uyarısını içersin.
- SC-6 — "Yarım kaldı" sonucunda otomatik devam (continuation) ya da daha yüksek limitle tekrar yapılacak mı? Öneri: Bu spec'te hayır — otomatik tekrar H-3 döngü riskidir; yarım sonucu nasıl ele alacağına çağıran özellik (0005/0006) karar verir. 32k/64k ihtiyacı katalog limitiyle karşılanır.
- SC-7 — Yuvarlama kuralı belirsiz ($2,365 → $2,37 mi $2,36 mı?). Öneri: Gösterimde "yarımı yukarı" (MidpointRounding.AwayFromZero), toplama her zaman yuvarlanmamış değerlerle yapılır.
- SC-8 — Data Protection anahtarları kaybolursa kayıtlı key'ler çözülemez (`docs/security.md`). Kullanıcı bunu nasıl görür? Öneri: Çözülemeyen key'in satırı "Key okunamıyor — lütfen yeniden girin" durumunu gösterir ve çağrılar bu nedenle reddedilir; bu durum için ayrı bir kriter eklensin.
- SC-9 — Göstergenin güncellenme süresi (Req 19, AC-21) brief'te ya da görüşmede yok; "10 saniye" önerisi bu spec'te uyduruldu. Öneri: en geç 10 sn (kısa aralıklı yenileme yeterli, ayrı bir anlık bildirim altyapısı gerekmez); daha sıkı bir beklentiniz varsa söyleyin.
- SC-10 — `docs/architecture.md` AiGateway'in sahip olduğu veriler arasında "model seçimleri"ni sayıyor, ama bu spec seçimlerin saklanmasından hiç söz etmiyor. Öneri: Seçimlerin davranışı (hangi rol için hangi model/efor, profil bazlı mı kullanıcı bazlı mı) seçim ekranlarıyla birlikte 0005/0006'da tanımlansın, verisi AiGateway'de tutulsun; bu spec yalnızca katalog ve kullanılabilirlik kararını sağlasın.
- SC-11 — Düzenleniyor durumundan çıkış (Vazgeç) ve kullanıcı kaydetmeden sayfadan ayrılırsa ne olacağı tanımlı değil. Öneri: Giriş alanının yanında "Vazgeç" olsun (Esc de aynı işi yapar); vazgeçince alan temizlenip kapanır, mevcut key değişmez; yazılmış key hiçbir yerde taslak olarak saklanmaz (H-2).

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
