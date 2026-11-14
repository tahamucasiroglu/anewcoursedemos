"""Kokoro-82M (hexgrad, Apache-2.0). pip: kokoro (+ misaki). HF: hexgrad/Kokoro-82M. No Turkish.

lang -> KPipeline lang_code: en a (US) / en-gb b, es e, fr f, hi h, it i, ja j, pt p, zh z.
"""

from adapters._common import load_on_device, resolve_device, write_wav

_LANG_CODES = {"en": "a", "en-us": "a", "en-gb": "b", "es": "e", "fr": "f", "hi": "h", "it": "i", "ja": "j", "pt": "p", "zh": "z"}
_DEFAULT_VOICES = {
    "a": "af_heart", "b": "bf_emma", "e": "ef_dora", "f": "ff_siwis", "h": "hf_alpha",
    "i": "if_sara", "j": "jf_alpha", "p": "pf_dora", "z": "zf_xiaobei",
}


class KokoroAdapter:
    sample_rate = 24000

    def __init__(self, model, device, options):
        self.repo = model or "hexgrad/Kokoro-82M"
        self.device = resolve_device(device)
        self.pipelines = {}

    def _pipeline(self, code):
        if code not in self.pipelines:
            from kokoro import KPipeline

            # GPU-first: the pipeline (model) goes to cuda; out of VRAM / driver error -> reported CPU fallback.
            pipeline, self.device = load_on_device(
                lambda dev: KPipeline(lang_code=code, repo_id=self.repo, device=dev), self.device, "Kokoro"
            )
            self.pipelines[code] = pipeline
        return self.pipelines[code]

    def synth(self, text, lang, voice, ref_audio, speed, out):
        code = _LANG_CODES.get((lang or "en").lower())
        if code is None:
            raise ValueError("Kokoro does not support language %r" % lang)
        samples = []
        for _, _, audio in self._pipeline(code)(text, voice=voice or _DEFAULT_VOICES[code], speed=speed):
            if audio is not None:
                samples.extend(float(x) for x in audio.tolist())
        return write_wav(out, samples, self.sample_rate)


def create(model, device, options):
    return KokoroAdapter(model, device, options)
