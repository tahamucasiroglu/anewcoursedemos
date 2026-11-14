# Dalga 2 — providers-media (online görsel + ses sağlayıcıları)

Online image and speech providers are in place in the Providers module, and `IMediaGateway` is implemented. Every one of the 11 online image models and 6 online speech models in catalog.json now has a concrete class. My projects build with 0 warnings and 0 errors, and the Providers test project passes 73/73 (40 existing + 33 new). No real provider was called (no keys), so request shapes are checked only against a fake HTTP handler. The architecture tests passed 16 of 17 at the time; the one failure was in the desktop agent's `*.axaml.cs` files. *(Entegratör notu: masaüstü dosyaları yeniden adlandırıldı; mimari testler artık 17/17.)*

**Public interfaces** (root `src/Modules/Providers/TahaKonsept.Modules.Providers.Application/`)
- `Concrete/MediaGateway.cs` implements `IMediaGateway` (signature unchanged). It is a singleton, built like `ProviderGateway`:
  - Checks the user id, looks up the model, and checks it is an online model of the right kind.
  - Gets the key from `IApiKeyVault`, builds the model through the factory and runs it.
  - If a speech request has no voice, it uses the default from `IVoiceCatalog`.
  - Retry and CallLog cost come from `AiModelBase`.
- `TahaKonsept.Modules.Providers.Application.Media.Abstract.IMediaModelFactory` (base class `Media/Base/MediaModelFactoryBase`):
  - `Result<IImageModel> CreateImage(ModelInfo model, ProviderCredential credential)`
  - `Result<ISpeechModel> CreateSpeech(ModelInfo model, ProviderCredential credential)`
- `...Media.Abstract.IVoiceCatalog` (base class `VoiceCatalogBase`):
  - `Result<IReadOnlyList<VoiceInfo>> List(string? modelKey)`
  - `string? DefaultVoiceId(string modelKey)`
  - Voices made for Turkish come first, then the default, then the rest.
- New contract `src/BuildingBlocks/TahaKonsept.Shared.Contracts/Providers/VoiceInfo.cs`: `{Id, DisplayName, Language, Gender, NativeTurkish, IsDefault, Notes}`.

**Infrastructure** (root `src/Modules/Providers/TahaKonsept.Modules.Providers.Infrastructure/`)
- **Shared code** in `Media/Common`:
  - `MediaHttp`: sending requests, mapping HTTP errors to the existing `ProviderErrorCodes`, downloading result URLs and `data:` URIs.
  - `MediaJson`: reading JSON responses.
  - `AudioFormats`: wrapping PCM as WAV, and measuring duration from the WAV header or by counting MP3 frames.
  - `ImageSizes`, `MediaModelOptions`, and `GeminiProtocol` (used by both Gemini image and Gemini TTS).
- **Image base classes**:
  - `ImageModels/Base/HttpImageModelBase` handles download, repeating the call for APIs that return one image per call, and per-image usage.
  - `PollingImageModelBase` handles submit, then poll, then download.
- **Image models**:
  - `GeminiImageModel` (3 models): IMAGE modality, aspect ratio, seed.
  - `OpenAiImageModel`: `b64_json` response. Cost is token-based when usage comes back and the catalog has token prices, otherwise per image, so nothing is counted twice.
  - `FalImageModel`: `fal.run` synchronous endpoint with `sync_mode`.
  - `BflImageModel`: polling.
  - `ZaiImageModel`.
  - `ReplicateImageModel`: polling.
- **Speech base class**: `SpeechModels/Base/HttpSpeechModelBase` records characters, seconds and any tokens, so whichever price the catalog defines (per 1K characters, per minute or per token) is applied.
- **Speech models**:
  - `OpenAiSpeechModel`: voice plus Turkish reading instructions.
  - `ElevenLabsSpeechModel`: MP3, or PCM wrapped as WAV.
  - `GeminiSpeechModel`: PCM wrapped as WAV; output is always WAV.
  - `AzureSpeechModel`: SSML with escaping, speed and a `<lang>` wrapper for multilingual voices; the region comes from the key vault, and a missing region gives `KeyMissing`.
  - `CartesiaSpeechModel`.
- `Media/Concrete/MediaModelFactory` (named HttpClient `providers-media`, 5-minute timeout) and `StaticVoiceCatalog`. The Azure list starts with tr-TR-AhmetNeural (default) and tr-TR-EmelNeural.
- DI registers `IMediaModelFactory`, `IVoiceCatalog` and `IMediaGateway` as singletons. catalog.json is unchanged.

**API route**: `GET api/providers/voices?modelKey=` was added to `ProvidersController` as a one-line action. *(Entegratör notu: production-web `KonsepttenVideoController.Sesler` bu rotayı vekilliyor.)*

**Tests** are in `tests/TahaKonsept.Modules.Providers.Tests/{ImageModels,SpeechModels,Media}`. For each platform family they check the request shape and auth header, response parsing (base64, bytes, data URI, polling), cost, and error mapping (refused, empty response, 401, error body detail). There are also gateway end-to-end tests with the real factory, a check that every online model in the catalog can be built, voice catalog tests, and helper tests.

**Commits**: `3dab869` (code), `968dfa5` (tests). Not pushed.

**For the integrator**
- Budget is not enforced in `IMediaGateway`; Production must call `IBudgetGuard` first.
- When a request asks for several images and a later one fails, the cost of the images already made is not logged.
- Replicate is capped at 4 images per request (`num_outputs` 4) and fal is clamped the same way, so asking for more returns at most 4.
- Gemini TTS always returns WAV, whatever format is requested.
- Cartesia has no price in the catalog, so its cost is logged as $0.

**Not verified** (no live calls):
- **Image models**:
  - Gemini: `responseModalities:["IMAGE"]` alone and `imageConfig.aspectRatio` on these model ids.
  - gpt-image-2: the size list and the `quality` / `output_format` fields.
  - fal: `sync_mode` returning data URIs, and the `image_size` fields for z-image / flux-2.
  - BFL: the `flux-3-image-klein` endpoint name and the multiple-of-16 size rule.
  - Z.ai: the `glm-image` size format.
  - Replicate: the input fields for flux-schnell.
- **Speech models**:
  - ElevenLabs: `eleven_v4` ids and `language_code` support; the premade voice ids come from memory.
  - OpenAI TTS: whether speed is honoured, and whether voice names like ballad and verse are available.
  - Gemini TTS: the 24 kHz mime type, and the voice list on 3.8.
  - Azure: the multilingual voice names, and whether tr-TR-AhmetNeural and tr-TR-EmelNeural still exist.
  - Cartesia: the API version, the `language` field, and the sample voice UUID, which is a placeholder; the user should pick a real Turkish voice id.
