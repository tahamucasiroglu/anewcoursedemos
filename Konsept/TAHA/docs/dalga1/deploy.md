The Pi deployment is live: both hosts run as systemd services on the Pi, and checks from this PC pass. `curl http://192.168.1.102:7111/api/health` returns `{"status":"ok","version":"0.1.0","environment":"Pi","database":"ready"}`, so the API reaches PostgreSQL. `http://192.168.1.102:7110/` returns 302 to `/Account/Login`, which then loads with 200.

I could not deploy straight from the working tree. Another agent's in-progress `Shared.Contracts` change does not compile (CS0246, `ImagePromptItem` missing in `ImagesStagePayload.cs`). So I added a `-Source Head` option that publishes the last commit, and deployed HEAD `e392960`. That commit contains the F0 hosts only, with no modules yet.

**What I built**
- **`deploy/publish-pi.ps1`**: reads `VERSION`, publishes both hosts with `-c Release -r linux-arm64 --self-contained`, builds a tar.gz (~93 MB), copies it with scp, runs the install script on the Pi, then checks the two URLs from this PC.
  - Options: `-Source WorkingTree|Head`, `-SkipPublish`, `-SkipVerify`, `-SshHost tmpi`, `-PiAddress 192.168.1.102`.
  - It builds into `deploy/.out/build` (`--artifacts-path`), so other agents' `bin/obj` folders are not touched. `deploy/.out/` is gitignored.
- **`deploy/pi/install-release.sh`**:
  - creates `/opt/tahakonsept/releases/<version>-<timestamp>` (owned by root, read-only);
  - installs the unit files and runs `daemon-reload` when they changed;
  - creates the data folders under `/var/lib/tahakonsept`;
  - switches the `current` symlink atomically and stores the old target as `previous`;
  - restarts both services and waits up to 90 s for health. If the check fails it switches back to the previous release by itself.
  - It keeps the last 5 releases and never deletes `current` or `previous`.
- **`deploy/pi/rollback.sh`** (also installed as `/opt/tahakonsept/bin/rollback.sh`): `--list`, no argument to go back to `previous`, or a release name. It restarts the services and runs the health check.
- **Unit files** `deploy/pi/tahakonsept-api.service` and `tahakonsept-web.service`: same hardening as `tmautoclip-canli-api.service`, under our own names.
  - Settings: `User=tahakonsept`, `ASPNETCORE_ENVIRONMENT=Pi`, `EnvironmentFile=-/var/lib/tahakonsept/tahakonsept.env`, `HOME=/var/lib/tahakonsept`, `TAHAKONSEPT_DATA`, `TAHAKONSEPT_LOG_DIR`, `ReadWritePaths=/var/lib/tahakonsept`.
  - `systemd-analyze security` rates the API unit 2.9 OK. Both services are enabled.
- **`appsettings.Pi.json`** (new files in both hosts):
  - API: `ConnectionStrings:Main Host=localhost;Port=5432;Database=tahakonsept;Username=tahakonsept`, `App:DataDirectory=/var/lib/tahakonsept/App_Data`, `Logging:Directory=/var/lib/tahakonsept/logs`.
  - Web: `Api:BaseUrl=http://127.0.0.1:7111`, `DataProtection:KeysDirectory=/var/lib/tahakonsept/App_Data/keys/web-dataprotection`, same log folder.
- **No `Paths:AppData` key added.** BuildingBlocks already lets you set the App_Data path: `AppDataLocation.Resolve` reads `App:DataDirectory`, then the `TAHAKONSEPT_DATA` env variable, then `<contentRoot>/App_Data`. So I left BuildingBlocks unchanged.
- **`scripts/dev-tunnel.ps1`**: `ssh -N -L 5433:localhost:5432 tmpi`. `-Background` runs it in the background, and it does nothing if port 5433 is already open (tested).
- **`scripts/dev-run.ps1`**: opens the tunnel, then starts the API and Web with `dotnet run` in separate windows. `-NoTunnel` skips the tunnel and `-Wait` stops both on Ctrl+C. I did not run it, to avoid taking ports 7110/7111 while other agents work.
- **`deploy/.gitattributes`** forces LF line endings on `.sh` and `.service` files, and the publish script also converts them when packaging.
- **`docs/DEPLOY.md`**: how to deploy and roll back, the Pi file layout, logs (`journalctl -u tahakonsept-api`), how to rewrite the env file, and a log of the releases installed.

**On the Pi**
- `/var/lib/tahakonsept/tahakonsept.env` is written with `TAHAKONSEPT_DB_PASSWORD` (chmod 600, owner `tahakonsept`). I confirmed the password logs in to PostgreSQL.
- Two releases were installed: `0.1.0-20261114-054614` and `0.1.0-20261114-054659`. Rolling back to 054614 and forward again both passed health. The active release is **054659**.

**Commits**
- `e392960`: intermediate commit (scripts, units, `appsettings.Pi.json`, dev scripts).
- `af1c3ab`: final commit (`-Source Head`, quieter curl output during health checks, `docs/DEPLOY.md`).

**Unfinished / for the integrator**
- The Pi has F0 only. Run `deploy\publish-pi.ps1 -Source Head` again once the modules are committed; it also installs the newer `rollback.sh` (the Pi still has the version before the curl change).
- Until the Identity module exists, the Web's call to `api/identity/setup/status` returns 404. That is expected.
- `App_Data/keys/master.key` is created the first time it is used. If it is lost, the encrypted API keys cannot be read, so it needs a backup.