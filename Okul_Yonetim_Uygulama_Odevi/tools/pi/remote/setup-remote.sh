# Pi'de root olarak çalışır (setup.sh gönderir). Spec 0003 kurulumu; her adım tekrar çalıştırılabilir (AC-12).
# Kullanım: setup-remote.sh <adım: disk|nginx|system|database|service|all> <API_PORT> <WEB_PORT> <PI_ADDRESS> <MIN_FREE_MB>
# Gizli bilgi: veritabanı şifresi burada üretilir; yalnızca /etc/okul/okul-api.env'ye (root, 600) yazılır, psql'e
# stdin ile verilir (süreç listesinde görünmez), hiçbir zaman ekrana basılmaz. "set -x" kullanılmaz.
set -euo pipefail
umask 022

STEP="${1:?adım}"
API_PORT="${2:?API_PORT}"
WEB_PORT="${3:?WEB_PORT}"
PI_ADDRESS="${4:?PI_ADDRESS}"
MIN_FREE_MB="${5:?MIN_FREE_MB}"
HERE="$(cd "$(dirname "$0")" && pwd)"

APP_USER=okul
DB_NAME=okul
DB_ROLE=okul_app
ETC_DIR=/etc/okul
ENV_FILE=/etc/okul/okul-api.env
JWT_FILE=/etc/okul/okul-jwt.env
TWOFA_FILE=/etc/okul/okul-2fa.env
HBA_BEGIN='# okul-begin (spec 0003 — Okul Yönetim; uninstall-remote.sh siler)'
HBA_END='# okul-end'
# Şifre içeren SQL bu oturumda sunucu loguna yazılmasın (review round 1, L-2; round 2, L-1): komut logu, hata
# durumunda STATEMENT satırı ve yavaş komut logu (duration) kapalı
NO_LOG_SQL="SET log_statement = 'none'; SET log_min_error_statement = 'panic'; SET log_min_duration_statement = -1; SET log_min_duration_sample = -1; "

say() { printf '  %s\n' "$*"; }
die() { printf 'HATA: %s\n' "$*" >&2; exit 1; }
pg() { sudo -u postgres psql -X -v ON_ERROR_STOP=1 -At "$@"; }

step_disk() {
  local free
  free="$(df -Pm / | awk 'NR==2 {print $4}')"
  [ "$free" -ge "$MIN_FREE_MB" ] || die "Pi'de yeterli boş alan yok: ${free} MB (en az ${MIN_FREE_MB} MB gerekli)."
  say "disk: ${free} MB boş"
}

step_nginx() {
  install -d -m 700 -o root -g root "$ETC_DIR"
  if ! command -v nginx >/dev/null 2>&1; then
    say "nginx kuruluyor (apt)…"
    DEBIAN_FRONTEND=noninteractive apt-get update -qq
    DEBIAN_FRONTEND=noninteractive apt-get install -y -qq nginx >/dev/null
    # Yeni kurulan nginx'in varsayılan sitesi 80'i dinler; Pi'deki diğer işlere yer kalsın diye kapatılır.
    rm -f /etc/nginx/sites-enabled/default
  fi
  sed -e "s/@WEB_PORT@/$WEB_PORT/g" -e "s/@API_PORT@/$API_PORT/g" "$HERE/nginx-okul.conf" > /etc/nginx/sites-available/okul.conf
  ln -sfn /etc/nginx/sites-available/okul.conf /etc/nginx/sites-enabled/okul.conf
  nginx -t -q || die "nginx ayarı geçersiz."
  systemctl enable --now nginx >/dev/null 2>&1
  systemctl reload nginx
  say "nginx: site okul.conf, port $WEB_PORT"
}

step_system() {
  if ! id "$APP_USER" >/dev/null 2>&1; then
    useradd --system --no-create-home --home-dir /nonexistent --shell /usr/sbin/nologin "$APP_USER"
  fi
  install -d -m 755 -o root -g root /opt/okul /opt/okul/api /opt/okul/api/releases /opt/okul/web /opt/okul/web/releases
  install -d -m 750 -o "$APP_USER" -g "$APP_USER" /var/log/okul
  install -d -m 700 -o root -g root "$ETC_DIR"
  say "sistem: kullanıcı $APP_USER (oturum açamaz), /opt/okul, /var/log/okul, $ETC_DIR"
}

write_env_file() {
  # $1 = şifre. Dosya önce 600 izinle oluşur, sonra yerine taşınır; içerik ekrana basılmaz.
  local tmp
  tmp="$(mktemp "$ETC_DIR/.okul-api.env.XXXXXX")"
  chmod 600 "$tmp"
  printf 'ConnectionStrings__OkulDb=Host=127.0.0.1;Port=5432;Database=%s;Username=%s;Password=%s\n' \
    "$DB_NAME" "$DB_ROLE" "$1" > "$tmp"
  chown root:root "$tmp"
  mv -f "$tmp" "$ENV_FILE"
}

# Oturum imza anahtarı (spec 0004): ayrı dosyada (yalnızca root, 600); yoksa bir kez üretilir, varsa dokunulmaz
# (değişseydi herkesin oturumu kapanırdı). Veritabanı şifre dosyası okunmaz/değişmez. Anahtar ekrana basılmaz.
ensure_jwt_key() {
  if [ -s "$JWT_FILE" ]; then
    say "oturum imza anahtarı zaten var (dokunulmadı)"
    return
  fi
  local tmp
  tmp="$(mktemp "$ETC_DIR/.okul-jwt.env.XXXXXX")"
  chmod 600 "$tmp"
  printf 'Jwt__SigningKey=%s\n' "$(openssl rand -base64 48 | tr -d '\n')" > "$tmp"
  chown root:root "$tmp"
  mv -f "$tmp" "$JWT_FILE"
  say "oturum imza anahtarı üretildi: $JWT_FILE ($(stat -c '%U:%G %a' "$JWT_FILE"))"
}

