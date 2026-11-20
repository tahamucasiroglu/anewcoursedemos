# Yol haritası — neredeyiz?

> Kaynak: `okul-yonetim-platformu-kapsam.md` (Faz 1 = MVP). Bu dosya "neredeyiz?" sorusunun cevabıdır.
> **Her ship'te güncellenir:** biten satırın durumu ✅ ve spec numarası yazılır; sıra değişirse kullanıcı kararıyla
> değişir. Ertelenen bulgular ayrı yerde: `specs/backlog.md`.

**Son güncelleme:** 2026-11-20 · 0006 ve 0007 VERIFY tamam (13/13 + 13/13); 0004, 0005, 0006, 0007 ship onayı bekliyor.

## Biten altyapı
| Spec | Ne | Durum |
|---|---|---|
| 0001 | Uygulama iskeleti: API + web + mobil + PostgreSQL + loglama + "sistem durumu" ekranı | ✅ 2026-11-19 |
| 0002 | Tema ve açık/koyu mod (web + mobil ortak renkler, seçim hatırlanır) | ✅ 2026-11-19 |
| 0003 | Raspberry Pi deneme ortamı: `http://192.168.1.102:8080`, tek komutla dağıtım ve geri dönüş, kısıtlı veritabanı kullanıcısı | ✅ 2026-11-19 |

## Faz 1 (MVP) — sıralı iş listesi
Durum: ⬜ başlanmadı · 🔄 sürüyor · ✅ bitti. Her satır bir `/new-feature` olarak açılır. Büyük maddeler analizde
daha küçük spec'lere bölünebilir; "sonra" diye işaretlenenler aynı modülün sonraki adımıdır.

