# High-Level Architecture

Blazor Server Host
- Razor Pages + Components
- SignalR Hub (LiveHub)
- REST Controllers (STT, Instructor, ATC, TTS, Sessions, State)
- EF Core (SQLite) + Repositories
- OpenAI Integration Services (STT, Responses, TTS)

Clients connect via websocket for hub events; audio upload via REST.
