# Pi'de root olarak çalışır (rollback.sh gönderir). Elle önceki sürüme dönüş (spec 0003, AC-7).
# Kullanım: rollback-remote.sh <birimler: web,api> <WEB_PORT>
# Veritabanı şeması geri alınmaz; geri dönülen API sürümünden sonra eklenmiş migration varsa uyarı verilir (Ö-2).
set -euo pipefail

LOCK=/run/lock/okul-deploy
UNITS="${1:?birimler}"
WEB_PORT="${2:?WEB_PORT}"

say() { printf '  %s\n' "$*"; }
die() { printf 'HATA: %s\n' "$*" >&2; exit 1; }

mkdir "$LOCK" 2>/dev/null || die "Başka bir dağıtım/geri alma sürüyor (kilit: $LOCK)."
trap 'rmdir "$LOCK" 2>/dev/null || true' EXIT

IFS=',' read -r -a unit_list <<< "$UNITS"
for unit in "${unit_list[@]}"; do
  current="$(readlink "/opt/okul/$unit/current" 2>/dev/null || true)"
  [ -n "$current" ] || die "$unit: etkin sürüm yok."
  previous=""
  while IFS= read -r dir; do
    rel="releases/$(basename "$dir")"
    if [ "$rel" != "$current" ]; then previous="$rel"; break; fi
  done < <(ls -1dt "/opt/okul/$unit/releases/"*/ 2>/dev/null)
  [ -n "$previous" ] || die "$unit: dönülecek önceki sürüm yok."
  if [ "$unit" = api ]; then
    newer="$(comm -23 <(sort "/opt/okul/api/$current/okul-migrations.txt" 2>/dev/null) \
      <(sort "/opt/okul/api/$previous/okul-migrations.txt" 2>/dev/null) || true)"
    if [ -n "$newer" ]; then
      printf 'UYARI: geri dönülen API sürümünden sonra eklenmiş migration var; veritabanı şeması geri alınmaz:\n%s\n' "$newer"
    fi
  fi
  ln -sfn "$previous" "/opt/okul/$unit/current.new"
  mv -T "/opt/okul/$unit/current.new" "/opt/okul/$unit/current"
  say "$unit: $(basename "$current") → $(basename "$previous")"
done
if [[ ",$UNITS," == *",api,"* ]]; then systemctl restart okul-api; fi

for i in $(seq 1 30); do
  if curl -fsS --max-time 5 "http://127.0.0.1:$WEB_PORT/api/system-health" 2>/dev/null | grep -q '"status":"healthy"'; then
    say "geri dönüldü; sistem durumu: çalışıyor"
    exit 0
  fi
  sleep 1
done
die "geri dönüldü ama sistem durumu 'çalışıyor' değil; bkz. status.sh ve /var/log/okul."
