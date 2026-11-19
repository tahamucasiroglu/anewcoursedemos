// Raspberry Pi dosyalarının statik denetimi (spec 0003) — Pi'ye bağlanmaz, ağdan bağımsızdır (check adımı pi-config).
// Pi'deki gerçek davranış VERIFY'da gözlemle kanıtlanır; burada dosyaların güvenlik ve doğruluk kuralları sınanır:
// betik sözdizimi, servis (root olmayan kullanıcı, yeniden başlatma, yalnızca 127.0.0.1, sertleştirme), nginx (yalnızca
// web portu, /api içeriye), şifrenin ekrana basılmaması, kaldırmanın onaysız çalışmaması, repoda şifre dosyası olmaması.
import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const root = path.resolve(import.meta.dirname, "..", "..");
const piDir = path.join(root, "tools", "pi");
const remoteDir = path.join(piDir, "remote");
const failures = [];
const fail = (message) => failures.push(message);
const read = (file) => fs.readFileSync(file, "utf8");
const rel = (file) => path.relative(root, file).replaceAll("\\", "/");

const scripts = [
  ...fs.readdirSync(piDir).filter((f) => f.endsWith(".sh")).map((f) => path.join(piDir, f)),
  ...fs.readdirSync(remoteDir).filter((f) => f.endsWith(".sh")).map((f) => path.join(remoteDir, f)),
];
if (scripts.length < 10) fail(`beklenen betikler eksik (bulunan: ${scripts.length})`);

// 1) Sözdizimi
for (const script of scripts) {
  const result = spawnSync("bash", ["-n", script], { encoding: "utf8" });
  if (result.status !== 0) fail(`${rel(script)}: bash sözdizimi hatası\n${result.stderr}`);
}

