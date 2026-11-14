/** Görsel tarama: tüm sayfaların açık/koyu tema ve dar ekran (390 px) ekran görüntüleri (out/pages-*.png). */
module.exports = async (ctx) => {
  const { page } = ctx;
  const pages = ['/Konseptler', '/KonsepttenVideo', '/Uretilenler', '/Silinenler', '/Yayinlama', '/Ayarlar', '/Admin'];
  for (const theme of ['light', 'dark']) {
    for (const p of pages) {
      await ctx.goto(p);
      await page.evaluate(t => document.documentElement.setAttribute('data-theme', t), theme);
      await ctx.sleep(800);
      await ctx.shot(`${theme}${p.replace(/\//g, '_')}`);
    }
  }
  await page.setViewport({ width: 390, height: 844 });
  for (const p of ['/Konseptler', '/KonsepttenVideo', '/Uretilenler']) {
    await ctx.goto(p);
    await ctx.shot(`mobile${p.replace(/\//g, '_')}`);
  }
  await page.setViewport({ width: 1600, height: 1000 });
  ctx.check('sayfa taraması tamamlandı', true);
};
