#!/bin/sh
# check.conf adımı: backend testleri, seçiciyle (trait) ikiye ayrılır (docs/testing.md, Two speeds kural 4).
#   fast → Category=Integration OLMAYAN tüm testler · full → yalnızca Category=Integration
# Bir test projesinde seçiciye uyan test yoksa test platformu 8 döner; bu yok sayılır, ama TOPLAM test sayısı
# sıfırsa adım kırmızıdır (seçici sessizce hiçbir şey çalıştırmasın).
# resource hygiene: build sunucusu yok, MSBuild node yeniden kullanımı yok (T6).
set -u
mode=${1:?usage: dotnet-test.sh fast|full}
case "$mode" in
  fast) filter="--filter-not-trait Category=Integration" ;;
  full) filter="--filter-trait Category=Integration" ;;
  *) echo "usage: dotnet-test.sh fast|full" >&2; exit 2 ;;
esac

out=$(mktemp)
trap 'rm -f "$out"' EXIT
cd "$(dirname -- "$0")/../../backend" || exit 2
DOTNET_CLI_UI_LANGUAGE=en DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  dotnet test --solution OkulYonetim.sln -c Release --no-build $filter --ignore-exit-code 8 >"$out" 2>&1
rc=$?
grep -E "^(failed|  total:|  failed:|  succeeded:|  skipped:)" "$out"
total=$(sed -n 's/^  total: *\([0-9][0-9]*\).*/\1/p' "$out" | tail -n 1)
if [ "$rc" -ne 0 ]; then
  cat "$out"
  exit "$rc"
fi
if [ -z "$total" ] || [ "$total" -eq 0 ]; then
  echo "dotnet-test ($mode): hiç test çalışmadı — seçiciyi kontrol edin" >&2
  exit 1
fi
echo "dotnet-test ($mode): $total test"
