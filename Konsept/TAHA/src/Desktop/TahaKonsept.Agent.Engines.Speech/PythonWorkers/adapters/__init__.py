"""Engine adapters for tts_worker.py.

Each module exposes ``create(model, device, options)`` returning an object with ``sample_rate`` and
``synth(text, lang, voice, ref_audio, speed, out) -> duration_seconds``.
"""
