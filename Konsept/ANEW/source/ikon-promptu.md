# İkon promptu (B-6) — Claude Web için

Kullanım: aşağıdaki kod bloğunu Claude Web'e olduğu gibi yapıştır. Claude SVG kodu üretir; beğendiğin
SVG'leri bu klasöre (`source/`) şu adlarla kaydet: `app-icon.svg` (büyük), `tray-icon.svg` (küçük).
Gerekirse PNG/ICO dönüşümünü ben yaparım (web favicon, Avalonia tray, Windows .ico).

```
Design an original app icon for "ANEW Konsept" — a tool that turns video ideas ("concepts":
scene titles, on-screen visuals and narration scripts) into finished social-media videos.

Concept to express: an idea becoming a video. Combine, in ONE simple mark, a spark/lightbulb or
script-page idea with a play triangle or film frame. Keep it abstract and geometric, not clip-art.

Style:
- Warm "coffee shop" palette: primary orange #E8772E, deep coffee brown #4A2C1D, cream #FFF4E6,
  optional caramel accent #C8813A. No blue, no purple, no neon.
- Flat or very soft gradient, rounded corners, bold silhouette. No text, no letters except an
  optional stylised "A" monogram if it fits naturally.
- Must stay recognisable at 16×16 px (system tray) and look polished at 512×512 px.

Deliverables (as separate SVG code blocks, each with viewBox="0 0 512 512", no external fonts,
no embedded raster images):
1. app-icon.svg — full icon on a rounded-square cream background.
2. app-icon-dark.svg — same mark on a deep coffee brown background (for dark mode).
3. tray-icon.svg — simplified version for 16–32 px: thicker strokes, fewer details,
   transparent background, readable on both light and dark Windows taskbars.

Show me 3 different design directions first (small previews), then produce the deliverables
for the direction I pick.
```
