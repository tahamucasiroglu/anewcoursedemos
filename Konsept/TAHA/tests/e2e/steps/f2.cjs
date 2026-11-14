/** Adım f2: üretilen videonun Web oynatıcısında ajan adresinden yüklenmesi (HTTP 200/206) + ekran görüntüsü. */
const fs = require('fs');
const path = require('path');
const items = r => (r && r.ok ? (r.data && r.data.items ? r.data.items : r.data) : []) || [];

module.exports = async (ctx) => {
  const { page } = ctx;
  const idFile = path.join(ctx.out, 'video-id.txt');
  let videoId = fs.existsSync(idFile) ? fs.readFileSync(idFile, 'utf8').trim() : null;
  const vids = items(await ctx.api.get('/KonsepttenVideo/Uretilenler'));
  if (!videoId || !vids.some(v => String(v.id) === videoId)) { videoId = vids.length ? String(vids[0].id) : null; fs.writeFileSync(idFile, videoId || ''); }
  const done = { ready: vids.find(v => String(v.id) === videoId) };
  ctx.check('üretilen video listede', !!done.ready, done.ready && { id: done.ready.id, title: done.ready.title, sec: done.ready.durationSec, cost: done.ready.costUsd });
  if (!done.ready) { return; }
  await ctx.goto('/KonsepttenVideo');
  await page.waitForSelector(`#kv-produced-list [data-id="${done.ready.id}"]`, { timeout: 15000 });
  const mediaResponses = [];
  const onResp = r => { if (/\.mp4|videos?\//i.test(r.url()) && r.request().resourceType() === 'media') { mediaResponses.push({ url: r.url(), status: r.status() }); } };
  page.on('response', onResp);
  await page.click(`#kv-produced-list [data-id="${done.ready.id}"]`);
  await ctx.sleep(4000);
  const vinfo = await page.evaluate(() => { const v = document.querySelector('#kv-player video'); return v ? { src: v.currentSrc || v.src, readyState: v.readyState, duration: v.duration, w: v.videoWidth, h: v.videoHeight, err: v.error && v.error.message } : null; });
  page.off('response', onResp);
  ctx.check('oynatıcı videoyu yükledi', vinfo && vinfo.readyState >= 1 && vinfo.duration > 5, vinfo);
  ctx.check('video adresi HTTP 200/206', mediaResponses.some(m => m.status === 200 || m.status === 206), mediaResponses.slice(0, 3));
  await ctx.shot('oynatici');
  fs.writeFileSync(path.join(ctx.out, 'video-info.json'), JSON.stringify({ video: done.ready, player: vinfo, media: mediaResponses }, null, 2));
};
