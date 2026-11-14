/**
 * Sahne kodu üreticileri (mock LLM) — HyperFrames (HTML+GSAP), Remotion (TSX) ve Revideo (TSX).
 *
 * Girdi, Production SceneCodeStageHandler'ın gerçek promptlarıdır:
 *   sistem promptu  = motorun yazım kılavuzu ("# HyperFrames|Remotion|Revideo sahne yazım sözleşmesi …", {W}/{H} doldurulmuş)
 *   kullanıcı promptu: "Video: \"…\" — 1080x1920, dil: tr" · "Sahne N — süre {D} = 7.2 sn" · "Başlık: …" · "Ekranda gösterilecek: …"
 *                      · "Kullanılabilir görseller (yalnızca bunlar): assets/a.png, …" ya da "Görsel dosyası yok: …"
 * Çıktı her motorun kılavuzuna ve ajan lint'ine uyar: başlık + ekran metni (güvenli alanda, alt %28 altyazı alanı boş),
 * görsel verilmişse tam ekran kaplama + yavaş Ken Burns + okunabilirlik katmanı, yoksa degrade + dekoratif şekiller.
 * Tüm hareket kareye/zaman çizelgesine bağlı; son 0,4 sn sabit. Rastgelelik/ağ yok.
 */
'use strict';

const PALETTES = [
  { a: '#e2711d', b: '#3d2412', c: '#ffe8cc', ink: '#2b1608' },
  { a: '#c2873a', b: '#1f2a44', c: '#f3e3c3', ink: '#1b1d2b' },
  { a: '#d9a441', b: '#2b1d14', c: '#fff1d6', ink: '#2b1d14' },
  { a: '#b5523b', b: '#1d1a24', c: '#ffe2d6', ink: '#24120c' },
];

function parse(systemPrompt, userPrompt) {
  const sys = String(systemPrompt || '');
  const usr = String(userPrompt || '');
  const engine = /#\s*Remotion sahne/i.test(sys) ? 'remotion' : /#\s*Revideo sahne/i.test(sys) ? 'revideo' : 'hyperframes';
  const size = /—\s*(\d+)x(\d+)/.exec(usr) || /width:\s*(\d+)px;\s*height:\s*(\d+)px/.exec(sys) || [0, 1080, 1920];
  const W = Number(size[1]);
  const H = Number(size[2]);
  const dur = /süre \{D\} = ([\d.]+) sn/.exec(usr);
  const D = dur ? Number(dur[1]) : 5;
  const fpsM = /Kare hızı (\d+) fps/.exec(sys);
  const sceneNo = Number((/Sahne (\d+)/.exec(usr) || [0, '1'])[1]);
  const onScreen = ((/Ekranda gösterilecek: (.*)/.exec(usr) || [0, ''])[1] || '').trim() || 'Taha Konsept';
  const title = ((/Başlık: (.*)/.exec(usr) || [0, ''])[1] || '').trim();
  const images = /Kullanılabilir görseller \(yalnızca bunlar\): (.*)/.exec(usr);
  const image = images ? images[1].split(',')[0].trim() : null; // "assets/x.png"
  return {
    engine, W, H, D, fps: fpsM ? Number(fpsM[1]) : 30, sceneNo, onScreen, title, image,
    vertical: H >= W, pal: PALETTES[(sceneNo - 1 + PALETTES.length) % PALETTES.length],
  };
}

const htmlEsc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const jsStr = s => JSON.stringify(String(s));
const f2 = n => (Math.round(n * 100) / 100).toFixed(2);
const pad2 = n => String(n).padStart(2, '0');

