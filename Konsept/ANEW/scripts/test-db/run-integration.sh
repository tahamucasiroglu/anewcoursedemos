#!/bin/sh
# Entegrasyon testlerini geçici bir PostgreSQL 17 cluster'ına karşı çalıştırır (plan 0001, R-1).
#   sh scripts/test-db/run-integration.sh          → cluster'ı başlat, testleri çalıştır, her durumda durdur ve sil
#   sh scripts/test-db/run-integration.sh --check  → yalnızca kurulu mu (check.conf "requires integration")
# Kaynak hijyeni (docs/testing.md): cluster rastgele portta, geçici klasörde açılır; hata ya da kesinti dahil
# çıkışta durdurulur ve klasörü silinir. Her test sınıfı ayrıca kendi geçici veritabanını açıp siler.
set -u
. "$(dirname "$0")/env.sh"
ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)

if [ "${1:-}" = "--check" ]; then
  if { [ -x "$PG_BIN/pg_ctl.exe" ] || [ -x "$PG_BIN/pg_ctl" ]; } && [ -d "$PG_HOME/share/timezonesets" ]; then exit 0; fi
  echo "Taşınabilir PostgreSQL yok: sh scripts/test-db/install.sh" >&2
  exit 1
fi

data=$(mktemp -d "${TMPDIR:-/tmp}/anewconcept-pg.XXXXXX")
started=0
cleanup() {
  if [ "$started" -eq 1 ]; then
    "$PG_BIN/pg_ctl" -D "$data/db" -m immediate -w stop >/dev/null 2>&1 || true
  fi
  rm -rf "$data"
}
trap cleanup EXIT INT TERM HUP

"$PG_BIN/initdb" -D "$data/db" -U postgres -A trust -E UTF8 --no-locale >"$data/initdb.log" 2>&1 || {
  cat "$data/initdb.log"; exit 1; }

port=0
for attempt in 1 2 3 4 5; do
  candidate=$(( 49152 + $(od -An -N2 -tu2 /dev/urandom | tr -d ' ') % 10000 ))
  if "$PG_BIN/pg_ctl" -D "$data/db" -l "$data/server.log" -w -t 30 \
      -o "-p $candidate -c listen_addresses=127.0.0.1 -c fsync=off -c synchronous_commit=off -c full_page_writes=off" \
      start >/dev/null 2>&1; then
    port=$candidate
    started=1
    break
  fi
done
if [ "$started" -ne 1 ]; then
  echo "test-db: PostgreSQL başlatılamadı" >&2
  cat "$data/server.log" >&2 2>/dev/null
  exit 1
fi

echo "test-db: geçici PostgreSQL 127.0.0.1:$port"
KONSEPT_TEST_PG="Host=127.0.0.1;Port=$port;Username=postgres;Database=postgres;Pooling=false"
export KONSEPT_TEST_PG
cd "$ROOT" && MSBUILDDISABLENODEREUSE=1 dotnet test Konsept.sln -c Release --no-build --filter "Category=Integration"
