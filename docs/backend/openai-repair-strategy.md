# Structured Output Repair Strategy

When a Responses API call (Instructor or ATC) returns invalid JSON:
1. Attempt to parse; if failure, capture raw text.
2. Issue a secondary call with system prompt prefix: `Return ONLY valid JSON for the provided schema. Original response: <<<...>>>`.
3. If second attempt still invalid, fallback:
   - Instructor: treat as critical block (block_reason = phraseology) with generic improvement message.
   - ATC: return hold_short true with transmission: "Standby.".
4. Log incident (without key) for diagnostics.