/* ------------------------------------------------------------------ HyperFrames */
function hyperframes(p) {
  const { W, H, D, pal, vertical } = p;
  const hold = Math.max(0.6, D - 0.4); // son 0,4 sn sabit
  const side = vertical ? 80 : 100;
  const chipTop = vertical ? 180 : 90;
  const titleTop = Math.round(H * (vertical ? 0.27 : 0.24));
  const cardTop = Math.round(H * (vertical ? 0.47 : 0.50));
  const ring = Math.round(Math.min(W, H) * 0.62);
  const titleSize = vertical ? 104 : 86;
  const cardSize = vertical ? 76 : 60;
  const img = p.image
    ? `\n  <img id="photo" class="clip" data-start="0" data-duration="${f2(D)}" src="${htmlEsc(p.image)}" alt="" style="left:0;top:0;width:${W}px;height:${H}px;object-fit:cover" />
  <div id="shade" class="clip" data-start="0" data-duration="${f2(D)}" style="left:0;top:0;width:${W}px;height:${H}px;background:linear-gradient(180deg, rgba(10,6,3,0.72) 0%, rgba(10,6,3,0.25) 45%, rgba(10,6,3,0.80) 100%)"></div>`
    : '';
  const dots = [0, 1, 2, 3, 4, 5].map(i => {
    let s = 7 + i * 31 + p.sceneNo * 13;
    const rnd = () => (s = (s * 16807) % 2147483647) / 2147483647;
    const x = Math.round(side + rnd() * (W - 2 * side - 40));
    const y = Math.round(H * 0.12 + rnd() * H * 0.55);
    const r = Math.round(14 + rnd() * 26);
    return { id: `dot${i}`, x, y, r };
  });
  const dotHtml = p.image ? '' : dots.map(d => `\n  <div id="${d.id}" class="clip dot" data-start="0" data-duration="${f2(D)}" style="left:${d.x}px;top:${d.y}px;width:${d.r}px;height:${d.r}px"></div>`).join('');
  const dotTl = p.image ? '' : dots.map((d, i) => `\n  tl.fromTo("#${d.id}", { opacity: 0, y: 40 }, { opacity: 0.55, y: -${20 + i * 8}, duration: ${f2(Math.max(0.6, hold - 0.2 - i * 0.1))}, ease: "sine.out" }, ${f2(0.1 + i * 0.1)});`).join('');
  return `<!doctype html>
<html lang="tr">
<head>
<meta charset="UTF-8" />
<link rel="stylesheet" href="fonts/tk-fonts.css" />
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  html, body { width: ${W}px; height: ${H}px; overflow: hidden; background: #111; }
  #root { position: relative; width: 100%; height: 100%; font-family: 'Inter', sans-serif; }
  .clip { position: absolute; }
  #bg { left: 0; top: 0; width: ${W}px; height: ${H}px; background: radial-gradient(circle at 50% 30%, ${pal.a} 0%, ${pal.b} 72%); }
  #ring { left: ${Math.round(W / 2 - ring / 2)}px; top: ${Math.round(H * (vertical ? 0.16 : 0.10))}px; width: ${ring}px; height: ${ring}px; border-radius: 50%; border: 10px solid rgba(255,255,255,0.18); }
  .dot { border-radius: 50%; background: ${pal.c}; opacity: 0; }
  #chip { left: ${side}px; top: ${chipTop}px; padding: 14px 28px; border-radius: 999px; background: rgba(0,0,0,0.45); color: ${pal.c}; font-family: 'Montserrat', sans-serif; font-weight: 700; font-size: ${vertical ? 38 : 32}px; letter-spacing: 4px; opacity: 0; }
  #rule { left: ${side}px; top: ${titleTop - 34}px; width: 220px; height: 10px; border-radius: 5px; background: ${pal.a}; transform-origin: left center; }
  #title { left: ${side}px; right: ${side}px; top: ${titleTop}px; color: #fff; font-family: 'Playfair Display', serif; font-weight: 700; font-size: ${titleSize}px; line-height: 1.08; text-shadow: 0 6px 24px rgba(0,0,0,0.45); opacity: 0; }
  #card { left: ${side}px; right: ${side}px; top: ${cardTop}px; padding: 36px 44px; border-radius: 28px; background: ${pal.c}; color: ${pal.ink}; font-family: 'Montserrat', sans-serif; font-weight: 900; font-size: ${cardSize}px; line-height: 1.12; box-shadow: 0 18px 48px rgba(0,0,0,0.35); opacity: 0; }
</style>
</head>
<body>
<div id="root" data-composition-id="main" data-start="0" data-duration="${f2(D)}" data-width="${W}" data-height="${H}">
  <div id="bg" class="clip" data-start="0" data-duration="${f2(D)}"></div>${img}
  <div id="ring" class="clip" data-start="0" data-duration="${f2(D)}"></div>${dotHtml}
  <div id="chip" class="clip" data-start="0" data-duration="${f2(D)}">SAHNE ${pad2(p.sceneNo)}</div>
  <div id="rule" class="clip" data-start="0" data-duration="${f2(D)}"></div>
  <h1 id="title" class="clip" data-start="0" data-duration="${f2(D)}">${htmlEsc(p.title || p.onScreen)}</h1>
  <div id="card" class="clip" data-start="0" data-duration="${f2(D)}">${htmlEsc(p.onScreen)}</div>
</div>
<script src="gsap.min.js"></script>
<script>
  window.__timelines = window.__timelines || {};
  const tl = gsap.timeline({ paused: true });${p.image ? `
  tl.fromTo("#photo", { scale: 1.0 }, { scale: 1.08, duration: ${f2(hold)}, ease: "none" }, 0);` : ''}
  tl.fromTo("#ring", { scale: 0.7, opacity: 0 }, { scale: 1, opacity: 1, duration: 0.8, ease: "power2.out" }, 0);
  tl.to("#ring", { rotation: 40, duration: ${f2(Math.max(0.2, hold - 0.8))}, ease: "none" }, 0.8);${dotTl}
  tl.fromTo("#chip", { opacity: 0, x: -40 }, { opacity: 1, x: 0, duration: 0.5, ease: "power3.out" }, 0.05);
  tl.fromTo("#rule", { scaleX: 0 }, { scaleX: 1, duration: 0.6, ease: "power3.out" }, 0.15);
  tl.fromTo("#title", { opacity: 0, y: 60 }, { opacity: 1, y: 0, duration: 0.7, ease: "power3.out" }, 0.2);
  tl.fromTo("#card", { opacity: 0, y: 50, scale: 0.94 }, { opacity: 1, y: 0, scale: 1, duration: 0.6, ease: "back.out(1.6)" }, ${f2(Math.min(0.9, D / 4))});
  window.__timelines["main"] = tl;
</script>
</body>
</html>`;
}

