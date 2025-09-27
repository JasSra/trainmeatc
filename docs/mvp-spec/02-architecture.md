# 2. Architecture (Monolith)

UI: Blazor Server (.NET 8/9) with WebAudio capture + audio playback.
Realtime: SignalR hub for partial transcripts, scoring deltas, TTS stream events.
LLM Services: STT, Instructor, ATC, TTS via OpenAI APIs. Realtime (WebRTC) optional later.
DB: SQLite (WAL mode). Audio blob storage on disk under `/app_data/audio`.
Optional future: Realtime API (ephemeral tokens) for sub-300ms loop.
