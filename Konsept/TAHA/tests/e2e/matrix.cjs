/**
 * Taha Konsept e2e — 30 saniyelik yöntem matrisi koşucusu ("kurgu30").
 *
 * Tek bir yöntem birleşimini (motor × ses × görsel × altyazı × ilerleme çubuğu) GERÇEK boru hattından geçirir ve doğrular:
 *   API'ye giriş (bearer) → üretim ayarları (PUT /api/production/settings) → kurgu30 konseptini bul ya da üret+onayla
 *   → sıraya ekle + başlat → aşamaları izle (ilerleme yazdırılır) → video ayrıntısı → mp4'ü ajandan indir
 *   → ffprobe (h264+aac, boyut, süre) · 4 kare (ffmpeg) · volumedetect (video sesi + speech.wav) · okunuş kontrolü (API spokenText)
 *   → tests/e2e/out/kurgu30/<etiket>/{result.json, video.mp4, speech.wav, frames/*.png, detail.json, queue-item.json}
 *
 * Kullanım (yığın: node tests/e2e/stack.cjs start everything — mock 7199, API 7191, Web 7190, ajan --headless 7120):
 *   node tests/e2e/matrix.cjs --label hf-supertonic3-off
 *   node tests/e2e/matrix.cjs --label rm-supertonic3-zimage --engine remotion --image-mode Local --image-model z-image-turbo
 *   node tests/e2e/matrix.cjs --combo "{\"engine\":\"revideo\",\"speechModelKey\":\"piper-tr\"}" --label rv-piper
 *   node tests/e2e/matrix.cjs --verify-only <videoId> --label ...        (üretmeden yalnız doğrulama)
 * Seçenekler (varsayılan = taban çizgisi):
 *   --engine hyperframes|remotion|revideo   --speech-mode Local|Online   --speech-model supertonic-3   --voice <id>
 *   --image-mode Off|Local|Online   --image-model <anahtar>   --subtitles 1|0   --progress-bar 1|0   --speed 1.0
 *   --timeout-min 40   --new-concept (kurgu30'u yeniden üret: yeni konsept = önceki işlerden yeniden kullanım yok, temiz ölçüm)
 *   --start-stack (yığın kapalıysa başlat)   --whisper [uv yolu] (faster-whisper ile kaba anlaşılırlık; --whisper-model small)
 * Çıkış kodu: 0 = tüm denetimler geçti, 1 = en az bir denetim başarısız, 2 = üretim başarısız/zaman aşımı.
 * Güvenlik: yalnız e2e yığını (tahakonsept_e2e, stack.cjs zorlar); parola .env'den okunur, asla yazdırılmaz; LLM = mock.
 */
'use strict';
const fs = require('fs');
const path = require('path');
const http = require('http');
const { execFileSync, spawnSync } = require('child_process');

const ROOT = path.resolve(__dirname, '..', '..');
const API = process.env.E2E_API || 'http://127.0.0.1:7191';
const AGENT_ROOT = path.join(ROOT, '.tools-dev', 'e2e-agent');
const FFMPEG = path.join(AGENT_ROOT, 'Tools', 'ffmpeg', 'bin', 'ffmpeg.exe');
const FFPROBE = path.join(AGENT_ROOT, 'Tools', 'ffmpeg', 'bin', 'ffprobe.exe');
const sleep = ms => new Promise(r => setTimeout(r, ms));

