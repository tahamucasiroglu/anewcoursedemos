/**
 * Taha Konsept e2e — sahte OpenAI uyumlu LLM sunucusu (bağımlılıksız Node http, varsayılan port 7199).
 *
 *   node tests/e2e/mock-llm/server.cjs [--port 7199] [--log <dosya>]
 *
 * Uçlar:
 *   GET  /v1/models            → {object:"list", data:[{id:"mock"}, …]}
 *   POST /v1/chat/completions  → isteği inceler (sistem/kullanıcı promptu, response_format json_schema şekli) ve
 *                                 GEÇERLİ hazır yanıt döner: konu önerisi, tam konsept (Türkçe, 3 kısa sahne, ~20 sn,
 *                                 "Google" ve "29 Mayıs 1453" ile normalizasyon denemesi, okunuş listesi), kelime denetimi,
 *                                 revize ve HyperFrames sahne kodu (ajanın lint'inden geçer). stream:true ise SSE.
 *   GET  /__log                → son 200 isteğin özeti (JSON)
 * Gerçek ağ çağrısı yok; ücretsiz; deterministik olmayan tek şey konu havuzundaki dönüş sırasıdır.
 */
'use strict';
const http = require('http');
const fs = require('fs');

const args = process.argv.slice(2);
const argOf = (name, def) => { const i = args.indexOf(name); return i >= 0 && args[i + 1] ? args[i + 1] : def; };
const PORT = Number(argOf('--port', process.env.MOCK_LLM_PORT || 7199));
const LOG_FILE = argOf('--log', null);
const history = [];
let topicCursor = 0;
let conceptCounter = 0;

function log(entry) {
  history.push(entry);
  if (history.length > 200) { history.shift(); }
  const line = `[${new Date().toISOString()}] ${entry.kind} ${entry.summary || ''}`;
  console.log(line);
  if (LOG_FILE) { try { fs.appendFileSync(LOG_FILE, line + '\n'); } catch { /* yoksay */ } }
}

/* ------------------------------------------------------------------ Hazır içerik */
const TOPIC_POOL = [
  ['İstanbul\'un fethinde gemilerin karadan yürütülmesi', 'Bir gecede 70 gemi tepeyi aştı.'],
  ['Topkapı Sarayı\'nın gizli Harem koridorları', 'Saraydaki 400 odanın çoğunu kimse görmedi.'],
  ['Barbaros Hayreddin ve Preveze zaferi', '122 gemiyle 300 gemiyi yendi.'],
  ['Kanuni\'nin Viyana kuşatması neden bitti?', 'Kış, ordudan önce geldi.'],
  ['Mimar Sinan\'ın deprem dayanıklı kubbeleri', '450 yıldır ayakta duran sır.'],
  ['Lale Devri\'nde bir soğanın ev fiyatına satılması', 'Bir lale soğanı bir konağa bedeldi.'],
  ['Osmanlı\'da kahvehanelerin yasaklanması', 'Kahve içene ağır ceza vardı.'],
  ['Evliya Çelebi\'nin 40 yıllık yolculuğu', 'Bir rüya, 10 ciltlik seyahatnameye dönüştü.'],
  ['Hezarfen Ahmed Çelebi\'nin Galata uçuşu', 'Galata Kulesi\'nden Üsküdar\'a kanat çırptı.'],
  ['Yeniçeri Ocağı\'nın kazan kaldırması', 'Kazanı devirmek isyan demekti.'],
  ['Piri Reis haritasındaki Amerika kıyıları', '1513\'te çizilen haritanın sırrı.'],
  ['Osmanlı\'nın ilk matbaası ve İbrahim Müteferrika', 'Matbaa 280 yıl gecikti mi?'],
  ['Kösem Sultan: dört padişahın gölgesi', 'İki oğlu ve bir torunu tahta çıktı.'],
  ['Çanakkale\'de Seyit Onbaşı\'nın 215 kiloluk mermisi', 'Tek başına kaldırdığı mermi tarihe geçti.'],
  ['Osmanlı\'da itfaiye: tulumbacılar', 'Yangına koşanlar önce pazarlık ederdi.'],
  ['Süleymaniye Camii\'nin is odası', 'Kandil isinden mürekkep yapılırdı.'],
  ['Fatih\'in Şahi topları', 'Tek atış için saatlerce soğutma gerekti.'],
  ['Osmanlı posta teşkilatı: menziller', 'Haber, atlarla günde 150 kilometre koştu.'],
  ['Kapalıçarşı\'nın 4000 dükkânı', 'Dünyanın ilk alışveriş merkezi mi?'],
  ['Osmanlı\'da lale bahçeleri ve çırağan eğlenceleri', 'Kaplumbağaların sırtında mum yanardı.'],
  ['Turgut Reis\'in Cerbe baskını', 'Kuşatılan donanma kanal kazıp kaçtı.'],
  ['II. Abdülhamid\'in fotoğraf albümleri', '1800 fotoğraf dünyaya hediye edildi.'],
  ['Osmanlı\'da saat kuleleri neden yapıldı?', 'Her şehrin zamanı ayrıydı.'],
  ['Sokullu Mehmed Paşa\'nın Don-Volga kanalı', 'İki nehri birleştirme hayali.'],
  ['Osmanlı mutfağında Matbah-ı Amire', 'Sarayda günde 5000 kişiye yemek pişti.'],
];

