/** Adım b: Ayarlar → Yerel LLM satırı (kök adres http://localhost:7199/v1), test; Konseptler model penceresi ve üretim kod modeli = local-openai:mock. */
module.exports = async (ctx) => {
  const { page } = ctx;
  await ctx.goto('/Ayarlar');
  await page.waitForSelector('#st-keys tr[data-platform="local-openai"]', { timeout: 15000 });
  const row = 'tr[data-platform="local-openai"]';
  await page.$eval(`${row} [data-field=baseUrl]`, el => { el.value = ''; });
  await page.type(`${row} [data-field=baseUrl]`, 'http://localhost:7199/v1');
  await page.click(`${row} [data-act=save]`);
  await ctx.waitFor(() => page.$(`${row} [data-act=test]`), 10000);
  await ctx.sleep(500);
  await page.click(`${row} [data-act=test]`);
  const tested = await ctx.waitFor(() => page.$eval(row, r => r.textContent.includes('Test: geçerli') || r.textContent.includes('Test: başarısız') ? r.textContent : null), 15000);
  ctx.check('Yerel LLM testi geçti (mock /v1/models)', tested && tested.includes('Test: geçerli'), tested && tested.replace(/\s+/g, ' ').slice(0, 160));
  const keys = await ctx.api.get('/Ayarlar/ApiAnahtarlari');
  const lk = (keys.data.items || keys.data).find(k => k.platform === 'local-openai');
  ctx.check('kayıt: kök adres döndü, anahtar yok', lk && lk.configured && lk.baseUrl === 'http://localhost:7199/v1' && !lk.last4, lk);
  const el = await page.$(row);
  await el.screenshot({ path: require('path').join(ctx.out, 'b-yerel-llm-satir.png') });

  // Konseptler → Modeller penceresi
  await ctx.goto('/Konseptler');
  await page.click('#kp-models');
  await ctx.modal();
  for (const name of ['conceptModelKey', 'topicModelKey', 'wordCheckModelKey']) {
    await page.select(`.tk-modal [name=${name}]`, '__pt:local-openai');
    await ctx.sleep(150);
    await page.$eval(`.tk-modal [data-pt-for=${name}]`, i => { i.value = ''; });
    await page.type(`.tk-modal [data-pt-for=${name}]`, 'mock');
  }
  await ctx.shot('konsept-modeller-modal');
  await page.click('.tk-modal-head .tk-mini-btn');
  await ctx.sleep(1500);
  const s = await ctx.api.get('/Konseptler/Ayarlar');
  ctx.check('konsept ayarları local-openai:mock', s.ok && s.data.conceptModelKey === 'local-openai:mock' && s.data.topicModelKey === 'local-openai:mock' && s.data.wordCheckModelKey === 'local-openai:mock', s.ok ? [s.data.conceptModelKey, s.data.topicModelKey, s.data.wordCheckModelKey] : s);
  const summary = await page.$eval('#kp-models-summary', e => e.textContent.trim());
  ctx.check('model özeti okunur', /Yerel LLM.*mock/.test(summary), summary);

  // Konseptten Video → kod modeli
  await ctx.goto('/KonsepttenVideo');
  const opened = await page.evaluate(() => {
    const el = document.querySelector('[data-model-kind="code"]');
    if (el) { el.click(); return true; }
    return false;
  });
  ctx.log('kod modeli düzenleyici açıldı mı', opened);
  await ctx.modal();
  await page.evaluate(() => {
    const r = document.querySelector('.tk-modal input[name=code][value="local-openai"]');
    r.click();
  });
  await ctx.sleep(200);
  await page.type('.tk-modal [data-pt="local-openai"]', 'mock');
  await ctx.shot('kod-modeli-modal');
  await page.click('.tk-modal-head .tk-mini-btn');
  await ctx.sleep(1500);
  const ps = await ctx.api.get('/KonsepttenVideo/Ayarlar');
  ctx.check('üretim kod modeli local-openai:mock', ps.ok && ps.data.codeModelKey === 'local-openai:mock', ps.ok ? ps.data.codeModelKey : ps);
};
