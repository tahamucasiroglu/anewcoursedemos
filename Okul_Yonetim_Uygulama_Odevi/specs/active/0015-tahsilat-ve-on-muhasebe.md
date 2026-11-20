# Spec 0015 — Tahsilat ve ön muhasebe: ücret, taksit, indirim/burs, tahsilat, makbuz, cari, gecikme hatırlatması

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0015-plan.md`
- Self-critique: done
- Approved by: tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20 — clarify cevapları = analistin
  önerileri; spec ve plan onayı önceden verildi; triage analist/geliştiricide (bilgi dışındaki bulgular düzeltilir);
  ship ve Pi yayını kullanıcıda.

## Intent
Kurucunun en büyük derdi tahsilat: hangi velinin ne kadar borcu olduğunu, hangi taksidin geciktiğini Excel'de ve
defterde takip ediyor, makbuzu elle yazıyor, geciken veliyi tek tek arıyor. Bu spec yol haritasının 9. satırını getirir:
kurucu **ücret tarifelerini** ve **indirim/burs tanımlarını** yapar; kurucu ya da muhasebe her öğrenciye bir **taksit
planı** açar, okula gelen ödemeyi **elle tahsilat** olarak kaydeder, sistem ödemeyi en eski taksitten başlayarak dağıtır
ve numaralı **makbuz** üretir; öğrencinin **cari** dökümü (borç, ödenen, kalan, geciken) her an hazırdır; vadesi geçen
taksit için velilere **otomatik gecikme hatırlatması** uygulama içinden gider; veli mobilde çocuklarının ödeme durumunu
görür. Başarı: muhasebe 9 taksitli bir plan açar, veli okulda 7.500 TL öder, muhasebe tahsilatı 30 saniyede kaydedip
makbuzu yazdırır, vadesi geçen taksidin velisi ertesi gün uygulamada hatırlatma görür. Bilinçli olarak yapılmayanlar:
online/kartla ödeme (B-1), e-Fatura/e-Arşiv (Faz 2), vergi/KDV hesabı, IBAN saklama, gecikme faizi, plan yeniden
yapılandırma, resmi muhasebe (kapsam dışı: "Tam muhasebe / bordro").

### Clarify kararları
Hepsi analistin önerisi; karar: — tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20.

- **C-1 — Ödeme nasıl alınır?** Yalnızca **elle tahsilat kaydı**: kurucu/muhasebe okulda nakit, havale/EFT ya da okulun
  kendi POS cihazıyla alınan ödemeyi sisteme yazar. Kart bilgisi sisteme hiç girmez. Online ödeme sağlayıcı hesabı
  açılınca ayrı spec (B-1). Neden: sağlayıcı hesabı yok; elle kayıt tahsilat takibinin tamamını bugün çözer.
  - Dokümanda istenen: "Online ödeme (kart, taksitli), otomatik gecikme hatırlatması"; domain: "Tahsilat (`Payment`) —
    MVP'de manuel kayıt (sağlayıcı yok)".
- **C-2 — Ücret, indirim ve plan nasıl kurulur?** Kurucu **ücret tarifesi** (ad + yıllık tutar) ve **indirim/burs tanımı**
  (ad + yüzde, 1–100) tanımlar. Plan öğrenci başınadır: tarife + en fazla bir indirim + taksit sayısı (1–12) + ilk vade;
  net tutar = tarife − indirim; taksitler eşit ve aylık, kuruş farkı son taksitte (BR-7). Tutarı sabit indirim, birden
  çok indirimin birleşmesi, taksit tutarını tek tek değiştirme sonra. Neden: okulların çoğu "yıllık ücret / X taksit /
  kardeş %10, burs %50" kalıbıyla çalışır; en sade model bu.
  - Dokümanda istenen: "Yıllık ücret, taksit planı, indirim ve burs tanımları"; BR-7: "taksit bölmede kuruş farkı son
    taksite eklenir (taksitlerin toplamı = ana tutar)".
- **C-3 — Para nasıl tutulur?** Türk lirası, kuruşlu (iki ondalık), proje kuralındaki ondalık para tipiyle; kuruş tamsayı
  önerisi `docs/conventions.md` (Data rules 1) ve BR-7 ile çeliştiği için alınmadı. Vergi/KDV yok; tutarlar "toplam
  tutar"dır. Tek tarife tutarı en fazla 10.000.000,00 TL. Neden: belge kuralı açık; çelişki üretmemek sade olandır.
  - Dokümanda istenen: BR-7 "Para tutarları TRY ve `decimal`"; kapsam dışı: "Tam muhasebe / bordro".
- **C-4 — Tahsilat taksitlere nasıl dağılır, yanlış kayıt nasıl düzelir?** Ödeme, vadesi en eski ödenmemiş taksitten
  başlayarak dağıtılır (kısmi ödeme olur). Kalan borçtan fazla tahsilat reddedilir (avans/alacak sonra). Yanlış kayıt
  silinmez; **gerekçeyle iptal edilir**, dağılım geri alınır, makbuz "İPTAL" olur, numara yeniden kullanılmaz. Plan,
  üzerinde geçerli tahsilat yoksa iptal edilebilir; varsa değiştirilemez. Neden: finansal kayıt iz bırakmalı; en
  öngörülebilir dağıtım kuralı vadeye göre sıralamadır.
  - Dokümanda istenen: "Makbuz/fatura çıktısı, cari takibi"; domain S4 "taksit, indirim/burs, tahsilat, makbuz, cari".
- **C-5 — Makbuz nedir?** Her tahsilata okul başına 1'den başlayan kesintisiz sıra numaralı makbuz; web'de yazdırılabilir
  sayfa (tarayıcının yazdır / PDF olarak kaydet işlevi). Üzerinde "Bu belge fatura yerine geçmez." yazar. e-Fatura/
  e-Arşiv Faz 2. Neden: PDF kütüphanesi gerekmez; numara sırası denetlenebilirlik sağlar.
  - Dokümanda istenen: "Makbuz/fatura çıktısı"; entegrasyon tablosu: "e-Fatura / e-Arşiv — Ücret faturası kesimi — Faz 2".
- **C-6 — Cari kimin hesabıdır?** Cari **öğrenci başına** tutulur (plan borcu − geçerli tahsilatlar); veli ekranında
  çocuklarının carileri ayrı ayrı ve toplamıyla görünür. Ödeyen veliyi kaydetme, veli başına birleşik cari sonra.
  Neden: ücret öğrenciye bağlı; iki velisi olan öğrencide borcu bir veliye yazmak yanlış olur; toplam görünüm velinin
  ihtiyacını karşılar.
  - Dokümanda istenen: domain "Cari (`Ledger`) — Velinin borç/alacak hesabı"; BR-2 "Veli tek hesapla tüm çocuklarını görür".
- **C-7 — Gecikme hatırlatması nasıl gider?** Otomatik, uygulama içi: vadesinin ertesi günü (Türkiye saati, 10:00'dan
  sonra) hâlâ ödenmemiş taksit için öğrencinin etkin velilerine, iletişim modülünün **bildirim kapısından** (spec 0011)
  "Okul bildirimi" gider; ödenmezse 7 günde bir tekrar, taksit başına en fazla 3. Taksit ödenince, plan iptal edilince ya
  da öğrenci ayrılınca durur. SMS/e-posta, gecikme faizi, okul bazında aç/kapa ayarı sonra. Neden: veliye giden her şeyin
  tek çıkışı kapıdır (mimari); 3 hatırlatma ısrarcı olmadan yeterli.
  - Dokümanda istenen: "otomatik gecikme hatırlatması"; mimari: "Communication … veliye giden her şeyin tek çıkışı".
- **C-8 — Kim ne yapar, hangi kanalda?** Kurucu: her şey (tarife ve indirim tanımı yalnız onda). Muhasebe: kendi
  şubelerinin öğrencileri için plan, tahsilat, iptal, makbuz, cari. Öğretmen: erişim yok. Veli: mobilde yalnızca kendi
  çocuklarının ödeme durumu (salt okunur). Personel ekranları web'de. Neden: kapsam rol tablosu; fiyat kararı kurucunundur.
  - Dokümanda istenen: "Muhasebe / İdari personel — Tahsilat, taksit, makbuz — Web"; "Veli — Takip, bildirim, ödeme —
    Mobil"; "Kurucu / Yönetici — Kayıt, tahsilat, raporlar, ayarlar — Web".
- **C-9 — IBAN tutulur mu?** Hayır. Havale/EFT yalnızca ödeme yöntemi olarak seçilir; banka/IBAN bilgisi girilmez.
  Neden: IBAN alan düzeyinde şifreleme ister (`docs/security.md`); MVP'de gereksiz yük ve risk.
  - Dokümanda istenen: domain S4 "… cari, IBAN; kart bilgisi sistemde hiç tutulmaz"; güvenlik: "TC kimlik no, sağlık
    notu, alerji, IBAN: alan düzeyinde şifreli".

## Requirements
1. **Ücret tarifesi:** Kurucu tarife ekler (ad zorunlu ≤ 100, okulda etkinler arasında tekil — harf farkı gözetmez; tutar
   > 0, ≤ 10.000.000,00, en fazla 2 ondalık), adını/tutarını düzenler, pasifleştirir/etkinleştirir. Pasif tarife yeni
   planda seçilemez. Düzenleme açılmış planları değiştirmez (plan, açıldığı andaki tutarı saklar).
2. **İndirim/burs tanımı:** Kurucu indirim ekler (ad zorunlu ≤ 100, tekil; yüzde 1–100 tamsayı), düzenler,
   pasifleştirir. Kurallar tarifeyle aynıdır.
3. **Taksit planı:** Kurucu ya da muhasebe, etkin bir öğrenci için tarife, isteğe bağlı bir indirim, taksit sayısı
   (1–12) ve ilk vade (bugün ile bugünden 365 gün sonrası arası) seçer. İndirim tutarı = tarife × yüzde / 100 (kuruşa
   yuvarlanır, yarım yukarı); net = tarife − indirim; net 0 ise "Net tutar 0 TL; tam burslu öğrenci için plan açılmaz."
   ile reddedilir. Taksitler eşit (kuruşa aşağı), kuruş farkı son taksitte; toplam = net (BR-7). Vadeler ilk vadeden
   itibaren aylık, aynı gün; ayda o gün yoksa ayın son günü. Öğrencinin aynı tarifeden geçerli ikinci planı açılamaz.
   Plan; tarife adı, tarife tutarı, indirim adı/yüzdesi/tutarı ve neti açıldığı andaki hâliyle saklar.
4. **Plan iptali:** Geçerli tahsilatı olmayan plan onayla iptal edilir; tahsilatı olan plan "Bu planda tahsilat var;
   önce tahsilatları iptal edin." ile reddedilir. İptal edilen plan carinin dışında kalır, listede "İptal" görünür.
5. **Tahsilat:** Kurucu ya da muhasebe planı olan öğrenci için tutar (> 0, en fazla 2 ondalık, kalan borcu aşamaz),
   tarih (bugün ya da en fazla 365 gün önce; gelecek olamaz), yöntem (Nakit / Havale-EFT / Kredi kartı (okul POS'u)) ve
   isteğe bağlı not (≤ 200) girer. Tutar vadesi en eski ödenmemiş taksitten başlayarak dağıtılır; taksit durumu Ödendi /
   Kısmen ödendi / Bekliyor / Gecikti olur. Kaydedilince makbuz numarası verilir.
6. **Tahsilat iptali:** Kurucu ya da muhasebe gerekçe (zorunlu ≤ 200) ile tahsilatı iptal eder; dağılım geri alınır,
   taksit durumları yeniden hesaplanır, makbuz "İPTAL EDİLDİ" (tarih, gerekçe) gösterir; iptal edilen iptal edilemez.
7. **Makbuz:** Okul başına 1'den başlayan, boşluksuz ve tekrarsız sıra numarası (aynı anda iki tahsilatta da). Sayfada:
   okul ve şube adı, makbuz no, tarih, öğrenci ad-soyad, plan adı, tutar, yöntem, dağıldığı taksitler, kaydeden kişinin
   adı ve "Bu belge fatura yerine geçmez."; TC kimlik no, IBAN, sağlık bilgisi yok.
8. **Cari ve gecikme:** Öğrencinin carisi: borç (plan neti), geçerli tahsilatlar, kalan, geciken tutar ve tarih sıralı
   hareket dökümü. Taksit, vade gününün bittiği andan (Türkiye saati, ertesi gün 00:00) itibaren kalan tutarı kadar
   gecikmiştir. Personel listesi: planı olan öğrenciler, şube/sınıf, net, ödenen, kalan, geciken; şube ve "yalnızca
   gecikenler" süzgeci, ad araması, 25'lik sayfa, altta toplamlar.
9. **Otomatik hatırlatma:** Her gün Türkiye saatiyle 10:00'dan sonra, vadesi geçmiş ve kalanı olan taksit için: 1.
   hatırlatma vade+1. gün, 2. vade+8, 3. vade+15 (kaçırılan gün sonraki çalışmada bir kez gönderilir, birikmez); en
   fazla 3. Öğrencinin okulda etkin her velisine bildirim kapısından tek bildirim: "{Öğrenci} için {vade} vadeli
   taksidin {kalan} TL'si ödenmemiştir. Bilgi için okul muhasebesiyle görüşebilirsiniz." Taksit ödendiyse, plan iptalse,
   öğrenci ayrıldıysa gitmez. Aynı hatırlatma iki kez gitmez (yeniden başlatma, eşzamanlı çalışma dahil).
10. **Veli görünümü:** Veli mobilde "Ödemeler"de seçili okuldaki çocuklarının (farklı şubeler dahil) planlarını görür:
    net, ödenen, kalan, geciken; taksitler (vade, tutar, durum) ve tahsilatlar (tarih, tutar, yöntem, makbuz no, iptal
    durumu); en üstte çocukların toplam kalanı. Planı yoksa "Henüz ödeme planı yok". Salt okunur; ödeme düğmesi yok.
11. **Yetki ve izolasyon:** Varsayılan reddet. Tarife/indirim yazma yalnız kurucu (muhasebe okur — plan formu için);
    plan/tahsilat/iptal/makbuz/cari kurucu ve muhasebe (muhasebe yalnız kendi şubesindeki öğrenci); öğretmen 403; veli
    yalnız kendi çocuğu; başka okul, yetkisiz şube, başkasının çocuğu 404. Rol ve şube her istekte veritabanından.
12. **Audit ve gizlilik:** Tarife ve indirim ekleme/düzenleme/pasifleştirme, plan açma/iptal, tahsilat kaydı/iptali
    audit'e yazılır (kim, ne, hangi kayıt, ne zaman, IP). Loglarda öğrenci/veli adı ve not/gerekçe metni yok (yalnızca
    kimlikler). Hatırlatma gönderimi Information log'u (kimlikler).

## User experience
- **Flow — web (kurucu, muhasebe):** Hoş geldin → "Tahsilat" bağlantısı (yalnızca kurucu ve muhasebede) → `#/tahsilat`:
  sekmeler "Öğrenciler" (cari listesi: arama, şube, yalnızca gecikenler), "Tarifeler" ve "İndirimler" (kurucuda düzenlenir,
  muhasebede gizli) → öğrenci satırı → öğrenci carisi: plan özeti, taksit tablosu, tahsilatlar, hareket dökümü; "Plan aç"
  (tarife, indirim, taksit sayısı, ilk vade → kaydetmeden önce taksit önizlemesi), "Tahsilat ekle" (tutar, tarih, yöntem,
  not → kaydet → makbuz), tahsilat satırında "Makbuz" ve "İptal et" (gerekçe + onay), planda "Planı iptal et" (onay) →
  makbuz sayfası `#/makbuz/<id>`: yazdırılabilir görünüm, "Yazdır" düğmesi.
