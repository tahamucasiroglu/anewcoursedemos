set -uo pipefail
q() { sudo -u postgres psql -X -tA "$@"; }
echo "== rol özellikleri (super, createdb, createrole, replication, bypassrls)"
q -c "select rolname, rolsuper, rolcreatedb, rolcreaterole, rolreplication, rolbypassrls from pg_roles where rolname='okul_app'"
echo "== okul_app CONNECT yetkisi (veritabanı düzeyi)"
q -c "select datname, has_database_privilege('okul_app', datname, 'CONNECT') from pg_database where datname in ('okul','postgres','tmautoclip_canli') order by 1"
echo "== okul veritabanının sahibi"
q -c "select datname, pg_get_userbyid(datdba) from pg_database where datname='okul'"
echo "== migration tablosu"
q -d okul -c 'select migration_id from "__EFMigrationsHistory"'
echo "== kasıtlı bozma (benzetim): okul bloğu olmasaydı okul_app hangi kurala düşerdi?"
q -c "select line_number, type, database, user_name, address, auth_method from pg_hba_file_rules where type='host' and address='127.0.0.1' and ('all' = any(user_name)) and ('all' = any(database)) order by line_number limit 1"