const SCENES = [
  {
    title: 'Surların önünde son gece',
    onScreen: '29 Mayıs 1453',
    narration: '29 Mayıs 1453 gecesi surların önünde binlerce asker bekliyordu. Surlar bin yıldır aşılamamıştı ve herkes sabahı düşünüyordu.',
    visualPrompt: 'Massive ancient stone city walls at night, torches, soldiers waiting, dramatic moonlight, cinematic wide shot, historical painting style',
  },
  {
    title: 'Toplar konuşuyor',
    onScreen: 'Şahi topları',
    narration: 'Dev Şahi topları surlara saatlerce ateş etti. Taş duvarlarda açılan gedikler gece boyunca hızla onarılmaya çalışıldı.',
    visualPrompt: 'Huge bronze cannon firing at stone walls, smoke and fire, 15th century siege, warm orange light, low angle, epic historical style',
  },
  {
    title: 'Bugünün haritasında',
    onScreen: 'Google haritasında bile',
    narration: 'Bugün Google haritasında baktığınızda o surların izini hâlâ görebilirsiniz. Peki bu şehir tek bir gecede nasıl değişti?',
    visualPrompt: 'Modern aerial view of Istanbul old city walls, golden hour, map-like top-down perspective, clean cinematic style',
  },
];

function wordCount(t) { return (String(t).match(/\S+/g) || []).length; }

function conceptFor(title, hook, revised) {
  conceptCounter++;
  const scenes = SCENES.map((s, i) => ({
    title: s.title,
    onScreen: s.onScreen,
    visualPrompt: s.visualPrompt,
    narration: revised && i === 0 ? s.narration.replace('binlerce asker bekliyordu', 'binlerce asker sessizce bekliyordu') : s.narration,
    durationHintSec: Math.round(wordCount(s.narration) / 2.4 * 10) / 10,
  }));
  return {
    title: (title || 'Konstantinopolis\'in son gecesi').slice(0, 70),
    hook: hook || '29 Mayıs 1453 gecesi bin yıllık surlar son kez bekliyordu.',
    summary: 'İstanbul\'un fethinden önceki son geceyi ve bugüne kalan izleri üç kısa sahnede anlatan video.',
    scenes,
    sources: [],
    pronunciations: [{ written: 'Google', spoken: 'Gugıl' }],
    tags: ['tarih', 'osmanlı', 'istanbul', 'fetih', '1453'],
  };
}

function topicsFor(count) {
  const n = Math.max(1, Math.min(50, count || 5));
  const out = [];
  for (let i = 0; i < n; i++) {
    const [title, hook] = TOPIC_POOL[(topicCursor + i) % TOPIC_POOL.length];
    const round = Math.floor((topicCursor + i) / TOPIC_POOL.length);
    out.push({ title: round > 0 ? `${title} (${round + 1}. bakış)` : title, hook });
  }
  topicCursor += n;
  return { topics: out };
}

