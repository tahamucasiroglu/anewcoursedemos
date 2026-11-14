/**
 * Adım f: Konseptten Video — ayarlar (HyperFrames, görsel kapalı, yerel Supertonic 3, altyazı + ilerleme çubuğu açık),
 * onaylı konsepti sıraya ekle → Sırayla üret → aşamalar Hazır'a; bir kez Durdur + Kaldığı yerden devam; mp4 diskte, oynatıcı yükler.
 */
const fs = require('fs');
const path = require('path');
const items = r => (r && r.ok ? (r.data && r.data.items ? r.data.items : r.data) : []) || [];

module.exports = async (ctx) => {
  const { page } = ctx;
  await ctx.goto('/KonsepttenVideo');

  // --- Motor: HyperFrames
  await page.click('[data-model-kind="engine"]');
  await ctx.modal();
  await page.evaluate(() => document.querySelector('.tk-modal input[name=engine][value="hyperframes"]').click());
  await page.click('.tk-modal-head .tk-mini-btn');
  await ctx.sleep(1200);

  // --- Görsel: Kapalı
  await page.click('[data-model-kind="image"]');
  await ctx.modal();
  await page.click('.tk-modal .kv-seg button[data-mode="Off"]');
  await ctx.sleep(300);
  await page.click('.tk-modal-head .tk-mini-btn');
  await ctx.sleep(1200);

  // --- Ses: Yerel → Supertonic 3
  await page.click('[data-model-kind="speech"]');
  await ctx.modal();
  await page.click('.tk-modal .kv-seg button[data-mode="Local"]');
  await ctx.sleep(500);
  await page.evaluate(() => { const r = document.querySelector('.tk-modal input[name=speech][value="supertonic-3"]'); if (r) { r.click(); } });
  await ctx.sleep(1500);
  await ctx.shot('ses-modal');
  await page.click('.tk-modal-head .tk-mini-btn');
  await ctx.sleep(1200);

  // --- Altyazı + ilerleme çubuğu açık
  for (const id of ['kv-subtitles', 'kv-progressbar']) {
    if (!(await page.$eval('#' + id, e => e.checked))) { await page.$eval('#' + id, e => e.click()); await ctx.sleep(800); }
  }
  const s = (await ctx.api.get('/KonsepttenVideo/Ayarlar')).data || {};
  ctx.check('ayarlar: hyperframes, görsel kapalı, yerel supertonic-3, altyazı+çubuk', String(s.videoEngine).toLowerCase() === 'hyperframes'
    && /off/i.test(String(s.imageMode)) && /local/i.test(String(s.speechMode)) && s.speechModelKey === 'supertonic-3' && s.burnSubtitles && s.showProgressBar,
    { engine: s.videoEngine, image: s.imageMode, speech: s.speechMode, speechModel: s.speechModelKey, voice: s.voiceId, subs: s.burnSubtitles, bar: s.showProgressBar, code: s.codeModelKey });
  await ctx.shot('ayarlar');

  // --- Onaylı konsepti sıraya ekle
  const approvedId = fs.readFileSync(path.join(ctx.out, 'approved-id.txt'), 'utf8').trim();
  await ctx.goto('/KonsepttenVideo');
  await page.waitForSelector(`#kv-approved [data-id="${approvedId}"] [data-check]`, { timeout: 15000 });
  await page.click(`#kv-approved [data-id="${approvedId}"] [data-check]`);
  await page.click('#kv-add-queue');
  await ctx.sleep(1500);
  const q0 = items(await ctx.api.get('/KonsepttenVideo/Sira'));
  ctx.log('sıra', JSON.stringify((await ctx.api.get('/KonsepttenVideo/Sira')).data).slice(0, 400));
  await page.click('#kv-start');
  const t0 = Date.now();
  await ctx.sleep(3000);
  await ctx.shot('sira-basladi');

  // --- Bir kez Durdur + Devam (render/ses aşamalarından biri sürerken)
  const queueState = async () => (await ctx.api.get('/KonsepttenVideo/Sira')).data;
  const stagesOf = q => JSON.stringify(q).match(/"stage":"[^"]+","status":"[^"]+"/g) || [];
  let paused = false;
  const pauseAt = await ctx.waitFor(async () => {
    const q = await queueState();
    const txt = JSON.stringify(q);
    return /SceneCode|Speech|Render|Normalize/i.test(txt) && /Running/i.test(txt) ? q : null;
  }, 120000, 1500);
  if (pauseAt) {
    await page.click('#kv-pause');
    await ctx.sleep(4000);
    await ctx.shot('durduruldu');
    const pq = await queueState();
    paused = /paused|durdur/i.test(JSON.stringify(pq));
    ctx.check('Durdur: sıra duraklatıldı', paused, JSON.stringify(pq).slice(0, 300));
    await page.waitForSelector('#kv-resume:not([hidden])', { timeout: 30000 }).catch(() => {});
    await page.click('#kv-resume');
    await ctx.sleep(2000);
    const rq = await queueState();
    ctx.check('Kaldığı yerden devam', !/"run(State|Status)?":"paused"/i.test(JSON.stringify(rq)), JSON.stringify(rq).slice(0, 300));
  } else {
    ctx.check('Durdur/Devam denendi', false, 'çalışan aşama yakalanamadı');
  }

  // --- Hazır olana kadar izle (en çok 25 dk)
  let last = '';
  const done = await ctx.waitFor(async () => {
    const q = await queueState();
    const txt = JSON.stringify(q);
    const snap = (txt.match(/"(currentStage|stage|status|message)":"[^"]*"/g) || []).slice(0, 12).join(' ');
    if (snap !== last) { ctx.log(Math.round((Date.now() - t0) / 1000) + 's', snap.slice(0, 300)); last = snap; }
    if (/"status":"(Failed|Error)"/i.test(txt) || /"state":"failed"/i.test(txt)) { return { failed: true, q }; }
    const vids = items(await ctx.api.get('/KonsepttenVideo/Uretilenler'));
    const item = (q.items || []).find(i => String(i.conceptId) === approvedId);
    const ready = item && item.status === 'Done' && item.videoId ? vids.find(v => String(v.id) === String(item.videoId)) : null;
    return ready ? { ready, q } : null;
  }, 25 * 60000, 5000);
  await ctx.shot('son-durum');
  if (!done || done.failed) {
    ctx.check('video Hazır', false, done ? JSON.stringify(done.q).slice(0, 1500) : 'zaman aşımı');
    return;
  }
  ctx.check('video Hazır', true, { id: done.ready.id, sec: Math.round((Date.now() - t0) / 1000) });
  fs.writeFileSync(path.join(ctx.out, 'video-id.txt'), String(done.ready.id));

  await require('./f2.cjs')(ctx);
};
