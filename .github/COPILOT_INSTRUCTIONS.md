# Repo-Specific Copilot Instructions

Primary Goals:
- Implement Blazor Server ATC training simulator per docs/.

Guidelines:
1. Reuse service interfaces in `docs/backend/service-interfaces.cs` until proper projects exist.
2. Generate strongly-typed records for JSON schemas (InstructorVerdict, AtcReply) and ensure System.Text.Json attributes if needed.
3. Always add CancellationToken parameters.
4. Keep methods small; prefer pure helpers for scoring & state reduction.
5. For new prompts, update docs/prompts and reference them in code comments.
6. Avoid leaking API keys or returning raw exceptions to clients.
7. When adding external libraries, update README and justify.

Output Style:
- For model outputs requiring JSON: ensure exact schema compliance.
