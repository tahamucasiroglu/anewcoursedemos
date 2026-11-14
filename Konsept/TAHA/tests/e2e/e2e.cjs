const puppeteer = require('C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/.tools-dev/agent-tools/engines/hyperframes/node_modules/puppeteer-core');
const fs = require('fs');
const BASE = 'http://localhost:7190';
const env = Object.fromEntries(fs.readFileSync('C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/.env', 'utf8')
  .split(/\r?\n/).filter(l => l.includes('=') && !l.startsWith('#'))
  .map(l => [l.slice(0, l.indexOf('=')), l.slice(l.indexOf('=') + 1)]));
const USER = env.TAHAKONSEPT_E2E_USER, PASS = env.TAHAKONSEPT_E2E_PASSWORD;
const mode = process.argv[2] || 'pages';
const sleep = ms => new Promise(r => setTimeout(r, ms));

(async () => {
  const browser = await puppeteer.launch({
    executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true,
    args: ['--window-size=1600,1000'], defaultViewport: { width: 1600, height: 1000 },
  });
  const page = await browser.newPage();
  const issues = [];
  page.on('console', m => { if (['error', 'warning'].includes(m.type())) issues.push(`[console.${m.type()}] ${page.url()} :: ${m.text()}`); });
  page.on('pageerror', e => issues.push(`[pageerror] ${page.url()} :: ${e.message}`));
  page.on('response', r => { if (r.status() >= 400) issues.push(`[http ${r.status()}] ${r.request().method()} ${r.url()}`); });
  page.on('requestfailed', r => issues.push(`[reqfail] ${r.url()} ${r.failure()?.errorText}`));

  await page.goto(BASE + '/', { waitUntil: 'networkidle2' });
  console.log('landed', page.url());
  if (page.url().includes('/Kurulum')) {
    const fields = await page.$$eval('input,button', els => els.map(e => `${e.tagName} id=${e.id} name=${e.name} type=${e.type}`));
    console.log(fields.join('\n'));
    await page.screenshot({ path: 'kurulum.png' });
    if (mode === 'setup') {
      const inputs = await page.$$('input[type=text],input:not([type])');
      const pw = await page.$$('input[type=password]');
      await inputs[0].type(USER);
      for (const p of pw) await p.type(PASS);
      await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }).catch(() => {}), page.click('button[type=submit]')]);
      await sleep(2000);
      console.log('after setup', page.url());
    }
  } else if (page.url().includes('/Account/Login')) {
    await page.type('input[type=text]', USER);
    await page.type('input[type=password]', PASS);
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }).catch(() => {}), page.click('button[type=submit]')]);
    await sleep(1500);
    console.log('after login', page.url());
  }

  if (mode === 'pages' || mode === 'setup') {
    const pages = ['/Konseptler', '/KonsepttenVideo', '/Uretilenler', '/Silinenler', '/Yayinlama', '/Ayarlar', '/Admin'];
    for (const theme of ['light', 'dark']) {
      for (const p of pages) {
        await page.goto(BASE + p, { waitUntil: 'networkidle2' }).catch(e => issues.push('[nav] ' + p + ' ' + e.message));
        await page.evaluate(t => document.documentElement.setAttribute('data-theme', t), theme);
        await sleep(1500);
        await page.screenshot({ path: `${theme}${p.replace(/\//g, '_')}.png` });
      }
    }
    await page.setViewport({ width: 390, height: 844 });
    for (const p of ['/Konseptler', '/KonsepttenVideo']) {
      await page.goto(BASE + p, { waitUntil: 'networkidle2' });
      await page.screenshot({ path: `mobile${p.replace(/\//g, '_')}.png` });
    }
  }
  if (mode.startsWith('js:')) {
    const fn = require('./' + mode.slice(3));
    await fn(page, BASE, issues, sleep);
  }
  const uniq = [...new Set(issues)];
  fs.writeFileSync('issues.txt', uniq.join('\n'));
  console.log('ISSUES:\n' + uniq.join('\n'));
  await browser.close();
})().catch(e => { console.error(e); process.exit(1); });