- **Flow — mobil (veli):** Hoş geldin → "Ödemeler" → toplam kalan → çocuk kartları → plan ayrıntısı (taksitler, tahsilatlar).
- **States:** her liste: yükleniyor / boş ("Henüz ödeme planı olan öğrenci yok", "Henüz tarife yok", "Henüz ödeme planı
  yok") / dolu / hata + "Tekrar dene"; formlar: alan hataları (istek gönderilmez) / gönderiliyor (düğme kilitli) / sunucu
  hatası (ör. "Tutar kalan borçtan fazla olamaz.", "Net tutar 0 TL; …", "Bu planda tahsilat var; …"); başarı (plan açıldı;
  tahsilat kaydedildi → makbuza git); onay iletişimi (Vazgeç / Onayla, Esc kapatır); makbuz: geçerli / "İPTAL EDİLDİ";
  geciken taksit "Gecikti" metniyle (renk tek başına anlam taşımaz); yetkisiz kişi `#/tahsilat`'ta "Bu sayfaya erişiminiz yok.".
- **Design reference:** none — the flow above is the design (spec 0005 yönetim sayfası sekme ve form kalıbı, 0006 mobil
  hesap ekranı kalıbı, ortak tema).
- **Accessibility:** web akışı yalnızca klavyeyle; form hataları alanla ilişkili ve duyurulur; tablolarda başlık hücreleri;
  tutarlar "1.234,50 TL" biçiminde; iki temada kontrast spec 0002 eşiklerinde; 320 px'te yatay kayma yok (tablo kendi
  içinde kayar); mobilde durum metinleri ve düğmelerde erişilebilirlik etiketi.

