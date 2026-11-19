// VERIFY ekran görüntüleri için sahte API + statik sunucu (repo dışında).
// API :5090 — mod /__mode/<healthy|unavailable|loading|unreachable> ile değişir.
// Web dist :4173, Expo web çıktısı :4174.
import http from "node:http";
import fs from "node:fs";
import path from "node:path";

const [webDir, mobileDir] = process.argv.slice(2);
let mode = "healthy";
const types = { ".html": "text/html; charset=utf-8", ".js": "text/javascript", ".css": "text/css", ".svg": "image/svg+xml", ".png": "image/png", ".ico": "image/x-icon", ".json": "application/json", ".ttf": "font/ttf" };

http.createServer((req, res) => {
  const cors = { "Access-Control-Allow-Origin": req.headers.origin ?? "*", "Content-Type": "application/json" };
  if (req.url.startsWith("/__mode/")) {
    mode = req.url.slice(8);
    res.end(mode);
    return;
  }
  if (req.method === "OPTIONS") {
    res.writeHead(204, { ...cors, "Access-Control-Allow-Headers": "*" });
    res.end();
    return;
  }
  if (mode === "loading") return; // hiç yanıt verme
  if (mode === "unreachable") { req.socket.destroy(); return; }
  if (mode === "unavailable") {
    res.writeHead(503, cors);
    res.end(JSON.stringify({ status: "unavailable", message: "Sistem şu anda hizmet dışı" }));
    return;
  }
  res.writeHead(200, cors);
  res.end(JSON.stringify({ status: "healthy", message: "Sistem çalışıyor" }));
}).listen(5090);

function statics(dir, port) {
  http.createServer((req, res) => {
    let p = path.join(dir, decodeURIComponent(req.url.split("?")[0]));
    if (!fs.existsSync(p) || fs.statSync(p).isDirectory()) p = path.join(dir, "index.html");
    res.writeHead(200, { "Content-Type": types[path.extname(p)] ?? "application/octet-stream" });
    fs.createReadStream(p).pipe(res);
  }).listen(port);
}
statics(webDir, 4173);
statics(mobileDir, 4174);
console.log("stub hazır");
