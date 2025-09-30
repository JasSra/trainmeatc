# TurnService Architecture

## Overview

The TurnService is a comprehensive turn processing service that implements ScenarioWorkbookV2 support for advanced ATC training scenarios. It provides sophisticated phase management, branching scenarios, traffic management, and comprehensive instructor feedback.

## Architecture

### Two Processing Paths

The system now supports two processing paths for backward compatibility and future extensibility:

#### 1. Legacy Path (Backward Compatible)
```
LiveSim.razor → /api/simulation/turn → SimulationController
  → STT (Speech-to-Text)
  → InstructorService (scoring)
  → OpenAiAtcService (ATC response)
  → TTS (Text-to-Speech)
  → Persist turn data
```

**Use case:** Simple scenarios with basic state management, single-phase operations

#### 2. TurnService Path (ScenarioWorkbookV2)
```
LiveSim.razor → /api/simulation/turn → SimulationController
  → ProcessTurnWithWorkbookAsync
    → STT (Speech-to-Text)
    → TurnService
      → ResponderRouter (decides ATC vs CTAF/Traffic)
        → OpenAiAtcService (tower-controlled)
        OR
        → TrafficAgent (CTAF/non-controlled)
      → InstructorServiceV2 (enhanced scoring with slots)
      → Phase management & branching
    → TTS (Text-to-Speech)
    → Persist turn data with phase info
```

**Use case:** Complex multi-phase scenarios with:
- Tower-controlled and non-controlled airspace
- Traffic conflicts and CTAF broadcasts
- Dynamic branching based on performance
- Comprehensive rubrics and safety gates
- Phase progression tracking

### Auto-Detection

SimulationController automatically detects which path to use:

```csharp
// Check if scenario uses ScenarioWorkbookV2 format
if (_turnService != null && !string.IsNullOrEmpty(session.Scenario?.InitialStateJson))
{
    try
    {
        var workbook = JsonSerializer.Deserialize<ScenarioWorkbookV2>(session.Scenario.InitialStateJson);
        if (workbook?.Phases?.Any() == true)
        {
            // Use new TurnService path
            return await ProcessTurnWithWorkbookAsync(...);
        }
    }
    catch (JsonException)
    {
        // Fall through to legacy processing
    }
}
// Legacy processing path...
```

## Components

### 1. TurnService

**Purpose:** Orchestrates complex turn processing with workbook support

**Key Features:**
- Phase-based scenario progression
- Branching scenarios with probability-based selection
- ResponderRouter for ATC vs CTAF/Traffic decisions
- Safety gate enforcement
- Comprehensive state management

**Key Methods:**
```csharp
Task<TurnResponse> ProcessTurnAsync(TurnRequest req, CancellationToken ct)
```

### 2. OpenAiAtcService

**Purpose:** Dedicated ATC service for tower-controlled operations

**Key Features:**
- ICAO/CASA-compliant phraseology
- Runway clearance enforcement
- Required readback validation
- Controller persona support (concise/normal/high_workload)
- JSON-structured responses

**System Prompt Highlights:**
- Never implies runway clearance without explicit instruction
- Requires full readback for safety-critical items
- Uses Australian aviation terminology
- Maintains professional tone

### 3. TrafficAgent

**Purpose:** Generates CTAF broadcasts and traffic position calls

**Key Features:**
- Non-controlled airspace communications
- Conflict advisories
- Standard Australian CTAF format
- Concise, professional broadcasts

### 4. ResponderRouter

**Purpose:** Decides who responds (ATC, CTAF, Traffic)

**Logic:**
```
IF tower_active THEN
    Use ATC
ELSE IF conflict_imminent THEN
    Use TrafficNearest
ELSE IF random < random_interject_prob THEN
    Use TrafficRandom
ELSE
    Use CTAF
```

### 5. InstructorServiceV2

**Purpose:** Enhanced scoring with slot-level analysis

**Key Features:**
- Slot-based component scoring (not surface strings)
- CTAF-aware assessment
- Mandatory readback/broadcast gates
- Safety flag detection
- Detailed component breakdown

