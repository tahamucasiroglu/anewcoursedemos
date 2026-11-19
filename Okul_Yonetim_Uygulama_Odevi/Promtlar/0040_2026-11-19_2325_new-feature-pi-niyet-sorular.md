# 0040 — /new-feature Raspberry Pi kurulumu · ANALYZE: niyet ve netleştirme soruları

**Tarih:** 2026-11-19 23:25

## İstek

> /new-feature API ve web, Pi'de servis olarak çalışır. Veritabanı için sadece kendi veritabanına yetkisi olan ayrı bir kullanıcı açılır. Böylece uygulamayı uzaktan, gerçek ortamda deneyebilirsin.  bu adıma başlayalım

## Cevap

Mod lite; `specs/active/` boş, bu işin ikinci bir spec'i yok. Aşama: **ANALYZE**. Bu adımda niyet taslağı ve netleşmesi gereken sorular var; kod ve plan yok.

### Niyet taslağı (iş dili)
Proje sahibi uygulamayı kendi bilgisayarı dışında, ağ üzerinden erişilen gerçeğe yakın bir ortamda denemek istiyor. Bu ortam evdeki Raspberry Pi.

**Başarı:**
- Pi açıldığında API ve web paneli kendiliğinden çalışır.
- Aynı ağdaki bir bilgisayarın tarayıcısından web paneli açılır. Telefondaki mobil uygulama Pi'deki API'ye bağlanır.
- Yeni bir sürüm tek komutla Pi'ye gönderilir; sorun çıkarsa bir önceki sürüme dönülür.
- Uygulama veritabanına yalnızca kendi veritabanına yetkili, ayrı bir kullanıcıyla bağlanır; bu kullanıcının yönetici yetkisi yoktur.
- Şifreler repoya, sohbete ve loglara girmez.
- Log klasörü Pi'de sabit bir yerdedir ve 30 günden eski loglar silinir.

**Bilinçli olarak yapılmayanlar:**
- İnternete açma ve HTTPS;
- otomatik yedekleme;
- birden fazla Pi;
- gerçek okul verisi.

**Hassas alan:** S3 = kimlik/gizli bilgi (veritabanı şifresi). Bu yüzden ANEW kuralı gereği her düzeltme turundan sonra yeniden bağımsız inceleme yapılacak.

> **Dokümanda istenen:**
> - Altyapı bölümü: "Yedekleme, Türkiye'de barındırma seçeneği" (s.111).
> - Ek kaynaklar: ADR-0004 (Pi = MVP deneme ortamı) ve ADR-0003 (log saklama sınırı, SD kart ömrü).
> - ADR-0004'ün açık soruları: PostgreSQL nerede, Pi modeli ve işletim sistemi, erişim (SSH).
> - Bunlar aşağıdaki sorulara dahil. Türkiye'de barındırma ve yedekleme pilot okul aşamasının konusu; burada kapsam dışı.

### Netleştirme soruları (önem sırasıyla; her birinde önerim var)

**1. Pi'nin modeli ve işletim sistemi ne?** Örnek: Pi 4 / Pi 5, kaç GB RAM, Raspberry Pi OS 64-bit mi?
- **Önerim:** 64-bit Raspberry Pi OS, en az 2 GB RAM.
- **Neden:** .NET 10 ve PostgreSQL 18 yalnızca 64-bit (arm64) sürümde çalışır. 32-bit sistem kuruluysa önce yeniden kurulum gerekir.
- Pi'nin ağdaki adı ya da IP'si de gerekli (örn. `okulpi.local` / `192.168.1.50`).

**2. Kurulumu kim yapacak, ben Pi'ye nasıl erişeceğim?**
- **Önerim:** SSH anahtarıyla erişim. Anahtarı bir kez sen tanımlarsın (`ssh-copy-id`); şifre hiçbir yerde yazılmaz.
- Sonra kurulum ve dağıtım betiklerini ben bu makineden çalıştırırım; her çalıştırmadan önce onay isterim.
- **Alternatif:** Betikleri ben yazarım, sen Pi'de elle çalıştırırsın.

**3. PostgreSQL nerede çalışsın?**
- **Önerim:** Pi'de, PostgreSQL 18. Geliştirmedeki sürümün aynısı, resmi paket deposundan arm64.
- **Neden:** Pi tek başına çalışan bir ortam olur. Windows makine kapalıyken de deneme yapılabilir.

**4. Web paneli nasıl sunulsun?**
- **Önerim:** Ayrı bir web sunucusu kurulmasın. API, web panelinin derlenmiş dosyalarını da kendisi sunsun.
  - Tek servis, tek port (örn. `http://okulpi.local:8080`).
  - Web ile API aynı adreste olduğu için ayrı bir CORS ayarı da gerekmez.
  - Geliştirmedeki ayrı çalışma düzeni (Vite + API) değişmez. Bu özellik yalnızca ayarla açılır.
- **Alternatif:** nginx ya da Caddy ile ayrı web sunucusu. Bir yazılım ve bir ayar dosyası daha demek; sade çözümü tercih ettiğin için önermiyorum.

**5. Erişim kapsamı ne olsun?**
- **Önerim:** Yalnızca yerel ağ (ev/ofis Wi-Fi), düz HTTP. İnternete açma ve HTTPS yok.
- Telefondaki mobil uygulama, Expo Go ile Pi'deki API'ye bağlanır. API adresi ortam ayarından verilir.
- **Neden:** Henüz giriş ve kişisel veri yok; internete açmak gereksiz risk. Dışarıdan erişim gerekirse ileride ayrı bir iş olur (backlog).

**6. Güncelleme ve geri alma nasıl olsun?**
- **Önerim:** Tek komut: Windows'ta derle, Pi'ye kopyala, servisi yeniden başlat.
  - API, .NET çalışma ortamı içine gömülü olarak derlenir (self-contained); böylece Pi'ye ayrıca .NET kurulmaz.
  - Son 2 sürüm Pi'de saklanır; tek komutla bir öncekine dönülür.
  - Dağıtımdan sonra sistem durumu adresi otomatik kontrol edilir: "çalışıyor" gelmezse komut hata verir.

**7. Pi'deki kurulum kaldırılabilsin mi, veriler ne olsun?**
- **Önerim:** Evet, ADR-0004 "kurulup kaldırılır" diyor. Bir kaldırma betiği olsun: servisi, uygulama dosyalarını, veritabanını ve veritabanı kullanıcısını siler; çalışmadan önce açıkça onay ister.
- PostgreSQL'in kendisi kaldırılmaz.
- Otomatik yedek yok; Pi'deki veri yalnızca deneme verisi.

**Kapsam dışı önerim:**
- internete açma / HTTPS / alan adı;
- otomatik yedekleme;
- birden fazla Pi;
- Pi'de izleme paneli;
- mobil uygulamanın mağaza derlemesi.

**Sonraki adım:** Cevaplarına göre spec'i yazarım, öz eleştiriyi yapar, ardından spec onayını isterim.

### Karar
7 sorudaki önerilerimi kabul ediyor musun? 1. soru için Pi'nin modelini, işletim sistemini ve ağ adını/IP'sini de yazar mısın? Bilmiyorsan Pi'de `cat /etc/os-release`, `uname -m` ve `hostname -I` komutlarının çıktısı yeterli.
