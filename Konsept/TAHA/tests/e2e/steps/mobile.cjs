/** Dar ekran (390 px) üst bar: tüm araçlar ekranda (taşma yok). */
module.exports = async (ctx) => {
  const { page } = ctx;
  await page.setViewport({ width: 390, height: 844 });
  await ctx.goto('/KonsepttenVideo');
  const r = await page.evaluate(() => {
    const ids = ['tk-profile-edit', 'tk-profile-add'];
    const els = ids.map(id => document.getElementById(id)).concat([document.querySelector('[data-theme-toggle]'), document.querySelector('.tk-dropdown [data-dropdown-toggle]')]);
    return { vw: window.innerWidth, scrollW: document.documentElement.scrollWidth, right: els.map(e => e ? Math.round(e.getBoundingClientRect().right) : null) };
  });
  await ctx.shot('topbar', { clip: { x: 0, y: 0, width: 390, height: 70 } });
  ctx.check('üst bar araçları 390 px içinde', r.right.every(x => x !== null && x <= r.vw), r);
  await page.setViewport({ width: 1600, height: 1000 });
};
