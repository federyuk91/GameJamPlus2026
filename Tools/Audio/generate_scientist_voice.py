"""Generate the replaceable, synthetic gibberish syllables. No external libraries."""
from pathlib import Path
import math
import struct
import wave

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Assets/Sounds/SFX/Scientist'
RATE = 22050
SYLLABLES = {
    'A': (730, 1090, 2440),
    'E': (530, 1840, 2480),
    'I': (270, 2290, 3010),
    'O': (570, 840, 2410),
    'U': (300, 870, 2240),
    'Mm': (250, 1000, 2100),
    'Buh': (480, 1100, 2300),
    'Duh': (500, 1500, 2500),
}


def generate(name, formants):
    count = int(RATE * 0.072)
    values = []
    phase = 0.0
    for index in range(count):
        time = index / RATE
        progress = index / (count - 1)
        fundamental = 142 * (1.06 - 0.15 * progress)
        phase += 2 * math.pi * fundamental / RATE
        sample = 0.0
        for harmonic in range(1, 52):
            frequency = harmonic * fundamental
            resonance = sum(
                weight * math.exp(-0.5 * ((frequency - center) / bandwidth) ** 2)
                for center, bandwidth, weight in zip(formants, (100, 150, 200), (1, 0.65, 0.3))
            )
            sample += math.sin(harmonic * phase) * (0.025 + resonance) / harmonic ** 0.7
        envelope = min(1, time / 0.008) * min(1, (count / RATE - time) / 0.018)
        if name in ('Buh', 'Duh'):
            envelope *= min(1, time / 0.018)
        values.append(sample * envelope)
    peak = max(abs(value) for value in values)
    pcm = b''.join(struct.pack('<h', round(value / peak * 0.65 * 32767)) for value in values)
    with wave.open(str(OUTPUT / f'Scientist_{name}.wav'), 'wb') as audio:
        audio.setnchannels(1)
        audio.setsampwidth(2)
        audio.setframerate(RATE)
        audio.writeframes(pcm)


if __name__ == '__main__':
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, formants in SYLLABLES.items():
        generate(name, formants)