## ScenarioWorkbookV2 Structure

```json
{
  "meta": { "id": "...", "version": "2.0", "author": "..." },
  "inputs": { "icao": "YMML", "aircraft": "C172", ... },
  "context_resolved": {
    "airport": { "tower_active": true, "tower_mhz": 118.1, ... },
    "runway_in_use": "16",
    "weather_si": { ... },
    "traffic_snapshot": { "density": "moderate", "actors": [...] }
  },
  "phases": [
    {
      "id": "taxi_request",
      "name": "Initial Taxi Request",
      "primary_freq_mhz": 121.7,
      "required_components": ["CALLSIGN", "LOCATION", "ATIS", "INTENTIONS"],
      "expected_readback": ["CALLSIGN", "TAXI_ROUTE", "HOLDING_POINT"],
      "branches": [
        {
          "id": "congestion_delay",
          "probability": 0.3,
          "guard": [...],
          "effects": [...]
        }
      ]
    }
  ],
  "rubric": { "readback_policy": { "block_on_missing": ["RUNWAY", ...] } },
  "tolerance": { ... },
  "global_safety_gates": [...]
}
```

## LiveSim UI Enhancements

### Phase Display
- Shows current phase ID and name in "Next Action" card
- Lists required components for current phase
- Real-time updates as phases progress

### Workbook Panel (Collapsible)
Shows comprehensive scenario information:
- **Metadata:** Scenario ID, version, author
- **Phases:** All phases with current phase indicator
- **Current Phase Details:** ID, name, frequency, required components, branches
- **Airport Context:** ICAO, tower status, runway in use
- **Traffic:** Density, aircraft count, active conflicts

### Phase Tracking
```csharp
// Automatically updates based on AtcResponse.NextState
if (nextState.TryGetProperty("phase", out var phaseElement))
{
    var newPhaseId = phaseElement.GetString();
    if (newPhaseId != currentPhaseId)
    {
        currentPhaseId = newPhaseId;
        currentPhase = workbook.Phases.FirstOrDefault(p => p.Id == newPhaseId);
    }
}
```

## Difficulty Profile Mapping

Maps simple difficulty strings to comprehensive profiles:

```csharp
"basic" → DifficultyLevel.Easy
    ParsingStrictness: 0.4
    Congestion: 0.2
    Variability: 0.1

"medium" → DifficultyLevel.Medium
    ParsingStrictness: 0.6
    Congestion: 0.4
    Variability: 0.3

"advanced" → DifficultyLevel.Hard
    ParsingStrictness: 0.8
    Congestion: 0.6
    Variability: 0.5
```

## State Management

### Turn State Tracking
Each turn preserves:
- Current phase ID
- State deltas from previous turn
- Next phase ID (from ATC/Traffic response)
- Full state bag as JsonElement

### Phase Transitions
```csharp
// Extract next phase from response
if (atc?.NextState != null)
{
    var bag = JsonSerializer.Deserialize<Dictionary<string, object>>(
        JsonSerializer.Serialize(atc.NextState));
    if (bag.TryGetValue("phase", out var p) && p is string s)
        return s;
}
return currentPhaseId; // Stay in current phase
```

## Branching Logic

Branches execute probabilistically based on:
1. **Probability:** Base probability of branch activation
2. **Variability:** Difficulty multiplier (higher difficulty = more variability)
3. **Guard Criteria:** Conditions that must be met
4. **Effects:** State modifications when branch activates

```csharp
var p = random.NextDouble();
double acc = 0;
foreach (var branch in phase.Branches)
{
    acc += branch.Probability * difficulty.Variability;
    if (p <= acc)
        return branch; // This branch activates
}
return null; // No branch activated
```

## Timeline and Transmissions

TurnService builds a timeline of all communications:

```csharp
public sealed class Transmission
{
    public string Source { get; init; } // "ATC", "TRAFFIC:VH-ABC", "SYSTEM"
    public double? FreqMhz { get; init; }
    public string Text { get; init; }
    public string Tone { get; init; } // "professional", "urgent", "calm"
    public string Persona { get; init; } // "concise", "normal", "high_workload"
    public Dictionary<string, string> Attributes { get; init; }
}
```

