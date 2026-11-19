# 0021 — /new-feature 0001 Walking skeleton · ANALYZE: spec taslağı ve onay kapısı

**Tarih:** 2026-11-19 15:46

## İstek

> /new-feature "Walking skeleton: nArchitecture backend (5 katman, K1–K3 kuralları, architecture testleri), PostgreSQL sağlık ucu + test veritabanı yaşam döngüsü, web (React) ve mobil (Expo) iskeleti, ortak api-client; check.conf adımlarını aktifleştir." Bana onay isterken son konuşmadan sonra yaptıklarını kısa net anlat. sorunun sebebini ve seçimin ne için olacağını anlat. öneri vermeye çalış. neye karar verdiğimizi anlat. Çünkü ben iş adımları yada k1 gibi karar verdiğimiz maddelerin madde kodundan kendisini anlayamıyorum hatırlamıyorum. projeden kopmamam için bunları ilerde bana k1=....  gibi açıklayarak sor. k dışında maddeler içinde geçerli. Feature bitince yeni feature önerileri sun. bu feature da bitirdiklerimizi genel bir yaz. ship mi neydi orada örnek resimler filan vereceğin zaman o resimleri Promtlar/Sources aç orada ürettiğin promt için kaynakları filanda saklarsın. böyle başlayalım.

## Cevap

**Çalışma şekli için yeni kurallar kaydedildi (hafıza):**
- Her onay isteğinde şunlar anlatılacak: son mesajdan beri ne yapıldı, sorun/sebep ne, seçim ne işe yarayacak, önerim ne, şimdiye kadar neye karar verdik.
- Kodlar asla tek başına yazılmayacak; her zaman açıklamasıyla birlikte verilecek (ör. "K1 = dosya başına tek tip").
- Özellik bitince: yapılanların genel özeti + yeni özellik önerileri.
- Ekran görüntüleri ve Promtlar kaynakları `Promtlar/Sources/` altına konacak.

**Yapılanlar:** `/new-feature` lite modda başladı → 1. aşama **ANALİZ**.
- Aynı konuda açık spec var mı diye bakıldı: yok. Numara **0001**.
- `specs/active/0001-walking-skeleton.md` yazıldı (Status: Draft): amaç, 9 gereksinim, ekran durumları, kısıtlar, 12 kabul kriteri.
- Hassas alan değil: kişisel veri, kimlik, para, veli iletişimi ve kiracı verisi yok. Bu yüzden lite modda netleştirme soruları ayrı sorulmadı; önerilerim taslağa işlendi.

**Spec'in özü — iskelet ne yapacak:**
- API'de **anonim "sistem durumu" sorgusu** olacak. Veritabanına ulaşabiliyorsa "çalışıyor", ulaşamıyorsa "hizmet dışı" döner. İç bilgi (sürüm, sunucu, hata ayrıntısı) sızdırmaz. Mesajlar Türkçe ve koda gömülü değil, kaynak dosyasından gelir.
- Web paneli ve mobil uygulama açılınca bu sorguyu **ortak istemci** üzerinden yapar ve 4 durumu gösterir: yükleniyor / çalışıyor / hizmet dışı / sunucuya ulaşılamıyor.
- Senin kod kuralların ilk günden otomatik zorlanır: **dosya başına tek tip**, **uyarı = hata**, **her tipin başında Türkçe açıklama**. Ayrıca **yasak bağımlılıklar** (ör. Domain katmanı hiçbir şeye bağımlı olamaz) mimari testlerle zorlanır.
- Test veritabanı her koşuda açılır ve silinir; geliştirme veritabanına dokunulmaz.
- `scripts/check` gerçekten çalışır hale gelir: hızlı ve tam profil, ve arkada süreç bırakmaz.

**Öz-eleştiri (bir geliştiricinin yine de soracağı boşluklar) ve önerilerim:**

| # | Boşluk | Önerim | Neden |
|---|---|---|---|
| 1 | "Hizmet dışı" hangi HTTP koduyla dönsün? | **503** (Service Unavailable) | Standart; izleme araçları da bunu anlar |
| 2 | Veritabanı kontrolü ne kadar beklesin? | **3 saniye**; aşarsa "hizmet dışı" | Ekran asılı kalmasın |
| 3 | Web/mobil API adresini nereden bilsin? | Ortam ayarından (`.env`); varsayılan `http://localhost:<port>` | Adres koda gömülmesin |
| 4 | Hata durumunda "Tekrar dene" düğmesi? | **Olsun** (web + mobil) | Kullanıcının uygulamayı kapatıp açması gerekmez; tek düğme, ucuz |
| 5 | Web başka bir adresten API'ye erişebilsin mi (CORS)? | Yalnızca ayarlarda tanımlı web adresine izin | Varsayılan reddet kuralı |
| 6 | Spec büyük mü (12 kriter, önerilen sınır 8–10)? | **Tek spec kalsın**; bölme önerisi kayıtlı | İskeletin amacı uçtan uca bağlantıyı tek seferde kanıtlamak; bölünürse yarım iskelet olur |
| 7 | Hızlı profil ne kadar sürmeli? | Kriter değil; plan/build'de 3 koşunun ortancası raporlanır | ANEW kuralı: süre ölçülür, kriter yapılmaz |

**Karar (tek):** Yukarıdaki 7 önerinin hepsini spec'e işleyip spec'i onaylıyor musun? Onaylarsan `Status: Approved` olur ve ikinci aşama **PLAN**'a geçilir. Planda paketler, dosya listesi ve test eşleşmesi gelir; kod yazılmaz.
