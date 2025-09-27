# Testing Strategy

Test Layers:
- Unit: service helpers (e.g., JSON schema validation, scoring normalizer)
- Integration: OpenAI call wrappers (behind interfaces) using test doubles
- Functional: Turn loop (in-memory DB + mocked OpenAI) ensuring retry gating

Key Cases:
1. Missing QNH -> Instructor critical -> block ATC
2. Wrong runway readback -> critical list populated
3. Persona fast + noisy RF -> still returns intelligible ATC transmission
4. Handoff only when next_state indicates readiness
5. Silence / timeout -> outcome set to timeout

Tooling:
- xUnit
- FluentAssertions
- Bogus (for generating sample transcripts) if needed

Determinism:
- Use seeded values for any randomized scenario elements.
