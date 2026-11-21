# Yerel yapay zekâ (MVP)

Sesli nottan veli mesajı (spec 0012) ve haftalık gelişim özeti (yol haritası 8) bu bilgisayarda çalışır; dış servis yok.

| Ne | Nerede | Not |
|---|---|---|
| Metin modeli | Ollama (kullanıcı kurulumu, `%LOCALAPPDATA%\Programs\Ollama`), model `gemma3:4b` | API `http://127.0.0.1:11434`; ekran kartında (RTX 3060 6 GB) çalışır |
| Ses → metin | `tools/ai/.venv` (Python 3.12, `uv` ile; sistemdeki Python'a dokunulmaz), faster-whisper `medium` | Model `tools/ai/models/` altında (`small` da var); servis `stt_server.py` |

Sürümler `requirements.lock.txt` dosyasında sabit (`av<16`: faster-whisper 1.2.1 ile uyum). Yeniden kurulum:
`uv venv --python 3.12 tools/ai/.venv` → `uv pip install --python tools/ai/.venv/Scripts/python.exe -r tools/ai/requirements.lock.txt`.
Windows'ta CUDA kütüphaneleri için `site-packages/nvidia/*/bin` klasörleri süreç içinde arama yoluna eklenir.

## Ses servisini başlatma

```powershell
powershell -ExecutionPolicy Bypass -File tools/ai/start-stt.ps1          # 127.0.0.1:8765, medium, CPU (int8)
powershell -ExecutionPolicy Bypass -File tools/ai/start-stt.ps1 -Device cuda   # kart boşsa GPU (float16)
```

Ön planda çalışır, Ctrl+C ile durur. Model açılışta bir kez yüklenir (~7 sn). Uçlar: `GET /health` (model yüklü mü,
cihaz), `POST /transcribe` (ham ses gövdesi, `Content-Type: audio/...` → `{"text","duration","language","elapsedMs"}`).
Ses yalnızca bellekte işlenir, diske yazılmaz; döküm loglanmaz (yalnızca bayt, süre, karakter sayısı, ölçülen süre).
Süre dökümden **önce** ölçülür: 60 sn'yi (`STT_MAX_SECONDS`) aşan ses dökülmez, boş metin ve ölçülen süreyle hemen döner
(`"skipped":"too-long"`; API "Sesli not en fazla 60 saniye olabilir." der). Aynı anda tek döküm (iş kilidi). Windows hizmeti olarak otomatik başlatma yok (spec kapsam dışı).

**Neden varsayılan CPU?** 6 GB kartta masaüstü uygulamaları ~2 GB, Ollama `gemma3:4b` ~2,9 GB tutuyor; Whisper da GPU'da
kalınca bellek taşıyor ve Ollama'nın yanıtı 2 sn'den 60–90 sn'ye çıkıyor (plan 0012 Build evidence). CPU'da `medium`
15 sn'lik sesi ~13 sn'de döküyor; toplam (ses → taslak) ~15 sn — hedef ≤ 30 sn.

API ayarı (geliştirme, `appsettings.Development.json`): `Ai:TextGenerator:BaseUrl=http://127.0.0.1:11434`,
`Ai:TextGenerator:Model=gemma3:4b`, `Ai:SpeechToText:BaseUrl=http://127.0.0.1:8765`. Varsayılan (Pi/üretim) adresler boş →
"Yapay zekâ şu an kullanılamıyor"; yalnızca yerel adresler kabul edilir.

## Duman testi (isteğe bağlı, `scripts/check`'te yok)

Ollama ve ses servisi açıkken:

```powershell
node tools/ai/smoke.mjs                          # sabit, kurgusal döküm → taslak (uygulamanın talimat dosyasıyla)
node tools/ai/smoke.mjs --audio C:\yol\not.m4a   # + sesi yazıya döker, dökümden taslak üretir, toplam süreyi yazar
```

Servislerden biri kapalıysa hangisi olduğunu yazar ve 1 ile çıkar. Talimat: `backend/src/Application/Features/Ai/Prompts/voice-message.tr.txt`
(API aynı dosyayı derlemeye gömer). Öğrenci verisi kullanılmaz.

## Yerel ağ demosu (Pi → bu bilgisayar)

Pi'de model yok; demo günü Pi'deki API bu bilgisayarın servislerine yönlendirilebilir. Veri okulun yerel ağından çıkmaz.
Servisler **kimlik doğrulamasızdır** — yalnızca demo süresince ve yalnızca Pi'nin adresine açın:

1. Ollama'yı ağa açın: kullanıcı ortam değişkeni `OLLAMA_HOST=0.0.0.0:11434`, Ollama'yı yeniden başlatın.
2. Ses servisini ağa açın: `start-stt.ps1 -ListenHost 0.0.0.0`.
3. Windows güvenlik duvarı (yönetici PowerShell) — yalnızca Pi (`192.168.1.102`):
   `New-NetFirewallRule -DisplayName "Okul AI demo" -Direction Inbound -Protocol TCP -LocalPort 11434,8765 -RemoteAddress 192.168.1.102 -Action Allow`
4. Pi'de `/etc/okul/okul-api.env`'e `Ai__*` satırları (`docs/deploy-pi.md`) → `sudo systemctl restart okul-api`.

Demo bitince: kuralı silin (`Remove-NetFirewallRule -DisplayName "Okul AI demo"`), `OLLAMA_HOST`'u kaldırın, ses servisini
varsayılanla (`127.0.0.1`) başlatın, Pi'deki `Ai__*` satırlarını silin. Trafik düz HTTP'dir; yerel ağ dışına açılmaz.
