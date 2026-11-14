# Claude Notları

## Tuzaklar
- Kök `.gitignore` içinde `Source/` kuralı var (`core.ignorecase=true`); `TAHA/.gitignore` içindeki `!source/` ile geri alındı. Bu dosyayı silme.
- Ollama bu projede TTS için kullanılamaz (rapor 0001): Türkçe yok, Windows desteği yok. Supertonic ONNX ile C#'tan, Chatterbox gömülü Python ile.
- SignalR JS istemcisi içeride fetch kullanır → tarayıcıda kullanma (fetch yasak); tarayıcı jQuery polling.
- Supertonic reposu arşivlendi (2026-09-09): model ağırlıklarının kopyasını kendimiz tutmalıyız.
- Türkçe metinde `text-transform: uppercase` i/İ hatası → `lang="tr"`.
- Pi'de .NET yok → deploy için self-contained linux-arm64 yayın ya da .NET runtime kurulumu.
- Pi'de 7100-7105 "canlı" portlar başka proje için ayrılmış; Taha Konsept için ayrı boş port (ör. 7110 web, 7111 API) seçilecek — kesinleşince kararlar.md'ye yaz.

## Açık sorular
- Bloklamayan sorular `PLAN.md` §5'te, varsayılanlarla ilerleniyor.

- (14 Kas 2026) Piper Türkçe: `rhasspy/piper-voices` main artık yalnız dfki içeriyor; fahrettin/fettah `speaches-ai/piper-tr_TR-<ses>-medium` aynasından iner (adapter otomatik). MMS-TTS sözlüğünde noktalama yok, adapter parçalı sentezler. Python ses motorları uv yoksa ajan uv'yi kendisi kurar (IToolProvisioner).
- (14 Kas 2026) `deploy/publish-pi.ps1`'i Git Bash'ten çağırma: GNU `/usr/bin/tar` `C:` yolunu uzak makine sanıyor; PowerShell aracıyla çalıştır. Pi'de API host'u `Agent.Engines.Video.dll`'i de yükler — e2e yığını açıkken derleme kilitlenirse api (ve web) durdurulup yeniden başlatılır.
