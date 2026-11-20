// Yerel yapay zekâ duman testi (spec 0012 AC-11) — isteğe bağlı, ./scripts/check'te YOK (CI'da ve Pi'de model yok).
// Bu bilgisayarda Ollama ve ses servisi (tools/ai/start-stt.ps1) açıkken gerçek modelleri dener:
//   1) Ollama ve ses servisi ayakta mı (kapalıysa hangisi olduğunu yazar, çıkış 1),
//   2) sabit, kurgusal bir dökümü uygulamanın talimat dosyasıyla (backend/.../Prompts/voice-message.tr.txt) taslağa çevirir,
//   3) --audio <dosya> verilirse dosyayı yazıya döker ve dökümden taslak üretir (tam zincir),
// ve süreleri yazar. Öğrenci verisi kullanılmaz; hiçbir dış servise istek gitmez (yalnızca verilen yerel adresler).
//
// Kullanım: node tools/ai/smoke.mjs [--audio not.m4a] [--ollama http://127.0.0.1:11434] [--stt http://127.0.0.1:8765] [--model gemma3:4b]
import { readFile } from "node:fs/promises";
import { dirname, extname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const promptFile = join(here, "..", "..", "backend", "src", "Application", "Features", "Ai", "Prompts", "voice-message.tr.txt");
const sampleTranscript = "merhaba ali bugün matematik ödevini getirmedi derste de biraz dalgındı yarın ödevini getirmesini rica ediyorum cuma günü de veli toplantısı var";
const audioTypes = { ".m4a": "audio/m4a", ".aac": "audio/aac", ".webm": "audio/webm", ".wav": "audio/wav", ".ogg": "audio/ogg", ".mp4": "audio/mp4" };

function option(name, fallback) {
  const index = process.argv.indexOf(`--${name}`);
  return index > 0 && process.argv[index + 1] ? process.argv[index + 1] : fallback;
}

const ollama = option("ollama", "http://127.0.0.1:11434").replace(/\/$/, "");
const stt = option("stt", "http://127.0.0.1:8765").replace(/\/$/, "");
const model = option("model", "gemma3:4b");
const audioPath = option("audio", null);

async function timed(work) {
  const started = performance.now();
  const result = await work();
  return { result, ms: Math.round(performance.now() - started) };
}

async function check(name, url, hint) {
  try {
    const response = await fetch(url, { signal: AbortSignal.timeout(5000) });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }
    return await response.json();
  } catch (error) {
    console.error(`KAPALI: ${name} (${url}) — ${error.message}. ${hint}`);
    return null;
  }
}

async function draft(instruction, transcript) {
  const response = await fetch(`${ollama}/api/chat`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      model,
      stream: false,
      keep_alive: "10m",
      messages: [
        { role: "system", content: instruction },
        { role: "user", content: transcript },
      ],
      options: { temperature: 0.3, num_predict: 400 },
    }),
    signal: AbortSignal.timeout(90_000),
  });
  if (!response.ok) {
    throw new Error(`Ollama HTTP ${response.status}`);
  }
  const text = ((await response.json()).message?.content ?? "").trim();
  if (!text) {
    throw new Error("Ollama boş yanıt verdi");
  }
  return text.slice(0, 1000);
}

async function main() {
  const tags = await check("Ollama", `${ollama}/api/tags`, "Ollama'yı başlatın (ollama serve).");
  const health = await check("Ses servisi", `${stt}/health`, "Başlatın: powershell -File tools/ai/start-stt.ps1");
  if (!tags || !health) {
    process.exit(1);
  }
  if (!(tags.models ?? []).some((m) => m.name === model || m.model === model)) {
    console.error(`KAPALI: Ollama'da '${model}' modeli yok (ollama pull ${model}).`);
    process.exit(1);
  }
  console.log(`Ollama: açık, model ${model} · Ses servisi: açık, ${health.model} ${health.device}/${health.computeType}`);

  const instruction = (await readFile(promptFile, "utf8")).replace(/\r\n/g, "\n").trim();
  const fixed = await timed(() => draft(instruction, sampleTranscript));
  console.log(`\n[1] Sabit döküm → taslak (${fixed.ms} ms):\n${fixed.result}`);

  if (audioPath) {
    const audio = await readFile(audioPath);
    const type = audioTypes[extname(audioPath).toLowerCase()] ?? "application/octet-stream";
    const heard = await timed(async () => {
      const response = await fetch(`${stt}/transcribe`, { method: "POST", headers: { "Content-Type": type }, body: audio, signal: AbortSignal.timeout(60_000) });
      if (!response.ok) {
        throw new Error(`Ses servisi HTTP ${response.status}`);
      }
      return response.json();
    });
    console.log(`\n[2] Ses → döküm (${heard.ms} ms; ses ${heard.result.duration} sn, ${audio.length} bayt):\n${heard.result.text}`);
    if (!heard.result.text) {
      console.error("Döküm boş — ses anlaşılamadı.");
      process.exit(1);
    }
    const chained = await timed(() => draft(instruction, heard.result.text));
    console.log(`\n[3] Döküm → taslak (${chained.ms} ms):\n${chained.result}`);
    console.log(`\nToplam (ses → taslak): ${heard.ms + chained.ms} ms (hedef ≤ 30000 ms)`);
  }

  console.log("\nDUMAN TESTİ TAMAM");
}

main().catch((error) => {
  console.error(`DUMAN TESTİ BAŞARISIZ: ${error.message}`);
  process.exit(1);
});
