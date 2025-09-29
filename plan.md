# TrainMeATC Product Delivery Plan

## 0. Executive Summary

TrainMeATC will deliver an adaptive, voice-driven ATC training simulator that provides real‑time feedback, dynamic scenarios, measurable performance metrics, and scalable multi-scenario content. This plan lays out phased execution from current prototype to a production-ready (PRD) GA system.

Target GA Window: 6–8 incremental releases (~Milestone-based).

Release Philosophy: Ship vertical slices that exercise the full turn loop, then deepen fidelity (scoring, content breadth, performance, resilience, compliance).

## 1. Product Vision & Outcomes

| Dimension | Goal | Measurable Outcome (KPI) |
|-----------|------|--------------------------|
| Learning Efficacy | Improve trainee phraseology accuracy | ≥30% improvement between first and fifth session (normalized score) |
| Engagement | Retain users through multi-session progression | >55% 7‑day return rate |
| Scenario Depth | Diverse operational contexts | ≥12 distinct scenario templates (VFR, IFR, Ground, Emergency, Tower) |
| Real-time Responsiveness | Low latency feedback loop | P95: STT partial < 700ms; Instructor verdict < 2.5s total; ATC reply < 1.2s |
| Reliability | Stable sessions | <1% session aborts due to system errors |
| Compliance & Safety | Appropriate content controls | Logged audit for 100% AI generated transmissions |
| Cost Efficiency | Sustainable per-session cost | <$0.15 per 5‑min session (model + infra) |

## 2. Foundational Principles

- Latency matters: Early streaming feedback outweighs perfect accuracy.
- Instructor clarity > model cleverness: deterministic rubric + recovery guidance.
- Every turn produces structured telemetry (scoring + errors) for analytics.
- Graceful degradation: If AI subsystem fails, fallback scripted branch.

## 3. High-Level Architecture Tracks

1. Core Simulation Loop (STT → Scoring → ATC → TTS → Score update)
2. Content & Scenario Authoring Pipeline
3. Real-time Comms: SignalR event layer & group/session isolation
4. AI Services Abstraction (STT / Instructor / ATC / TTS pluggable)
5. Persistence & Analytics (Session, Turn, Metrics, Errors)
6. Observability & Operations (Logging, Traces, Metrics, Alerting)
7. Platform Hardening (Security, Rate Limiting, Abuse Detection)
8. Frontend UX/Accessibility (PTT, transcripts, verdict coaching)

## 4. Phase Breakdown

### Phase 1: Stabilize MVP Core (Current → Solid Baseline)

Objectives:

- Correct SignalR payload schema usage.
- Implement full client turn state machine.
- Persist complete turn history (User STT, Verdict, ATC, ScoreDelta, Timings).
- Add mic + speaker test UX, retry flow, expected readback UI.

Deliverables:

- Updated `LiveSim.razor` + backing services.
- Turn model persisted (SessionTurns table + migration if needed).
- Basic instructor rubric & exemplar output.

Acceptance Criteria:

- A full session from start to end with ≥3 turns works without manual DB hacks.
- Score shown equals sum of deltas; blocked turns don’t count.

Exit Gate:

- Demo run recorded + latency sampled (manual log).

Status (2025-09-29): COMPLETED
- Turn persistence implemented with `turn` table extensions: timing metrics (stt_ms, instructor_ms, atc_ms, tts_ms, total_ms), score_delta, blocked flag, started_utc.
- Session score integrity: only non-blocked turn score deltas accumulated; SignalR `scoreTick` only emitted on score change.
- Client state machine integrated with blocking logic and partial transcript flow.
- Basic unit test (`SessionScoreEqualsSumOfNonBlockedTurns`) validates score accumulation logic.

### Phase 2: Structured Scoring & Feedback Depth

Objectives:

- Formalize scoring rubric mapping (phrase accuracy, ordering, omissions, safety).
- Expand InstructorVerdict to include category codes & severity levels.
- Introduce coaching hints & exemplar explanation.

Deliverables:

- Updated `InstructorVerdict` contract & UI panel.
- Score breakdown (hover / expandable card).
- Metrics: average normalized, retries, safety flags.

Acceptance Criteria:

- Two users produce reproducible verdicts for same transcript (±5% normalized variance).

Exit Gate:

- Rubric doc versioned in `/docs/backend/scoring-algorithm-notes.md`.

Status (2025-09-29): IN PROGRESS

