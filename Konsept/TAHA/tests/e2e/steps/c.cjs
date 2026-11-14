/**
 * Adım c: Konseptler — 5 konu öner → 2 seç → Seçtiklerini üret → iş biter → Üretilenler; Fikirden üret 1; düzenle + kaydet;
 * revize (talimat → aday penceresi → Bunu da düzenle → Seç); sil → Silinenler → geri al → tekrar sil → kalıcı sil; onayla.
 */
const items = r => (r && r.ok ? (r.data && r.data.items ? r.data.items : r.data) : []) || [];

module.exports = async (ctx) => {
  const { page } = ctx;
  await ctx.goto('/Konseptler');
  const startList = items(await ctx.api.get('/Konseptler/Liste'));
  ctx.log('başlangıçta üretilen konsept', startList.length);
  const waitJob = async (label) => {
    await ctx.sleep(1000);
    const done = await ctx.waitFor(async () => {
      const t = await page.$eval('#kp-job', el => el.hidden ? '' : el.innerText.replace(/\s+/g, ' ')).catch(() => '');
      return /üretildi|tamamland|bitti|hata|durduruldu|başarısız/i.test(t) ? t : null;
    }, 120000, 1500);
    ctx.log(label + ' iş durumu:', done);
    return done;
  };

  // --- Kelime denetimi açık (tekrar bulunursa denetim modeli çağrılır)
  if (!(await page.$eval('#kp-wordcheck', e => e.checked))) { await page.click('#kp-wordcheck + .tk-switch-track').catch(() => page.$eval('#kp-wordcheck', e => e.click())); await ctx.sleep(1000); }
  const ws = await ctx.api.get('/Konseptler/Ayarlar');
  ctx.check('kelime denetimi açık', ws.ok && ws.data.wordRepetitionCheck === true);
  const mockBefore = await (await fetch('http://127.0.0.1:7199/__log')).json();

  // --- Konudan üret
  await page.$eval('#kp-topic-count', el => { el.value = '5'; });
  await page.click('#kp-topic-suggest');
  await ctx.waitFor(() => page.$$eval('#kp-topic-list .kp-topic-item', l => l.length >= 5), 30000);
  const topics = await page.$$eval('#kp-topic-list .kp-topic-item .tk-list-title', l => l.map(e => e.textContent));
  ctx.check('5 konu önerildi', topics.length === 5, topics);
  await page.click('#kp-topic-list .kp-topic-item[data-index="0"]');
  await page.click('#kp-topic-list .kp-topic-item[data-index="1"]');
  const selCount = await page.$eval('#kp-topic-selected', e => e.textContent.trim());
  ctx.check('2 konu seçildi', selCount === '2', selCount);
  await ctx.shot('konular');
  await page.click('#kp-topic-generate');
  await ctx.sleep(1500);
  await ctx.shot('is-ilerleme');
  await waitJob('seçilenler');
  const afterTopic = await ctx.waitFor(async () => { const l = items(await ctx.api.get('/Konseptler/Liste')); return l.length >= startList.length + 2 ? l : null; }, 60000, 2000);
  ctx.check('2 konsept Üretilenler\'de', afterTopic && afterTopic.length >= startList.length + 2, afterTopic ? afterTopic.map(c => c.title) : null);
  await ctx.shot('uretilenler-2');
  const mockAfter = await (await fetch('http://127.0.0.1:7199/__log')).json();
  const kinds = mockAfter.slice(mockBefore.length).map(e => e.kind);
  ctx.check('LLM çağrıları: konu + konsept + kelime denetimi', kinds.includes('topics') && kinds.includes('concept') && kinds.includes('word-check'), kinds);

  // --- Fikirden üret
  await page.$eval('#kp-idea-text', el => { el.value = 'Fatih Sultan Mehmet\'in gemileri karadan yürütmesi ve Haliç zinciri'; el.dispatchEvent(new Event('input', { bubbles: true })); });
  await page.$eval('#kp-idea-count', el => { el.value = '1'; el.dispatchEvent(new Event('input', { bubbles: true })); });
  await page.click('#kp-idea-generate');
  await waitJob('fikir');
  const afterIdea = await ctx.waitFor(async () => { const l = items(await ctx.api.get('/Konseptler/Liste')); return l.length >= startList.length + 3 ? l : null; }, 60000, 2000);
  ctx.check('fikirden 1 konsept üretildi', afterIdea && afterIdea.length >= startList.length + 3, afterIdea ? afterIdea.length : null);
  await page.click('#kp-generated-refresh');
  await ctx.sleep(1500);

  // --- Düzenle + kaydet (ilk konsept)
  const ids = await page.$$eval('#kp-generated-list .tk-list-item', l => l.map(e => e.getAttribute('data-id')));
  ctx.log('liste kimlikleri', ids.length);
  const target = ids[0];
  await page.click(`#kp-generated-list .tk-list-item[data-id="${target}"] .tk-list-title`).catch(async () => page.click(`#kp-generated-list .tk-list-item[data-id="${target}"]`));
  await page.waitForSelector('#kp-f-title', { timeout: 10000 });
  await ctx.sleep(500);
  const newHook = 'E2E düzenleme: bin yıllık surlar o gece son kez sessizdi.';
  await page.$eval('#kp-f-hook', (el, v) => { el.value = v; el.dispatchEvent(new Event('input', { bubbles: true })); }, newHook);
  await ctx.shot('duzenleme');
  await page.click('#kp-editor-save');
  await ctx.sleep(1500);
  const got = await ctx.api.get('/Konseptler/Getir/' + target);
  const gotDoc = got.ok ? (got.data.document || got.data) : null;
  ctx.check('düzenleme kaydedildi (kanca)', gotDoc && gotDoc.hook === newHook, gotDoc && gotDoc.hook);

  // --- Revize: talimat → aday → Bunu da düzenle → Tekrar üret → Seç
  await page.click('#kp-editor .tk-tab[data-tab="revise"]');
  await page.type('#kp-revise-text', 'Girişi daha çarpıcı yap.');
  await page.click('#kp-revise');
  await ctx.modal(60000);
  await ctx.shot('revize-aday');
  await ctx.clickText('.tk-modal-foot button, .kp-modal-foot button', 'Bunu da düzenle');
  await page.type('#kp-cand-instr', 'Son sahneyi de kısalt.');
  await page.click('.tk-modal [data-act="again-run"]');
  await ctx.sleep(3000);
  await ctx.modal(60000);
  await ctx.shot('revize-aday-2');
  await ctx.clickText('.kp-modal-foot button', 'Seç');
  await ctx.sleep(2000);
  const rev = await ctx.api.get('/Konseptler/Getir/' + target);
  const revDoc = rev.ok ? (rev.data.document || rev.data) : null;
  ctx.check('revize seçildi (başlık "(revize)")', revDoc && /\(revize\)/.test(revDoc.title), revDoc && revDoc.title);

  // --- Sil → Silinenler → Geri al → Sil → Kalıcı sil (ikinci konsept)
  const victim = ids[1];
  await page.click(`#kp-generated-list .tk-list-item[data-id="${victim}"] [data-act="delete"]`);
  await ctx.modal();
  await ctx.clickText('.tk-modal-foot button', 'Sil');
  await ctx.sleep(1500);
  await ctx.goto('/Silinenler');
  let row = await page.$(`#sl-list [data-id="${victim}"]`);
  ctx.check('silinen konsept Silinenler\'de', !!row);
  await ctx.shot('silinenler');
  await page.click(`#sl-list [data-id="${victim}"] [data-act="restore"]`);
  await ctx.sleep(1500);
  const back = items(await ctx.api.get('/Konseptler/Liste')).some(c => String(c.id) === String(victim));
  ctx.check('geri alındı', back);
  await ctx.goto('/Konseptler');
  await page.click(`#kp-generated-list .tk-list-item[data-id="${victim}"] [data-act="delete"]`);
  await ctx.modal();
  await ctx.clickText('.tk-modal-foot button', 'Sil');
  await ctx.sleep(1500);
  await ctx.goto('/Silinenler');
  await page.click(`#sl-list [data-id="${victim}"] [data-act="purge"]`);
  await ctx.modal();
  await ctx.clickText('.tk-modal-foot button', 'Kalıcı sil');
  await ctx.sleep(1500);
  row = await page.$(`#sl-list [data-id="${victim}"]`);
  const del = items(await ctx.api.get('/Silinenler/Liste')).some(c => String(c.id) === String(victim));
  ctx.check('kalıcı silindi', !row && !del);

  // --- Onayla (revize edilen konsept)
  await ctx.goto('/Konseptler');
  await page.click(`#kp-generated-list .tk-list-item[data-id="${target}"] [data-act="approve"]`);
  await ctx.sleep(1500);
  const approved = await ctx.api.get('/KonsepttenVideo/Onaylananlar');
  ctx.check('onaylanan listede', items(approved).some(c => String(c.id || c.conceptId) === String(target)), items(approved).map(c => c.title));
  await ctx.shot('son');
  require('fs').writeFileSync(require('path').join(ctx.out, 'approved-id.txt'), String(target));
};
