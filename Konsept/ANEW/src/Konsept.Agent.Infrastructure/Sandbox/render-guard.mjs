// ANEW render ağ koruması (spec 0018 L3 yedek yolu, ADR-0004). Node'a --import ile yüklenir; render motorunun başlattığı ajan
// Chromium'una (PRODUCER_HEADLESS_SHELL_PATH) ölü proxy, DNS kapatma ve WebRTC UDP kapatma bayraklarını ekler. Yerel motor sunucusu
// (localhost/127.0.0.1) dışında hiçbir istek çıkamaz. Motorun bu bayrakları ezmesi önlenir (aynı adlı bayraklar çıkarılır).
import childProcess from 'node:child_process';
import { syncBuiltinESMExports } from 'node:module';

const norm = (p) => String(p).replace(/\//g, '\\').toLowerCase();
const browser = norm(process.env.PRODUCER_HEADLESS_SHELL_PATH || '');
const isBrowser = (file) => typeof file === 'string' && (norm(file) === browser || /chrom|headless/i.test(file.split(/[\\/]/).pop() || ''));
const guard = [
  '--proxy-server=http://127.0.0.1:9',
  '--proxy-bypass-list=localhost;127.0.0.1',
  '--host-resolver-rules=MAP * ~NOTFOUND , EXCLUDE localhost',
  '--force-webrtc-ip-handling-policy=disable_non_proxied_udp',
  '--webrtc-ip-handling-policy=disable_non_proxied_udp',
];
// Chromium sandbox'ını kapatan bayraklar da ayıklanır (review r1 L-3): renderer her zaman Chromium'un kendi sandbox'ında çalışır.
const overridden = [
  '--proxy', '--host-resolver-rules', '--force-webrtc-ip-handling-policy', '--webrtc-ip-handling-policy', '--no-proxy-server',
  '--no-sandbox', '--disable-setuid-sandbox', '--no-zygote', '--disable-gpu-sandbox', '--disable-web-security', '--allow-file-access-from-files',
];
const spawn = childProcess.spawn;

childProcess.spawn = function guardedSpawn(file, args, options) {
  if (isBrowser(file) && Array.isArray(args)) {
    args = [...args.filter((a) => !overridden.some((o) => String(a).startsWith(o))), ...guard];
  }
  return spawn.call(this, file, args, options);
};
syncBuiltinESMExports();
