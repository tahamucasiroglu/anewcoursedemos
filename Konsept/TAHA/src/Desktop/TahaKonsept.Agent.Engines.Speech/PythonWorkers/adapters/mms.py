"""Meta MMS-TTS Turkish (VITS, cc-by-nc-4.0). pip: transformers. HF: facebook/mms-tts-tur. GPU first (cuda), CPU fallback.

The tokenizer vocabulary is lowercase letters only (no punctuation) and it lowercases with Python's str.lower(), which is wrong
for Turkish ('I' -> 'i' instead of 'ı', 'İ' -> 'i' + combining dot). Without punctuation the model also reads a whole scene as one
breathless run. So this adapter (1) lowercases the Turkish way and (2) synthesizes clause by clause, inserting a short pause after
commas and a longer one after sentence ends.
"""

import re

from adapters._common import load_on_device, to_mono_list, write_wav

# Pause after a chunk, by the punctuation that ended it (seconds).
_PAUSES = {",": 0.15, ";": 0.2, ":": 0.2, ".": 0.35, "!": 0.35, "?": 0.35}
_CHUNK = re.compile(r"[^,;:.!?]+[,;:.!?]*")


def turkish_lower(text):
    """Lowercase with Turkish rules: I -> ı, İ -> i (str.lower() alone gets both wrong)."""
    return text.replace("I", "ı").replace("İ", "i").lower()


def split_chunks(text):
    """Split text into (chunk, pause seconds) pairs at punctuation; chunks without letters are dropped."""
    chunks = []
    for match in _CHUNK.finditer(text):
        raw = match.group(0)
        body = raw.rstrip(",;:.!? ").strip()
        if not any(ch.isalnum() for ch in body):
            continue
        tail = raw.rstrip()[-1:] if raw.rstrip() else ""
        chunks.append((body, _PAUSES.get(tail, 0.0)))
    return chunks


class MmsAdapter:
    def __init__(self, model, device, options):
        import torch
        from transformers import AutoTokenizer, VitsModel

        repo = model or "facebook/mms-tts-tur"
        self.torch = torch
        self.model, self.device = load_on_device(lambda dev: VitsModel.from_pretrained(repo).to(dev).eval(), device, "MMS-TTS")
        self.tokenizer = AutoTokenizer.from_pretrained(repo)
        self.sample_rate = self.model.config.sampling_rate

    def _synth_chunk(self, text):
        inputs = self.tokenizer(text, return_tensors="pt")
        if inputs["input_ids"].shape[-1] == 0:
            return []
        inputs = inputs.to(self.device)
        with self.torch.no_grad():
            waveform = self.model(**inputs).waveform
        return to_mono_list(waveform[0])

    def synth(self, text, lang, voice, ref_audio, speed, out):
        self.model.speaking_rate = speed
        samples = []
        chunks = split_chunks(turkish_lower(text)) or [(turkish_lower(text), 0.0)]
        for index, (chunk, pause) in enumerate(chunks):
            samples.extend(self._synth_chunk(chunk))
            if index < len(chunks) - 1 and pause > 0:
                samples.extend([0.0] * int(pause * self.sample_rate))
        return write_wav(out, samples, self.sample_rate)


def create(model, device, options):
    return MmsAdapter(model, device, options)
