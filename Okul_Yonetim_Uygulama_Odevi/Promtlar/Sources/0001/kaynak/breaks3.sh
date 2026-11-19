# VERIFY kasıtlı bozmaları: repo DIŞINDA bir kopyada, her bozma kendi temiz kopyasında.
set -u
SRC="C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Okul_Yonetim_Uygulama_Odevi"
W="C:/Users/taham/AppData/Local/Temp/claude/09d252a19f5bfcd1/2fc78961-704e-4a2f-9b3f-d7c2e5ec5024/scratchpad/verify"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_CLI_UI_LANGUAGE=en

fresh() {
  rm -rf "$W"; mkdir -p "$W/packages/api-client"
  cp "$SRC/packages/api-client/openapi.json" "$W/packages/api-client/"
  (cd "$SRC" && tar --exclude='bin' --exclude='obj' -cf - backend) | (cd "$W" && tar -xf -)
}
build() { (cd "$W/backend" && dotnet build OkulYonetim.sln -c Release -nodeReuse:false --disable-build-servers -v:q -nologo 2>&1 | python -I "$W/../errcodes.py"); }
archtest() { (cd "$W/backend" && dotnet test --project tests/Architecture/Architecture.csproj -c Release --no-build 2>&1 | grep -E "^failed |  total:|  failed:" | head -6); }

H="$W/backend/src/Application/Features/SystemHealth/Queries/GetSystemHealth/GetSystemHealthQueryHandler.cs"
for v in 30 2; do
  echo "=== B10 (AC-2 sınır) zaman aşımı 3 sn -> $v sn ==="
  fresh; sed -i "s/DatabaseTimeoutSeconds = 3;/DatabaseTimeoutSeconds = $v;/" "$H"; grep -c "DatabaseTimeoutSeconds = $v;" "$H"
  build
  (cd "$W/backend" && timeout 120 dotnet test --project tests/Unit/Unit.csproj -c Release --no-build 2>&1 | grep -E "^failed |  total:|  failed:" | head -6)
done
rm -rf "$W"
