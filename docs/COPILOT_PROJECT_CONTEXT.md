# Copilot Project Context Summary

Goal: Voice-only ATC comms training simulator (Australia focus) with Instructor + ATC bots, using OpenAI for STT, LLM logic, and TTS.

Key Directories:
- docs/prompts -> authoritative prompts (keep in sync)
- docs/db/schema.sql -> SQLite schema baseline
- backend/ -> Blazor Server solution (to be scaffolded)
- frontend/ -> UI Razor components (within same server project, but logical separation)

Service Interfaces (planned): ISttService, IInstructorService, IAtcService, ITtsService.

Non-negotiables:
- No API key leakage
- Structured JSON outputs validated
- Australian phraseology adherence

When generating code: ensure async, cancellation support, small cohesive methods, DI-friendly.
