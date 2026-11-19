# Pi tarafındaki betiklerin ortak parçası (spec 0003): mesaj, kilit, sağlık kontrolü, migration uyarısı.
# activate-remote.sh ve rollback-remote.sh tarafından "source" edilir; tek başına çalıştırılmaz.

LOCK=/run/lock/okul-deploy

say() { printf '  %s\n' "$*"; }
die() { printf 'HATA: %s\n' "$*" >&2; exit 1; }

# Dağıtım/geri alma kilidi: aynı anda ikincisi reddedilir (Ö-3). 30 dakikadan eski kilit yarıda kalmış bir işten
# kalmıştır ve temizlenir (review round 1, L-4 — dağıtım ve geri almada aynı kural).
acquire_lock() {
  if [ -d "$LOCK" ] && [ -n "$(find "$LOCK" -maxdepth 0 -mmin +30 2>/dev/null)" ]; then
    rmdir "$LOCK"
  fi
  mkdir "$LOCK" 2>/dev/null || die "Başka bir dağıtım/geri alma sürüyor (kilit: $LOCK). Bitmesini bekleyin."
}

release_lock() { rmdir "$LOCK" 2>/dev/null || true; }

# Web ana sayfası ve nginx üzerinden API sistem durumu "healthy" mi (30 sn'ye kadar)
healthy() {
  local web_port="$1" i
  for i in $(seq 1 30); do
    if curl -fsS --max-time 5 "http://127.0.0.1:$web_port/api/system-health" 2>/dev/null | grep -q '"status":"healthy"' \
      && curl -fsS --max-time 5 "http://127.0.0.1:$web_port/" 2>/dev/null | grep -q 'id="root"'; then
      return 0
    fi
    sleep 1
  done
  return 1
}

# Daha yeni API sürümünde olup geri dönülen sürümde olmayan migration varsa uyarır; veritabanı şeması geri alınmaz (Ö-2).
# $1 = yeni sürüm klasörü (releases/...), $2 = geri dönülen sürüm klasörü (releases/...)
warn_newer_migrations() {
  local newer
  newer="$(comm -23 <(sort "/opt/okul/api/$1/okul-migrations.txt" 2>/dev/null) \
    <(sort "/opt/okul/api/$2/okul-migrations.txt" 2>/dev/null) || true)"
  if [ -n "$newer" ]; then
    printf 'UYARI: geri dönülen API sürümünden sonra eklenmiş migration var; veritabanı şeması geri alınmaz:\n%s\n' "$newer"
  fi
}