## Constraints & out of scope
- Sensitive area: S4 (para — tarife, indirim/burs, taksit, tahsilat, makbuz, cari), S1 (okul/şube izolasyonu, muhasebenin
  şube sınırı), S2 (öğrenci ad-soyad + borç eşleşmesi), S5 (veliye giden otomatik hatırlatma), S6 (audit).
- **Bağımlılık:** spec 0008 (şube, sınıf), 0009 (öğrenci, etkin/ayrıldı durumu, öğrenci–veli bağı), 0011 (bildirim
  kapısı ve velinin akışında "Okul bildirimi") önce biter. Adlar build'de amendment ile eşlenir.
- **Kapsam dışı / sonra:**
  - Online/kartla, taksitli ödeme ve ödeme sağlayıcısı (B-1); veliden uygulama içi ödeme düğmesi.
  - e-Fatura/e-Arşiv (Faz 2), KDV/vergi, resmi muhasebe programına (Logo, Mikro) aktarma.
  - IBAN ve banka hesabı bilgisi; ödeyen veli seçimi; veli başına birleşik cari.
  - Sabit tutarlı indirim, birden çok indirimin birleşmesi, peşinat, taksit tutarını/vadesini tek tek düzenleme, plan
    yeniden yapılandırma, avans/fazla ödeme alacağı, iade.
  - Gecikme faizi, SMS/e-posta hatırlatma (B-2), hatırlatmayı okul/öğrenci bazında kapatma, elle "hatırlatma gönder".
  - Velinin mobilde makbuz görüntülemesi/indirmesi; makbuzda tutarın yazıyla yazılması; PDF dosyası üretimi.
  - Excel'den plan/tahsilat alma; tahsilat raporu ve Excel'e aktarma (yönetici paneli, spec 0017); tahsilat riski
    uyarısı (AI, Faz 2); kayıt silme/anonimleştirme (Compliance spec'i).
- Split: proposed 0015a "Tarife, indirim ve taksit planı" ve 0015b "Tahsilat, makbuz, cari ve hatırlatma"; kept as one —
  tahamucasiroglu (önceden verilen onay, Promtlar 0082), 2026-11-20, yol haritası satırı tek iş; plan olmadan tahsilat,
  tahsilat olmadan plan denenemez; 13 kriterde kalıyor.

## Acceptance criteria
- [ ] AC-1 — Yetki: öğretmen bütün tahsilat işlemlerinde, muhasebe tarife/indirim ekleme-düzenleme-pasifleştirmede 403 alır ve hiçbir şey değişmez; veli personel uçlarında 403; girişsiz istek 401; "Tahsilat" bağlantısı yalnızca kurucu ve muhasebede, başkası `#/tahsilat`'ta "Bu sayfaya erişiminiz yok." görür; rolü alınan muhasebenin bir sonraki isteği reddedilir.
- [ ] AC-2 — İzolasyon: A okulunun kurucusu/muhasebesi B okulunun tarifesini, planını, tahsilatını, makbuzunu ve carisini (kimliğini bilerek bile) göremez, değiştiremez — 404; muhasebe kendi şubesi dışındaki öğrenci için plan açamaz, tahsilat giremez, makbuz/cari açamaz (404) ve listesinde görmez; veli başka velinin çocuğunun ödeme bilgisine 404 alır.
- [ ] AC-3 — Tarife ve indirim: geçerli tarife/indirim eklenir, düzenlenir, pasifleştirilir ve yeniden etkinleştirilir; boş ad, 101 karakter ad, tutar 0 / negatif / 3 ondalık / 10.000.000,01, yüzde 0 / 101 / ondalıklı alan hatası verir ve hiçbir şey oluşmaz (10.000.000,00 ve %100 kabul); aynı adla ikinci etkin kayıt (harf farkı gözetmeden) reddedilir; pasif tarife/indirim plan formunda seçilemez; tarife tutarı değişince açılmış planların tutarı değişmez.
- [ ] AC-4 — Plan hesabı: 10.000,00 TL + %15 indirim + 3 taksit → indirim 1.500,00, net 8.500,00, taksitler 2.833,33 / 2.833,33 / 2.833,34; 1.000,00 / 7 taksit kuruş farkı son taksitte ve toplam her zaman neti tutar; %100 indirim "Net tutar 0 TL; tam burslu öğrenci için plan açılmaz." ile reddedilir; 31 Ocak ilk vadeli planda Şubat vadesi ayın son günü; taksit sayısı 0/13, dünkü ya da 366 gün sonraki ilk vade, ayrılmış öğrenci ve aynı tarifeden ikinci geçerli plan reddedilir.
- [ ] AC-5 — Plan iptali: tahsilatı olmayan plan iptal edilir, listede "İptal" görünür ve cariye/gecikmeye/hatırlatmaya girmez; geçerli tahsilatı olan plan "Bu planda tahsilat var; önce tahsilatları iptal edin." ile reddedilir; tahsilatları iptal edilmiş plan iptal edilebilir.
- [ ] AC-6 — Tahsilat ve dağıtım: 3 × 1.000 TL taksitli planda 1.500 TL tahsilat 1. taksidi "Ödendi", 2.'yi "Kısmen ödendi" (500 kalan) yapar; kalan borca eşit tutar kabul edilir, 0,01 fazlası "Tutar kalan borçtan fazla olamaz." ile reddedilir; tutar 0, 3 ondalık, gelecek tarih, 366 gün önceki tarih, eksik yöntem, 201 karakterlik not alan hatası verir ve hiçbir şey kaydedilmez; iptal planına tahsilat reddedilir.
- [ ] AC-7 — Tahsilat iptali: gerekçeyle iptal edilen tahsilatın dağılımı geri alınır ve taksit durumları yeniden hesaplanır (geçmiş vadeli taksit yeniden "Gecikti"); gerekçesiz iptal alan hatası; aynı tahsilat ikinci kez iptal edilemez; iptal edilen tahsilat cari toplamlarına girmez ama listede ve makbuzda "İPTAL EDİLDİ" (tarih, gerekçe) olarak kalır.
- [ ] AC-8 — Makbuz: okulun ilk tahsilatı 1 numarayı alır, sonrakiler boşluksuz artar, iptal edilenin numarası yeniden verilmez, iki okulun numaraları birbirinden bağımsızdır; makbuz sayfası gereksinim 7'deki alanları ve "Bu belge fatura yerine geçmez." içerir, TC kimlik no, IBAN ve sağlık bilgisi içermez; yazdırma görünümünde menü/düğmeler basılmaz.
- [ ] AC-9 — Cari ve gecikme: öğrenci carisinde borç, geçerli tahsilat toplamı, kalan ve geciken tutar doğrudur ve hareket dökümü tarih sırasıyladır; vadesi bugün olan taksit gecikmiş sayılmaz, Türkiye saatiyle ertesi gün 00:00'dan sonra sayılır (UTC'de hâlâ önceki gün olan saatlerde de); personel listesinde şube süzgeci, "yalnızca gecikenler", ad araması, 25'lik sayfalama ve alt toplamlar doğrudur; liste boşken "Henüz ödeme planı olan öğrenci yok".
- [ ] AC-10 — Otomatik hatırlatma: vade+1. gün 10:00'dan (Türkiye saati) önce hiçbir şey gitmez, sonra öğrencinin etkin her velisine bir bildirim gider ve velinin akışında "Okul bildirimi" olarak görünür; vade+8 ve vade+15'te tekrar, 4. hiç gitmez; birkaç gün kapalı kalan sistem açılınca birikmiş hatırlatmalardan yalnızca bir tane gönderir; ödenen taksit, iptal plan, ayrılmış öğrenci, pasif veli ve başka öğrencinin velisi bildirim almaz; aynı zamanda iki çalışma tek bildirim üretir; bildirim kapısı hata verirse Warning loglanır ve diğer hatırlatmalar sürer.
- [ ] AC-11 — Veli görünümü: veli mobilde seçili okuldaki bütün çocuklarının (farklı şubeler dahil) planlarını, taksit durumlarını, tahsilatlarını (makbuz no, iptal durumu) ve toplam kalanı doğru görür; planı olmayan velide "Henüz ödeme planı yok"; ödeme/düzenleme işlemi yoktur; başka okulun çocuk kaydı gelmez.
- [ ] AC-12 — Eşzamanlılık, audit ve gizlilik: aynı plana aynı anda gönderilen iki tahsilat toplamda kalan borcu aşamaz (biri reddedilir) ve aynı makbuz numarası iki kez verilmez; gereksinim 12'deki her işlem audit kaydı oluşturur (kim, ne, hangi kayıt, ne zaman, IP), reddedilen işlem audit'e başarı olarak yazılmaz; log satırlarında ve hata yanıtlarında öğrenci/veli adı, not ve iptal gerekçesi geçmez.
- [ ] AC-13 — Ekran durumları (iki tema): web — cari listesi, öğrenci carisi, tarife/indirim sekmeleri (yükleniyor / boş / dolu / hata + tekrar dene), plan formu taksit önizlemesiyle, tahsilat ve iptal formları (alan hataları — istek gitmez; gönderiliyor — düğme kilitli; sunucu hatası duyurulur), onay iletişimi, makbuz (geçerli / İPTAL EDİLDİ, Yazdır), muhasebede tarife/indirim sekmesi yok; akış yalnızca klavyeyle, 320 px'te yatay kayma yok; mobil — "Ödemeler" yalnızca velide, yükleniyor / boş / dolu / hata + tekrar dene, "Gecikti" metinle, erişilebilirlik etiketleri.

