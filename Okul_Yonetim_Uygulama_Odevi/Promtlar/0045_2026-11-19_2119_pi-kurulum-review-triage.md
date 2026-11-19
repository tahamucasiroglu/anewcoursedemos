# 0045 — Pi'de kurulum ve ilk dağıtım · Review round 1 · TRIAGE

**Tarih:** 2026-11-19 21:19

## İstek

> onaylıyorum

## Cevap

### Son mesajdan beri yapılanlar
1. **Pi'de 5 adım** (senin onayınla), hepsi başarılı:
   - nginx;
   - sistem kullanıcısı ve klasörler;
   - veritabanı: `okul_app` kullanıcısı (yönetici değil), `okul` veritabanı, `pg_hba` bloğu (önce yedek alındı), şifre dosyası (root, 600);
   - servis;
   - ilk dağıtım: sürüm `20261119180117-9dad0d6`, "çalışıyor".
2. **Kontroller:**
   - Tarayıcıdan `http://192.168.1.102:8080` → "Sistem çalışıyor". Derin bağlantı da açılıyor.
   - Ağdan 5080'e doğrudan bağlanılamıyor.
   - Kurulum öncesi ve sonrası karşılaştırıldı. Fark yalnızca bizim eklediklerimiz (+ apt'nin `packagekit`'i). Diğer servislerin, portların, veritabanların ve `pg_hba`'nın geri kalanı aynı.
3. **Tarih hatası düzeltildi** (`2edc593`): Son kayıtlarda yanlışlıkla 20 Kasım yazılmıştı. Bundan sonra saati sistemden alıyorum.
4. **Kontrol:** `CHECK GREEN (full, 12 steps)`; residue iki profilde temiz. İlk ölçümdeki "kalanlar" başka bir oturumun süreçleriydi.
5. **Bağımsız inceleme (hassas alan):**
   - Kritik ve yüksek bulgu yok.
   - İnceleyicinin özeti: "Çekirdek gizli bilgi akışı sağlam." Şifre üretimi, stdin ile verilmesi, dosya izni, rol yetkileri ve `pg_hba` doğrulaması doğru bulundu.
   - Bulgular: 2 orta, 5 düşük, 7 bilgi.

### TRIAGE — bulgular ve önerim
| # | Bulgu | Önem | Önerim |
|---|---|---|---|
| 1 | **Geçici dosyalar tahmin edilebilir yerde.** Kaldırma ve dağıtım sırasında root, `/tmp` altında adı önceden bilinen dosya ve klasörler kullanıyor. Pi'deki başka bir hesap (ör. komşu projelerin servis kullanıcıları) bu yolları önceden hazırlarsa, kaldırma sırasında ortak `pg_hba.conf`'a kendi satırını sokabilir. Bu, Pi'deki tüm veritabanlarını tehlikeye atar. | Orta | **Düzelt:** Yedek `/etc/okul` (yalnızca root) içine alınır. Dağıtım klasörü rastgele adla açılır ve root, kullanmadan önce sahibini ve bağlantı olmadığını denetler. |
| 2 | **Mobil kriterin (AC-10) planlanan kanıt yolu çalışmaz.** Expo'nun tarayıcı sürümü Pi'ye farklı bir adresten bağlandığı için tarayıcı isteği engeller (CORS). Telefondaki Expo Go bu engele takılmaz; kriter büyük olasılıkla sağlanıyor, sorun kanıtta. | Orta | **Kanıtı değiştir:** Mobilin kullandığı ortak istemci kodu Node'dan Pi'ye çağrılır; tarayıcı olmadığı için CORS yok, gözlemle gösterilir. Telefonda Expo Go denemesi isteğe bağlı olarak sende. Plan haritası güncellenir. Pi'ye geliştirme adresi için CORS izni **açılmaz**. |
| 3 | Otomatik geri dönüşte (dağıtım başarısız olunca) migration uyarısı verilmiyor; uyarı yalnızca elle geri almada var. | Düşük | **Düzelt:** Aynı uyarı otomatik geri dönüşte de verilsin. |
| 4 | Şifre içeren SQL komutu, PostgreSQL ayarına göre sunucu loguna düşebilir (ör. komut hata verirse). | Düşük | **Düzelt:** Bu oturumda komut loglaması kapatılır (tek satır). |
| 5 | Kaldırmada `pg_hba` bloğu silinirken bitiş işareti denetlenmiyor. İşaret bozulmuşsa diğer projelerin satırları da silinebilir. | Düşük | **Düzelt:** İki işaret tam bir kez ve aralarında beklenen 3 kural yoksa dosyaya dokunulmaz. |
| 6 | Elle geri almada bir birimin önceki sürümü yoksa yarım kalabiliyor; ayrıca eskimiş kilit geri almayı engelleyebiliyor. | Düşük | **Düzelt:** Önce hepsi kontrol edilsin, sonra çevrilsin. Eski kilit kuralı geri almada da geçerli olsun. |
| 7 | `pi-config`'in "şifre basılmıyor" denetimi dar: `cat /etc/okul/okul-api.env` gibi yolları yakalamıyor. | Düşük | **Düzelt:** Şifre dosyasını okuyan komutlar yasaklanır, aynı satırdaki ikinci komut ayrıca denetlenir. Kuralın sınırı belgeye yazılır. |
| 8 | Plan dışı ekler (A-1). Ayrıca kilit `flock` yerine klasörle yapılmış, `pg_hba` doğrulaması başka bir yöntemle yapılmış. İnceleyici ikisini de "mantıklı, eşdeğer" buldu. | Bilgi | **Onayla** (A-1'e eklenir). |
| 9 | Ölü kod (`NGINX_MARK`). Ayrıca servis dosyasındaki CORS satırı gereksiz: web aynı adresten çalışıyor. IP değişince bu satır eskide kalır. | Bilgi | **Kaldır** (sadelik; "IP değişince yalnızca `pi.conf`" kuralı doğru kalır). |
| 10 | İleride çerez veya oturum anahtarı kullanılırsa servise kalıcı bir klasör gerekecek. | Bilgi | **Backlog'a not** (giriş/kimlik spec'i). |
| 11 | Kılavuzda `uninstall.sh --onay KALDIR` seçeneği yazmıyor. | Bilgi | **Ekle.** |
| — | Diğer bilgi notları: kabuk ve SQL enjeksiyonu yolu yok; .NET bağlantı cümlesini loglamıyor; web `/` desteği temiz; geri kalan kanıtlar VERIFY'a ait. | Bilgi | Eylem yok. |

**Sonraki adım:**
- Düzeltme turu 1 yapılır.
- Hassas alan olduğu için düzeltmeler Pi'ye yeniden dağıtılır ve **yeniden bağımsız inceleme** yapılır.
- Ardından VERIFY, gerçek Pi'de:
  - süreç öldürme ve Pi'yi yeniden başlatma;
  - başka veritabanına bağlanma denemesi;
  - kilit ve disk eşiği;
  - bozuk sürümle otomatik geri dönüş;
  - yalnızca web veya yalnızca API dağıtımı;
  - ikinci kez kurulum;
  - onaysız kaldırma denemesi.
- Pi'yi yeniden başlatmadan önce ayrıca onayını isteyeceğim; diğer uygulamaların da kısa süre kapanır.

### Karar (tek)
Bu önerileri kabul ediyor musun?
