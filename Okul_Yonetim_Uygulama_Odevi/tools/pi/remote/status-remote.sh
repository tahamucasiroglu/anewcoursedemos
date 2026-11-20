# Pi'de root olarak çalışır (status.sh gönderir). Salt okuma durum raporu (spec 0003; VERIFY kanıtı).
# Hiçbir şeyi değiştirmez; şifre dosyasının yalnızca sahibi/izni gösterilir, içeriği asla.
set -uo pipefail

WEB_PORT="${1:-8080}"
pg() { sudo -u postgres psql -X -At "$@" 2>/dev/null; }

echo "== servis okul-api"
printf '  durum: %s · açılışta: %s · PID: %s · kullanıcı: %s\n' \
  "$(systemctl is-active okul-api 2>/dev/null)" "$(systemctl is-enabled okul-api 2>/dev/null)" \
  "$(systemctl show -p MainPID --value okul-api 2>/dev/null)" "$(systemctl show -p User --value okul-api 2>/dev/null)"
printf '  okul kullanıcısının kabuğu: %s\n' "$(getent passwd okul | cut -d: -f7)"
echo "== dinlenen portlar (okul)"
ss -ltnpH 2>/dev/null | awk '{print "  " $4 "  " $6}' | grep -E "OkulYonetim|nginx" || echo "  (yok)"
echo "== tüm dinlenen portlar"
printf '  %s\n' "$(ss -ltnH 2>/dev/null | awk '{print $4}' | sort -u | tr '\n' ' ')"
echo "== sürümler"
for unit in api web; do
  printf '  %s: etkin=%s · tutulan=%s\n' "$unit" "$(basename "$(readlink "/opt/okul/$unit/current" 2>/dev/null)" 2>/dev/null)" \
    "$(ls -1 "/opt/okul/$unit/releases" 2>/dev/null | tr '\n' ' ')"
done
echo "== sistem durumu (nginx üzerinden)"
printf '  %s\n' "$(curl -sS --max-time 5 "http://127.0.0.1:$WEB_PORT/api/system-health" 2>&1)"
echo "== veritabanı"
printf '  rol okul_app: %s\n' "$(pg -c "select 'süper='||rolsuper||' veritabanı_açar='||rolcreatedb||' rol_açar='||rolcreaterole||' giriş='||rolcanlogin from pg_roles where rolname='okul_app'")"
printf '  veritabanı okul sahibi: %s\n' "$(pg -c "select pg_get_userbyid(datdba) from pg_database where datname='okul'")"
printf '  uygulanan migration: %s\n' "$(pg -d okul -c 'select count(*) from "__EFMigrationsHistory"')"
printf '  pg_hba okul bloğu: %s\n' "$(grep -c '^# okul-begin' "$(pg -c 'show hba_file')" 2>/dev/null)"
echo "== gizli bilgi dosyası"
printf '  /etc/okul/okul-api.env: %s\n' "$(stat -c '%U:%G %a' /etc/okul/okul-api.env 2>/dev/null || echo yok)"
printf '  /etc/okul/okul-jwt.env: %s\n' "$(stat -c '%U:%G %a' /etc/okul/okul-jwt.env 2>/dev/null || echo yok)"
printf '  /etc/okul/okul-2fa.env: %s\n' "$(stat -c '%U:%G %a' /etc/okul/okul-2fa.env 2>/dev/null || echo yok)"
echo "== loglar"
ls -1 /var/log/okul 2>/dev/null | sed 's/^/  /' | tail -n 5
echo "== disk"
printf '  boş: %s MB\n' "$(df -Pm / | awk 'NR==2 {print $4}')"
