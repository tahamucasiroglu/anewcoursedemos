# Pi'de root olarak çalışır. Şifreyi ASLA basmaz: yalnızca uzunluğu ve eşleşme SAYILARI yazılır.
# stdin: geliştirme makinesindeki kurulum/dağıtım çıktıları (yerel loglar).
set -uo pipefail
pw="$(sed -n 's/.*Password=\([^;]*\).*/\1/p' /etc/okul/okul-api.env)"
[ -n "$pw" ] || { echo "şifre okunamadı"; exit 2; }
echo "şifre uzunluğu: ${#pw} (içerik basılmaz)"
pat="$(mktemp)"; trap 'rm -f "$pat"' EXIT
printf '%s\n' "$pw" > "$pat"
unset pw
count() { grep -F -c -f "$pat" 2>/dev/null || true; }
printf 'yerel kurulum/dağıtım çıktıları: %s\n' "$(count)"
printf '/var/log/okul: %s\n' "$(cat /var/log/okul/* 2>/dev/null | count)"
printf '/var/log/postgresql: %s\n' "$(zcat -f /var/log/postgresql/* 2>/dev/null | count)"
printf '/var/log/nginx: %s\n' "$(zcat -f /var/log/nginx/* 2>/dev/null | count)"
printf 'journal okul-api: %s\n' "$(journalctl -u okul-api --no-pager -o cat 2>/dev/null | count)"
printf 'journal postgresql: %s\n' "$(journalctl -u 'postgresql*' --no-pager -o cat 2>/dev/null | count)"
printf 'journal tümü (son 24 sa): %s\n' "$(journalctl --since -24h --no-pager -o cat 2>/dev/null | count)"
echo "== kontrol (arama çalışıyor mu): 'okul_app' geçen satır, /etc/okul/okul-api.env: $(grep -c okul_app /etc/okul/okul-api.env)"
echo "== PostgreSQL log ayarları (sunucu)"
sudo -u postgres psql -X -tA -c "select name||'='||setting from pg_settings where name in ('log_statement','log_min_error_statement','log_min_duration_statement','log_min_duration_sample') order by 1"
echo "== AC-9 log dosyası"
ls -l /var/log/okul/ | tail -n +2
