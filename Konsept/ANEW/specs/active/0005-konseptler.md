# Spec 0005 — konseptler

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0005-plan.md`
- Self-critique: pending (spec onayıyla birlikte; aşağıdaki Open questions)

## Intent
İçerik üreticisi, üst bardan seçtiği kanal profiline uygun video konseptlerini Konseptler sayfasında
online modellerle üretmek istiyor: ya modelin önerdiği kısa konu önerilerinden seçerek (konudan üret) ya da
kendi yazdığı bir fikirden doğrudan (fikirden üret). Üretilen konseptleri okuyup elle ya da talimatla
düzeltiyor, beğenmediklerini Silinenler'e atıyor, beğendiklerini video üretimi için onaylıyor. Başarı:
konseptler gerçek bilgiye dayanıyor (uydurma yok), aynı profilde aynı konu iki kez önerilmiyor, her online
çağrının maliyeti kaydediliyor ve hiçbir otomatik döngü kullanıcının bütçesini kontrolsüz harcamıyor (H-3).
Bilinçli olarak yapılmayanlar: Konseptten Video sayfası ve üretim sırası (0006), üretilen videolar ve videoların
Silinenler tarafı (0007), sağlayıcı/model kataloğu ve API key yönetimi (0004), profil yönetimi (0003).

## Requirements
Kaynak: `ilk_promt.txt` (Konseptler bölümü), `docs/domain.md` (BR-1…BR-8, BR-12, BR-13, BR-15, BR-16),
`Promtlar/0005`–`0006` (terimler, BR-3, BR-16), `0009` (yeniden deneme), `0011` (LLM çıktısı güvenilmeyen veri);
çağrı kuralları spec 0004'ten devralınır. Kararı verilmemiş noktalar Open questions'ta öneriyle bekliyor (S-n / G-n).

**A. Üst şerit: model bilgisi, ince ayar, kelime denetimi**
1. Sayfanın üst barın hemen altındaki şeridinde solda **model bilgisi** görünür: üç model rolü (konsept üretimi,
   anlamca tekrar denetimi, konu önerisi) için seçili model ve performans/efor düzeyi kısa metinle
   gösterilir (ör. "Konsept: Claude Sonnet 5.5 High").
2. Model bilgisine tıklanınca sayfanın üstünde ayrı bir **modal** açılır (sayfa içine açılır alan değil). Modalda
   her rol için model ve (model destekliyorsa) performans/efor düzeyi seçilir. Modal kapat düğmesiyle ya da modal
   dışına tıklanarak kapanır ve seçimler kaydedilir; şerit yeni seçimleri gösterir.
3. Modalda yalnızca kullanıcının kullanabileceği modeller listelenir: geçerli API key'i olan sağlayıcıların ve
   yetkisi olan modellerin modelleri (BR-13; katalog ve key 0004'ten gelir).
4. Şeridin ortasında (ekranın sağ yarısının başında) **ince ayar bilgisi** görünür: seçili ton, izleyici ve anlatım
   biçimi. Tıklanınca bir seçim alanı açılır:
   - Ton: gizemli, eğitici, dramatik, sakin, belgesel, esprili, ilham verici, sorgulayıcı.
   - İzleyici: konuya yeni başlayan, meraklı, aile ve çocuklara uygun, konuyu bilen.
   - Anlatım biçimi: tek bir hikâye, sıralı liste, efsane mi gerçek mi, ya olsaydı (liste kapsamı → G-12).
   Seçimler sonraki konu önerisi ve konsept üretimlerine uygulanır (kalıcılık ve tek/çoklu seçim → S-6).
5. Şeridin sağında **kelime denetimi** onay kutusu vardır. Açıkken her üretilen konseptte sık tekrar eden kelime
   denetlenir; eşik aşılırsa konsept otomatik olarak **en fazla 1 kez** yeniden üretilir; yine aşılırsa konsept
   bir "kelime tekrarı" uyarısıyla Üretilenler'e düşer (BR-15; eşik → S-2).

**B. Konudan üret (sol üst alan)**
6. Kullanıcı 1–100 arası bir adet girip "Konu üret" der; konu önerisi modeli o kadar **konu önerisi** üretir. Her
   öneri 1 cümlelik başlık + en fazla 3 cümlelik kısa özettir; sahne detayı üretilmez (BR-3).
7. Öneriler seçili profilin kanal içeriğine, video diline ve ince ayara uygun olur. Aynı profilde daha önce
   üretilmiş, videosu yapılmış, silinmiş (Silinenler) ya da önerilip seçilmemiş başlıklar tekrar önerilmez;
   Silinenler negatif sinyal olarak kullanılır (BR-4, BR-8). Model aynı başlığı yine döndürürse o öneri
   listelenmez. Öneri talimatı az token harcayacak biçimde kısa tutulur: elenen konular ve daha önce seçilip
   üretilen konular ("şu konular elendi, şu konular seçildi") modele bir odak bilgisi olarak özetle verilir
   (ilk_promt.txt; geçmiş büyüdükçe nasıl kısaltılacağı → G-17).
8. Öneriler seçilebilir bir liste olarak gösterilir. Kullanıcı bir ya da birden çok öneri seçip "Seçtiklerini üret"
   der; yalnızca seçilenler için tam konsept üretilir ve Üretilenler'e düşer. Seçilmeyenler "önerilip seçilmemiş"
   olarak hafızaya alınır (zamanlama → G-1).

**C. Fikirden üret (sağ üst alan)**
9. Kullanıcı serbest metinle bir fikir yazar (ör. "Osmanlı'nın kuruluşu, ilk 3 padişah, savaş zaferleri, ilham
   veren anlatım"), 1–100 arası konsept sayısı girer ve "Üret" der. Konseptler öneri adımı olmadan doğrudan
   Üretilenler'e düşer (BR-5).

**D. Konsept üretimi (her iki yol için ortak)**
10. Konsept; başlık ve sıralı sahnelerden oluşur. Her sahnede sahne başlığı, ekranda gösterilecek şeyin tarifi,
    seslendirme metni ve süre bulunur. Konsept profilin video dilinde ve profilin hedef süresine uygun üretilir.
11. Konsept içeriği gerçek bilgiye dayanır; üretim talimatı doğruluk ister ve uydurmayı yasaklar (domain değişmezi;
    nasıl kanıtlanacağı ve kaynak gösterimi → S-3).
12. Her yeni konsept "Üretildi" durumuyla seçili profile bağlı olarak kaydedilir (BR-2, BR-16).
13. Bu sayfanın yaptığı her online model çağrısı (öneri, konsept, kelime denetimi kaynaklı yeniden üretim, anlamca
    tekrar denetimi — S-4 kabul edilirse —, düzenleme) harcama kaydına kendi model rolüyle ve seçili profille
    girer (BR-12). Kaydın kendisi, maliyet hesabı ve üst bardaki aylık harcama göstergesi 0004'ün davranışıdır;
    bu spec yalnızca hiçbir çağrısının kayıt dışı kalmamasını sağlar.
14. Toplu üretimde kullanıcı ilerlemeyi görür (ör. "12/100") ve hata alan konseptler ayrıca bildirilir. Başarısız
    çağrılarda yeniden deneme yalnızca 0004'ün çağrı kuralıyla yapılır (429/5xx, en fazla 2 tekrar); bu sayfa kendi
    ek yeniden denemesini eklemez (durdurma ve kısmi hata → G-3).
14a. Model çıktısı token sınırına takılıp yarım kalırsa (0004 Req 11 "yarım kaldı") ya da konsept/öneri biçimine
    uymazsa sonuç konsept ya da öneri olarak kaydedilmez, hata olarak bildirilir ve otomatik olarak yeniden
    üretilmez (H-3; uzun videolarda token sınırının yetmemesi ilk_promt.txt'te bilinen risk → G-18).

**E. Üretilenler (sol alt alan)**
15. Üretilenler, seçili profilin silinmemiş konseptlerini başlık ve durumla listeler. Profil değişince liste o
    profilin konseptlerine geçer (BR-2). Hangi durumların görüneceği → S-5.
16. Bir konsepte tıklanınca sağ alttaki düzenleme alanında konseptin tüm sahneleri (başlık, ekran tarifi,
    seslendirme metni, süre) görünür. Konsept metni düz metin gösterilir (LLM çıktısı güvenilmeyen veridir).
17. Her satırın sağında kalem (düzenle) ve kırmızı çöp kutusu (sil) ikonları vardır.
18. Kullanıcı bir konsepti video üretimi için **onaylar**; konsept "Onaylandı" durumuna geçer ve Konseptten Video
    sayfasındaki "Onaylanan konsept listesi"ne düşer (BR-16). Onay kontrolünün biçimi ve geri alınabilirliği → S-1.

**F. Düzenleme döngüsü (sağ alt alan + revizyon modalı)**
19. Kalem ikonuyla düzenleme alanı düzenlenebilir hale gelir. Kullanıcı iki yoldan düzenler:
    (a) sahne metinlerini doğrudan değiştirir; (b) alttaki talimat alanına ne düzeltilmesini istediğini yazıp
    "Yeniden üret" der — bu, konsept üretim modeliyle online yeniden üretimdir (iki yolun birlikteliği → G-2).
20. Talimatla yeniden üretimin sonucu bir **konsept revizyonu** olarak sayfanın üstünde ayrı bir modalda gösterilir.
    Modalda üç eylem vardır: **Onayla** (revizyon konseptin yerini alır), **Sil** (revizyon atılır, konsept aynen
    kalır), **Tekrar düzenle** (yeni talimatla yeni revizyon üretilir ve aynı modalda gösterilir). Döngünün sınırı
    yoktur; her tur kullanıcının açık eylemiyle başlar (BR-6).

**G. Silme ve Silinenler (konsept tarafı)**
21. Çöp kutusu ile silinen konsept Üretilenler'den kalkar ve Silinenler sayfasına gider; süresiz saklanır (BR-7).
22. Silinenler sayfası seçili profilin silinmiş konseptlerini listeler. Buradan silinen konsept kalıcı olarak gider
    ve geri alınamaz (BR-7). Silinenler'deki konseptler konu önerisinde negatif sinyal olmaya devam eder (BR-8);
    kalıcı silinen konseptin negatif sinyal olup olmayacağı → G-9.

**H. Önerileri sıfırla (Ayarlar)**
23. Ayarlar'da "Önerileri sıfırla" eylemi vardır; ne silineceğini anlatan bir onay sonrası **yalnızca önerilip
    seçilmemiş** başlıkların hafızasını siler. Üretilmiş ve silinmiş konseptler tekrar önerilmemeye devam eder
    (BR-4; kapsam → G-10).

**I. Yetki ve sahiplik**
24. Kullanıcı yalnızca kendi profillerinin konseptlerini, önerilerini ve Silinenler'ini görür ve değiştirebilir
    (BR-1). Yetkisi olmayan model hiçbir yoldan kullanılamaz (BR-13).

## User experience
- Flow (konudan): Kullanıcı üst bardan profil seçer → Konseptler sayfası açılır → gerekirse model bilgisi modalından
  modelleri, ince ayardan ton/izleyici/anlatım biçimini seçer, kelime denetimini açar → sol üstte adet (ör. 20)
  girip "Konu üret" der → başlık + kısa özet listesi gelir → beğendiklerini işaretleyip "Seçtiklerini üret" der →
  ilerleme görünür → konseptler sol alttaki Üretilenler'e düşer.
- Flow (fikirden): Sağ üstte fikir metnini yazar, adet girer, "Üret" der → ilerleme → konseptler Üretilenler'e düşer.
- Flow (düzenleme): Üretilenler'den konsepte tıklar → sağ altta sahneler görünür → kalem → metni doğrudan düzeltir
  ve kaydeder ya da talimat yazıp "Yeniden üret" der → revizyon modalı açılır → Onayla / Sil / Tekrar düzenle.
- Flow (karar): Beğendiği konsepti onaylar ("Onaylandı") ya da çöp kutusuyla Silinenler'e atar; Silinenler
  sayfasından kalıcı olarak silebilir. Ayarlar'dan önerileri sıfırlayabilir.
- States:
  - Sayfa: profil yok ("Önce bir profil oluştur" + artı'ya yönlendirme) · gerekli rol için model seçilmemiş ya da
    geçerli API key yok (üret düğmeleri pasif, Ayarlar'a yönlendiren açıklama) · hazır.
  - Model bilgisi modalı: açık · seçilebilir model yok (açıklama + Ayarlar yönlendirmesi) · kaydedildi.
  - İnce ayar: kapalı (özet metin) · açık (seçim alanı).
  - Konudan üret: boş (henüz öneri yok) · geçersiz adet (hata mesajı) · üretiliyor (ilerleme, düğmeler pasif) ·
    öneriler listelendi · hiç seçim yokken "Seçtiklerini üret" pasif · sağlayıcı hatası (sade Türkçe mesaj) ·
    yeni öneri çıkmadı (tüm adaylar BR-4 ile elendi; açıklama).
  - Fikirden üret: boş metin (hata) · geçersiz adet (hata) · üretiliyor (ilerleme) · tamamlandı · kısmi hata
    (n konsept üretilemedi; yarım kalan ya da biçimi bozuk çıktılar dahil) · sağlayıcı hatası (hiç konsept
    üretilemedi).
  - Üretilenler: boş ("Henüz konsept yok") · liste · seçili konsept · kelime tekrarı uyarılı konsept · durum
    rozetleri (Üretildi, Onaylandı, …).
  - Düzenleme alanı: konsept seçilmedi (yönlendirme metni) · görüntüleme · düzenleme · kaydediliyor · yeniden
    üretiliyor (talimat alanı ve düğmeler pasif) · boş talimat (hata) · sağlayıcı hatası.
  - Revizyon modalı: revizyon gösteriliyor · tekrar düzenle (talimat alanı) · yeniden üretiliyor · hata.
  - Silinenler (konsept): boş · liste · kalıcı silme onayı · silindi.
  - Önerileri sıfırla: onay diyaloğu · tamamlandı.
  - Yetki yok: başka kullanıcının konseptine erişim "bulunamadı" ile sonuçlanır; yetkisiz model seçilemez.
- Design reference: `source/konseptler sayfası tasarımı.png` — commit `61cc824` sürümü. Yerleşim: üst barın altında
  şerit (solda "modeller bilgisi", ortada "ince ayar bilgisi", sağda "kelime denetimi seçimi"); altında dört alan:
  sol üst "konu üreterek", sağ üst "fikirden üreterek", sol alt "üretilenler", sağ alt "üretilenleri düzenleme
  alanı". Görseldeki üst bar "profil" bağlantısı kaldırıldı (0001/ilk_promt). Modallar, ince ayar seçim alanı,
  öneri listesi ve revizyon modalı için görsel yok — yukarıdaki akış tasarımdır. Renk: turuncu/kahve, açık/koyu tema
  değişkenleri (K-7).
- Accessibility: tüm kontroller (adet alanları, onay kutuları, ikon düğmeleri, modal eylemleri) klavyeyle erişilebilir;
  modallar açılınca odak modalın içine gider, kapanınca açan kontrole döner; kalem/çöp/onay ikon düğmelerinin
  erişilebilir adı var ("Düzenle: <başlık>"); ilerleme ve hata mesajları ekran okuyucuya duyurulur; WCAG AA kontrast
  her iki temada.

## Constraints & out of scope
- Sensitive area: H-3 (Maliyet doğuran çağrılar) — toplu öneri/konsept üretimi (1–100), BR-15 otomatik yeniden üretimi,
  düzenleme döngüsü, maliyet kaydı. H-3 kriterleri (AC-16, AC-18, AC-19, AC-20, AC-28) kasıtlı bozma ile kanıtlanır
  (`docs/testing.md`). H-1 (model yetkisi BR-13, veri sahipliği BR-1) bu spec'te yalnızca tüketilir; zorlama 0002/0004'te
  (bkz. G-21).
- Online çağrılar yalnızca AiGateway üzerinden yapılır (Y-7); bu spec sağlayıcı ekleme, key girme, model kataloğu,
  yeniden deneme kuralı, token sınırının uygulanması, harcama kaydının biçimi ve aylık gösterge getirmez (0004'e
  bağımlı; bu spec bunları yeniden tanımlamaz, yalnızca kendi çağrılarının bunlara uyduğunu kanıtlar). Profil seçimi ve profil verisi 0003'ten gelir. Kimlik 0002'den gelir.
- Token limitleri model kataloğundan okunur; kodda sabit değer olmaz (`docs/conventions.md`).
- Testlerde gerçek LLM çağrısı yapılmaz (stub; `docs/testing.md`).
- Out of scope: Konseptten Video sayfası, üretim sırası, altyazı/ilerleme çubuğu seçenekleri (0006) · videolar ve
  videoların Silinenler tarafı (0007) · Günlük şeridindeki olay akışı (0008; bu spec yalnızca olayları üretir mi →
  plan) · Ayarlar sayfasının temeli (0004) · yayınlama · konsept metninden sonra yapılacak video sürümleri.
- Bağımlılık sırası: 0002 → 0003 → 0004 → 0005.
- Split: proposed — 0005a "Model bilgisi ve ince ayar" (Req 1–5: şerit, model modalı, ince ayar, kelime denetimi
  kutusu; AC-1…5) → 0005b "Konsept üretimi ve onay" (Req 6–18, 24: konudan/fikirden üret, ortak kurallar, Üretilenler,
  onay; AC-6…24, 34, 35) → 0005c "Düzenleme döngüsü" (Req 19–20; AC-25…29) → 0005d "Silinenler (konsept) ve
  önerileri sıfırla" (Req 21–23; AC-30…33). Bu spec birden çok kullanıcı yeteneği ve 35 kriter içeriyor; 0005b
  tek başına hâlâ ~21 kriter (H-3 çekirdeği). Karar → S-7.

## Acceptance criteria
**A. Şerit, model ve ince ayar**
- [ ] AC-1 — Sayfa açıldığında şerit, üç rolün (konsept üretimi, anlamca tekrar denetimi, konu önerisi) seçili modelini ve (model destekliyorsa) efor düzeyini, ince ayar özetini ve kelime denetimi kutusunu tasarımdaki yerleşimle gösterir; altında dört alan (konu üreterek, fikirden üreterek, üretilenler, düzenleme alanı) yer alır.
- [ ] AC-2 — Model bilgisine tıklamak sayfanın üstünde bir modal açar; bir rol için model değiştirilip modal kapat düğmesiyle ya da dışına tıklanarak kapatılınca seçim kaydedilir, şerit yeni modeli gösterir ve sayfa yenilendiğinde seçim korunur.
- [ ] AC-3 — Modalda yalnızca geçerli API key'i olan sağlayıcıların ve kullanıcının yetkili olduğu modeller listelenir; yetkisiz bir modeli seçmeye yönelik istek reddedilir (BR-13). Seçilebilir model yoksa modal açıklama ve Ayarlar yönlendirmesi gösterir.
- [ ] AC-4 — İnce ayar alanı tonda 8, izleyicide 4, anlatım biçiminde (onaylanan liste kadar) seçenek sunar; yapılan seçim şeritte görünür, sayfa yenilendiğinde korunur ve sonraki üretim isteğinin talimatına girer (stub sağlayıcıya giden istekte görülür).
- [ ] AC-5 — Gerekli rol için model seçilmemişse ya da geçerli API key yoksa ilgili üret düğmeleri pasiftir ve Ayarlar'a yönlendiren açıklama görünür; seçili profil yoksa sayfa "Önce bir profil oluştur" durumunu gösterir.

**B. Konudan üret**
- [ ] AC-6 — Adet 1 ve 100 kabul edilir; 0, 101, boş, ondalık ve harf içeren değerler reddedilir ve alanın yanında hata mesajı görünür; reddedilen istekte hiçbir online çağrı yapılmaz.
- [ ] AC-7 — N adetli "Konu üret" en fazla N öneri listeler; her öneride 1 cümlelik başlık ve en fazla 3 cümlelik özet vardır, sahne detayı yoktur; 3 cümleden uzun özet ya da sahne içeren öneri listelenmez (BR-3; kesme mi eleme mi → G-19).
- [ ] AC-8 — Aynı profilde üretilmiş, videosu yapılmış, Silinenler'de bulunan ya da önerilip seçilmemiş bir başlık yeni önerilerde yer almaz; bu başlıklar öneri talimatında elenecek konular olarak gider; stub bu başlıklardan birini birebir döndürse bile listelenmez; başka profildeki başlıklar engel olmaz (BR-4, BR-8, BR-2). Daha önce seçilip üretilen başlıklar talimatta "seçilen konular" odak bilgisi olarak yer alır (stub'a giden istekte görülür).
- [ ] AC-9 — Hiç öneri seçili değilken "Seçtiklerini üret" pasiftir; k öneri seçilip üretilince yalnızca k konsept üretilir, Üretilenler'e "Üretildi" durumuyla düşer ve seçilmeyen öneriler önerilip seçilmemiş olarak kaydedilir.
- [ ] AC-10 — Henüz öneri yokken alan boş durum metnini; üretim sırasında ilerleme göstergesini ve pasif düğmeleri; tüm adaylar elendiğinde "yeni öneri bulunamadı" açıklamasını; sağlayıcı hatasında sade Türkçe mesajı (stack trace ya da key olmadan) gösterir.

**C. Fikirden üret**
- [ ] AC-11 — Fikir metni boşken ya da yalnızca boşluk içerirken "Üret" reddedilir ve hata mesajı görünür; adet AC-6'daki sınırlarla doğrulanır.
- [ ] AC-12 — Geçerli fikir ve N adetle "Üret" öneri adımı olmadan N konsept üretir; konseptler doğrudan Üretilenler'e "Üretildi" durumuyla, seçili profile bağlı olarak düşer (BR-5).
- [ ] AC-13 — Fikirden üretim sırasında ilerleme ("k/N") görünür; bazı konseptler sağlayıcı hatasıyla üretilemezse başarılılar kaydedilir ve "n konsept üretilemedi" bildirimi görünür; hiçbiri üretilemezse sade Türkçe hata mesajı (stack trace ya da key olmadan) görünür ve Üretilenler değişmez.

**D. Ortak üretim kuralları (H-3)**
- [ ] AC-14 — Üretilen her konsept başlık ve en az bir sahne içerir; her sahnede başlık, ekran tarifi, seslendirme metni ve süre bulunur; eksik alanlı model çıktısı konsept olarak kaydedilmez ve hata olarak bildirilir.
- [ ] AC-15 — Konsept üretim talimatı profilin kanal içeriğini, video dilini, hedef süresini, ince ayarı ve doğruluk/uydurma yasağı talimatını içerir (stub'a giden istekte doğrulanır). Talimata dış içerik (araştırma sonucu, S-3) ya da kullanıcı metni (fikir, düzenleme talimatı) girdiğinde talimat olarak değil "veri" olarak işaretlenmiş bölümde yer alır (`docs/security.md`).
- [ ] AC-16 — Kelime denetimi açıkken stub ilk çıktıda eşiği aşan konsept döndürürse tam 1 yeniden üretim yapılır; ikinci çıktı da aşarsa konsept "kelime tekrarı" uyarısıyla kaydedilir ve üçüncü bir çağrı yapılmaz (BR-15). Kasıtlı bozma: sınır kaldırılınca test kırmızıya döner.
- [ ] AC-17 — Kelime denetimi kapalıyken eşiği aşan konsept yeniden üretilmez ve uyarısız kaydedilir; eşiğin altındaki konsept, kutu açık olsa da yeniden üretilmez.
- [ ] AC-18 — Öneri, konsept, BR-15 yeniden üretimi, anlamca tekrar denetimi (S-4 kabul edilirse) ve düzenleme çağrılarının her biri, sağlayıcıya ulaştığında, doğru model rolü ve seçili profille bir harcama kaydı üretir; stub'a giden çağrı sayısı ile oluşan kayıt sayısı eşittir (BR-12; kayıt biçimi ve aylık gösterge 0004 AC-16…AC-21). Kasıtlı bozma: bir çağrı türünün kaydı atlanınca test kırmızıya döner.
- [ ] AC-19 — N adetli bir konsept üretim isteğinde (sürekli 5xx ve sürekli eşik aşımı dönen stub ile) konsept üretim çağrısı sayısı N × 2 (ilk + BR-15) × 3 (ilk deneme + 0004'ün en fazla 2 tekrarı) üst sınırını aşmaz; bu sayfa 0004'ün yeniden denemesine kendi tekrarını eklemez (4xx'te konsept başına tam 1 çağrı). Kasıtlı bozma: sayfa düzeyinde ek bir tekrar eklenince test kırmızıya döner.
- [ ] AC-20 — Stub "yarım kaldı" (token sınırına takılmış) ya da biçimi bozuk (ayrıştırılamayan) bir çıktı döndürünce sonuç konsept ya da öneri olarak kaydedilmez, kullanıcıya "üretilemedi" olarak bildirilir ve aynı konsept için otomatik yeni çağrı yapılmaz (H-3, Req 14a). Çağrıda istenen çıktı sınırı seçili konsept modelinin katalog limitidir (kodda sabit değer yok; sınırın uygulanması 0004 AC-12).

**E. Üretilenler ve onay**
- [ ] AC-21 — Üretilenler yalnızca seçili profilin silinmemiş konseptlerini başlık ve durum rozetiyle listeler; profil değiştirilince liste yeni profile göre yenilenir; konsept yokken "Henüz konsept yok" boş durumu görünür.
- [ ] AC-22 — Bir konsepte tıklanınca düzenleme alanında tüm sahneleri (başlık, ekran tarifi, seslendirme metni, süre) düz metin olarak görünür; model çıktısındaki HTML/script etiketleri çalışmaz, metin olarak gösterilir. Konsept seçilmemişken alan yönlendirme metni gösterir.
- [ ] AC-23 — Kelime tekrarı uyarısıyla kaydedilmiş konsept listede ve detayda uyarı işaretiyle görünür.
- [ ] AC-24 — "Üretildi" durumundaki bir konsept onaylanınca "Onaylandı" olur, rozeti değişir ve Konseptten Video'nun okuyacağı onaylı konseptler arasında yer alır; onaylanmamış ("Üretildi") ve silinmiş konseptler bu listede yer almaz (BR-16). Onay kontrolünün biçimi S-1 kararına göre.

**F. Düzenleme döngüsü**
- [ ] AC-25 — Kalem ikonu düzenleme alanını düzenlenebilir yapar; sahne metni elle değiştirilip kaydedilince kaydetme sırasında "kaydediliyor" durumu görünür, sonra konsept güncellenir, yenilemede korunur ve hiçbir online çağrı yapılmaz (G-2 kararına göre). Kaydetme başarısız olursa sade hata mesajı görünür ve düzenlenen metin kaybolmaz; geçersiz elle düzenleme G-20 kararına göre reddedilir.
- [ ] AC-26 — Talimat alanı boşken "Yeniden üret" reddedilir; dolu talimatla yeniden üretim, mevcut konsepti ve talimatı modele gönderir ve sonucu sayfanın üstündeki revizyon modalında gösterir; bu sırada talimat alanı ve düğmeler pasiftir.
- [ ] AC-27 — Revizyon modalında "Onayla" revizyonu konseptin yerine koyar (Üretilenler ve detay yeni içeriği gösterir, durum değişmez — Onaylandı konsept için S-5 kararına göre); "Sil" revizyonu atar ve konsept birebir aynı kalır (BR-6).
- [ ] AC-28 — "Tekrar düzenle" yeni talimatla yeni bir revizyon üretip aynı modalda gösterir; art arda en az 3 tur çalışır (döngü sınırı yok) ve her tur yalnızca kullanıcının açık eylemiyle başlar — kullanıcı eylemi olmadan hiçbir yeniden üretim çağrısı yapılmaz (H-3). Kasıtlı bozma ile kanıtlanır.
- [ ] AC-29 — Yeniden üretimde sağlayıcı hatası olursa modal ya da düzenleme alanı sade bir hata mesajı gösterir ve konsept değişmeden kalır.

**G. Silme ve Silinenler**
- [ ] AC-30 — Çöp kutusuyla silinen konsept Üretilenler'den kalkar, Silinenler sayfasında (seçili profil için) görünür ve yeniden yüklemeden sonra da orada durur (BR-7).
- [ ] AC-31 — Silinenler'den kalıcı silme bir onay diyaloğu ister; onaylanınca konsept tamamen gider, Silinenler'de ve Üretilenler'de görünmez; vazgeçilirse hiçbir şey değişmez. Silinenler boşken boş durum metni görünür.
- [ ] AC-32 — Silinenler'deki konseptin başlığı sonraki konu önerisi talimatına negatif sinyal olarak girer ve yeni önerilerde yer almaz (BR-8).

**H. Önerileri sıfırla**
- [ ] AC-33 — Ayarlar'daki "Önerileri sıfırla" ne silineceğini anlatan bir onay diyaloğu açar; onaylanınca seçili profilin önerilip seçilmemiş başlıkları tekrar önerilebilir olur, üretilmiş ve Silinenler'deki konseptlerin başlıkları önerilmemeye devam eder (BR-4); vazgeçilirse hiçbir şey değişmez.

**I. Yetki ve sahiplik**
- [ ] AC-34 — Kullanıcı A, kullanıcı B'nin konseptini, önerisini ya da Silinenler kaydını görüntüleme, düzenleme, onaylama, silme ya da kalıcı silme girişiminde "bulunamadı" alır ve B'nin verisi değişmez (BR-1). Girişsiz istekler reddedilir.
- [ ] AC-35 — Tüm tarayıcı istekleri Web controller'ı üzerinden `$.ajax` ile gider (K-6); modallar ve ikon düğmeleri klavyeyle kullanılabilir, modal açılınca odak modala geçer, kapanınca açan kontrole döner; ikon düğmelerinin erişilebilir adı vardır.

## Open questions (awaiting human)
Bu bölüm spec onaylanınca kaldırılır; kararlar yukarıya işlenir.

**Clarify soruları (etki sırasıyla)**
- **S-1 — Onay kontrolü (BR-16):** Konsept "Onaylandı"ya hangi kontrolle geçsin, geri alınabilsin mi?
  Öneri: Üretilenler satırında kalem ve çöpün yanında bir **onay (✓) ikon düğmesi** + çoklu seçim kutularıyla
  "Seçilenleri onayla"; onaylı konseptte aynı düğme "Onayı geri al" olur ve konsept "Sırada"ya geçene kadar
  "Üretildi"ye dönebilir. Gerekçe: tasarımda onay yeri yok; satırdaki ikon kalem/çöp düzeniyle tutarlı, toplu seçim
  100'lük üretimlerde gerekli; geri alma yanlış tıklamayı ucuzca düzeltir.
- **S-2 — Kelime denetimi eşiği (BR-15):** "Sık tekrar" nasıl ölçülsün?
  Öneri: model çağrısı olmadan, yerel sayımla: seslendirme metinlerinde durak kelimeler (ve, bir, bu…; profil diline
  göre) hariç, aynı kelimenin (küçük harfe çevrilmiş, Türkçe I/İ kuralına uygun) **en az 5 kez geçmesi ve toplam
  kelimelerin %2'sini aşması**; eşik değerleri Ayarlar'dan değil yapılandırmadan gelir. Gerekçe: ücretsiz ve
  deterministik (test edilebilir); iki koşul kısa metinde de uzun metinde de yanlış alarmı azaltır.
- **S-3 — Gerçek veriye dayanma:** Uydurma yasağı yalnızca talimatla mı sağlansın, kaynak da gösterilsin mi?
  Öneri: seçili modelin sağlayıcısı web arama aracını destekliyorsa konsept üretiminde açılsın, konsept kaynak
  listesi (başlık + bağlantı) içersin ve düzenleme alanında gösterilsin; araç yoksa yalnızca doğruluk talimatı +
  "emin olunmayan bilgiyi konsepte koyma" kuralı. Gerekçe: brief "gerçekten konuyu araştıracak" diyor; kaynak
  listesi kullanıcının hukuki/doğruluk kontrolünü kolaylaştırır. Bedeli: arama ek maliyet (H-3), maliyet kaydına girer.
- **S-4 — "Anlamca tekrar denetimi" modelinin işi:** Brief bu rolü model bilgisinde sayıyor ama ne yaptığını tarif
  etmiyor (domain: kelime denetiminden farklı).
  Brief'teki tek ipucu: "Anlam yakalamayı işte multilanguage mini LLM'le yaptık" (küçük, çok dilli bir model).
  Öneri: BR-4'ün anlam düzeyindeki uygulaması — yeni konu önerileri ve fikirden üretilen başlıklar, profilin geçmiş
  başlıklarıyla küçük ve ucuz bir modelle anlamca karşılaştırılır; aynı konunun farklı ifadesi elenir. Çağrı sunucuda
  (AiGateway) yapılır, yerel model şimdilik yok. Gerekçe: "Vikinglerin Ayasofya seferi" ile "Vikingler
  Konstantinopolis'te" gibi farklı yazılmış aynı konuyu yalnızca anlam karşılaştırması yakalar; ucuz model maliyeti düşük tutar.
- **S-5 — Hangi durumdaki konsept Üretilenler'de görünür ve düzenlenir/silinir?**
  Öneri: Üretilenler silinmemiş tüm konseptleri durum rozetiyle gösterir (varsayılan filtre: Üretildi + Onaylandı);
  düzenleme ve silme yalnızca **Üretildi** ve **Onaylandı** durumunda; Sırada/Üretiliyor konsept kilitli (önce
  sıradan çıkarılmalı, 0006); Video oldu konseptin metin değişikliği 0007'deki yeni sürüm akışına aittir.
  Onaylı konsept düzenlenip revizyonu onaylanırsa "Onaylandı" kalır. Gerekçe: üretim sırasındaki konseptin metni
  değişirse BR-9 gereği her şey baştan üretilir; kilit boşa harcamayı önler.
- **S-6 — Ayarların kalıcılığı ve seçim tipi:**
  Öneri: model seçimleri **kullanıcı düzeyinde** (key kullanıcıya ait), ince ayar ve kelime denetimi **profil
  düzeyinde** (ton kanala özgü) saklanır. İnce ayarın her grubunda **tek seçim** ve "Belirtilmedi" varsayılanı
  (model profil içeriğine göre karar verir); ince ayar hem konu önerisine hem konsept üretimine uygulanır.
  Gerekçe: tarih kanalı "belgesel", eğlence kanalı "esprili" ister; tek seçim talimatı net tutar.
- **S-7 — Split:** Öneri: dört spec'e bölünsün — 0005a Model bilgisi ve ince ayar (AC-1…5) → 0005b Konsept
  üretimi ve onay (AC-6…24, 34, 35) → 0005c Düzenleme döngüsü (AC-25…29) → 0005d Silinenler (konsept) + önerileri
  sıfırla (AC-30…33). Gerekçe: 35 kriter tek build/review turu için büyük; üçlü bölmede ilk parça ~26 kriterle yine
  büyük kalıyordu. 0005a küçük ve risksizdir (çağrı yok), 0005b konsept üretip onaylamayı tek başına sağlar ve
  0006'yı açar; H-3 riski en yüksek parça (toplu üretim + BR-15) ayrı review alır. Alternatif: 0005a+0005b birleşik
  kalsın (model seçimi olmadan üretim denenemez) — o zaman üç parça, ilki ~26 kriter.

**Self-critique boşlukları (hasım geliştirici gözüyle)**
- **G-1 — Öneri yaşam döngüsü:** Öneriler ne zaman "seçilmemiş" sayılır? Öneri: listedeki öneriler kaydedilir ve
  sayfa yenilense de kalır; "Seçtiklerini üret" denince o listedeki seçilmeyenler, yeni "Konu üret" denince bekleyen
  tüm öneriler "seçilmemiş" olur ve listeden kalkar.
- **G-2 — Doğrudan düzenleme ve talimat birlikte:** Öneri: elle düzenleme "Kaydet" ile doğrudan konseptin yerini
  alır (modal yok, çağrı yok); kaydedilmemiş elle değişiklik varken "Yeniden üret" denirse değişikliklerle birlikte
  gönderilir (brief: "fikriyle beraber ya da değiştirerek"). Kaydedilmemiş değişiklikle başka konsepte geçerken uyarı.
- **G-3 — Toplu üretimi durdurma ve kısmi hata (H-3):** Öneri: üretim sürerken "Durdur" düğmesi; basılınca
  başlamamış konseptler iptal edilir, süren çağrı tamamlanır; başarısız konseptler adlarıyla bildirilir ve
  otomatik tekrar edilmez. Aynı anda konudan ve fikirden üretim çalışabilir; aynı konseptte aynı anda tek düzenleme.
- **G-4 — Kullanıcı sayfadan ayrılırsa:** Öneri: üretim sunucuda sürer, kullanıcı dönünce ilerlemeyi görür;
  tarayıcı kapatmak üretimi iptal etmez (yalnızca "Durdur" eder).
- **G-5 — Fikir metni sınırı:** Öneri: en fazla 2.000 karakter; aşılırsa hata mesajı. Gerekçe: token maliyeti.
- **G-6 — Fikirden üretimde mükerrer başlık:** Öneri: aynı partide ve profilin mevcut başlıklarıyla aynı başlık
  üretilmez (BR-4 ruhu; S-4 kabul edilirse anlamca denetimden de geçer).
- **G-7 — Revizyon modalının kapanması:** Öneri: modal dışına tıklamayla kapanmaz (ücretli revizyon kazara
  kaybolmasın); Esc "Revizyon silinsin mi?" onayı sorar.
- **G-8 — BR-15 revizyonlara da uygulanır mı?** Öneri: evet, kelime denetimi açıkken revizyon da aynı kuralla en
  fazla 1 kez yeniden üretilir.
- **G-9 — Silme onayı ve geri yükleme:** Brief Silinenler'den geri yükleme istemiyor. Öneri: v1'de geri yükleme yok;
  Üretilenler'den silmede kısa bir onay diyaloğu olsun (geri yükleme olmadığı için); kalıcı silinen konseptin
  başlığı negatif sinyal olmaktan çıkar (yalnızca Silinenler'deki sinyal sayılır).
- **G-10 — Önerileri sıfırla kapsamı:** Öneri: yalnızca seçili profil için; Ayarlar sayfası 0004'te gelir, bu spec
  oraya bölüm ekler.
- **G-11 — "Ya olsaydı" ile uydurma yasağı:** Öneri: bu anlatım biçiminde varsayım bölümü seslendirmede açıkça
  "varsayım" olarak sunulur; temel gerçekler yine doğru olmalıdır.
- **G-12 — Anlatım biçimi listesi:** Brief "… gibi çeşitli internet konuları" diyor. Öneri: şimdilik 4 seçenek;
  eklemek istediklerin varsa onayda yaz.
- **G-13 — Hedef süreye uyum:** Öneri: sahne sürelerinin toplamı profil hedef süresinin ±%15'i içinde olmalı;
  dışındaysa konsept uyarıyla kaydedilir (otomatik yeniden üretim yok, H-3).
- **G-14 — Liste büyüklüğü:** Öneri: Üretilenler ve Silinenler en yeni üstte sıralanır, 50'şerli sayfalanır; arama
  v1'de yok.
- **G-15 — Silinenler'de detay:** Öneri: silinmiş konseptin sahneleri salt okunur görüntülenebilir; düzenle/onay yok.
- **G-16 — Dil:** Öneri: öneriler ve konseptler profilin video dilinde; arayüz metinleri Türkçe.
- **G-17 — Öneri talimatının token ekonomisi (Req 7):** Profilin geçmişi yüzlerce başlığa çıkınca "elenen/seçilen
  konular" listesi talimatı şişirir. Öneri: talimata yalnızca başlıklar (özet değil) girer; liste belirli bir
  boyutu aşınca en yeni başlıklar tam, eskiler konu kümeleri hâlinde kısa özetle verilir; eksik kalan birebir
  tekrarlar Req 7'deki liste sonrası elemeyle, anlamca tekrarlar S-4 ile yakalanır. Gerekçe: brief "çok token
  yemeyecek şekilde optimize" diyor; kesin eleme yine sunucuda yapıldığı için özetleme güvenlidir.
- **G-18 — Uzun video ve token sınırı (Req 14a):** 15 dakikalık bir konsept tek çağrıda katalog sınırına
  sığmayabilir (ilk_promt.txt: "32.000 token bazen yetmiyor"). Öneri: v1'de tek çağrı; yarım kalırsa konsept
  kaydedilmez ve "Bu süre için seçili modelin çıktı sınırı yetmedi; daha yüksek sınırlı bir model seçin" mesajı
  görünür; sahneleri parça parça üretme ileride. Gerekçe: otomatik parçalı yeniden üretim gizli maliyet doğurur (H-3).
- **G-19 — BR-3 biçim ihlali:** 3 cümleden uzun özet ya da 1 cümleyi aşan başlık gelirse ne olur? Öneri: öneri
  kesilmez, listelenmez ve eksik kalan adet için yeni çağrı yapılmaz (kullanıcı "n öneri biçim dışı olduğu için
  gösterilmedi" bilgisini görür). Gerekçe: kesmek anlamı bozar; tamamlamak için ek çağrı H-3 riskidir. Cümle
  sayımı nokta/soru/ünlem işaretleriyle yapılır.
- **G-20 — Elle düzenlemede geçersiz girdi:** Öneri: sahne başlığı ve seslendirme metni boş bırakılamaz; süre
  pozitif tam saniye olmalı; konsept en az bir sahne içermeli; sahne ekleme/silme/sıralama v1'de yok (yalnızca
  metin ve süre düzenlenir). Gerekçe: brief yalnızca "metinlere müdahale" diyor; 0006 boş sahne alırsa
  render'da bozulur.
- **G-21 — H-1 kapsamı:** Bu spec model yetkisini (BR-13) ve veri sahipliğini (BR-1) kullanıyor ama zorlamıyor.
  Öneri: "Sensitive area" satırına H-1 eklenmesin; AC-3 ve AC-34 normal testle kanıtlansın, zorlamanın kasıtlı
  bozma kanıtı 0002/0004'te kalsın. Gerekçe: aynı kontrolü iki spec'te kanıtlamak tekrar; ancak 0004 SC-1 H-1'i
  eklerse bu spec de aynı kararı devralır.

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
