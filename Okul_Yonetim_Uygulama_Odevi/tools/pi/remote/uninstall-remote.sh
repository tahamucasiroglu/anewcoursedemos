# Pi'de root olarak çalışır (uninstall.sh gönderir). Spec 0003 kaldırma (AC-11): yalnızca bu projenin kurduklarını
# siler — servis, /opt/okul, /var/log/okul, /etc/okul, nginx site ayarı, okul veritabanı, okul_app rolü, pg_hba bloğu,
# okul sistem kullanıcısı. nginx, PostgreSQL ve diğer uygulamalar/veritabanları/portlar korunur.
# Kullanım: uninstall-remote.sh KALDIR   (ikinci güvenlik: onay metni burada da istenir)
set -euo pipefail

[ "${1:-}" = "KALDIR" ] || { printf 'Onay yok; hiçbir şey silinmedi.\n' >&2; exit 1; }

say() { printf '  %s\n' "$*"; }
pg() { sudo -u postgres psql -X -v ON_ERROR_STOP=1 -At "$@"; }
HBA_BEGIN_PREFIX='# okul-begin'
HBA_END='# okul-end'

if systemctl list-unit-files okul-api.service >/dev/null 2>&1; then
  systemctl disable --now okul-api >/dev/null 2>&1 || true
  rm -f /etc/systemd/system/okul-api.service
  systemctl daemon-reload
  say "servis okul-api silindi"
fi

if [ -e /etc/nginx/sites-available/okul.conf ] || [ -L /etc/nginx/sites-enabled/okul.conf ]; then
  rm -f /etc/nginx/sites-enabled/okul.conf /etc/nginx/sites-available/okul.conf
  if command -v nginx >/dev/null 2>&1 && nginx -t -q 2>/dev/null; then systemctl reload nginx || true; fi
  say "nginx site ayarı silindi (nginx korunur)"
fi

if systemctl is-active --quiet postgresql; then
  if [ "$(pg -c "select count(*) from pg_database where datname = 'okul'")" != "0" ]; then
    pg -c 'DROP DATABASE okul WITH (FORCE)' >/dev/null
    say "veritabanı okul silindi"
  fi
  if [ "$(pg -c "select count(*) from pg_roles where rolname = 'okul_app'")" != "0" ]; then
    pg -c 'DROP ROLE okul_app' >/dev/null
    say "veritabanı kullanıcısı okul_app silindi"
  fi
  hba="$(pg -c 'show hba_file')"
  if grep -q "^$HBA_BEGIN_PREFIX" "$hba"; then
    # Blok beklenen biçimde mi (review round 1, L-3): başlangıç ve bitiş işareti tam bir kez, aralarında tam 3 kural.
    # Değilse dosyaya dokunulmaz — diğer projelerin satırları yanlışlıkla silinmesin.
    begins="$(grep -c "^$HBA_BEGIN_PREFIX" "$hba" || true)"
    ends="$(grep -cx "$HBA_END" "$hba" || true)"
    inner="$(awk -v b="$HBA_BEGIN_PREFIX" -v e="$HBA_END" 'index($0,b)==1{s=1;next} s&&$0==e{s=0;next} s{n++} END{print n+0}' "$hba")"
    if [ "$begins" != "1" ] || [ "$ends" != "1" ] || [ "$inner" != "3" ]; then
      printf 'HATA: pg_hba.conf içindeki okul bloğu beklenen biçimde değil (başlangıç=%s bitiş=%s kural=%s); dosyaya dokunulmadı, elle kontrol edin.\n' \
        "$begins" "$ends" "$inner" >&2
      exit 1
    fi
    # Yedek yalnızca root'un erişebildiği /etc/okul içinde (tahmin edilebilir /tmp yolu yok; review round 1, M-1)
    install -d -m 700 -o root -g root /etc/okul
    backup="$(mktemp /etc/okul/pg_hba.conf.uninstall.XXXXXX)"
    cp -p "$hba" "$backup"
    awk -v b="$HBA_BEGIN_PREFIX" -v e="$HBA_END" 'index($0,b)==1{skip=1;next} skip&&$0==e{skip=0;next} !skip' \
      "$backup" > "$hba"
    if [ "$(pg -c 'select count(*) from pg_hba_file_rules where error is not null')" != "0" ]; then
      cp -p "$backup" "$hba"
      printf 'HATA: pg_hba.conf doğrulanamadı; eski hâli geri kondu.\n' >&2
      exit 1
    fi
    pg -c 'select pg_reload_conf()' >/dev/null
    rm -f "$backup"
    say "pg_hba okul bloğu silindi"
  fi
fi

rm -rf /opt/okul /var/log/okul /etc/okul /run/lock/okul-deploy
say "/opt/okul, /var/log/okul, /etc/okul silindi"
if id okul >/dev/null 2>&1; then
  userdel okul
  say "sistem kullanıcısı okul silindi"
fi
say "kaldırma tamamlandı"
