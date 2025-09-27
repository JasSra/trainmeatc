# Fuzzy Readback Matcher (Future Improvement)

Approach:
- Tokenize expected_readback items and user transcript.
- Use normalized Levenshtein or Sørensen-Dice coefficient for similarity.
- Thresholds:
  - Critical if <0.6 similarity on mandatory items (runway, QNH, altitude, callsign)
  - Minor warning if 0.6..0.8

Potential libs:
- Implement small custom (avoid heavy deps) or use `FuzzySharp` if acceptable.

Note: Accent / STT errors - allow phonetic approximations (kilo vs kelo) by custom replacement map before compare.
