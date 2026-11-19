#!/usr/bin/env bash
# Raspberry Pi durum raporu (spec 0003) — salt okuma: servis, portlar, sürümler, veritabanı yetkileri, gizli bilgi
# dosyasının izni (içeriği değil), loglar, disk. Hiçbir şeyi değiştirmez.
# Kullanım: bash tools/pi/status.sh
. "$(dirname "$0")/lib.sh"

check_ssh
say "Pi durumu ($PI_SSH, http://$PI_ADDRESS:$WEB_PORT)"
run_remote status-remote.sh "$WEB_PORT"
