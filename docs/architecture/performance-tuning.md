# Performance Tuning Notes

- Use HttpClientFactory for OpenAI calls to avoid socket exhaustion.
- Enable Response Compression for SignalR JSON payloads.
- Consider binary protocol for transcripts (MessagePack) later.
- Warm up: pre-load first model call on startup to reduce user-perceived latency.