/* ------------------------------------------------------------------ Remotion */
function remotion(p) {
  const { pal, vertical } = p;
  const side = vertical ? 80 : 100;
  const asset = p.image ? p.image.replace(/^\/+/, '') : null; // staticFile("assets/x.png")
  return `import React from "react";
import { AbsoluteFill, Img, staticFile, interpolate, spring, useCurrentFrame, useVideoConfig, Easing } from "remotion";

const TITLE = ${jsStr(p.title || p.onScreen)};
const ON_SCREEN = ${jsStr(p.onScreen)};
const CHIP = ${jsStr('SAHNE ' + pad2(p.sceneNo))};
const IMAGE: string | null = ${asset ? jsStr(asset) : 'null'};

const clamp = { extrapolateLeft: "clamp" as const, extrapolateRight: "clamp" as const };

export default function Scene() {
  const frame = useCurrentFrame();
  const { fps, width, height, durationInFrames } = useVideoConfig();
  const hold = Math.max(1, durationInFrames - Math.round(0.4 * fps));
  const at = (sec: number) => Math.round(sec * fps);
  const chipIn = interpolate(frame, [at(0.05), at(0.55)], [0, 1], { ...clamp, easing: Easing.out(Easing.cubic) });
  const rule = interpolate(frame, [at(0.15), at(0.75)], [0, 1], { ...clamp, easing: Easing.out(Easing.cubic) });
  const titleIn = spring({ frame: frame - at(0.2), fps, config: { damping: 200 } });
  const cardIn = spring({ frame: frame - at(Math.min(0.9, durationInFrames / fps / 4)), fps, config: { damping: 14, stiffness: 120 } });
  const ken = interpolate(frame, [0, hold], [1, 1.08], clamp);
  const ringScale = interpolate(frame, [0, at(0.8)], [0.7, 1], { ...clamp, easing: Easing.out(Easing.quad) });
  const ringRot = interpolate(frame, [at(0.8), hold], [0, 40], clamp);
  const ring = Math.round(Math.min(width, height) * 0.62);
  const titleTop = Math.round(height * ${vertical ? 0.27 : 0.24});
  const cardTop = Math.round(height * ${vertical ? 0.47 : 0.5});
  return (
    <AbsoluteFill style={{ background: "radial-gradient(circle at 50% 30%, ${pal.a} 0%, ${pal.b} 72%)", fontFamily: "Inter, sans-serif" }}>
      {IMAGE ? (
        <AbsoluteFill style={{ transform: \`scale(\${ken})\` }}>
          <Img src={staticFile(IMAGE)} style={{ width: "100%", height: "100%", objectFit: "cover" }} />
        </AbsoluteFill>
      ) : null}
      {IMAGE ? (
        <AbsoluteFill style={{ background: "linear-gradient(180deg, rgba(10,6,3,0.72) 0%, rgba(10,6,3,0.25) 45%, rgba(10,6,3,0.8) 100%)" }} />
      ) : null}
      <div style={{ position: "absolute", left: width / 2 - ring / 2, top: Math.round(height * ${vertical ? 0.16 : 0.1}), width: ring, height: ring, borderRadius: "50%",
        border: "10px solid rgba(255,255,255,0.18)", opacity: interpolate(frame, [0, at(0.8)], [0, 1], clamp),
        transform: \`scale(\${ringScale}) rotate(\${ringRot}deg)\` }} />
      <div style={{ position: "absolute", left: ${side}, top: ${vertical ? 180 : 90}, padding: "14px 28px", borderRadius: 999, background: "rgba(0,0,0,0.45)",
        color: "${pal.c}", fontFamily: "Montserrat, sans-serif", fontWeight: 700, fontSize: ${vertical ? 38 : 32}, letterSpacing: 4,
        opacity: chipIn, transform: \`translateX(\${(1 - chipIn) * -40}px)\` }}>{CHIP}</div>
      <div style={{ position: "absolute", left: ${side}, top: titleTop - 34, width: 220, height: 10, borderRadius: 5, background: "${pal.a}",
        transform: \`scaleX(\${rule})\`, transformOrigin: "left center" }} />
      <div style={{ position: "absolute", left: ${side}, right: ${side}, top: titleTop, color: "#fff", fontFamily: "Playfair Display, serif", fontWeight: 700,
        fontSize: ${vertical ? 104 : 86}, lineHeight: 1.08, textShadow: "0 6px 24px rgba(0,0,0,0.45)",
        opacity: titleIn, transform: \`translateY(\${(1 - titleIn) * 60}px)\` }}>{TITLE}</div>
      <div style={{ position: "absolute", left: ${side}, right: ${side}, top: cardTop, padding: "36px 44px", borderRadius: 28, background: "${pal.c}",
        color: "${pal.ink}", fontFamily: "Montserrat, sans-serif", fontWeight: 900, fontSize: ${vertical ? 76 : 60}, lineHeight: 1.12,
        boxShadow: "0 18px 48px rgba(0,0,0,0.35)", opacity: Math.min(1, cardIn), transform: \`translateY(\${(1 - cardIn) * 50}px) scale(\${0.94 + 0.06 * cardIn})\` }}>{ON_SCREEN}</div>
    </AbsoluteFill>
  );
}
`;
}

