#!/usr/bin/env bash
# Taha Konsept — Pi'de bir sürümü kurar (deploy/publish-pi.ps1 çağırır; elle de çalışır).
#
# Kullanım: sudo bash install-release.sh <sürüm-adı> <paket-klasörü>
#   <paket-klasörü> içinde: api/ (TahaKonsept.Api), web/ (TahaKonsept.Web), pi/ (bu betik + unit dosyaları + rollback.sh)
#
# Yaptıkları:
#   1. /opt/tahakonsept/releases/<sürüm-adı>/{api,web} oluşturur (root sahipli, salt okunur).
#   2. systemd unit'lerini /etc/systemd/system'e kurar, rollback.sh'ı /opt/tahakonsept/bin'e koyar.
#   3. /var/lib/tahakonsept altındaki veri klasörlerini (App_Data/keys, App_Data/artifacts, logs) garanti eder.
#   4. /opt/tahakonsept/current sembolik bağını atomik olarak yeni sürüme çevirir, eski hedefi 'previous' olarak saklar.
#   5. Servisleri yeniden başlatır, sağlık kontrolü yapar (API status=ok + database=ready + yeni sürüm; "degraded" başarı
#      sayılmaz); başarısızsa önceki sürüme otomatik geri döner.
#   6. Son KEEP_RELEASES (varsayılan 5) sürümü tutar, eskileri siler (current/previous asla silinmez).
set -euo pipefail

RELEASE_NAME="${1:?sürüm adı gerekli (ör. 0.1.0-20261114-120000)}"
BUNDLE_DIR="${2:?paket klasörü gerekli}"

APP_ROOT=/opt/tahakonsept
RELEASES_DIR="$APP_ROOT/releases"
DATA_ROOT=/var/lib/tahakonsept
ENV_FILE="$DATA_ROOT/tahakonsept.env"
SERVICE_USER=tahakonsept
KEEP_RELEASES="${KEEP_RELEASES:-5}"
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-90}"
API_HEALTH_URL=http://127.0.0.1:7111/api/health
WEB_URL=http://127.0.0.1:7110/

log() { echo "[install] $*"; }
die() { echo "[install] HATA: $*" >&2; exit 1; }

[[ $EUID -eq 0 ]] || die "root olarak çalıştırın (sudo)."
[[ "$RELEASE_NAME" =~ ^[A-Za-z0-9._-]+$ ]] || die "geçersiz sürüm adı: $RELEASE_NAME"
[[ -f "$BUNDLE_DIR/api/TahaKonsept.Api" ]] || die "$BUNDLE_DIR/api/TahaKonsept.Api yok."
[[ -f "$BUNDLE_DIR/web/TahaKonsept.Web" ]] || die "$BUNDLE_DIR/web/TahaKonsept.Web yok."
id "$SERVICE_USER" >/dev/null 2>&1 || die "sistem kullanıcısı $SERVICE_USER yok."

TARGET="$RELEASES_DIR/$RELEASE_NAME"
if [[ -e "$TARGET" ]]; then die "$TARGET zaten var."; fi

# --- 1. Sürüm klasörü ---------------------------------------------------------------------------
log "Sürüm klasörü: $TARGET"
mkdir -p "$RELEASES_DIR" "$APP_ROOT/bin"
mkdir "$TARGET"
mv "$BUNDLE_DIR/api" "$TARGET/api"
mv "$BUNDLE_DIR/web" "$TARGET/web"
echo "$RELEASE_NAME" > "$TARGET/RELEASE"
chown -R root:root "$TARGET"
chmod -R u=rwX,go=rX "$TARGET"
chmod 0755 "$TARGET/api/TahaKonsept.Api" "$TARGET/web/TahaKonsept.Web"

# --- 2. Unit dosyaları + yardımcı betikler ------------------------------------------------------
units_changed=0
for unit in tahakonsept-api.service tahakonsept-web.service; do
  src="$BUNDLE_DIR/pi/$unit"
  dst="/etc/systemd/system/$unit"
  [[ -f "$src" ]] || die "$src yok."
  if ! cmp -s "$src" "$dst" 2>/dev/null; then
    install -m 0644 -o root -g root "$src" "$dst"
    units_changed=1
    log "Unit güncellendi: $dst"
  fi
done
for script in rollback.sh install-release.sh; do
  if [[ -f "$BUNDLE_DIR/pi/$script" ]]; then
    install -m 0755 -o root -g root "$BUNDLE_DIR/pi/$script" "$APP_ROOT/bin/$script"
  fi
done
if [[ $units_changed -eq 1 ]]; then systemctl daemon-reload; fi

