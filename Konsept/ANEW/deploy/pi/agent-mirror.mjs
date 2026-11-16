// Ajan bileşen aynası listesi (spec 0009 Req 11–12, plan 0009): ajana gömülü components.json'dan "mirrored": true olanları
// "<sha256> <dosya> <adres>" satırları olarak yazar; deploy.sh bunları geliştirici PC'sinde önbelleğe indirip hash'i doğrulayarak
// Pi'ye (/opt/anewconcept/agent/components) yükler. Kullanım: node deploy/pi/agent-mirror.mjs <components.json>
import { readFileSync } from 'node:fs';

const path = process.argv[2];
if (!path) {
  console.error('agent-mirror: components.json yolu gerekli');
  process.exit(2);
}

const safe = /^[A-Za-z0-9._-]+$/;
for (const component of JSON.parse(readFileSync(path, 'utf8'))) {
  if (!component.mirrored) {
    continue;
  }
  if (!safe.test(component.fileName) || !/^[0-9a-f]{64}$/.test(component.sha256) || !component.url.startsWith('https://')) {
    console.error(`agent-mirror: geçersiz bileşen kaydı: ${component.id}`);
    process.exit(1);
  }
  console.log(`${component.sha256} ${component.fileName} ${component.url}`);
}