// 2) İzleme modu yok, şifre ekrana basılmaz: şifre değişkeni yazdıran bir komut ancak psql'e boru (| pg) ya da
//    dosyaya yönlendirme (> "...") ile kullanılabilir. "\" ile bölünmüş satırlar birleştirilerek denetlenir.
for (const script of scripts) {
  const statements = read(script).replace(/\\\r?\n/g, " ").split("\n");
  statements.forEach((line, i) => {
    const code = line.replace(/^\s*#.*$/, "");
    if (/\bset\s+-[a-z]*x|\bbash\s+-x\b/.test(code)) fail(`${rel(script)}: izleme modu (set -x) yasak — gizli bilgi sızar: ${code.trim()}`);
    const printsPassword = /\b(echo|say|printf)\b/.test(code) && /\$\{?password\b/i.test(code);
    const safelyRouted = /\|\s*pg\b/.test(code) || />\s*"\$/.test(code);
    if (printsPassword && !safelyRouted) fail(`${rel(script)} (ifade ${i + 1}): şifre değişkeni ekrana yazdırılıyor: ${code.trim()}`);
  });
}
const setup = read(path.join(remoteDir, "setup-remote.sh"));
// Şifre psql'e komut satırıyla (-c) verilmez; stdin'den verilir (süreç listesinde görünmez)
if (/-c\s+["'][^"']*PASSWORD/.test(setup)) fail("setup-remote.sh: şifre psql'e komut satırıyla veriliyor (-c); stdin kullanılmalı");
if (!/PASSWORD '%s'.*\|\s*pg\b/.test(setup)) fail("setup-remote.sh: şifre psql'e stdin ile verilmiyor");
for (const forbidden of ["SUPERUSER ", "CREATEDB ", "CREATEROLE "]) {
  if (new RegExp(`(?<!NO)${forbidden}`).test(setup)) fail(`setup-remote.sh: rol yetkisi ${forbidden.trim()} verilmemeli`);
}
for (const required of ["NOSUPERUSER", "NOCREATEDB", "NOCREATEROLE", "chmod 600", "reject", "REVOKE CONNECT"]) {
  if (!setup.includes(required)) fail(`setup-remote.sh: '${required}' bulunamadı`);
}

// 3) Servis dosyası
const service = read(path.join(remoteDir, "okul-api.service"));
const expectLine = (text, pattern, message) => { if (!pattern.test(text)) fail(message); };
expectLine(service, /^User=okul$/m, "servis: User=okul olmalı (root değil)");
expectLine(service, /^Restart=always$/m, "servis: Restart=always olmalı");
expectLine(service, /^Environment=ASPNETCORE_URLS=http:\/\/127\.0\.0\.1:@API_PORT@$/m, "servis: API yalnızca 127.0.0.1'i dinlemeli");
expectLine(service, /^EnvironmentFile=\/etc\/okul\/okul-api\.env$/m, "servis: bağlantı bilgisi /etc/okul/okul-api.env'den gelmeli");
expectLine(service, /^NoNewPrivileges=true$/m, "servis: NoNewPrivileges=true olmalı");
expectLine(service, /^ProtectSystem=strict$/m, "servis: ProtectSystem=strict olmalı");
expectLine(service, /^WantedBy=multi-user\.target$/m, "servis: açılışta başlamalı (WantedBy=multi-user.target)");
if (/Password=/i.test(service)) fail("servis: şifre servis dosyasında olmamalı");

// 4) nginx
const nginx = read(path.join(remoteDir, "nginx-okul.conf"));
const listens = [...nginx.matchAll(/^\s*listen\s+([^;]+);/gm)].map((m) => m[1].trim());
if (listens.length === 0 || listens.some((l) => !l.endsWith("@WEB_PORT@"))) fail(`nginx: yalnızca @WEB_PORT@ dinlenmeli (bulunan: ${listens.join(", ")})`);
expectLine(nginx, /location \/api\/ \{[^}]*proxy_pass http:\/\/127\.0\.0\.1:@API_PORT@;/s, "nginx: /api/ içeriye (127.0.0.1:@API_PORT@) iletilmeli");
expectLine(nginx, /try_files \$uri \$uri\/ \/index\.html;/, "nginx: sayfa yenilemede index.html dönmeli");

// 5) Kaldırma onaysız çalışmaz: onay kontrolü ilk uzak çağrıdan önce
const uninstall = read(path.join(piDir, "uninstall.sh"));
const confirmAt = uninstall.indexOf('"$CONFIRM" != "KALDIR"');
const remoteAt = uninstall.indexOf("run_remote");
if (confirmAt < 0 || remoteAt < 0 || confirmAt > remoteAt) fail("uninstall.sh: KALDIR onayı uzak komuttan önce denetlenmeli");
if (!read(path.join(remoteDir, "uninstall-remote.sh")).includes('[ "${1:-}" = "KALDIR" ]')) fail("uninstall-remote.sh: onay metni Pi tarafında da istenmeli");
const noConfirm = spawnSync("bash", [path.join(piDir, "uninstall.sh")], { encoding: "utf8", input: "" });
if (noConfirm.status === 0 || !/hiçbir şey silinmedi/.test(noConfirm.stdout + noConfirm.stderr)) {
  fail("uninstall.sh: onaysız çalıştırıldığında hiçbir şey yapmadan hata ile çıkmalı");
}

// 6) Repoda gizli bilgi dosyası yok
const conf = read(path.join(piDir, "pi.conf"));
if (/password|Password=|PGPASSWORD/i.test(conf.replace(/^#.*$/gm, ""))) fail("pi.conf: şifre içermemeli");
for (const file of fs.readdirSync(path.join(piDir), { recursive: true })) {
  if (/\.env$/.test(String(file))) fail(`tools/pi/${file}: repoda .env dosyası olmamalı`);
}

if (failures.length > 0) {
  console.error(`pi-config: ${failures.length} sorun\n- ${failures.join("\n- ")}`);
  process.exit(1);
}
console.log(`pi-config: ${scripts.length} betik ve Pi ayar dosyaları denetlendi, sorun yok`);