/* ------------------------------------------------------------------ argümanlar */
function parseArgs(argv) {
  const o = {};
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (!a.startsWith('--')) { continue; }
    const k = a.slice(2);
    const v = argv[i + 1] && !argv[i + 1].startsWith('--') ? argv[++i] : '1';
    o[k] = v;
  }
  return o;
}
const args = parseArgs(process.argv.slice(2));
const combo = Object.assign({
  engine: 'hyperframes', speechMode: 'Local', speechModelKey: 'supertonic-3', voice: null,
  imageMode: 'Off', imageModelKey: null, subtitles: true, progressBar: true, speed: 1.0,
}, args.combo ? JSON.parse(args.combo) : {});
if (args.engine) { combo.engine = args.engine; }
if (args['speech-mode']) { combo.speechMode = args['speech-mode']; }
if (args['speech-model']) { combo.speechModelKey = args['speech-model']; }
if (args.voice) { combo.voice = args.voice; }
if (args['image-mode']) { combo.imageMode = args['image-mode']; }
if (args['image-model']) { combo.imageModelKey = args['image-model']; }
if (args.subtitles) { combo.subtitles = args.subtitles !== '0'; }
if (args['progress-bar']) { combo.progressBar = args['progress-bar'] !== '0'; }
if (args.speed) { combo.speed = Number(args.speed); }
const LABEL = args.label || [combo.engine, combo.speechModelKey, combo.imageMode === 'Off' ? 'img-off' : (combo.imageModelKey || combo.imageMode)].join('_');
const OUT = path.join(__dirname, 'out', 'kurgu30', LABEL.replace(/[^\w.-]+/g, '-'));
const TIMEOUT_MS = Number(args['timeout-min'] || 40) * 60000;

const t0 = Date.now();
const log = (...a) => console.log(`[${String(Math.round((Date.now() - t0) / 1000)).padStart(4)}s]`, ...a);

/* ------------------------------------------------------------------ HTTP (Node http; tarayıcı yok) */
let token = null;
function request(method, url, body, opts) {
  const o = opts || {};
  return new Promise((resolve, reject) => {
    const u = new URL(url.startsWith('http') ? url : API + url);
    const data = body === undefined || body === null ? null : Buffer.from(JSON.stringify(body));
    const headers = { Accept: 'application/json' };
    if (data) { headers['Content-Type'] = 'application/json'; headers['Content-Length'] = data.length; }
    if (token && !o.noAuth) { headers.Authorization = 'Bearer ' + token; }
    const req = http.request({ method, hostname: u.hostname, port: u.port, path: u.pathname + u.search, headers }, res => {
      if (o.toFile) {
        const ws = fs.createWriteStream(o.toFile);
        res.pipe(ws);
        ws.on('finish', () => resolve({ status: res.statusCode, file: o.toFile }));
        ws.on('error', reject);
        return;
      }
      const chunks = [];
      res.on('data', c => chunks.push(c));
      res.on('end', () => {
        const txt = Buffer.concat(chunks).toString('utf8');
        let json = null;
        try { json = txt ? JSON.parse(txt) : null; } catch { json = null; }
        resolve({ status: res.statusCode, json, text: txt });
      });
    });
    req.on('error', reject);
    req.setTimeout(o.timeoutMs || 60000, () => req.destroy(new Error('HTTP zaman aşımı: ' + method + ' ' + url)));
    if (data) { req.write(data); }
    req.end();
  });
}
async function api(method, url, body) {
  const r = await request(method, url, body);
  if (r.status >= 400) { throw new Error(`${method} ${url} → ${r.status} ${r.text.slice(0, 400)}`); }
  return r.json;
}
async function health(url) { try { return (await request('GET', url, null, { noAuth: true, timeoutMs: 3000 })).status; } catch { return 0; } }

function readEnv() {
  return Object.fromEntries(fs.readFileSync(path.join(ROOT, '.env'), 'utf8').split(/\r?\n/)
    .filter(l => l.includes('=') && !l.trim().startsWith('#'))
    .map(l => [l.slice(0, l.indexOf('=')).trim(), l.slice(l.indexOf('=') + 1).trim()])
    .filter(([k]) => k.startsWith('TAHAKONSEPT_E2E_')));
}

/* ------------------------------------------------------------------ adımlar */
async function ensureStack() {
  const checks = { mock: 'http://127.0.0.1:7199/v1/models', api: API + '/api/health', web: 'http://127.0.0.1:7190/', agent: 'http://127.0.0.1:7120/' };
  const down = [];
  for (const [n, u] of Object.entries(checks)) { if (!(await health(u))) { down.push(n); } }
  if (down.length && args['start-stack']) {
    log('yığın başlatılıyor:', down.join(','));
    execFileSync(process.execPath, [path.join(__dirname, 'stack.cjs'), 'start', down.join(',')], { stdio: 'inherit' });
  } else if (down.length) {
    throw new Error('Yığın çalışmıyor (' + down.join(', ') + '). Önce: node tests/e2e/stack.cjs start everything (ya da --start-stack).');
  }
}

