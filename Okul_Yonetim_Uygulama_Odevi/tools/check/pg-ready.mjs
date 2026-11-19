// check.conf önkoşulu: PostgreSQL portu açık mı? (PATH'teki psql/pg_isready'e bağımlı değil.)
// Adres: OKUL_DB_HOST (varsayılan localhost), OKUL_DB_PORT (varsayılan 5432). Çıkış 0 = ulaşılabilir.
import net from "node:net";

const host = process.env.OKUL_DB_HOST || "localhost";
const port = Number(process.env.OKUL_DB_PORT || 5432);

const socket = net.connect({ host, port });
socket.setTimeout(2000);
socket.once("connect", () => {
  socket.end();
  process.exit(0);
});
const fail = (reason) => {
  console.error(`PostgreSQL'e ulaşılamıyor (${host}:${port}): ${reason}`);
  socket.destroy();
  process.exit(1);
};
socket.once("timeout", () => fail("zaman aşımı"));
socket.once("error", (error) => fail(error.code || error.message));
