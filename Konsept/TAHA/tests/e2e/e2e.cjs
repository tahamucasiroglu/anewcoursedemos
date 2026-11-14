/**
 * Taha Konsept e2e — betikli senaryo koşucusu (puppeteer-core + sistem Chrome, headless). .NET derlemesinin dışındadır.
 *
 *   node tests/e2e/e2e.cjs <adım> [<adım> …]     ör. node tests/e2e/e2e.cjs a b c
 *   node tests/e2e/e2e.cjs pages                  → tüm sayfaların açık/koyu ekran görüntüsü
 *
 * Adımlar tests/e2e/steps/<ad>.cjs dosyalarıdır: `module.exports = async (ctx) => { … }`.
 * ctx: { page, browser, base, shot(name), api.get/post/put/del (sayfa içinden TK.api — $.ajax), check(ad, koşul, ayrıntı),
 *        sleep(ms), waitFor(fn, ms), out, env (yalnızca .env'deki e2e değişkenleri; asla yazdırılmaz), log(...) }.
 * Ekran görüntüleri ve sonuçlar tests/e2e/out/ altına (git dışı): <adım>-<ad>.png, results.json, issues.txt.
 * Önkoşul: `node tests/e2e/stack.cjs start` (mock 7199, API 7191, Web 7190; yalnız tahakonsept_e2e).
 */
'use strict';
const fs = require('fs');
const path = require('path');
const puppeteer = require(path.resolve(__dirname, '../../.tools-dev/agent-tools/engines/hyperframes/node_modules/puppeteer-core'));

const BASE = process.env.E2E_BASE || 'http://localhost:7190';
const OUT = path.join(__dirname, 'out');
const CHROME = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
fs.mkdirSync(OUT, { recursive: true });

const env = Object.fromEntries(fs.readFileSync(path.resolve(__dirname, '../../.env'), 'utf8')
  .split(/\r?\n/).filter(l => l.includes('=') && !l.trim().startsWith('#'))
  .map(l => [l.slice(0, l.indexOf('=')).trim(), l.slice(l.indexOf('=') + 1).trim()])
  .filter(([k]) => k.startsWith('TAHAKONSEPT_E2E_')));

const sleep = ms => new Promise(r => setTimeout(r, ms));

async function login(page) {
  await page.goto(BASE + '/', { waitUntil: 'networkidle2' });
  if (page.url().includes('/Account/Login')) {
    await page.type('#login-user', env.TAHAKONSEPT_E2E_USER);
    await page.type('#login-pass', env.TAHAKONSEPT_E2E_PASSWORD);
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }).catch(() => {}), page.click('button[type=submit]')]);
    await sleep(1000);
  }
  if (page.url().includes('/Account/Login') || page.url().includes('/Kurulum')) { throw new Error('Giriş başarısız: ' + page.url()); }
}

(async () => {
  const steps = process.argv.slice(2);
  if (!steps.length) { console.log('Kullanım: node tests/e2e/e2e.cjs <adım> …'); process.exit(2); }
  const browser = await puppeteer.launch({
    executablePath: CHROME, headless: true,
    args: ['--window-size=1600,1000', '--autoplay-policy=no-user-gesture-required'], defaultViewport: { width: 1600, height: 1000 },
  });
  const page = await browser.newPage();
  const issues = [];
  page.on('console', m => { if (['error', 'warning'].includes(m.type())) { issues.push(`[console.${m.type()}] ${page.url()} :: ${m.text()}`); } });
  page.on('pageerror', e => issues.push(`[pageerror] ${page.url()} :: ${e.message}`));
  page.on('response', r => { if (r.status() >= 400) { issues.push(`[http ${r.status()}] ${r.request().method()} ${r.url()}`); } });
  page.on('requestfailed', r => { if (!/\.mp4|media|video/i.test(r.url())) { issues.push(`[reqfail] ${r.url()} ${r.failure()?.errorText}`); } });
  page.on('dialog', d => d.accept().catch(() => {}));

  const resultsFile = path.join(OUT, 'results.json');
  const results = fs.existsSync(resultsFile) ? JSON.parse(fs.readFileSync(resultsFile, 'utf8')) : {};
  let current = '';
  const ctx = {
    page, browser, base: BASE, out: OUT, env, sleep,
    log: (...a) => console.log(`[${current}]`, ...a),
    async shot(name, opts) { const f = path.join(OUT, `${current}-${name}.png`); await page.screenshot({ path: f, ...(opts || {}) }); console.log(`[${current}] 📷 ${path.basename(f)}`); return f; },
    check(name, ok, detail) {
      (results[current] = results[current] || []).push({ name, ok: !!ok, detail: detail === undefined ? null : detail, at: new Date().toISOString() });
      console.log(`[${current}] ${ok ? '✅' : '❌'} ${name}${detail !== undefined ? ' — ' + (typeof detail === 'string' ? detail : JSON.stringify(detail)) : ''}`);
      return !!ok;
    },
    async waitFor(fn, ms, every) {
      const until = Date.now() + (ms || 30000);
      let last;
      while (Date.now() < until) { last = await fn(); if (last) { return last; } await sleep(every || 1000); }
      return last;
    },
    async goto(p) { await page.goto(BASE + p, { waitUntil: 'networkidle2' }); await sleep(800); },
    api: {
      call(method, url, body) {
        return page.evaluate((m, u, b) => new Promise(resolve => {
          const fn = { GET: 'get', POST: 'post', PUT: 'put', DELETE: 'del' }[m];
          window.TK.api[fn](u, b === null ? undefined : b, { silent: true })
            .done(d => resolve({ ok: true, data: d === undefined ? null : d }))
            .fail(x => resolve({ ok: false, status: x && x.status, text: x && x.responseText ? String(x.responseText).slice(0, 800) : null }));
        }), method, url, body === undefined ? null : body);
      },
      get(u, b) { return this.call('GET', u, b); },
      post(u, b) { return this.call('POST', u, b); },
      put(u, b) { return this.call('PUT', u, b); },
      del(u, b) { return this.call('DELETE', u, b); },
    },
    /** Görünür modalı bekler ve döndürür. */
    async modal(ms) { await page.waitForSelector('.tk-modal-backdrop.is-open .tk-modal-body', { visible: true, timeout: ms || 10000 }); await sleep(400); },
    async clickText(selector, text) {
      const ok = await page.evaluate((s, t) => {
        const el = [...document.querySelectorAll(s)].find(e => e.offsetParent !== null && e.textContent.trim().includes(t));
        if (el) { el.click(); return true; }
        return false;
      }, selector, text);
      if (!ok) { throw new Error(`Tıklanacak öğe yok: ${selector} "${text}"`); }
      await sleep(300);
    },
  };

  try {
    await login(page);
    for (const step of steps) {
      current = step;
      results[step] = [];
      console.log(`\n=== Adım ${step} ===`);
      const file = path.join(__dirname, 'steps', `${step}.cjs`);
      try {
        await require(file)(ctx);
      } catch (e) {
        ctx.check('adım istisnasız bitti', false, e.stack ? e.stack.split('\n').slice(0, 4).join(' | ') : String(e));
        await ctx.shot('hata').catch(() => {});
      }
      fs.writeFileSync(resultsFile, JSON.stringify(results, null, 2));
    }
  } finally {
    const uniq = [...new Set(issues)];
    fs.writeFileSync(path.join(OUT, 'issues.txt'), uniq.join('\n'));
    console.log('\nSORUNLAR (konsol/HTTP):\n' + (uniq.join('\n') || '(yok)'));
    await browser.close();
  }
})().catch(e => { console.error(e); process.exit(1); });
