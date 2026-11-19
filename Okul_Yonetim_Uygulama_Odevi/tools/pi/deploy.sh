#!/usr/bin/env bash
# Raspberry Pi'ye dağıtım (spec 0003, AC-6/7/8): derle → Pi'ye kopyala → etkinleştir → sağlık kontrolü;
# kontrol başarısızsa Pi önceki sürüme kendisi döner ve komut hata verir.
# Kullanım: bash tools/pi/deploy.sh [all|web|api]   (varsayılan: all)
#   Web ve API ayrı birimlerdir: "web" API'yi yeniden başlatmaz, "api" web dosyalarına dokunmaz.
#   OKUL_SOURCE_DIR=<klasör> verilirse derleme o klasörden yapılır (VERIFY'da kasıtlı bozuk sürüm denemesi için).
. "$(dirname "$0")/lib.sh"

TARGET="${1:-all}"
case "$TARGET" in
  all) UNITS="web,api" ;;
  web) UNITS="web" ;;
  api) UNITS="api" ;;
  *) die "bilinmeyen hedef: $TARGET (all|web|api)" ;;
esac
SOURCE="${OKUL_SOURCE_DIR:-$REPO_DIR}"
SHA="$(git -C "$SOURCE" rev-parse --short HEAD 2>/dev/null || echo yerel)"
VERSION="$(date -u +%Y%m%d%H%M%S)-$SHA"
OUT="$(mktemp -d)"
LOCKED=0
STAGE=""

cleanup() {
  rm -rf "$OUT"
  # Etkinleştirme başlamadan yarıda kalındıysa Pi'deki hazırlık klasörü SSH kullanıcısıyla (root değil) silinir
  # (review round 2, I-2); etkinleştirme başladıysa onu activate-remote.sh siler.
  if [ "$LOCKED" = 1 ]; then
    if [ -n "$STAGE" ]; then ssh -o BatchMode=yes "$PI_SSH" "rm -rf '$STAGE'" >/dev/null 2>&1 || true; fi
    run_remote activate-remote.sh release >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

check_ssh
say "Dağıtım: $UNITS · sürüm $VERSION · kaynak $SOURCE"
run_remote activate-remote.sh preflight "$MIN_FREE_MB"
LOCKED=1

if [[ ",$UNITS," == *",api,"* ]]; then
  say "API derleniyor (linux-arm64, .NET gömülü)…"
  DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet publish \
    "$SOURCE/backend/src/WebAPI/WebAPI.csproj" -c Release -r linux-arm64 --self-contained true \
    -p:OpenApiGenerateDocuments=false -p:UseSharedCompilation=false -nodeReuse:false --disable-build-servers \
    -v:q -nologo -o "$OUT/api" >/dev/null || die "API derlenemedi."
  # Bu sürümün migration listesi: geri alırken "şemadan yeni sürüme dönülüyor" uyarısı için (Ö-2)
  (cd "$SOURCE/backend/src/Persistence/Migrations" && ls -1 ./*.cs 2>/dev/null | sed 's#^\./##; s#\.cs$##' \
    | grep -v -e '\.Designer$' -e 'ModelSnapshot$' || true) > "$OUT/api/okul-migrations.txt"
fi
if [[ ",$UNITS," == *",web,"* ]]; then
  say "Web derleniyor (API adresi: sayfayla aynı adres)…"
  # Git Bash (MSYS), Windows programına giden ortam değişkenindeki "/" değerini yol sanıp "C:/Program Files/Git/"
  # yapar; çeviri bu değişken için kapatılır (VERIFY V-1, docs/conventions.md "Known traps").
  (cd "$SOURCE/web" && MSYS2_ENV_CONV_EXCL=VITE_API_BASE_URL MSYS_NO_PATHCONV=1 VITE_API_BASE_URL=/ \
    npx vite build --outDir "$OUT/web" --emptyOutDir >/dev/null) || die "Web derlenemedi."
  # Koruma: pakette yerel bir yol (C:/…, file:) varsa sayfa API'ye ulaşamaz → dağıtım durur, Pi'ye hiçbir şey gitmez
  if grep -rqE '[A-Za-z]:/(Program Files|Users|msys|Windows)|file:///' "$OUT/web"; then
    die "Web paketinde yerel bir yol var (ör. C:/Program Files/Git/); API adresi bozuk derlendi. Dağıtım durduruldu."
  fi
fi

say "Pi'ye kopyalanıyor…"
# Hazırlık klasörü Pi'de rastgele adla açılır (tahmin edilebilir /tmp yolu yok; review round 1, M-1)
# Değer önce doğrulanır, ancak sonra STAGE'e (temizlikte silinecek yol) atanır (review round 3, L-2)
stage_candidate="$(ssh -o BatchMode=yes "$PI_SSH" 'mktemp -d /tmp/okul-deploy.XXXXXXXXXX')"
[[ "$stage_candidate" =~ ^/tmp/okul-deploy\.[A-Za-z0-9]{10}$ ]] || die "Pi'de hazırlık klasörü açılamadı."
STAGE="$stage_candidate"
(cd "$OUT" && tar -cf - ${UNITS//,/ }) | ssh -o BatchMode=yes "$PI_SSH" "tar -C '$STAGE' -xf -"

say "Etkinleştiriliyor ve kontrol ediliyor…"
LOCKED=0
run_remote activate-remote.sh activate "$UNITS" "$VERSION" "$WEB_PORT" "$KEEP_RELEASES" "$STAGE"
say "Hazır: http://$PI_ADDRESS:$WEB_PORT  (sürüm $VERSION)"
