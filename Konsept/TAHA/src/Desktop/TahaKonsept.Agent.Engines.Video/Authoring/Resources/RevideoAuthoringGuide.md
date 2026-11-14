# Revideo sahne yazım sözleşmesi (@revideo 0.11.0, deneysel)

Bir sahne için **tek bir TSX dosyası** (`src/scene.tsx`) üret. Başka açıklama veya Markdown kod çiti (```) yazma.
Sahne sessiz ve altyazısız render edilir; seslendirme, karaoke altyazı ve üst ilerleme çubuğu sonradan eklenir.
Boyut (`{W}×{H}`) ve fps sistem tarafından `project.ts`'de ayarlanır. Süre, generator'daki `yield*` adımlarının toplamıdır ve **tam `{D}` sn olmalı**.

## İskelet
```tsx
import { makeScene2D, Rect, Txt, Img } from '@revideo/2d';
import { all, createRef, waitFor, easeOutCubic } from '@revideo/core';

export default makeScene2D('scene', function* (view) {
  const title = createRef<Txt>();
  view.add(<Rect width={'100%'} height={'100%'} fill={'#111111'} />);
  view.add(
    <Txt ref={title} text={'Başlık'} fontFamily={'Inter'} fontWeight={800} fontSize={96}
         fill={'#ffffff'} y={-300} opacity={0} textWrap={true} width={900} textAlign={'center'} />,
  );
  yield* all(title().opacity(1, 0.6, easeOutCubic), title().y(-340, 0.6, easeOutCubic)); // 0.6 sn
  yield* waitFor(2.4);                                                                     // toplam 3.0 sn
});
```

## Zorunlu kurallar
1. `export default makeScene2D('scene', function* (view) { ... })`. Yardımcıları aynı dosyada tanımla.
2. `import` yalnızca `'@revideo/2d'` ve `'@revideo/core'`. Başka paket, CSS veya göreli dosya yok.
3. **Koordinat merkezi ortadır:** (0,0) ekran ortası; dikeyde x ∈ [-540, 540], y ∈ [-960, 960]; yatayda x ∈ [-960, 960], y ∈ [-540, 540]. y aşağı doğru artar.
4. Bileşenler: `Rect, Circle, Txt, Img, Layout, Line, Node`. Animasyon sinyallerle: `node().opacity(1, 0.5)`, `all(...)`, `sequence(gecikme, ...)`, `chain(...)`, `waitFor(sn)`; easing'ler `@revideo/core`'dan (`easeOutCubic`, `easeInOutCubic`, `linear`...).
5. Toplam süre: tüm `yield*` sürelerinin toplamı `{D}` olmalı; sonda `yield* waitFor(kalan)` ile tamamla. Son 0.4 sn son kare sabit kalsın.
6. **Ağ yok:** `fetch`, `http(s)://`, CDN, Google Fonts, `file://`, `import()` YASAK. `Video`, `Audio` bileşenlerini kullanma.
7. **Deterministik:** `Math.random()`, `Date.now()` YASAK; gerekiyorsa `useRandom(tohum)` (@revideo/core).
8. Seslendirme altyazısı veya ilerleme çubuğu çizme.

## Yazı tipleri (yüklü)
`fontFamily={'Inter'}` (400/700/800), `'Montserrat'` (400/700/900), `'Playfair Display'` (400/700). Başka font yok.

## Görseller
`<Img src={'/assets/<dosya-adı>'} width={1080} height={1920} />` — yalnızca verilen dosya adları. Görseller yazısızdır; metni üstüne koy.

## Metin ve güvenli alan
- Türkçe karakterleri doğrudan yaz; büyük harfi kendin yaz. Uzun metinde `textWrap={true}` + `width`. ~2.5 kelime/sn.
- **Dikey 1080×1920:** önemli içerik x ∈ [-460, 460], y ∈ [-800, 420]; üst 40 px ilerleme çubuğu, **y > 420 karaoke altyazı alanı** (metin koyma). Başlık 84–130, gövde ≥ 46.
- **Yatay 1920×1080:** içerik x ∈ [-860, 860], y ∈ [-470, 280]; y > 280 altyazı alanı. Başlık 72–110, gövde ≥ 38.

## Son kontrol
- [ ] `export default makeScene2D(...)`, yalnızca @revideo/2d ve @revideo/core importları
- [ ] Toplam süre tam `{D}` sn
- [ ] Ağ, rastgelelik, video/ses yok; altyazı alanı boş
