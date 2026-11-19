# Pi'de root olarak çalışır (deploy.sh gönderir). Spec 0003 dağıtımı.
# Kullanım:
#   activate-remote.sh preflight <MIN_FREE_MB>                 — boş alan + kilit al (başka dağıtım varsa reddeder)
#   activate-remote.sh release                                  — kilidi bırak (dağıtım yarıda kalırsa)
#   activate-remote.sh activate <birimler: web,api> <SÜRÜM> <WEB_PORT> <KEEP>
# Etkinleştirme: sürüm klasörüne taşı → current bağlantısını çevir → yalnızca API değiştiyse API'yi yeniden başlat →
# sistem durumu "healthy" değilse önceki sürüme dön ve hata ver (AC-7) → son KEEP sürüm dışındakileri sil (AC-6).
set -euo pipefail

LOCK=/run/lock/okul-deploy
MODE="${1:?mod}"

say() { printf '  %s\n' "$*"; }
die() { printf 'HATA: %s\n' "$*" >&2; exit 1; }

healthy() {
  local web_port="$1" i
  for i in $(seq 1 30); do
    if curl -fsS --max-time 5 "http://127.0.0.1:$web_port/api/system-health" 2>/dev/null | grep -q '"status":"healthy"' \
      && curl -fsS --max-time 5 "http://127.0.0.1:$web_port/" 2>/dev/null | grep -q 'id="root"'; then
      return 0
    fi
    sleep 1
  done
  return 1
}

switch_to() {
  # $1 birim, $2 hedef (releases/<sürüm>) — bağlantı atomik çevrilir
  ln -sfn "$2" "/opt/okul/$1/current.new"
  mv -T "/opt/okul/$1/current.new" "/opt/okul/$1/current"
}

case "$MODE" in
  preflight)
    MIN_FREE_MB="${2:?MIN_FREE_MB}"
    free="$(df -Pm / | awk 'NR==2 {print $4}')"
    [ "$free" -ge "$MIN_FREE_MB" ] || die "Pi'de yeterli boş alan yok: ${free} MB (en az ${MIN_FREE_MB} MB gerekli)."
    # 30 dakikadan eski kilit yarıda kalmış bir dağıtımdan kalmıştır
    if [ -d "$LOCK" ] && [ -n "$(find "$LOCK" -maxdepth 0 -mmin +30 2>/dev/null)" ]; then
      rmdir "$LOCK"
    fi
    mkdir "$LOCK" 2>/dev/null || die "Başka bir dağıtım/geri alma sürüyor (kilit: $LOCK). Bitmesini bekleyin."
    say "ön kontrol: ${free} MB boş, kilit alındı"
    ;;
  release)
    rmdir "$LOCK" 2>/dev/null || true
    ;;
  activate)
    UNITS="${2:?birimler}"
    VERSION="${3:?sürüm}"
    WEB_PORT="${4:?WEB_PORT}"
    KEEP="${5:?KEEP}"
    STAGE="/tmp/okul-deploy-$VERSION"
    [ -d "$LOCK" ] || die "Kilit yok; önce preflight çalışmalı."
    trap 'rmdir "$LOCK" 2>/dev/null || true; rm -rf "$STAGE"' EXIT
    declare -A PREV=()
    IFS=',' read -r -a unit_list <<< "$UNITS"
    for unit in "${unit_list[@]}"; do
      [ -d "$STAGE/$unit" ] || die "Yüklenen paket eksik: $unit"
      PREV[$unit]="$(readlink "/opt/okul/$unit/current" 2>/dev/null || true)"
      rm -rf "/opt/okul/$unit/releases/$VERSION"
      mv "$STAGE/$unit" "/opt/okul/$unit/releases/$VERSION"
      chown -R root:root "/opt/okul/$unit/releases/$VERSION"
      chmod -R u=rwX,go=rX "/opt/okul/$unit/releases/$VERSION"
      if [ "$unit" = api ]; then
        chmod 755 "/opt/okul/api/releases/$VERSION/OkulYonetim.WebAPI"
      fi
      switch_to "$unit" "releases/$VERSION"
    done
    if [[ ",$UNITS," == *",api,"* ]]; then
      systemctl restart okul-api
    fi
    if healthy "$WEB_PORT"; then
      for unit in "${unit_list[@]}"; do
        # En yeni KEEP sürüm kalır; etkin olan zaten en yenisidir
        ls -1dt "/opt/okul/$unit/releases/"*/ 2>/dev/null | tail -n +"$((KEEP + 1))" | xargs -r rm -rf
      done
      say "sürüm $VERSION etkin ($UNITS); sistem durumu: çalışıyor"
      exit 0
    fi
    printf 'HATA: sürüm %s sağlık kontrolünden geçmedi; önceki sürüme dönülüyor.\n' "$VERSION" >&2
    for unit in "${unit_list[@]}"; do
      if [ -n "${PREV[$unit]}" ]; then
        switch_to "$unit" "${PREV[$unit]}"
      else
        rm -f "/opt/okul/$unit/current"
      fi
      rm -rf "/opt/okul/$unit/releases/$VERSION"
    done
    if [[ ",$UNITS," == *",api,"* ]]; then
      if [ -e /opt/okul/api/current ]; then systemctl restart okul-api; else systemctl stop okul-api; fi
    fi
    if healthy "$WEB_PORT"; then
      printf 'HATA: dağıtım başarısız; önceki sürüm geri getirildi ve çalışıyor.\n' >&2
    else
      printf 'HATA: dağıtım başarısız; önceki sürüm de sağlık kontrolünden geçmedi (ilk dağıtım olabilir). Bkz. status.sh, /var/log/okul.\n' >&2
    fi
    exit 1
    ;;
  *) die "bilinmeyen mod: $MODE" ;;
esac
