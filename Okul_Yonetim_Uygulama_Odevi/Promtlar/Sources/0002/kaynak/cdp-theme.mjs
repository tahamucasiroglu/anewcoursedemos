// Spec 0002 VERIFY ekran görüntüleri: başsız Chrome + DevTools protokolü + sahte API (stub.mjs, 0001'den).
// Mod, kayıtlı seçimle (localStorage "okul.theme") ya da cihaz tercihi taklidiyle (prefers-color-scheme) verilir.
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";

const [chrome, outDir, profile, webDir, mobileDir, stubPath] = process.argv.slice(2);
const port = 9334;
fs.mkdirSync(outDir, { recursive: true });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

const stub = spawn(process.execPath, [stubPath, webDir, mobileDir], { stdio: "ignore" });
for (let i = 0; i < 50; i++) {
  try { await fetch("http://localhost:5090/__mode/healthy"); break; } catch { await sleep(200); }
}
const proc = spawn(chrome, [
  "--headless=new", "--disable-gpu", "--no-first-run", "--no-default-browser-check", "--hide-scrollbars",
  `--user-data-dir=${profile}`, `--remote-debugging-port=${port}`, "about:blank",
], { stdio: "ignore" });

let target;
for (let i = 0; i < 100 && !target; i++) {
  try {
    target = (await (await fetch(`http://127.0.0.1:${port}/json/list`)).json()).find((t) => t.type === "page");
  } catch { /* henüz hazır değil */ }
  if (!target) await sleep(200);
}
if (!target) { proc.kill(); stub.kill(); throw new Error("Chrome açılmadı"); }

const ws = new WebSocket(target.webSocketDebuggerUrl);
await new Promise((r) => ws.addEventListener("open", r, { once: true }));
let id = 0;
const pending = new Map();
ws.addEventListener("message", (e) => {
  const msg = JSON.parse(e.data);
  if (msg.id && pending.has(msg.id)) { pending.get(msg.id)(msg.result); pending.delete(msg.id); }
});
const send = (method, params = {}) => new Promise((r) => { id++; pending.set(id, r); ws.send(JSON.stringify({ id, method, params })); });

const apps = {
  web: { url: "http://localhost:4173/", width: 1280, height: 720, mobile: false },
  mobile: { url: "http://localhost:4174/", width: 414, height: 800, mobile: true },
};

async function open(appName, { stored, device = "light", width, state = "healthy" }) {
  const app = apps[appName];
  await fetch(`http://localhost:5090/__mode/${state}`);
  await send("Emulation.setDeviceMetricsOverride", { width: width ?? app.width, height: app.height, deviceScaleFactor: 1, mobile: app.mobile });
  await send("Emulation.setEmulatedMedia", { features: [{ name: "prefers-color-scheme", value: device }] });
  await send("Page.navigate", { url: app.url });
  await sleep(600);
  await send("Runtime.evaluate", {
    expression: stored ? `localStorage.setItem("okul.theme", ${JSON.stringify(stored)})` : `localStorage.removeItem("okul.theme")`,
  });
  await send("Page.navigate", { url: "about:blank" });
  await sleep(200);
  await send("Page.navigate", { url: app.url });
  await sleep(2200);
}

async function shot(name) {
  const r = await send("Page.captureScreenshot", { format: "png" });
  fs.writeFileSync(path.join(outDir, `${name}.png`), Buffer.from(r.data, "base64"));
  console.log("ok", name);
}

async function tab(times) {
  for (let i = 0; i < times; i++) {
    for (const type of ["keyDown", "keyUp"]) {
      await send("Input.dispatchKeyEvent", { type, key: "Tab", code: "Tab", windowsVirtualKeyCode: 9 });
    }
  }
  await sleep(300);
}

const order = ["healthy", "unavailable", "unreachable", "loading"]; // "loading" en sona (asılı istek, 0001'deki ders)
const number = { loading: 1, healthy: 2, unavailable: 3, unreachable: 4 };
try {
  await send("Page.enable");
  await send("Runtime.enable");
  for (const state of order.filter((s) => s !== "loading")) {
    for (const mode of ["light", "dark"]) {
      for (const appName of ["web", "mobile"]) {
        await open(appName, { stored: mode, state });
        await shot(`S-${number[state]}-${appName}-${mode}-${state}`);
      }
    }
  }
  // Sistem: kayıt yok, cihaz koyu → koyu (AC-2)
  for (const appName of ["web", "mobile"]) {
    await open(appName, { stored: null, device: "dark" });
    await shot(`S-5-${appName}-sistem-cihaz-koyu`);
  }
  // 320 px (AC-3): seçici alt satıra iner
  for (const mode of ["light", "dark"]) {
    await open("web", { stored: mode, width: 320 });
    await shot(`S-6-web-320px-${mode}`);
    await open("mobile", { stored: mode, width: 320 });
    await shot(`S-6-mobile-320px-${mode}`);
  }
  // Klavye odağı (AC-11): Tab ile Sistem, Açık, Koyu
  for (const mode of ["light", "dark"]) {
    for (const [n, label] of [[1, "sistem"], [2, "acik"], [3, "koyu"]]) {
      await open("web", { stored: mode });
      await tab(n);
      await shot(`S-7-web-odak-${label}-${mode}`);
    }
  }
  for (const mode of ["light", "dark"]) {
    for (const appName of ["web", "mobile"]) {
      await open(appName, { stored: mode, state: "loading" });
      await shot(`S-1-${appName}-${mode}-loading`);
    }
  }
} finally {
  ws.close();
  proc.kill();
  stub.kill();
  await sleep(1500);
  fs.rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 300 });
}
