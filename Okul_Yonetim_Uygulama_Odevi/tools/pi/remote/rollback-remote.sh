# Pi'de root olarak çalışır (rollback.sh gönderir). Elle önceki sürüme dönüş (spec 0003, AC-7).
# Kullanım: rollback-remote.sh <birimler: web,api> <WEB_PORT>
# Önce tüm birimlerin dönülecek sürümü bulunur; biri yoksa hiçbir şey değiştirilmez (review round 1, L-4).
# Veritabanı şeması geri alınmaz; geri dönülen API sürümünden sonra eklenmiş migration varsa uyarı verilir (Ö-2).
set -euo pipefail
. "$(cd "$(dirname "$0")" && pwd)/common.sh"

UNITS="${1:?birimler}"
WEB_PORT="${2:?WEB_PORT}"

acquire_lock
trap 'release_lock' EXIT

declare -A CURRENT=() PREVIOUS=()
IFS=',' read -r -a unit_list <<< "$UNITS"
for unit in "${unit_list[@]}"; do
  current="$(readlink "/opt/okul/$unit/current" 2>/dev/null || true)"
  [ -n "$current" ] || die "$unit: etkin sürüm yok; hiçbir şey değiştirilmedi."
  previous=""
  while IFS= read -r dir; do
    rel="releases/$(basename "$dir")"
    if [ "$rel" != "$current" ]; then previous="$rel"; break; fi
  done < <(ls -1dt "/opt/okul/$unit/releases/"*/ 2>/dev/null)
  [ -n "$previous" ] || die "$unit: dönülecek önceki sürüm yok; hiçbir şey değiştirilmedi."
  CURRENT[$unit]="$current"
  PREVIOUS[$unit]="$previous"
done

for unit in "${unit_list[@]}"; do
  if [ "$unit" = api ]; then warn_newer_migrations "${CURRENT[$unit]}" "${PREVIOUS[$unit]}"; fi
  ln -sfn "${PREVIOUS[$unit]}" "/opt/okul/$unit/current.new"
  mv -T "/opt/okul/$unit/current.new" "/opt/okul/$unit/current"
  say "$unit: $(basename "${CURRENT[$unit]}") → $(basename "${PREVIOUS[$unit]}")"
done
if [[ ",$UNITS," == *",api,"* ]]; then systemctl restart okul-api; fi

if healthy "$WEB_PORT"; then
  say "geri dönüldü; sistem durumu: çalışıyor"
else
  die "geri dönüldü ama sistem durumu 'çalışıyor' değil; bkz. status.sh ve /var/log/okul."
fi
