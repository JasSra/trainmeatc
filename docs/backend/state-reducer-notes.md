# State Reducer Notes

The ATC reply includes `next_state` patch fields. Apply reducer logic:

Inputs:
- Prior state object (immutable snapshot)
- AtcReply.NextState (partial)

Process:
1. Validate altitude/heading ranges (altitude 0..12000 ft for VFR training scope; heading 0..359).
2. Merge non-null fields.
3. Append sequencing / approach changes to a state log for Debrief timeline.
4. Persist serialized JSON with turn record.

Edge Cases:
- hold_short true: do not mutate state except maybe a retry counter.
- missing fields: keep previous values.
