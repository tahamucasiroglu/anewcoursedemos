"""F5-TTS with the Turkish fine-tune marduk-ra/F5-TTS-Turkish (cc-by-nc-4.0). pip: f5-tts.

F5 always clones: it needs a reference clip and its transcript, and it copies the clip's accent and pace. Without ref_audio
the bundled Turkish clip assets/f5_ref_tr.wav (6.7 s, Supertonic 3 voice M1) and its transcript are used; the library's English
example clip made Turkish narration slow (~44 s instead of ~31 s for the e2e story) and accented. Give options.ref_text together
with a custom ref_audio for best results (empty ref_text = F5 transcribes the clip itself).
"""

import os

from adapters._common import resolve_device, wav_duration

DEFAULT_REF_AUDIO = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "assets", "f5_ref_tr.wav")
DEFAULT_REF_TEXT = "Henüz yirmi bir yaşındaki İkinci Mehmed, dev Şahi toplarıyla surları haftalarca dövdü."


def default_reference():
    """Return (ref_file, ref_text) used when no reference clip is given: the bundled Turkish clip, else the library's English one."""
    if os.path.isfile(DEFAULT_REF_AUDIO):
        return DEFAULT_REF_AUDIO, DEFAULT_REF_TEXT
    from importlib.resources import files

    return str(files("f5_tts").joinpath("infer/examples/basic/basic_ref_en.wav")), "Some call me nature, others call me mother nature."


class F5TtsAdapter:
    def __init__(self, model, device, options):
        from f5_tts.api import F5TTS
        from huggingface_hub import hf_hub_download

        repo = model or "marduk-ra/F5-TTS-Turkish"
        ckpt = hf_hub_download(repo, options.get("ckpt_file", "f5_tts_turkish_1000000.safetensors"))
        vocab = hf_hub_download(repo, options.get("vocab_file", "vocab.txt"))
        self.device = resolve_device(device)
        self.f5 = F5TTS(model="F5TTS_Base", ckpt_file=ckpt, vocab_file=vocab, device=self.device)
        self.sample_rate = 24000
        self.nfe_step = int(options.get("nfe_step", 64))
        self.ref_text = options.get("ref_text", "")

    def synth(self, text, lang, voice, ref_audio, speed, out):
        ref_file = ref_audio
        ref_text = self.ref_text if ref_audio else ""
        if not ref_file:
            ref_file, ref_text = default_reference()
        self.f5.infer(
            ref_file=ref_file,
            ref_text=ref_text,
            gen_text=text,
            file_wave=out,
            speed=speed,
            nfe_step=self.nfe_step,
        )
        return wav_duration(out)


def create(model, device, options):
    return F5TtsAdapter(model, device, options)