- InstructorVerdict expanded with components + sub-scores (phraseAccuracy, ordering, omissions, safety, safetyFlag, rubricVersion)
- OpenAI instructor prompt updated to emit structured JSON; parser implemented
- VerdictDetail entity & migration added; per-component persistence operational
- Turn metrics now record normalized and sub-scores + safetyFlag
- LiveSim UI includes expandable breakdown table & sub-score badges
- Deterministic reproducibility test added (`ScoringReproTests`) for monotonic normalized lengths
- Rubric documentation added & versioned (v1) in `scoring-algorithm-notes.md`
- Remaining: Debrief aggregate summaries, timeline with audio, session replay, detailed performance metrics

### Phase 3: Scenario Authoring & Content Scale

Objectives:

- Scenario DSL or structured JSON (initial fields: airport, traffic seed, weather, difficulty tags, end conditions).
- Admin/import tool for scenarios with validation.
- Content lint: detect unreferenced states, unreachable branches.

Deliverables:

- `ScenarioDefinitions` storage + loader service.
- Authoring CLI or minimal web admin page.
- Validation pipeline (CI job) for new scenario submissions.

Acceptance Criteria:

- Add a new scenario via definition file → appears in UI without code changes.

Exit Gate:

- At least 5 varied scenarios live.

### Phase 4: Performance & Latency Optimization

Objectives:

- Introduce streaming partial STT end-to-end (already started, refine).
- Parallelize Instructor + future ATC prefetch where viable.
- Cache TTS for repeated fixed phrase templates.
- Add metrics instrumentation (OpenTelemetry) for each segment.

Deliverables:

- Dashboard (Grafana or Azure Monitor) with: STT latency, Instructor processing time, ATC generation time, TTS synthesis time, SignalR send time.
- P95 latencies meet provisional SLOs.

Acceptance Criteria:

- P95 targets: STT partial < 700ms, instructor verdict < 2.5s, ATC reply < 1.2s, TTS synth < 1.5s.

Exit Gate:

- Load test at 50 concurrent sessions within latency SLO.

### Phase 5: Reliability & Resilience

Objectives:

- Circuit breakers / retry policy around AI upstream.
- Fallback scripted ATC response on failure.
- Idle detection & auto-end with user notification.

Deliverables:

- Policy library (Polly) integrated.
- Fallback path unit/integration tests.
- Idle timer config in appsettings.

Acceptance Criteria:

- Injected failure tests pass (graceful degrade, no unhandled exceptions surfaced to client).

Exit Gate:

- Chaos test report (manual or automated) archived.

### Phase 6: Security, Compliance & Abuse Controls

Objectives:

- API key & rate limiting (per IP/session) for STT/AI endpoints.
- Content safety filter for user audio transcript (profanity, disallowed phrases) with soft/hard block.
- Audit log for every AI-generated ATC transmission.

Deliverables:

- Middleware for request rate limiting.
- `AuditLog` table + writer service.
- Content moderation integration (lightweight first, pluggable).

Acceptance Criteria:

- Attempted abusive transcript flagged & session continues with warning.

Exit Gate:

- Security review checklist passed.

### Phase 7: Analytics & Progression System

Objectives:

- Trainee profile: cumulative stats, skill progression tiers.
- Leaderboards (optional privacy-aware).
- Export CSV / JSON for instructor oversight.

Deliverables:

- `UserStats` aggregation job (nightly) or on-demand update.
- Progression badges & difficulty unlock logic.

Acceptance Criteria:

- After 5 sessions, user progression reflects improved normalized average if deltas trend up.

Exit Gate:

- Product review sign-off.

### Phase 8: Pre-GA Hardening & Observability Maturity

Objectives:

- Comprehensive logging correlation IDs (session, turn, user).
- Alerting thresholds (latency, error %, blocked turns spike).
- Synthetic canary session every 10 minutes.

Deliverables:

- Alert runbooks.
- Canary runner (CLI or Function).

Acceptance Criteria:

- MTTR estimation path proven in tabletop exercise.

Exit Gate:

- Go/No-Go checklist signed.

### Phase 9: GA Launch & Post-Launch Iteration

Objectives:

- Rollout staged (10% / 50% / 100%).
- Collect qualitative feedback inside app (NPS micro prompt after debrief).
- Post-launch tuning of scoring weights.

Deliverables:

- Feature flag config.
- Feedback submission endpoint & dashboard.

Acceptance Criteria:

- No Sev1 incidents in first 14 days; latency SLO maintained.

Exit Gate:

- Official GA announcement.

## 5. Cross-Cutting Workstreams

