#!/bin/sh
# check.conf adımı: bilinen güvenlik açığı olan NuGet paketi (dolaylı dahil) varsa kırmızı (docs/security.md, Dependencies).
set -u
out=$(mktemp)
trap 'rm -f "$out"' EXIT
cd "$(dirname -- "$0")/../../backend" || exit 2
DOTNET_CLI_UI_LANGUAGE=en DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  dotnet list OkulYonetim.sln package --vulnerable --include-transitive >"$out" 2>&1
rc=$?
if [ "$rc" -ne 0 ]; then
  cat "$out"
  exit "$rc"
fi
if grep -q "has the following vulnerable packages" "$out"; then
  cat "$out"
  echo "dotnet-vuln: açığı olan paket var" >&2
  exit 1
fi
echo "dotnet-vuln: bilinen açığı olan paket yok"
