// check.conf adımı: ortak istemcinin tipleri (src/schema.d.ts), backend derlemesinin ürettiği openapi.json ile
// güncel mi? Backend değişip istemci yeniden üretilmezse kırmızı (spec 0001 AC-5). Düzeltme: npm run api-client:generate
import { execFileSync } from "node:child_process";
import { mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const pkg = join(import.meta.dirname, "..", "..", "packages", "api-client");
const dir = mkdtempSync(join(tmpdir(), "okul-api-client-"));
try {
  const fresh = join(dir, "schema.d.ts");
  const cli = join(import.meta.dirname, "..", "..", "node_modules", "openapi-typescript", "bin", "cli.js");
  execFileSync(process.execPath, [cli, join(pkg, "openapi.json"), "-o", fresh], { stdio: "pipe" });
  const normalize = (text) => text.replace(/\r\n/g, "\n");
  if (normalize(readFileSync(fresh, "utf8")) !== normalize(readFileSync(join(pkg, "src", "schema.d.ts"), "utf8"))) {
    console.error("api-client güncel değil: backend'in API tanımı değişmiş. Çalıştırın: npm run api-client:generate");
    process.exit(1);
  }
  console.log("api-client: openapi.json ile güncel");
} finally {
  rmSync(dir, { recursive: true, force: true });
}
