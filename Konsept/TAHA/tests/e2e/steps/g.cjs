/**
 * Adım g: Üretilenler — sekmeler (seslendirme/konsept/ayrıntı+ücret), Yeni sürüm (yalnız altyazı kapalı) → yalnız Compose/Mux
 * yeniden çalışır, v2 oluşur; Klasörde aç / Sistem oynatıcısı komutları ajana ulaşır (ajan logu); v2 Sil.
 */
const fs = require('fs');
const path = require('path');
const items = r => (r && r.ok ? (r.data && r.data.items ? r.data.items : r.data) : []) || [];
const AGENT_LOG_DIR = path.resolve(__dirname, '../../../.tools-dev/e2e-agent/Logs');
const agentLog = () => fs.readdirSync(AGENT_LOG_DIR).filter(f => f.endsWith('.log')).map(f => fs.readFileSync(path.join(AGENT_LOG_DIR, f), 'utf8')).join('\n');

module.exports = async (ctx) => {
  const { page } = ctx;
  const videoId = fs.readFileSync(path.join(ctx.out, 'video-id.txt'), 'utf8').trim();
  await ctx.goto('/Uretilenler?video=' + videoId);
  await page.waitForSelector('#ur-player video', { timeout: 20000 }).catch(() => {});
  await ctx.sleep(2500);
  await ctx.shot('sayfa');

  // --- Sekmeler
  for (const [tab, name] of [['scenes', 'seslendirme'], ['concept', 'konsept'], ['details', 'ayrinti']]) {
    await page.click(`#tk-uretilenler .tk-tab[data-tab="${tab}"]`);
    await ctx.sleep(600);
    const text = await page.$eval(`#tk-uretilenler .tk-tab-panel[data-panel="${tab}"]`, e => e.innerText.replace(/\s+/g, ' ').slice(0, 400));
    ctx.check(`sekme "${name}" dolu`, text.length > 40, text.slice(0, 160));
    if (tab === 'details') {
      ctx.check('ücret sekmesinde aşamalar/ücret', /\$|ücret|Compose|Birleştirme|Render/i.test(text), text.slice(0, 200));
      await ctx.shot('ayrinti-ucret');
    }
  }
  const detail1 = (await ctx.api.get('/Uretilenler/Detay/' + videoId)).data;
  fs.writeFileSync(path.join(ctx.out, 'g-detail-v1.json'), JSON.stringify(detail1, null, 2));

  // --- Klasörde aç / Sistem oynatıcısı → ajan logunda komut
  const before = agentLog().length;
  await page.click('#ur-actions [data-act="folder"]');
  await ctx.sleep(2500);
  await page.click('#ur-actions [data-act="system"]');
  await ctx.sleep(3000);
  const newLog = agentLog().slice(before);
  // Açılan pencereleri kapat (sistem oynatıcısı + Videos klasörü penceresi); masaüstünde iz bırakma.
  try {
    require('child_process').execFileSync('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', path.join(__dirname, '..', 'close-windows.ps1')], { stdio: 'ignore', timeout: 20000 });
  } catch { /* yoksay */ }
  fs.writeFileSync(path.join(ctx.out, 'g-agent-log-commands.txt'), newLog);
  ctx.check('Klasörde aç komutu ajana ulaştı', /Sunucu komutu işlendi: OpenFolder/.test(newLog), (newLog.match(/.*OpenFolder.*/) || [''])[0].slice(0, 200));
  ctx.check('Sistem oynatıcısı komutu ajana ulaştı', /Sunucu komutu işlendi: OpenInSystemPlayer/.test(newLog), (newLog.match(/.*OpenInSystemPlayer.*/) || [''])[0].slice(0, 200));

  // --- Yeni sürüm: yalnız altyazıyı kapat
  const beforeIds = new Set((((await ctx.api.get('/KonsepttenVideo/Sira')).data || {}).items || []).map(i => String(i.id)));
  await page.click('#ur-actions [data-act="version"]');
  await ctx.modal(15000);
  await page.click('.tk-modal [data-vtab="compose"]');
  await ctx.sleep(400);
  const toggled = await page.evaluate(() => {
    const panel = document.querySelector('.tk-modal [data-vpanel="compose"]');
    const box = [...panel.querySelectorAll('input[type=checkbox]')].find(i => /altyazı/i.test((i.closest('label') || i.parentElement).textContent));
    if (!box) { return null; }
    box.click();
    return box.checked;
  });
  await ctx.sleep(600);
  const rerun = await page.$eval('.tk-modal .ur-rerun', e => e.innerText.replace(/\s+/g, ' '));
  ctx.check('tahmin: yalnız birleştirme aşamaları yeniden', toggled === false && /Yeniden çalışacak/.test(rerun), rerun.slice(0, 250));
  await ctx.shot('yeni-surum-modal');
  await ctx.clickText('.tk-modal-foot button', 'Yeni sürümü üret');
  await ctx.sleep(2000);

  // Sıra duruyorsa başlat
  const q = (await ctx.api.get('/KonsepttenVideo/Sira')).data;
  if (q && /idle|paused/i.test(q.status)) { await ctx.api.post('/KonsepttenVideo/Baslat'); }
  const v2 = await ctx.waitFor(async () => {
    const qq = (await ctx.api.get('/KonsepttenVideo/Sira')).data || {};
    const it = (qq.items || []).find(i => !beforeIds.has(String(i.id)) && String(i.parentVideoId) === videoId);
    if (it && /Failed/i.test(it.status)) { return { failed: it }; }
    return it && it.status === 'Done' && it.videoId ? it : null;
  }, 10 * 60000, 3000);
  ctx.check('v2 video hazır', v2 && !v2.failed, v2 && (v2.failed ? v2.failed : { video: v2.videoId }));
  if (!v2 || v2.failed) { return; }
  const detail2 = (await ctx.api.get('/Uretilenler/Detay/' + v2.videoId)).data;
  fs.writeFileSync(path.join(ctx.out, 'g-detail-v2.json'), JSON.stringify(detail2, null, 2));
  const runs = JSON.stringify(detail2);
  const stages = (detail2.costBreakdown || []).map(s => ({ stage: s.stage, reused: s.reused }));
  ctx.log('v2 aşamaları', JSON.stringify(stages));
  const rerunStages = stages.filter(s => s.reused === false).map(s => s.stage);
  ctx.check('v2: yalnız Compose/Mux yeniden çalıştı', rerunStages.length > 0 && rerunStages.every(s => /^(Compose|Mux)$/.test(String(s))), { rerun: rerunStages, all: stages.length });
  ctx.check('v2 sürüm numarası 2', Number(detail2.version || (detail2.video && detail2.video.version)) === 2 || /"version":2/.test(runs));

  // --- v2 oynat + sil
  await ctx.goto('/Uretilenler?video=' + v2.videoId);
  await ctx.sleep(3000);
  await ctx.shot('v2');
  await page.click('#ur-actions [data-act="delete"]');
  await ctx.modal();
  await ctx.clickText('.tk-modal-foot button', 'Sil');
  await ctx.sleep(2000);
  const left = items(await ctx.api.get('/Uretilenler/Liste'));
  ctx.check('v2 silindi', !left.some(v => String(v.id) === String(v2.videoId)), left.map(v => v.version));
};
