Below is a **complete author‑tool spec**. No code. Compact. Deterministic. SI in data.

---

## 1) Goals

1. Let authors build **ScenarioWorkbookV2** quickly via GUI + AI assists.
2. Support **ATC** and **CTAF** flows, branches, traffic, gates, tolerance, completion rules.
3. Enforce **CASA‑aligned** mandatory items.
4. Export **valid, versioned JSON** plus STT bias list.

---

## 2) Roles

1. **Author**: creates and edits scenarios.
2. **Reviewer**: locks, approves, tags for training track.
3. **Admin**: manages lookups, slot patterns, phrase libraries.

---

## 3) UX flow (wizard)

1. **Select family**: taxi | departure | arrival/approach | CTAF inbound/join/circuit | transit | mixed | emergency.
2. **Inputs**: `{ icao, aircraft, start_utc, time_of_day, difficulty, variability, weather_source, notam_source }`.
3. **Resolve context**: auto‑fill lat/lon, tower_active, freqs, runway_in_use, weather(SI), ATIS text, NOTAM, traffic snapshot.
4. **Objectives**: pick observable goals from templates or add custom.
5. **Phases**: add rows → set `primary_freq`, `required_components` (ATC) or `broadcast_components` (CTAF), cues, tips.
6. **Branches**: add probabilistic branches with guards and effects; preview tree graph.
7. **Gates**: choose `block_on_missing` and `warn_on_missing` from slot library; add safety gates.
8. **Tolerance**: synonyms, phonetics, edit distance, timing window.
9. **Traffic**: add actors, intents, conflict timings ≤90 s.
10. **Completion**: success/fail rules and end phase.
11. **STT bias**: auto‑generate checklist for model priming.
12. **QA**: run validator; fix red flags.
13. **Publish**: version, tag, export JSON, change log.

---

## 4) Screens

1. **Dashboard**: drafts, approved, by family, by ICAO.
2. **Scenario Wizard**: 12 steps above with progress bar.
3. **Phase Graph**: left‑to‑right DAG, branches with p%.
4. **Traffic Map**: 2D plan view; positions in km; circuit altitude in m AGL.
5. **Gates & Slots**: checklist with CASA mandatory toggles.
6. **Simulator Preview**: synthetic turns; shows timeline and gate hits.
7. **JSON View**: read‑only diff; export.

---

## 5) Authoring data model (tool‑side, not runtime)

**Entities**

* `ScenarioDraft`

  * `id`, `title`, `family`, `icao`, `aircraft`, `start_utc`, `time_of_day`, `difficulty`, `variability`, `weather_source`, `notam_source`, `status`, `version`, `author`, `reviewer`, `notes`
  * `context_resolved` (AirportCtx, WeatherSI, freqs, runway_in_use, ATIS text, NOTAMs)
  * `objectives[]` (key, description, metric)
  * `phases[]` (PhaseDraft)
  * `rubric` (RubricSpec)
  * `tolerance` (ToleranceSpec)
  * `global_safety_gates[]` (SafetyGate)
  * `completion` (CompletionSpec)
  * `stt_bias[]` (string)
* `PhaseDraft`

  * `id`, `name`, `primary_freq_mhz`, `pilot_cue`, `controller_policy`
  * `required_components[]` (ATC)
  * `broadcast_components[]` (CTAF)
  * `entry_criteria[]` (Criterion)
  * `common_errors[]`, `coaching_tips[]`
  * `safety_gates[]` (SafetyGate)
  * `responder_map` (ResponderMap)
  * `next_state` (NextState)
  * `branches[]` (BranchSpec)
* `TrafficDraft`

  * `actors[]` (callsign, type, wake, lat_deg, lon_deg, alt_m_msl, gs_mps, intent)
  * `conflicts[]` (with_callsign, event, time_to_conflict_s)

**Storage**

* Drafts in DB as JSONB or text.
* Published assets in object storage with semantic version `v{major.minor.patch}`.

---

## 6) Lookup tables (admin editable)

1. **Families**: `taxi, clearance, departure, approach, landing, ctaf_inbound, ctaf_circuit, transit, mixed, emergency`.
2. **Phases by family** (templates): e.g., taxi: `pre_start, taxi, taxi_progress, hold_short`.
3. **Slots ATC**: `RUNWAY, HOLDING_POINT, ROUTE_TAXI, QNH, SQUAWK, FREQ, LEVEL, HEADING, SPEED, APPROACH_CLR, VACATE, CONDITIONAL`.
4. **Slots CTAF**: `LOCATION_TRAFFIC, TYPE, CALLSIGN, POSITION, LEVEL_m_MSL, INTENTIONS, LOCATION_REPEAT`.
5. **Slot regex** (author can override):

   * `RUNWAY: \b\d{2}[LRC]?\b`
   * `HOLDING_POINT: \b[A-Z][0-9]?\b`
   * `ROUTE_TAXI: \b([A-Z][0-9]?)(,? ?| -> )+`
   * `QNH: \b(QNH )?\d{3,4}\b`
   * `FREQ: \b\d{3}\.\d\b`
   * `SQUAWK: \b\d{4}\b`
   * `LEVEL_m_MSL: \b\d{2,4}\b`
