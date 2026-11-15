/**
 * Taha Konsept e2e — yerel yığını başlatır / durdurur (yalnız tahakonsept_e2e veritabanı).
 *
 *   node tests/e2e/stack.cjs start [mock|api|web|all]   → arka planda başlatır, PID'leri out/pids.json'a yazar, sağlık bekler
 *   node tests/e2e/stack.cjs stop  [mock|api|web|all]   → başlattıklarını (süreç ağacıyla) durdurur
 *   node tests/e2e/stack.cjs status
 *   node tests/e2e/stack.cjs start agent              → masaüstü ajanı --headless (eşleme kodu out/pair-code.txt); 'everything' = dördü
 *
 * Portlar: mock LLM 7199, API 7191, Web 7190. Veri klasörü: tests/e2e/out/appdata-{api,web} (git dışı).
 * Bağlantı: Host=localhost;Port=5433;Database=tahakonsept_e2e;Username=tahakonsept — parola host tarafından TAHA/.env'den
 * eklenir (bu betik okumaz, yazdırmaz). Gerçek 'tahakonsept' veritabanı ASLA kullanılmaz (aşağıdaki koruma).
 * Yönetici tohumu: API'ye SAHTE e2e yöneticisi verilir (lib/e2e-admin.cjs → out/e2e-admin.json); gerçek .env özeti kullanılmaz.
 * Önkoşul: `dotnet build TahaKonsept.slnx` (Debug) ve 5433 tüneli (scripts/dev-tunnel.ps1).
 */
'use strict';
const { spawn, execSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const http = require('http');
const { e2eAdmin } = require('./lib/e2e-admin.cjs');

const ROOT = path.resolve(__dirname, '..', '..');
const OUT = path.join(__dirname, 'out');
const PIDS = path.join(OUT, 'pids.json');
const CONN = 'Host=localhost;Port=5433;Database=tahakonsept_e2e;Username=tahakonsept';
if (!/Database=tahakonsept_e2e;/.test(CONN)) { throw new Error('e2e yalnız tahakonsept_e2e veritabanıyla çalışır.'); }

const SERVICES = {
  mock: {
    cmd: process.execPath,
    args: [path.join(__dirname, 'mock-llm', 'server.cjs'), '--port', '7199', '--log', path.join(OUT, 'mock-llm.log')],
    cwd: __dirname,
    env: {},
    health: 'http://127.0.0.1:7199/v1/models',
  },
  api: {
    cmd: 'dotnet',
    args: [path.join(ROOT, 'src/Hosts/TahaKonsept.Api/bin/Debug/net10.0/TahaKonsept.Api.dll')],
    cwd: path.join(ROOT, 'src/Hosts/TahaKonsept.Api'),
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ConnectionStrings__Main: CONN,
      Kestrel__Endpoints__Http__Url: 'http://0.0.0.0:7191',
      App__DataDirectory: path.join(OUT, 'appdata-api'),
      Agents__WebPort: '7190',
      // SAHTE e2e yöneticisi (out/e2e-admin.json); gerçek .env yöneticisi e2e veritabanına girmesin diye her zaman verilir.
      TAHAKONSEPT_ADMIN_EMAIL: e2eAdmin().email,
      TAHAKONSEPT_ADMIN_PASSWORD_HASH: e2eAdmin().hash,
    },
    health: 'http://127.0.0.1:7191/api/health',
  },
  web: {
    cmd: 'dotnet',
    args: [path.join(ROOT, 'src/Hosts/TahaKonsept.Web/bin/Debug/net10.0/TahaKonsept.Web.dll')],
    cwd: path.join(ROOT, 'src/Hosts/TahaKonsept.Web'),
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      Kestrel__Endpoints__Http__Url: 'http://0.0.0.0:7190',
      Api__BaseUrl: 'http://127.0.0.1:7191',
      App__DataDirectory: path.join(OUT, 'appdata-web'),
    },
    health: 'http://127.0.0.1:7190/',
  },
  agent: {
    // Gerçek masaüstü ajanı, penceresiz geliştirici modunda (--headless). Kök: .tools-dev/e2e-agent (araçlar bağlantılarla).
    cmd: path.join(ROOT, 'src/Desktop/TahaKonsept.Agent/bin/Debug/net10.0-windows/TahaKonsept.Agent.exe'),
    args: ['--headless', '--server', 'http://localhost:7191', '--pair-code-out', path.join(OUT, 'pair-code.txt'), '--stop-file', path.join(OUT, 'agent.stop')],
    cwd: path.join(ROOT, 'src/Desktop/TahaKonsept.Agent/bin/Debug/net10.0-windows'),
    env: { TAHAKONSEPT_AGENT_ROOT: path.join(ROOT, '.tools-dev', 'e2e-agent'), TAHAKONSEPT_AGENT_VIDEO_PORT: '7120' },
    health: 'http://127.0.0.1:7120/',
  },
};

