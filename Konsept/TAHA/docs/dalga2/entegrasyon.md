# Dalga 2 — entegrasyon raporu (14 Kas 2026)

Dalga 2 parçaları (providers-media, normalize, concepts, production-core, desktop-app, production-web) tek ağaçta birleştirildi, derlendi, test edildi, canlı denendi ve Pi'ye **0.2.0** olarak yayınlandı. Parça raporları: `docs/dalga2/<parça>.md`.

## Bağlananlar
- **Çözüm / host:** tüm yeni projeler `TahaKonsept.slnx`'te; `ApiModules.cs` sırası Identity → Profiles → Providers → **Concepts → Production** → Agents → Diagnostics (migration sırası); `TahaKonsept.Api.csproj` iki modülün Api projesine başvuruyor.
- **Ajan olay işleyicisi:** Production `services.Replace(Scoped<IAgentStageEventHandler, ProductionStageEventHandler>)`; Agents sonradan `TryAddScoped<…, NullAgentStageEventHandler>` yapar → kayıt zaten olduğu için eklenmez. Sıradan bağımsız doğru.
- **Konsept adaptörü (yeni):** `TahaKonsept.Modules.Production.Infrastructure.Integration.Concrete.ConceptsConceptSource : ConceptSourceBase` — Concepts `IConceptReader.GetAsync` → `ConceptSnapshot` (Id, Title — boşsa belge başlığı, ProfileId, Document); `DeletedAt` dolu ya da durum `Deleted` ise `null`. `AddProductionModule` içinde `AddProductionCore()`'dan sonra `Replace(Scoped<IConceptSource, ConceptsConceptSource>)` (testlerin `AddProductionCore` + sahte kaynağı etkilenmez). Production.Infrastructure → yalnız **Concepts.Application** başvurusu. 3 yeni test (`tests/TahaKonsept.Modules.Production.Tests/Integration/ConceptsConceptSourceTests.cs`).
- **Okunuş önerileri:** iş başlarken `JobFactory`, konsept belgesindeki `Pronunciations`'ı `IPronunciationSuggestions.AddSuggestionsAsync` ile sözlüğe **Öneri** olarak ekler (aynı yazılış varsa dokunmaz). Concepts `IConceptPronunciationSource` aynı veriyi verdiği için kullanılmadı.
- **Medya / normalizasyon:** Production `IMediaGateway`'i (Providers, singleton) `ImagesStageHandler`, `SpeechStageHandler`, `PronunciationService`'te kullanıyor; `AddProductionCore` → `AddSpeechTextNormalization()` çağrılıyor (doğrulandı).
- **Web:** `.nupkg` için statik dosya içerik türü eşlemesi (`WebApplicationExtensions.UseTahaWeb`) — ajan güncellemeleri `/downloads/*.nupkg` indirebilir.
- **Yayın betiği:** `deploy/publish-pi.ps1` artık çalışma klasöründeki `wwwroot/downloads/*` (gitignore'daki Setup.exe, .nupkg, releases/assets json, RELEASES) dosyalarını web çıktısına kopyalıyor (`-Source Head` ile de) ve doğrulamada Setup.exe'yi kontrol ediyor.
- **Identity `agent_device_id` (dalga 1 boşluk 5):** YAPILMADI. `ITokenIssuer.IssueAsync(userId, kind)` imzası ek claim almıyor ve refresh token döndürmede (`RotateAsync`) claim'i yeniden üretmek için cihaz kimliğinin refresh token kaydında saklanması gerekir (Identity şeması + migration). Küçük değişiklik değil. **Güncelleme (inceleme düzeltmesi):** hub artık cihazı token ailesiyle (`tfam` → `AgentDevice.TokenFamilyId`) bulur, `client`≠`agent` token'ı reddeder; Hello'daki bilgisayar adı kimlik için kullanılmaz.
- **Sürüm:** `VERSION` 0.1.0 → **0.2.0**. `docs/WEB-KABUK.md`'ye dalga 2 MVC eylemleri eklendi.

## Derleme / test
- `dotnet build TahaKonsept.slnx --no-incremental`: **0 uyarı, 0 hata**.
- Testler (15 proje) **752/752 yeşil**: Agent.Core 62, Engines.Image 40, Engines.Speech 29, Engines.Video 45, Agent 42, Architecture 17, Integration 3, Agents 32, Concepts 52, Diagnostics 41, Identity 22, Normalization 236, Production 39, Profiles 19, Providers 73.

## Canlı duman testi (yerel, geliştirme DB'si — tünel 5433)
- API 7191 + Web 7190 (`Kestrel__Endpoints__Http__Url` ile; `--urls` appsettings Kestrel uç noktasını ezmiyor).
- 7 modülün migration'ı uygulandı (Concepts, Production dahil), orkestratör başladı; `api/health` → `ok`, `database: ready`. Logda WRN/ERR yok.
- 45 yeni rota (concepts 20, production 24, providers/voices 1) oturumsuz **401**; bilinmeyen rota 404 (401 genel değil).
- Web `/Konseptler`, `/KonsepttenVideo`, `/Uretilenler`, `/Silinenler`, `/Ayarlar` → **302** `/Account/Login`; `/downloads/` altındaki Setup.exe, .nupkg, releases.win.json → 200. İki host durduruldu.

## Pi yayını
- `deploy\publish-pi.ps1 -Source Head` (HEAD `f6b047c`) → `0.2.0-20261114-075813` (paket 252.6 MB; ajan indirmeleri 160.6 MB dahil).
- `http://192.168.1.102:7111/api/health` → `{"status":"ok","version":"0.2.0","environment":"Pi","database":"ready"}` (kurulum betiğinin kendi kontrolü migration sürerken `degraded/unavailable` gördü ama geçti; birkaç saniye sonra `ready`).
- `http://192.168.1.102:7110/` → 302 `/Account/Login`; `/downloads/TahaKonsept.Agent-win-Setup.exe` → 200 (87 991 142 bayt); `.nupkg` → 200; Pi'de `api/concepts`, `api/production/queue` → 401.

## Kalan boşluklar
1. **Oturumlu uçtan uca hiç denenmedi:** geliştirme DB'sinde yönetici yok (tek seferlik kurulum çalıştırılmadı); gerçek LLM / görsel / ses sağlayıcı çağrısı, gerçek ajan eşleme ve üretim yapılmadı.
2. **Sayfalar tarayıcıda açılmadı:** Konseptler, Silinenler, Konseptten Video, Üretilenler, Okunuş sözlüğü — düzen, koyu mod, modallar, konsol hataları kontrol edilmedi.
3. **Identity `agent_device_id` claim'i yok** (yukarıda; refresh token'da cihaz kimliği saklama + migration gerekir).
4. **Kurulum betiği sağlık kontrolü** `degraded` durumunu da başarı sayıyor; migration bitmeden "geçti" diyebilir (`install-release.sh` `database: ready` beklemeli).
5. **Ajan paketi 0.1.0:** `wwwroot/downloads` içindeki Setup.exe/.nupkg 0.1.0 sürümüyle paketlendi; 0.2.0 için `deploy/agent/pack-agent.ps1` yeniden çalıştırılmadı. Paketler imzasız; Velopack güncellemesi canlı denenmedi.
6. **Bellekte tutulan durum:** Concepts revizyon adayları + üretim görevi API yeniden başlayınca kaybolur.
7. **Okunuş deneme kutusu ücretli:** önizleme her zaman ses sentezliyor; `POST api/production/pronunciations/normalize` (yalnız metin) rotası yok.
8. **Online ses yalnız WAV birleşir:** MP3 dönen model sunucuda birleştirilemez (Production `Format="wav"` istiyor; Gemini her zaman WAV).
9. **Medya maliyet boşlukları:** çok görselli istekte sonradan hata olursa önceki görsellerin maliyeti loglanmaz; Cartesia fiyatı katalogda yok ($0); Replicate/fal en çok 4 görsel.
10. **Doğrulanmamış sağlayıcı ayrıntıları:** model kimlikleri/alanları (bkz. `providers-media.md` "Not verified"), Cartesia örnek ses UUID'i yer tutucu.
11. **Normalizasyon küçük boşlukları:** ayrık para simgesi eki, "COVID-19", "1,2,3" listeleri.
12. **VRAM uyarısı** ajan `vramMb` bildirmezse görünmez.
13. **Production kilidi:** sunucu aşaması sürerken çalışan sıra öğesini silmek mevcut LLM çağrısı bitene kadar bekler; iş bitince silinen online artefaktlar yeni çalışma alanlı yeni sürümde yeniden üretilir (yeni maliyet).
14. Dalga 1'den kalan: `diagnostics.error_reports`'taki duman testi satırı.

## Commit'ler
`fe7f32c` (adaptör + test + .nupkg eşlemesi), `f6b047c` (0.2.0, publish-pi indirmeler, dalga 2 raporları, WEB-KABUK), bu rapor + DEPLOY kaydı ayrı commit. Push yok.