Multiple transmissions can occur in a single turn:
- ATC clearance
- Traffic position call
- Conflict advisory
- Coaching prompt

## Safety Gates

Enforced at multiple levels:

### Phase-Level Gates
```json
{
  "safety_gates": [
    {
      "id": "runway_readback",
      "trigger": "missing_component",
      "components": ["RUNWAY"],
      "action": "block"
    }
  ]
}
```

### Global Gates
Apply across all phases, defined in workbook root

### Instructor Scoring
Flags safety concerns via `SafetyFlag: true`

## Data Persistence

### Turn Table
Extended with workbook support:
- `InstructorMs` now includes TurnService time
- State stored as JsonElement in AtcJson
- Phase information preserved for debrief

### Metrics
All sub-scores tracked:
- `turn.normalized`
- `turn.phraseAccuracy`
- `turn.ordering`
- `turn.omissions`
- `turn.safety`
- `turn.safetyFlag`

### Verdict Details
Component breakdown per turn:
- Code (e.g., "PA_CALLSIGN")
- Category (PhraseAccuracy, Ordering, etc.)
- Severity (info, minor, major, critical)
- Score, Delta, Detail

## Migration Guide

### For Existing Scenarios
No changes needed - legacy path continues to work

### For New Workbook Scenarios
1. Create ScenarioWorkbookV2 JSON structure
2. Store in `Scenario.InitialStateJson`
3. Define phases with required components
4. Set up branching if desired
5. Configure rubric and safety gates
6. System automatically uses TurnService path

### Testing
Both paths tested via same endpoint:
- Legacy: Simple InitialStateJson or none
- Workbook: Full ScenarioWorkbookV2 structure

## Performance Considerations

### TurnService Path
- Single LLM call per responder (ATC or Traffic)
- Parallel-ready architecture (currently sequential)
- Timeline built incrementally
- State management via JsonElement (efficient)

### Caching Opportunities
- Phase definitions (static per scenario)
- Workbook context (changes rarely)
- Airport data (cached at DB level)

## Future Enhancements

### Potential Improvements
1. **Parallel Processing:** ATC and Traffic calls could run concurrently
2. **State Caching:** Cache frequently accessed state elements
3. **Advanced Branching:** Condition evaluation engine
4. **Multi-Actor Timeline:** Simultaneous transmissions on different frequencies
5. **Voice Synthesis Streaming:** Real-time audio generation
6. **Phase Preview:** Show upcoming phases to students
7. **Adaptive Difficulty:** Automatic difficulty adjustment based on performance

### Planned Features
- Visual phase progression timeline
- Scenario authoring UI
- Workbook validator
- Performance analytics per phase
- Conflict visualization

## Troubleshooting

### Workbook Not Detected
**Symptoms:** Scenario with workbook uses legacy path

**Solutions:**
- Check `InitialStateJson` is valid JSON
- Verify `phases` array exists and is non-empty
- Check logs for JSON parsing errors

### Phase Not Advancing
**Symptoms:** Phase stays same after turn

**Solutions:**
- Verify ATC/Traffic response includes `nextState.phase`
- Check phase IDs match workbook definition
- Review logs for phase transition logic

### Missing Timeline
**Symptoms:** No ATC or traffic responses

**Solutions:**
- Check ResponderRouter logic
- Verify tower_active flag in workbook
- Review traffic snapshot data
- Check for LLM API errors

## References

- [ScenarioWorkbookV2 Specification](../backend/PilotSim.Core/Class1.cs) - Core data structures
- [TurnService Implementation](../backend/PilotSim.Server/Services/TurnService.cs) - Main orchestration logic
- [SimulationController](../backend/PilotSim.Server/Controllers/SimulationController.cs) - API endpoint and routing
- [LiveSim Component](../backend/PilotSim.Server/Components/Pages/LiveSim.razor) - UI implementation
