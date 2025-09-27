# Latency Targets

Per 5s speech chunk:
- STT: <600ms
- Instructor: <400ms
- ATC: <600ms
- TTS: <400ms

Streaming Goals:
- partialTranscript events >= 2 Hz

Mitigations:
- Parallelize Instructor request prep while final STT words arriving.
- Cache frequent TTS phrases (greetings, acknowledgements).
- Use seeded randomness to avoid re-generation drift causing cache misses.
