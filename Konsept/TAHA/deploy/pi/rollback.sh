#!/usr/bin/env bash
# Taha Konsept — Pi'de önceki (ya da belirtilen) sürüme geri döner.
#
# Kullanım:
#   sudo /opt/tahakonsept/bin/rollback.sh            # 'previous' bağının gösterdiği sürüme (yoksa current'tan bir önceki)
#   sudo /opt/tahakonsept/bin/rollback.sh --list     # kurulu sürümleri listeler
#   sudo /opt/tahakonsept/bin/rollback.sh <sürüm-adı> # belirli bir sürüme (releases/ altındaki klasör adı)
#
# Not: veritabanı migration'ları geri alınmaz. Şema değiştiren bir sürümden geri dönerken docs/DEPLOY.md'ye bakın.
set -euo pipefail

APP_ROOT=/opt/tahakonsept
RELEASES_DIR="$APP_ROOT/releases"
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-90}"
API_HEALTH_URL=http://127.0.0.1:7111/api/health
WEB_URL=http://127.0.0.1:7110/

log() { echo "[rollback] $*"; }
die() { echo "[rollback] HATA: $*" >&2; exit 1; }

current_real="$( [[ -L "$APP_ROOT/current" ]] && readlink -f "$APP_ROOT/current" || true )"

if [[ "${1:-}" == "--list" ]]; then
  for rel in $(ls -1t "$RELEASES_DIR"); do
    marker=""
    [[ "$RELEASES_DIR/$rel" == "$current_real" ]] && marker=" (current)"
    if [[ -L "$APP_ROOT/previous" && "$RELEASES_DIR/$rel" == "$(readlink -f "$APP_ROOT/previous")" ]]; then marker="$marker (previous)"; fi
    echo "$rel$marker"
  done
  exit 0
fi

[[ $EUID -eq 0 ]] || die "root olarak çalıştırın (sudo)."

if [[ -n "${1:-}" ]]; then
  [[ "$1" =~ ^[A-Za-z0-9._-]+$ ]] || die "geçersiz sürüm adı: $1"
  target="$RELEASES_DIR/$1"
elif [[ -L "$APP_ROOT/previous" ]] && [[ "$(readlink -f "$APP_ROOT/previous")" != "$current_real" ]]; then
  target="$(readlink -f "$APP_ROOT/previous")"
else
  # previous yoksa: current'tan daha eski en yeni sürüm (değişiklik zamanına göre).
  target=""
  found_current=0
  for rel in $(ls -1dt "$RELEASES_DIR"/*/ | sed 's:/*$::'); do
    if [[ $found_current -eq 1 ]]; then target="$rel"; break; fi
    [[ "$rel" == "$current_real" ]] && found_current=1
  done
  [[ -n "$target" ]] || die "geri dönülecek önceki sürüm bulunamadı."
fi

[[ -d "$target" ]] || die "sürüm yok: $target"
[[ "$target" != "$current_real" ]] || die "zaten bu sürümdesiniz: $target"

log "current: ${current_real:-yok} → $target"
ln -sfn "$target" "$APP_ROOT/current.tmp"
mv -Tf "$APP_ROOT/current.tmp" "$APP_ROOT/current"
if [[ -n "$current_real" ]]; then
  ln -sfn "$current_real" "$APP_ROOT/previous.tmp"
  mv -Tf "$APP_ROOT/previous.tmp" "$APP_ROOT/previous"
fi

systemctl restart tahakonsept-api.service
systemctl restart tahakonsept-web.service

wait_for() {
  local url="$1" deadline=$((SECONDS + HEALTH_TIMEOUT))
  while (( SECONDS < deadline )); do
    if curl -fs -o /dev/null --max-time 5 "$url" 2>/dev/null; then return 0; fi
    sleep 2
  done
  return 1
}

if wait_for "$API_HEALTH_URL" && wait_for "$WEB_URL"; then
  log "Geri dönüş tamam: $(curl -fsS --max-time 5 "$API_HEALTH_URL")"
else
  journalctl -u tahakonsept-api.service -n 40 --no-pager || true
  die "geri dönülen sürüm sağlık kontrolünden geçmedi: $target"
fi
