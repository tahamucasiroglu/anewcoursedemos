"""Taha Konsept TTS worker.

Long-lived process driven by the desktop agent (PythonSpeechEngineBase) over a JSON-lines protocol:

  stdin  (one JSON object per line):
    {"cmd": "ping"}
    {"cmd": "load", "engine": "chatterbox", "model": "...", "device": "cuda", "options": {...}}
    {"cmd": "synth", "text": "...", "lang": "tr", "voice": null, "ref_audio": null, "speed": 1.0, "out": "C:/.../a.wav"}
    {"cmd": "quit"}

  stdout (exactly one JSON object per request line):
    {"ok": true, "pong": true}
    {"ok": true, "sample_rate": 24000, "device": "cuda"}            (load; "device_note" explains a cuda -> cpu fallback)
    {"ok": true, "out": "C:/.../a.wav", "duration": 3.21, "sample_rate": 24000}
    {"ok": false, "error": "message"}

Anything libraries print goes to stderr: the real stdout handle is reserved for protocol lines.
"""

import importlib
import json
import os
import re
import sys
import traceback

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

# Reserve the real stdout for protocol messages; everything else (library prints, progress bars) goes to stderr.
_PROTOCOL_OUT = sys.stdout
sys.stdout = sys.stderr

_ENGINE_NAME = re.compile(r"^[a-z0-9_]+$")


def _send(payload):
    _PROTOCOL_OUT.write(json.dumps(payload, ensure_ascii=False) + "\n")
    _PROTOCOL_OUT.flush()


def _load(request, state):
    engine = (request.get("engine") or "").strip().lower()
    if not _ENGINE_NAME.match(engine):
        raise ValueError("invalid engine name: %r" % engine)
    module = importlib.import_module("adapters." + engine)
    requested = request.get("device") or "cpu"
    adapter = module.create(request.get("model"), requested, request.get("options") or {})
    state["adapter"] = adapter
    state["engine"] = engine
    from adapters._common import device_note

    response = {"ok": True, "sample_rate": getattr(adapter, "sample_rate", None), "device": str(getattr(adapter, "device", requested))}
    note = device_note()
    if note:
        response["device_note"] = note
    return response


def _synth(request, state):
    adapter = state.get("adapter")
    if adapter is None:
        raise RuntimeError("no engine loaded; send a 'load' command first")
    out = request.get("out")
    text = request.get("text") or ""
    if not out:
        raise ValueError("'out' is required")
    if not text.strip():
        raise ValueError("'text' is empty")
    out_dir = os.path.dirname(os.path.abspath(out))
    os.makedirs(out_dir, exist_ok=True)
    duration = adapter.synth(
        text=text,
        lang=request.get("lang") or "tr",
        voice=request.get("voice"),
        ref_audio=request.get("ref_audio"),
        speed=float(request.get("speed") or 1.0),
        out=out,
    )
    return {"ok": True, "out": out, "duration": float(duration), "sample_rate": getattr(adapter, "sample_rate", None)}


def main():
    state = {}
    stdin = sys.stdin
    for raw in stdin:
        line = raw.strip()
        if not line:
            continue
        try:
            request = json.loads(line)
            cmd = request.get("cmd")
            if cmd == "ping":
                _send({"ok": True, "pong": True})
            elif cmd == "load":
                _send(_load(request, state))
            elif cmd == "synth":
                _send(_synth(request, state))
            elif cmd in ("quit", "exit"):
                _send({"ok": True})
                break
            else:
                _send({"ok": False, "error": "unknown cmd: %r" % cmd})
        except Exception as exc:  # noqa: BLE001 - every failure must become a protocol error line
            traceback.print_exc(file=sys.stderr)
            _send({"ok": False, "error": "%s: %s" % (type(exc).__name__, exc)})


if __name__ == "__main__":
    try:
        sys.stdin.reconfigure(encoding="utf-8")
        _PROTOCOL_OUT.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    main()
