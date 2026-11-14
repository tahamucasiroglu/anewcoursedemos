# Spec 0014 — Konsept üretimi ve onay

- Status: Draft
- Mode: lite
- Plan: `specs/plans/0014-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0005-konseptler.md` (Req 6–18, 24; AC-5'in üretim kısmı, AC-4'ün talimat kısmı, AC-6…AC-24, AC-18'in düzenleme dışı kısmı, AC-34–35'in bu alanlara düşen kısmı; S-1…S-5, G-1, G-3…G-6, G-11, G-13, G-14, G-16…G-19) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: H-3 Maliyet doğuran çağrılar (toplu öneri/konsept üretimi 1–100, anlamca tekrar denetimi, web araması, BR-15 otomatik yeniden üretimi, maliyet kaydı)
- Depends on: 0001, 0002, 0003, 0004, 0013, 0005

## Intent
İçerik üreticisi, seçili kanal profiline uygun video konseptlerini online modellerle üretmek istiyor: ya modelin
önerdiği kısa konu önerilerinden seçerek (konudan üret) ya da kendi yazdığı bir fikirden doğrudan (fikirden üret).
Üretilen konseptleri Üretilenler'de okuyor ve beğendiklerini video üretimi için onaylıyor (BR-16). Başarı: konseptler
gerçek bilgiye dayanıyor (uydurma yok), aynı profilde aynı konu iki kez önerilmiyor (birebir ve anlamca), her online
çağrının maliyeti kaydediliyor ve hiçbir otomatik döngü kullanıcının bütçesini kontrolsüz harcamıyor (H-3).
Bilinçli olarak yapılmayanlar: model/ince ayar seçim ekranları (0005), düzenleme döngüsü (0015), silme ve Silinenler
(0016), Konseptten Video sayfası ve üretim sırası (0006), çağrı kapısı, yeniden deneme ve harcama kaydının biçimi (0013).

## Requirements
Kaynak: `ilk_promt.txt` (Konseptler bölümü), `docs/domain.md` (BR-1…BR-5, BR-12, BR-13, BR-15, BR-16),
`Promtlar/0005`–`0006` (terimler, BR-3, BR-16), `0009` (yeniden deneme), `0011` (LLM çıktısı güvenilmeyen veri);
çağrı kuralları 0013'ten devralınır; sabah karar özeti Ç-3, Ç-4, Ç-7.

**A. Üretime hazır olma**
1. Üret düğmeleri yalnızca gerekli roller için seçili profilde model seçilmişse ve o modellerin sağlayıcısında geçerli
   API key varsa etkindir: "Konu üret" → konu önerisi + anlamca tekrar denetimi; "Seçtiklerini üret" → konsept
   üretimi; fikirden "Üret" → konsept üretimi + anlamca tekrar denetimi. Aksi halde düğme pasiftir ve Ayarlar'a /
   model bilgisi modalına yönlendiren açıklama görünür.

**B. Konudan üret (sol üst alan)**
2. Kullanıcı 1–100 arası bir tam sayı adet girip "Konu üret" der; konu önerisi modeli en fazla o kadar **konu önerisi**
   üretir. Her öneri 1 cümlelik başlık + en fazla 3 cümlelik kısa özettir; sahne detayı üretilmez (BR-3).
3. Biçim ihlali (başlık 1 cümleden uzun, özet 3 cümleden uzun ya da sahne detayı içeren öneri) kesilmez, listelenmez ve
   eksik kalan adet için yeni çağrı yapılmaz; kullanıcı "n öneri biçim dışı olduğu için gösterilmedi" bilgisini görür.
   Cümle sayımı nokta, soru ve ünlem işaretleriyle yapılır (G-19).
4. Öneriler seçili profilin kanal içeriğine, video diline ve ince ayarına uygun olur. Aynı profilde daha önce üretilmiş
   (durumundan bağımsız; videosu yapılmış olanlar dahil) ya da önerilip seçilmemiş başlıklar tekrar önerilmez (BR-4).
   Bu başlıklar öneri talimatına "elenen konular", daha önce seçilip üretilen başlıklar "seçilen konular" odak bilgisi
   olarak girer (ilk_promt.txt). Model elenecek bir başlığı yine döndürürse o öneri listelenmez. Silinenler'in negatif
   sinyali 0016 ile eklenir.