function readPids() { try { return JSON.parse(fs.readFileSync(PIDS, 'utf8')); } catch { return {}; } }
function writePids(p) { fs.mkdirSync(OUT, { recursive: true }); fs.writeFileSync(PIDS, JSON.stringify(p, null, 2)); }
function alive(pid) { try { process.kill(pid, 0); return true; } catch { return false; } }

function get(url) {
  return new Promise(resolve => {
    const req = http.get(url, res => { res.resume(); resolve(res.statusCode); });
    req.on('error', () => resolve(0));
    req.setTimeout(3000, () => { req.destroy(); resolve(0); });
  });
}

async function waitHealthy(name, timeoutMs) {
  const until = Date.now() + timeoutMs;
  while (Date.now() < until) {
    const code = await get(SERVICES[name].health);
    if (code > 0 && code < 500) { return code; }
    await new Promise(r => setTimeout(r, 1000));
  }
  return 0;
}

async function start(name) {
  const pids = readPids();
  if (pids[name] && alive(pids[name])) { console.log(`${name}: zaten çalışıyor (PID ${pids[name]})`); return; }
  const s = SERVICES[name];
  for (const v of Object.values(s.env)) { if (typeof v === 'string' && v.startsWith(OUT)) { fs.mkdirSync(v, { recursive: true }); } }
  fs.mkdirSync(OUT, { recursive: true });
  const log = fs.openSync(path.join(OUT, `${name}.out.log`), 'a');
  const child = spawn(s.cmd, s.args, { cwd: s.cwd, env: { ...process.env, ...s.env }, detached: true, stdio: ['ignore', log, log], windowsHide: true });
  child.unref();
  pids[name] = child.pid;
  writePids(pids);
  const code = await waitHealthy(name, name === 'mock' ? 10000 : name === 'agent' ? 60000 : 180000);
  console.log(`${name}: PID ${child.pid} → ${code ? 'hazır (HTTP ' + code + ')' : 'SAĞLIK YANITI YOK, bkz. out/' + name + '.out.log'}`);
}

function stop(name) {
  const pids = readPids();
  const pid = pids[name];
  if (!pid) { console.log(`${name}: kayıt yok`); return; }
  if (name === 'agent' && alive(pid)) {
    // Nazik kapanış: durdurma dosyası → ajan işleri bırakıp hub'ı kapatır.
    fs.writeFileSync(path.join(OUT, 'agent.stop'), 'stop');
    const until = Date.now() + 20000;
    while (alive(pid) && Date.now() < until) { execSync('ping -n 2 127.0.0.1', { stdio: 'ignore' }); }
    if (!alive(pid)) { console.log(`${name}: düzgün kapandı (PID ${pid})`); delete pids[name]; writePids(pids); return; }
  }
  if (alive(pid)) {
    try { execSync(`taskkill /PID ${pid} /T /F`, { stdio: 'ignore' }); } catch { /* zaten bitmiş */ }
    console.log(`${name}: durduruldu (PID ${pid})`);
  } else {
    console.log(`${name}: zaten durmuş`);
  }
  delete pids[name];
  writePids(pids);
}

(async () => {
  const [action = 'status', which = 'all'] = process.argv.slice(2);
  const names = which === 'all' ? ['mock', 'api', 'web'] : which === 'everything' ? ['mock', 'api', 'web', 'agent'] : which.split(',');
  if (action === 'start') { for (const n of names) { await start(n); } }
  else if (action === 'stop') { for (const n of names.reverse()) { stop(n); } }
  else {
    const pids = readPids();
    for (const n of Object.keys(SERVICES)) { console.log(`${n}: ${pids[n] && alive(pids[n]) ? 'çalışıyor PID ' + pids[n] : 'durmuş'} · sağlık ${await get(SERVICES[n].health)}`); }
  }
})();
