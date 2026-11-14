"""EMA Lightning (Turkish only, Apache-2.0, ~34 MB). pip: ema-lightning. HF: canberkkkkkk/ema-lightning. Single voice, CPU friendly."""

from adapters._common import wav_duration


class EmaLightningAdapter:
    def __init__(self, model, device, options):
        from ema_lightning import EMA

        self.tts = EMA()
        if (device or "").startswith("cuda") and options.get("lightning", True):
            try:
                self.tts = self.tts.lightning()
            except Exception:  # noqa: BLE001 - the GPU speedup is optional
                pass
        self.sample_rate = 48000

    def synth(self, text, lang, voice, ref_audio, speed, out):
        speech = self.tts.say(text, path=out, speed=max(0.25, min(4.0, speed)))
        duration = getattr(speech, "duration", None)
        return float(duration) if duration is not None else wav_duration(out)


def create(model, device, options):
    return EmaLightningAdapter(model, device, options)
