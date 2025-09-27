# OpenAI Integration Notes

Services:
- STT: Audio Transcriptions (gpt-4o-mini-transcribe primary, whisper-1 fallback)
- Instructor: Responses API + structured output schema (see prompts)
- ATC: Responses API + structured output schema
- TTS: Speech API (gpt-4o-mini-tts)

General guidelines:
- Always pass shared system preamble.
- Use temperature: Instructor 0.3, ATC 0.4; set seed for reproducibility.
- Validate JSON against schema; retry once on failure with a repair prompt.
- Stream partial tokens where possible; push partialTranscript events.
- Do not log raw API key.

Error handling:
- Network / 5xx: exponential backoff (250ms, 500ms, 1s) max 3 attempts.
- JSON validation failure: attempt structured repair (include original response in a tool call repair message) then fallback to safe block.

Security:
- Only server holds OPENAI_API_KEY (IConfiguration / environment variable).
- If/when realtime added: server mints ephemeral tokens (short TTL) using official endpoint.
