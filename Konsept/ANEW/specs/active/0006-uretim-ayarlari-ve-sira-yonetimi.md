# Spec 0006 — Üretim ayarları ve sıra yönetimi

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0006-plan.md`
- Self-critique: done (öneriler 2026-11-14 onaylandı ve işlendi)
- Source: `specs/active/0006-uretim-konseptten-video.md` (bölüm A, B, F; Req 1–10, 24; AC-3 arayüz kısmı, AC-5–17, AC-36/37 ayar-sıra kısmı) — bölündü 2026-11-14, onay: tahamucasiroglu "önerilerin uygun"
- Sensitive area: none — yok (sahiplik kontrolü 0003'teki BR-1 kalıbını kullanır ve normal testle kanıtlanır; bu spec hiçbir maliyet doğuran çağrı yapmaz — çağrılar 0017'de. Çıkışta durdurma kuralı H-3'ü koruyan bir kuraldır, çağrı yapmaz.)
- Depends on: 0001, 0002, 0003, 0004, 0011, 0012, 0005, 0014, 0015, 0009

## Intent
İçerik üreticisi, Konseptler sayfasında video için seçtiği (Onaylandı) konseptleri Konseptten Video
sayfasında görüyor, ayrıntısını okuyor, üretim sırasına alıyor, sırayı Yukarı/Aşağı ile düzenliyor ve
istemediğini çıkarıyor; videonun nasıl üretileceğini (altyazı, ilerleme çubuğu, video kodu modeli ve eforu,
video motoru, görsel modeli ya da "koddan çiz", ses modeli ya da "sessiz") profil başına bu sayfadan seçiyor.
Bu spec ayrıca tek iş durum listesinin (Sırada · Üretiliyor · Durduruldu · Ajan bekleniyor · Hata ·
Tamamlandı) ve sıranın başlat / durdur / devam denetiminin sahibidir; çıkışta sıranın durdurulması da
buradadır. Başarı: yüzlerce konseptlik bir sıra Pi'de akıcı biçimde hazırlanabiliyor, ayarlar profil başına
korunuyor ve kullanılamayacak bir ayarla "Sırayla üret" hiçbir zaman başlatılamıyor. Bilinçli olarak
yapılmayanlar: işin aşamaları, online çağrılar, maliyet, ajan ve Üretilenler paneli (0017); ajanda kısıtlı
render (0018); konsept düzenleme (0015); Üretilen Videolar sayfası (0007).

## Requirements
**A. Üretim ayarları ve model seçimi**
1. Sayfanın sağ üstünde iki aç/kapa seçimi vardır: **Altyazı** ve **İlerleme çubuğu** (ilk_promt: altyazı
   seçiminin yanında). Seçimlerin videoya yansıması 0017'dedir; bu spec seçimi gösterir, saklar ve işe sabitler.
2. Sayfanın sol üstünde, Konseptler sayfasındaki model bilgisi alanıyla (0005) aynı biçimde bir **model
   seçimi** özeti durur (seçili modellerin adı ve efor seviyesi). Tıklanınca sayfanın üstünde ayrı bir modal
   açılır; modal, dışına tıklanınca, Kapat ile ya da Esc ile kapanır ve seçimler kaydedilir. Modalda dört seçim vardır:
   - **Video kodu modeli** + **efor seviyesi** (seçili modelin katalogda desteklediği seviyeler, low … max);
   - **Video motoru**: **HyperFrames** (varsayılan, sürümü sabit) ve **Remotion** (opsiyonel; yalnızca ajanda
     kuruluysa — 0009'daki isteğe bağlı bileşen, Req 19 — listelenir; "Lisans uyarısı" etiketli, hiçbir profilde
     varsayılan olarak seçili gelmez). Revideo ve Motion Canvas listede yoktur (Karar 5);
   - **Görsel modeli** ya da **Kapalı — koddan çiz** (görsel üretilmez, sahneler yalnızca video koduyla çizilir).
     v1 listesi: Z-Image-Turbo (yerel), FLUX.2 klein 4B (yerel), OpenAI gpt-image (online; 0004'teki tek online
     görsel sağlayıcısı). Yeni profilde varsayılan: ajanda Z-Image-Turbo kuruluysa (0009 "Yerel görsel" bileşeni,
     Req 19) Z-Image-Turbo, değilse **Kapalı — koddan çiz**. FLUX.2 klein 9B lisans nedeniyle listede yoktur;
     fal Z-Image-Turbo ve Nano Banana backlog'dadır;
   - **Ses modeli** ya da **Kapalı — sessiz video** (seslendirme yapılmaz). v1 listesi: Supertonic 3 (yerel,
     varsayılan), Chatterbox Multilingual V3 (yerel). Katalogdaki "ses" türü bu spec'te tanımlanır (Req 18);
     online ses modeli (ElevenLabs) ve karakter başı fiyat 0017'de eklenir; OpenAI TTS listede yoktur.
3. Her seçeneğin yanında **"yerel"** ya da **"online"** etiketi vardır; yalnızca tek dili destekleyen ses ya da
   görsel modellerinin yanında **"Yalnız Türkçe"** / **"Yalnız İngilizce"** etiketi vardır (ilk_promt). Ollama
   modelleri hiçbir listede yer almaz (E-4).
4. Listelerde yalnızca kullanıcının kullanma yetkisi olan modeller seçilebilir (BR-13; yetki iskeleti 0011).
   Online bir model için kullanıcının o sağlayıcıya ait kayıtlı API key'i yoksa model, 0004'teki katalog
   etiketiyle ("Key yok") gösterilir; açıklama Ayarlar'a yönlendirir (0005 ile aynı davranış).
5. Ses modeli "Kapalı" iken Altyazı seçimi pasiftir ve yanında "Sessiz videoda altyazı yok" açıklaması görünür
   (altyazı zamanlaması sese bağlıdır; sessiz videoda metin göstermek ayrı bir özelliktir ve v1'de yoktur).
6. Seçili profilin sırasındaki *Sırada* işlerden birinin konseptinin **kaydındaki video dili** (0014 Req 27;
   profil sonradan düzenlense de değişmez, Req 20) ile seçili ses ya da görsel modelinin tek dil etiketi
   uyuşmuyorsa (ör. dili İngilizce olan konseptin işi sıradayken "Yalnız Türkçe" ses modeli) seçim yapılabilir,
   ama modalda ve özet yanında uyarı görünür ve Sırayla üret pasif olur; neden "Seçili ses modeli konsept
   dilini desteklemiyor" (görselde "Seçili görsel modeli konsept dilini desteklemiyor") yazar. Profilin
   güncel dili (0003) bu kontrolde kullanılmaz.
7. Ayarlar (altyazı, ilerleme çubuğu, dört model seçimi ve efor) **seçili profil için** saklanır ve sayfa yeniden
   açıldığında son seçimle gelir (Ç-3); hiç seçim yapılmamış profilde varsayılanlar gelir (görsel varsayılanı
   Req 2'deki kurulu bileşen kuralıyla, sayfa açıldığı anda belirlenir). Bir iş
   *Üretiliyor*'a geçtiği anda o işin ayarları işe sabitlenir; sonradan yapılan değişiklik o işi değil,
   henüz başlamamış işleri etkiler.

**B. Onaylanan konsept listesi ve üretim sırası**
8. **Onaylanan konsept listesi** (sol üst), üst barda seçili profilin (BR-2) yalnızca *Onaylandı*
   durumundaki konseptlerini gösterir (BR-16); Konseptler sayfasında onaylanan konsept buraya kendiliğinden
   gelir. Her satırda konseptin başlığı ve bir seçim kutusu vardır.
9. Listede bir konseptin satırına tıklamak, altındaki **konsept ayrıntısı** alanında o konseptin başlığını ve
   sahnelerini (sahne başlığı, ekranda gösterilecek şey, seslendirme metni, süre) düz metin olarak gösterir.
   Ayrıntı alanı **salt okunurdur** ve "Konseptler'de düzenle" bağlantısı verir; bağlantı Konseptler
   sayfasında o konseptin düzenleme akışını (0015) açar. Düzenleme döngüsü yalnızca 0015'tedir (Ç-11).
10. Seçim kutuları işaretlenip **Üretim sırasına al** denince seçilen konseptler, listedeki görünüş sırasıyla
    üretim sırasının sonuna eklenir; işlerin durumu *Sırada*, konseptlerin durumu *Sırada* olur ve onaylanan
    listeden kalkarlar. Hiçbir kutu işaretli değilse eylem pasiftir. Sıraya alma yalnızca *Onaylandı*
    durumundaki konseptler için geçerlidir; başka durumdaki (ör. zaten *Sırada*) ya da silinmiş bir konsepti
    sıraya alma girişimi reddedilir; aynı konsept iki kez sıraya giremez.
11. Her profilin kendi üretim sırası vardır. **Üretim sırası** (orta sütun) her satırda konsept başlığını,
    sıra numarasını, işin durumunu ve sağda **Yukarı**, **Aşağı** ve **Çıkar (×)** düğmelerini gösterir.
    En üstteki satırda Yukarı, en alttakinde Aşağı pasiftir. Çıkar, işi sıradan alır; konseptin durumu
    *Onaylandı*'ya döner ve onaylanan listeye geri gelir. *Durduruldu* ya da *Hata* durumundaki iş de
    çıkarılabilir (tamamlanmış aşama çıktılarının saklanması 0017'de). *Üretiliyor* ya da *Ajan bekleniyor*
    durumundaki iş taşınamaz ve çıkarılamaz; önce durdurulmalıdır.
12. Sıradaki bir işin konsepti (iş durumu Sırada, Üretiliyor, Durduruldu, Ajan bekleniyor ya da Hata iken)
    Konseptler sayfasında düzenlenemez ve silinemez; ilgili düğmeler pasiftir ve "Önce üretim sırasından
    çıkarın" açıklamasını gösterir; API isteğiyle zorlanırsa reddedilir.

**C. Tek iş durum listesi ve sıra denetimi (Ç-4, Ç-5)**
13. Bir işin durumu yalnızca şunlardan biridir: **Sırada · Üretiliyor · Durduruldu · Ajan bekleniyor · Hata ·
    Tamamlandı**. Bu liste tek kaynaktır; diğer spec'ler (0017, 0007, 0019, 0020, 0024) buna referans verir.
    *Tamamlandı* olan iş sıradan kalkar. Ajan bekleniyor, Hata ve Tamamlandı geçişlerini 0017 tetikler.
14. Sıranın üstünde **Sırayla üret** düğmesi vardır. Basılınca sıra "çalışıyor" olur ve en üstteki *Sırada*
    iş *Üretiliyor*'a geçer (konsepti *Üretiliyor*). Kullanıcı başına aynı anda tek iş çalışır. Düğme şu
    durumlarda pasiftir ve nedenini yazar: sıra boş ("Sıra boş") · seçili modellerden birine yetki yok
    ("Model yetkiniz yok") · seçili online modelin API key'i yok ("API key yok — Ayarlar") · dil uyuşmazlığı
    (Req 6) · model listesi yüklenemedi · bu profilde üretim zaten çalışıyor ("Üretim sürüyor") · başka bir
    profilde üretim çalışıyor ("Başka bir profilde üretim sürüyor"). Üst barda profil değiştirmek çalışan ya da
    sıradaki işleri etkilemez; işler kendi profillerinin sırasında sürer (0003 Req 11).
15. Çalışan iş varken **Durdur** düğmesi görünür: çalışan iş *Durduruldu* olur ve sıranın ilerlemesi durur
    (Sırada işler kendiliğinden başlamaz). *Durduruldu* ya da *Hata* durumundaki işte **Kaldığı yerden devam
    et** görünür: iş yeniden *Üretiliyor* olur ve sıra yeniden "çalışıyor"a geçer. Hangi aşamanın yeniden
    çalışacağı 0017'dedir.
16. Kullanıcı web'den çıkış yaptığında (ya da son geçerli web oturumunun süresi dolduğunda, Req 21) o kullanıcının çalışan işi *Durduruldu* olur ve sırası durur (Sırada
    işler kendiliğinden başlamaz); tamamlanan aşamalar korunur. Yeniden girişte sıra ve işler bu durumda
    görünür ve kullanıcı "Kaldığı yerden devam et" ile sürdürür. Ban durumunda aynı mekanizma 0024'te kullanılır.
    Durdurma anında süren bir online çağrının davranışı 0017'dedir.

**D. Yetki**
17. Kullanıcı yalnızca kendi profillerinin konseptlerini, sırasını, işlerini ve ayarlarını görür ve yönetir
    (BR-1); başka bir kullanıcının konseptini sıraya alma, sırasını değiştirme, işini durdurma/devam ettirme
    ya da ayarlarını değiştirme girişimleri reddedilir.

**E. Yerel katalog girişleri ve kurulu bileşenler (Ç-8)**
18. Bu spec 0004'ün kataloğunu yerel girişlerle ve iki yeni türle genişletir (0004 yalnızca online modelleri
    ve "metin" / "görsel" türlerini taşır; Ollama/yerel LLM yine yoktur, E-4): katalog türlerine **"ses"** ve
    **"video motoru"** eklenir; yerel görsel modelleri (Z-Image-Turbo, FLUX.2 klein 4B) "görsel" türünde, yerel
    ses modelleri (Supertonic 3, Chatterbox Multilingual V3) "ses" türünde ve video motorları (HyperFrames,
    Remotion) "video motoru" türünde, hepsi "yerel" olarak tanımlanır. Yerel girişler API key gerektirmez ve birim fiyat taşımaz; tek dil etiketi
    (Req 3) ve Remotion'un "Lisans uyarısı" katalog verisidir. Online ses girişi (ElevenLabs) ve karakter başı
    fiyat bu "ses" türüne 0017'de eklenir; tür yeniden tanımlanmaz.
19. Eşleşmiş ajan (0012) sunucuya bağlandığında ve "Kurulum ve bileşenler"de (0009) bir bileşen kurulduğunda
    kurulu isteğe bağlı bileşenlerin listesini bildirir; sunucu bunu kullanıcı başına saklar. Ajan hiç
    bağlanmamışsa hiçbir isteğe bağlı bileşen kurulu sayılmaz. Bu bilgi yalnızca Remotion'un listelenmesini ve
    yeni profilin görsel varsayılanını belirler (Req 2); diğer yerel modeller kurulu olmasalar da listelenir,
    eksik kurulumun üretimdeki sonucu 0017'dedir.

**F. Konseptin taşıdığı değerler (0014 Req 27)**
20. İş, konseptin **kaydındaki** video dilini, biçimini (yatay/dikey) ve hedef süresini kullanır; profilin
    (0003) güncel değerleri kullanılmaz. Bir iş *Üretiliyor*'a geçtiğinde bu üç değer Req 7'deki ayarlarla
    birlikte işe sabitlenir; profil sonradan düzenlense de sıradaki ve çalışan işler etkilenmez. Dil kontrolü
    Req 6'daki gibi konsept diliyle yapılır; biçim ve sürenin videoya yansıması 0017 ve 0018'de bu sabitlenen
    değerlerle olur.

**G. Birden çok tarayıcı ve oturumun kapanması (Ç-4)**
21. Req 16'daki durdurma, kullanıcının **son geçerli web oturumu kapandığında** tetiklenir: çıkışta (0002 Req 15
    çıkışta tüm oturumları düşürdüğü için her çıkış son oturumu da kapatır) ya da kullanıcının son geçerli web
    oturumunun süresi dolduğunda (0002 Req 13). Bir tarayıcıdaki oturumun süresi dolarken başka bir tarayıcıda
    geçerli oturum varsa üretim işleri durdurulmaz. Son oturumun süresi dolunca çalışan iş en geç bir sonraki
    aşama ya da online çağrı başlamadan *Durduruldu* olur ve sıra durur; süre dolduktan sonra yeni hiçbir online
    çağrı başlamaz (H-3).

## User experience
- Flow: Kullanıcı üst bardan profilini seçer → Konseptten Video'yu açar → sol üstte seçili modelleri görür,
  tıklayıp modalda video kodu modelini ve eforunu, motoru, görsel ve ses modelini seçer, kapatır → sağ üstte
  Altyazı ve İlerleme çubuğu seçimlerini yapar → onaylanan konsept listesinde bir konsepte tıklayıp
  ayrıntısını alttaki alanda okur (gerekirse "Konseptler'de düzenle"ye gider) → istediklerini işaretleyip
  "Üretim sırasına al" der → orta sütunda Yukarı/Aşağı ile sıralar, istemediğini × ile çıkarır → "Sırayla
  üret" der → gerekirse "Durdur", sonra "Kaldığı yerden devam et" der.
- States:
  - Profil seçili değil (hiç profil yok): sayfa "Önce bir profil oluşturun" gösterir, tüm eylemler pasif.
  - Onaylanan liste boş: "Onaylanmış konsept yok — Konseptler sayfasından video için konsept seçin".
  - Ayrıntı alanı: hiçbir konsept seçili değil → "Ayrıntı için bir konsepte tıklayın"; seçili → salt okunur içerik + "Konseptler'de düzenle".
  - Üretim sırası boş: "Sıra boş"; Sırayla üret pasif.
  - Sırayla üret pasif nedenleri: Req 14'teki liste.
  - İş durumları: Sırada · Üretiliyor · Durduruldu · Ajan bekleniyor · Hata · Tamamlandı (sıradan kalkar).
  - Model seçimi modalı: açık · kapalı; model listesi yüklenemedi (hata) · "Key yok" / "Yetki yok" etiketli
    seçenek (0004) · dil uyuşmazlığı uyarısı · "yerel"/"online", "Yalnız Türkçe"/"Yalnız İngilizce", "Lisans uyarısı" etiketleri.
  - Altyazı pasif (ses Kapalı): "Sessiz videoda altyazı yok".
  - Yükleniyor (listeler ilk açılışta) ve sunucuya ulaşılamadı (hata, korelasyon numarasıyla).
  - Yetkisiz: oturum yoksa giriş sayfasına yönlenir (0002).
- Design reference: `source/konseptten video.png`, commit `61cc824` sürümü: üstte "modeller seçimi" (sol) ve
  "altyazı var ya da yok ayarı" (sağ) şeridi; altında sol üstte "onaylanan konseptlerin listesi", sol altta
  "konsept ayrıntısı alanı" (görseldeki "düzenleyebilir de" notu yerine salt okunur + bağlantı — Ç-11), ortada
  "üretim sırası". Sağ sütun (Üretilenler) 0017'dedir. Üst bar 0001'den gelir. İlerleme çubuğu seçimi görselde
  yok; ilk_promt'a göre altyazı seçiminin yanına konur. Renk: turuncu/kahve, açık mod esas.
- Accessibility: tüm seçim kutuları, aç/kapa düğmeleri, Yukarı/Aşağı/× ve Sırayla üret/Durdur/Devam klavyeyle
  (Tab, Enter/Space) kullanılabilir; ikon düğmelerinin erişilebilir adı vardır ("<başlık>'ı yukarı taşı"
  gibi); modal açılınca odak modala geçer, Esc kapatır, kapanınca odak tetikleyen öğeye döner; iş durumu
  yalnızca renkle değil metinle de belirtilir; kontrast her iki temada WCAG AA.

## Constraints & out of scope
- Kapsam: bu spec işin içindeki aşamaları çalıştırmaz. 0017 gelene kadar *Üretiliyor*'a geçen iş orada kalır;
  Ajan bekleniyor / Hata / Tamamlandı geçişlerini 0017 ekler. Kriterler durum geçişleriyle kanıtlanır.
- Model listeleri 0004'ün kataloğundan okunur; yerel modelleri ve video motorlarını kataloğa "yerel" olarak bu
  spec ekler (Req 18, Ç-8); 0004 online metin + görsel girişlerini taşır, online ses (ElevenLabs) 0017'de eklenir.
- Performans: 250 konseptlik onaylanan liste ve 250 işlik sıra Pi 5 üzerinde ilk görünümde ≤ 2 sn'de açılır,
  sıraya alma / taşıma / çıkarma sonrası güncelleme ≤ 1 sn'dir; gerekirse liste sayfalı ya da sanal kaydırmalıdır.
- Out of scope: işin aşamaları, durdur/devamın aşama düzeyindeki anlamı, online çağrılar ve maliyet, ajan,
  Üretilenler paneli ve yerel video akışı (0017) · ajanda kısıtlı render (0018) · konsept düzenleme (0015) ·
  Üretilen Videolar sayfası, yeni sürüm, video silme (0007, 0019, 0020) · ban (0024) · aynı anda birden fazla
  iş · tek işi atlatan "Atla" düğmesi (ileride) · sessiz videoda ekran yazısı · Revideo, Motion Canvas,
  FLUX.2 klein 9B, OpenAI TTS, Ollama · fal Z-Image-Turbo, Nano Banana ve GLM ile görsel üretimi (backlog) ·
  yayınlama · *Video oldu* konseptin "Yeniden üret" sürüm işinin sıraya kabulü (0020 genişletir; Req 10 ve
  AC-10'daki ret bu bağlam dışında geçerlidir) · × ile çıkarılan sürüm işinde konsept *Video oldu* kalır
  (0020 Req 15; AC-12'deki *Onaylandı*'ya dönüş yalnızca bu spec'in sıraya aldığı *Onaylandı* konseptlerin
  işleri için geçerlidir).
- Split: done — 0006 Üretim ayarları ve sıra yönetimi / kardeşler: 0017, 0018

## Acceptance criteria
**Ayarlar ve model seçimi**
- [ ] AC-1 — Model seçimi özetine tıklanınca modal açılır; dört seçim (video kodu modeli + efor, video motoru, görsel modeli/Kapalı — koddan çiz, ses modeli/Kapalı — sessiz video) görünür; dışına tıklama, Kapat ve Esc modalı kapatır ve seçimi kaydeder; özet yeni seçimi gösterir. Video motoru listesi yalnızca HyperFrames (varsayılan) ve Remotion'dur; Remotion yalnızca ajan onu kurulu bildirdiyse (0009 isteğe bağlı bileşeni) listelenir, kurulu değilse ya da ajan hiç bağlanmadıysa listede görünmez; listelendiğinde "Lisans uyarısı" etiketlidir ve yeni profilde seçili gelmez; Revideo ve Motion Canvas yoktur.
- [ ] AC-2 — Efor listesi yalnızca seçili modelin katalogda desteklediği seviyeleri gösterir; model değişince desteklenmeyen efor seçimi modelin varsayılanına döner.
- [ ] AC-3 — Görsel listesi yalnızca Z-Image-Turbo, FLUX.2 klein 4B, OpenAI gpt-image (online) ve "Kapalı — koddan çiz"i, ses listesi Supertonic 3, Chatterbox Multilingual V3 ve "Kapalı — sessiz video"yu gösterir; fal Z-Image-Turbo ve Nano Banana yoktur. Yeni profilde seste Supertonic 3 seçili gelir; görselde ajan Z-Image-Turbo'yu kurulu bildirdiyse Z-Image-Turbo, bildirmediyse (ya da ajan hiç bağlanmadıysa) "Kapalı — koddan çiz" seçili gelir; FLUX.2 klein 9B, OpenAI TTS ve hiçbir Ollama modeli hiçbir listede yoktur. Her seçenekte "yerel" ya da "online" etiketi, tek dilli modellerde "Yalnız Türkçe"/"Yalnız İngilizce" etiketi görünür.
- [ ] AC-4 — Kullanıcının yetkisi olmayan bir model listede pasiftir ve seçilemez; API isteğiyle zorlanırsa reddedilir (BR-13). API key'i olmayan online model "Key yok" etiketiyle görünür ve seçiliyken Sırayla üret pasiftir, nedeni "API key yok — Ayarlar" yazılıdır; yetkisi kalkmış bir model seçili kaldıysa neden "Model yetkiniz yok" yazılıdır.
- [ ] AC-5 — Ses modeli "Kapalı — sessiz video" seçilince Altyazı seçimi pasif olur ve "Sessiz videoda altyazı yok" açıklaması görünür; ses modeli yeniden seçilince Altyazı önceki değeriyle etkin olur.
- [ ] AC-6 — Kaydındaki video dili İngilizce olan bir konseptin işi sıradayken "Yalnız Türkçe" etiketli bir ses (ya da görsel) modeli seçilince uyarı görünür ve Sırayla üret pasif olur, nedeni "Seçili ses modeli konsept dilini desteklemiyor" ("Seçili görsel modeli …") yazılıdır; profilin dili sonradan Türkçe yapılsa da uyarı sürer (kontrol profilin güncel dilini değil konseptin kaydındaki dili kullanır); uyumlu model seçilince ya da iş sıradan çıkarılınca uyarı kalkar.
- [ ] AC-7 — Ayarlar profil başına saklanır: A profilinde yapılan seçim sayfa yenilenince A'da korunur, B profiline geçince B'nin kendi seçimi gelir; hiç seçim yapılmamış profilde varsayılanlar gelir. Bir iş *Üretiliyor*'a geçtiğinde ayarları işe kaydedilir; ardından profil ayarı değiştirilirse o işin kayıtlı ayarı değişmez, sonra başlayan iş yeni ayarı kaydeder.

**Onaylanan liste ve sıra**
- [ ] AC-8 — Onaylanan konsept listesinde seçili profilin yalnızca *Onaylandı* durumundaki konseptleri görünür; başka profilin ya da Üretildi / Sırada / Üretiliyor / Video oldu durumundaki konseptler görünmez. Konseptler sayfasında yeni onaylanan konsept, sayfa yenilenince listede görünür.
- [ ] AC-9 — Listede bir konsepte tıklanınca ayrıntı alanında başlığı ve tüm sahneleri (başlık, ekran tarifi, seslendirme metni, süre) salt okunur düz metin olarak görünür; içerikteki HTML/script çalışmaz, metin olarak görünür; alanda düzenlenebilir hiçbir öğe yoktur. "Konseptler'de düzenle" bağlantısı Konseptler sayfasında o konseptin düzenleme akışını açar. Hiç seçim yokken "Ayrıntı için bir konsepte tıklayın" görünür.
- [ ] AC-10 — 3 konsept işaretlenip "Üretim sırasına al" denince üçü listedeki görünüş sırasıyla sıranın sonuna eklenir, iş ve konsept durumları *Sırada* olur, onaylanan listeden kalkar; hiçbir kutu işaretli değilken eylem pasiftir. *Onaylandı* olmayan (Üretildi, zaten Sırada, Video oldu) ya da silinmiş bir konsepti sıraya alma isteği reddedilir, sıra ve konsept durumu değişmez; aynı konsept iki kez sıraya giremez.
- [ ] AC-11 — Yukarı/Aşağı bir işi bir sıra taşır ve yeni sıra yenilemeden sonra korunur; en üstteki işte Yukarı, en alttakinde Aşağı pasiftir; tek elemanlı sırada ikisi de pasiftir. Her profilin sırası ayrıdır: A profilinde yapılan değişiklik B'nin sırasını değiştirmez.
- [ ] AC-12 — × ile çıkarılan *Sırada*, *Durduruldu* ya da *Hata* durumundaki işin konsepti *Onaylandı*'ya döner ve onaylanan listede yeniden görünür; diğer işlerin göreli sırası değişmez.
- [ ] AC-13 — *Üretiliyor* ya da *Ajan bekleniyor* durumundaki işin Yukarı/Aşağı/× düğmeleri pasiftir; API isteğiyle zorlanırsa reddedilir ve sıra değişmez.
- [ ] AC-14 — Sıradaki bir işin konsepti (Sırada, Üretiliyor, Durduruldu, Ajan bekleniyor, Hata) Konseptler sayfasında düzenlenemez ve silinemez: düğmeler pasiftir, "Önce üretim sırasından çıkarın" yazılıdır; API isteğiyle zorlanırsa reddedilir ve konsept değişmez. Sıradan çıkarılınca düzenleme ve silme yeniden açılır.
- [ ] AC-15 — Boş durumlar: hiç profil yokken "Önce bir profil oluşturun" ve tüm eylemler pasif; onaylanan liste boşken "Onaylanmış konsept yok — Konseptler sayfasından video için konsept seçin"; sıra boşken "Sıra boş" ve Sırayla üret pasif.
- [ ] AC-16 — 250 konseptlik onaylanan liste ve 250 işlik sıra Pi 5 üzerinde ≤ 2 sn'de ilk görünüme gelir, kaydırılabilir; sıraya alma, taşıma ve çıkarma sonrası güncelleme ≤ 1 sn'dir.

**İş durumu ve sıra denetimi**
- [ ] AC-17 — Sırayla üret denince en üstteki *Sırada* iş *Üretiliyor* olur (konsepti *Üretiliyor*); aynı kullanıcının hiçbir zaman iki işi *Üretiliyor* değildir. Üst barda profil değiştirmek çalışan ya da sıradaki işlerin durumunu ve sırasını değiştirmez (0003 Req 11). Bu profilde iş çalışırken düğme pasif ve "Üretim sürüyor", başka profilde iş çalışırken "Başka bir profilde üretim sürüyor" yazılıdır. API ve arayüzde görünen iş durumları yalnızca Sırada · Üretiliyor · Durduruldu · Ajan bekleniyor · Hata · Tamamlandı'dır.
- [ ] AC-18 — Durdur denince çalışan iş *Durduruldu* olur ve sıradaki hiçbir iş kendiliğinden *Üretiliyor*'a geçmez; "Kaldığı yerden devam et" denince aynı iş yeniden *Üretiliyor* olur ve sıra yeniden çalışır; *Hata* durumundaki işte de "Kaldığı yerden devam et" görünür.
- [ ] AC-19 — Kullanıcının bir işi *Üretiliyor* iken web'den çıkış yapılınca iş *Durduruldu* olur ve Sırada işler başlamaz; yeniden girişte sıra ve iş bu durumda görünür ve "Kaldığı yerden devam et" işi yeniden *Üretiliyor* yapar.

**Yetki ve genel durumlar**
- [ ] AC-20 — Başka bir kullanıcının konseptini sıraya alma, sırasını değiştirme, işini durdurma/devam ettirme ve profil ayarlarını değiştirme istekleri reddedilir ve hiçbir veri değişmez; listeler yalnızca oturumdaki kullanıcının verisini içerir (BR-1).
- [ ] AC-21 — Erişilebilirlik: bu sayfadaki tüm kontroller klavyeyle kullanılabilir ve erişilebilir ada sahiptir; modal açılınca odak modala geçer, Esc kapatır, odak tetikleyen öğeye döner; iş durumu renk dışında metinle de belirtilir.
- [ ] AC-22 — Diğer sayfa durumları: listeler ilk açılışta "Yükleniyor" gösterir; sunucuya ulaşılamazsa sade Türkçe hata ve korelasyon numarası görünür (stack trace yok); model listesi yüklenemezse modal hata mesajı gösterir ve Sırayla üret pasif kalır; oturum yokken sayfa giriş sayfasına yönlenir.

**Yerel katalog ve kurulu bileşenler**
- [ ] AC-23 — Katalogda Z-Image-Turbo, FLUX.2 klein 4B, Supertonic 3, Chatterbox Multilingual V3, HyperFrames ve Remotion "yerel" girişler olarak bulunur; türleri açıkça Z-Image-Turbo ve FLUX.2 klein 4B için "görsel", Supertonic 3 ve Chatterbox Multilingual V3 için "ses", HyperFrames ve Remotion için "video motoru"dur ve katalogun tür listesi "metin", "görsel", "ses", "video motoru"dur; API key'i hiç kayıtlı olmayan kullanıcıda bu girişler "Key yok" etiketi almaz ve birim fiyatları yoktur; katalogda hiçbir Ollama/yerel LLM girişi yoktur (Ç-8, E-4).
- [ ] AC-24 — Ajan bağlanıp kurulu isteğe bağlı bileşen listesini bildirince sunucu bunu o kullanıcı için saklar; listede Remotion ve Z-Image-Turbo varken AC-1 ve AC-3'teki "kurulu" davranışı, yokken "kurulu değil" davranışı görülür; başka kullanıcının ajanının bildirimi bu kullanıcının listesini etkilemez (BR-1).

**Konsept değerleri ve oturumun kapanması**
- [ ] AC-25 — Türkçe / dikey / 60 sn profilde üretilip onaylanan konsept sıraya alınır; ardından profil İngilizce / yatay / 120 sn olarak düzenlenir; iş *Üretiliyor*'a geçtiğinde işe sabitlenen dil, biçim ve hedef süre konseptin kaydındakilerdir (Türkçe, dikey, 60 sn), profilinkiler değildir; "Yalnız Türkçe" ses modeli seçiliyken bu iş için dil uyarısı görünmez ve Sırayla üret dil nedeniyle pasif olmaz (Req 6, Req 20).
- [ ] AC-26 — Kullanıcının iki tarayıcıda geçerli oturumu ve *Üretiliyor* bir işi varken birinci tarayıcının oturumunun süresi dolar (saat ilerletilerek): iş *Üretiliyor* kalır ve sıra durmaz. Ardından kalan son oturumun da süresi dolunca iş *Durduruldu* olur, *Sırada* işler başlamaz ve süre dolduktan sonra yeni hiçbir online çağrı başlamaz (stub sağlayıcının çağrı sayısıyla). İki oturum açıkken birinden "Çıkış yap" denince (0002 Req 15 tüm oturumları düşürür) iş AC-19'daki gibi *Durduruldu* olur (Req 21).

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
