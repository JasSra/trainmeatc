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