6. **Risk levels**: `Low, Medium, High, Critical`.
7. **Gate catalog**: `RUNWAY_ENTRY_WITHOUT_CLR, WRONG_FREQ, HP_CROSS, STRAIGHT_IN_LATE, OVERHEAD_WRONG_SIDE`.
8. **Persona**: `concise, normal, high_workload, urgent`.
9. **Difficulty profiles**: Easy, Medium, Hard → strictness, congestion, variability.
10. **Weather presets**: VMC, Marginal, IMC; wind range; QNH range; cloud base m AGL.
11. **Traffic intents**: `hold_short, vacate_rwy, final, base, crosswind, downwind, approach, taxi_out, transit`.
12. **Branch library**: the 66 situations mapped to default guards/effects.

---

## 7) AI assistants (prompted)

1. **Context Resolver Agent**

   * Input: `{icao, start_utc, weather_source, notam_source}`.
   * Output: `AirportCtx, runway_in_use, weather_si, ATIS text, NOTAM summaries`.
   * System rule: SI in data, RTF in ATIS only.

2. **Phase Generator Agent**

   * Input: `{family, objectives, context_resolved, difficulty, variability}`.
   * Output: phases with `required_components` or `broadcast_components`, cues, `responder_map`, `safety_gates`, `next_state`.
   * Rule: ATC phases must include CASA mandatory readbacks; CTAF phases must have no clearances.

3. **Branch Builder Agent**

   * Input: `{phase_id, branch_situation_code, guards, p}`.
   * Output: `BranchSpec` with optional `atc_override`.
   * Rule: sum of branch p ≤ 1.

4. **Traffic Synth Agent**

   * Input: `{actors, runway, phase, conflict_window_s}`.
   * Output: conflicts array; CTAF broadcast samples.

5. **Rubric Composer Agent**

   * Input: slot selections + gates.
   * Output: `RubricSpec` + `ReadbackPolicy` with `block_on_missing` and `warn_on_missing`.

6. **STT Bias Agent**

   * Input: `{icao, runways, taxiways, freqs, qnh, squawks}`.
   * Output: newline keyword list.

7. **QA Agent**

   * Input: entire workbook.
   * Output: issues list + fixes.

**All agents return JSON only.**

---

## 8) Validation rules (hard)

1. Branch probabilities per phase: `Σp ≤ 1`.
2. `next_state.phase_id` exists.
3. ATC phase has **no** `broadcast_components`; CTAF phase has **no** `required_components`.
4. `block_on_missing ⊆ mandatory_components`.
5. All slots in gates exist in `slot_definitions`.
6. At least one **end** path reachable.
7. Traffic conflicts `0 < time_to_conflict_s ≤ 90`.
8. SI only in data: wind m/s, vis km, altitudes m, QNH hPa.
9. Frequencies match `FREQ` pattern.
10. Tower inactive ⇒ responder cannot be ATC.

---

## 9) Generation pipeline

1. **Draft init**: family + inputs.
2. **Resolve**: context, traffic, weather.
3. **Auto‑seed**: phase template by family.
4. **Author edits**: phases, branches, gates, tolerance.
5. **Compose rubric**: from slots and gates.
6. **QA**: run validator + agent fix proposals.
7. **Preview**: dry‑run turn loop with synthetic student.
8. **Publish**: freeze JSON, version, changelog, hash.
9. **Distribute**: push to training catalogue.

---

## 10) Export format

* `InitialStateJson`: full ScenarioWorkbookV2 payload minus rubric/tolerance if stored separately.
* `RubricJson`: RubricSpec + ReadbackPolicy + Timing.
* `Meta`: `{id, version, author, review_date_utc}`.
* `Manifest`: `{id, version, family, icao, aircraft, checksum_sha256}`.

---

## 11) Author actions per step (GUI)

