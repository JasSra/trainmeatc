# Scoring Algorithm Notes

Base: score_total starts at 0.
- Add Instructor verdict ScoreDelta on each accepted turn.
- Critical errors -> negative deltas and block progression.

Metrics:
- average_normalized = mean of normalized values for accepted turns
- retries = count of blocked turns preceding acceptance
- total_time = session end - start

Outcome resolution:
- Completed when scenario end condition reached (e.g., join downwind acknowledged)
- Timeout after configurable idle threshold (e.g., 90s no user audio)
- safety_block if repeated safety critical failures (>=3) without recovery

## Phase 2 Structured Rubric (Rubric Version v1)

Primary Dimensions (all 0.0–1.0 when present):

- PhraseAccuracy: Correct ICAO callsign usage, required phraseology tokens, readback completeness
- Ordering: Logical sequencing of information (callsign, intent, position, altitude, request)
- Omissions: Presence of required mandatory elements for context
- Safety: Potential safety or separation impact of errors

Derived Fields:

- normalized: Aggregated score (weighted sum of component scores; may exclude Safety if purely flag-based)
- scoreDelta: Integer delta applied to session score; sum of component deltas (rounded) unless SafetyFlag triggers override
- safetyFlag: True if any component with severity=critical in Safety category or cross-dimension hazard (e.g., wrong runway readback)

Component Object Schema:

```json
{
  "code": "PA_CALLSIGN",        // Unique short code
  "category": "PhraseAccuracy", // PhraseAccuracy|Ordering|Omissions|Safety
  "severity": "minor",          // info|minor|major|critical
  "weight": 0.25,                // Contribution weight (0-1, sum <=1 for non-safety comps)
  "score": 0.8,                  // Raw component performance 0-1
  "delta": 3,                    // Signed contribution to overall integer ScoreDelta
  "detail": "Correct prefix; minor phonetics lapses"
}
```

Severity Guidance:

- info: Stylistic; no score penalty (delta 0 to +1)
- minor: Minor phraseology lapse; small positive or zero delta if mostly correct (-1 to +2)
- major: Material omission or incorrect ordering affecting clarity (-3 to +1)
- critical: Safety or fundamental breakdown; usually negative (-5 to -10) and may set safetyFlag

Example Component Codes:

- PhraseAccuracy: PA_CALLSIGN, PA_PREFIX, PA_RUNWAY, PA_QNH
- Ordering: OR_SEQUENCE, OR_POSITION_FIRST, OR_INTENT_PLACEMENT
- Omissions: OM_ALT, OM_POSITION, OM_REQUEST, OM_READBACK
- Safety: SF_WRONG_RUNWAY, SF_MISREAD_ALT, SF_CONFLICTING_CLEARANCE

Aggregation (example reference algorithm – model may approximate):

```text
normalized = sum(weight_i * score_i for non-safety components)
scoreDelta = round(sum(delta_i)) with clamp [-15, +15]
if safetyFlag and scoreDelta > 0 => scoreDelta = 0 (cannot reward when safety compromised)
```

Blocked Turn Logic:

- A turn is blocked when BlockReason non-empty AND at least one critical issue or safetyFlag true.
- Blocked turns do not modify session ScoreTotal.
- Metrics still capture timing and normalized value (if provided) for analysis.

Metrics Added Per Turn (Phase 2):

- turn.normalized
- turn.phraseAccuracy
- turn.ordering
- turn.omissions
- turn.safety
- turn.safetyFlag (1 or 0)

Future Considerations:

- Introduce rubricVersion bump for expanded categories (e.g., Brevity, ReadbackFidelity)
- Deterministic fallback scoring path for offline mode/testing
- Calibration harness comparing model outputs to gold rubric examples
