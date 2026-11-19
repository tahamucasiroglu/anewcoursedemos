# Pi betiklerinin ortak parçası (spec 0003): ayarı yükler, Pi'ye SSH ile uzak betik çalıştırır.
# Geliştirme makinesinde (Windows: Git Bash) çalışır. "set -x" kullanılmaz (gizli bilgi sızmasın).
set -euo pipefail

PI_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$PI_DIR/../.." && pwd)"
# shellcheck source=pi.conf
. "$PI_DIR/pi.conf"

say() { printf '%s\n' "$*"; }
die() { printf 'HATA: %s\n' "$*" >&2; exit 1; }

# Pi'ye anahtarla ulaşılabiliyor mu (parola sormaz; ulaşılamazsa açık hata)
check_ssh() {
  ssh -o BatchMode=yes -o ConnectTimeout=8 "$PI_SSH" true 2>/dev/null \
    || die "Pi'ye ulaşılamıyor (SSH tanımı: $PI_SSH, adres: $PI_ADDRESS). Pi açık mı, ağda mı?"
}

# remote/ klasörünü Pi'de geçici bir klasöre açar ve verilen betiği root olarak çalıştırır; sonra klasörü siler.
# Argümanlar düz kelimelerdir (boşluk/tırnak içermez).
run_remote() {
  local script="$1"
  shift
  (cd "$PI_DIR/remote" && tar -cf - .) | ssh -o BatchMode=yes "$PI_SSH" \
    "d=\$(mktemp -d) && tar -C \"\$d\" -xf - && sudo bash \"\$d/$script\" $*; rc=\$?; rm -rf \"\$d\"; exit \$rc"
}
