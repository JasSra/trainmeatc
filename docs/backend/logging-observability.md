# Logging & Observability

- Use Serilog (console + rolling file) excluding sensitive data.
- Correlate per-session logs with session_id.
- Log key events: STT chunk received, Instructor verdict summary, ATC transmission JSON, TTS synth success, errors.
- Metrics (future): latency histograms per stage (STT, Instructor, ATC, TTS) exported via /metrics (Prometheus format).
