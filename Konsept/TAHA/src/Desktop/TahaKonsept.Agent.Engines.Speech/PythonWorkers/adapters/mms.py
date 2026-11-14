"""Meta MMS-TTS Turkish (VITS, cc-by-nc-4.0). pip: transformers. HF: facebook/mms-tts-tur. CPU."""

from adapters._common import write_wav


class MmsAdapter:
    def __init__(self, model, device, options):
        import torch
        from transformers import AutoTokenizer, VitsModel

        repo = model or "facebook/mms-tts-tur"
        self.torch = torch
        self.model = VitsModel.from_pretrained(repo)
        self.tokenizer = AutoTokenizer.from_pretrained(repo)
        self.sample_rate = self.model.config.sampling_rate

    def synth(self, text, lang, voice, ref_audio, speed, out):
        self.model.speaking_rate = speed
        inputs = self.tokenizer(text, return_tensors="pt")
        with self.torch.no_grad():
            waveform = self.model(**inputs).waveform
        return write_wav(out, waveform[0], self.sample_rate)


def create(model, device, options):
    return MmsAdapter(model, device, options)
