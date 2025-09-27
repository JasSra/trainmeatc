# Backend Segment

Blazor Server app (monolith) providing:
- REST endpoints for STT, Instructor, ATC, TTS, Sessions, State
- SignalR hub for realtime updates
- EF Core (SQLite) data layer
- OpenAI service integrations (STT, Responses, TTS)

Planned projects:
- `PilotSim.Server` (main Host)
- `PilotSim.Core` (domain models + services interfaces)
- `PilotSim.Data` (EF Core DbContext, migrations)
- `PilotSim.Tests` (unit tests)
