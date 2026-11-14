# HyperFrames sahne yazım sözleşmesi (hyperframes 0.9.43 + GSAP 3.15)

Bir sahne için **tek, eksiksiz bir HTML belgesi** üret. Başka açıklama, Markdown kod çiti (```) veya ek dosya yazma.
Sahne sessiz ve altyazısız render edilir; seslendirme, karaoke altyazı ve üst ilerleme çubuğu sonradan eklenir.

## Sabit iskelet
Değişkenler: `{W}` genişlik, `{H}` yükseklik, `{D}` sahne süresi (sn). Bunları verilen sayılarla doldur.

```html
<!doctype html>
<html lang="tr">
<head>
<meta charset="UTF-8" />
<link rel="stylesheet" href="fonts/tk-fonts.css" />
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  html, body { width: {W}px; height: {H}px; overflow: hidden; background: #111; }
  #root { position: relative; width: 100%; height: 100%; font-family: 'Inter', sans-serif; }
  .clip { position: absolute; }
</style>
</head>
<body>
<div id="root" data-composition-id="main" data-start="0" data-duration="{D}" data-width="{W}" data-height="{H}">
  <div id="bg" class="clip" data-start="0" data-duration="{D}" style="inset:0"></div>
  <h1 id="title" class="clip" data-start="0.2" data-duration="3">Başlık</h1>
</div>
<script src="gsap.min.js"></script>
<script>
  window.__timelines = window.__timelines || {};
  const tl = gsap.timeline({ paused: true });
  tl.fromTo("#title", { opacity: 0, y: 60 }, { opacity: 1, y: 0, duration: 0.6, ease: "power3.out" }, 0.2);
  window.__timelines["main"] = tl;
</script>
</body>
</html>
```

## Zorunlu kurallar
1. `<html lang="tr">` ve `<meta charset="UTF-8">`. Türkçe karakterleri (ç ğ ı İ ö ş ü) doğrudan yaz; büyük harfi metne kendin yaz (CSS `text-transform` kullanma).
2. Kök öğe: `id="root"`, `data-composition-id="main"`, `data-start="0"`, `data-duration="{D}"`, `data-width="{W}"`, `data-height="{H}"`. Kök boyutu sistem tarafından zorlanır.
3. Zamanlı her görünür öğe: benzersiz `id`, `class="clip"`, `data-start` ve `data-duration` (saniye; `data-start + data-duration ≤ {D}`). Öğe yalnızca bu aralıkta görünür.
4. Tek bir **duraklatılmış** GSAP zaman çizelgesi: `gsap.timeline({ paused: true })`, `window.__timelines["main"]` olarak kaydedilir. Konum parametresi (3. argüman) mutlak saniyedir: `tl.to(el, vars, 1.5)`. Yalnızca `set`, `to`, `from`, `fromTo`. `play()`, `seek()`, `repeat: -1`, `yoyo` sonsuz döngü kullanma.
5. Tüm hareket zaman çizelgesinden: CSS `animation`/`transition`, `setTimeout`, `setInterval`, `requestAnimationFrame`, `<canvas>` çizim döngüsü YASAK (render kare kare ileri sarar).
6. Güvenilir animasyon özellikleri: `opacity, x, y, scale, scaleX, scaleY, rotation, width, height, visibility`. Renk/gölge değişimleri için ayrı katmanların `opacity` değerini değiştir.
7. **Ağ yok:** `fetch`, `XMLHttpRequest`, `WebSocket`, `import()`, `http(s)://` adresleri, CDN, Google Fonts, `@import url`, `file://` YASAK. Tek betik `gsap.min.js` (yerel). Başka kütüphane yok.
8. **Deterministik:** `Math.random()`, `Date.now()`, `new Date()` YASAK. Rastgelelik gerekiyorsa sabit tohumlu üretici yaz: `let s=7; const rnd=()=>(s=(s*16807)%2147483647)/2147483647;`.
9. `<audio>`, `<video>`, `<iframe>` kullanma. Seslendirme altyazısı veya ilerleme çubuğu çizme (sonradan eklenir).
10. `eval`, `new Function`, `process`, `require` yok.

## Yazı tipleri (yalnızca bunlar, yerel)
- `'Inter'` 400/700/800 — gövde, sade başlık
- `'Montserrat'` 400/700/900 — güçlü başlık
- `'Playfair Display'` 400/700 — tarih/klasik tema
Yedek: `sans-serif` / `serif`. Başka font adı yazma.

## Görseller
Verilen görsellere yalnızca `assets/<dosya-adı>` ile başvur (`<img src="assets/sahne1.png">` ya da `background-image: url('assets/sahne1.png')`). Listede olmayan dosya kullanma. Görseller yazısızdır; metni HTML ile üstüne koy. Kaplama için `object-fit: cover`. Ken Burns için `scale` 1→1.08 yavaş tween.

## Süre ve ritim
- Sahne tam `{D}` sn. İlk hareket 0.0–0.3 sn içinde başlasın; son 0.4 sn son kare sabit kalsın (kararmaya gitme, sahneler art arda birleşir).
- Metni okunacak kadar ekranda tut: kabaca 2.5 kelime/sn. Bir anda en çok 2 metin bloğu.
- Giriş 0.4–0.8 sn, `power2/power3.out`; çıkış istenirse 0.3–0.5 sn.

## Güvenli alanlar
- **Dikey 1080×1920:** önemli içerik x 80–1000, y 160–1380 aralığında. Üst 40 px ilerleme çubuğuna, **alt %28 (y > 1380) karaoke altyazıya** ayrılmıştır — oraya metin koyma (arka plan/görsel olabilir). Başlık 84–130 px, gövde ≥ 46 px, satır başına ≤ 22 karakter.
- **Yatay 1920×1080:** önemli içerik x 100–1820, y 70–820. Alt %22 altyazı alanı. Başlık 72–110 px, gövde ≥ 38 px.
- Yüksek kontrast (açık zemin + koyu yazı ya da tersi); ince yazıyı düz renkli zemine koy.

## Performans
- ≤ 40 zamanlı öğe, ≤ 80 tween. Büyük `filter: blur()` ve `backdrop-filter` kullanma; çok katmanlı `box-shadow` yerine tek gölge.
- Dönüşümleri `transform` tabanlı özelliklerle (x, y, scale, rotation) yap; layout değiştiren `top/left` tween'lerinden kaçın.
- Basit şekiller için CSS/SVG (inline `<svg>`; `xmlns="http://www.w3.org/2000/svg"` serbest).

## Son kontrol
- [ ] Tek HTML belgesi, `lang="tr"`, kök öznitelikleri tam
- [ ] Her zamanlı öğede id + class="clip" + data-start + data-duration
- [ ] `window.__timelines["main"]` kayıtlı, timeline paused
- [ ] Ağ, rastgelelik, CSS animasyonu yok; tek betik `gsap.min.js`
- [ ] Altyazı alanı boş, metinler güvenli alanda, Türkçe karakterler doğru
