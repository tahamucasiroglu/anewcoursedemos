# 0033 — /new-feature Tema ve açık/koyu mod · ANALYZE: niyet ve netleştirme soruları

**Tarih:** 2026-11-19 20:05

## İstek

> /new-feature Tema ve açık/koyu mod: web ve mobilde ortak renk teması, açık/koyu mod düğmesi, seçim hatırlansın, ilk açılışta cihaz ayarına uysun.

## Cevap

Mod lite, `specs/active/` boş (aynı işin ikinci spec'i yok). Aşama: **ANALYZE (analiz)**. Bu adımda niyeti yazıp spec öncesinde netleşmesi gereken soruları soruyorum; kod ve plan yok.

### Niyet taslağı (iş dili)
Okul yöneticisi, öğretmen ve veli uygulamayı açtığında "sade/ham" değil, bakımlı ve tutarlı bir görünüm görmeli. Gece kullanımda göz yormayan koyu bir seçenek de olmalı. Başarı:
- Web paneli ve mobil uygulama aynı renk temasını kullanıyor.
- Kullanıcı açık ya da koyu modu tek dokunuşla seçebiliyor; seçim bir sonraki açılışta hatırlanıyor.
- Hiç seçim yapmamış kullanıcı cihazının ayarını görüyor.
- İki modda da metinler rahat okunuyor.

Bilinçli olarak yapılmayanlar:
- kuruma özel logo ve renk (markalı uygulama, ileriki faz);
- seçimin hesaba kaydedilmesi (henüz giriş yok);
- yeni ekran.

> **Dokümanda istenen:**
> - "Velinin bayılacağı mobil deneyim satış kancası" (s.7).
> - "Farklılaşma: Modern mobil deneyim" (s.12).
> - Mobil: "Push bildirim, kurum logosuyla markalı açılış" (s.62).
> - "Kurum markalı uygulama: okula özel uygulama adı ve logosu" — premium paket (s.98), Faz 3 (s.148).
>
> Doküman tema ya da koyu moddan doğrudan bahsetmiyor; ama modern görünüm ve ileride kuruma özel marka rengi istendiği için temayı "ana renk sonradan değiştirilebilir" şekilde kurmayı öneriyorum.

### Netleştirme soruları (önem sırasıyla; her birinde önerim var, karar senin)

**1. Renk teması nasıl olsun?** Elimizde hazır bir tasarım ya da kurum rengi yok.
- **Önerim:** Sakin ve nötr bir zemin, tek bir ana renk (lacivert, mobildeki "Tekrar dene" düğmesinin rengi) ve durum renkleri (başarılı = yeşil, hata = kırmızı). Bu renkleri bir "renk sözlüğü" (tema değişkenleri) olarak tek yerde tanımlarım.
- **Neden:** İleride kurum markası geldiğinde yalnızca ana rengi değiştirmek yeter. Görsel onayı SHIP'te ekran görüntülerine bakarak verirsin.

**2. Kaç seçenek olsun: 2 (Açık / Koyu) mu, 3 (Sistem / Açık / Koyu) mü?**
- **Önerim:** 3 seçenek. Varsayılan "Sistem", yani cihaz ayarına uyar.
- **Neden:** Sadece 2 seçenek olursa kullanıcı bir kez seçtikten sonra "tekrar cihazıma uysun" diyemez.

**3. Düğme nerede ve nasıl dursun?**
- **Önerim:** Başlığın sağ üstünde, üç parçalı küçük bir seçici: "Sistem · Açık · Koyu". Seçili olan belli olsun. Hem yazı hem simge olsun; yalnızca simge kullanılırsa anlaşılmayabilir.
- **Neden:** Tek düğmeyle sırayla dönen bir yapıda hangi modda olduğun gizli kalır. Üç parçalı seçici ekran okuyucuyla da kolay kullanılır.

**4. Seçim nerede hatırlansın?**
- **Önerim:** Cihaz başına: web'de tarayıcıda, mobilde telefonda saklansın. Sunucuya gönderilmesin.
- **Neden:** Henüz kullanıcı girişi yok. Giriş özelliği gelince "hesabıma kaydet" seçeneğini backlog'a yazarım.

**5. "Sistem" seçiliyken, uygulama açıkken cihazın modu değişirse (ör. telefon akşam otomatik koyuya geçerse) ne olsun?**
- **Önerim:** Uygulama anında uysun; yeniden açmak gerekmesin.

**6. Web açılırken yanlış renk "parlaması" olmasın mı?**
- **Sorun:** Koyu modu seçmiş kullanıcı, sayfa açılırken bir an beyaz ekran görebilir.
- **Önerim:** Olmasın. Kayıtlı tema, sayfa ilk çizilmeden önce uygulansın.

**7. Okunabilirlik (kontrast) için hedef ne olsun?**
- **Önerim:** WCAG AA. Normal metinde en az 4.5:1, büyük metin ve düğme kenarlarında en az 3:1. Bu oranı iki modda da her renk çifti için otomatik testle doğrularım.
- **Neden:** Kontrast "gözle iyi görünüyor" diye değil, ölçülerek doğrulanmalı.

**Kapsam dışı önerim:**
- kuruma özel logo ve renk;
- seçimin hesaba kaydı (giriş gelince);
- yazı boyutu ayarı;
- yeni ekranlar.

**Sonraki adım:** Cevaplarına göre spec'i yazarım, ardından bir "düşmanca okuyucu" gözüyle kendi spec'imi eleştiririm (eksik noktaları öneriyle getiririm). Sonra spec onay kapısı gelir.

### Karar
7 sorudaki önerilerimi kabul ediyor musun? Değiştirmek istediğin varsa numarasıyla söyle (ör. "2: sadece açık/koyu olsun").