1. **Family**: radio buttons.
2. **Inputs**: form; `time_of_day` picklist; UTC picker; difficulty slider → shows strictness, congestion, variability.
3. **Resolve**: “Generate” button uses Context Agent. Manual override allowed.
4. **Objectives**: checklist + custom text.
5. **Phases**: add row; choose ATC or CTAF; slot checkboxes; freq picker; cue text.
6. **Branches**: add row; pick from library; set p and guard; live tree preview.
7. **Gates**: tick mandatory, move to block or warn.
8. **Tolerance**: sliders and token fields.
9. **Traffic**: table + map; add actors, draw positions; auto conflict compute.
10. **Completion**: add criteria using LHS/op/RHS builder.
11. **STT bias**: generate → copy or export `.txt`.
12. **QA**: run; show errors in panel; one‑click fix proposals.
13. **Publish**: version bump, reviewer assign, export.

---

## 12) Example authoring session (CTAF: join downwind)

1. Family: `ctaf_inbound`.
2. Inputs: `{icao:YMMB, aircraft:C172, start_utc:{{ PLACEHOLDER }}, time_of_day:day, difficulty:Easy, variability:0.3, weather_source:sim}`.
3. Resolve: runway 17L, pattern alt 305 m AGL, CTAF 118.1 MHz, wind 160/4 m/s, QNH 1016 hPa.
4. Objectives: `all CTAF broadcasts present at inbound/downwind/base/final`.
5. Phases: `inbound_18km`, `overhead`, `pre_downwind`, `downwind`, `base`, `final`, `vacate`, `clear`. Each with `broadcast_components = [LOC, TYPE, CS, POS, LVL_m_MSL, INT, LOC2]`.
6. Branch: `long_final_traffic` p=0.2; `late_straight_in_block` p=0.1.
7. Gates: `STRAIGHT_IN_LATE` = block.
8. Tolerance: synonyms `[["decimal","point"]]`, edit distance 2.
9. Traffic: add VH‑XYZ on final; conflict at 60 s.
10. Completion: success when `phase=end` and `gate.critical_count=0`.
11. STT bias: auto list.
12. QA: pass.
13. Publish: v2.1.0.

---

## 13) Metrics captured per scenario (for review)

1. Coverage %, latency s, overlap count.
2. Safety gate trips by code.
3. Conflict avoidance events.
4. Turn count, branch path distribution.
5. Time to complete min.

---

## 14) Security and versioning

1. Role‑based: Author/Reviewer/Admin.
2. Version immutability post‑approval.
3. Audit trail on each field change.
4. Digital signature on export manifest.

---

## 15) Templates shipped (starter set)

* **ATC**: Basic taxi, IFR clearance, vectors, ILS, go‑around, landing, vacate.
* **CTAF**: Inbound 18 km, overhead join, downwind, final, go‑around, vacate, clear.
* **Mixed**: Controlled‑to‑CTAF transition.
* **Emergency**: PAN enroute, MAYDAY on climb‑out.

---

## 16) Top lookups pre‑filled (starter values)

1. **Synonyms**: `hold short↔holding point`, `decimal↔point`, `line up↔line up and wait`.
2. **Phonetics**: `A: alpha|alfa`, `N: november`.
3. **Frequencies**: common AU CTAF 126.7 MHz, tower/ground per major aerodromes.
4. **Runways**: per ICAO, with magnetic hdg and length m.
5. **Pattern altitudes**: 300 m AGL default GA.

---

## 17) Output checklist (must pass before Publish)

1. Branch Σp ≤ 1 per phase.
2. End phase reachable.
3. ATC vs CTAF components separated.
4. CASA mandatory slots in `block_on_missing`.
5. SI only in data.
6. Traffic conflicts within 90 s window when used.
7. STT bias generated.
8. Version set and author/reviewer recorded.

---

## 18) Minimal prompts (copy/paste)

**Context Resolver (SYSTEM)**
“Return JSON for Australian aerodrome context. SI in data. Include AirportCtx, tower_active, freqs, runway_in_use, weather_si, ATIS text, NOTAM summaries.”

**Phase Generator (SYSTEM)**
“Given family and objectives, output phases with required/broadcast components, responder_map, safety_gates, next_state. No clearances on CTAF.”

**Branch Builder (SYSTEM)**
“Return BranchSpec for code {{ PLACEHOLDER }} with p, guards, effects, optional atc_override. Ensure Σp ≤ 1.”

**Rubric Composer (SYSTEM)**
“Compose ReadbackPolicy. Put CASA‑mandatory items in block_on_missing. Add slot_definitions with regex.”

**Traffic Synth (SYSTEM)**
“Return actors and conflicts ≤90 s plus one CTAF sample call for each actor on likely phase.”

**QA Agent (SYSTEM)**
“Validate workbook JSON. Return issues[] with path, severity, fix.”

---

Deliverable: use this plan to implement a **GUI wizard + AI assists** that emits **ScenarioWorkbookV2** JSON, a rubric JSON, and a bias list, with strict validation and CASA‑aligned gates.
