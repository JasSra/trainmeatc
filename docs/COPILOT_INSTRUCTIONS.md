# GitHub Copilot Instructions

Provide focused assistance for this project:

- Stack: .NET 9 (or 8 if 9 not GA yet), Blazor Server, C#, SQLite (via EF Core), SignalR, OpenAI SDK.
- Follow existing interfaces and prompts in `docs/prompts`.
- Prefer streaming patterns for STT and model responses.
- Do not expose `OPENAI_API_KEY` to client code; only server-side services may access it.
- Generate minimal, testable slices: add service interface, then stub implementation, then wire into DI and a razor page.
- Keep transmissions and prompt outputs JSON-valid when required.
- Use cancellation tokens in all async service calls.
- When adding new airports or frequencies, place authoritative source comment referencing AIP (do not hardcode speculative data).
- For EF Core migrations: name them with timestamp + concise purpose, e.g., `20250927_Init`.

When unsure: search the `docs/` directory before generating new patterns to avoid drift.
