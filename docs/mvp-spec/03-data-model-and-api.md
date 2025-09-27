# 3. Data Model & API Surface

Includes SQLite schema (see `../db/schema.sql`) and REST + SignalR endpoints.

API (internal):
- POST /api/stt
- POST /api/instructor
- POST /api/atc
- POST /api/tts
- POST /api/session (start/end semantics)
- GET /api/state/{sessionId}
SignalR Hub: /hubs/live with events: partialTranscript, instructorVerdict, atcTransmission, ttsReady, scoreTick.