function wordCheckFor(userPrompt) {
  const scenes = [];
  const re = /^\[(\d+)\]\s+(.*)$/gm;
  let m;
  while ((m = re.exec(userPrompt)) !== null) {
    const idx = Number(m[1]);
    let text = m[2];
    // İkinci "surlar" geçişini eş anlamlıyla değiştir (uzunluk korunur).
    let seen = 0;
    text = text.replace(/\b(surlar|surları|surlara)\b/gi, w => (++seen === 2 ? 'duvarlar' : w));
    scenes.push({ index: idx, narration: text });
  }
  return { scenes };
}

function sceneCodeFor(systemPrompt, userPrompt) {
  const size = /—\s*(\d+)x(\d+)/.exec(userPrompt) || /width:\s*(\d+)px;\s*height:\s*(\d+)px/.exec(systemPrompt) || [0, 1080, 1920];
  const W = Number(size[1]); const H = Number(size[2]);
  const dur = /süre \{D\} = ([\d.]+) sn/.exec(userPrompt);
  const D = dur ? Number(dur[1]) : 5;
  const sceneNo = (/Sahne (\d+)/.exec(userPrompt) || [0, '1'])[1];
  const onScreen = ((/Ekranda gösterilecek: (.*)/.exec(userPrompt) || [0, 'Taha Konsept'])[1] || 'Taha Konsept').trim();
  const titleText = ((/Başlık: (.*)/.exec(userPrompt) || [0, ''])[1] || '').trim();
  const images = /Kullanılabilir görseller \(yalnızca bunlar\): (.*)/.exec(userPrompt);
  const firstImage = images ? images[1].split(',')[0].trim() : null;
  const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  const vertical = H >= W;
  const titleSize = vertical ? 104 : 88;
  const subSize = vertical ? 52 : 42;
  const palette = [['#e2711d', '#3d2412'], ['#b5651d', '#1f2a44'], ['#d9a441', '#2b1d14']][(Number(sceneNo) - 1) % 3];
  const hold = Math.max(0.5, D - 0.4).toFixed(2);
  const subStart = Math.min(1.0, D / 3).toFixed(2);
  const bgImage = firstImage ? `\n  <img id="photo" class="clip" data-start="0" data-duration="${D}" src="${esc(firstImage)}" style="inset:0;width:100%;height:100%;object-fit:cover;opacity:0.85" />` : '';
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
  #bg { inset: 0; background: radial-gradient(circle at 50% 35%, ${palette[0]} 0%, ${palette[1]} 75%); }
  #ring { left: ${Math.round(W / 2 - W * 0.3)}px; top: ${Math.round(H * 0.18)}px; width: ${Math.round(W * 0.6)}px; height: ${Math.round(W * 0.6)}px; border-radius: 50%; border: 12px solid rgba(255,255,255,0.25); }
  #title { left: ${vertical ? 80 : 100}px; right: ${vertical ? 80 : 100}px; top: ${Math.round(H * (vertical ? 0.36 : 0.30))}px; text-align: center; color: #fff; font-family: 'Montserrat', sans-serif; font-weight: 900; font-size: ${titleSize}px; line-height: 1.1; opacity: 0; }
  #sub { left: ${vertical ? 80 : 100}px; right: ${vertical ? 80 : 100}px; top: ${Math.round(H * (vertical ? 0.55 : 0.55))}px; text-align: center; color: #ffe8cc; font-weight: 700; font-size: ${subSize}px; opacity: 0; }
</style>
</head>
<body>
<div id="root" data-composition-id="main" data-start="0" data-duration="${D}" data-width="${W}" data-height="${H}">
  <div id="bg" class="clip" data-start="0" data-duration="${D}"></div>${bgImage}
  <div id="ring" class="clip" data-start="0" data-duration="${D}"></div>
  <h1 id="title" class="clip" data-start="0" data-duration="${D}">${esc(onScreen)}</h1>
  <div id="sub" class="clip" data-start="${subStart}" data-duration="${(D - Number(subStart)).toFixed(2)}">${esc(titleText)}</div>
</div>
<script src="gsap.min.js"></script>
<script>
  window.__timelines = window.__timelines || {};
  const tl = gsap.timeline({ paused: true });
  tl.fromTo("#ring", { scale: 0.6, opacity: 0 }, { scale: 1, opacity: 1, duration: 0.8, ease: "power2.out" }, 0.0);
  tl.fromTo("#title", { opacity: 0, y: 60 }, { opacity: 1, y: 0, duration: 0.6, ease: "power3.out" }, 0.1);
  tl.fromTo("#sub", { opacity: 0, y: 30 }, { opacity: 1, y: 0, duration: 0.5, ease: "power2.out" }, ${subStart});
  tl.to("#ring", { rotation: 30, duration: ${Math.max(0.5, Number(hold) - 0.8).toFixed(2)}, ease: "none" }, 0.8);
  window.__timelines["main"] = tl;
</script>
</body>
</html>`;
}

/* ------------------------------------------------------------------ İstek sınıflandırma */
function classify(body) {
  const messages = body.messages || [];
  const system = (messages.find(m => m.role === 'system') || {}).content || '';
  const user = (messages.filter(m => m.role === 'user').pop() || {}).content || '';
  const sys = typeof system === 'string' ? system : JSON.stringify(system);
  const usr = typeof user === 'string' ? user : JSON.stringify(user);
  const rf = body.response_format || null;
  const props = rf && rf.json_schema && rf.json_schema.schema && rf.json_schema.schema.properties ? Object.keys(rf.json_schema.schema.properties) : [];
  const sceneProps = rf && rf.json_schema && rf.json_schema.schema && rf.json_schema.schema.properties && rf.json_schema.schema.properties.scenes
    && rf.json_schema.schema.properties.scenes.items && rf.json_schema.schema.properties.scenes.items.properties
    ? Object.keys(rf.json_schema.schema.properties.scenes.items.properties) : [];

  if (props.includes('topics')) {
    const count = Number((/(\d+) konu öner\./.exec(usr) || [0, 5])[1]);
    return { kind: 'topics', summary: `count=${count}`, json: topicsFor(count) };
  }
  if (props.includes('scenes') && props.includes('title')) {
    if (usr.includes('Düzeltme talimatı:')) {
      const doc = /"title"\s*:\s*"((?:[^"\\]|\\.)*)"/.exec(usr);
      const instr = ((/Düzeltme talimatı: (.*)/.exec(usr) || [0, ''])[1]).trim();
      const base = doc ? JSON.parse('"' + doc[1] + '"') : 'Konstantinopolis\'in son gecesi';
      const title = base.replace(/ \(revize( \d+)?\)$/, '') + ' (revize)';
      return { kind: 'revise', summary: `instr="${instr.slice(0, 60)}"`, json: conceptFor(title, 'Bin yıllık surlar o gece son kez sessizdi.', true) };
    }
    const t = /Konu: (.*)/.exec(usr);
    const h = /Önerilen kanca: (.*)/.exec(usr);
    const idea = /Kullanıcının fikri: (.*)/.exec(usr);
    const angle = /Aynı fikirden üretilen (\d+)\. konsept/.exec(usr);
    let title = t ? t[1].trim() : (idea ? idea[1].trim().slice(0, 50) : null);
    if (title && angle) { title = `${title.slice(0, 55)} — ${angle[1]}. açı`; }
    return { kind: 'concept', summary: `title="${title || ''}"`, json: conceptFor(title, h ? h[1].trim() : null, false) };
  }
  if (props.includes('scenes') && sceneProps.includes('index')) {
    return { kind: 'word-check', summary: '', json: wordCheckFor(usr) };
  }
  if (sys.includes('sahne yazım sözleşmesi') || /Kuralları uygula ve yalnızca sahne kodunu döndür/.test(usr)) {
    const code = sceneCodeFor(sys, usr);
    return { kind: 'scene-code', summary: (/Sahne (\d+) — süre \{D\} = ([\d.]+)/.exec(usr) || ['?'])[0], text: code };
  }
  if (rf && rf.type === 'json_schema') {
    return { kind: 'unknown-json', summary: `props=${props.join(',')}`, json: {} };
  }
  return { kind: 'text', summary: usr.slice(0, 80), text: 'Tamam.' };
}

/* ------------------------------------------------------------------ HTTP */
function sendJson(res, status, obj) {
  const data = JSON.stringify(obj);
  res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8', 'Content-Length': Buffer.byteLength(data) });
  res.end(data);
}

function completion(body, content) {
  const promptTokens = Math.ceil(JSON.stringify(body.messages || []).length / 4);
  const completionTokens = Math.ceil(content.length / 4);
  return {
    id: 'chatcmpl-mock-' + Date.now(), object: 'chat.completion', created: Math.floor(Date.now() / 1000), model: body.model || 'mock',
    choices: [{ index: 0, message: { role: 'assistant', content }, finish_reason: 'stop' }],
    usage: { prompt_tokens: promptTokens, completion_tokens: completionTokens, total_tokens: promptTokens + completionTokens },
  };
}

function stream(res, body, content) {
  res.writeHead(200, { 'Content-Type': 'text/event-stream; charset=utf-8', 'Cache-Control': 'no-cache', Connection: 'keep-alive' });
  const id = 'chatcmpl-mock-' + Date.now();
  const model = body.model || 'mock';
  const chunk = obj => res.write('data: ' + JSON.stringify(obj) + '\n\n');
  for (let i = 0; i < content.length; i += 400) {
    chunk({ id, object: 'chat.completion.chunk', model, choices: [{ index: 0, delta: { content: content.slice(i, i + 400) }, finish_reason: null }] });
  }
  chunk({ id, object: 'chat.completion.chunk', model, choices: [{ index: 0, delta: {}, finish_reason: 'stop' }] });
  const full = completion(body, content);
  chunk({ id, object: 'chat.completion.chunk', model, choices: [], usage: full.usage });
  res.write('data: [DONE]\n\n');
  res.end();
}

function handler(req, res) {
  const url = req.url.split('?')[0].replace(/\/+$/, '');
  if (req.method === 'GET' && url === '/v1/models') {
    log({ kind: 'models', summary: req.headers.authorization ? 'auth' : 'no-auth' });
    return sendJson(res, 200, { object: 'list', data: [{ id: 'mock', object: 'model', owned_by: 'taha-e2e' }, { id: 'mock-large', object: 'model', owned_by: 'taha-e2e' }] });
  }
  if (req.method === 'GET' && url === '/__log') { return sendJson(res, 200, history); }
  if (req.method === 'POST' && url === '/v1/chat/completions') {
    let raw = '';
    req.setEncoding('utf8');
    req.on('data', c => { raw += c; });
    req.on('end', () => {
      let body;
      try { body = JSON.parse(raw); } catch (e) { return sendJson(res, 400, { error: { message: 'Geçersiz JSON: ' + e.message } }); }
      let result;
      try { result = classify(body); } catch (e) { log({ kind: 'error', summary: e.stack }); return sendJson(res, 500, { error: { message: e.message } }); }
      const content = result.json !== undefined ? JSON.stringify(result.json) : result.text;
      log({ kind: result.kind, summary: `${result.summary || ''} model=${body.model} stream=${!!body.stream} max=${body.max_tokens || body.max_completion_tokens}` });
      if (body.stream) { return stream(res, body, content); }
      return sendJson(res, 200, completion(body, content));
    });
    return undefined;
  }
  return sendJson(res, 404, { error: { message: 'Bulunamadı: ' + req.method + ' ' + req.url } });
}

/* Yalnız geri döngü: hem IPv4 hem IPv6 (localhost ::1'e çözülebilir). */
http.createServer(handler).listen(PORT, '127.0.0.1', () => console.log(`mock-llm http://127.0.0.1:${PORT}/v1 hazır`));
http.createServer(handler).on('error', () => { /* IPv6 yoksa yoksay */ }).listen(PORT, '::1');