# --- 3. Veri klasörleri ---------------------------------------------------------------------------
install -d -m 0750 -o "$SERVICE_USER" -g "$SERVICE_USER" "$DATA_ROOT"
install -d -m 0750 -o "$SERVICE_USER" -g "$SERVICE_USER" "$DATA_ROOT/App_Data" "$DATA_ROOT/App_Data/artifacts" "$DATA_ROOT/logs"
install -d -m 0700 -o "$SERVICE_USER" -g "$SERVICE_USER" "$DATA_ROOT/App_Data/keys"
if [[ -f "$ENV_FILE" ]]; then
  chown "$SERVICE_USER:$SERVICE_USER" "$ENV_FILE"
  chmod 0600 "$ENV_FILE"
else
  log "UYARI: $ENV_FILE yok — API veritabanına bağlanamaz (TAHAKONSEPT_DB_PASSWORD). Bkz. docs/DEPLOY.md."
fi

# --- 4. current bağını çevir ------------------------------------------------------------------------
PREVIOUS_TARGET=""
if [[ -L "$APP_ROOT/current" ]]; then
  PREVIOUS_TARGET="$(readlink -f "$APP_ROOT/current")"
fi

switch_current() {
  local to="$1"
  ln -sfn "$to" "$APP_ROOT/current.tmp"
  mv -Tf "$APP_ROOT/current.tmp" "$APP_ROOT/current"
}

switch_current "$TARGET"
if [[ -n "$PREVIOUS_TARGET" && "$PREVIOUS_TARGET" != "$TARGET" ]]; then
  ln -sfn "$PREVIOUS_TARGET" "$APP_ROOT/previous.tmp"
  mv -Tf "$APP_ROOT/previous.tmp" "$APP_ROOT/previous"
fi
log "current → $TARGET (önceki: ${PREVIOUS_TARGET:-yok})"

# --- 5. Servisleri başlat + sağlık kontrolü ---------------------------------------------------------
wait_for() {
  local url="$1" deadline=$((SECONDS + HEALTH_TIMEOUT))
  while (( SECONDS < deadline )); do
    if curl -fs -o /dev/null --max-time 5 "$url" 2>/dev/null; then return 0; fi
    sleep 2
  done
  return 1
}

# API 200 dönse de 'degraded' (veritabanı hazır değil) başarı sayılmaz: status=ok + database=ready (ve verildiyse sürüm) beklenir.
wait_for_api() {
  local expected_version="${1:-}" deadline=$((SECONDS + HEALTH_TIMEOUT)) body=""
  while (( SECONDS < deadline )); do
    body="$(curl -fs --max-time 5 "$API_HEALTH_URL" 2>/dev/null || true)"
    if [[ "$body" == *'"status":"ok"'* && "$body" == *'"database":"ready"'* ]] && { [[ -z "$expected_version" ]] || [[ "$body" == *"\"version\":\"$expected_version\""* ]]; }; then
      return 0
    fi
    sleep 2
  done
  log "API sağlıklı değil (son yanıt: ${body:-yok})"
  return 1
}

EXPECTED_VERSION="${RELEASE_NAME%-*-*}"
[[ "$EXPECTED_VERSION" == "$RELEASE_NAME" ]] && EXPECTED_VERSION=""

restart_services() {
  systemctl enable tahakonsept-api.service tahakonsept-web.service >/dev/null 2>&1
  systemctl restart tahakonsept-api.service
  systemctl restart tahakonsept-web.service
}

restart_services
if wait_for_api "$EXPECTED_VERSION" && wait_for "$WEB_URL"; then
  log "Sağlık kontrolü geçti: $(curl -fsS --max-time 5 "$API_HEALTH_URL")"
else
  log "Sağlık kontrolü BAŞARISIZ. Son loglar:"
  journalctl -u tahakonsept-api.service -n 40 --no-pager || true
  journalctl -u tahakonsept-web.service -n 20 --no-pager || true
  if [[ -n "$PREVIOUS_TARGET" && -d "$PREVIOUS_TARGET" && "$PREVIOUS_TARGET" != "$TARGET" ]]; then
    log "Önceki sürüme geri dönülüyor: $PREVIOUS_TARGET"
    switch_current "$PREVIOUS_TARGET"
    restart_services
    if wait_for_api; then log "Geri dönüş tamam."; else log "Geri dönülen sürüm de sağlıksız!"; fi
  fi
  die "kurulum başarısız: $RELEASE_NAME"
fi

# --- 6. Eski sürümleri temizle ----------------------------------------------------------------------
current_real="$(readlink -f "$APP_ROOT/current")"
previous_real="$( [[ -L "$APP_ROOT/previous" ]] && readlink -f "$APP_ROOT/previous" || true )"
mapfile -t all_releases < <(ls -1dt "$RELEASES_DIR"/*/ 2>/dev/null | sed 's:/*$::')
count=0
for rel in "${all_releases[@]}"; do
  count=$((count + 1))
  if (( count > KEEP_RELEASES )) && [[ "$rel" != "$current_real" && "$rel" != "$previous_real" ]]; then
    log "Eski sürüm siliniyor: $rel"
    rm -rf -- "$rel"
  fi
done

log "Kurulum tamam: $RELEASE_NAME"
