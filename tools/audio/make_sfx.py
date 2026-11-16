"""Procedural SFX placeholders of GDD 12.7 / A14: 44.1 kHz mono 16-bit WAV, stdlib only, deterministic.

Run from the repo root:  python tools/audio/make_sfx.py
Writes Assets/_Project/Audio/SFX/sfx_*.wav (overwrites). Replace with final sounds later; keep the names.
"""
import math
import random
import struct
import wave
from pathlib import Path

RATE = 44100
OUT = Path(__file__).resolve().parents[2] / "Assets/_Project/Audio/SFX"


def env(t, attack, decay):
    """Fast attack, exponential decay."""
    return min(1.0, t / attack) * math.exp(-t / decay) if attack > 0 else math.exp(-t / decay)


def tone(seconds, freq, attack=0.005, decay=0.1, sweep=0.0, noise=0.0, rng=None):
    out, phase = [], 0.0
    for i in range(int(seconds * RATE)):
        t = i / RATE
        f = freq * (1.0 + sweep * t / seconds)
        phase += 2 * math.pi * f / RATE
        s = math.sin(phase) * (1 - noise) + (rng.uniform(-1, 1) * noise if noise else 0.0)
        out.append(s * env(t, attack, decay))
    return out


def mix(*parts):
    n = max(len(p) for p in parts)
    return [sum(p[i] for p in parts if i < len(p)) for i in range(n)]


def concat(*parts):
    return [s for p in parts for s in p]


def silence(seconds):
    return [0.0] * int(seconds * RATE)


def write(name, samples):
    peak = max(1e-6, max(abs(s) for s in samples))
    OUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUT / f"{name}.wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(s / peak * 0.8 * 32767)) for s in samples))


def main():
    rng = random.Random(18)
    write("sfx_bump", tone(0.15, 140, decay=0.04, sweep=-0.4))
    write("sfx_squish_bump", mix(tone(0.3, 220, decay=0.08, sweep=-0.6), tone(0.3, 0, decay=0.05, noise=1.0, rng=rng)))
    write("sfx_dash", tone(0.3, 800, attack=0.03, decay=0.1, noise=0.9, sweep=-0.5, rng=rng))
    write("sfx_jump", tone(0.35, 260, decay=0.15, sweep=1.5))
    # Charge for the whole 1.0 s telegraph, then the shot.
    write("sfx_cannon", concat(tone(1.0, 120, attack=0.9, decay=10.0, sweep=2.0), tone(0.35, 0, decay=0.08, noise=1.0, rng=rng)))
    write("sfx_ice_crack", concat(tone(0.08, 0, decay=0.02, noise=1.0, rng=rng), silence(0.1),
                                  tone(0.4, 0, decay=0.12, noise=1.0, rng=rng)))
    # Rising creak for 3 s, then a soft thump.
    write("sfx_ring_drop", concat(tone(3.0, 90, attack=2.5, decay=20.0, sweep=1.0, noise=0.3, rng=rng), tone(0.4, 60, decay=0.12)))
    write("sfx_obstacle_whoosh", tone(0.8, 300, attack=0.4, decay=0.3, noise=0.95, rng=rng))
    write("sfx_pusher_horn", mix(tone(0.5, 330, attack=0.02, decay=1.0), tone(0.5, 415, attack=0.02, decay=1.0)))
    write("sfx_fall_off", concat(tone(0.9, 1200, attack=0.05, decay=2.0, sweep=-0.8), tone(0.3, 0, decay=0.08, noise=1.0, rng=rng)))
    write("sfx_respawn", mix(tone(0.4, 880, decay=0.12, sweep=0.5), tone(0.4, 1320, decay=0.08)))
    jingle = [tone(0.25, f, decay=0.2) for f in (523, 659, 784, 1047)]
    write("sfx_win", concat(*jingle, tone(1.2, 1047, decay=0.5)))


if __name__ == "__main__":
    main()