5. Öneri talimatının token ekonomisi (G-17): talimata yalnızca başlıklar girer (özet girmez); profil geçmişindeki
   başlık sayısı 200'ü aşınca en yeni 200 başlık birebir, daha eskileri tek bir kısa özet bölümüyle verilir. Özet ek bir
   online çağrı olmadan oluşturulur. Talimata girmeyen eski başlıkların birebir tekrarları liste sonrası elemeyle,
   anlamca tekrarları Req 6 ile yakalanır. 200 değeri yapılandırmadan gelir.
6. **Anlamca tekrar denetimi** (BR-4'ün anlam düzeyi; S-4): yeni konu önerileri, profilin geçmiş başlıklarıyla (Req 5'teki
   aynı başlık kümesi) "anlamca tekrar denetimi" rolündeki küçük ve ucuz modelle karşılaştırılır; aynı konunun farklı
   ifadesi (ör. "Vikinglerin Ayasofya seferi" / "Vikingler Konstantinopolis'te") listelenmez. Denetim öneri partisi
   başına tek çağrıdır ve sunucuda AiGateway üzerinden yapılır (yerel model yok). Denetim çağrısı 0013'ün tekrarlarından
   sonra da başarısız olursa öneriler yalnızca birebir elemeyle listelenir ve "Anlamca denetim yapılamadı" bilgisi
   görünür; denetim için ek tekrar yapılmaz.
7. Öneriler seçilebilir bir liste olarak gösterilir ve kaydedilir; sayfa yenilense de liste kalır (G-1). Kullanıcı bir ya
   da birden çok öneri seçip "Seçtiklerini üret" der; yalnızca seçilenler için tam konsept üretilir ve Üretilenler'e
   düşer; o listedeki seçilmeyenler "önerilip seçilmemiş" olarak hafızaya alınır ve listeden kalkar. Yeni "Konu üret"
   denince bekleyen tüm öneriler "önerilip seçilmemiş" olur ve listeden kalkar.

**C. Fikirden üret (sağ üst alan)**
8. Kullanıcı serbest metinle en fazla 2.000 karakterlik bir fikir yazar (ör. "Osmanlı'nın kuruluşu, ilk 3 padişah,
   savaş zaferleri, ilham veren anlatım"; G-5), 1–100 arası konsept sayısı girer ve "Üret" der. Konseptler öneri
   adımı olmadan doğrudan Üretilenler'e düşer (BR-5).
9. Fikirden üretimde aynı partide birbirinin aynısı olan ya da profilin mevcut başlıklarıyla birebir ya da anlamca
   (parti başına tek denetim çağrısı, Req 6) aynı olan konsept kaydedilmez; kullanıcı "n konsept mevcut konularla aynı
   olduğu için kaydedilmedi" bilgisini görür; yerine yeni konsept üretilmez (G-6, H-3).

**D. Konsept üretimi (her iki yol için ortak)**
10. Konsept; başlık ve sıralı sahnelerden oluşur. Her sahnede sahne başlığı, ekranda gösterilecek şeyin tarifi,
    seslendirme metni ve süre (pozitif tam saniye) bulunur. Öneriler ve konseptler profilin video dilinde üretilir;
    arayüz metinleri Türkçedir (G-16).
11. Sahne sürelerinin toplamı profilin hedef süresinin ±%15'i dışındaysa konsept "süre uyumsuz" uyarısıyla kaydedilir;
    otomatik yeniden üretim yapılmaz (G-13, H-3).
12. Konsept üretim talimatı profilin kanal içeriğini, video dilini, hedef süresini ve ince ayarını (0005; "Belirtilmedi"
    olan grup talimata girmez) içerir; doğruluk ister, uydurmayı yasaklar ve "emin olunmayan bilgiyi konsepte koyma"
    kuralını içerir. Anlatım biçimi "ya olsaydı" ise talimat, varsayım bölümünün seslendirmede açıkça "varsayım" olarak
    sunulmasını ve temel gerçeklerin doğru kalmasını ister (G-11). İnce ayar konu önerisi talimatına da uygulanır.
    Kullanıcı metni (fikir) ve dış içerik (arama sonucu) talimatta talimat olarak değil "veri" olarak işaretlenmiş
    bölümde yer alır (`docs/security.md`).
13. **Gerçek veriye dayanma** (S-3): seçili konsept modelinin sağlayıcısı web arama aracını destekliyorsa konsept
    üretiminde araç açılır ve konsept bir **kaynak listesi** (başlık + bağlantı) içerir; kaynaklar düzenleme alanında
    düz metin gösterilir, yalnızca `http`/`https` bağlantılar tıklanabilir ve yeni sekmede açılır. Araç desteklenmiyorsa
    yalnızca Req 12'deki doğruluk kuralları uygulanır ve kaynak listesi boş kalır. Aramanın maliyeti harcama kaydına girer.
    Modelin web araması destekleyip desteklemediği ve arama birim fiyatı katalogdan okunur; bu spec katalogu bu iki
    bilgiyle genişletir (0004 temel katalog).
14. **Kelime denetimi** (BR-15; S-2): seçili profilde kutu açıksa her üretilen konseptin seslendirme metinleri model
    çağrısı olmadan yerel olarak sayılır: profil diline göre durak kelimeler (ve, bir, bu…) hariç, küçük harfe çevrilmiş
    (Türkçe I/İ kuralına uygun) aynı kelime **en az 5 kez geçer ve toplam kelimelerin %2'sini aşarsa** eşik aşılmış
    sayılır. Eşik aşılırsa konsept otomatik olarak **en fazla 1 kez** yeniden üretilir; yine aşılırsa konsept "kelime
    tekrarı" uyarısıyla kaydedilir. Eşik değerleri (5, %2) ve durak kelime listeleri yapılandırmadan gelir, Ayarlar'dan değil.
15. Her yeni konsept "Üretildi" durumuyla seçili profile bağlı olarak kaydedilir (BR-2, BR-16).
16. Bu spec'in yaptığı her online model çağrısı (öneri, konsept, BR-15 yeniden üretimi, anlamca tekrar denetimi, web
    araması) harcama kaydına kendi model rolüyle ve seçili profille girer (BR-12). Kaydın biçimi, maliyet hesabı ve aylık
    gösterge 0013'ün davranışıdır; bu spec yalnızca hiçbir çağrısının kayıt dışı kalmamasını sağlar.
