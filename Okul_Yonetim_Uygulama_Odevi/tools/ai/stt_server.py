"""Yerel ses -> metin servisi (spec 0012 C-1, AC-10).

Yalnızca Python standart kütüphanesi (http.server) + faster-whisper. Model açılışta bir kez yüklenir; ses yalnızca
bellekte (BytesIO) işlenir, diske yazılmaz; döküm loglanmaz (yalnızca süre ve ölçümler). Varsayılan olarak yalnızca
127.0.0.1 dinlenir; yerel ağ demosu için STT_HOST=0.0.0.0 (tools/ai/README.md, güvenlik duvarı kuralıyla).

Uçlar:
  GET  /health      -> {"status":"ok","model":"medium","device":"cuda","computeType":"float16","loaded":true}
  POST /transcribe  -> ham ses gövdesi (Content-Type: audio/...) -> {"text":"...","duration":21.4,"language":"tr","elapsedMs":3100}

Süre dökümden ÖNCE ölçülür (ses 16 kHz'e çözülüp örnek sayısından): STT_MAX_SECONDS'u aşan ses dökülmez, metin boş ve
ölçülen süreyle hemen döner ("skipped":"too-long") — API "en fazla 60 saniye" der; CPU'da uzun döküm sırayı kilitlemez.

Ortam değişkenleri: STT_HOST (127.0.0.1), STT_PORT (8765), STT_MODEL (medium), STT_DEVICE (auto|cuda|cpu),
STT_MAX_BYTES (3145728), STT_MAX_SECONDS (60).
"""

import io
import json
import os
import sys
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

HERE = os.path.dirname(os.path.abspath(__file__))
MODELS = os.path.join(HERE, "models")
HOST = os.environ.get("STT_HOST", "127.0.0.1")
PORT = int(os.environ.get("STT_PORT", "8765"))
MODEL_NAME = os.environ.get("STT_MODEL", "medium")
DEVICE = os.environ.get("STT_DEVICE", "auto")
MAX_BYTES = int(os.environ.get("STT_MAX_BYTES", str(3 * 1024 * 1024)))
MAX_SECONDS = float(os.environ.get("STT_MAX_SECONDS", "60"))
SAMPLE_RATE = 16000


if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")


def log(message):
    stamp = time.strftime("%Y-%m-%d %H:%M:%S")
    print(f"{stamp} {message}", file=sys.stderr, flush=True)


def add_cuda_paths():
    """Windows: ortamın içindeki CUDA kütüphanelerini (site-packages/nvidia/*/bin) süreç içinde arama yoluna ekler."""
    if os.name != "nt":
        return
    import site

    for root in site.getsitepackages():
        nvidia = os.path.join(root, "nvidia")
        if not os.path.isdir(nvidia):
            continue
        for name in os.listdir(nvidia):
            folder = os.path.join(nvidia, name, "bin")
            if os.path.isdir(folder):
                os.add_dll_directory(folder)
                os.environ["PATH"] = folder + os.pathsep + os.environ.get("PATH", "")


def load_model():
    os.environ.setdefault("HF_HUB_OFFLINE", "1")
    add_cuda_paths()
    from faster_whisper import WhisperModel

    choices = [("cuda", "float16"), ("cpu", "int8")]
    if DEVICE == "cuda":
        choices = choices[:1]
    elif DEVICE == "cpu":
        choices = choices[1:]
    last_error = None
    for device, compute_type in choices:
        try:
            started = time.monotonic()
            model = WhisperModel(MODEL_NAME, device=device, compute_type=compute_type, download_root=MODELS, local_files_only=True)
            log(f"model yüklendi: {MODEL_NAME} {device}/{compute_type} ({time.monotonic() - started:.1f} sn)")
            return model, device, compute_type
        except Exception as error:  # noqa: BLE001 — CUDA yoksa CPU'ya düşülür
            last_error = error
            log(f"{device}/{compute_type} açılamadı: {type(error).__name__}")
    raise SystemExit(f"Model yüklenemedi ({MODEL_NAME}): {last_error}")


MODEL, MODEL_DEVICE, MODEL_COMPUTE = None, None, None
WORK = threading.Lock()


class Handler(BaseHTTPRequestHandler):
    server_version = "okul-stt"
    sys_version = ""

    def log_message(self, format, *args):  # noqa: A002 — standart imza; yalnızca istek satırı (gövde yok)
        log(f"{self.command} {self.path.split('?')[0]} {args[1] if len(args) > 1 else ''}")

    def send_json(self, status, body):
        data = json.dumps(body, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path.split("?")[0] != "/health":
            self.send_json(404, {"error": "bulunamadı"})
            return
        self.send_json(200, {
            "status": "ok",
            "model": MODEL_NAME,
            "device": MODEL_DEVICE,
            "computeType": MODEL_COMPUTE,
            "loaded": MODEL is not None,
        })

    def do_POST(self):
        if self.path.split("?")[0] != "/transcribe":
            self.send_json(404, {"error": "bulunamadı"})
            return
        length = int(self.headers.get("Content-Length") or 0)
        if length <= 0:
            self.send_json(400, {"error": "ses boş"})
            return
        if length > MAX_BYTES:
            self.send_json(413, {"error": "ses çok büyük"})
            return
        audio = io.BytesIO(self.rfile.read(length))
        started = time.monotonic()
        try:
            from faster_whisper import decode_audio

            samples = decode_audio(audio, sampling_rate=SAMPLE_RATE)
        except Exception as error:  # noqa: BLE001 — çözülemeyen ses 400; ayrıntı (içerik) loglanmaz
            log(f"çözülemedi: {type(error).__name__}")
            self.send_json(400, {"error": "ses çözülemedi"})
            return
        duration = len(samples) / SAMPLE_RATE
        if duration > MAX_SECONDS:
            elapsed = int((time.monotonic() - started) * 1000)
            log(f"dökülmedi (uzun): {length} bayt, {duration:.1f} sn ses, {elapsed} ms")
            self.send_json(200, {"text": "", "duration": round(duration, 2), "language": "tr", "elapsedMs": elapsed, "skipped": "too-long"})
            return
        try:
            with WORK:
                segments, info = MODEL.transcribe(samples, language="tr", vad_filter=True, beam_size=5)
                text = " ".join(segment.text.strip() for segment in segments).strip()
        except Exception as error:  # noqa: BLE001 — dökülemeyen ses 400; ayrıntı (içerik) loglanmaz
            log(f"dökülemedi: {type(error).__name__}")
            self.send_json(400, {"error": "ses çözülemedi"})
            return
        elapsed = int((time.monotonic() - started) * 1000)
        log(f"döküm: {length} bayt, {info.duration:.1f} sn ses, {len(text)} karakter, {elapsed} ms")
        self.send_json(200, {"text": text, "duration": round(info.duration, 2), "language": info.language, "elapsedMs": elapsed})


def main():
    global MODEL, MODEL_DEVICE, MODEL_COMPUTE
    MODEL, MODEL_DEVICE, MODEL_COMPUTE = load_model()
    server = ThreadingHTTPServer((HOST, PORT), Handler)
    log(f"ses servisi dinliyor: http://{HOST}:{PORT} (durdurmak için Ctrl+C)")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
        log("ses servisi durdu")


if __name__ == "__main__":
    main()
