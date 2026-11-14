"""Fish Audio S2 Pro (FISH AUDIO RESEARCH LICENSE, ~12-24 GB VRAM). Code: github.com/fishaudio/fish-speech, HF: fishaudio/s2-pro.

fish-speech has no stable in-process Python API, so this adapter starts its HTTP server (tools/api_server.py from the
fish-speech checkout given by options.repo_dir) on a local port and calls POST /v1/tts.
"""

import base64
import json
import os
import subprocess
import sys
import time
import urllib.request

from adapters._common import wav_duration


class FishAdapter:
    def __init__(self, model, device, options):
        from huggingface_hub import snapshot_download

        self.repo_dir = options.get("repo_dir")
        if not self.repo_dir or not os.path.isdir(self.repo_dir):
            raise RuntimeError("options.repo_dir must point to a fish-speech checkout")
        checkpoint = snapshot_download(model or "fishaudio/s2-pro")
        self.port = int(options.get("port", 7139))
        self.url = "http://127.0.0.1:%d" % self.port
        self.sample_rate = 44100
        self.server = subprocess.Popen(
            [
                sys.executable, "tools/api_server.py",
                "--listen", "127.0.0.1:%d" % self.port,
                "--llama-checkpoint-path", checkpoint,
                "--decoder-checkpoint-path", os.path.join(checkpoint, "codec.pth"),
                "--device", "cuda" if (device or "").startswith("cuda") else "cpu",
            ],
            cwd=self.repo_dir,
            stdout=sys.stderr,
            stderr=sys.stderr,
        )
        self._wait_ready(float(options.get("startup_timeout", 900)))

    def _wait_ready(self, timeout):
        deadline = time.time() + timeout
        while time.time() < deadline:
            if self.server.poll() is not None:
                raise RuntimeError("fish-speech api_server exited with code %s" % self.server.returncode)
            try:
                with urllib.request.urlopen(self.url + "/v1/health", timeout=5):
                    return
            except Exception:  # noqa: BLE001 - server is still starting
                time.sleep(3)
        raise TimeoutError("fish-speech api_server did not become ready")

    def synth(self, text, lang, voice, ref_audio, speed, out):
        payload = {"text": text, "format": "wav", "references": []}
        if ref_audio:
            with open(ref_audio, "rb") as handle:
                payload["references"].append({"audio": base64.b64encode(handle.read()).decode("ascii"), "text": ""})
        request = urllib.request.Request(
            self.url + "/v1/tts",
            data=json.dumps(payload).encode("utf-8"),
            headers={"Content-Type": "application/json"},
        )
        with urllib.request.urlopen(request, timeout=1800) as response, open(out, "wb") as target:
            target.write(response.read())
        return wav_duration(out)


def create(model, device, options):
    return FishAdapter(model, device, options)
