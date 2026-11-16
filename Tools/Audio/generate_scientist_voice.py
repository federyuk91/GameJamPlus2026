"""Generate the replaceable, synthetic gibberish syllables. No external libraries."""
import argparse
from pathlib import Path
import math
import random
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


def generate(name, formants, duration=0.072, onset_formants=None):
    count = int(RATE * duration)
    values = []
    phase = 0.0
    for index in range(count):
        time = index / RATE
        progress = index / (count - 1)
        fundamental = 142 * (1.06 - 0.15 * progress)
        phase += 2 * math.pi * fundamental / RATE
        centers = formants
        if onset_formants is not None:
            glide = min(1.0, max(0.0, (time - 0.008) / 0.036))
            glide = glide * glide * (3 - 2 * glide)
            centers = tuple(start + (end - start) * glide for start, end in zip(onset_formants, formants))
        sample = 0.0
        for harmonic in range(1, 52):
            frequency = harmonic * fundamental
            resonance = sum(
                weight * math.exp(-0.5 * ((frequency - center) / bandwidth) ** 2)
                for center, bandwidth, weight in zip(centers, (100, 150, 200), (1, 0.65, 0.3))
            )
            sample += math.sin(harmonic * phase) * (0.025 + resonance) / harmonic ** 0.7
        envelope = min(1, time / 0.008) * min(1, (count / RATE - time) / 0.018)
        if name in ('Buh', 'Duh'):
            envelope *= min(1, time / 0.018)
        values.append(sample * envelope)
    write_wav(name, values)


def generate_breath(name, duration, low_hz, high_hz, seed):
    noise = random.Random(seed)
    count = round(RATE * duration)
    low_band = 0.0
    high_band = 0.0
    low_mix = 1 - math.exp(-2 * math.pi * low_hz / RATE)
    high_mix = 1 - math.exp(-2 * math.pi * high_hz / RATE)
    values = []
    for index in range(count):
        sample = noise.uniform(-1, 1)
        low_band += low_mix * (sample - low_band)
        high_band += high_mix * (sample - high_band)
        envelope = math.sin(math.pi * index / (count - 1)) ** 2
        values.append((high_band - low_band) * envelope)
    write_wav(name, values)


def write_wav(name, values):
    peak = max(abs(value) for value in values)
    pcm = b''.join(struct.pack('<h', round(value / peak * 0.65 * 32767)) for value in values)
    with wave.open(str(OUTPUT / f'Scientist_{name}.wav'), 'wb') as audio:
        audio.setnchannels(1)
        audio.setsampwidth(2)
        audio.setframerate(RATE)
        audio.writeframes(pcm)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--combinations-only', action='store_true')
    options = parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    if not options.combinations_only:
        for name, formants in SYLLABLES.items():
            generate(name, formants)
    generate_breath('Sh', 0.055, 900, 3300, 31)
    generate_breath('Th', 0.043, 400, 1500, 47)
    generate('We', SYLLABLES['E'], duration=0.09, onset_formants=(280, 650, 2200))
