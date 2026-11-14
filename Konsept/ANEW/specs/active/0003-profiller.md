# Spec 0003 — profiller

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0003-plan.md`
- Source: `specs/active/0003-profiller.md` (orijinal taslak); bölünmedi 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: H-1 Kimlik ve yetki — yalnızca sahiplik (BR-1, AC-15)
- Depends on: 0001, 0002
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)

## Intent
İçerik üreticisinin birden fazla sosyal medya kanalı olabiliyor (ör. biri tarih, biri teknoloji, biri futbol);
her kanal için ayrı video konsepti üretmek istiyor. Bu spec, kullanıcının kanallarını **profil** olarak
tanımlamasını sağlar: kanal adı, video dili, biçim (yatay/dikey), hedef süre ve kanal içeriği. Profil, üst
bardaki seçimle tüm sayfaların bağlamı olur (BR-2); kanal içeriği, kullanıcının görmediği arka planda konsept
üretimine giden düzgün ve sabit bir sistem talimatına dönüşür. Yeni kullanıcı ne yazacağını görebilsin diye beş
hazır şablon sunulur. Başarı: kullanıcı üst bardaki artıyla profil ekler (yeni profil kendiliğinden seçilir),
kalemle seçili profili düzenler ya da hiç konsepti olmayan bir profili siler, profiller arasında geçiş yapar ve
seçimi sunucuda, cihazlar arasında korunur. Bilinçli olarak yapılmayanlar: örnek görsel alanı (kullanıcı vazgeçti),
ayrı bir "Profil" sayfası (üst bardan kaldırıldı, yerine Yayınlama geldi), konsept üretiminin kendisi (0014),
profil başına model seçimi ve ince ayar (0005), otomatik "Varsayılan" profil, profil arşivleme.

## Requirements
1. Profil, giriş yapmış kullanıcıya aittir; kullanıcı yalnızca kendi profillerini görür, seçer, düzenler, siler
   (BR-1). Bir kullanıcının en fazla **20** profili olabilir.
2. Bir profilin alanları ve kuralları (tümü zorunlu; örnek görsel alanı yoktur):
   - **Kanal adı:** 1–80 karakter. Aynı kullanıcının profilleri arasında benzersizdir; karşılaştırma büyük/küçük
     harf duyarsız ve Türkçe kurala göredir (tr-TR: "İstanbul" = "istanbul"). Farklı kullanıcılar aynı adı kullanabilir.
   - **Video dili:** listeden seçilir — **Türkçe** ve **İngilizce**; yeni profilde varsayılan Türkçe. Liste
     genişletilebilir tutulur (yeni dil, ses modeli desteği netleşince eklenir).
   - **Biçim:** **yatay** ya da **dikey**.
   - **Hedef süre:** dakika + saniye olarak girilir; izin verilen aralık **15 sn – 15 dk** (dahil). Biçimle süre
     arasında zorunlu bağ yoktur (dikey 15 dk da kaydedilir).
   - **Kanal içeriği:** kanalı tanıtan serbest metin (odak konusu, dönem/tarih aralığı gibi); 1–2000 karakter.
   - Metin alanlarında baştaki ve sondaki boşluklar kaydetmeden önce kırpılır; yalnızca boşluktan oluşan değer
     boş sayılır. Uzunluk sınırları kırpılmış değer üzerinden uygulanır.
3. Üst bardaki **artı** düğmesi, sayfanın üstünde açılan bir **profil modalı** açar (yeni profil). Kullanıcı
   alanları doldurup **Kaydet** derse profil kaydedilir, modal kapanır, yeni profil üst bardaki profil seçiminde
   görünür ve **kendiliğinden seçili profil olur**. Kullanıcının 20 profili varken artı modalı açmaz; "En fazla 20
   profil oluşturabilirsiniz" uyarısını gösterir. Sınır sunucuda da uygulanır.
4. Üst bardaki **kalem** düğmesi, seçili profili aynı modalda, mevcut değerleriyle açar; **Kaydet** değişiklikleri
   kaydeder. Seçili profil yokken kalem kullanılamaz.
5. Yeni profil modalında **beş hazır şablon** sunulur: **Tarih – Bizans**, **Tarih – Osmanlı**, **Teknoloji**,
   **Antik teknoloji**, **Günümüz teknolojisi**. Her şablon örnek kanal adı, video dili (Türkçe), biçim, hedef süre
   ve 2–4 cümlelik kanal içeriği taşır; bu metinler plan aşamasında taslak olarak yazılır ve insan onaylar.
   Şablonlar tüm kullanıcılar için aynıdır, kullanıcı değiştiremez.
   - Bir şablon seçilince modalın beş alanı şablonun değerleriyle doldurulur; kullanıcı kaydetmeden önce her alanı
     değiştirebilir. Şablon seçmek tek başına profil oluşturmaz.
   - Yeni modalda kullanıcının doldurduğu ya da değiştirdiği bir alan varken şablon seçilirse "Doldurduğunuz alanlar
     şablonla değiştirilecek" onayı sorulur; onaylanmazsa alanlar değişmez.
   - Şablonlar **yalnızca yeni profil modalında** vardır; düzenleme modalında şablon seçimi görünmez.
6. Zorunlu alanlardan biri boşsa ya da Requirements 2'deki bir kurala uymuyorsa kayıt yapılmaz; ilgili alanın
   yanında anlaşılır bir hata gösterilir ve girilen değerler korunur. Aynı kontroller arayüzü atlayan doğrudan
   isteklerde sunucuda da uygulanır.
7. Modal kaydedilmeden kapatılırken (Esc, kapat düğmesi) alanlarda kaydedilmemiş değişiklik varsa "Değişiklikler
   kaydedilmeyecek" onayı sorulur; onaylanırsa modal kapanır ve hiçbir şey kaydedilmez, onaylanmazsa modal açık
   kalır. Değişiklik yoksa modal doğrudan kapanır.
8. Üst bardaki **profil seçimi** kullanıcının tüm profillerini kanal adına göre alfabetik (tr-TR sıralaması)
   listeler; bir profil seçilince o profil seçili profil olur ve açık sayfa seçili profilin bağlamıyla yeniden yüklenir.
9. Seçili profil **kullanıcı hesabında, sunucuda** "son seçilen profil" olarak hatırlanır: sayfa değişiminde, sayfa
   yenilendiğinde ve başka bir tarayıcı ya da cihazdan girişte aynı profil seçili gelir. Hatırlanan profil silinmiş
   ya da erişilemezse kullanıcının **ilk oluşturulan** profili seçilir.
10. Seçili profil tüm sayfaların bağlamıdır: Konseptler, Konseptten Video, Üretilenler ve Silinenler yalnızca seçili
    profilin verisini gösterir (BR-2); bu listelemenin kendisi ilgili spec'lerde (0014, 0016, 0006, 0007) kanıtlanır.
    Aylık harcama profilden bağımsızdır, kullanıcının içinde bulunulan aydaki toplamını gösterir (BR-12); harcama
    göstergesi 0013'te geldiği için bu davranış 0013'te kanıtlanır, bu spec'te kriteri yoktur.
11. Profil seçimi yalnızca **görüntülenen** bağlamı değiştirir; çalışan ya da sıradaki üretim işlerini etkilemez
    (sıranın kendisi 0006'dadır; bu kuralın kanıtı 0006 AC-17'dedir).
12. Kullanıcının hiç profili yoksa otomatik profil oluşturulmaz: üst bar profil seçimi yerine profil oluşturmaya
    yönlendiren bir boş durum gösterir; profil gerektiren sayfalar "Önce bir profil oluşturun" boş durumunu gösterir.
    Yönlendirme, artının açtığı yeni profil modalını açar.
13. Profil düzenlemesi **yalnızca bundan sonraki üretimleri** etkiler; mevcut konseptler ve videolar üretildikleri
    andaki dil, biçim ve süreyle kalır (konsept bunları kendisi taşır). Bu bir kuraldır; konsept bu spec'te
    olmadığı için kanıtı konsepti getiren spec'tedir (0014; işte kullanımı 0006 Req 20, biçim 0018 Req 6).
14. **Profil silme:** kalem modalında bir **Sil** eylemi vardır; onay sorusu ("Bu profil kalıcı olarak silinecek")
    onaylanınca profil kalıcı olarak silinir, modal kapanır ve Requirements 9'daki kurala göre başka bir profil
    seçilir (hiç profil kalmazsa Requirements 12'deki boş durum). Silme **yalnızca hiç konsepti (Silinenler dahil)
    olmayan** profil için yapılabilir; konsepti olan profilde Sil pasiftir ve nedenini söyler. Kural sunucuda da
    uygulanır. Konsept bu spec'te henüz olmadığından "konsepti olan profil silinemez" kanıtı 0014'tedir (Silinenler kısmı 0016'da).
15. Aynı profil iki sekmede eşzamanlı düzenlenirse **son kaydeden kazanır**; kilit yoktur.
16. Bir profilin alanları, kullanıcıya gösterilmeyen arka planda, konsept üretiminde kullanılacak bir **sistem
    talimatına** dönüştürülür. Talimat:
    - kanal adını, video dilini (üretilecek metnin dili), biçimi, hedef süreyi ve kanal içeriğini içerir;
    - konsept içeriğinin gerçek bilgiye dayanması, uydurma olmaması talimatını içerir (domain değişmezi);
    - boş ya da gereksiz alan eklemez, gereksiz tekrar içermez (token tasarrufu); kanal içeriği hariç sabit metin
      kısmı en fazla **150 token**dır;
    - kullanıcının kanal içeriği metnini **talimat değil, kanal tanımı verisi** olarak, ayrılmış biçimde taşır
      (`docs/security.md`: kullanıcı/dış içerik promptta "veri" olarak işaretlenir);
    - aynı profil için her zaman birebir aynıdır (sağlayıcı tarafı prompt önbelleğine uygun; beklenmedik değişim
      testte yakalanır).

## User experience
- Flow:
  - **Yeni profil:** Kullanıcı üst barda artıya basar → profil modalı açılır (boş alanlar, video dili Türkçe, beş
    hazır şablon) → isterse bir şablon seçer, alanlar dolar → alanları kendi kanalına göre düzenler → Kaydet → modal
    kapanır, yeni profil üst bardaki seçimde seçili olarak görünür, açık sayfa yeni profille yüklenir.
  - **Düzenle:** Kullanıcı kalem düğmesine basar → modal seçili profilin değerleriyle açılır (şablon yok) →
    değiştirir → Kaydet → üst bardaki ad güncellenir.
  - **Sil:** Kullanıcı kalemle modalı açar → Sil → onay → profil silinir, başka profil seçilir ya da boş duruma düşülür.
  - **Geçiş:** Kullanıcı üst bardaki profil seçiminden başka bir profili seçer → açık sayfa yeni profille yüklenir.
  - **İlk kullanım:** Hiç profili olmayan kullanıcı siteye girer → boş durum onu yeni profil modalına yönlendirir.
- States:
  - Profil seçimi: yükleniyor · profil yok (boş durum) · profiller listelendi (alfabetik, biri seçili) · yükleme hatası.
  - Kalem: seçili profil var (etkin) · seçili profil yok (pasif, nedenini söyleyen ipucu).
  - Artı: etkin · 20 profil sınırında (modal açılmaz, sınır uyarısı).
  - Profil modalı: yeni (boş) · şablonla doldurulmuş · şablon değiştirme onayı · düzenleme (dolu, şablonsuz) ·
    kaydediliyor (Kaydet tekrar basılamaz) · doğrulama hatası (alan bazında) · kayıt hatası (sunucu/bağlantı;
    değerler korunur) · kaydedilmemiş değişiklik onayı · başarı (modal kapanır).
  - Sil: etkin (konsepti yok) · pasif (konsepti var, neden metni) · silme onayı · silme hatası (profil kalır).
  - Profil gerektiren sayfa: profil yok boş durumu · seçili profil bağlamında yüklendi.
- Design reference: `source/konseptler sayfası tasarımı.png` — commit `61cc824` sürümü: üst barın sağında
  "profil seçimi dropdown" ve "+" yer alır. Görseldeki "profil" sayfa bağlantısı **kaldırılmıştır** (yerine
  Yayınlama; ilk_promt.txt), artının yanına **kalem** eklenmiştir (ilk_promt.txt; sıra spec 0001 Requirements 1:
  profil seçimi · kalem · artı · hata bildir · ayarlar · tema). Modalın kendisi için tasarım yok — none, the flow
  above is the design; tema renkleri 0001'deki tema değişkenlerinden gelir.
- Accessibility: profil seçimi, kalem ve artı klavyeyle erişilebilir (Tab, Enter/Space) ve erişilebilir ada
  sahiptir ("Profil seç", "Seçili profili düzenle", "Yeni profil"); modal açılınca odak modalın ilk alanına gider,
  odak modal içinde kalır, Esc modalı kapatır (kaydedilmemiş değişiklik varsa önce onay), kapanınca odak açan
  düğmeye döner; her alanın görünür etiketi vardır ve hata metni alana bağlıdır; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Sensitive area: H-1 Kimlik ve yetki — yalnızca **sahiplik** (BR-1, AC-15). Gerekçe: profil, kullanıcıya ait
  yeni bir veri türüdür ve H-1'in tanımlı zararı ("bir kullanıcı başkasının verisine erişebilir") doğrudan bu
  kritere karşılık gelir; profil sonraki tüm sayfaların bağlamı olduğu için burada sızan bir sahiplik hatası
  0014–0020'ye yayılır. AC-15 için kasıtlı bozma kanıtı (sahiplik filtresi kaldırılınca test kırmızı) istenir ve
  her düzeltme turunda yeniden review yapılır. Giriş/oturum/ban mekanizmalarına dokunulmaz, yalnızca kullanılır.
  Sistem talimatı bu spec'te yalnızca oluşturulur; maliyet doğuran çağrıda kullanımı 0014'te H-3 kapsamındadır.
- Bağımlılık: 0001 (üst bar; pasif profil seçimi, kalem ve artı bu spec'le etkinleşir), 0002 (Kimlik — profil bir
  kullanıcıya aittir, giriş gerektirir).
- Sınırlar: kanal adı 1–80 karakter, kanal içeriği 1–2000 karakter, hedef süre 15 sn – 15 dk, kullanıcı başına en
  fazla 20 profil, talimatın sabit kısmı en fazla 150 token.
- Out of scope: örnek görsel alanı; ayrı profil sayfası; konsept üretimi ve profil talimatının bir modele
  gönderilmesi (0014); model seçimi, ince ayar ve kelime denetimi (profil başına saklanırlar, sahibi 0005);
  profiller arası konsept taşıma; profil arşivleme; otomatik "Varsayılan" profil; ilk girişte modalın kendiliğinden
  açılması; kullanıcının kendi şablonunu kaydetmesi; şablonların admin tarafından yönetilmesi; Türkçe ve İngilizce
  dışında video dili; eşzamanlı düzenleme kilidi; sıranın profiller arası mı profil başına mı olduğu (0006);
  arayüzün çok dilli olması (arayüz yalnızca Türkçe, metinler resx'te — video dili ayrı konudur).
- Split: kept as one — tahamucasiroglu, 2026-11-14, talimat küçük ve profil alanlarıyla birlikte değişiyor

## Acceptance criteria
- [ ] AC-1 — Yeni profil: artıya basılınca modal boş alanlarla (video dili Türkçe) açılır; geçerli değerlerle Kaydet denince profil kaydedilir, modal kapanır, yeni profil üst bardaki seçimde seçili olarak görünür, açık sayfa o profille yüklenir ve sayfa yenilendikten sonra da listede durur.
- [ ] AC-2 — Modalda kanal adı, video dili (Türkçe, İngilizce), biçim (yatay/dikey), hedef süre (dakika + saniye) ve kanal içeriği alanları vardır; örnek görsel alanı yoktur.
- [ ] AC-3 — Şablon: yeni profil modalında beş şablon (Tarih – Bizans, Tarih – Osmanlı, Teknoloji, Antik teknoloji, Günümüz teknolojisi) listelenir; her biri seçilince beş alan o şablonun onaylı değerleriyle dolar; kullanıcı alanları değiştirip kaydedince profil değiştirilmiş değerlerle kaydedilir; şablon seçip Kaydet'e basmadan modal kapatılırsa hiçbir profil oluşmaz.
- [ ] AC-4 — Şablon koruması: kullanıcı bir alanı doldurmuşken şablon seçerse onay sorulur; onaylanmazsa alanlar değişmez, onaylanırsa şablon değerleri yazılır; düzenleme modalında şablon seçimi görünmez.
- [ ] AC-5 — Düzenle: kalem, seçili profili mevcut değerleriyle açar; Kaydet sonrası değişiklikler kaydedilir, üst bardaki ad güncellenir ve başka hiçbir profil değişmez. İki sekmede aynı profil art arda kaydedilirse son kaydedilen değerler kalır.
- [ ] AC-6 — Kaydetmeden kapatma (Esc, kapat düğmesi): değişiklik yoksa modal doğrudan kapanır; değişiklik varsa "Değişiklikler kaydedilmeyecek" onayı sorulur, onaylanmazsa modal değerleriyle açık kalır, onaylanırsa kapanır; her iki durumda da yeni modalda profil oluşmaz, düzenleme modalında profil değişmeden kalır.
- [ ] AC-7 — Geçersiz giriş: zorunlu bir alan boşken ya da yalnızca boşluk içerirken, video dili/biçim listede olmayan bir değerken, kanal adı aynı kullanıcının başka bir profiliyle tr-TR büyük/küçük harf duyarsız eşleşirken ("İstanbul" ile "istanbul") kayıt yapılmaz; ilgili alanın yanında hata görünür ve girilen değerler korunur. Aynı kontroller arayüzü atlayan doğrudan isteklerde sunucuda da uygulanır; başka bir kullanıcının aynı kanal adını kullanması kaydı engellemez. Baştaki/sondaki boşluklar kırpılarak kaydedilir.
- [ ] AC-8 — Sınırlar: kanal adı 1 ve 80 karakterde kabul, 81 karakterde ret; kanal içeriği 1 ve 2000 karakterde kabul, 2001 karakterde ret (uzunluk kırpılmış değerle ölçülür); hedef süre 15 sn ve 15 dk 0 sn'de kabul, 14 sn ve 15 dk 1 sn'de ret. Hem arayüzde hem doğrudan istekte.
- [ ] AC-9 — Profil sayısı: 20. profil kaydedilir; 20 profili olan kullanıcıda artı modalı açmaz ve "En fazla 20 profil oluşturabilirsiniz" uyarısını gösterir; 21. profil için doğrudan istek sunucuda reddedilir.
- [ ] AC-10 — Kaydediliyor / kayıt hatası: kayıt sürerken Kaydet tekrar basılamaz (çift tıklama tek profil oluşturur); sunucu ya da bağlantı hatasında modal açık kalır, bir hata mesajı görünür ve girilen değerler kaybolmaz.
- [ ] AC-11 — Geçiş, sıra ve hatırlama: profil seçimi profilleri kanal adına göre tr-TR alfabetik listeler (ör. "Çağ" "Cem"den sonra, "Dünya"dan önce); başka bir profil seçilince açık sayfa o profille yeniden yüklenir; başka sayfaya geçince, sayfa yenilenince ve başka bir tarayıcıda giriş yapılınca seçili profil aynı kalır; hatırlanan profil silinmişse ilk oluşturulan profil seçili gelir.
- [ ] AC-12 — Boş durum: hiç profili olmayan kullanıcıda otomatik profil oluşmaz; üst bar profil oluşturmaya yönlendiren boş durumu gösterir ve yönlendirme yeni profil modalını açar; kalem pasiftir ve nedenini söyleyen ipucu verir; profil gerektiren sayfalar "Önce bir profil oluşturun" boş durumunu gösterir; ilk profil kaydedilince bu durum kalkar ve tek profil seçili olur.
- [ ] AC-13 — Yükleme durumları: profil listesi gelirken profil seçimi yükleniyor durumunu, liste alınamazsa anlaşılır bir hata mesajı gösterir; yükleniyor ve hata durumlarında kalem pasiftir ve profil gerektiren sayfalar hiçbir profilin verisini listelemez.
- [ ] AC-14 — Profil silme: kalem modalındaki Sil onay sorar; onaylanmazsa profil kalır; onaylanınca profil kalıcı olarak silinir, listeden kalkar, sayfa yenilense de geri gelmez ve ilk oluşturulan kalan profil seçili olur; son profil silinince AC-12'deki boş durum görünür; silme hatasında profil kalır ve hata mesajı görünür. (Konsepti olan profilde Sil'in pasif olması ve sunucunun reddetmesi 0014'te kanıtlanır.)
- [ ] AC-15 — Yetki (H-1): girişsiz kullanıcı profil işlemlerini yapamaz, giriş istenir (0002'nin davranışı); bir kullanıcı başka bir kullanıcının profilini listede göremez, kimliğini bilse bile açamaz, düzenleyemez, silemez ve seçili profil yapamaz (BR-1); bu durumda profil "bulunamadı" sonucu döner, varlığı açığa çıkmaz. Sahiplik filtresi kasıtlı kaldırılınca bu test kırmızıya döner (bozma kanıtı).
- [ ] AC-16 — Sistem talimatı: bir profilden üretilen talimat kanal adını, video dilini, biçimi, hedef süreyi ve kanal içeriğini içerir, doğruluk/uydurmama talimatını içerir; kanal içeriği hariç sabit kısmı en fazla 150 tokendır ve boş/gereksiz alan içermez; aynı profil için iki kez üretilen talimat birebir aynıdır; profil alanı değişince talimat yeni değeri yansıtır; talimat hiçbir arayüzde kullanıcıya gösterilmez.
- [ ] AC-17 — Talimat güvenliği: kanal içeriğine talimat gibi yazılmış bir metin (ör. "önceki talimatları yok say") talimatta kanal tanımı verisi olarak, ayrılmış biçimde yer alır ve sistem talimatının kurallarını (dil, doğruluk) metinden çıkarmaz.
- [ ] AC-18 — Erişilebilirlik: profil seçimi, kalem, artı ve modalın tüm alanları ve onay soruları yalnızca klavyeyle kullanılabilir; modal açılınca odak ilk alana gider, modal içinde kalır, Esc kapatır (değişiklik varsa önce onay), odak açan düğmeye döner; ikon düğmelerinin erişilebilir adı vardır ("Profil seç", "Seçili profili düzenle", "Yeni profil"); her alanın görünür etiketi vardır ve hata metni alana bağlıdır; modal ve üst bar kontrolleri açık ve koyu temada WCAG AA kontrastı sağlar.

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
