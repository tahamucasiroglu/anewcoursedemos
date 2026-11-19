# 0036 — Plan onayı (sadeleştirmeyle) · BUILD · Review round 1 · TRIAGE

**Tarih:** 2026-11-19 21:40

## İstek

> uygun gibi bu kadar kapsamlı tema için düşünmeye gerek var mı emin değilim. bir temna kuralı olsa her birine ayrı ayrı eklesekte olur. mobil ve web react aslında tek kalıpta yeter. sen halledersin uygun

## Cevap

### Son mesajdan beri yapılanlar
1. **Plan sadeleştirildi ve onaylandı** (`3c3af80`).
   - Web'deki Vite eklentisi kaldırıldı. Kayıtlı tema, `index.html`'e doğrudan yazılmış küçük bir betikle uygulanıyor.
   - Renkler düz CSS'te tutuluyor; ortak renk dosyasıyla aynı olduklarını bir test denetliyor.
   - Web ve mobil aynı kalıbı kullanıyor: ortak renk dosyası + seçici + saklama.
2. **BUILD:** 5 adım, ayrı commit'ler.
   - Tema paketi (`ae93dae`): renkler, seçim mantığı ve kontrast testi.
   - Web, açılışta renk parlaması olmaması (`2188e41`).
   - Web seçicisi (`e0dde20`).
   - Mobil (`b62e712`): telefonda saklama, durum çubuğu, ekran okuyucu.
   - Belgeler (`07ca5a5`).
3. **Kontrol:** `CHECK GREEN (full, 11 steps, 127 s)`.
   - Testler: tema 42, web 75, mobil 32.
   - Kontrol sonrası arkada çalışan süreç kalmıyor; iki profilde de doğrulandı.
4. **Bağımsız inceleme (Review round 1):** Kritik ve yüksek bulgu yok. 1 orta, 2 düşük, 4 bilgi bulgusu var.
   - İnceleyicinin değerlendirmesi: "Kullanıcının istediği sadeleştirme uygulanmış, gereksiz karmaşıklık yok."
   - Web'deki betik ile ortak mantığın tutarlı olduğu ve güvenlik açısından sorun olmadığı da doğrulandı.

### TRIAGE — bulgular ve önerim
| # | Bulgu | Önem | Önerim |
|---|---|---|---|
| 1 | **Web seçicide klavye odak çerçevesi kırpılıyor.** Seçicinin kutusu kenarları taşırmıyor (`overflow: hidden`). Bu yüzden Tab ile gelinen düğmenin çerçevesinin büyük kısmı görünmüyor. Spec "odak çerçevesi görünür" diyor. | Orta | **Düzelt:** Taşma kırpmasını kaldır, köşe yuvarlamasını ilk ve son düğmeye ver. Doğrulamada her üç seçenek için odak ekran görüntüsü alınır. |
| 2 | **Mobilde "500 ms'de okuma bitmezse Sistem" yolu test edilmiyor.** Bu yol bozulursa uygulama boş ekranda kalır ve hiçbir test fark etmez. | Düşük | **Düzelt:** Hiç bitmeyen okuma + 500 ms ileri sarma testi. Geç gelen sonucun seçimi ezmediği de sınanır. |
| 3 | **Gerçek Android derlemesinde** cihaz modunu okumak için ek bir Expo modülü (`expo-system-ui`) gerekebilir. Açılışta en çok 500 ms'lik açık bir kare görünebilir. Expo Go ve web'de sorun yok; bugün Android derlememiz de yok. | Düşük | **Ertele** (backlog B-12): İlk Android derleme işinde modül kurulur ve açılış zemini temadan ayarlanır. |
| 4 | Kontrast listesinde iki çift eksik: durum kartının yeşil ve kırmızı kenarının dış zeminle kontrastı. Bugünkü değerler eşiği geçiyor (4.79 ve 6.18). | Bilgi | **Ekle:** iki satır; böylece bu kenarlar da otomatik korunur. |
| 5 | Lint renk yasağı "white", "red" gibi adla yazılmış renkleri yakalamıyor. Kod bugün temiz. | Bilgi | **Belgeye not:** "adla yazılan renk de yasak". Lint'i genişletmeye gerek yok. |
| 6 | Tuzak listesinde T11, T10'un önüne yazılmış. | Bilgi | **Düzelt** (kozmetik). |
| 7 | Plan dışı küçük ekler (A-1): tema testi ayarı, web test ayarında CSS'in okunabilmesi, CSS'in bağlanması, durum çubuğunun tema bileşenine taşınması. | Bilgi | **Onayla.** İnceleyici de "gerekçeli ve küçük" dedi. |

**Sonra ne olacak:**
- Düzeltme turu 1 yapılır, her bulgu ayrı commit.
- Orta seviyede bir bulgu olduğu için **yeniden bağımsız inceleme** yapılır.
- Ardından VERIFY: kasıtlı bozma (kontrast eşiği) ve ekran görüntüleri. Görüntüler: 2 mod × 4 durum × web ve mobil, 320 px görünümü, odak çerçevesi.

### Karar (tek)
Bu 7 öneriyi kabul ediyor musun?
