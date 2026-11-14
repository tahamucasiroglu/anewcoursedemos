"""Piper (ONNX VITS) Turkish voice tr_TR-dfki-medium (voice license CC BY-NC-SA). pip: piper-tts.

Voice files come from HF rhasspy/piper-voices (tr/tr_TR/dfki/medium/*.onnx + .onnx.json).
"""

import wave

from adapters._common import wav_duration


class PiperAdapter:
    def __init__(self, model, device, options):
        self.use_cuda = (device or "").startswith("cuda")
        self.default_name = model or "tr_TR-dfki-medium"
        self.voices = {}
        self.sample_rate = self._voice(self.default_name).config.sample_rate

    def _voice(self, name):
        """Load (and cache) a piper voice such as tr_TR-fahrettin-medium from rhasspy/piper-voices."""
        if name not in self.voices:
            from huggingface_hub import hf_hub_download
            from piper import PiperVoice

            lang_region, speaker, quality = name.split("-", 2)
            lang = lang_region.split("_")[0]
            base = f"{lang}/{lang_region}/{speaker}/{quality}/{name}"
            onnx_path = hf_hub_download("rhasspy/piper-voices", base + ".onnx")
            hf_hub_download("rhasspy/piper-voices", base + ".onnx.json")
            self.voices[name] = PiperVoice.load(onnx_path, use_cuda=self.use_cuda)
        return self.voices[name]

    def synth(self, text, lang, voice, ref_audio, speed, out):
        from piper import SynthesisConfig

        piper_voice = self._voice(voice or self.default_name)
        self.sample_rate = piper_voice.config.sample_rate
        config = SynthesisConfig(length_scale=1.0 / max(0.1, speed))
        with wave.open(out, "wb") as handle:
            piper_voice.synthesize_wav(text, handle, syn_config=config)
        return wav_duration(out)


def create(model, device, options):
    return PiperAdapter(model, device, options)
