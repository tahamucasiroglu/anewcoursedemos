#!/bin/sh
# Taşınabilir PostgreSQL 17'yi bir kez kurar (yönetici izni gerekmez). Kullanım: sh scripts/test-db/install.sh
# Zip repo dışına indirilir, yalnızca sunucuyu çalıştırmak için gereken klasörler açılır.
set -eu
. "$(dirname "$0")/env.sh"

# Tam kurulumun işareti: bin + share/timezonesets (initdb ikisini de ister).
if { [ -x "$PG_BIN/pg_ctl.exe" ] || [ -x "$PG_BIN/pg_ctl" ]; } && [ -d "$PG_HOME/share/timezonesets" ]; then
  echo "test-db: zaten kurulu: $PG_HOME"
  exit 0
fi

mkdir -p "$DEV_HOME/download"
zip="$DEV_HOME/download/pg17.zip"
if [ ! -s "$zip" ]; then
  echo "test-db: indiriliyor: $PG_URL"
  curl -fL -o "$zip.part" "$PG_URL"
  mv "$zip.part" "$zip"
fi

echo "test-db: açılıyor: $PG_HOME"
rm -rf "$DEV_HOME/extract"
mkdir -p "$DEV_HOME/extract"
unzip -q "$zip" -x 'pgsql/pgAdmin/*' 'pgsql/StackBuilder/*' 'pgsql/doc/*' 'pgsql/include/*' -d "$DEV_HOME/extract"
rm -rf "$PG_HOME"
mv "$DEV_HOME/extract/pgsql" "$PG_HOME"
rm -rf "$DEV_HOME/extract"
"$PG_BIN/pg_ctl" --version
