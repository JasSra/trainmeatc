# Audio Capture & Playback Notes

- Use JS interop to access MediaDevices.getUserMedia with 16kHz mono processing through OfflineAudioContext resample if needed.
- Chunking: 1.5s PCM buffers encoded to WAV (PCM 16-bit LE) -> base64 -> POST /api/stt.
- Provide visual level meter (RMS) and disable send while retry loop active.
- Playback: HTMLAudioElement for TTS mp3/wav sources from /app_data/audio (served via static file mapping).
- Optional RF quality simulation: apply BiquadFilter (high-pass ~300Hz) + subtle pink noise gain 0.02 for 'noisy'.
