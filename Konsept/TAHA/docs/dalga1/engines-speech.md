I built the local speech engines part and committed it as `d71204a`. Supertonic 3 now synthesizes the Turkish test sentence in C#, and all 29 speech tests pass, including two live ones. Both projects build with 0 warnings and 0 errors, and the 17 architecture tests still pass. None of the 13 Python engines has been run against its real model; only the worker protocol was tested, using a dummy adapter.

**What other modules use** (all under `TahaKonsept.Agent.Engines.Speech`)
- **Registration:** `DependencyInjection.SpeechEngineServiceCollectionExtensions.AddSpeechEngines(this IServiceCollection services)`. It registers 14 singletons, each as its concrete type and as `ISpeechEngine`. It also sets up the download HttpClient (`SpeechEngineServices.HttpClientName`), logging and `Base.SpeechEngineServices(IAgentPaths, IToolLocator, IProcessRunner, IHttpClientFactory, ILoggerFactory)`.
- **`Base.SpeechEngineBase : ISpeechEngine`**
  - `SynthesizeAsync` checks the request first: empty text, output path, language (`tr-TR` is treated as `tr`), speed between 0.5 and 2.0, and cloning with an existing reference file. If the engine isn't installed it returns the error `speech.not_installed`; it never starts a download on its own.
  - Helpers: `WriteWav`, `ReadDurationSec`, `ModelFilesExist`, `DownloadHuggingFaceFilesAsync(repo, files, progress, ct)` and `DownloadFileAsync`. Downloads go into `ModelsDir/<Key>`, as `.part` files first, then moved into place.
  - `public static Uri HuggingFaceUrl(string repo, string file, string revision = "main")` builds the `huggingface.co/<repo>/resolve/main/<file>` address.
  - `public bool SupportsLanguage(string?)`.
  - Error codes and Turkish messages are all in `Base.SpeechErrors`, codes `speech.*`.
- **`Concrete.SupertonicSpeechEngine`** (`supertonic-3`)
  - Ported from the official C# example; the full ONNX chain runs: text encoding, duration predictor, text encoder, 8 flow-matching steps, vocoder.
  - Long text is split into chunks with 0.3 s of silence between them. Speed is the request speed × 1.05.
  - Voices are F1–F5 and M1–M5, or a path to a custom style JSON.
  - It tries CUDA and otherwise uses CPU. Since the project uses the CPU-only ONNX Runtime package, it currently always runs on CPU.
  - The model (about 400 MB) goes to `ModelsDir/supertonic-3/{onnx,voice_styles}`.
- **`Base.PythonSpeechEngineBase`**
  - Setup uses uv: `uv venv` under `PythonEnvsDir/<Key>/.venv` (Python 3.11 by default, downloaded by uv into `ToolsDir/uv-python`), then `uv pip install` of the pinned packages. GPU engines get the PyTorch CUDA wheel index (cu124 or cu128).
  - A fingerprint of the package list is written to a marker file. `IsInstalled()` compares it, so changing the pins triggers a reinstall.
  - `PythonWorkers/tts_worker.py` runs as one long-lived process using the JSON-lines protocol you specified. Models load on the first request, the process restarts if it crashes, and `HF_HOME` is set to `ModelsDir/huggingface`.
  - Public: `PingAsync`, `StopWorkerAsync`, `DisposeAsync` / `Dispose`.
  - Protocol types: `Python.Protocol.WorkerRequest`, `WorkerResponse`, `WorkerProtocol`. Client: `Python.Abstract.ITtsWorkerClient`, `Python.Base.TtsWorkerClientBase`, `Python.Concrete.ProcessTtsWorkerClient`.
- **Python engines** (`Concrete.Python.*`), one adapter file each under `PythonWorkers/adapters/`, with package names and Hugging Face repos taken from each model card. The keys match the catalog: Chatterbox (4096 MB VRAM, cloning), XttsV2 (cloning), F5TtsTurkish (`marduk-ra/F5-TTS-Turkish`), EmaLightning, Ahenk, AhenkCep, Trendyol (marked experimental), PiperTurkish (via the `piper-tts` Python package, not the Windows exe), MmsTurkish (`facebook/mms-tts-tur`), OmniVoice, VoxCpm2 (8192 MB), FishS2Pro (12288 MB), Kokoro (no Turkish).
- **Audio helpers:** `Audio.WavFile` (`WriteMono16`, `TryReadInfo`, `GetDurationSec`, `DurationSec`) and `Audio.WavInfo`.

There are no API routes or Web pages in this part, and I didn't touch any Web JavaScript.

**Tests** (`tests/TahaKonsept.Agent.Engines.Speech.Tests`): 29 passed, 0 skipped.
- Unit tests cover WAV writing and reading, protocol serialization and parsing, Supertonic text processing and chunking, the DI catalog (14 keys and their metadata), request checks, the missing-uv error, and the Hugging Face URL.
- **Supertonic live test:** it downloaded the 8 missing voice styles through the base class helper, then synthesized the Turkish sentence. The M1 voice gave 8.2 s of 44.1 kHz audio in about 7 s on CPU; F1 at speed 1.2 gave 6.8 s. Both files have normal speech activity. I checked only duration and loudness; nobody has listened to them yet. They are in `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/.tools-dev/speech/out/`.
- **Worker end-to-end test:** it uses a portable uv 0.13.8 in `.tools-dev/speech/uv` to create a venv, ping the worker, run two synths (the second at speed 2 comes out half as long), restart the worker and synthesize again.
- Both live tests skip themselves if the model or uv isn't present. `.tools-dev/` was already in the gitignore.

**What the integrator must wire or know**
- Agent.Core has to register `IAgentPaths`, `IToolLocator` (for Uv and Python) and `IProcessRunner` before calling `AddSpeechEngines()`.
- Only `ISpeechEngine` is registered, not `IAgentEngine`. Add that forwarding if the capability or setup screen lists engines through `IAgentEngine`.
- Ahenk and Ahenk Cep install from git URLs, so git must be installed on the user's machine.
- I took some package pins from memory rather than checking them: `transformers==4.56.2` for MMS, and the torch 2.8.0 pin used for most engines. XTTS also relies on the library's built-in speaker names. The Fish adapter downloads the fish-speech source and runs its `api_server.py` with arguments I didn't verify. Expect to fix some of this the first time each real engine is installed.
- Chatterbox has no speed control, and F5 without a reference clip falls back to an English sample voice.

Shared file edited: `TahaKonsept.slnx`, to add the two projects. I didn't edit `Directory.Packages.props`; every package I needed was already listed.