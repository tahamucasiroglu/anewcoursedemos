"""
Taha Konsept e2e — Türkçe seslendirme için kaba anlaşılırlık denetimi (faster-whisper, CPU int8).

  uv run --python 3.12 --with faster-whisper python tests/e2e/whisper-check.py <ses.wav> <beklenen.txt> [--model small] [--out sonuc.json]

<beklenen.txt>: TTS'e giden okunuş metni (matrix.cjs'in yazdığı spoken.txt kabul edilir; '→' satırları alınır).
Çıktı: transkript + kelime geri çağırma oranı (beklenen kelimelerin transkriptte geçme yüzdesi; okunuşa ve yazılışa göre, büyüğü). Model önbelleği:
HF_HOME verilmezse .tools-dev/e2e-agent/Models/hf-cache (git dışı). Ağ yalnız ilk model indirmesi için gerekir.
"""
import json
import os
import re
import sys
import time

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FFMPEG = os.path.join(ROOT, ".tools-dev", "e2e-agent", "Tools", "ffmpeg", "bin", "ffmpeg.exe")
os.environ.setdefault("HF_HUB_DISABLE_SYMLINKS_WARNING", "1")
os.environ.setdefault("HF_HOME", os.path.join(ROOT, ".tools-dev", "e2e-agent", "Models", "hf-cache"))


def words(text):
    text = text.replace("I", "ı").replace("İ", "i").lower()
    return re.findall(r"[a-zçğıöşüâîû0-9]+", text)


def main():
    args = sys.argv[1:]
    if len(args) < 2:
        print(__doc__)
        return 2
    wav, expected_file = args[0], args[1]
    model_name = args[args.index("--model") + 1] if "--model" in args else "small"
    out = args[args.index("--out") + 1] if "--out" in args else None
    raw = open(expected_file, encoding="utf-8").read()
    arrows = [l.split("→", 1)[1] for l in raw.splitlines() if "→" in l]
    written = [re.sub(r"^\[\d+\]\s*", "", l) for l in raw.splitlines() if re.match(r"^\[\d+\]", l)]
    expected = " ".join(arrows) if arrows else raw
    # Whisper sayıları rakamla, yabancı adları özgün yazımıyla yazar ("1453", "Google"); bu yüzden yazılış metnine göre de ölçülür.
    expected_written = " ".join(written) if written else expected

    import subprocess
    import numpy as np
    from faster_whisper import WhisperModel

    # Ses ajan ffmpeg'iyle 16 kHz mono PCM'e çevrilir (PyAV sürüm uyumsuzluklarından bağımsız).
    pcm = subprocess.run([FFMPEG, "-v", "error", "-i", wav, "-ac", "1", "-ar", "16000", "-f", "s16le", "-"], capture_output=True, check=True).stdout
    audio = np.frombuffer(pcm, dtype=np.int16).astype(np.float32) / 32768.0

    t0 = time.time()
    model = WhisperModel(model_name, device="cpu", compute_type="int8")
    segments, info = model.transcribe(audio, language="tr", beam_size=5, vad_filter=False)
    transcript = " ".join(s.text.strip() for s in segments)
    got = set(words(transcript))

    def recall_of(text):
        exp = words(text)
        return len([w for w in exp if w in got]) / max(1, len(exp)), [w for w in exp if w not in got]

    spoken_recall, spoken_missing = recall_of(expected)
    written_recall, written_missing = recall_of(expected_written)
    recall = max(spoken_recall, written_recall)
    result = {
        "model": model_name,
        "seconds": round(time.time() - t0, 1),
        "audioSec": round(info.duration, 2),
        "transcript": transcript,
        "expected": expected,
        "wordRecall": round(recall, 3),
        "spokenRecall": round(spoken_recall, 3),
        "writtenRecall": round(written_recall, 3),
        "missingSpoken": spoken_missing,
        "missingWritten": written_missing,
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    if out:
        with open(out, "w", encoding="utf-8") as f:
            json.dump(result, f, ensure_ascii=False, indent=2)
    return 0


if __name__ == "__main__":
    sys.exit(main())
