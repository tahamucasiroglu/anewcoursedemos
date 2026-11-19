# Pi'de root olarak çalışır; salt okuma. Şifre dosyasının yalnızca özeti (sha256) ve izni yazılır, içeriği değil.
set -euo pipefail
echo "== env dosyası (izin + özet)"
stat -c '%U:%G %a' /etc/okul/okul-api.env
sha256sum /etc/okul/okul-api.env | cut -c1-16
echo "== dinlenen portlar"
ss -Hltn | awk '{print $4}' | sort -u
echo "== servisler (running)"
systemctl list-units --type=service --state=running --no-legend --plain | awk '{print $1}' | sort
echo "== okul-api"
systemctl show okul-api -p MainPID -p ActiveState -p UnitFileState -p NRestarts -p User
echo "== sürümler"
for u in web api; do echo "$u current -> $(readlink /opt/okul/$u/current)"; ls -1 /opt/okul/$u/releases; done
echo "== veritabanları"
sudo -u postgres psql -tAc "select datname from pg_database order by 1"
echo "== pg_hba okul dışı özet"
hba="$(sudo -u postgres psql -tAc 'show hba_file')"
awk '/^# okul-begin/{s=1} !s{print} /^# okul-end/{s=0}' "$hba" | sha256sum | cut -c1-16
