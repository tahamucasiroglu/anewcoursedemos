"""Dummy adapter: writes a sine tone whose length follows the text length. Used to test the worker protocol end to end."""

from adapters._common import sine, write_wav


class DummyAdapter:
    """No model; 60 ms per character divided by speed, 440 Hz tone."""

    sample_rate = 16000

    def __init__(self, model, device, options):
        self.model = model
        self.device = device
        self.seconds_per_char = float(options.get("seconds_per_char", 0.06))

    def synth(self, text, lang, voice, ref_audio, speed, out):
        seconds = max(0.2, len(text) * self.seconds_per_char / max(0.1, speed))
        return write_wav(out, sine(seconds, self.sample_rate), self.sample_rate)


def create(model, device, options):
    return DummyAdapter(model, device, options)
