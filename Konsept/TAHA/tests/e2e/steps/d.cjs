/** Adım d: Ayarlar → Okunuş sözlüğü: kayıt ekle; deneme kutusu yalnız metin (ses üretmeden) normalleştirilmiş okunuşu gösterir. */
module.exports = async (ctx) => {
  const { page } = ctx;
  await ctx.goto('/Ayarlar');
  await page.waitForSelector('#st-dict-form [name=written]', { timeout: 15000 });
  const word = 'Bizantion';
  await page.type('#st-dict-form [name=written]', word);
  await page.type('#st-dict-form [name=spoken]', 'Bizantiyon');
  await page.click('#st-dict-form [type=submit]');
  await ctx.sleep(1500);
  const list = await ctx.api.get('/Ayarlar/Sozluk');
  const rows = list.ok ? (list.data.items || list.data) : [];
  ctx.check('sözlüğe eklendi', rows.some(r => r.written === word && r.spoken === 'Bizantiyon'), rows.map(r => r.written + '→' + r.spoken));

  await page.$eval('#sp-test-text', el => { el.value = ''; });
  await page.type('#sp-test-text', 'Google 29 Mayıs 1453\'te Bizantion surlarını III. Ahmet ile anlattı.');
  let previewReq = null;
  const onReq = r => { if (r.url().includes('/Ayarlar/SozlukDinle')) { previewReq = r.postData(); } };
  page.on('request', onReq);
  await page.click('#sp-test-run');
  await page.waitForSelector('#sp-test-result .sp-result-box', { timeout: 15000 });
  await ctx.sleep(500);
  page.off('request', onReq);
  const result = await page.$eval('#sp-test-result', e => e.innerText.replace(/\s+/g, ' '));
  ctx.check('istek yalnız metin (synthesize:false)', previewReq && JSON.parse(previewReq).synthesize === false, previewReq);
  ctx.check('okunuş normalleşti (1453 → yazıyla, sözlük uygulandı)', /bin dört yüz elli üç/i.test(result) && /Bizantiyon/.test(result), result.slice(0, 300));
  ctx.check('ses üretilmedi (audio yok)', !(await page.$('#sp-test-result audio')));
  const el = await page.$('.sp-test');
  await el.screenshot({ path: require('path').join(ctx.out, 'd-deneme-kutusu.png') });
};