async function login() {
  const env = readEnv();
  const r = await request('POST', '/api/identity/login', { userName: env.TAHAKONSEPT_E2E_USER, password: env.TAHAKONSEPT_E2E_PASSWORD }, { noAuth: true });
  if (r.status !== 200 || !r.json || !r.json.accessToken) { throw new Error('Giriş başarısız: HTTP ' + r.status); }
  token = r.json.accessToken;
  log('giriş: tamam (bearer)');
}

function settingsFor(current) {
  return Object.assign({}, current, {
    codeModelKey: 'local-openai:mock',
    videoEngine: combo.engine,
    imageMode: combo.imageMode,
    imageModelKey: combo.imageMode === 'Off' ? null : combo.imageModelKey,
    speechMode: combo.speechMode,
    speechModelKey: combo.speechModelKey,
    voiceId: combo.voice || null,
    speechSpeed: combo.speed,
    burnSubtitles: !!combo.subtitles,
    showProgressBar: !!combo.progressBar,
  });
}

async function ensureConceptModels() {
  const cs = await api('GET', '/api/concepts/settings');
  if (cs.conceptModelKey !== 'local-openai:mock') {
    await api('PUT', '/api/concepts/settings', Object.assign({}, cs, { conceptModelKey: 'local-openai:mock', topicModelKey: 'local-openai:mock', wordCheckModelKey: 'local-openai:mock' }));
    log('konsept modelleri → local-openai:mock');
  }
}

async function findKurgu30() {
  const list = await api('GET', '/api/concepts');
  const items = Array.isArray(list) ? list : (list.items || []);
  return items.filter(c => (c.tags || []).includes('kurgu30') && c.status === 'Approved' && !c.deletedAt)
    .sort((a, b) => String(b.createdAt).localeCompare(String(a.createdAt)))[0] || null;
}

async function ensureConcept() {
  if (!args['new-concept']) {
    const existing = await findKurgu30();
    if (existing) { log('kurgu30 konsepti yeniden kullanılıyor:', existing.id, `(${existing.sceneCount} sahne, ~${existing.estimatedDurationSec} sn)`); return existing.id; }
  }
  await ensureConceptModels();
  const job = await api('POST', '/api/concepts/from-idea', { idea: 'kurgu30: İstanbul\'un fethi, 30 saniyelik sabit deneme kurgusu', count: 1 });
  log('kurgu30 üretiliyor (konsept işi', job.id + ')');
  const until = Date.now() + 5 * 60000;
  let cur = job;
  while (Date.now() < until) {
    await sleep(1500);
    cur = await api('GET', '/api/concepts/jobs/current');
    if (cur && /Completed|Failed|Cancelled|Done/i.test(String(cur.status)) && (!cur.id || cur.id === job.id)) { break; }
  }
  const id = (cur && cur.producedIds && cur.producedIds[0]) || ((await (async () => {
    const l = await api('GET', '/api/concepts');
    return (Array.isArray(l) ? l : l.items || []).filter(c => (c.tags || []).includes('kurgu30')).sort((a, b) => String(b.createdAt).localeCompare(String(a.createdAt)))[0];
  })()) || {}).id;
  if (!id) { throw new Error('kurgu30 konsepti üretilemedi: ' + JSON.stringify(cur).slice(0, 400)); }
  await api('POST', `/api/concepts/${id}/approve`);
  log('kurgu30 onaylandı:', id);
  return id;
}

function stageLine(item) {
  return (item.stages || []).map(s => {
    const st = { Completed: '✓', Running: '▶', Failed: '✗', Pending: '·', Skipped: '-', Waiting: '…' }[s.status] || s.status;
    const sc = s.scenesTotal ? ` ${s.scenesDone}/${s.scenesTotal}` : '';
    const pr = s.status === 'Running' ? ` ${Math.round(s.progress)}%` : '';
    return `${s.stage}${st}${sc}${pr}`;
  }).join(' ');
}