17. Toplu üretimde kullanıcı ilerlemeyi görür (ör. "12/100") ve hata alan konseptler adlarıyla (fikirden üretimde sıra
    numarasıyla) bildirilir. Başarısız çağrılarda yeniden deneme yalnızca 0013'ün çağrı kuralıyla yapılır (429/5xx, en
    fazla 2 tekrar); bu sayfa kendi ek yeniden denemesini eklemez ve başarısız konsepti otomatik tekrar üretmez.
18. Model çıktısı token sınırına takılıp yarım kalırsa (0013 "yarım kaldı") ya da konsept/öneri biçimine uymazsa sonuç
    kaydedilmez, "üretilemedi" olarak bildirilir ve otomatik olarak yeniden üretilmez (H-3). Yarım kalan konsept için
    mesaj: "Bu süre için seçili modelin çıktı sınırı yetmedi; daha yüksek sınırlı bir model seçin". v1'de konsept tek
    çağrıda üretilir; sahneleri parça parça üretme kapsam dışıdır (G-18).
19. **Durdurma** (G-3): üretim sürerken "Durdur" düğmesi görünür; basılınca başlamamış konseptler iptal edilir, süren
    çağrı tamamlanır ve sonucu kaydedilir. Konudan ve fikirden üretim aynı anda çalışabilir.
20. Üretim sunucuda sürer: kullanıcı sayfadan ayrılıp dönünce ilerlemeyi görür; tarayıcıyı kapatmak üretimi iptal etmez
    (G-4). Kullanıcı çıkış yaptığında süren öneri/konsept üretimi Req 19'daki gibi durdurulur (Ç-4 ilkesi).