| Workstream | Description | Phases Touched |
|------------|-------------|----------------|
| DevEx | Local docker, test data seeding, Make targets | 1–3 |
| Testing Strategy | Unit, integration (SignalR loop), load, chaos | 1–8 |
| Documentation | Architecture, API, authoring guide, runbooks | All |
| Observability | Structured logs, tracing, metrics | 4–8 |
| Security | AuthN/Z, secrets management, rate limiting | 1,6,8 |
| Cost Optimization | Model selection, caching, batching | 4,5,9 |

## 6. Data Model Evolution Plan

| Stage | New Entities/Changes | Rationale |
|-------|----------------------|-----------|
| Phase 1 | SessionTurn (transcript, verdict JSON, atc JSON, timings) | Replay & analytics |
| Phase 2 | VerdictDetail (normalized components) | Detailed scoring breakdown |
| Phase 3 | ScenarioDefinition (JSON) | Authoring & dynamic loading |
| Phase 5 | AuditLog, FailureEvent | Reliability + compliance |
| Phase 7 | UserStats, ProgressionTier | Gamification & retention |
| Phase 8 | SyntheticSession | Canary health checks |

## 7. Testing Ladder

- Unit: Rubric scoring functions, fallback selection, TTS cache key.
- Integration: End-to-end simulated turn (mock AI services) < 3s.
- Contract: JSON schema validation on SignalR payloads.
- Load: 50–100 concurrent sessions sustaining for 15 minutes.
- Chaos: Inject upstream latency + failures (50% TTS failure scenario) ensuring fallback.
- UX: Keyboard accessibility (PTT key), screen-reader checks on transcript & feedback.

## 8. Metrics & SLOs

| Metric | SLI Definition | Target |
|--------|----------------|--------|
| STT Partial Latency | p95(ms) from audio start -> first partial | <700ms |
| Verdict Turnaround | p95 from final STT -> verdict event | <2500ms |
| ATC Reply Latency | p95 from verdict accepted -> ATC event | <1200ms |
| Session Error Rate | (# errored sessions / total) | <1% |
| Blocked Turn Ratio | blocked turns / total turns | <25% (healthy) |
| Idle Timeout Incidents | sessions ended by idle / total | <5% |
| Cost per Session | (AI+$ infra)/session | <$0.15 |

## 9. Risk Register (Excerpt)

| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|------------|
| Model latency spikes | Degraded UX | Medium | Multi-region, provider fallback, caching |
| STT accuracy low in noisy env | Learning frustration | High | Noise suppression, bias prompts, retry coaching |
| Cost overrun with scaling | Budget pressure | Medium | Tiered model usage, batching, pre-synth library |
| Data drift in scoring | Unfair feedback | Medium | Periodic rubric calibration set, regression tests |
| Abuse / inappropriate audio | Trust & safety issues | Medium | Moderation pipeline + escalation flags |
| Vendor dependency lock-in | Reduced flexibility | Low | Abstraction interfaces, dual vendor readiness |
| Unhandled concurrency (SignalR groups) | Cross-session leakage | Low | Strict group naming + tests |

## 10. Roles & Ownership (Sample)

| Area | Owner (TBD) |
|------|-------------|
| Core Turn Loop | Backend Lead |
| Frontend LiveSim UX | Frontend Engineer |
| AI Service Integrations | AI Systems Engineer |
| Observability & SRE | Platform Engineer |
| Security & Compliance | Security Engineer |
| Content Pipeline | Scenario Designer |

## 11. Release Governance

- Definition of Done requires: code + tests + docs + metrics instrumentation.
- Feature flags for any experimental AI model changes.
- CHANGELOG updates each release.
- Automated schema migration check in CI.

## 12. Tooling & Automation Backlog

- CLI: scenario validate, ingest, list.
- Script: synthetic session generator.
- GitHub Actions: contract test on SignalR payload schema.
- Load test harness (k6 / Locust) scripted scenario.

## 13. Open Questions

- Authentication model (anonymous vs account) for early phases?
- Multi-turn branching depth: cap or adaptive pruning?
- Instructor rubric maintainability: DSL vs encoded logic?
- Voice personalization (future) roadmap placement.

## 14. Phase Exit Checklist Template

For each phase produce:

- Updated docs (architecture + CHANGELOG).
- Test report (unit coverage delta + latency samples).
- Risk delta assessment.
- Go/No-Go decision record.

## 15. Appendices

A. Current Gaps vs Plan (see initial audit).  
B. Proposed Data Schemas (to be iteratively added).  
C. Latency Budget Breakdown Sheet (future).

---
This plan is a living document; update per phase completion with empirical data and refined targets.
