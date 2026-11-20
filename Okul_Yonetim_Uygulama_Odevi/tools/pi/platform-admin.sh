#!/usr/bin/env bash
# Pi'de platform yöneticisi komutları (spec 0007): yetki ver / al, iki adımlı doğrulamayı sıfırla. Gizli bilgi sorulmaz.
# Komut Pi'de API servisinin kullanıcısıyla (okul) ve servisin env dosyalarıyla çalışır; kişinin açık oturumları kapanır.
# Pi'de önce setup.sh (iki adımlı doğrulama anahtarı dahil) ve deploy.sh çalışmış olmalı.
# Kullanım: bash tools/pi/platform-admin.sh grant-platform-admin|revoke-platform-admin|reset-two-factor --eposta kisi@ornek.com
. "$(dirname "$0")/lib.sh"

case "${1:-}" in
  grant-platform-admin|revoke-platform-admin|reset-two-factor) ;;
  *) die "Kullanım: bash tools/pi/platform-admin.sh grant-platform-admin|revoke-platform-admin|reset-two-factor --eposta e-posta" ;;
esac
[ "$#" -eq 3 ] && [ "$2" = "--eposta" ] || die "Kullanım: bash tools/pi/platform-admin.sh $1 --eposta e-posta"
check_ssh

# Her argüman uzak kabuk için ayrı ayrı kaçışlanır.
args="$(printf ' %q' "$@")"
ssh -o BatchMode=yes "$PI_SSH" \
  "sudo systemd-run --wait --pipe --quiet --uid=okul --gid=okul -p EnvironmentFile=/etc/okul/okul-api.env -p EnvironmentFile=/etc/okul/okul-jwt.env \
-p EnvironmentFile=-/etc/okul/okul-2fa.env -p WorkingDirectory=/opt/okul/api/current -E ASPNETCORE_ENVIRONMENT=Production \
-E FileLogging__Folder=/var/log/okul -E DOTNET_NOLOGO=1 /opt/okul/api/current/OkulYonetim.WebAPI$args"
