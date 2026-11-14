"""Shared helpers for adapters: GPU-first device resolution (with logged CPU fallback) and WAV writing.

GPU-first rule: when the agent asks for 'cuda', adapters must run on the GPU. CPU is only a fallback (torch without CUDA,
GPU not visible, out of VRAM, driver error) and every fallback is reported: a '[taha:warn]' line on stderr (the agent logs it
at Warning) plus device_note(), which tts_worker returns in the load response.
"""

import math
import struct
import sys
import wave

WARN_PREFIX = "[taha:warn]"

_device_note = None


def warn(message):
    """Write a warning line the agent picks up at Warning level (stdout is reserved for protocol lines)."""
    print(f"{WARN_PREFIX} {message}", file=sys.stderr, flush=True)


def device_note():
    """Reason of the last cuda -> cpu fallback in this process (None if there was none)."""
    return _device_note


def note_cpu_fallback(reason):
    """Record and report a cuda -> cpu fallback."""
    global _device_note
    _device_note = reason
    warn("GPU yerine CPU: " + reason)


def resolve_device(device):
    """Return the requested cuda device when torch actually sees a GPU; otherwise 'cpu' (the fallback is reported)."""
    wanted = (device or "cpu").lower()
    if not wanted.startswith("cuda"):
        return wanted
    try:
        import torch
    except ImportError:
        note_cpu_fallback("cuda istendi ama torch kurulu değil")
        return "cpu"
    if torch.cuda.is_available():
        return wanted
    if getattr(torch.version, "cuda", None) is None:
        note_cpu_fallback(f"cuda istendi ama torch {torch.__version__} CUDA'sız (CPU tekerleği) kurulmuş")
    else:
        note_cpu_fallback(f"cuda istendi ama torch {torch.__version__} (CUDA {torch.version.cuda}) GPU göremiyor (sürücü/GPU yok)")
    return "cpu"


def load_on_device(factory, device, what="model"):
    """Build something with factory(device); if that fails on the GPU (out of VRAM, driver error), report and retry on CPU.

    Returns (result, device actually used)."""
    device = resolve_device(device)
    if not device.startswith("cuda"):
        return factory(device), device
    try:
        return factory(device), device
    except Exception as exc:  # noqa: BLE001 - any GPU failure falls back to CPU, loudly
        _free_cuda_memory()
        note_cpu_fallback(f"{what} GPU'ya yüklenemedi ({type(exc).__name__}: {exc})")
        return factory("cpu"), "cpu"


def _free_cuda_memory():
    try:
        import torch

        if torch.cuda.is_available():
            torch.cuda.empty_cache()
    except Exception:  # noqa: BLE001 - best effort cleanup only
        pass


def to_mono_list(audio):
    """Convert a torch tensor / numpy array / list ([T], [1, T] or [C, T]) to a flat list of floats (first channel)."""
    if hasattr(audio, "detach"):
        audio = audio.detach().cpu().float().numpy()
    if hasattr(audio, "ndim"):
        if audio.ndim > 1:
            audio = audio.reshape(-1, audio.shape[-1])[0]
        return [float(x) for x in audio.tolist()]
    return [float(x) for x in audio]


def write_wav(out, samples, sample_rate):
    """Write mono float samples (-1..1) as 16-bit PCM WAV and return the duration in seconds."""
    samples = to_mono_list(samples)
    frames = bytearray()
    for value in samples:
        if value != value:  # NaN
            value = 0.0
        clamped = max(-1.0, min(1.0, value))
        frames += struct.pack("<h", int(round(clamped * 32767)))
    with wave.open(out, "wb") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(int(sample_rate))
        handle.writeframes(bytes(frames))
    return len(samples) / float(sample_rate)


def wav_duration(path):
    """Read the duration of a PCM WAV file in seconds."""
    with wave.open(path, "rb") as handle:
        return handle.getnframes() / float(handle.getframerate())


def sine(seconds, sample_rate, frequency=440.0, amplitude=0.3):
    """Generate a sine tone (used by the dummy adapter)."""
    count = max(1, int(seconds * sample_rate))
    return [amplitude * math.sin(2.0 * math.pi * frequency * i / sample_rate) for i in range(count)]
