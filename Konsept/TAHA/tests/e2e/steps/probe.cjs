/** Hızlı durum yoklaması: profil listesi + üst bar görüntüsü. */
module.exports = async (ctx) => {
  await ctx.goto('/Konseptler');
  const r = await ctx.api.get('/Profil/Liste');
  ctx.log('profiller', JSON.stringify(r).slice(0, 600));
  await ctx.shot('topbar', { clip: { x: 0, y: 0, width: 1600, height: 70 } });
  await ctx.shot('sayfa');
};
