# 0022 — Yeni mimari kararlar: kapsamlı loglama + Raspberry Pi · spec 0001 revize

**Tarih:** 2026-11-19 15:56

## İstek

> ilk spec ve sonrası için unuttum mimarisel bir karar var. loglama eklenecek kapsamlı loglama olacak. loglar günlük dosyalara bölünerek tutulacak. leveli ayarlanabilir oalcak şuanlık info ve üstü kaydedilecek. geliştirme sırası kapsamlı hata yönetimi sağlarız ve çözümleri hızlı hallederiz. kodlarda loglamalarda eklenecek.
>
> rasperry pi var. mvp için ksıa süreli projeleri opraya kurup uzaktan local erişerek denenir. web ve api orada çalışır test ederiz dedim.
>
> açık noktalarda önerilerini kabul ediyorum

## Cevap

### Son mesajdan beri yapılanlar
1. **Spec'teki 7 açık nokta senin onayınla işlendi:** "hizmet dışı" 503 koduyla döner; veritabanı en fazla 3 saniye beklenir; web ve mobil API adresini ayar dosyasından alır; hata durumlarında "Tekrar dene" düğmesi olur; tarayıcı erişimi yalnızca ayarda tanımlı web adresine açıktır; spec tek parça kalır; hızlı kontrol süresi kriter değil, ölçülüp raporlanır.
2. **Loglama kararı mimariye yazıldı** → yeni mimari karar kaydı `docs/decisions/0003-kapsamli-loglama-gunluk-dosyalar.md` + `docs/architecture.md` "Logging" + `docs/conventions.md` "Logging" (kodda nasıl log yazılacağı).
3. **Raspberry Pi kararı mimariye yazıldı** → `docs/decisions/0004-raspberry-pi-mvp-deneme-ortami.md` + `docs/architecture.md` "Runtime environments".
4. `.gitignore`'a `logs/` eklendi (log dosyaları repoya girmez). Backlog'a B-7 eklendi: web/mobil hatalarının sunucuya toplanması, ileride.
5. **Spec 0001 güncellendi:** 2 yeni gereksinim (loglama, ortamdan bağımsız ayarlar) ve 3 yeni kabul kriteri (toplam 15).

### Loglama — önerdiğim ayrıntılar (senin isteğin + boşlukları doldurdum)
- **Araç:** Serilog. nArchitecture'ın zaten kullandığı log kütüphanesi.
- **Dosyalar:** `logs/okul-api-20261119.log` gibi, **her gün yeni dosya**. Bir dosya aynı gün 100 MB'ı geçerse bölünür. **30 gün** saklanır, eskisi kendiliğinden silinir. *Neden:* Pi'nin SD kartı dolmasın.
- **Seviye:** Ayar dosyasından veya ortam değişkeninden, yeniden derlemeden değişir. Varsayılan **Information ve üstü** (senin kararın).
- **Biçim:** İnsanın okuyacağı tek satır: zaman, seviye, izleme numarası (traceId), hangi sınıf, mesaj, hata varsa ayrıntısı. *Neden:* Geliştirmede sen okuyacaksın. JSON makine için iyidir ama göz yorar.
- **Ne loglanır:** Her istek (yöntem, adres, sonuç kodu, süre, izleme numarası); her işlemin (Command) başlangıcı ve bitişi; iş olayları ("öğrenci kaydedildi"); kural ihlalleri (Warning); beklenmeyen hatalar (Error, **bir kez**, izleme numarasıyla).
- **Hızlı çözüm için kilit nokta:** Kullanıcı hata ekranında bir **izleme numarası** görür. O numarayı log dosyasında aratınca hatanın tam satırını bulursun.
- **Kodda kurallar:** Her işlem en az bir bilgi logu yazar. Hata "sessizce yutulmaz": ya yeniden fırlatılır ya loglanır. Log mesajları şablonla yazılır. Bu kural derleyici uyarısıyla zorlanır ve senin "uyarı = hata" kuralın sayesinde ihlal derlemeyi kırar.
- **Ne loglanmaz:** Şifre, token, TC, sağlık bilgisi, telefon, e-posta, adres; istek içeriği. Yalnızca kimlik numaraları (öğrenci no, kurum no) yazılır. Bu, güvenlik kararlarımızla uyumlu.

### Raspberry Pi — ne değişti
- Pi = **MVP deneme ortamı**. API ve web orada çalışacak, yerel ağdan erişeceğiz. Mobil uygulama gerçek telefondan Pi'deki API'ye bağlanabilecek.
- **Bu spec'te yapılacak:** Uygulamanın hiçbir yeri Windows'a veya sabit bir adrese bağlı olmayacak. Adres, port, veritabanı bağlantısı, izinli web adresi ve log klasörü ayardan gelecek. Böylece Pi'ye taşımak sadece ayar değiştirmek olur.
- **Önerim:** Pi'ye kurulum ve dağıtım **ayrı bir sonraki spec (0002)** olsun. *Neden:* Bu spec zaten büyük (15 kriter). Ayrıca Pi için cevaplanması gereken sorular var: veritabanı Pi'de mi yoksa bu bilgisayarda mı, Pi modeli ve işletim sistemi, uzaktan erişim (SSH) bilgisi. Bunları 0002'de sorarım.

### Şimdiye kadar verilen kararların özeti (kodsuz)
- Kod kuralların: **dosya başına tek tip**, **uyarı = hata**, **her tipin başında Türkçe açıklama**. Bunlara şimdi **kapsamlı loglama** eklendi.
- Mimari: nArchitecture tarzı tek parça uygulama, .NET 10, PostgreSQL 18, web React, mobil Expo.
- Çalışma: üst repoda, `main` üzerinde, Türkçe commit, push sende. Lite mod: her aşamada senin onayın.

### Karar (tek)
Güncellenmiş spec 0001'i (7 önerin + yukarıdaki loglama ayrıntıları + ortamdan bağımsız ayarlar; 15 kabul kriteri) onaylıyor musun? Onaylarsan PLAN aşamasına geçerim.
