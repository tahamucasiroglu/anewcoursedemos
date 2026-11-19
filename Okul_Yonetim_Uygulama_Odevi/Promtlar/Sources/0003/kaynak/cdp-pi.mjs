// Spec 0003 VERIFY: Pi'deki sayfanın gerçek zamanlı başsız Chrome ile ekran görüntüsü + ağ/konsol kaydı.
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const [chrome, outDir, profile] = process.argv.slice(2);
const shots = [
  ["V-1-pi-ana-sayfa.png", "http://192.168.1.102:8080/"],
  ["V-1-pi-derin-baglanti.png", "http://192.168.1.102:8080/okul/derin/baglanti"],
];
const port = 9335;
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const proc = spawn(chrome, ["--headless=new", "--disable-gpu", "--no-first-run", "--no-default-browser-check",
  "--hide-scrollbars", `--user-data-dir=${profile}`, `--remote-debugging-port=${port}`, "about:blank"], { stdio: "ignore" });
let target;
for (let i = 0; i < 100 && !target; i++) {
  try { target = (await (await fetch(`http://127.0.0.1:${port}/json/list`)).json()).find((t) => t.type === "page"); } catch {}
  if (!target) await sleep(200);
}
if (!target) { proc.kill(); throw new Error("Chrome açılmadı"); }
const ws = new WebSocket(target.webSocketDebuggerUrl);
await new Promise((r) => ws.addEventListener("open", r, { once: true }));
let id = 0;
const pending = new Map();
ws.addEventListener("message", (e) => {
  const m = JSON.parse(e.data);
  if (m.id && pending.has(m.id)) { pending.get(m.id)(m.result ?? m.error); pending.delete(m.id); }
  if (m.method === "Network.responseReceived") console.log("  yanıt", m.params.response.status, m.params.response.url);
  if (m.method === "Network.loadingFailed") console.log("  BAŞARISIZ", m.params.errorText, m.params.requestId);
  if (m.method === "Network.requestWillBeSent") console.log("  istek", m.params.request.method, m.params.request.url);
  if (m.method === "Runtime.consoleAPICalled") console.log("  konsol", m.params.args.map((a) => a.value ?? a.description).join(" "));
  if (m.method === "Runtime.exceptionThrown") console.log("  istisna", m.params.exceptionDetails.text, m.params.exceptionDetails.exception?.description);
});
const send = (method, params = {}) => new Promise((r) => { id++; pending.set(id, r); ws.send(JSON.stringify({ id, method, params })); });
await send("Network.enable");
await send("Runtime.enable");
await send("Emulation.setDeviceMetricsOverride", { width: 1280, height: 800, deviceScaleFactor: 1, mobile: false });
for (const [file, url] of shots) {
  console.log(file, url);
  await send("Page.navigate", { url });
  await sleep(4000);
  const text = await send("Runtime.evaluate", { expression: "document.body.innerText", returnByValue: true });
  console.log("  metin:", JSON.stringify(text.result?.value));
  const shot = await send("Page.captureScreenshot", { format: "png" });
  fs.writeFileSync(path.join(outDir, file), Buffer.from(shot.data, "base64"));
}
ws.close();
proc.kill();
