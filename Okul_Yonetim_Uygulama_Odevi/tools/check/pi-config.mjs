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
//    Kuralın sınırı (docs/security.md): bu bir sezgisel denetimdir — şifre taşıyan değişken adları (password, pw, pass,
//    secret, write_env_file içindeki $1) ve şifre dosyasını okuyan komutlar aranır; her komut (; && || ile ayrılan
//    parça) ayrı denetlenir. Asıl güvence review + VERIFY'daki çıktı/log taramasıdır.
const secretVar = /\$\{?(password|pw|pass|secret)\b/i;
const readsEnvFile = /\b(cat|head|tail|less|more|grep|awk|sed|cut|strings|base64|xxd|od)\b[^|;&]*(\$\{?(ENV|JWT)_FILE\b|okul-(api|jwt)\.env)/;

// Tırnak içindeki ; & | karakterleri komut ayırıcı sayılmaz (SQL ve bağlantı cümlesinde geçer; review round 2, M-1).
// Yerine boşluk konur: kelime karakteri ("_") olsaydı "$password;" → "$password_" olur, değişken tanınmazdı (round 3, L-1).
function neutralizeQuoted(code) {
  return code.replace(/'[^']*'|"(?:[^"\\]|\\.)*"/g, (quoted) => quoted.replace(/[;&|]/g, " "));
}

/** Şifre içeren her SQL printf'i sunucu logunu kapatan önekle (${NO_LOG_SQL}) başlamalı (round 3, I-1). */
function noLogPrefixMissing(text) {
  return text.replace(/\\\r?\n/g, " ").split("\n")
    .filter((line) => /PASSWORD '%s'/.test(line) && /\bprintf\b/.test(line) && !/printf "\$\{NO_LOG_SQL\}/.test(line))
    .map((line) => line.trim());
}

/** Bir betik metninde şifre basma / şifre dosyası okuma / izleme modu sorunlarını döndürür (öz-sınamada da kullanılır). */
function secretLeaks(text, name) {
  const problems = [];
  const statements = text.replace(/\\\r?\n/g, " ").split("\n");
  let inEnvWriter = false;
  statements.forEach((line, i) => {
    const code = line.replace(/^\s*#.*$/, "");
    if (/^\s*write_env_file\(\)/.test(code)) inEnvWriter = true;
    else if (/^\}/.test(code)) inEnvWriter = false;
    if (/\bset\s+-[a-z]*x|\bbash\s+-x\b/.test(code)) problems.push(`${name}: izleme modu (set -x) yasak — gizli bilgi sızar: ${code.trim()}`);
    if (readsEnvFile.test(code)) problems.push(`${name} (ifade ${i + 1}): şifre dosyası okunuyor/basılıyor: ${code.trim()}`);
    const carries = (text) => secretVar.test(text) || (inEnvWriter && /"\$1"/.test(text));
    const routed = (text) => /\|\s*pg\b/.test(text) || />\s*"\$/.test(text);
    const prints = (text) => /\b(echo|say|printf)\b/.test(text);
    // İfade bütün olarak ve tırnak dışındaki her komut parçası ayrı ayrı denetlenir
    const parts = neutralizeQuoted(code).split(/;|&&|\|\|/);
    const leaks = parts.some((part) => prints(part) && carries(part) && !routed(part))
      || (parts.length === 1 && prints(code) && carries(code) && !routed(code));
    if (leaks) problems.push(`${name} (ifade ${i + 1}): şifre ekrana yazdırılıyor: ${code.trim()}`);
  });
  return problems;
}

for (const script of scripts) {
  for (const problem of secretLeaks(read(script), rel(script))) fail(problem);
}

// Öz-sınama (kasıtlı bozmanın kalıcı hâli): kurulum betiğinin şifreyi ekrana basacak şekilde bozulmuş kopyaları
// bellekte üretilir; her biri yakalanmazsa bu denetim işe yaramıyor demektir.
const setupText = read(path.join(remoteDir, "setup-remote.sh"));
const mutations = [
  ["CREATE ROLE çıktısı psql yerine ekrana", /("\$password" \| pg -q >\/dev\/null)/, '"$password"'],
  ["ALTER ROLE çıktısı psql yerine ekrana", /(ALTER ROLE %s PASSWORD '%s';\\n" "\$DB_ROLE" "\$password") \| pg -q >\/dev\/null/, "$1"],
  ["bağlantı cümlesi dosya yerine ekrana", /("\$DB_NAME" "\$DB_ROLE" "\$1") > "\$tmp"/, "$1"],
  ["şifre doğrudan basılıyor", /(    unset password)/, '    echo "$password"\n$1'],
  ["çok komutlu satırda tırnak içinde şifre", /(    unset password)/, '    true && say "sifre $password; tamam"\n$1'],
];
for (const [label, pattern, replacement] of mutations) {
  if (!pattern.test(setupText)) {
    fail(`pi-config öz-sınama: '${label}' için kalıp setup-remote.sh'de bulunamadı (betik değiştiyse öz-sınamayı güncelleyin)`);
    continue;
  }
  if (secretLeaks(setupText.replace(pattern, replacement), "öz-sınama").length === 0) {
    fail(`pi-config öz-sınama: '${label}' bozması yakalanmadı`);
  }
}
// Root'un kullandığı geçici yollar tahmin edilemez olmalı (review round 1, M-1): /tmp geçen satır ya mktemp ile
// rastgele ad üretir ya da üretilen adı doğrular (=~).
for (const script of scripts) {
  read(script).split("\n").forEach((line, i) => {
    const code = line.replace(/^\s*#.*$/, "");
    if (code.includes("/tmp/") && !/mktemp|=~/.test(code)) fail(`${rel(script)}:${i + 1}: tahmin edilebilir /tmp yolu: ${code.trim()}`);
  });
}
const setup = read(path.join(remoteDir, "setup-remote.sh"));
for (const line of noLogPrefixMissing(setup)) fail(`setup-remote.sh: şifreli SQL \${NO_LOG_SQL} önekiyle başlamalı: ${line}`);
// Öz-sınama: önek CREATE ya da ALTER satırından silinirse yakalanmalı
for (const [label, sql] of [["CREATE ROLE", "CREATE ROLE %s LOGIN"], ["ALTER ROLE", "ALTER ROLE %s PASSWORD"]]) {
  const withPrefix = 'printf "${NO_LOG_SQL}' + sql;
  if (!setup.includes(withPrefix)) fail(`pi-config öz-sınama: ${label} satırında \${NO_LOG_SQL} öneki bulunamadı`);
  else if (noLogPrefixMissing(setup.replace(withPrefix, 'printf "' + sql)).length === 0) fail(`pi-config öz-sınama: ${label} öneki silinmesi yakalanmadı`);
}
for (const setting of ["log_statement = 'none'", "log_min_error_statement = 'panic'", "log_min_duration_statement = -1", "log_min_duration_sample = -1"]) {
  if (!setup.includes(setting)) fail(`setup-remote.sh: şifreli SQL için '${setting}' ayarlanmalı (review round 1 L-2, round 2 L-1)`);
}
// Şifre psql'e komut satırıyla (-c) verilmez; stdin'den verilir (süreç listesinde görünmez)
if (/-c\s+["'][^"']*PASSWORD/.test(setup)) fail("setup-remote.sh: şifre psql'e komut satırıyla veriliyor (-c); stdin kullanılmalı");
if (!/PASSWORD '%s'.*\|\s*pg\b/.test(setup)) fail("setup-remote.sh: şifre psql'e stdin ile verilmiyor");
for (const forbidden of ["SUPERUSER ", "CREATEDB ", "CREATEROLE "]) {
  if (new RegExp(`(?<!NO)${forbidden}`).test(setup)) fail(`setup-remote.sh: rol yetkisi ${forbidden.trim()} verilmemeli`);
}
for (const required of ["NOSUPERUSER", "NOCREATEDB", "NOCREATEROLE", "chmod 600", "reject", "REVOKE CONNECT"]) {
  if (!setup.includes(required)) fail(`setup-remote.sh: '${required}' bulunamadı`);
}
// Veritabanı şifre dosyası da 600 izinli geçici dosyayla yazılmalı. Genel "chmod 600" araması yetmez: spec 0004'teki
// imza anahtarı adımında da geçtiği için buradan silinmesi gözden kaçardı (VERIFY bozma denemesi, spec 0004).
const envWriter = setup.match(/write_env_file\(\) \{[\s\S]*?\n\}/)?.[0] ?? "";
if (!/chmod 600 "\$tmp"/.test(envWriter)) fail("setup-remote.sh: write_env_file şifre dosyasını 600 izinli geçici dosyayla yazmalı");

// Oturum imza anahtarı (spec 0004): rastgele üretilir, yalnızca yoksa eklenir (tekrar çalıştırmada değişmez), 600 izinli
// geçici dosya üzerinden yazılır ve ekrana basılmaz.
const jwt = setup.match(/ensure_jwt_key\(\) \{[\s\S]*?\n\}/)?.[0] ?? "";
if (!jwt) fail("setup-remote.sh: ensure_jwt_key (oturum imza anahtarı) bulunamadı");
else {
  if (!/if \[ -s "\$JWT_FILE" \]; then[\s\S]*?return/.test(jwt)) fail("setup-remote.sh: imza anahtarı varsa korunmalı (tekrar üretilmemeli)");
  if (!/openssl rand -base64 48/.test(jwt)) fail("setup-remote.sh: imza anahtarı openssl rand ile (en az 32 bayt) üretilmeli");
  if (!/chmod 600 "\$tmp"/.test(jwt)) fail("setup-remote.sh: imza anahtarı 600 izinli geçici dosyayla yazılmalı");
  const writes = jwt.split("\n").filter((line) => line.includes("Jwt__SigningKey=%s"));
  if (/\b(say|echo)\b[^\n]*openssl/.test(jwt) || writes.length !== 1 || !writes[0].trimEnd().endsWith('> "$tmp"')) fail("setup-remote.sh: imza anahtarı ekrana basılmamalı");
  if (!/^\s*ensure_jwt_key$/m.test(setup.replace(jwt, ""))) fail("setup-remote.sh: ensure_jwt_key kurulumda çağrılmalı");
}

// Okul açma sarmalayıcısı (spec 0004): şifre argüman değil (komut gizli girişle sorar); argümanlar printf %q ile taşınır;
// komut servis kullanıcısıyla ve servisin env dosyasıyla çalışır.
const createSchool = read(path.join(piDir, "create-school.sh"));
if (/--sifre|--password/i.test(createSchool)) fail("create-school.sh: şifre argümanla verilmemeli");
if (!/printf ' %q'/.test(createSchool)) fail("create-school.sh: argümanlar printf %q ile taşınmalı (okul adında boşluk)");
if (!/systemd-run --pty --quiet --uid=okul --gid=okul -p EnvironmentFile=\/etc\/okul\/okul-api\.env -p EnvironmentFile=\/etc\/okul\/okul-jwt\.env/.test(createSchool)) {
  fail("create-school.sh: komut okul kullanıcısıyla, servis env dosyasıyla (systemd-run --pty) çalışmalı");
}

// Web derlemesi Git Bash'te: VITE_API_BASE_URL'in yol çevirisi kapalı olmalı ve pakette yerel yol koruması bulunmalı
// (VERIFY V-1). Öz-sınama: koruma kalıbı gerçek bozuk değeri yakalar, doğru paketi yakalamaz.
const deploy = read(path.join(piDir, "deploy.sh"));
if (!/MSYS2_ENV_CONV_EXCL=VITE_API_BASE_URL\b[^\n]*VITE_API_BASE_URL=\//.test(deploy)) {
  fail("deploy.sh: web derlemesinde VITE_API_BASE_URL için MSYS2_ENV_CONV_EXCL olmalı (Git Bash '/' değerini 'C:/Program Files/Git/' yapar)");
}
if (/^[^#]*MSYS_NO_PATHCONV/m.test(deploy)) fail("deploy.sh: MSYS_NO_PATHCONV kullanılmaz — --outDir gibi yol argümanları da çevrilmez, derleme yanlış klasöre yazılır");
if (!deploy.includes('[ -f "$OUT/web/index.html" ]')) fail("deploy.sh: web derlemesinin beklenen klasöre yazıldığı denetlenmeli");
const guard = deploy.match(/grep -rqE '([^']+)' "\$OUT\/web"/);
if (!guard) fail("deploy.sh: web paketinde yerel yol koruması (grep -rqE … \"$OUT/web\") bulunamadı");
else {
  const re = new RegExp(guard[1]);
  if (!re.test('baseUrl:"C:/Program Files/Git/"') || !re.test("file:///C:/x")) fail("deploy.sh: yerel yol koruması bozuk değeri yakalamıyor");
  if (re.test('fetch("/api/system-health")') || re.test("http://192.168.1.102:8080/")) fail("deploy.sh: yerel yol koruması doğru paketi reddediyor");
}

// 3) Servis dosyası
const service = read(path.join(remoteDir, "okul-api.service"));
const expectLine = (text, pattern, message) => { if (!pattern.test(text)) fail(message); };
expectLine(service, /^User=okul$/m, "servis: User=okul olmalı (root değil)");
expectLine(service, /^Restart=always$/m, "servis: Restart=always olmalı");
expectLine(service, /^Environment=ASPNETCORE_URLS=http:\/\/127\.0\.0\.1:@API_PORT@$/m, "servis: API yalnızca 127.0.0.1'i dinlemeli");
expectLine(service, /^EnvironmentFile=\/etc\/okul\/okul-api\.env$/m, "servis: bağlantı bilgisi /etc/okul/okul-api.env'den gelmeli");
expectLine(service, /^EnvironmentFile=\/etc\/okul\/okul-jwt\.env$/m, "servis: oturum imza anahtarı /etc/okul/okul-jwt.env'den gelmeli (spec 0004)");
expectLine(service, /^NoNewPrivileges=true$/m, "servis: NoNewPrivileges=true olmalı");
expectLine(service, /^ProtectSystem=strict$/m, "servis: ProtectSystem=strict olmalı");
expectLine(service, /^WantedBy=multi-user\.target$/m, "servis: açılışta başlamalı (WantedBy=multi-user.target)");
if (/Password=/i.test(service)) fail("servis: şifre servis dosyasında olmamalı");
if (/^Environment=Cors__/m.test(service)) fail("servis: CORS izni gerekmez (web aynı adresten çalışır; Pi IP'si değişince eskide kalır)");

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