| Sıra | Feature | Kapsamdan (ne içerir) | Neden bu sırada | Durum |
|---|---|---|---|---|
| 1a | **Giriş ve oturum** (spec 0004) | Okul açma komutu (okul + ilk şube + kurucu); e-posta + şifre ile giriş/çıkış web + mobil; tek kimlik birden çok okul, okul seçimi; 3 hatada kilit; okul ve şube duvarı; audit | Her şeyin temeli: kim giriyor, hangi okulun verisini görüyor | 🔄 Kod bitti, Pi'de çalışıyor, VERIFY'da 12/13 kanıtlı; **ship tek gözlem bekliyor:** okul açma komutunda şifrenin terminalde görünmediği (AC-1, hassas alan — kanıtsız kapanamaz). Kullanıcı kendi kurucu hesabını açarken bakacak |
| 1b | **Şube ve kullanıcı yönetimi** (spec 0005) | Kurucu web'den şube ve kullanıcı ekler, rol/şube verir, pasifleştirir; geçici şifre, kurucunun şifre sıfırlaması | Diğer rollerin girebilmesi için | 🔄 VERIFY 13/13 kanıtlı; **ship onayı bekliyor** |
| 1b-2 | **Şifre işlemleri ve e-posta** (spec 0006) | Şifremi değiştir (web + mobil), şifremi unuttum (30 dk tek kullanımlık bağlantı), SMTP'ye hazır e-posta (bilgiler girilmeden) | 0005'ten bölündü (kullanıcı kararı 2026-11-20) | 🔄 spec 0006 (Profilim ile birlikte): VERIFY 13/13 kanıtlı; **ship onayı bekliyor** |
| 1b-3 | **Profilim (ad, telefon)** | Kişi kendi ad-soyad ve telefonunu düzenler (web + mobil); iki okulda kayıtlı kişinin bilgisini yalnızca kendisi değiştirebildiği için gerekli | 0005 review tur 2 notu (kullanıcı kararı 2026-11-20); spec 0006 içinde | 🔄 spec 0006 |
| 1c | **Platform yönetim paneli** | Platform sahibinin (proje sahibi) paneli: okul ekle (okul + ilk şube + kurucu), okulları listele, okulu pasifleştir. Bütün okulları gören tek rol → kendi spec'i, ayrı rolü ve 2 adımlı doğrulama ister. Komut (`create-school`) ilk platform yöneticisi ve acil durum için kalır | Kullanıcı isteği 2026-11-20; 0005'in kurucu paneli ekran kalıbını kullanır | 🔄 spec 0007: VERIFY 13/13 kanıtlı; **ship onayı bekliyor** (Pi adımları ship'te kullanıcıda) |
| 2 | **Kurum ve akademik yapı** (spec 0008) | Şube, sınıf, şube-sınıf eşleşmesi, dönem, akademik takvim. Sonra: ders programı (elle + Excel'den alma) | Öğrenci ve yoklama sınıfa bağlı | 🔄 spec 0008: BUILD bitti (sınıf seviyesi, dönem, sınıf, akademik takvim, okul takvimi web + mobil); review bekliyor |
| 3 | **Öğrenci ve veli kayıtları** (spec 0009) | Öğrenci kartı, birden çok veli, kardeş bağlantısı, sağlık notu/alerji, teslim alabilecek kişiler. Sonra: Excel ve e-Okul çıktısından alma | Yoklama ve iletişim öğrenciye bağlı | 🔄 spec 0009: BUILD bitti (web öğrenci sayfası ve kartı, veli bağlama, şifreli sağlık bilgisi, mobil Çocuklarım); review bekliyor. Pi adımları ship'te kullanıcıda (`docs/deploy-pi.md`) |
| 4 | **Yoklama ve devamsızlık** | Öğretmen mobilden 10 saniyede yoklama alır; devamsız öğrencinin velisine otomatik bildirim | Kapsamda MVP'nin ilk maddesi | ⬜ |
| 5 | **Veli iletişimi** | Duyuru (okul/sınıf/öğrenci) + okundu bilgisi; öğretmen-veli mesajı, mesai dışı sessiz mod; onay formları (gezi izni vb.); fotoğraf/dosya paylaşımı | | ⬜ |
| 6 | **AI: sesli nottan veli mesajı** | Öğretmenin 20 sn'lik sesli notunu kibar bir veli mesajına çevirir; öğretmen onaylamadan gönderilmez | Kapsama göre "satış demosunun yıldızı"; mesajlaşmaya bağlı | ⬜ |
| 7 | **Ödev ve not takibi** | Ödev verme, teslim durumu, yazılı/sözlü not; velide sade özet | | ⬜ |
| 8 | **AI: haftalık gelişim özeti** | Yoklama, ödev, not ve öğretmen notlarından çocuğa özel haftalık veli özeti | Önce bu verilerin olması gerekir | ⬜ |
| 9 | **Tahsilat ve ön muhasebe** | Yıllık ücret, taksit planı, indirim/burs, gecikme hatırlatması, makbuz, cari takibi. Online ödeme sağlayıcısı sonra (backlog B-1) | | ⬜ |
| 10 | **Randevu** | Öğretmen müsaitlik takvimi, veliden randevu talebi | | ⬜ |
| 11 | **Yönetici paneli** | Günlük özet (devamsızlık, geciken ödemeler, okunmayan duyurular), temel raporlar, Excel'e aktarma | Diğer modüllerin verisini özetler | ⬜ |

**Her feature'da birlikte gelenler:** web + mobil taraf (veli ve öğretmen için tek mobil uygulama, role göre ekran),
Türkçe metinler, Pi'ye dağıtım. Push bildirim, kurum logosuyla markalı açılış: ilgili feature'larda (sağlayıcı: B-6).

## Pilot öncesi zorunlu (gerçek okul verisinden önce)
| Ne | Neden | Durum |
|---|---|---|
| 2FA (kurucu/yönetici ve muhasebe zorunlu) + çok bilinen şifre listesi — backlog B-17 | Güvenlik kuralı; MVP demosu için ertelendi (kullanıcı kararı 2026-11-19) | ⬜ |
| KVKK veli aydınlatma metni + açık rıza akışı (sürümlü rıza kaydı; fotoğraf rızası öğrenci bazlı) | Öğrenci ve sağlık verisi özel nitelikli kişisel veri; spec 0009 analiz notu (önceden verilen onay, Promtlar 0082) | ⬜ |
| Pi veritabanı günlük yedeği — backlog B-14 | Sağlık verisi şifreleme anahtarı ve veri kaybı riski (spec 0009 analiz notu) | ⬜ |

## Faz 1 dışı (şimdilik yapılmaz)
Faz 2 (sınav analizi, servis, etüt, öğrenci uygulaması, rehberlik) ve Faz 3 (ön kayıt CRM'i, çok şube büyümesi,
markalı uygulama), kapsamın "kapı" kuralına göre: önceki faz pilotta kanıtlanmadan başlamaz.
