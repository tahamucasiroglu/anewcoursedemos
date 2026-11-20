# Yerel yapay zekâ (MVP)

Sesli nottan veli mesajı ve haftalık gelişim özeti (yol haritası 6 ve 8) bu bilgisayarda çalışır; dış servis yok.

| Ne | Nerede | Not |
|---|---|---|
| Metin modeli | Ollama (kullanıcı kurulumu, `%LOCALAPPDATA%\Programs\Ollama`), model `gemma3:4b` | API `http://127.0.0.1:11434`; ekran kartında (RTX 3060 6 GB) çalışır |
| Ses → metin | `tools/ai/.venv` (Python 3.12, `uv` ile; sistemdeki Python'a dokunulmaz), faster-whisper `medium` | Model `tools/ai/models/` altında; CUDA kütüphaneleri ortamın içinde |

Sürümler `requirements.lock.txt` dosyasında sabit (`av<16`: faster-whisper 1.2.1 ile uyum). Yeniden kurulum:
`uv venv --python 3.12 tools/ai/.venv` → `uv pip install --python tools/ai/.venv/Scripts/python.exe -r tools/ai/requirements.lock.txt`.
Windows'ta CUDA kütüphaneleri için `site-packages/nvidia/*/bin` klasörleri süreç içinde arama yoluna eklenir.
