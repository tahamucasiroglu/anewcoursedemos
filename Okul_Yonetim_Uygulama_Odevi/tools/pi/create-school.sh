#!/usr/bin/env bash
# Pi'de okul açma (spec 0004): okulu, ilk şubesini ve kurucu hesabını oluşturur. Şifre argüman DEĞİLDİR: komut yeni
# kullanıcı için şifreyi ekranda göstermeden iki kez sorar (siz yazarsınız). E-posta zaten varsa şifre sorulmaz.
# Komut Pi'de API servisinin kullanıcısıyla (okul) ve servisin gizli env dosyalarıyla çalışır; veritabanını günceller.
# Pi'de önce setup.sh ve deploy.sh çalışmış olmalı.
# Kullanım: bash tools/pi/create-school.sh --okul "Çınar Koleji" --sube "Merkez" --ad "Ayşe Yılmaz" --eposta ayse@ornek.com
. "$(dirname "$0")/lib.sh"

[ "$#" -gt 0 ] || die "Kullanım: bash tools/pi/create-school.sh --okul \"Okul adı\" --sube \"Şube adı\" --ad \"Ad Soyad\" --eposta e-posta"
check_ssh

# Okul adında boşluk ve Türkçe harf olabilir: her argüman uzak kabuk için ayrı ayrı kaçışlanır.
args="$(printf ' %q' "$@")"
ssh -t -o BatchMode=yes "$PI_SSH" \
  "sudo systemd-run --pty --quiet --uid=okul --gid=okul -p EnvironmentFile=/etc/okul/okul-api.env -p EnvironmentFile=/etc/okul/okul-jwt.env \
-p WorkingDirectory=/opt/okul/api/current -E ASPNETCORE_ENVIRONMENT=Production -E FileLogging__Folder=/var/log/okul \
-E DOTNET_NOLOGO=1 /opt/okul/api/current/OkulYonetim.WebAPI create-school$args"
