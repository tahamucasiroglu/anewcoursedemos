# 0053 — Son incelemenin 3 bilgi notu, açık anlatımla

**Tarih:** 2026-11-19 23:16

## İstek

> dedim sana maddeleri sadece söyleme ne olduğunu yaz t13 ne mesela

## Cevap

Haklısın, kuralı çiğnedim: yalnızca "T13" dedim, ne olduğunu yazmadım. Hafızadaki proje notuna bunu daha açık yazdım. Her karar mesajında maddenin ne olduğunu yeniden anlatacağım.

### Önce: "T13" nedir?
`docs/conventions.md` dosyasında **"Bilinen tuzaklar" (Known traps)** diye bir liste var. Bu projede bir kez yaşanmış ya da kolayca yaşanabilecek, kodu yazarken dikkat edilmesi gereken hatalar burada numarayla tutuluyor (T1 … T13). Amaç, aynı hatanın ikinci kez yapılmaması. Örnek: T1 "Türkçe I/ı harf dönüşümü", T12 "Pi'de PostgreSQL 17 var, geliştirmede 18".

**T13, bu feature'da az önce yaşadığımız hata.** Şöyle diyor:
> Windows'taki Git Bash, bir programa (node, npx, dotnet) giden ayarlarda `/` ile başlayan değeri dosya yolu sanıp çeviriyor. Web'e "API adresi sayfanın kendi adresi" demek için verdiğimiz `VITE_API_BASE_URL=/` değeri bu yüzden pakete `C:/Program Files/Git/` olarak gömüldü ve Pi'deki sayfa API'ye ulaşamadı.
> Çözüm: böyle bir değer `MSYS2_ENV_CONV_EXCL=<değişken adı>` ile verilir. Dağıtımda derlenen dosyalarda yerel bir yol (`C:/…`, `file:`) arayan bir koruma bulunur. Not: curl ile yapılan sağlık kontrolü bunu yakalamaz; sayfanın kendi isteği tarayıcıda gözlenmeli.

### Bilgi notu 1 — T13'teki koruma tarifi, koruma kodundan geniş
- **Ne:** T13 korumayı "yerel yol (`C:/…`, `file:`) arar" diye anlatıyor. Dağıtım betiğindeki koruma ise yalnızca bilinen yerleri arıyor: `X:/Program Files`, `X:/Users`, `X:/msys`, `X:/Windows` ve `file:///`.
- **Sonuç:** Git örneğin `D:/Git/` gibi başka bir yere kurulmuş olsaydı koruma bunu yakalamazdı. "Herhangi bir `X:/`" diye aramak da olmuyor, çünkü normal `http://` adreslerindeki `p:/` yanlış alarm veriyor.
- **Neden sorun değil:** Asıl düzeltme (çevirinin kapatılması) dağıtıma zorunlu olarak eklendi ve otomatik kontrol onu denetliyor. Koruma yalnızca ikinci bir emniyet.
- **Önerim:** Yalnızca belgeyi düzeltmek. T13'teki ifade "derlenen dosyalarda **bilinen** yerel yolları (Git'in kurulu olduğu klasör, `Users`, `Windows`, `file:`) arayan koruma" olur. Kod değişmez.

### Bilgi notu 2 — Plandaki analizde eski bir öneri kalmış
- **Ne:** Planda, dördüncü düzeltme turundan önce yazdığım kök neden analizi bölümünde çözüm "`MSYS2_ENV_CONV_EXCL` **+ `MSYS_NO_PATHCONV=1`**" diye geçiyor.
- **Uygulamada ne oldu:** İkinci ayarı (`MSYS_NO_PATHCONV`) ilk denemede kullandım. Bu ayar web derlemesinin çıktı klasörünün yolunu da çevrilmez hâle getirdi ve derleme yanlış klasöre (`C:\tmp`) yazıldı. Bu yüzden onu bilerek çıkardım; otomatik kontrol artık bu ayarı yasaklıyor.
- **Sonuç:** Planı okuyan kişi, önerilen ile yapılan arasında çelişki görür.
- **Önerim:** O satırın yanına tek cümlelik bir not eklemek: "`MSYS_NO_PATHCONV` uygulamada bilerek kullanılmadı; çıktı klasörü yolunu da bozuyor (bkz. 4. düzeltme turu kanıtı)." Kod değişmez.

### Bilgi notu 3 — Koruma, arama komutu hata verirse "temiz" sayıyor
- **Ne:** Koruma `grep` komutuyla arama yapıyor. `grep` üç şekilde biter: buldu (0), bulamadı (1) ya da hata (2; örneğin klasör yok). Koruma yalnızca "buldu" durumunda dağıtımı durduruyor. Yani hata durumu "temiz" sayılıyor.
- **Neden sorun değil:** Hemen bir önceki satır, derleme klasörünün ve `index.html` dosyasının var olduğunu zaten kontrol ediyor; yoksa dağıtım orada duruyor. Bu yüzden `grep`'in hata vermesi pratikte olmuyor.
- **Önerim:** Bir şey yapmamak.

### Ardından
- Bu iki belge düzeltmesi yapılır. Kod değişmediği için yeniden inceleme gerekmez.
- Sonra **yayın (SHIP) onayı** istenir: spec 0003 "Raspberry Pi kurulumu" tamamlanmış sayılır; özet ve sonraki feature önerileri gelir.

### Karar (tek)
Bu üç not için önerilerimi kabul ediyor musun? (İki belge düzeltmesi + üçüncüde eylem yok)
