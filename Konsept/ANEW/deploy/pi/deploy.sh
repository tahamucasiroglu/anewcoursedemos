#!/bin/sh
# Web ve Api'yi Pi'ye kurar (spec 0001 AC-10, plan 0001 adım 9). Kullanım: sh deploy/pi/deploy.sh
# - linux-arm64 self-contained publish (Pi'de .NET runtime gerekmez).
# - Pi bağlantısı repo kökündeki .env'den (PI_SSH_ALIAS); gizli değer yazdırılmaz.
# - Kurulumdan önce ve sonra diğer servislerin durumu ve dinlenen portlar karşılaştırılır; fark varsa hata.
set -eu
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
REPO_ROOT=$(CDPATH= cd -- "$ROOT/../.." && pwd)
PI=$(grep -E '^PI_SSH_ALIAS=' "$REPO_ROOT/.env" | cut -d= -f2- | tr -d '"\r')
[ -n "$PI" ] || { echo "deploy: .env içinde PI_SSH_ALIAS yok" >&2; exit 1; }

work=$(mktemp -d "${TMPDIR:-/tmp}/anewconcept-deploy.XXXXXX")
trap 'rm -rf "$work"' EXIT INT TERM HUP

echo "deploy: publish (linux-arm64, self-contained)"
for app in Api Web; do
  lower=$(echo "$app" | tr '[:upper:]' '[:lower:]')
  MSBUILDDISABLENODEREUSE=1 dotnet publish "$ROOT/src/Konsept.$app/Konsept.$app.csproj" -c Release -r linux-arm64 \
    --self-contained true -p:UseSharedCompilation=false -nodeReuse:false -o "$work/$lower" >/dev/null
done
cp "$ROOT/deploy/pi/anewconcept-api.service" "$ROOT/deploy/pi/anewconcept-web.service" "$ROOT/deploy/pi/provision.sh" "$work/"

snapshot() {
  ssh -o BatchMode=yes "$PI" "systemctl list-units --type=service --state=running --no-legend --plain | awk '{print \$1}' | grep -v '^anewconcept-' | sort; echo '--- ports'; ss -Hltn | awk '{print \$4}' | grep -v -E ':(8000|8001)\$' | sort -u"
}

echo "deploy: önce — diğer servisler ve portlar"
snapshot > "$work/before.txt"

echo "deploy: yükleniyor"
ssh -o BatchMode=yes "$PI" 'rm -rf /tmp/anewconcept-deploy && mkdir -p /tmp/anewconcept-deploy'
tar -C "$work" -czf - api web anewconcept-api.service anewconcept-web.service provision.sh \
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

echo "deploy: sonra — diğer servisler ve portlar"
snapshot > "$work/after.txt"
if diff "$work/before.txt" "$work/after.txt"; then
  echo "deploy: diğer servisler ve portlar değişmedi"
else
  echo "deploy: UYARI — diğer servislerde ya da portlarda fark var (yukarıda)" >&2
  exit 1
fi
