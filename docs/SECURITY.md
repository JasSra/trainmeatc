# Security Notes

- API key isolation: only server environment variable `OPENAI_API_KEY`.
- Planned rate limits: STT (20 req/min/IP), ATC (15 req/min/IP).
- Audio upload size max: 2.5MB per chunk.
- Purge: implement /api/admin/purge (auth TBD) to delete old audio + sessions.
- No dynamic frequency generation; ensure prompts never request unseeded frequencies.
