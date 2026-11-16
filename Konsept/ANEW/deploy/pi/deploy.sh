#!/bin/sh
# Web ve Api'yi Pi'ye kurar (spec 0001 AC-10, plan 0001 adım 9). Kullanım: sh deploy/pi/deploy.sh
# - linux-arm64 self-contained publish (Pi'de .NET runtime gerekmez).
# - Pi bağlantısı repo kökündeki .env'den (PI_SSH_ALIAS); gizli değer yazdırılmaz.
# - Kurulumdan önce ve sonra diğer servislerin durumu ve dinlenen portlar karşılaştırılır; fark varsa hata.
# - Ajan (spec 0009): win-x64 self-contained tek exe, Pi adresleri gömülü (PI_HOST'tan; AGENT_SERVER_URL / AGENT_SITE_URL ile
#   ezilebilir) → /opt/anewconcept/agent/anewconcept-ajan-<sürüm>-win-x64.exe; Web "Ajanı indir" ile sunar. Bileşen aynası
#   (components.json'da "mirrored") geliştirici PC'sinde önbelleğe indirilir, SHA-256 doğrulanır, Pi'de yoksa yüklenir.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
REPO_ROOT=$(CDPATH= cd -- "$ROOT/../.." && pwd)
PI=$(grep -E '^PI_SSH_ALIAS=' "$REPO_ROOT/.env" | cut -d= -f2- | tr -d '"\r')
[ -n "$PI" ] || { echo "deploy: .env içinde PI_SSH_ALIAS yok" >&2; exit 1; }
PI_HOST=$(grep -E '^PI_HOST=' "$REPO_ROOT/.env" | cut -d= -f2- | tr -d '"\r')
[ -n "$PI_HOST" ] || { [ -n "${AGENT_SERVER_URL:-}" ] && [ -n "${AGENT_SITE_URL:-}" ]; } || { echo "deploy: .env içinde PI_HOST yok (ya da AGENT_SERVER_URL ve AGENT_SITE_URL verin)" >&2; exit 1; }
AGENT_SERVER_URL=${AGENT_SERVER_URL:-http://$PI_HOST:8001/}
AGENT_SITE_URL=${AGENT_SITE_URL:-http://$PI_HOST:8000/}
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props" | head -1)
AGENT_FILE="anewconcept-ajan-$VERSION-win-x64.exe"

work=$(mktemp -d "${TMPDIR:-/tmp}/anewconcept-deploy.XXXXXX")
trap 'rm -rf "$work"' EXIT INT TERM HUP

echo "deploy: publish (linux-arm64, self-contained)"
for app in Api Web; do
  lower=$(echo "$app" | tr '[:upper:]' '[:lower:]')
  MSBUILDDISABLENODEREUSE=1 dotnet publish "$ROOT/src/Konsept.$app/Konsept.$app.csproj" -c Release -r linux-arm64 \
    --self-contained true -p:UseSharedCompilation=false -nodeReuse:false -o "$work/$lower" >/dev/null
done
echo "deploy: ajan (win-x64, self-contained tek dosya, $AGENT_FILE)"
MSBUILDDISABLENODEREUSE=1 dotnet publish "$ROOT/src/Konsept.Agent/Konsept.Agent.csproj" -c Release -r win-x64 --self-contained true   -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none   -p:AgentServerUrl="$AGENT_SERVER_URL" -p:AgentSiteUrl="$AGENT_SITE_URL"   -p:UseSharedCompilation=false -nodeReuse:false -o "$work/agent-publish" >/dev/null
mkdir -p "$work/agent" "$work/mirror"
cp "$work/agent-publish/Konsept.Agent.exe" "$work/agent/$AGENT_FILE"

echo "deploy: bileşen aynası"
cache=${ANEWCONCEPT_MIRROR_CACHE:-$HOME/.cache/anewconcept-mirror}
mkdir -p "$cache"
node "$ROOT/deploy/pi/agent-mirror.mjs" "$ROOT/src/Konsept.Agent.Infrastructure/Setup/components.json" > "$work/mirror.txt"
while read -r sha file url; do
  if ssh -n -o BatchMode=yes "$PI" "test -f /opt/anewconcept/agent/components/$file"; then
    echo "deploy: aynada var: $file"; continue
  fi
  if [ ! -f "$cache/$file" ] || [ "$(sha256sum "$cache/$file" | cut -d' ' -f1)" != "$sha" ]; then
    curl -fsSL --retry 2 -o "$cache/$file.part" "$url" && mv "$cache/$file.part" "$cache/$file" || rm -f "$cache/$file.part"
  fi
  if [ -f "$cache/$file" ] && [ "$(sha256sum "$cache/$file" | cut -d' ' -f1)" = "$sha" ]; then
    cp "$cache/$file" "$work/mirror/"; echo "deploy: aynaya eklenecek: $file"
  else
    echo "deploy: UYARI — ayna dosyası indirilemedi ya da doğrulanamadı: $file (ajan resmi kaynağı dener)" >&2
  fi
done < "$work/mirror.txt"

cp "$ROOT/deploy/pi/anewconcept-api.service" "$ROOT/deploy/pi/anewconcept-web.service" "$ROOT/deploy/pi/provision.sh" "$work/"
# Windows'ta düzenlenmiş dosyada CRLF kalmışsa Pi'de sh/systemd bozulur; yüklemeden önce LF'ye çevrilir.
sed -i 's/\r$//' "$work/anewconcept-api.service" "$work/anewconcept-web.service" "$work/provision.sh"

snapshot() {
  ssh -o BatchMode=yes "$PI" "systemctl list-units --type=service --state=running --no-legend --plain | awk '{print \$1}' | grep -v '^anewconcept-' | sort; echo '--- ports'; ss -Hltn | awk '{print \$4}' | grep -v -E ':(8000|8001)\$' | sort -u"
}

echo "deploy: önce — diğer servisler ve portlar"
snapshot > "$work/before.txt"

echo "deploy: yükleniyor"
ssh -o BatchMode=yes "$PI" 'rm -rf /tmp/anewconcept-deploy && mkdir -p /tmp/anewconcept-deploy'
tar -C "$work" -czf - api web agent mirror anewconcept-api.service anewconcept-web.service provision.sh \
  | ssh -o BatchMode=yes "$PI" 'tar -xzf - -C /tmp/anewconcept-deploy'

echo "deploy: kurulum (sudo)"
ssh -o BatchMode=yes "$PI" 'sudo sh /tmp/anewconcept-deploy/provision.sh /tmp/anewconcept-deploy && rm -rf /tmp/anewconcept-deploy'

echo "deploy: sağlık kontrolü"
ok=0
for i in $(seq 1 30); do
  if ssh -o BatchMode=yes "$PI" 'curl -fsS http://127.0.0.1:8001/health && curl -fsS -o /dev/null http://127.0.0.1:8000/' >"$work/health.txt" 2>/dev/null; then
    ok=1; break
  fi
  sleep 2
done
[ "$ok" -eq 1 ] || { echo "deploy: servisler ayağa kalkmadı" >&2; ssh "$PI" 'systemctl --no-pager status anewconcept-api anewconcept-web | tail -20' >&2; exit 1; }
cat "$work/health.txt"; echo
ssh -o BatchMode=yes "$PI" "test -f /opt/anewconcept/agent/$AGENT_FILE"   || { echo "deploy: ajan kurulum dosyası Pi'de yok: $AGENT_FILE" >&2; exit 1; }
echo "deploy: ajan indirilebilir: $AGENT_SITE_URL""ajan/indir ($AGENT_FILE)"

echo "deploy: sonra — diğer servisler ve portlar"
snapshot > "$work/after.txt"
if diff "$work/before.txt" "$work/after.txt"; then
  echo "deploy: diğer servisler ve portlar değişmedi"
else
  echo "deploy: UYARI — diğer servislerde ya da portlarda fark var (yukarıda)" >&2
  exit 1
fi