async function produce(conceptId, settings) {
  const before = await api('GET', '/api/production/queue');
  const knownIds = new Set((before.items || []).map(i => i.id));
  if ((before.items || []).some(i => /Running|Queued|Waiting/i.test(String(i.status)) && i.conceptId !== conceptId)) {
    log('UYARI: sırada başka bekleyen/çalışan öğeler var:', (before.items || []).filter(i => !/Done|Failed|Cancelled/i.test(String(i.status))).map(i => i.title + ':' + i.status).join(', '));
  }
  const added = await api('POST', '/api/production/queue', { conceptIds: [conceptId], settings });
  const item0 = (added.items || []).filter(i => !knownIds.has(i.id) && i.conceptId === conceptId).pop();
  if (!item0) { throw new Error('Sıraya eklenen öğe bulunamadı: ' + JSON.stringify(added).slice(0, 400)); }
  log('sıraya eklendi: öğe', item0.id);
  await api('POST', '/api/production/queue/start');
  const tStart = Date.now();
  let last = '';
  let lastMsg = '';
  let speechCopied = false;
  while (Date.now() - tStart < TIMEOUT_MS) {
    await sleep(3000);
    const q = await api('GET', '/api/production/queue');
    const it = (q.items || []).find(i => i.id === item0.id);
    if (!it) { throw new Error('Sıra öğesi kayboldu'); }
    const line = `${it.status} ${Math.round(it.progress)}% | ${stageLine(it)}`;
    const msg = it.message || '';
    if (line !== last || msg !== lastMsg) { log(line + (msg ? '  — ' + msg : '') + (q.agentOnline ? '' : '  [AJAN ÇEVRİMDIŞI]')); last = line; lastMsg = msg; }
    if (!speechCopied && it.jobId) { speechCopied = copySpeech(it.jobId); }
    if (it.status === 'Done' && it.videoId) { return { item: it, wallSec: (Date.now() - tStart) / 1000 }; }
    if (/Failed|Cancelled/i.test(String(it.status))) { return { item: it, failed: true, wallSec: (Date.now() - tStart) / 1000 }; }
  }
  return { timeout: true, wallSec: (Date.now() - tStart) / 1000 };
}

function copySpeech(jobId) {
  const p = path.join(AGENT_ROOT, 'Workspace', String(jobId).replace(/-/g, ''), 'speech', 'speech.wav');
  try {
    if (fs.existsSync(p) && fs.statSync(p).size > 1000) { fs.copyFileSync(p, path.join(OUT, 'speech.wav')); return true; }
  } catch { /* yazılıyor olabilir; sonraki turda */ }
  return false;
}

/* ------------------------------------------------------------------ doğrulama */
function ffprobe(file) {
  const out = execFileSync(FFPROBE, ['-v', 'error', '-show_streams', '-show_format', '-of', 'json', file], { encoding: 'utf8' });
  return JSON.parse(out);
}
function volume(file, extraArgs) {
  const r = spawnSync(FFMPEG, ['-hide_banner', '-nostats', '-i', file, ...(extraArgs || []), '-af', 'volumedetect', '-vn', '-f', 'null', '-'], { encoding: 'utf8' });
  const txt = r.stderr || '';
  const mean = /mean_volume:\s*(-?[\d.]+) dB/.exec(txt);
  const max = /max_volume:\s*(-?[\d.]+) dB/.exec(txt);
  return { meanDb: mean ? Number(mean[1]) : null, maxDb: max ? Number(max[1]) : null };
}
function frames(file, duration) {
  const dir = path.join(OUT, 'frames');
  fs.mkdirSync(dir, { recursive: true });
  const list = [];
  for (const frac of [0.08, 0.35, 0.62, 0.9]) {
    const t = Math.max(0.2, duration * frac);
    const f = path.join(dir, `frame-${String(Math.round(t * 10) / 10).replace('.', '_')}s.png`);
    spawnSync(FFMPEG, ['-hide_banner', '-loglevel', 'error', '-y', '-ss', t.toFixed(2), '-i', file, '-frames:v', '1', f]);
    list.push({ t: Math.round(t * 100) / 100, file: f, ok: fs.existsSync(f) && fs.statSync(f).size > 5000 });
  }
  return list;
}
const norm = s => String(s || '').toLocaleLowerCase('tr-TR').replace(/\s+/g, ' ');

