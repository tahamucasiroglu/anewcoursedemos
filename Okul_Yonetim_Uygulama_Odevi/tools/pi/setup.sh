#!/usr/bin/env bash
# Raspberry Pi ilk kurulumu (spec 0003). Tekrar çalıştırılabilir; mevcut veriyi ve şifreyi bozmaz (AC-12).
# Kullanım: bash tools/pi/setup.sh [all|disk|nginx|system|database|service]   (varsayılan: all)
# İlk kurulumda adımlar tek tek çalıştırılabilir (her biri kullanıcı onayıyla): nginx → system → database → service.
. "$(dirname "$0")/lib.sh"

STEP="${1:-all}"
case "$STEP" in all|disk|nginx|system|database|service) ;; *) die "bilinmeyen adım: $STEP" ;; esac

check_ssh
say "Pi kurulumu — adım: $STEP ($PI_SSH, $PI_ADDRESS)"
run_remote setup-remote.sh "$STEP" "$API_PORT" "$WEB_PORT" "$PI_ADDRESS" "$MIN_FREE_MB"
say "Tamam. Sonraki: bash tools/pi/deploy.sh   ·   Durum: bash tools/pi/status.sh"
