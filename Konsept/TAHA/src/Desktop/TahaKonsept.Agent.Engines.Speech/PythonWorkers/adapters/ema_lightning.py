"""EMA Lightning (Turkish only, Apache-2.0, ~34 MB). pip: ema-lightning. HF: canberkkkkkk/ema-lightning. Single voice.

GPU first: the model is built on cuda (plus the optional CUDA-graph "lightning" speedup); CPU only as a reported fallback.
"""

from adapters._common import load_on_device, warn, wav_duration


class EmaLightningAdapter:
    def __init__(self, model, device, options):
        from ema_lightning import EMA

        self.tts, self.device = load_on_device(lambda dev: EMA(device=dev), device, "EMA Lightning")
        if self.device.startswith("cuda") and options.get("lightning", True):
            try:
                self.tts = self.tts.lightning()
            except Exception as exc:  # noqa: BLE001 - the CUDA-graph speedup is optional; the model stays on the GPU
                warn(f"EMA Lightning CUDA grafikleri kurulamadı, düz GPU yolu kullanılıyor ({type(exc).__name__}: {exc})")
        self.sample_rate = 48000

    def synth(self, text, lang, voice, ref_audio, speed, out):
        speech = self.tts.say(text, path=out, speed=max(0.25, min(4.0, speed)))
        duration = getattr(speech, "duration", None)
        return float(duration) if duration is not None else wav_duration(out)


def create(model, device, options):
    return EmaLightningAdapter(model, device, options)
