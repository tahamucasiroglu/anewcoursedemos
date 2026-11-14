#!/bin/sh
# Pi'de (sudo ile) bir kez/her kurulumda çalışır: yalnızca "anewconcept" adlı kullanıcı, klasör, veritabanı ve
# servisleri oluşturur; başka uygulamalara (muskitakip, tmautoclip, tahakonsept) dokunmaz (spec 0001 D1, D3).
# Veritabanı şifresi Pi'de üretilir ve yalnızca /etc/anewconcept/api.env'de durur (repoya, loglara girmez).
#   sudo sh provision.sh <yükleme-klasörü>
set -eu
SRC=${1:?yükleme klasörü gerekli}

id -u anewconcept >/dev/null 2>&1 || useradd --system --no-create-home --shell /usr/sbin/nologin anewconcept
install -d -o root -g root -m 755 /opt/anewconcept
install -d -o anewconcept -g anewconcept -m 750 /var/log/anewconcept
install -d -o root -g anewconcept -m 750 /etc/anewconcept

if [ ! -f /etc/anewconcept/api.env ]; then
  password=$(openssl rand -hex 24)
  umask 027
  printf 'ConnectionStrings__Konsept=Host=127.0.0.1;Port=5432;Database=anewconcept;Username=anewconcept;Password=%s\n' "$password" > /etc/anewconcept/api.env
  chown root:anewconcept /etc/anewconcept/api.env
  chmod 640 /etc/anewconcept/api.env
  # Şifre psql'e stdin'den gider (printf kabuk yerleşiği): süreç listesinde görünmez (review I-5).
  if sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='anewconcept'" | grep -q 1; then
    verb=ALTER
  else
    verb=CREATE
  fi
  printf "%s ROLE anewconcept WITH LOGIN PASSWORD '%s';\n" "$verb" "$password" | sudo -u postgres psql -q -v ON_ERROR_STOP=1
fi
sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='anewconcept'" | grep -q 1 \
  || sudo -u postgres psql -q -c "CREATE DATABASE anewconcept OWNER anewconcept ENCODING 'UTF8' TEMPLATE template0"

for app in api web; do
  rm -rf "/opt/anewconcept/$app.new"
  cp -r "$SRC/$app" "/opt/anewconcept/$app.new"
  chown -R root:anewconcept "/opt/anewconcept/$app.new"
  chmod -R o-rwx "/opt/anewconcept/$app.new"
  rm -rf "/opt/anewconcept/$app.old"
  [ -d "/opt/anewconcept/$app" ] && mv "/opt/anewconcept/$app" "/opt/anewconcept/$app.old"
  mv "/opt/anewconcept/$app.new" "/opt/anewconcept/$app"
  install -m 644 "$SRC/anewconcept-$app.service" "/etc/systemd/system/anewconcept-$app.service"
done
chmod 750 /opt/anewconcept/api/Konsept.Api /opt/anewconcept/web/Konsept.Web

systemctl daemon-reload
systemctl enable anewconcept-api.service anewconcept-web.service >/dev/null
systemctl restart anewconcept-api.service
systemctl restart anewconcept-web.service
rm -rf /opt/anewconcept/api.old /opt/anewconcept/web.old
