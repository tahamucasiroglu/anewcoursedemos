"""Piper (ONNX VITS) Turkish voices, e.g. tr_TR-dfki-medium (voice license CC BY-NC-SA). pip: piper-tts.

Voice files are looked up in order (first source that has the files wins):
  1. HF rhasspy/piper-voices   tr/tr_TR/<speaker>/<quality>/<name>.onnx + .onnx.json   (official; dfki)
  2. HF speaches-ai/piper-<name>   model.onnx + config.json                            (mirror of the original release;
     fahrettin and fettah are no longer in rhasspy/piper-voices main, which returns 404 for them)
"""

import wave

from adapters._common import wav_duration


def voice_sources(name):
    """Return the (repo, onnx file, config file) candidates for a voice name such as tr_TR-fahrettin-medium."""
    lang_region, speaker, quality = name.split("-", 2)
    lang = lang_region.split("_")[0]
    base = f"{lang}/{lang_region}/{speaker}/{quality}/{name}"
    return [
        ("rhasspy/piper-voices", base + ".onnx", base + ".onnx.json"),
        (f"speaches-ai/piper-{name}", "model.onnx", "config.json"),
    ]


def _download(name):
    """Download a voice from the first source that has it; return (onnx path, config path)."""
    from huggingface_hub import hf_hub_download
    from huggingface_hub.errors import EntryNotFoundError, RepositoryNotFoundError

    missing = []
    for repo, onnx_file, config_file in voice_sources(name):
        try:
            onnx_path = hf_hub_download(repo, onnx_file)
            config_path = hf_hub_download(repo, config_file)
            return onnx_path, config_path
        except (EntryNotFoundError, RepositoryNotFoundError) as exc:
            missing.append(f"{repo}/{onnx_file}: {type(exc).__name__}")
    raise FileNotFoundError(f"Piper voice '{name}' was not found: " + "; ".join(missing))


class PiperAdapter:
    def __init__(self, model, device, options):
        self.use_cuda = (device or "").startswith("cuda")
        self.default_name = model or "tr_TR-dfki-medium"
        self.voices = {}
        self.sample_rate = self._voice(self.default_name).config.sample_rate

    def _voice(self, name):
        """Load (and cache) a piper voice such as tr_TR-fahrettin-medium."""
        if name not in self.voices:
            from piper import PiperVoice

            onnx_path, config_path = _download(name)
            self.voices[name] = PiperVoice.load(onnx_path, config_path=config_path, use_cuda=self.use_cuda)
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
