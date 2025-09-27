# Error Codes (Planned)

| Code | HTTP | Meaning |
|------|------|---------|
| STT_RATE_LIMIT | 429 | Too many STT requests |
| ATC_RATE_LIMIT | 429 | Too many ATC requests |
| AUDIO_TOO_LARGE | 413 | Uploaded audio chunk exceeds max size |
| JSON_SCHEMA_FAIL | 500 | Model output invalid after repair attempts |
| OPENAI_UPSTREAM | 502 | Upstream OpenAI temporary failure |
| SESSION_NOT_FOUND | 404 | Session id invalid |
| STATE_BLOCKED | 409 | Action blocked due to Instructor verdict |
