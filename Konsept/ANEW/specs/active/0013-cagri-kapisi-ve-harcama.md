# Spec 0013 — cagri-kapisi-ve-harcama

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0013-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0004-ai-gateway-ve-harcama.md` (bölündü 2026-11-14, onay: tahamucasiroglu 'önerilerin uygun'); Çağrı kapısı, harcama kaydı ve aylık gösterge kısmı (eski Req 9–19)
- Sensitive area: H-3 Maliyet doğuran çağrılar (çağrı kapısı, token limiti, efor, yeniden deneme, zaman aşımı, harcama kaydı)
- Sensitive area: H-1 Kimlik ve yetki (yalnızca model kullanma yetkisi kontrolü, BR-13 — `model.use:<model>`; rol/yetki iskeleti 0011'de)
- Depends on: 0001, 0008, 0002, 0011, 0003, 0004

## Intent
İçerik üreticisi online modelleri kendi parasıyla kullanır; brief "token sınırlarına dikkat et", "boşa para
harcamasın" ve "herkes her modeli kullanamayacak" diyor, üst barda da "bu ay $2 harcadınız" gibi sent dahil bir
aylık harcama istiyor (`ilk_promt.txt`). Bu spec, 0004'ün kataloğu ve key'leri üzerine **tek çağrı kapısını**
kuruyor: yalnızca yetkili ve key'i girilmiş modeller çağrılır, token limiti ve efor katalogdan uygulanır,
yeniden deneme sınırlıdır, sağlayıcıya ulaşan her deneme maliyetiyle kaydedilir ve üst bardaki aylık gösterge bu
kayıtlardan beslenir. Başarı: sonraki spec'ler (0014 Konsept üretimi, 0017 Üretim hattı …) online modeli yalnızca
bu kapıdan çağırır; harcama sent dahil doğru görünür; hiçbir hata çağrının kontrolsüz tekrarına yol açmaz.
Bilinçli olarak yapılmayanlar: harcama limiti, bütçe ya da uyarı (BR-12: yalnızca bilgi), harcama ayrıntı
raporu/kırılımı, model yetkisi verme ekranı, model seçim ekranları.

## Requirements
**Kullanılabilirlik ve yetki (BR-13)**
1. Bir model ancak (a) kullanıcının o modeli kullanma yetkisi (`model.use:<model>`) varsa ve (b) modelin sağlayıcısı
   için kullanıcının kayıtlı ve çözülebilir key'i varsa kullanılabilir. Kullanılamayan bir model için yapılan çağrı
   **sağlayıcıya hiç gitmeden** reddedilir, kayıt ve maliyet oluşmaz ve neden Türkçe bildirilir ("Anthropic için API
   key girilmemiş — Ayarlar" / "Anthropic key'i okunamıyor — Ayarlar'dan yeniden girin" / "Bu modeli kullanma
   yetkiniz yok"). Karar her zaman sunucudadır; arayüzün gizlemesi yeterli sayılmaz.
2. Model yetkileri 0011'in rol/yetki iskeletiyle taşınır. Varsayılan **kullanıcı** rolü katalogdaki tüm modellere
   yetkilidir (`model.use:*` karşılığı). Kısıtlama yeteneği bu spec'te kurulur ve testlenir (rol yapılandırması/seed
   ile); yetki veren ya da kısıtlayan bir ekran v1'de yoktur.
3. 0004'teki Modeller listesi, yetkisi olmayan modeli **"Yetki yok"** etiketiyle gösterir (key durumundan önce gelir).
4. Bir çağrı her zaman çağıran kullanıcının kendi key'iyle yapılır; başka kullanıcının key'i hiçbir yoldan
   kullanılamaz (BR-1).

**Çağrı kuralları (H-3)**
5. Bir metin çağrısının çıktı token üst sınırı, seçilen modelin katalogdaki limitini asla aşmaz. Çağıran özellik sınır
   belirtmez ya da daha yüksek bir sınır isterse istek katalog limitiyle gider. Çıktı bu sınıra takılıp yarım kalırsa
   sonuç **"yarım kaldı"** olarak işaretlenir ve çağıran özelliğe bildirilir; sessizce tam sonuç gibi kabul edilmez.
   Kapı yarım sonuç için otomatik devam (continuation) ya da daha yüksek limitle tekrar çağrısı **yapmaz**; nasıl ele
   alınacağına çağıran özellik karar verir.
6. Seçilen efor seviyesi modelin desteklediklerinden biri değilse çağrı sağlayıcıya gitmeden reddedilir; efor
   belirtilmezse modelin katalogdaki varsayılanı kullanılır.
7. Yeniden deneme yalnızca şu durumlarda yapılır: sağlayıcı **429** ya da **5xx** döndürdü, ya da **bağlantı hiç
   kurulamadı** (istek sağlayıcıya ulaşmadı). En fazla **2 tekrar** (toplam en fazla 3 deneme), her tekrarda bekleme
   artar. Diğer 4xx hatalarında (geçersiz key, yetersiz bakiye, hatalı istek) hiç tekrar yapılmaz (`docs/security.md`).
8. İstek sağlayıcıya gönderildikten sonra yanıt beklenirken modelin katalogdaki **çağrı zaman aşımı** (varsayılan
   10 dk) dolarsa çağrı **tekrar edilmez** (sağlayıcı işlemiş ve ücretlendirmiş olabilir); sonuç **"yanıt alınamadı"**
   olarak kaydedilir ve kullanıcıya Türkçe bildirilir.
9. Hata kullanıcıya Türkçe, sade bir mesajla döner (ör. "Sağlayıcı API key'i reddetti", "Sağlayıcı şu an yoğun,
   tekrar denendi ama olmadı", "Sağlayıcıdan yanıt alınamadı"); sağlayıcının ham yanıtı, key ya da iç ayrıntı
   gösterilmez.
10. Key silindiğinde o sağlayıcıya o anda gönderilmiş çağrı tamamlanır; sonraki çağrılar Req 1 ile reddedilir.
    Key'i olmayan sağlayıcıya gelen sıradaki işin harcama oluşmadan "API key yok — Ayarlar" nedeniyle Hata olması
    0017'nin çalışma anı davranışıdır (bu kapının Req 1 reddine dayanır).

**Harcama kaydı (BR-12)**
11. Sağlayıcıya ulaşan **her deneme** (başarılı, hata, yarım kaldı, yanıt alınamadı ya da yeniden deneme olsun) için
    bir harcama kaydı tutulur: zaman, kullanıcı, sağlayıcı, model, efor, amaç (model rolü: konsept üretimi, denetim,
    konu önerisi, video kodu, görsel, ses), varsa ilgili profil, giriş/çıkış token sayısı (görselde görsel adedi),
    sonuç ve maliyet. Sağlayıcıya hiç gitmeyen (reddedilen ya da bağlantısı kurulamayan) istek kayıt ve maliyet üretmez.
12. Maliyet, çağrı anındaki katalog fiyatıyla ve sentin kesirlerini kaybetmeden USD olarak hesaplanır (metin: giriş
    token × giriş fiyatı + çıkış token × çıkış fiyatı; görsel: görsel adedi × görsel fiyatı). Yanıtında gerçek maliyeti
    bildiren sağlayıcılarda (ör. OpenRouter) **bildirilen maliyet** esas alınır. Katalog fiyatı sonradan değişse de
    eski kayıtlar değişmez.
13. Token sayısı bilinmiyorsa:
    - Sağlayıcı bir hata yanıtı döndürdüyse (429, 5xx, diğer 4xx — istek işlenmemiş sayılır) kayıt **0 maliyetle** yazılır.
    - Başarılı, yarım kaldı ya da yanıt alınamadı sonucunda token sayısı yoksa kayıt, gönderilen giriş tokenının
      tahminiyle hesaplanan maliyetle yazılır ve **"tahmini"** olarak işaretlenir (0 maliyetle yazılmaz).
14. Key silinse de geçmiş harcama kayıtları silinmez.

**Aylık harcama göstergesi**
15. Üst barda, logo ve "ANEW Konsept" adının hemen yanında aylık harcama görünür (tasarım): kullanıcının içinde
    bulunulan takvim ayındaki tüm harcama kayıtlarının toplamı, **tüm profiller dahil**, USD, iki ondalık haneyle ve
    Türkçe ondalık virgülle (ör. "Bu ay: $2,37"). Toplama her zaman yuvarlanmamış değerlerle yapılır; gösterimde
    yarım yukarı yuvarlanır ($2,365 → $2,37).
16. Ay sınırı **Europe/Istanbul** saat dilimine göredir (şimdilik sabit ayar; kayıtlar UTC tutulur). Ayın ilk anında
    gösterge sıfırdan başlar.
17. Gösterge yalnızca bilgi amaçlıdır: hiçbir tutar bir çağrıyı engellemez, uyarı ya da limit yoktur (BR-12).
    Gösterge tıklanmaz; profil/model kırılımı yoktur.
18. Yeni bir harcama kaydı oluştuğunda gösterge sayfa yenilenmeden **en geç 10 saniye** içinde güncellenir (kısa
    aralıklı yenileme yeterlidir; ayrı bir anlık bildirim altyapısı gerekmez).

## User experience
- Flow:
  1. Kullanıcı sonraki spec'lerde bir üretim başlatır → çağrı kapıdan geçer; key ya da yetki eksikse üretim
     sağlayıcıya gitmeden Türkçe nedenle reddedilir ("… için API key girilmemiş — Ayarlar").
  2. Çağrı tamamlandıkça üst bardaki "Bu ay: $0,00" tutarı en geç 10 sn içinde artar.
  3. Ayarlar > Modeller listesinde yetkisi olmayan bir model "Yetki yok" olarak görünür.
- States:
  - Aylık harcama göstergesi: yükleniyor · hiç kayıt yok ("$0,00") · tutar · okunamadı ("—" ve "Harcama alınamadı"
    ipucu). Giriş yapmamış kullanıcıya gösterge hiç görünmez.
  - Modeller listesi (0004'e ek): "Yetki yok" etiketi.
  - Çağrı reddi / hata: çağıran özelliğin ekranında Türkçe neden mesajı (key yok · key okunamıyor · yetki yok ·
    desteklenmeyen efor · sağlayıcı key'i reddetti · sağlayıcı yoğun · yanıt alınamadı).
- Design reference: `source/konseptler sayfası tasarımı.png` — commit `61cc824` sürümü: üst barda "Aylık harcama"
  alanının yeri (logo + program adının sağı). Renk: turuncu/kahve tema değişkenleri (K-7).
- Accessibility: harcama göstergesinin erişilebilir adı "Bu ayki harcama: 2,37 dolar" biçimindedir; gösterge
  tıklanabilir bir kontrol değildir (odak almaz); kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Mimari sınırlar: online sağlayıcıyı çağıran ve çözülmüş key'i gören tek yer AiGateway'dir (Y-7); key'ler Pi'den hiç
  çıkmaz (Y-8). Testlerde gerçek sağlayıcı çağrısı yapılmaz (stub, sabit saat); gerçek deneme yalnızca elle başlatılan
  smoke testle (`docs/testing.md`).
- Model seçimleri (hangi rol için hangi model/efor) **profil başına** saklanır, verisi AiGateway'dedir; seçim
  ekranları ve davranışı 0005 / 0006 / 0017'de tanımlanır. Bu spec yalnızca katalog üzerinden kullanılabilirlik kararını
  ve çağrı kapısını sağlar.
- Para: yalnızca USD, sentin kesirleri korunur (E-3); ekranda 2 hane ve Türkçe ondalık virgül (`$2,37`).
- Hassas alan kuralı gereği H-3 ve H-1 kriterleri (AC-1…AC-13) kasıtlı bozma kanıtıyla doğrulanır.
- Out of scope: katalog, key yönetimi ve Ayarlar sayfası (0004) · harcama limiti, bütçe, uyarı (BR-12 gereği hiç yok) ·
  ayrıntılı harcama raporu/grafiği ve profil/model kırılımı (ileride ayrı spec) · TL gösterimi (ileride) · kullanıcı
  başına saat dilimi ayarı (ileride) · model yetkisi verme/kısıtlama ekranı ve tüm kullanıcıların harcama istatistiği
  (v1'de yok) · online ses girişi (ElevenLabs) ve karakter başı fiyat (0017; "ses" türü 0006) · yarım sonucun nasıl ele alınacağı (çağıran spec'ler) ·
  key'i silinmiş sağlayıcıyı kullanan sıradaki işin Hata olması (0017).
- Split: done — 0013 Çağrı kapısı, harcama kaydı ve aylık gösterge / kardeşler: 0004

## Acceptance criteria
**Kullanılabilirlik ve yetki (H-1, H-3)**
- [ ] AC-1 — Key'i girilmemiş sağlayıcının modeliyle yapılan çağrı "… için API key girilmemiş" ile, key'i çözülemeyen sağlayıcının modeliyle yapılan çağrı "… key'i okunamıyor" ile sağlayıcıya gitmeden reddedilir; harcama kaydı ve maliyet oluşmaz (stub sağlayıcı hiç çağrılmamış olmalı).
- [ ] AC-2 — Varsayılan kullanıcı rolü katalogdaki her modeli çağırabilir; rol yapılandırmasında bir modelin yetkisi kaldırılınca o modelle yapılan çağrı, key kayıtlı olsa bile, sağlayıcıya gitmeden "Bu modeli kullanma yetkiniz yok" ile reddedilir; arayüzü atlayan doğrudan istek de reddedilir; Modeller listesinde model "Yetki yok" görünür. Kasıtlı bozma: yetki kontrolü kaldırılınca test kırmızıya döner.
- [ ] AC-3 — Kullanıcı A'nın çağrısı her zaman A'nın key'iyle yapılır; A, B'nin key'iyle hiçbir yoldan çağrı yaptıramaz (A'nın key'i yoksa çağrı AC-1 ile reddedilir, B'nin key'i kullanılmaz). Kasıtlı bozma: key sahiplik filtresi kaldırılınca test kırmızıya döner.

**Çağrı kuralları (H-3)**
- [ ] AC-4 — Çıktı sınırı istenmeyen ya da katalog limitinden yüksek istenen çağrı sağlayıcıya katalog limitiyle gider; limitin altındaki istek olduğu gibi gider; katalog limiti değiştirilince yeni limit kullanılır; çıktı sınıra takılırsa sonuç "yarım kaldı" olarak işaretlenir, kayıtta bu sonuç görünür ve stub tam 1 kez çağrılır (otomatik devam ya da tekrar yok).
- [ ] AC-5 — Modelin desteklemediği efor seviyesiyle çağrı sağlayıcıya gitmeden reddedilir; efor belirtilmezse katalogdaki varsayılan efor kullanılır.
- [ ] AC-6 — Stub sağlayıcı 429 ya da 5xx döndürdüğünde ya da bağlantı kurulamadığında çağrı en fazla 2 kez tekrarlanır (toplam ≤3 deneme), ardışık beklemeler artar (sabit saatle ölçülür); 2. tekrarda başarılı olursa sonuç başarılıdır; 3 denemede de başarısızsa Türkçe hata döner ve 4. deneme yapılmaz.
- [ ] AC-7 — İstek gönderildikten sonra stub modelin katalogdaki zaman aşımı içinde yanıt vermezse (sabit saatle) çağrı tekrar edilmez (tam 1 deneme), kayıt "yanıt alınamadı" sonucu ve "tahmini" işaretli giriş-token maliyetiyle yazılır, kullanıcıya "Sağlayıcıdan yanıt alınamadı" döner; zaman aşımı katalog değeri değiştirilince yeni değer kullanılır.
- [ ] AC-8 — Stub sağlayıcı 400, 401, 402, 403 ya da 404 döndürdüğünde hiç tekrar yapılmaz (tam 1 deneme) ve kullanıcıya Türkçe mesaj döner (401 → "Sağlayıcı API key'i reddetti"); bu akışlarda üretilen hata yanıtı, teknik loglar ve Günlük satırları 0008'in tarama yöntemiyle taranır ve sağlayıcının ham yanıtı ya da key'in düz hali hiçbirinde yoktur.

**Harcama kaydı (BR-12)**
- [ ] AC-9 — Başarılı bir metin çağrısından sonra bir harcama kaydı oluşur: zaman, sağlayıcı, model, efor, amaç, profil, giriş/çıkış token ve maliyet = giriş token × giriş fiyatı + çıkış token × çıkış fiyatı; sent altı kesirler korunur (ör. 1 234 giriş + 567 çıkış token için beklenen tutar 6 hane hassasiyetle eşleşir); yanıtında maliyet bildiren stub (OpenRouter) için kayıt bildirilen maliyeti taşır.
- [ ] AC-10 — Yeniden denemeli bir çağrıda sağlayıcıya ulaşan her deneme ayrı kayıt olur (ör. 429 + 429 + başarı → 3 kayıt, sonuçlarıyla); bağlantısı kurulamayan deneme kayıt üretmez; görsel çağrısının maliyeti görsel adedi × görsel fiyatıdır.
- [ ] AC-11 — Token sayısı bildirilmeyen yanıtlarda: 429/5xx/diğer 4xx hata yanıtı kaydı 0 maliyetle; token sayısı olmayan başarılı ya da yarım kalmış yanıtın kaydı giriş-token tahminiyle hesaplanan, 0'dan büyük ve "tahmini" işaretli maliyetle yazılır.
- [ ] AC-12 — Katalog fiyatı değiştirildikten sonra eski kayıtların maliyeti değişmez, yeni çağrılar yeni fiyatla kaydedilir.
- [ ] AC-13 — Bir sağlayıcının key'i, o sağlayıcıya gönderilmiş bir çağrı sürerken silinirse o çağrı tamamlanır ve kaydı yazılır; silmeden sonraki çağrı AC-1 ile reddedilir; o sağlayıcıya ait geçmiş harcama kayıtları yerinde kalır ve aylık toplama girmeye devam eder.

**Aylık harcama göstergesi**
- [ ] AC-14 — Üst bar, girişli kullanıcının bu takvim ayındaki tüm profillerine ait kayıtların yuvarlanmamış toplamını iki haneyle, yarım yukarı yuvarlayarak gösterir (ör. 1,234567 + 1,131 → "$2,37"; toplam 2,365 → "$2,37"); önceki aya ve başka kullanıcılara ait kayıtlar toplama girmez; hiç kayıt yoksa "$0,00" görünür.
- [ ] AC-15 — Ay sınırı Europe/Istanbul'a göredir: ayın son günü 23:59:59 (İstanbul) kaydı o aya, ertesi ayın ilk anındaki (İstanbul 00:00:00, UTC 21:00:00) kayıt yeni aya sayılır (sabit saatle test edilir).
- [ ] AC-16 — Yeni bir kayıt oluştuktan sonra gösterge sayfa yenilenmeden en geç 10 sn içinde yeni toplamı gösterir; gösterge verisi alınamazsa "—" ve "Harcama alınamadı" ipucu görünür, sayfanın geri kalanı çalışmaya devam eder.
- [ ] AC-17 — Toplam ne kadar yüksek olursa olsun hiçbir çağrı harcama yüzünden engellenmez ve uyarı gösterilmez (BR-12).
- [ ] AC-18 — Gösterge veri gelene kadar yükleniyor durumu gösterir; giriş yapmamış kullanıcıya gösterge hiç görünmez; göstergenin erişilebilir adı "Bu ayki harcama: 2,37 dolar" biçimindedir; gösterge tıklanınca hiçbir şey açılmaz ve odak almaz; renkler yalnızca tema değişkenlerinden gelir (K-7).

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
