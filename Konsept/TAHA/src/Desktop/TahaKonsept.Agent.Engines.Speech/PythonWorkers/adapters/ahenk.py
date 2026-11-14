"""Ahenk Turkish TTS (weights CC BY 4.0, github.com/ozguradmin/ahenk-turkish-tts).

model == "ahenk-cep": small student model, HF ozgurguler/ahenk-cep-tr (needs ema-lightning==1.1.2 + the ahenk repo package);
  GPU first (cuda), CPU only as a reported fallback.
otherwise: Ahenk (VoxCPM2 2B + LoRA), HF ozgurguler/ahenk-tts-tr, GPU; text is normalized with trnorm. voice = emotion.
"""

import json
import sys
from pathlib import Path

from adapters._common import load_on_device, write_wav


class AhenkCepAdapter:
    def __init__(self, device, options):
        from huggingface_hub import snapshot_download

        repo_dir = snapshot_download("ozgurguler/ahenk-cep-tr", allow_patterns=["*.pt", "ahenk_cep.py"])
        if repo_dir not in sys.path:
            sys.path.insert(0, repo_dir)
        from ahenk_cep import AhenkCep

        self.tts, self.device = load_on_device(
            lambda dev: AhenkCep(f"{repo_dir}/student.pt", f"{repo_dir}/decoder.pt", device=dev), device, "Ahenk Cep"
        )
        self.sample_rate = None

    def synth(self, text, lang, voice, ref_audio, speed, out):
        audio, sr = self.tts.say(text)
        self.sample_rate = sr
        return write_wav(out, audio, sr)


class AhenkAdapter:
    def __init__(self, options):
        from huggingface_hub import snapshot_download
        from voxcpm import VoxCPM
        from voxcpm.model.voxcpm import LoRAConfig

        repo_dir = snapshot_download(
            "ozgurguler/ahenk-tts-tr", allow_patterns=["lora_*", "*.py", "instructions_tr.json"]
        )
        if repo_dir not in sys.path:
            sys.path.insert(0, repo_dir)
        from instruction_pick import compose, pick
        from pause_clean import clean

        self._compose, self._pick, self._clean = compose, pick, clean
        lora_cfg = json.loads(Path(repo_dir, "lora_config.json").read_text(encoding="utf-8"))["lora_config"]
        self.tts = VoxCPM.from_pretrained(
            "openbmb/VoxCPM2",
            load_denoiser=False,
            optimize=False,
            lora_config=LoRAConfig(**lora_cfg),
            lora_weights_path=repo_dir,
        )
        self.sample_rate = self.tts.tts_model.sample_rate
        self.emotion = options.get("emotion", "")

    def synth(self, text, lang, voice, ref_audio, speed, out):
        from trnorm import normalize

        emotion = self.emotion if voice in (None, "", "neutral", "default") else voice
        instruction, model_text, _ = self._pick(emotion, normalize(text), "auto")
        wav = self.tts.generate(
            text=self._compose(instruction, model_text),
            cfg_value=2.0,
            inference_timesteps=16,
            normalize=False,
        )
        return write_wav(out, self._clean(wav, self.sample_rate)[0], self.sample_rate)


def create(model, device, options):
    if (model or "").lower() == "ahenk-cep":
        return AhenkCepAdapter(device, options)
    return AhenkAdapter(options)
