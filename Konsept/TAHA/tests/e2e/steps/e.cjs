/**
 * Adım e: gerçek masaüstü ajanı (--headless, stack.cjs start agent) eşleme kodunu out/pair-code.txt'ye yazar;
 * kod Web'den (Ayarlar → Masaüstü programı → Kodu gir) onaylanır; ajan çevrimiçi görünür.
 */
const fs = require('fs');
const path = require('path');

module.exports = async (ctx) => {
  const { page } = ctx;
  const codeFile = path.join(ctx.out, 'pair-code.txt');
  const code = await ctx.waitFor(() => {
    const t = fs.existsSync(codeFile) ? fs.readFileSync(codeFile, 'utf8').split(/\r?\n/)[0].trim() : '';
    return /^[A-Z0-9]{4}-[A-Z0-9]{4}$/.test(t) || t === 'PAIRED' ? t : null;
  }, 60000);
  ctx.check('ajan eşleme kodu üretti', !!code, code === 'PAIRED' ? 'zaten eşli' : 'kod alındı');
  if (code && code !== 'PAIRED') {
    await ctx.goto('/Ayarlar#masaustu');
    await page.waitForSelector('#st-agent-form [name=userCode]', { timeout: 10000 });
    await page.type('#st-agent-form [name=userCode]', code);
    await page.click('#st-agent-form [type=submit]');
    const paired = await ctx.waitFor(() => (fs.readFileSync(codeFile, 'utf8').startsWith('PAIRED') ? true : null), 30000);
    ctx.check('ajan eşlendi (kod dosyası PAIRED)', !!paired);
  }
  const online = await ctx.waitFor(async () => {
    const r = await ctx.api.get('/KonsepttenVideo/Ajanlar');
    const list = r.ok ? (r.data.items || r.data) : [];
    return list.find(a => a.isOnline || a.online || a.connected) || null;
  }, 40000, 2000);
  ctx.check('ajan çevrimiçi (hub bağlı)', !!online, online ? { machine: online.machineName, vram: online.vramMb || online.gpuVramMb, capabilities: online.capabilities ? Object.keys(online.capabilities) : null } : null);
  await ctx.goto('/Ayarlar#masaustu');
  await ctx.sleep(1500);
  const el = await page.$('#masaustu');
  if (el) { await el.screenshot({ path: path.join(ctx.out, 'e-masaustu.png') }); }
};
