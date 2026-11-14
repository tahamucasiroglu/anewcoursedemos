"""Chatterbox Multilingual (Resemble AI, MIT). pip: chatterbox-tts. HF: ResembleAI/chatterbox. Zero-shot cloning via ref_audio.

Output carries the Perth (implicit) watermark added by the library.
"""

from adapters._common import resolve_device, write_wav


class ChatterboxAdapter:
    def __init__(self, model, device, options):
        from chatterbox.mtl_tts import ChatterboxMultilingualTTS

        self.device = resolve_device(device)
        self.model = ChatterboxMultilingualTTS.from_pretrained(device=self.device)
        self.sample_rate = self.model.sr
        self.exaggeration = float(options.get("exaggeration", 0.5))
        self.cfg_weight = float(options.get("cfg_weight", 0.5))
        self.temperature = float(options.get("temperature", 0.8))

    def synth(self, text, lang, voice, ref_audio, speed, out):
        kwargs = {
            "language_id": lang or "tr",
            "exaggeration": self.exaggeration,
            "cfg_weight": self.cfg_weight,
            "temperature": self.temperature,
        }
        if ref_audio:
            kwargs["audio_prompt_path"] = ref_audio
        wav = self.model.generate(text, **kwargs)
        # Chatterbox has no speed control; the composer applies tempo (atempo) when needed.
        return write_wav(out, wav, self.sample_rate)


def create(model, device, options):
    return ChatterboxAdapter(model, device, options)
