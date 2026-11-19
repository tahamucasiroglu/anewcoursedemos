# Pi'de root olarak çalışır (deploy.sh gönderir). Spec 0003 dağıtımı.
# Kullanım:
#   activate-remote.sh preflight <MIN_FREE_MB>                 — boş alan + kilit al (başka dağıtım varsa reddeder)
#   activate-remote.sh release                                  — kilidi bırak (dağıtım yarıda kalırsa)
#   activate-remote.sh activate <birimler: web,api> <SÜRÜM> <WEB_PORT> <KEEP> <HAZIRLIK_KLASÖRÜ>
# Etkinleştirme: sürüm klasörüne taşı → current bağlantısını çevir → yalnızca API değiştiyse API'yi yeniden başlat →
# sistem durumu "healthy" değilse önceki sürüme dön (migration uyarısıyla) ve hata ver (AC-7) → son KEEP sürüm
# dışındakileri sil (AC-6).
set -euo pipefail
. "$(cd "$(dirname "$0")" && pwd)/common.sh"

MODE="${1:?mod}"

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
    acquire_lock
    say "ön kontrol: ${free} MB boş, kilit alındı"
    ;;
  release)
    release_lock
    ;;
  activate)
    UNITS="${2:?birimler}"
    VERSION="${3:?sürüm}"
    WEB_PORT="${4:?WEB_PORT}"
    KEEP="${5:?KEEP}"
    STAGE="${6:?hazırlık klasörü}"
    [ -d "$LOCK" ] || die "Kilit yok; önce preflight çalışmalı."
    trap 'release_lock' EXIT
    # Hazırlık klasörü güvenli mi (review round 1, M-1): beklenen biçim, gerçek klasör (bağlantı değil), sahibi
    # dağıtımı başlatan kullanıcı; içindeki birimler de bağlantı olmamalı.
    [[ "$STAGE" =~ ^/tmp/okul-deploy\.[A-Za-z0-9]{10}$ ]] || die "Geçersiz hazırlık klasörü: $STAGE"
    { [ -d "$STAGE" ] && [ ! -L "$STAGE" ]; } || die "Hazırlık klasörü yok ya da bağlantı: $STAGE"
    [ "$(stat -c %U "$STAGE")" = "${SUDO_USER:-root}" ] || die "Hazırlık klasörünün sahibi beklenen kullanıcı değil."
    trap 'release_lock; rm -rf "$STAGE"' EXIT
    declare -A PREV=()
    IFS=',' read -r -a unit_list <<< "$UNITS"
    for unit in "${unit_list[@]}"; do
      { [ -d "$STAGE/$unit" ] && [ ! -L "$STAGE/$unit" ]; } || die "Yüklenen paket eksik ya da bağlantı: $unit"
    done
    for unit in "${unit_list[@]}"; do
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
        # Yeni sürüm migration uygulamış olabilir; şema geri alınmaz, kullanıcı uyarılır (review round 1, L-1)
        if [ "$unit" = api ]; then warn_newer_migrations "releases/$VERSION" "${PREV[$unit]}"; fi
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
