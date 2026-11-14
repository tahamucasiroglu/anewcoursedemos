I built the local image engines. Both projects build with 0 warnings and all 40 tests pass. One of those tests is a real Z-Image-Turbo generation at 512x768 on the RTX 3060 Laptop. That run took about 25 s for the actual generation (about 27 s of wall time) and saved a PNG of the right size and quality to `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/.tools-dev/out/engine-test.png`. Everything is in commit `5298fdd`.

**Integrator: two things to wire up**
- **Required services:** before calling `services.AddImageEngines()`, the agent host must register `IAgentPaths` and `IProcessRunner`. These come from Agent.Core, which I don't own; without them the engines can't be built.
- **Engine list:** the engines are registered as `IImageEngine` and `ILocalImageEngine`, but not as `IAgentEngine`. If the setup screen lists engines through `IAgentEngine`, add that registration or enumerate `IImageEngine` directly.

**Projects**
- `src/Desktop/TahaKonsept.Agent.Engines.Image`, which references only Agent.Abstractions, Microsoft.Extensions.Http and Microsoft.Extensions.Options.
- `tests/TahaKonsept.Agent.Engines.Image.Tests`.
- The only shared file I changed is `TahaKonsept.slnx`, where I added both projects.
- There are no API routes, Web pages, JS, DbContext or migrations; this part is desktop-only.

**What other modules will use**
- `TahaKonsept.Agent.Engines.Image.DependencyInjection.ImageEngineServiceCollectionExtensions.AddImageEngines(this IServiceCollection services, Action<ImageEngineOptions>? configure = null)`
  - Registers both engines as `IImageEngine` and `ILocalImageEngine`, in this order: sd.cpp first, ComfyUI second.
  - `IToolLocator` is optional.
- `TahaKonsept.Agent.Engines.Image.Abstract.ILocalImageEngine : IImageEngine` adds two members for the setup screen:
  - `bool IsModelInstalled(string modelKey)`
  - `Task<Result> EnsureModelAsync(string modelKey, IProgress<EngineSetupProgress>? progress, CancellationToken ct)`
- `TahaKonsept.Agent.Engines.Image.Options.ImageEngineOptions` holds the settings:
  - Folders: models go to `ModelsDir/image` and the sd.cpp binary goes to `ToolsDir/sd-cpp`.
  - Backend choice `SdCppBackend`: `Auto`, `Cuda`, `Vulkan` or `Cpu`.
  - `LowVram`, default on.
  - `AutoDownloadModels`, default on: if model files are missing at generation time they are downloaded first.
  - `DefaultModelKey = "z-image-turbo"`.
  - `GenerationTimeout`, 15 minutes.
  - `ComfyUiBaseUrl = http://127.0.0.1:8188`.
  - `HuggingFaceToken`, only needed for gated repos.
- Helper interfaces:
  - `Catalog.Abstract.IImageModelCatalog`
  - `Downloads.Abstract.IModelFileDownloader`
  - `StableDiffusionCpp.Abstract.ISdCppInstaller`: `FindExecutable()`, `EnsureInstalledAsync(...)`, `ResolveBackend()`, `SelectAssets(...)`
  - `StableDiffusionCpp.Abstract.ISdCppArgumentBuilder`
  - `ComfyUi.Abstract.IComfyUiClient`
  - `ComfyUi.Abstract.IComfyUiWorkflowProvider`
  - `Gpu.Abstract.IGpuCapabilityProbe`

**The engines**
- **`ImageEngineBase`**
  - Checks each request: the model is supported, width and height are 64–4096 and a multiple of 8, the prompt isn't empty, and the output file is .png, .jpg or .webp.
  - Turns unexpected exceptions into a `Result` instead of throwing.
  - Downloads a model's files with combined progress, resuming through a `.part` file and HTTP Range, with retries and size and SHA-256 checks.
- **`StableDiffusionCppImageEngine`** (key `stable-diffusion-cpp`)
  - Gets the latest release from the GitHub API and picks the build: CUDA 12 plus the separate CUDA runtime zip when an NVIDIA driver is present, otherwise Vulkan, otherwise CPU.
  - Runs `sd-cli.exe` once per image. Current releases call it `sd-cli.exe`, not `sd.exe`; the old name is still accepted.
  - On failure, the error includes the last 30 lines of sd.cpp output.
  - Low-VRAM flags are `--offload-to-cpu --flash-attn auto,diffusion=on --vae-tiling`. `--diffusion-fa` is deprecated in the current build, so I used its replacement.
- **Model catalog** (`Catalog/image-models.json`, embedded): every file has its Hugging Face repo, size and SHA-256. Total download per model:

| Model key | Main weights (GGUF) | Total download |
|---|---|---|
| `z-image-turbo` | Q3_K | 5.6 GB |
| `flux2-klein-4b` | Q4_0 | 4.9 GB |
| `flux2-klein-9b` | Q4_0 | 10.4 GB |
| `flux3-klein` | Q4_K | 6.0 GB |
| `sd35-medium` | Q4_0 | 5.4 GB |
| `sdxl-lightning` | Q4_0 | 2.7 GB |
| `qwen-image` | Q4_0 | 15.6 GB |

- The VAE files come from ungated Comfy-Org mirrors, so no Hugging Face token is needed.
- **`ComfyUiImageEngine`** (key `comfyui`)
  - Sends a built-in workflow to `/prompt`, polls `/history` until it finishes, then downloads the image from `/view`.
  - Has templates for `z-image-turbo` and `flux2-klein-4b`.
  - `IsInstalled()` means the server answered; the result is cached for 15 seconds.
  - It doesn't download models; the user's ComfyUI keeps them in its own `models` folder.

**Tests (40 passed, 0 skipped)**
- Catalog integrity.
- Argument building, including Turkish culture (so decimals are written `4.5`, not `4,5`).
- Choosing the right download for each backend, and a full install against a fake GitHub.
- Downloader: resume after a dropped connection, a server that ignores Range, skipping an existing file, hash mismatch, and the gated-repo message.
- The sd.cpp engine with a fake process: auto-download, error mapping, validation and progress.
- The ComfyUI engine against a fake server: success, execution error, rejected workflow, and reachability.
- DI registration.
- Real-binary tests: `--help` and the 512x768 generation. These skip if `.tools-dev/sd-cpp/sd-cli.exe` or the model files are missing, and they never download anything.
- The sd.cpp CUDA build and the Z-Image files (about 6 GB) are in `C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Konsept/TAHA/.tools-dev/`, which is gitignored.

**Not verified**
- **ComfyUI workflows:** I couldn't test either template against a real ComfyUI, because none is running here. They follow the official API format, but the node and file names I assumed may not match a given install: `EmptyFlux2LatentImage`, `CLIPLoader` types `lumina2` and `flux2`, and `z_image_turbo_bf16.safetensors`.
- **Models other than Z-Image:** for the other six models, the command-line flags come from the sd.cpp docs; only Z-Image was actually run.
- **Licences:** the report's licence notes still apply. FLUX.2 klein 9B is non-commercial, and FLUX.3 Klein should not be offered until its licence has been read. The catalog shows the licence for each model, but nothing enforces it.