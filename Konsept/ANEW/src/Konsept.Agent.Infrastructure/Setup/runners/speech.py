"""ANEW Konsept ajanı — metinden konuşma çalıştırıcısı (spec 0028 Req 8).

Tek betik, ses türündeki her yerel model için: model bileşen listesindeki şablonla çağrılır, arka uç (--backend) modeli
yükleme ve sentez biçimini seçer. Tüm sahneler tek süreçte üretilir (model bir kez yüklenir).

Girdi (--input): {"items": [{"text": "...", "output": "C:/.../voice.raw.wav"}, ...]}
Çıktı (--output): {"device": "cpu"|"gpu", "items": [{"output": "...", "seconds": 7.03}, ...]}

Paketler bileşen klasöründen (--site) yüklenir; Python -I (yalıtılmış) çalışır, ağ kullanılmaz (HF_HUB_OFFLINE=1).
Hata: çıkış kodu 2, kısa açıklama standart hataya.
"""

import argparse
import json
import re
import site
import sys
import time


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--site", required=True)
    parser.add_argument("--backend", required=True, choices=["supertonic", "chatterbox"])
    parser.add_argument("--model-dir", required=True)
    parser.add_argument("--lang", required=True)
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--voice", default="F1")
    parser.add_argument("--model-name", default="supertonic-3")
    parser.add_argument("--t3", default="t3_mtl23ls_v3.safetensors")
    return parser.parse_args()


def sentences(text, limit=280):
    """Uzun metni cümle sınırlarından parçalar (Chatterbox tek seferde ~40 sn üretir)."""
    parts, current = [], ""
    for piece in re.split(r"(?<=[.!?…])\s+", text.strip()):
        if current and len(current) + len(piece) + 1 > limit:
            parts.append(current)
            current = piece
        else:
            current = (current + " " + piece).strip()
    if current:
        parts.append(current)
    return parts or [text]


def run_supertonic(args, items):
    from supertonic import TTS
    from supertonic.config import AVAILABLE_LANGUAGES

    tts = TTS(model=args.model_name, model_dir=args.model_dir, auto_download=False)
    style = tts.get_voice_style(args.voice)
    lang = args.lang if args.lang in AVAILABLE_LANGUAGES else "na"
    results = []
    for item in items:
        wav, duration = tts.synthesize(item["text"], voice_style=style, lang=lang)
        tts.save_audio(wav, item["output"])
        seconds = float(duration[0]) if hasattr(duration, "__len__") else float(duration)
        results.append({"output": item["output"], "seconds": seconds})
    return "cpu", results


def run_chatterbox(args, items):
    import numpy as np
    import soundfile as sf
    import torch
    import chatterbox.mtl_tts as mtl

    device = "cuda" if torch.cuda.is_available() else "cpu"
    # chatterbox-tts 0.1.7 yükleyicisi çok dilli T3 dosyasının adını sabit tutar; v3 ağırlığı aynı mimaridedir (üst sürümde adla seçilir).
    original = mtl.load_safetensors
    mtl.load_safetensors = lambda path, *a, **k: original(str(path).replace("t3_mtl23ls_v2.safetensors", args.t3), *a, **k)
    model = mtl.ChatterboxMultilingualTTS.from_local(args.model_dir, device)
    gap = np.zeros(int(model.sr * 0.15), dtype=np.float32)
    results = []
    for item in items:
        chunks = []
        for part in sentences(item["text"]):
            wav = model.generate(part, language_id=args.lang, repetition_penalty=1.2)
            chunks.append(wav.squeeze(0).detach().cpu().numpy().astype(np.float32))
            chunks.append(gap)
        audio = np.concatenate(chunks[:-1]) if chunks else gap
        sf.write(item["output"], audio, model.sr)
        results.append({"output": item["output"], "seconds": len(audio) / float(model.sr)})
    return ("gpu" if device == "cuda" else "cpu"), results


def main():
    args = parse_args()
    site.addsitedir(args.site)
    with open(args.input, encoding="utf-8") as handle:
        items = json.load(handle)["items"]
    started = time.time()
    backend = run_supertonic if args.backend == "supertonic" else run_chatterbox
    device, results = backend(args, items)
    with open(args.output, "w", encoding="utf-8") as handle:
        json.dump({"device": device, "items": results, "elapsed": round(time.time() - started, 2)}, handle)


if __name__ == "__main__":
    try:
        main()
    except Exception as error:  # noqa: BLE001 — ajan çıkış kodu ve kısa mesajla bildirir.
        print(f"speech runner error: {type(error).__name__}: {error}", file=sys.stderr)
        sys.exit(2)