/* ------------------------------------------------------------------ Revideo */
function revideo(p) {
  const { W, H, D, pal, vertical } = p;
  // Merkez koordinat: (0,0) ekran ortası; y aşağı artar. Dikey güvenli alan y ∈ [-800, 420].
  const half = { x: W / 2, y: H / 2 };
  const side = vertical ? 80 : 100;
  const textW = W - 2 * side;
  const chipY = -half.y + (vertical ? 210 : 115);
  const titleY = Math.round(-half.y + H * (vertical ? 0.27 : 0.24) + 110);
  const cardY = Math.round(-half.y + H * (vertical ? 0.47 : 0.5) + 120);
  const ring = Math.round(Math.min(W, H) * 0.62);
  const ringY = Math.round(-half.y + H * (vertical ? 0.16 : 0.1) + ring / 2);
  const intro = 1.0;
  const hold = Math.max(0.1, D - intro); // toplam tam D: 1.0 giriş + kalan bekleme
  const asset = p.image ? '/' + p.image.replace(/^\/+/, '') : null; // '/assets/x.png'
  return `import { makeScene2D, Rect, Txt, Img, Circle } from '@revideo/2d';
import { all, createRef, waitFor, easeOutCubic, easeOutBack, linear } from '@revideo/core';

export default makeScene2D('scene', function* (view) {
  const photo = createRef<Img>();
  const ring = createRef<Circle>();
  const chip = createRef<Rect>();
  const rule = createRef<Rect>();
  const title = createRef<Txt>();
  const card = createRef<Rect>();

  view.add(<Rect width={${W}} height={${H}} fill={'${pal.b}'} />);
  view.add(<Circle width={${Math.round(W * 1.3)}} height={${Math.round(W * 1.3)}} y={${Math.round(-H * 0.2)}} fill={'${pal.a}'} opacity={0.55} />);
${asset ? `  view.add(<Img ref={photo} src={'${asset}'} height={${H}} scale={1} />);
  view.add(<Rect width={${W}} height={${H}} fill={'#0a0603'} opacity={0.45} />);
` : ''}  view.add(<Circle ref={ring} width={${ring}} height={${ring}} y={${ringY}} stroke={'rgba(255,255,255,0.18)'} lineWidth={10} scale={0.7} opacity={0} />);
  view.add(
    <Rect ref={chip} layout padding={[14, 28]} radius={999} fill={'rgba(0,0,0,0.45)'} x={${Math.round(-half.x + side + 150)}} y={${Math.round(chipY)}} opacity={0}>
      <Txt text={${jsStr('SAHNE ' + pad2(p.sceneNo))}} fontFamily={'Montserrat'} fontWeight={700} fontSize={${vertical ? 38 : 32}} letterSpacing={4} fill={'${pal.c}'} />
    </Rect>,
  );
  view.add(<Rect ref={rule} width={220} height={10} radius={5} fill={'${pal.a}'} x={${Math.round(-half.x + side + 110)}} y={${titleY - 150}} scale={[0, 1]} />);
  view.add(
    <Txt ref={title} text={${jsStr(p.title || p.onScreen)}} fontFamily={'Playfair Display'} fontWeight={700} fontSize={${vertical ? 104 : 86}}
         lineHeight={${vertical ? 112 : 94}} fill={'#ffffff'} textWrap={true} width={${textW}} textAlign={'left'} y={${titleY + 60}} opacity={0} />,
  );
  view.add(
    <Rect ref={card} layout padding={[36, 44]} radius={28} fill={'${pal.c}'} width={${textW}} y={${cardY}} opacity={0} scale={0.94}>
      <Txt text={${jsStr(p.onScreen)}} fontFamily={'Montserrat'} fontWeight={900} fontSize={${vertical ? 76 : 60}} lineHeight={${vertical ? 86 : 68}}
           fill={'${pal.ink}'} textWrap={true} width={${textW - 88}} />
    </Rect>,
  );

  // Giriş (toplam ${intro.toFixed(1)} sn), ardından bekleme; uzun hareketler (Ken Burns, halka dönüşü) arka planda sürer.
  yield* all(
    ${asset ? `photo().scale(1.08, ${f2(D - 0.4)}, linear),
    ` : ''}ring().rotation(40, ${f2(Math.max(0.2, D - 0.4))}, linear),
    (function* () {
      yield* all(ring().opacity(1, 0.6, easeOutCubic), ring().scale(1, 0.6, easeOutCubic), chip().opacity(1, 0.5, easeOutCubic), rule().scale([1, 1], 0.6, easeOutCubic),
        title().opacity(1, 0.7, easeOutCubic), title().y(${titleY}, 0.7, easeOutCubic));
      yield* all(card().opacity(1, 0.3, easeOutCubic), card().scale(1, 0.3, easeOutBack));
      yield* waitFor(${f2(hold)});
    })(),
  );
});
`;
}

/** Motoruna göre sahne kodu üretir. */
function sceneCode(systemPrompt, userPrompt) {
  const p = parse(systemPrompt, userPrompt);
  const code = p.engine === 'remotion' ? remotion(p) : p.engine === 'revideo' ? revideo(p) : hyperframes(p);
  return { engine: p.engine, sceneNo: p.sceneNo, D: p.D, image: p.image, code };
}

module.exports = { sceneCode, parse };
