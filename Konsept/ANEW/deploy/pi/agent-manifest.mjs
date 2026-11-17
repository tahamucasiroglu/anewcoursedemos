// Ajan güncelleme listesi (spec 0023 Req 6): Ed25519 anahtarı, açık anahtar ve imzalı manifest. Node'un yerleşik crypto modülü (yeni araç yok).
// Özel anahtar repoda ve Pi'de durmaz: yayın PC'sinde AGENT_UPDATE_KEY dosyasında (PEM, PKCS#8). Hiçbir komut özel anahtarı yazdırmaz.
//   node deploy/pi/agent-manifest.mjs keygen <özel-anahtar.pem>        → anahtar üretir (dosya varsa dokunmaz), açık anahtarı yazar
//   node deploy/pi/agent-manifest.mjs pubkey <özel-anahtar.pem>        → ajana gömülecek açık anahtar (base64, 32 bayt)
//   node deploy/pi/agent-manifest.mjs sign <özel-anahtar.pem> <exe> <sürüm> <çıkış-klasörü>
//                                                                      → <çıkış>/manifest.json + manifest.json.sig (+ exe kopyası)
import { createHash, createPrivateKey, createPublicKey, generateKeyPairSync, sign } from 'node:crypto';
import { copyFileSync, existsSync, mkdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { basename, join } from 'node:path';

const [command, keyPath, ...rest] = process.argv.slice(2);

function fail(message) {
  console.error(`agent-manifest: ${message}`);
  process.exit(1);
}

function publicKeyBase64(privateKey) {
  const jwk = createPublicKey(privateKey).export({ format: 'jwk' });
  return Buffer.from(jwk.x, 'base64url').toString('base64');
}

function loadKey() {
  if (!keyPath || !existsSync(keyPath)) {
    fail('özel anahtar dosyası bulunamadı (AGENT_UPDATE_KEY)');
  }
  const key = createPrivateKey(readFileSync(keyPath));
  if (key.asymmetricKeyType !== 'ed25519') {
    fail('anahtar Ed25519 değil');
  }
  return key;
}

switch (command) {
  case 'keygen': {
    if (!keyPath) {
      fail('kullanım: keygen <özel-anahtar.pem>');
    }
    if (existsSync(keyPath)) {
      fail('anahtar dosyası zaten var; üzerine yazılmadı');
    }
    const { privateKey } = generateKeyPairSync('ed25519');
    writeFileSync(keyPath, privateKey.export({ format: 'pem', type: 'pkcs8' }), { mode: 0o600 });
    console.log(publicKeyBase64(privateKey));
    break;
  }
  case 'pubkey':
    console.log(publicKeyBase64(loadKey()));
    break;
  case 'sign': {
    const [exe, version, outDir] = rest;
    if (!exe || !version || !outDir) {
      fail('kullanım: sign <özel-anahtar.pem> <exe> <sürüm> <çıkış-klasörü>');
    }
    if (!/^\d+\.\d+(\.\d+){0,2}$/.test(version)) {
      fail(`geçersiz sürüm: ${version}`);
    }
    const key = loadKey();
    const bytes = readFileSync(exe);
    const fileName = basename(exe);
    if (!/^[A-Za-z0-9._-]+\.exe$/.test(fileName)) {
      fail(`geçersiz exe adı: ${fileName}`);
    }
    const manifest = Buffer.from(JSON.stringify({
      version,
      fileName,
      sha256: createHash('sha256').update(bytes).digest('hex'),
      size: statSync(exe).size,
    }));
    mkdirSync(outDir, { recursive: true });
    writeFileSync(join(outDir, 'manifest.json'), manifest);
    writeFileSync(join(outDir, 'manifest.json.sig'), sign(null, manifest, key));
    copyFileSync(exe, join(outDir, fileName));
    console.log(`agent-manifest: ${version} imzalandı (${fileName})`);
    break;
  }
  default:
    fail('komut: keygen | pubkey | sign');
}