# İki adımlı doğrulama şifreleme anahtarı (spec 0007): platform yöneticisinin doğrulama anahtarı veritabanında bununla
# şifrelenir. Yalnızca yoksa üretilir (değişirse kurulu doğrulamalar çözülemez); root 600; ekrana basılmaz.
ensure_twofactor_key() {
  if [ -s "$TWOFA_FILE" ]; then
    say "iki adımlı doğrulama anahtarı zaten var (dokunulmadı)"
    return
  fi
  local tmp
  tmp="$(mktemp "$ETC_DIR/.okul-2fa.env.XXXXXX")"
  chmod 600 "$tmp"
  printf 'TwoFactor__EncryptionKey=%s\n' "$(openssl rand -base64 32 | tr -d '\n')" > "$tmp"
  chown root:root "$tmp"
  mv -f "$tmp" "$TWOFA_FILE"
  say "iki adımlı doğrulama anahtarı üretildi: $TWOFA_FILE ($(stat -c '%U:%G %a' "$TWOFA_FILE"))"
}

ensure_hba_block() {
  local hba backup errors
  hba="$(pg -c 'show hba_file')"
  if grep -qF "$HBA_BEGIN" "$hba"; then
    say "pg_hba: okul bloğu zaten var"
    return
  fi
  backup="$ETC_DIR/pg_hba.conf.bak-$(date -u +%Y%m%d%H%M%S)"
  cp -p "$hba" "$backup"
  {
    printf '%s\n' "$HBA_BEGIN"
    printf 'host    %s    %s    127.0.0.1/32    scram-sha-256\n' "$DB_NAME" "$DB_ROLE"
    printf 'host    all     %s    all             reject\n' "$DB_ROLE"
    printf 'local   all     %s                    reject\n' "$DB_ROLE"
    printf '%s\n' "$HBA_END"
    cat "$backup"
  } > "$hba"
  errors="$(pg -c 'select count(*) from pg_hba_file_rules where error is not null')"
  if [ "$errors" != "0" ]; then
    cp -p "$backup" "$hba"
    die "pg_hba.conf doğrulanamadı; yedek geri kondu ($backup)."
  fi
  pg -c 'select pg_reload_conf()' >/dev/null
  say "pg_hba: okul bloğu en üste eklendi (yedek: $backup)"
}

step_database() {
  systemctl is-active --quiet postgresql || die "PostgreSQL çalışmıyor (systemctl status postgresql)."
  install -d -m 700 -o root -g root "$ETC_DIR"
  local role_exists password
  role_exists="$(pg -c "select count(*) from pg_roles where rolname = '$DB_ROLE'")"
  if [ "$role_exists" = "0" ] || [ ! -s "$ENV_FILE" ]; then
    password="$(openssl rand -hex 24)"
    if [ "$role_exists" = "0" ]; then
      printf "${NO_LOG_SQL}CREATE ROLE %s LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD '%s';\n" \
        "$DB_ROLE" "$password" | pg -q >/dev/null
      say "veritabanı kullanıcısı $DB_ROLE oluşturuldu (yönetici değil, veritabanı/rol açamaz)"
    else
      printf "${NO_LOG_SQL}ALTER ROLE %s PASSWORD '%s';\n" "$DB_ROLE" "$password" | pg -q >/dev/null
      say "şifre dosyası yoktu: $DB_ROLE için yeni şifre üretildi"
    fi
    write_env_file "$password"
    unset password
  else
    say "veritabanı kullanıcısı $DB_ROLE ve şifre dosyası zaten var (dokunulmadı)"
  fi
  # Yetkiler her çalıştırmada aynı duruma getirilir
  pg -c "ALTER ROLE $DB_ROLE NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS" >/dev/null
  if [ "$(pg -c "select count(*) from pg_database where datname = '$DB_NAME'")" = "0" ]; then
    pg -c "CREATE DATABASE $DB_NAME OWNER $DB_ROLE" >/dev/null
    say "veritabanı $DB_NAME oluşturuldu (sahibi $DB_ROLE)"
  else
    say "veritabanı $DB_NAME zaten var"
  fi
  pg -c "REVOKE CONNECT ON DATABASE $DB_NAME FROM PUBLIC" >/dev/null
  pg -c "GRANT CONNECT ON DATABASE $DB_NAME TO $DB_ROLE" >/dev/null
  ensure_hba_block
  ensure_jwt_key
  ensure_twofactor_key
  say "şifre dosyası: $ENV_FILE ($(stat -c '%U:%G %a' "$ENV_FILE"))"
}

step_service() {
  sed -e "s/@API_PORT@/$API_PORT/g" -e "s/@WEB_PORT@/$WEB_PORT/g" -e "s/@PI_ADDRESS@/$PI_ADDRESS/g" \
    "$HERE/okul-api.service" > /etc/systemd/system/okul-api.service
  systemctl daemon-reload
  systemctl enable okul-api >/dev/null 2>&1
  if [ -e /opt/okul/api/current ]; then
    systemctl restart okul-api
    say "servis okul-api: yeniden başlatıldı"
  else
    say "servis okul-api: açılışta başlayacak şekilde kuruldu (ilk dağıtımda çalışır)"
  fi
}

case "$STEP" in
  disk) step_disk ;;
  nginx) step_disk; step_nginx ;;
  system) step_system ;;
  database) step_database ;;
  service) step_service ;;
  all) step_disk; step_system; step_nginx; step_database; step_service ;;
  *) die "bilinmeyen adım: $STEP" ;;
esac
