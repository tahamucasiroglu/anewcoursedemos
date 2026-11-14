/** Adım i: Web'den çıkış (logout-all) → ajan yetkisini kaybeder ve yeniden eşleme ister (ajan logu + yeni kod dosyası). */
const fs = require('fs');
const path = require('path');
const AGENT_LOG_DIR = path.resolve(__dirname, '../../../.tools-dev/e2e-agent/Logs');
const agentLog = () => fs.readdirSync(AGENT_LOG_DIR).filter(f => f.endsWith('.log')).map(f => fs.readFileSync(path.join(AGENT_LOG_DIR, f), 'utf8')).join('\n');

module.exports = async (ctx) => {
  const { page } = ctx;
  const before = agentLog().length;
  await ctx.goto('/Konseptler');
  await page.click('.tk-dropdown [data-dropdown-toggle]');
  await ctx.sleep(300);
  await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2', timeout: 15000 }).catch(() => {}), page.click('#tk-logout')]);
  await ctx.sleep(1000);
  ctx.check('web çıkışı → giriş sayfası', page.url().includes('/Account/Login'), page.url());
  await ctx.shot('cikis');
  const log = await ctx.waitFor(() => {
    const l = agentLog().slice(before);
    return /yeniden eşleme gerekiyor/.test(l) && /Eşleme kodu:/.test(l) ? l : null;
  }, 90000, 2000);
  const tail = log || agentLog().slice(before);
  fs.writeFileSync(path.join(ctx.out, 'i-agent-log.txt'), tail);
  ctx.check('ajan yetkisiz kaldı ve yeniden eşleme istedi (log)', !!log, (tail.match(/.*(Oturum kapandı|yetki|Unauthorized|401|yeniden eşleme|Eşleme kodu).*/g) || []).slice(0, 4).map(x => x.slice(30, 200)));
  const code = fs.readFileSync(path.join(ctx.out, 'pair-code.txt'), 'utf8').split(/\r?\n/)[0];
  ctx.check('yeni eşleme kodu üretildi', /^[A-Z0-9]{4}-[A-Z0-9]{4}$/.test(code), code ? 'kod var' : 'yok');
};
