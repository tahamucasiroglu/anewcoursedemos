"""F5-TTS with the Turkish fine-tune marduk-ra/F5-TTS-Turkish (cc-by-nc-4.0). pip: f5-tts.

F5 always clones: it needs a reference clip and its transcript. Without ref_audio the library's bundled English
example clip is used (accent will be off); give options.ref_text together with a custom ref_audio for best results.
"""

from adapters._common import resolve_device, wav_duration


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
            from importlib.resources import files

            ref_file = str(files("f5_tts").joinpath("infer/examples/basic/basic_ref_en.wav"))
            ref_text = "Some call me nature, others call me mother nature."
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