21. Bir üretim partisi bittiğinde, durdurulduğunda ya da tamamen başarısız olduğunda 0001'in olay kaydına bir olay
    yazılır ve Günlük şeridinde görünür (Ç-7): tür, profil adı ve sayılar (ör. "Konu önerisi: 18 öneri", "Konsept
    üretimi: 18/20 tamamlandı, 2 hata", "Konsept üretimi durduruldu: 5/20"); olay konsept başlığı ya da metni içermez.

**E. Üretilenler (sol alt alan) ve onay**
22. Üretilenler, seçili profilin silinmemiş konseptlerini başlık ve durum rozetiyle listeler; varsayılan filtre
    Üretildi + Onaylandı'dır, diğer durumlar (Sırada, Üretiliyor, Video oldu) filtreyle görünür (S-5). Liste en yeni
    üstte sıralanır ve 50'şerli sayfalanır; arama v1'de yok (G-14). Profil değişince liste o profilin konseptlerine geçer (BR-2).
23. Bir konsepte tıklanınca sağ alttaki düzenleme alanında konseptin tüm sahneleri (başlık, ekran tarifi, seslendirme
    metni, süre) ve varsa kaynak listesi salt okunur görünür. Konsept metni düz metin gösterilir (LLM çıktısı güvenilmeyen
    veridir). Düzenleme 0015 ile gelir.
24. Kullanıcı bir konsepti video üretimi için **onaylar** (S-1): Üretilenler satırında bir **onay (✓) ikon düğmesi** ve
    satır başındaki seçim kutularıyla "Seçilenleri onayla" toplu eylemi vardır. Onaylanan konsept "Onaylandı" durumuna
    geçer ve Konseptten Video sayfasındaki "Onaylanan konsept listesi"nin okuyacağı konseptler arasına girer (BR-16).
    Onaylı konseptte aynı düğme "Onayı geri al" olur ve konsept "Üretildi"ye döner; geri alma yalnızca "Onaylandı"
    durumunda mümkündür (Sırada ve sonrası değil). Satırdaki kalem ikonu 0015, çöp kutusu ikonu 0016 ile eklenir.

**F. Yetki ve sahiplik**
25. Kullanıcı yalnızca kendi profillerinin konseptlerini ve önerilerini görür, üretir ve onaylar (BR-1). Yetkisi olmayan
    model hiçbir yoldan kullanılamaz (BR-13; zorlama 0013).

**G. Profille ilişki (0003'ten devralınan kanıtlar)**
26. **Konsepti olan profil silinemez** (0003 Req 14): profilde durumundan ve silinme işaretinden bağımsız en az bir
    konsept varsa (Silinenler'deki konseptler dahil; silme 0016 ile gelir ve bu kısmın kanıtı 0016'dadır) profil kalem modalındaki Sil pasiftir ve
    nedenini söyler ("Bu profilin konseptleri var; silinemez"). Kural sunucuda da uygulanır: arayüzü atlayan silme
    isteği reddedilir ve profil yerinde kalır.
27. **Konsept profil değerlerini kendisi taşır** (0003 Req 13): konsept, üretildiği andaki profilin video dilini,
    biçimini (yatay/dikey) ve hedef süresini kendi kaydında saklar. Profil sonradan düzenlense de mevcut konseptin bu
    değerleri ve içeriği değişmez; profil düzenlemesi yalnızca bundan sonraki üretimleri etkiler. Konseptin videoya
    dönüşürken bu değerleri kullanması 0006 (Req 6 dil kontrolü, Req 20 işe sabitleme) ve 0018'de (Req 6 biçim)
    devralınır.

## User experience
- Flow (konudan): Kullanıcı üst bardan profil seçer → Konseptler sayfası açılır → sol üstte adet (ör. 20) girip
  "Konu üret" der → başlık + kısa özet listesi gelir → beğendiklerini işaretleyip "Seçtiklerini üret" der → ilerleme
  görünür (gerekirse "Durdur") → konseptler sol alttaki Üretilenler'e düşer.
- Flow (fikirden): Sağ üstte fikir metnini yazar, adet girer, "Üret" der → ilerleme → konseptler Üretilenler'e düşer.
- Flow (karar): Üretilenler'den konsepte tıklar → sahneleri ve kaynakları okur → satırdaki ✓ ile ya da birden çok
  konsepti işaretleyip "Seçilenleri onayla" ile onaylar → yanlış onayı "Onayı geri al" ile düzeltir.
- States:
  - Sayfa: gerekli rol için model seçilmemiş ya da geçerli API key yok (ilgili üret düğmeleri pasif, Ayarlar / model
    modalına yönlendiren açıklama) · hazır.
  - Konudan üret: boş (henüz öneri yok) · geçersiz adet (hata mesajı) · üretiliyor (ilerleme, düğmeler pasif, "Durdur") ·
    öneriler listelendi · hiç seçim yokken "Seçtiklerini üret" pasif · biçim dışı öneri bilgisi · anlamca denetim
    yapılamadı bilgisi · sağlayıcı hatası (sade Türkçe mesaj) · yeni öneri çıkmadı (tüm adaylar elendi; açıklama).
  - Fikirden üret: boş metin (hata) · 2.000 karakter aşıldı (hata) · geçersiz adet (hata) · üretiliyor (ilerleme,
    "Durdur") · tamamlandı · durduruldu · kısmi hata (n konsept üretilemedi; yarım kalan ya da biçimi bozuk çıktılar
    dahil) · mükerrer konsept bilgisi · sağlayıcı hatası (hiç konsept üretilemedi).
  - Üretilenler: yükleniyor · boş ("Henüz konsept yok") · liste · filtre · sayfalama · seçili konsept · kelime tekrarı
    uyarılı konsept · süre uyumsuz uyarılı konsept · durum rozetleri (Üretildi, Onaylandı, Sırada, Üretiliyor, Video oldu).
  - Düzenleme alanı (bu spec'te salt okunur): konsept seçilmedi (yönlendirme metni) · görüntüleme · kaynak listesi
    var / yok.
  - Onay: onaylanıyor · onaylandı · onayı geri al · hata (sade mesaj, durum değişmez).
  - Yetki yok: başka kullanıcının konseptine erişim "bulunamadı" ile sonuçlanır.
- Design reference: `source/konseptler sayfası tasarımı.png` — commit `61cc824` sürümü (sol üst "konu üreterek", sağ
  üst "fikirden üreterek", sol alt "üretilenler", sağ alt "üretilenleri düzenleme alanı"). Öneri listesi, onay
  düğmesi, filtre ve kaynak listesi için görsel yok — yukarıdaki akış tasarımdır. Renk: tema değişkenleri (K-7).
- Accessibility: adet alanları, onay kutuları, ✓ ve "Durdur" düğmeleri klavyeyle erişilebilir; ikon düğmelerinin
  erişilebilir adı var ("Onayla: <başlık>", "Onayı geri al: <başlık>"); ilerleme ve hata mesajları ekran okuyucuya
  duyurulur; WCAG AA kontrast her iki temada.

## Constraints & out of scope
- Sensitive area: H-3 — kriterler AC-12, AC-16, AC-18, AC-19, AC-20, AC-21 kasıtlı bozma ile kanıtlanır
  (`docs/testing.md`). H-1 (model yetkisi BR-13, veri sahipliği BR-1) bu spec'te yalnızca tüketilir; zorlamanın kasıtlı
  bozma kanıtı 0002/0003/0013'te, AC-25 normal testle kanıtlanır (G-21).
- Online çağrılar yalnızca AiGateway üzerinden yapılır (Y-7); bu spec sağlayıcı ekleme, key girme, yeniden deneme kuralı,
  token sınırının uygulanması, harcama kaydının biçimi ve aylık gösterge getirmez (0004/0013); yalnızca kendi
  çağrılarının bunlara uyduğunu kanıtlar. Katalog genişletmesi yalnızca "web araması destekler" ve arama birim fiyatıdır.
- Token limitleri, eşik değerleri (kelime denetimi 5 / %2, talimattaki 200 başlık, süre toleransı ±%15) kodda sabit
  olmaz; katalogdan ya da yapılandırmadan okunur (`docs/conventions.md`).
- Para yalnızca USD ve sent (E-3).
- Testlerde gerçek LLM ya da web araması çağrısı yapılmaz (stub; `docs/testing.md`).
- Out of scope: model/ince ayar/kelime denetimi kutusunun seçimi ve saklanması (0005) · düzenleme döngüsü ve revizyon
  modalı (0015) · silme, Silinenler ve Silinenler'in negatif sinyali, önerileri sıfırla (0016) · Konseptten Video
  sayfası, üretim sırası ve iş durumları (0006) · Günlük geçmişi ve filtreleri (0021) · sahneleri parça parça üretme ·
  arama ve gelişmiş filtreleme · yayınlama.
- Split: done — 0014 Konsept üretimi ve onay / kardeşler: 0005, 0015, 0016

## Acceptance criteria
**A. Hazır olma**
- [ ] AC-1 — "Konu üret" için konu önerisi ya da anlamca tekrar denetimi rolünde, "Seçtiklerini üret" için konsept rolünde, fikirden "Üret" için konsept ya da anlamca tekrar denetimi rolünde model seçilmemişse ya da modelin sağlayıcısında geçerli API key yoksa ilgili düğme pasiftir ve Ayarlar / model modalına yönlendiren açıklama görünür; düğmeyi atlayan doğrudan istek sağlayıcıya çağrı yapılmadan reddedilir.

**B. Konudan üret**
- [ ] AC-2 — Adet 1 ve 100 kabul edilir; 0, 101, boş, ondalık ve harf içeren değerler reddedilir ve alanın yanında hata mesajı görünür; reddedilen istekte hiçbir online çağrı yapılmaz.
- [ ] AC-3 — N adetli "Konu üret" en fazla N öneri listeler; her öneride 1 cümlelik başlık ve en fazla 3 cümlelik özet vardır, sahne detayı yoktur. Stub 2 cümlelik başlık, 4 cümlelik özet ya da sahne içeren öneri döndürünce o öneri kesilmeden elenir, "n öneri biçim dışı olduğu için gösterilmedi" bilgisi görünür ve eksik adet için ek çağrı yapılmaz (BR-3, G-19).
- [ ] AC-4 — Aynı profilde üretilmiş (Üretildi, Onaylandı, Sırada, Üretiliyor ya da Video oldu durumunda) ya da önerilip seçilmemiş bir başlık yeni önerilerde yer almaz; bu başlıklar öneri talimatında "elenen konular", seçilip üretilen başlıklar "seçilen konular" olarak gider (stub'a giden istekte görülür); stub bu başlıklardan birini birebir döndürse bile listelenmez; başka profildeki başlıklar engel olmaz (BR-4, BR-2).
- [ ] AC-5 — Profil geçmişinde 250 başlık varken öneri talimatı en yeni 200 başlığı birebir, kalan 50 için tek bir özet bölümünü içerir, hiçbir öneri özeti içermez ve özet için online çağrı yapılmaz; geçmiş 200 ya da daha azken tüm başlıklar birebir girer. Talimata birebir girmeyen eski bir başlığı stub döndürürse öneri yine listelenmez (G-17).
- [ ] AC-6 — Anlamca tekrar denetimi: stub denetçi bir adayı geçmiş bir başlıkla "aynı konu" işaretleyince o aday listelenmez; bir "Konu üret" partisinde denetim rolüyle tam 1 denetim çağrısı yapılır (0013 tekrarları hariç). Denetim çağrısı tüm denemelerde başarısız olursa öneriler yalnızca birebir elemeyle listelenir, "Anlamca denetim yapılamadı" bilgisi görünür ve ek denetim çağrısı yapılmaz (S-4).
- [ ] AC-7 — Hiç öneri seçili değilken "Seçtiklerini üret" pasiftir; k öneri seçilip üretilince yalnızca k konsept üretilir ve Üretilenler'e "Üretildi" durumuyla düşer; listedeki seçilmeyenler "önerilip seçilmemiş" olarak kaydedilip listeden kalkar. Öneri listesi sayfa yenilendiğinde korunur; yeni "Konu üret" denince bekleyen tüm öneriler "önerilip seçilmemiş" olur ve listeden kalkar (G-1).
- [ ] AC-8 — Henüz öneri yokken alan boş durum metnini; üretim sırasında ilerleme göstergesini ve pasif düğmeleri; tüm adaylar elendiğinde "yeni öneri bulunamadı" açıklamasını; sağlayıcı hatasında sade Türkçe mesajı (stack trace ya da key olmadan) gösterir.

**C. Fikirden üret**
- [ ] AC-9 — Fikir metni boşken, yalnızca boşluk içerirken ya da 2.000 karakteri aşarken "Üret" reddedilir ve hata mesajı görünür; 2.000 karakterlik metin kabul edilir; adet AC-2'deki sınırlarla doğrulanır; reddedilen istekte online çağrı yapılmaz (G-5).
- [ ] AC-10 — Geçerli fikir ve N adetle "Üret" öneri adımı olmadan en fazla N konsept üretir; konseptler doğrudan Üretilenler'e "Üretildi" durumuyla, seçili profile bağlı olarak düşer (BR-5).
- [ ] AC-11 — Fikirden üretim sırasında ilerleme ("k/N") görünür; bazı konseptler sağlayıcı hatasıyla üretilemezse başarılılar kaydedilir ve "n konsept üretilemedi" bildirimi sıra numaralarıyla görünür; hiçbiri üretilemezse sade Türkçe hata mesajı (stack trace ya da key olmadan) görünür ve Üretilenler değişmez.
- [ ] AC-12 — Fikirden üretimde stub aynı partide aynı başlığı iki kez ya da profilde mevcut bir başlığı (birebir ya da denetçinin "aynı konu" işaretiyle) döndürünce o konsept kaydedilmez, "n konsept mevcut konularla aynı olduğu için kaydedilmedi" bilgisi görünür ve yerine yeni konsept üretim çağrısı yapılmaz; parti başına tam 1 anlamca denetim çağrısı yapılır (G-6, H-3). Kasıtlı bozma: yerine üretim eklenince test kırmızıya döner.

**D. Ortak üretim kuralları (H-3)**
- [ ] AC-13 — Üretilen her konsept başlık ve en az bir sahne içerir; her sahnede başlık, ekran tarifi, seslendirme metni ve pozitif tam saniye süre bulunur; eksik alanlı model çıktısı konsept olarak kaydedilmez ve hata olarak bildirilir. Sahne süreleri toplamı hedef sürenin ±%15'i dışındaysa (ör. hedef 60 sn için 50 sn ve 70 sn) konsept "süre uyumsuz" uyarısıyla kaydedilir ve ek çağrı yapılmaz; 51–69 sn arası uyarısızdır (G-13).
- [ ] AC-14 — Konsept üretim talimatı profilin kanal içeriğini, video dilini, hedef süresini, seçili ince ayarı ve doğruluk/uydurma yasağı/"emin olunmayan bilgiyi koyma" kurallarını içerir; "Belirtilmedi" olan ince ayar grubu talimata girmez; anlatım biçimi "ya olsaydı" iken talimat varsayım bölümünün açıkça "varsayım" olarak sunulmasını ister; konu önerisi talimatı da seçili ince ayarı içerir (stub'a giden istekte doğrulanır). Fikir metni ve arama sonuçları talimat olarak değil "veri" olarak işaretlenmiş bölümde yer alır (`docs/security.md`).
- [ ] AC-15 — Web araması: web aramasını destekleyen (katalog) stub modelle konsept üretiminde istek arama aracını içerir, konsept kaynak listesiyle kaydedilir ve aramanın maliyeti harcama kaydına girer; desteklemeyen modelde istekte araç yoktur ve kaynak listesi boştur. Kaynaklar düz metin gösterilir; `http`/`https` dışındaki bağlantı (ör. `javascript:`) tıklanabilir olmaz (S-3).
- [ ] AC-16 — Kelime denetimi açıkken stub ilk çıktıda eşiği aşan konsept döndürürse tam 1 yeniden üretim yapılır; ikinci çıktı da aşarsa konsept "kelime tekrarı" uyarısıyla kaydedilir ve üçüncü bir çağrı yapılmaz (BR-15). Eşik: durak kelimeler hariç aynı kelime ≥5 kez ve toplam kelimelerin >%2'si — 4 kez geçen kelime ya da 5 kez geçip %2'yi aşmayan kelime eşiği aşmaz; "Işık"/"ışık" ve "İz"/"iz" aynı kelime sayılır (S-2). Kasıtlı bozma: sınır kaldırılınca test kırmızıya döner.
- [ ] AC-17 — Kelime denetimi kapalıyken eşiği aşan konsept yeniden üretilmez ve uyarısız kaydedilir; eşiğin altındaki konsept, kutu açık olsa da yeniden üretilmez.
- [ ] AC-18 — Öneri, konsept, BR-15 yeniden üretimi, anlamca tekrar denetimi ve web araması çağrılarının her biri, sağlayıcıya ulaştığında, doğru model rolü ve seçili profille bir harcama kaydı üretir; stub'a giden çağrı sayısı ile oluşan kayıt sayısı eşittir (BR-12; kayıt biçimi ve aylık gösterge 0013). Kasıtlı bozma: bir çağrı türünün kaydı atlanınca test kırmızıya döner.
- [ ] AC-19 — N adetli bir konsept üretim isteğinde (sürekli 5xx ve sürekli eşik aşımı dönen stub ile) konsept üretim çağrısı sayısı N × 2 (ilk + BR-15) × 3 (ilk deneme + 0013'ün en fazla 2 tekrarı) üst sınırını aşmaz; bir "Konu üret" partisinde öneri çağrısı ≤3 ve denetim çağrısı ≤3 kalır; bu sayfa 0013'ün yeniden denemesine kendi tekrarını eklemez (4xx'te konsept başına tam 1 çağrı). Kasıtlı bozma: sayfa düzeyinde ek bir tekrar eklenince test kırmızıya döner.
- [ ] AC-20 — Stub "yarım kaldı" (token sınırına takılmış) ya da biçimi bozuk (ayrıştırılamayan) bir çıktı döndürünce sonuç konsept ya da öneri olarak kaydedilmez, kullanıcıya "üretilemedi" olarak bildirilir (yarım kalan konseptte "Bu süre için seçili modelin çıktı sınırı yetmedi; daha yüksek sınırlı bir model seçin") ve aynı konsept için otomatik yeni çağrı yapılmaz (H-3, G-18). Çağrıda istenen çıktı sınırı seçili konsept modelinin katalog limitidir (kodda sabit değer yok; uygulanması 0013).
- [ ] AC-21 — Üretim sürerken "Durdur" görünür; 10 konseptlik partide 3. konseptin çağrısı sürerken "Durdur" denince 3. konsept tamamlanıp kaydedilir, 4–10 için hiçbir çağrı yapılmaz ve "durduruldu" durumu görünür; başarısız konseptler otomatik tekrar edilmez. Konudan ve fikirden üretim aynı anda çalışabilir ve ilerlemeleri ayrı görünür (G-3).
- [ ] AC-22 — Üretim sürerken sayfa kapatılıp yeniden açılınca üretim kesilmeden sürer ve ilerleme güncel görünür (G-4); kullanıcı çıkış yapınca süren parti AC-21'deki gibi durdurulur ve başlamamış konseptler için çağrı yapılmaz (Ç-4 ilkesi).
- [ ] AC-23 — Bir parti bittiğinde, durdurulduğunda ya da tamamen başarısız olduğunda olay kaydına bir olay yazılır ve Günlük şeridinde son olay olarak görünür (ör. "Konsept üretimi: 18/20 tamamlandı, 2 hata"); olay tür, profil adı ve sayıları içerir, hiçbir konsept ya da öneri başlığı/metni içermez (Ç-7).

**E. Üretilenler ve onay**
- [ ] AC-24 — Üretilenler yalnızca seçili profilin silinmemiş konseptlerini başlık ve durum rozetiyle, en yeni üstte listeler; varsayılan filtre Üretildi + Onaylandı'dır, filtre değiştirilince Sırada / Üretiliyor / Video oldu durumundakiler de görünür; 51. konsept ikinci sayfadadır; profil değiştirilince liste yeni profile göre yenilenir; konsept yokken "Henüz konsept yok", yüklenirken yükleniyor durumu görünür (S-5, G-14).
- [ ] AC-25 — Bir konsepte tıklanınca düzenleme alanında tüm sahneleri (başlık, ekran tarifi, seslendirme metni, süre) ve varsa kaynak listesi salt okunur düz metin olarak görünür; model çıktısındaki HTML/script etiketleri çalışmaz, metin olarak gösterilir. Konsept seçilmemişken alan yönlendirme metni gösterir.
- [ ] AC-26 — "Kelime tekrarı" ya da "süre uyumsuz" uyarısıyla kaydedilmiş konsept listede ve detayda ilgili uyarı işaretiyle görünür.
- [ ] AC-27 — "Üretildi" durumundaki bir konsept satırdaki ✓ ile onaylanınca "Onaylandı" olur, rozeti değişir ve Konseptten Video'nun okuyacağı onaylı konseptler arasında yer alır; seçim kutularıyla işaretlenen k konsept "Seçilenleri onayla" ile tek seferde onaylanır; hiç işaret yokken toplu eylem pasiftir. Onaylı konseptte "Onayı geri al" konsepti "Üretildi"ye döndürür ve onaylı listeden çıkarır; "Onaylandı" dışındaki durumlarda onay ve geri alma istekleri reddedilir. Onaylı listede yalnızca "Onaylandı" durumundakiler yer alır (BR-16, S-1).

**F. Yetki ve erişilebilirlik**
- [ ] AC-28 — Kullanıcı A, kullanıcı B'nin konseptini ya da önerisini görüntüleme, üretme (B'nin profiliyle) ya da onaylama/onayı geri alma girişiminde "bulunamadı" alır, hiçbir online çağrı yapılmaz ve B'nin verisi değişmez (BR-1). Girişsiz istekler reddedilir.
- [ ] AC-29 — Tüm tarayıcı istekleri Web controller'ı üzerinden `$.ajax` ile gider (K-6); adet alanları, seçim kutuları, ✓, "Durdur" ve filtre yalnızca klavyeyle kullanılabilir; ikon düğmelerinin konsept başlığını içeren erişilebilir adı vardır; ilerleme ve hata mesajları ekran okuyucuya duyurulur.

**G. Profille ilişki**
- [ ] AC-30 — En az bir konsepti olan profilin kalem modalında Sil pasiftir ve nedenini ("Bu profilin konseptleri var; silinemez") gösterir; arayüzü atlayan doğrudan silme isteği sunucuda reddedilir, profil ve konseptleri yerinde kalır. Kural konseptin durumuna bakmaz (Üretildi, Onaylandı ve sonraki her durum engeldir). "Silinenler dahil" kısmı (silinmiş konsepti olan profilin de silinememesi) konsept silme 0016 ile geldiği için orada kanıtlanır (0016 AC-13). Konsepti olmayan profil 0003 AC-14'teki gibi silinebilir (0003 Req 14).
- [ ] AC-31 — Video dili Türkçe, biçimi dikey, hedef süresi 60 sn olan profilde üretilen konsept bu üç değeri kendi kaydında taşır; profil İngilizce / yatay / 120 sn olarak düzenlendikten sonra mevcut konseptin dili, biçimi, hedef süresi ve içeriği değişmez, düzenlemeden sonra üretilen konsept yeni değerleri taşır (0003 Req 13).

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