async function verify(videoId, run, settings) {
  const checks = [];
  const check = (name, ok, detail) => { checks.push({ name, ok: !!ok, detail: detail === undefined ? null : detail }); log(`${ok ? 'OK ' : 'HATA'} ${name}${detail !== undefined ? ' — ' + (typeof detail === 'string' ? detail : JSON.stringify(detail)) : ''}`); };
  const detail = await api('GET', `/api/production/videos/${videoId}`);
  fs.writeFileSync(path.join(OUT, 'detail.json'), JSON.stringify(detail, null, 2));
  const s = detail.settings || {};
  check('ayarlar videoya işlendi', String(s.videoEngine).toLowerCase() === combo.engine && s.speechModelKey === combo.speechModelKey
    && String(s.imageMode) === combo.imageMode && !!s.burnSubtitles === !!combo.subtitles && !!s.showProgressBar === !!combo.progressBar,
  { engine: s.videoEngine, speech: s.speechModelKey, voice: s.voiceId, image: s.imageMode, imageModel: s.imageModelKey, subs: s.burnSubtitles, bar: s.showProgressBar });

  // mp4'ü ajandan indir (oynatıcı adresi); olmazsa ajan Videos klasöründen kopyala
  const mp4 = path.join(OUT, 'video.mp4');
  let got = false;
  if (detail.playbackUrl) {
    try { const r = await request('GET', detail.playbackUrl, null, { toFile: mp4, noAuth: true, timeoutMs: 120000 }); got = r.status === 200 || r.status === 206; } catch (e) { log('indirme hatası:', e.message); }
  }
  if (!got || !fs.existsSync(mp4) || fs.statSync(mp4).size < 10000) {
    const vids = fs.readdirSync(path.join(AGENT_ROOT, 'Videos')).filter(f => f.endsWith('.mp4') && f.includes(String(videoId).slice(0, 8)));
    if (vids.length) { fs.copyFileSync(path.join(AGENT_ROOT, 'Videos', vids[0]), mp4); got = true; }
  }
  check('mp4 alındı', got && fs.existsSync(mp4), detail.playbackUrl ? 'oynatıcı adresinden' : 'ajan klasöründen');
  if (!fs.existsSync(mp4)) { return { checks, detail }; }

  const probe = ffprobe(mp4);
  fs.writeFileSync(path.join(OUT, 'ffprobe.json'), JSON.stringify(probe, null, 2));
  const v = (probe.streams || []).find(x => x.codec_type === 'video') || {};
  const a = (probe.streams || []).find(x => x.codec_type === 'audio') || {};
  const dur = Number(probe.format && probe.format.duration) || 0;
  const W = detail.width || 1080; const H = detail.height || 1920;
  check('video akışı h264', v.codec_name === 'h264', `${v.codec_name} ${v.width}x${v.height} ${v.r_frame_rate} ${v.pix_fmt}`);
  check('ses akışı aac', a.codec_name === 'aac', `${a.codec_name} ${a.sample_rate} Hz ${a.channels} kanal`);
  check(`çözünürlük ${W}x${H}`, v.width === W && v.height === H, `${v.width}x${v.height}`);
  check('süre 25–40 sn', dur >= 25 && dur <= 40, `${dur.toFixed(2)} sn`);
  const vDur = Number(v.duration) || dur; const aDur = Number(a.duration) || dur;
  check('görüntü ≈ ses süresi (±0,5 sn)', Math.abs(vDur - aDur) <= 0.5, `görüntü ${vDur.toFixed(2)} / ses ${aDur.toFixed(2)}`);

  const vol = volume(mp4);
  check('video sesi sessiz değil (mean > -45 dB)', vol.meanDb !== null && vol.meanDb > -45, vol);
  let speech = null;
  if (!fs.existsSync(path.join(OUT, 'speech.wav')) && detail.jobId) { copySpeech(detail.jobId); }
  const wav = path.join(OUT, 'speech.wav');
  if (fs.existsSync(wav)) {
    const sp = ffprobe(wav);
    speech = { durationSec: Number(sp.format.duration), ...volume(wav) };
    check('speech.wav sessiz değil (mean > -45 dB)', speech.meanDb !== null && speech.meanDb > -45, speech);
    check('süre ≈ seslendirme (±1,5 sn)', Math.abs(dur - speech.durationSec) <= 1.5, `video ${dur.toFixed(2)} / seslendirme ${speech.durationSec.toFixed(2)}`);
  } else {
    // speech.wav çalışma klasöründen silinmişse videodaki ses izi seslendirmedir (müzik yok).
    spawnSync(FFMPEG, ['-hide_banner', '-loglevel', 'error', '-y', '-i', mp4, '-vn', '-ac', '1', '-ar', '24000', path.join(OUT, 'audio-from-video.wav')]);
    log('speech.wav bulunamadı; ses izi audio-from-video.wav olarak çıkarıldı');
  }

  const fr = frames(mp4, dur);
  check('4 kare çıkarıldı', fr.every(f => f.ok), fr.map(f => path.basename(f.file)));

  // Okunuş (normalize) kontrolü: API ayrıntısındaki spokenText
  const spoken = norm((detail.scenes || []).map(x => x.spokenText).join(' '));
  const display = (detail.scenes || []).map(x => x.displayText).join(' ');
  const expect = [
    ['Google → Gugıl', 'gugıl', 'google'],
    ['29 Mayıs 1453 → yirmi dokuz Mayıs bin dört yüz elli üç', 'yirmi dokuz mayıs bin dört yüz elli üç', '1453'],
    ['II. Mehmed → İkinci Mehmed', 'ikinci mehmed', 'ii. mehmed'],
    ['%40 → yüzde kırk', 'yüzde kırk', '%40'],
    ['XV. yüzyıl → On beşinci yüzyıl', 'on beşinci yüzyıl', 'xv.'],
  ];
  for (const [name, want, bad] of expect) { check('okunuş: ' + name, spoken.includes(want) && !spoken.includes(bad), null); }
  check('yazılış korunuyor (displayText)', /Google/.test(display) && /1453/.test(display) && /II\. Mehmed/.test(display), null);
  fs.writeFileSync(path.join(OUT, 'spoken.txt'), (detail.scenes || []).map(x => `[${x.index}] ${x.displayText}\n    → ${x.spokenText}`).join('\n'));

  // İsteğe bağlı kaba anlaşılırlık: faster-whisper (uv ile; model .tools-dev/e2e-agent/Models/hf-cache)
  let whisper = null;
  if (args.whisper) {
    const uv = args.whisper === '1' ? (process.env.UV || 'uv') : args.whisper;
    const audio = fs.existsSync(wav) ? wav : path.join(OUT, 'audio-from-video.wav');
    const r = spawnSync(uv, ['run', '--python', '3.12', '--with', 'faster-whisper', 'python', path.join(__dirname, 'whisper-check.py'), audio, path.join(OUT, 'spoken.txt'),
      '--model', args['whisper-model'] || 'small', '--out', path.join(OUT, 'whisper.json')], { encoding: 'utf8', env: { ...process.env, PYTHONIOENCODING: 'utf-8' }, timeout: 15 * 60000 });
    try { whisper = JSON.parse(fs.readFileSync(path.join(OUT, 'whisper.json'), 'utf8')); } catch { whisper = null; }
    if (whisper) {
      log('whisper:', whisper.transcript);
      check('whisper anlaşılırlık (kelime geri çağırma ≥ 0,70)', whisper.wordRecall >= 0.7, { wordRecall: whisper.wordRecall, spoken: whisper.spokenRecall, written: whisper.writtenRecall });
    } else {
      log('whisper çalışmadı (denetim atlandı):', (r.error && r.error.message) || String(r.stderr || '').slice(-400));
    }
  }

  return { checks, detail, whisper, probe: { durationSec: dur, video: { codec: v.codec_name, width: v.width, height: v.height, fps: v.r_frame_rate }, audio: { codec: a.codec_name, sampleRate: a.sample_rate } }, volume: vol, speech, frames: fr, spoken };
}

