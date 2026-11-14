"""XTTS-v2 (Coqui, maintained idiap fork). pip: coqui-tts. Model: tts_models/multilingual/multi-dataset/xtts_v2 (HF coqui/XTTS-v2).

License CPML (non-commercial). COQUI_TOS_AGREED=1 is needed for unattended download.
"""

import os

from adapters._common import resolve_device, wav_duration


class XttsAdapter:
    DEFAULT_SPEAKER = "Ana Florence"

    def __init__(self, model, device, options):
        os.environ.setdefault("COQUI_TOS_AGREED", "1")
        from TTS.api import TTS

        self.device = resolve_device(device)
        self.tts = TTS(model or "tts_models/multilingual/multi-dataset/xtts_v2").to(self.device)
        self.sample_rate = 24000

    def synth(self, text, lang, voice, ref_audio, speed, out):
        kwargs = {"text": text, "language": lang or "tr", "file_path": out, "speed": speed}
        if ref_audio:
            kwargs["speaker_wav"] = ref_audio
        else:
            kwargs["speaker"] = voice or self.DEFAULT_SPEAKER
        self.tts.tts_to_file(**kwargs)
        return wav_duration(out)


def create(model, device, options):
    return XttsAdapter(model, device, options)