## Self-critique (hostile reader) — folded in
Önceden verilen onayla önerilerin hepsi yukarıya işlendi:
- "Yüzde yuvarlaması?" → kuruşa, yarım yukarı (AwayFromZero); taksit kuruşa aşağı, fark son taksitte (AC-4 örnekleri).
- "Aynı öğrenciye iki plan (ör. eğitim + yemek)?" → farklı tarifeden olur; aynı tarifeden geçerli ikinci plan olmaz.
- "Plan dönemle bağlı mı?" → hayır; tarife adı ("2026-2027 Eğitim Ücreti") dönemi anlatır (sade); döneme bağlama sonra.
- "Öğrenci ayrılınca borç?" → plan ve cari kalır, personel görür; hatırlatma durur.
- "Tahsilat tarihi plandan önce olabilir mi?" → evet (okul eski bir ödemeyi girer); yalnızca gelecek ve 365 günden eski yasak.
- "Hatırlatma metninde tutar var, S4 değil mi?" → yalnızca o öğrencinin velilerinin uygulama içi akışında; sahte push
  log'unda yalnızca kimlikler; e-posta/SMS yok.
- "Kurucu aynı zamanda muhasebe ise?" → kurucu yetkisi geçerli (bütün şubeler).
- "Muhasebe tarifeyi görmeli mi?" → plan formunda etkin tarifeleri ve indirimleri seçmek için okur; yönetim sekmesi yok.
- "Makbuz numarası yıl başında sıfırlanır mı?" → hayır, okul başına tek kesintisiz sıra (sade, denetlenebilir).

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
