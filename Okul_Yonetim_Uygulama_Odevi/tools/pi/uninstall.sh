#!/usr/bin/env bash
# Raspberry Pi'den kaldırma (spec 0003, AC-11). YALNIZCA kullanıcı "kaldır" dediğinde çalıştırılır.
# Açık onay olmadan hiçbir şey silmez: ya "--onay KALDIR" verilir ya da sorulduğunda KALDIR yazılır.
# Silinir: servis, uygulama dosyaları, loglar, nginx site ayarı, okul veritabanı ve okul_app kullanıcısı, pg_hba bloğu.
# Korunur: nginx, PostgreSQL, Pi'deki diğer uygulamalar/veritabanları/portlar.
# Kullanım: bash tools/pi/uninstall.sh [--onay KALDIR]
. "$(dirname "$0")/lib.sh"

CONFIRM=""
if [ "${1:-}" = "--onay" ]; then
  CONFIRM="${2:-}"
elif [ -t 0 ]; then
  printf "Pi'deki Okul Yönetim kurulumu (veritabanı dahil) silinecek. Onay için KALDIR yazın: "
  read -r CONFIRM
fi
if [ "$CONFIRM" != "KALDIR" ]; then
  say "Onay verilmedi; hiçbir şey silinmedi."
  exit 1
fi

check_ssh
say "Kaldırılıyor ($PI_SSH)…"
run_remote uninstall-remote.sh KALDIR
