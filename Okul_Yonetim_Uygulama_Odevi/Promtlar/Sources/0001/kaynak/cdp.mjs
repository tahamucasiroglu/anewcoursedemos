// Başsız Chrome'u DevTools protokolüyle sürer: her durum için modu ayarla, sayfayı aç, bekle, görüntü al.
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const [chrome, outDir, profile, webDir, mobileDir] = process.argv.slice(2);
const port = 9333;
fs.mkdirSync(outDir, { recursive: true });
const stub = spawn(process.execPath, [path.join(import.meta.dirname, "stub.mjs"), webDir, mobileDir], { stdio: "ignore" });
for (let i = 0; i < 50; i++) {
  try { await fetch("http://localhost:5090/__mode/healthy"); break; } catch { await new Promise((r) => setTimeout(r, 200)); }
}
const proc = spawn(chrome, [
  "--headless=new", "--disable-gpu", "--no-first-run", "--no-default-browser-check", "--hide-scrollbars",
  `--user-data-dir=${profile}`, `--remote-debugging-port=${port}`, "about:blank",
], { stdio: "ignore" });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let target;
for (let i = 0; i < 100 && !target; i++) {
  try {
    const list = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
    target = list.find((t) => t.type === "page");
  } catch { /* chrome henüz hazır değil */ }
  if (!target) await sleep(200);
}
if (!target) { proc.kill(); throw new Error("Chrome açılmadı"); }

const ws = new WebSocket(target.webSocketDebuggerUrl);
await new Promise((r) => ws.addEventListener("open", r, { once: true }));
let id = 0;
const pending = new Map();
ws.addEventListener("message", (e) => {
  const msg = JSON.parse(e.data);
  if (msg.id && pending.has(msg.id)) { pending.get(msg.id)(msg.result); pending.delete(msg.id); }
  if (process.env.DEBUG && msg.method && /console|Exception|loadingFailed|requestWillBeSent|responseReceived/.test(msg.method)) console.log(msg.method, JSON.stringify(msg.params).slice(0, 300));
});
const send = (method, params = {}) => new Promise((r) => { id++; pending.set(id, r); ws.send(JSON.stringify({ id, method, params })); });

const states = process.env.STATES ? process.env.STATES.split(",") : ["loading", "healthy", "unavailable", "unreachable"];
const apps = [
  { name: "web", url: "http://localhost:4173/", width: 1280, height: 720, mobile: false },
  { name: "mobile", url: "http://localhost:4174/", width: 414, height: 800, mobile: true },
];
try {
  await send("Page.enable");
  await send("Runtime.enable");
  await send("Network.enable");
  // "loading" en sona: asılı istek Chrome'un aynı adrese sonraki isteklerini bekletir.
  const order = ["loading", "healthy", "unavailable", "unreachable"];
  const run = [...states.filter((x) => x !== "loading"), ...states.filter((x) => x === "loading")];
  for (const state of run) {
    const i = order.indexOf(state);
    await fetch(`http://localhost:5090/__mode/${state}`);
    for (const app of apps) {
      await send("Emulation.setDeviceMetricsOverride", { width: app.width, height: app.height, deviceScaleFactor: 1, mobile: app.mobile });
      await send("Page.navigate", { url: "about:blank" });
      await sleep(300);
      await send("Page.navigate", { url: app.url });
      await sleep(2500);
      const shot = await send("Page.captureScreenshot", { format: "png" });
      const file = path.join(outDir, `S-${i + 1}-${app.name}-${state}.png`);
      fs.writeFileSync(file, Buffer.from(shot.data, "base64"));
      console.log("ok", path.basename(file));
    }
  }
} finally {
  ws.close();
  proc.kill();
  stub.kill();
  await sleep(1500);
  fs.rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 300 });
}
