"""Trendyol-TTS (Turkish only, experimental): VoxCPM2 with a merged Turkish LoRA. HF: Trendyol/Trendyol-TTS. pip: voxcpm. GPU."""

from adapters._common import write_wav


class TrendyolAdapter:
    def __init__(self, model, device, options):
        from voxcpm.core import VoxCPM

        self.tts = VoxCPM.from_pretrained(
            hf_model_id=model or "Trendyol/Trendyol-TTS", load_denoiser=False, optimize=False
        )
        self.sample_rate = self.tts.tts_model.sample_rate
        self.cfg_value = float(options.get("cfg_value", 2.0))
        self.timesteps = int(options.get("inference_timesteps", 16))

    def synth(self, text, lang, voice, ref_audio, speed, out):
        audio = self.tts.generate(
            text=text,
            cfg_value=self.cfg_value,
            inference_timesteps=self.timesteps,
            max_len=4096,
            normalize=True,
            denoise=False,
        )
        return write_wav(out, audio, self.sample_rate)


def create(model, device, options):
    return TrendyolAdapter(model, device, options)
