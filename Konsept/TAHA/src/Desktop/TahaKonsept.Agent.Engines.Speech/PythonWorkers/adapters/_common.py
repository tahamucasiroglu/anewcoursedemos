"""Shared helpers for adapters: device resolution and WAV writing."""

import math
import struct
import wave


def resolve_device(device):
    """Return 'cuda' only when requested and torch actually sees a GPU; otherwise 'cpu'."""
    wanted = (device or "cpu").lower()
    if wanted.startswith("cuda"):
        try:
            import torch

            if torch.cuda.is_available():
                return wanted
        except ImportError:
            pass
        return "cpu"
    return wanted


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
