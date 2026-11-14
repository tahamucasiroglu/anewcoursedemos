// Marka ikonlarını tek kaynaktan üretir: source/taha-konsept-app.svg → masaüstü ajanı için app.png (256) + app.ico (16–256).
// Kullanım: node deploy/brand/make-icons.cjs [puppeteer-core yolu] [chrome yolu]
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..', '..');
const puppeteerPath = process.argv[2] || path.join(root, '.tools-dev/agent-tools/engines/hyperframes/node_modules/puppeteer-core');
const chromePath = process.argv[3] || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const svg = fs.readFileSync(path.join(root, 'source/taha-konsept-app.svg'), 'utf8');
const outDir = path.join(root, 'src/Desktop/TahaKonsept.Agent/Assets');
const sizes = [16, 24, 32, 48, 64, 128, 256];

/** PNG'leri tek bir ICO dosyasına (PNG gömülü girişler) paketler. */
function buildIco(pngs) {
  const header = Buffer.alloc(6);
  header.writeUInt16LE(0, 0);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(pngs.length, 4);
  const entries = [];
  let offset = 6 + 16 * pngs.length;
  for (const { size, data } of pngs) {
    const e = Buffer.alloc(16);
    e.writeUInt8(size >= 256 ? 0 : size, 0);
    e.writeUInt8(size >= 256 ? 0 : size, 1);
    e.writeUInt8(0, 2);
    e.writeUInt8(0, 3);
    e.writeUInt16LE(1, 4);
    e.writeUInt16LE(32, 6);
    e.writeUInt32LE(data.length, 8);
    e.writeUInt32LE(offset, 12);
    offset += data.length;
    entries.push(e);
  }
  return Buffer.concat([header, ...entries, ...pngs.map(p => p.data)]);
}

(async () => {
  const puppeteer = require(puppeteerPath);
  const browser = await puppeteer.launch({ executablePath: chromePath, headless: true });
  const page = await browser.newPage();
  const pngs = [];
  for (const size of sizes) {
    await page.setViewport({ width: size, height: size, deviceScaleFactor: 1 });
    const html = `<html><body style="margin:0;background:transparent">${svg.replace('<svg ', `<svg width="${size}" height="${size}" `)}</body></html>`;
    await page.setContent(html);
    const data = await page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: size, height: size } });
    pngs.push({ size, data: Buffer.from(data) });
  }
  await browser.close();
  fs.writeFileSync(path.join(outDir, 'app.png'), pngs.find(p => p.size === 256).data);
  fs.writeFileSync(path.join(outDir, 'app.ico'), buildIco(pngs));
  console.log('app.png + app.ico yazıldı:', outDir);
})().catch(e => { console.error(e); process.exit(1); });
