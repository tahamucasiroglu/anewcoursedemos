"""VoxCPM2 (OpenBMB, Apache-2.0, ~8 GB VRAM). pip: voxcpm. HF: openbmb/VoxCPM2. Zero-shot cloning via reference_wav_path."""

from adapters._common import write_wav


class VoxCpmAdapter:
    def __init__(self, model, device, options):
        from voxcpm import VoxCPM

        self.tts = VoxCPM.from_pretrained(model or "openbmb/VoxCPM2", load_denoiser=False)
        self.sample_rate = self.tts.tts_model.sample_rate
        self.cfg_value = float(options.get("cfg_value", 2.0))
        self.timesteps = int(options.get("inference_timesteps", 10))

    def synth(self, text, lang, voice, ref_audio, speed, out):
        kwargs = {"text": text, "cfg_value": self.cfg_value, "inference_timesteps": self.timesteps}
        if ref_audio:
            kwargs["reference_wav_path"] = ref_audio
        audio = self.tts.generate(**kwargs)
        return write_wav(out, audio, self.sample_rate)


def create(model, device, options):
    return VoxCpmAdapter(model, device, options)
