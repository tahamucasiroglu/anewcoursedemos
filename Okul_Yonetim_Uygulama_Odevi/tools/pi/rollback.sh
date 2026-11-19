#!/usr/bin/env bash
# Raspberry Pi'de elle önceki sürüme dönüş (spec 0003, AC-7). Veritabanı şeması geri alınmaz; geri dönülen API
# sürümünden sonra migration eklenmişse uyarı verilir.
# Kullanım: bash tools/pi/rollback.sh [all|web|api]   (varsayılan: all)
. "$(dirname "$0")/lib.sh"

case "${1:-all}" in
  all) UNITS="web,api" ;;
  web) UNITS="web" ;;
  api) UNITS="api" ;;
  *) die "bilinmeyen hedef: $1 (all|web|api)" ;;
esac
check_ssh
say "Geri alma: $UNITS"
run_remote rollback-remote.sh "$UNITS" "$WEB_PORT"
