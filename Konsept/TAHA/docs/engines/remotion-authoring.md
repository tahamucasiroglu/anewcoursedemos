# Remotion sahne yazım sözleşmesi (remotion 4.0.543, React 19)

Bir sahne için **tek bir TSX dosyası** (`Scene.tsx`) üret. Başka açıklama veya Markdown kod çiti (```) yazma.
Sahne sessiz ve altyazısız render edilir; seslendirme, karaoke altyazı ve üst ilerleme çubuğu sonradan eklenir.
Boyut (`{W}×{H}`), fps ve süre (`{D}` sn) sistem tarafından `Root.tsx`'de ayarlanır; değerleri `useVideoConfig()` ile oku.

## İskelet
```tsx
import React from "react";
import { AbsoluteFill, Sequence, interpolate, spring, useCurrentFrame, useVideoConfig, Easing } from "remotion";

export default function Scene() {
  const frame = useCurrentFrame();
  const { fps, width, height, durationInFrames } = useVideoConfig();
  const enter = spring({ frame, fps, config: { damping: 200 } });
  const fade = interpolate(frame, [0, 0.5 * fps], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  return (
    <AbsoluteFill style={{ background: "#111", fontFamily: "Inter" }}>
      <div style={{ opacity: fade, transform: `translateY(${(1 - enter) * 60}px)`, color: "#fff", fontSize: 96, fontWeight: 800 }}>Başlık</div>
    </AbsoluteFill>
  );
}
```

## Zorunlu kurallar
1. `export default` ile **props almayan** tek bir bileşen. Yardımcı bileşenleri aynı dosyada tanımla.
2. `import` yalnızca `"react"` ve `"remotion"` modüllerinden. Başka paket, CSS dosyası veya göreli dosya yok.
3. Tüm hareket `useCurrentFrame()` üzerinden: `interpolate` (her zaman `extrapolateLeft/Right: "clamp"`), `spring`, `Easing`. Zamanlama karede: saniye × `fps`.
4. CSS `animation`/`transition`, `useEffect` zamanlayıcıları, `useState` ile animasyon, `setTimeout`, `requestAnimationFrame` YASAK.
5. Parçaları zamanlamak için `<Sequence from={kare} durationInFrames={kare}>` kullan (içeride `useCurrentFrame()` sıfırdan başlar).
6. **Ağ yok:** `fetch`, `http(s)://`, CDN, Google Fonts, `file://`, `import()` YASAK. `<Video>`, `<OffthreadVideo>`, `<Audio>`, `<IFrame>` kullanma.
7. **Deterministik:** `Math.random()`, `Date.now()`, `new Date()` YASAK; rastgelelik için `random("tohum")` (remotion) kullan.
8. Seslendirme altyazısı veya ilerleme çubuğu çizme.

## Yazı tipleri (önceden yüklü)
`fontFamily: "Inter"` (400/700/800), `"Montserrat"` (400/700/900), `"Playfair Display"` (400/700). Yedek `sans-serif`/`serif`. Başka font yok.

## Görseller
`import { Img, staticFile } from "remotion"` → `<Img src={staticFile("assets/<dosya-adı>")} style={{ width: "100%", height: "100%", objectFit: "cover" }} />`. Yalnızca verilen dosya adları. Görseller yazısızdır; metni üstüne koy.

## Süre, metin, güvenli alan
- Toplam süre `durationInFrames`; ilk hareket ilk 0.3 sn'de, son 0.4 sn son kare sabit (kararmaya gitme).
- Türkçe karakterleri doğrudan yaz; büyük harfi metne kendin yaz (`textTransform` kullanma). ~2.5 kelime/sn okunabilirlik.
- **Dikey 1080×1920:** önemli içerik x 80–1000, y 160–1380; üst 40 px ilerleme çubuğu, **alt %28 karaoke altyazı alanı** (metin koyma). Başlık 84–130 px, gövde ≥ 46 px.
- **Yatay 1920×1080:** içerik x 100–1820, y 70–820; alt %22 altyazı alanı. Başlık 72–110 px, gövde ≥ 38 px.

## Performans
`transform`/`opacity` ile animasyon; büyük `filter: blur()` ve `backdrop-filter` yok; ≤ 40 öğe. Ağır hesapları `useMemo` ile kareden bağımsız yap.

## Son kontrol
- [ ] Tek dosya, `export default`, yalnızca react/remotion importları
- [ ] Animasyon yalnızca kareye bağlı, `clamp` var
- [ ] Ağ, rastgelelik, video/ses yok; altyazı alanı boş
