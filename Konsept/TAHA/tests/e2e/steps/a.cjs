/** Adım a: üst bar profil açılır listesi, ➕ ile şablondan profil (Tarih – Osmanlı), aktif etme, ✏️ ile düzenleme. */
module.exports = async (ctx) => {
  const { page } = ctx;
  await ctx.goto('/Konseptler');
  await ctx.shot('topbar-once', { clip: { x: 0, y: 0, width: 1600, height: 70 } });
  const before = await page.$eval('#tk-profile', s => ({ text: s.options[s.selectedIndex] && s.options[s.selectedIndex].text, count: s.options.length, sw: s.scrollWidth, cw: s.clientWidth }));
  ctx.log('profil listesi önce', before);

  // ➕ Yeni profil, şablon "Osmanlı"
  await page.click('#tk-profile-add');
  await ctx.modal();
  await ctx.waitFor(() => page.$eval('#pf-template', s => s.options.length > 1), 10000);
  const tpl = await page.$eval('#pf-template', s => {
    const o = [...s.options].find(x => /osmanl/i.test(x.text));
    return o ? { value: o.value, text: o.text } : null;
  });
  ctx.check('Osmanlı şablonu listede', !!tpl, tpl);
  await page.select('#pf-template', tpl.value);
  await ctx.sleep(300);
  const name = 'E2E Osmanlı ' + new Date().toISOString().slice(11, 19).replace(/:/g, '');
  await page.$eval('#pf-name', (el, v) => { el.value = v; el.dispatchEvent(new Event('input', { bubbles: true })); }, name);
  await page.$eval('#pf-duration', el => { el.value = '20'; });
  await ctx.shot('profil-ekle-modal');
  await ctx.clickText('.tk-modal-foot button', 'Kaydet');
  await ctx.waitFor(() => page.$('.tk-modal-backdrop.is-open').then(x => !x), 10000);
  await ctx.sleep(1500);
  const active = await page.$eval('#tk-profile', s => s.options[s.selectedIndex] && s.options[s.selectedIndex].text);
  ctx.check('yeni profil aktif', active === name, active);
  const activeOf = async () => { const r = await ctx.api.get('/Profil/Liste'); const items = r.ok ? (r.data.items || r.data) : []; return items.find(p => p.isActive) || null; };
  const prof = await activeOf();
  ctx.check('API: yeni profil aktif', prof && prof.name === name, prof && prof.name);

  // ✏️ düzenle: hedef süre 20 kalsın, ton ekle
  await page.click('#tk-profile-edit');
  await ctx.modal();
  await ctx.sleep(500);
  const editName = await page.$eval('#pf-name', el => el.value);
  ctx.check('düzenle penceresi profili getirdi', editName === name, editName);
  await page.$eval('details.tk-profile-advanced', d => { d.open = true; });
  await page.$eval('#pf-tone', el => { el.value = 'Merak uyandıran, e2e'; });
  await ctx.shot('profil-duzenle-modal');
  await ctx.clickText('.tk-modal-foot button', 'Kaydet');
  await ctx.sleep(1500);
  const after = await activeOf();
  const full = after ? await ctx.api.get('/Profil/Getir/' + after.id) : null;
  ctx.check('düzenleme kaydedildi (ton)', full && full.ok && full.data.defaultTone === 'Merak uyandıran, e2e', full && full.data && full.data.defaultTone);
  await ctx.shot('topbar-sonra', { clip: { x: 0, y: 0, width: 1600, height: 70 } });

  // Dar ekranda profil listesi metni taşmamalı
  await page.setViewport({ width: 1280, height: 900 });
  await ctx.sleep(500);
  const fit = await page.$eval('#tk-profile', s => ({ sw: s.scrollWidth, cw: s.clientWidth, ow: s.offsetWidth }));
  await ctx.shot('topbar-1280', { clip: { x: 0, y: 0, width: 1280, height: 70 } });
  ctx.log('profil seçici boyutları', fit);
  await page.setViewport({ width: 1600, height: 1000 });
};
