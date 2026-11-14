/** Adım h: Yönetim paneli (video/konsept sayıları, etkinlik günlükleri), Günlük şeridi, Hata bildir penceresi gönderimi. */
module.exports = async (ctx) => {
  const { page } = ctx;
  await ctx.goto('/Admin');
  await ctx.sleep(2500);
  const tiles = await page.evaluate(() => ({
    concepts: (document.querySelector('#ad-concepts') || {}).textContent,
    videos: (document.querySelector('#ad-videos') || {}).textContent,
    users: (document.querySelector('#ad-users') || {}).textContent,
  }));
  await page.click('#tk-admin .tk-tab[data-tab="gunluk"]');
  await ctx.sleep(2000);
  tiles.logs = await page.$eval('#ad-logs', e => e.innerText.replace(/\s+/g, ' ').slice(0, 400));
  await ctx.shot('admin-gunluk');
  await page.click('#tk-admin .tk-tab[data-tab="genel"]');
  const n = s => Number(String(s || '').replace(/[^\d]/g, '')) || 0;
  ctx.check('panel: konsept sayısı > 0', n(tiles.concepts) > 0, tiles.concepts);
  ctx.check('panel: video sayısı > 0', n(tiles.videos) > 0, tiles.videos);
  ctx.check('panel: etkinlik günlükleri listeleniyor', /Video|Konsept|Production|Concepts/i.test(tiles.logs), tiles.logs.slice(0, 200));
  await ctx.shot('admin', { fullPage: true });

  // Günlük şeridi
  await ctx.goto('/Konseptler');
  await ctx.sleep(1500);
  const strip = await page.$eval('#tk-journal', e => e.innerText.replace(/\s+/g, ' '));
  ctx.check('Günlük şeridinde son işlem', strip.length > 10 && !/Henüz işlem yok/i.test(strip), strip.slice(0, 200));
  await page.click('#tk-journal');
  await ctx.sleep(800);
  const panel = await page.$eval('#tk-journal-panel', e => e.innerText.replace(/\s+/g, ' ').slice(0, 300));
  ctx.check('Günlük paneli açılıyor', panel.length > 20, panel.slice(0, 160));
  await ctx.shot('gunluk');
  await page.click('#tk-journal');

  // Hata bildir
  await page.click('#tk-bug');
  await ctx.modal();
  await page.type('#tk-bug-msg', 'E2E: hata bildirimi deneme mesajı (otomatik test).');
  await ctx.shot('hata-bildir');
  let sent = null;
  const onResp = r => { if (r.url().includes('/HataBildir/Gonder')) { sent = r.status(); } };
  page.on('response', onResp);
  await ctx.clickText('.tk-modal-foot button', 'Gönder');
  await ctx.waitFor(() => sent, 15000, 300);
  page.off('response', onResp);
  ctx.check('Hata bildir gönderildi (HTTP 2xx)', sent >= 200 && sent < 300, sent);
  await ctx.sleep(1000);
  await ctx.goto('/Admin');
  await ctx.sleep(2000);
  await page.click('#tk-admin .tk-tab[data-tab="hatalar"]');
  await ctx.sleep(2000);
  const reports = await page.$eval('#ad-reports', e => e.innerText.replace(/\s+/g, ' '));
  await ctx.shot('admin-hatalar');
  ctx.check('rapor yönetim panelinde', /E2E: hata bildirimi/.test(reports), reports.slice(0, 200));
};
