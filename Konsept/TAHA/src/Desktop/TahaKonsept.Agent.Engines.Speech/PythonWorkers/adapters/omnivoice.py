"""OmniVoice (k2-fsa, 600+ languages, zero-shot cloning). pip: omnivoice. HF: k2-fsa/OmniVoice.

Without ref_audio, the voice is designed from an instruction (voice id or options.instruct).
"""

from adapters._common import resolve_device, write_wav


class OmniVoiceAdapter:
    def __init__(self, model, device, options):
        import torch
        from omnivoice import OmniVoice

        self.device = resolve_device(device)
        on_gpu = self.device.startswith("cuda")
        self.model = OmniVoice.from_pretrained(
            model or "k2-fsa/OmniVoice",
            device_map="cuda:0" if on_gpu else "cpu",
            dtype=torch.float16 if on_gpu else torch.float32,
        )
        self.sample_rate = int(getattr(self.model, "sampling_rate", 24000))
        self.instruct = options.get("instruct", "male, middle-aged, moderate pitch")

    def synth(self, text, lang, voice, ref_audio, speed, out):
        kwargs = {"text": text, "speed": speed}
        if ref_audio:
            kwargs["ref_audio"] = ref_audio
        else:
            kwargs["instruct"] = voice or self.instruct
        audio = self.model.generate(**kwargs)
        if isinstance(audio, (list, tuple)):
            audio = audio[0]
        return write_wav(out, audio, self.sample_rate)


def create(model, device, options):
    return OmniVoiceAdapter(model, device, options)
