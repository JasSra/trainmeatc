# Backend Segment

Blazor Server app (monolith) providing:
- REST endpoints for STT, Instructor, ATC, TTS, Sessions, State
- SignalR hub for realtime updates
- EF Core (SQLite) data layer
- OpenAI service integrations (STT, Responses, TTS)

## Current Status: ✅ MILESTONE 1 COMPLETE

### Implemented Projects:
- ✅ `PilotSim.Server` (main Host) - Blazor Server with API controllers
- ✅ `PilotSim.Core` (domain models + services interfaces) 
- ✅ `PilotSim.Data` (EF Core DbContext, migrations)
- ✅ `PilotSim.Tests` (unit tests)

### Features Completed:
- ✅ OpenAI integrations for all services (Whisper STT, GPT-4o-mini for Instructor/ATC, TTS-1)
- ✅ Full turn-based simulation loop with real-time SignalR updates
- ✅ Comprehensive API surface with all required endpoints
- ✅ Database schema with sample data (Melbourne Airport)
- ✅ Error handling and fallback to stub services when no API key

### API Endpoints:
- `POST /api/stt` - Speech-to-text transcription
- `POST /api/instructor` - Instructor scoring and feedback
- `POST /api/atc` - ATC response generation
- `POST /api/tts` - Text-to-speech synthesis
- `POST /api/session` - Session start/end management
- `GET /api/state/{sessionId}` - Session state retrieval
- `POST /api/simulation/turn` - **NEW** Complete turn processing pipeline
- `GET /api/simulation/session/{sessionId}/scenario` - **NEW** Scenario information

### SignalR Hub:
- `/hubs/live` with real-time events:
  - `partialTranscript` - Live transcription updates
  - `instructorVerdict` - Instructor feedback
  - `atcTransmission` - ATC responses
  - `ttsReady` - Audio synthesis completion
  - `scoreTick` - Score updates

### Next Steps (Milestone 2):
- Score aggregation display
- Timeline with audio + transcripts
- Debrief functionality
