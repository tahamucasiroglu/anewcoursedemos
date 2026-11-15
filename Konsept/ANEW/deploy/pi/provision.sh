#!/bin/sh
# Pi'de (sudo ile) bir kez/her kurulumda çalışır: yalnızca "anewconcept" adlı kullanıcı, klasör, veritabanı ve
# servisleri oluşturur; başka uygulamalara (muskitakip, tmautoclip, tahakonsept) dokunmaz (spec 0001 D1, D3).
# Veritabanı şifresi ve Web ↔ Api servis anahtarı Pi'de üretilir; yalnızca /etc/anewconcept/*.env'de durur
# (repoya, loglara girmez). Loglar yalnızca /opt/anewconcept/logs/{api,web} altında (spec 0008 Req 5).
#   sudo sh provision.sh <yükleme-klasörü>
set -eu
SRC=${1:?yükleme klasörü gerekli}

id -u anewconcept >/dev/null 2>&1 || useradd --system --no-create-home --shell /usr/sbin/nologin anewconcept
install -d -o root -g root -m 755 /opt/anewconcept
install -d -o anewconcept -g anewconcept -m 750 /opt/anewconcept/logs /opt/anewconcept/logs/api /opt/anewconcept/logs/web
install -d -o root -g anewconcept -m 750 /etc/anewconcept

# 0.1.x logları /var/log/anewconcept'teydi; bir kez yeni yere taşınır, boş kalan eski klasör kaldırılır.
if [ -d /var/log/anewconcept ]; then
  for app in api web; do
    for f in /var/log/anewconcept/"$app"-*.log; do
      if [ -e "$f" ]; then mv "$f" "/opt/anewconcept/logs/$app/"; fi
    done
  done
  chown -R anewconcept:anewconcept /opt/anewconcept/logs
  rmdir /var/log/anewconcept 2>/dev/null || true
fi

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
# Web → Api iç uç servis anahtarı (plan 0008, Risk 2): bir kez üretilir, iki env dosyasına da yazılır.
if ! grep -q '^Internal__ServiceKey=' /etc/anewconcept/api.env; then
  umask 027
  printf 'Internal__ServiceKey=%s\n' "$(openssl rand -hex 32)" >> /etc/anewconcept/api.env
fi
# JWT imza anahtarı (plan 0002, B-46): bir kez üretilir, yalnızca api.env'de durur; değişirse tüm oturumlar düşer.
if ! grep -q '^Auth__SigningKey=' /etc/anewconcept/api.env; then
  umask 027
  printf 'Auth__SigningKey=%s\n' "$(openssl rand -hex 32)" >> /etc/anewconcept/api.env
fi
# İlk admin e-postası (spec 0011, plan 0011 Risk 8): Identity__AdminEmail satırı api.env'e elle (ya da kurulumu yapan
# orkestratörce) eklenir; gizli bilgi değildir ama repoda değeri yoktur. Bu betik o satırı yazmaz, silmez, ezmez; api.env
# yalnızca yoksa oluşturulur ve yalnızca eksik satırlar eklenir. Satır yoksa ya da boşsa hiçbir hesap admin olmaz; Api her
# açılışta rolleri bu ayara eşitler.
# Web çerez şifreleme anahtarları (plan 0002, B-46): kalıcı klasör; silinirse tüm oturumlar düşer.
install -d -o anewconcept -g anewconcept -m 700 /opt/anewconcept/keys /opt/anewconcept/keys/web
umask 027
# web.env her kurulumda api.env'deki anahtardan yeniden yazılır; buraya elle değişken eklemeyin (review I-4).
grep '^Internal__ServiceKey=' /etc/anewconcept/api.env > /etc/anewconcept/web.env
chown root:anewconcept /etc/anewconcept/web.env
chmod 640 /etc/anewconcept/web.env

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