/* ------------------------------------------------------------------ ana akış */
(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  for (const f of ['video.mp4', 'speech.wav', 'audio-from-video.wav']) { try { fs.unlinkSync(path.join(OUT, f)); } catch { /* yok */ } }
  try { fs.rmSync(path.join(OUT, 'frames'), { recursive: true, force: true }); } catch { /* yok */ }
  const result = { label: LABEL, combo, startedAt: new Date().toISOString(), status: 'running' };
  const save = () => fs.writeFileSync(path.join(OUT, 'result.json'), JSON.stringify(result, null, 2));
  let code = 0;
  try {
    log('yöntem:', LABEL, JSON.stringify(combo));
    await ensureStack();
    await login();
    const agents = await api('GET', '/api/agents');
    const agent = (agents || []).find(x => x.isOnline);
    if (!agent) { throw new Error('Çevrimiçi ajan yok (stack.cjs start agent; eşleme: out/pair-code.txt).'); }
    result.agent = { version: agent.agentVersion, gpu: agent.gpuName, vramMb: agent.vramMb };
    const current = await api('GET', '/api/production/settings');
    const settings = settingsFor(current);
    await api('PUT', '/api/production/settings', settings);
    log('üretim ayarları yazıldı');

    let videoId = args['verify-only'] || null;
    if (!videoId) {
      const conceptId = await ensureConcept();
      result.conceptId = conceptId;
      const run = await produce(conceptId, settings);
      result.wallSec = Math.round(run.wallSec);
      if (run.item) {
        fs.writeFileSync(path.join(OUT, 'queue-item.json'), JSON.stringify(run.item, null, 2));
        result.jobId = run.item.jobId;
        result.stages = (run.item.stages || []).map(s => ({ stage: s.stage, status: s.status, location: s.location, ms: s.durationMs, reused: s.reused, message: s.message }));
        const st = run.item.startedAt ? Date.parse(run.item.startedAt) : null;
        const fn = run.item.finishedAt ? Date.parse(run.item.finishedAt) : null;
        result.renderSec = st && fn ? Math.round((fn - st) / 100) / 10 : null;
      }
      if (run.timeout || run.failed) {
        result.status = run.timeout ? 'timeout' : 'failed';
        result.error = run.item ? (run.item.message || '') + ' | ' + stageLine(run.item) : 'zaman aşımı';
        log('ÜRETİM BAŞARISIZ:', result.error);
        save();
        process.exitCode = 2;
        return;
      }
      videoId = run.item.videoId;
    }
    result.videoId = videoId;
    const ver = await verify(videoId, null, settings);
    Object.assign(result, { checks: ver.checks, whisper: ver.whisper ? { wordRecall: ver.whisper.wordRecall, transcript: ver.whisper.transcript } : null, probe: ver.probe, volume: ver.volume, speech: ver.speech, frames: (ver.frames || []).map(f => path.relative(OUT, f.file)) });
    result.passed = ver.checks.every(c => c.ok);
    result.status = result.passed ? 'ok' : 'checks-failed';
    code = result.passed ? 0 : 1;
  } catch (e) {
    result.status = 'error';
    result.error = e.message;
    log('HATA:', e.stack || e.message);
    code = 2;
  }
  result.finishedAt = new Date().toISOString();
  save();
  log(`sonuç: ${result.status}${result.renderSec ? ' · render ' + result.renderSec + ' sn' : ''} → ${path.relative(ROOT, OUT)}`);
  process.exitCode = code;
})();
