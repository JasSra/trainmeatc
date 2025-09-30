# Branch Matrix Scenarios - Complete Reference

This document provides comprehensive examples of all 30+ branch scenarios supported by the TurnService micro-flow architecture.

## Overview

The branch matrix system allows scenarios to dynamically adapt based on:
- **Environment changes** (weather, runway, NOTAMs)
- **Traffic conditions** (conflicts, crossing traffic, compression)
- **Controller decisions** (amendments, holds, priority handling)
- **Emergency situations** (radio failures, PAN/MAYDAY, abnormals)

Each branch is defined in the `PhaseSpec.Branches` array with:
- **Probability**: Base chance of activation (0.0 to 1.0)
- **Guard criteria**: Conditions that must be met
- **Effects**: State modifications when activated
- **ATC override**: Optional custom transmission/readback requirements

## Branch Categories

### Environment (1-5)

#### 1. Wind Shift → Runway Change
```json
{
  "id": "wind_shift_runway_change",
  "probability": 0.15,
  "guard": [
    {
      "lhs": "weather_si.wind_dir_deg",
      "op": ">=",
      "rhs": 200
    }
  ],
  "effects": [
    {
      "key": "runway_in_use",
      "value": "20"
    },
    {
      "key": "atis_code",
      "value": "F"
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, wind now 200 at 15, runway changed to 20, expect vectors for visual approach runway 20.",
    "expected_readback": ["CALLSIGN", "RUNWAY", "APPROACH_TYPE"],
    "required_components": ["CALLSIGN", "RUNWAY_20", "VISUAL_APPROACH"]
  }
}
```

**Trigger**: Wind direction shift exceeds runway crosswind limits
**Effect**: Changes active runway, updates ATIS, requires new approach briefing
**Phase applicability**: Inbound, circuit, approach phases

#### 2. QNH Update
```json
{
  "id": "qnh_update",
  "probability": 0.20,
  "guard": [],
  "effects": [
    {
      "key": "weather_si.qnh_hpa",
      "value": 1008
    },
    {
      "key": "atis_code",
      "value": "C"
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, QNH now 1008, advise received information Charlie.",
    "expected_readback": ["CALLSIGN", "QNH", "ATIS_CODE"],
    "required_components": ["QNH_1008", "INFORMATION_CHARLIE"]
  }
}
```

**Trigger**: Atmospheric pressure change
**Effect**: Updates QNH, changes ATIS code
**Phase applicability**: All phases

#### 3. Visibility/Ceiling Drop → Restrict Joins
```json
{
  "id": "vis_ceiling_restrict",
  "probability": 0.10,
  "guard": [
    {
      "lhs": "weather_si.vis_km",
      "op": "<=",
      "rhs": 8.0
    }
  ],
  "effects": [
    {
      "key": "weather_si.vis_km",
      "value": 5.0
    },
    {
      "key": "weather_si.ceiling_m_agl",
      "value": 450
    },
    {
      "key": "joins_restricted",
      "value": true
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, visibility now 5 kilometres, ceiling 1500 feet, overhead joins only, no straight-in approaches.",
    "expected_readback": ["CALLSIGN", "VISIBILITY", "CEILING", "JOIN_TYPE"],
    "required_components": ["VIS_5KM", "CEILING_1500FT", "OVERHEAD_JOIN"]
  },
  "adds_required_components": ["OVERHEAD_JOIN"]
}
```

**Trigger**: Deteriorating weather conditions
**Effect**: Restricts join types, adds overhead join requirement
**Phase applicability**: Inbound, join phases

#### 4. Tower Toggles Inactive ↔ CTAF
```json
{
  "id": "tower_close_time",
  "probability": 0.05,
  "guard": [
    {
      "lhs": "time_of_day",
      "op": "==",
      "rhs": "dusk"
    }
  ],
  "effects": [
    {
      "key": "airport.tower_active",
      "value": false
    },
    {
      "key": "primary_freq_mhz",
      "value": 126.7
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, tower closing now, switch to CTAF 126.7, good evening.",
    "expected_readback": ["CALLSIGN", "FREQUENCY", "CTAF"],
    "required_components": ["FREQ_126_7", "CTAF"]
  }
}
```

**Trigger**: Time-based tower closure
**Effect**: Switches from ATC to CTAF operations, changes frequency
**Phase applicability**: Any phase when tower is active

#### 5. NOTAM Impact
```json
{
  "id": "notam_lighting_us",
  "probability": 0.08,
  "guard": [],
  "effects": [
    {
      "key": "notams",
      "value": [
        {
          "id": "A0123/24",
          "summary": "RWY 16/34 PAPI unserviceable",
          "active": true
        }
      ]
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, be advised runway 16 PAPI unserviceable, visual approach only.",
    "expected_readback": ["CALLSIGN", "NOTAM_ACKNOWLEDGEMENT"],
    "required_components": ["PAPI_US", "VISUAL_APPROACH"]
  }
}
```

**Trigger**: New NOTAM activated
**Effect**: Adds operational restrictions
**Phase applicability**: Approach, circuit phases

### Traffic (6-10)

#### 6. Conditional Clearance (After Crossing Traffic)
```json
{
  "id": "conditional_clearance",
  "probability": 0.25,
  "guard": [
    {
      "lhs": "traffic_snapshot.conflicts[0].event",
      "op": "==",
      "rhs": "runway_crossing"
    }
  ],
  "effects": [
    {
      "key": "clearance_conditional",
      "value": true
    },
    {
      "key": "traffic_reference",
      "value": "QFA456"
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, behind the crossing Qantas 737, line up runway 16 and wait.",
    "expected_readback": ["CALLSIGN", "TRAFFIC_REFERENCE", "LINEUP_WAIT", "RUNWAY"],
    "required_components": ["BEHIND_TRAFFIC", "QFA456", "LINEUP_WAIT", "RUNWAY_16"]
  },
  "adds_required_components": ["TRAFFIC_REFERENCE"]
}
```

**Trigger**: Traffic crossing runway ahead
**Effect**: Issues conditional clearance with traffic reference
**Phase applicability**: Taxi, lineup phases

#### 7. Crossing Runway Traffic
```json
{
  "id": "crossing_runway",
  "probability": 0.20,
  "guard": [
    {
      "lhs": "traffic_snapshot.density",
      "op": "==",
      "rhs": "moderate"
    }
  ],
  "effects": [
    {
      "key": "traffic_snapshot.conflicts",
      "value": [
        {
          "with_callsign": "VH-ABC",
          "event": "runway_crossing",
          "time_to_conflict_s": 45
        }
      ]
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, hold position, traffic crossing ahead will be a Cessna from left to right.",
    "expected_readback": ["CALLSIGN", "HOLD_POSITION", "TRAFFIC_REFERENCE"],
    "required_components": ["HOLD_POSITION", "TRAFFIC_SIGHTED"]
  }
}
```

**Trigger**: Traffic needs to cross runway
**Effect**: Creates hold short situation
**Phase applicability**: Taxi, lineup, takeoff phases

#### 8. Sequence Compression → "Continue" or Go-Around
```json
{
  "id": "sequence_compression",
  "probability": 0.18,
  "guard": [
    {
      "lhs": "traffic_snapshot.conflicts[0].time_to_conflict_s",
      "op": "<=",
      "rhs": 60
    }
  ],
  "effects": [
    {
      "key": "sequence_tight",
      "value": true
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, traffic ahead not yet clear of runway, continue approach, be prepared for go-around.",
    "expected_readback": ["CALLSIGN", "CONTINUE", "GO_AROUND_READY"],
    "required_components": ["CONTINUE_APPROACH", "GO_AROUND_READY"]
  }
}
```

**Trigger**: Traffic on runway with aircraft on final
**Effect**: Sets up potential go-around scenario
**Phase applicability**: Final approach phase

#### 9. Stepped-on/Garbled Segment; Readback Retry
```json
{
  "id": "radio_interference",
  "probability": 0.12,
  "guard": [
    {
      "lhs": "traffic_snapshot.density",
      "op": "==",
      "rhs": "heavy"
    }
  ],
  "effects": [
    {
      "key": "radio_quality",
      "value": "garbled"
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, say again your last, you were blocked.",
    "expected_readback": ["CALLSIGN", "READBACK_REPEAT"],
    "required_components": ["FULL_READBACK_REPEAT"]
  }
}
```

**Trigger**: Heavy traffic causing radio congestion
**Effect**: Requires readback repetition
**Phase applicability**: All phases

#### 10. Helo Transit or Opposite-Direction Circuit
```json
{
  "id": "helo_transit",
  "probability": 0.10,
  "guard": [],
  "effects": [
    {
      "key": "traffic_snapshot.actors",
      "value": [
        {
          "callsign": "HELO-1",
          "type": "R44",
          "wake": "Light",
          "intent": "transit_east",
          "alt_m_msl": 300,
          "gs_mps": 30
        }
      ]
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, traffic 2 o'clock, 1 mile, helicopter tracking east at 1000 feet.",
    "expected_readback": ["CALLSIGN", "TRAFFIC_SIGHTED"],
    "required_components": ["TRAFFIC_SIGHTED", "HELO"]
  }
}
```

**Trigger**: Helicopter operating in circuit area
**Effect**: Adds traffic advisory
**Phase applicability**: Circuit phases

### Taxi/Line-up (Controlled) (11-14)

#### 11. Taxi Route Amended Mid-Movement
```json
{
  "id": "taxi_route_change",
  "probability": 0.15,
  "guard": [
    {
      "lhs": "phase_id",
      "op": "==",
      "rhs": "taxi_to_runway"
    }
  ],
  "effects": [
    {
      "key": "taxi_route",
      "value": ["A", "B2", "B"]
    },
    {
      "key": "holding_point",
      "value": "B"
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, amended taxi instructions, taxi via Alpha, Bravo 2, hold short Bravo.",
    "expected_readback": ["CALLSIGN", "TAXI_ROUTE", "HOLDING_POINT"],
    "required_components": ["VIA_A", "VIA_B2", "HOLD_SHORT_B"]
  }
}
```

**Trigger**: Traffic or operational requirements
**Effect**: Changes taxi route
**Phase applicability**: Taxi phases

#### 12. Hold Short Extension
```json
{
  "id": "hold_short_extension",
  "probability": 0.20,
  "guard": [],
  "effects": [
    {
      "key": "hold_extended",
      "value": true
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, continue holding, I'll call your departure.",
    "expected_readback": ["CALLSIGN", "HOLDING"],
    "required_components": ["HOLDING"]
  }
}
```

**Trigger**: Traffic requires extended hold
**Effect**: Maintains hold short position
**Phase applicability**: Holding point phase

#### 13. Backtrack Instruction
```json
{
  "id": "backtrack",
  "probability": 0.12,
  "guard": [],
  "effects": [
    {
      "key": "backtrack_required",
      "value": true
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, enter and backtrack runway 16, line up and wait.",
    "expected_readback": ["CALLSIGN", "BACKTRACK", "RUNWAY", "LINEUP_WAIT"],
    "required_components": ["BACKTRACK", "RUNWAY_16", "LINEUP_WAIT"]
  }
}
```

**Trigger**: Departure from mid-runway position
**Effect**: Requires backtrack maneuver
**Phase applicability**: Lineup phase

#### 14. Line Up and Wait with Traffic on Final
```json
{
  "id": "lineup_wait_traffic",
  "probability": 0.25,
  "guard": [
    {
      "lhs": "traffic_snapshot.conflicts[0].event",
      "op": "==",
      "rhs": "on_final"
    }
  ],
  "effects": [
    {
      "key": "traffic_on_final",
      "value": true
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, traffic 3 mile final, line up runway 16 and wait.",
    "expected_readback": ["CALLSIGN", "TRAFFIC", "LINEUP_WAIT", "RUNWAY"],
    "required_components": ["TRAFFIC_FINAL", "LINEUP_WAIT", "RUNWAY_16"]
  }
}
```

**Trigger**: Sequencing for departure between arrivals
**Effect**: Line up with traffic awareness
**Phase applicability**: Lineup phase

### Departure (Controlled) (15-18)

#### 15. SID vs Radar Vectors Swap
```json
{
  "id": "sid_to_vectors",
  "probability": 0.15,
  "guard": [],
  "effects": [
    {
      "key": "departure_type",
      "value": "radar_vectors"
    },
    {
      "key": "sid_cancelled",
      "value": true
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, cancel SID, fly runway heading, I'll vector you.",
    "expected_readback": ["CALLSIGN", "CANCEL_SID", "RUNWAY_HEADING", "VECTORS"],
    "required_components": ["CANCEL_SID", "RUNWAY_HEADING"]
  }
}
```

**Trigger**: Traffic flow management
**Effect**: Changes departure procedure
**Phase applicability**: Pre-departure, initial climb phases

#### 16. SSR Re-assignment
```json
{
  "id": "squawk_reassignment",
  "probability": 0.10,
  "guard": [],
  "effects": [
    {
      "key": "squawk_code",
      "value": "3521"
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, squawk 3521.",
    "expected_readback": ["CALLSIGN", "SQUAWK"],
    "required_components": ["SQUAWK_3521"]
  }
}
```

**Trigger**: SSR code conflict or tracking requirement
**Effect**: Changes transponder code
**Phase applicability**: Any controlled phase

#### 17. Early Turn Restriction
```json
{
  "id": "early_turn_restriction",
  "probability": 0.12,
  "guard": [],
  "effects": [
    {
      "key": "turn_restriction",
      "value": "not_before_500ft"
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, not before 500 feet, turn left heading 120.",
    "expected_readback": ["CALLSIGN", "ALTITUDE_RESTRICTION", "HEADING"],
    "required_components": ["NOT_BEFORE_500FT", "LEFT", "HEADING_120"]
  }
}
```

**Trigger**: Noise abatement or traffic separation
**Effect**: Adds altitude restriction to turn
**Phase applicability**: Initial departure phase

#### 18. Runway Change Before Take-off Roll
```json
{
  "id": "runway_change_pre_takeoff",
  "probability": 0.08,
  "guard": [
    {
      "lhs": "phase_id",
      "op": "==",
      "rhs": "lineup_wait"
    }
  ],
  "effects": [
    {
      "key": "runway_in_use",
      "value": "34"
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, wind now favouring runway 34, cancel take-off clearance, taxi via Bravo, Alpha 3 for runway 34.",
    "expected_readback": ["CALLSIGN", "CANCEL_TAKEOFF", "TAXI_ROUTE", "RUNWAY"],
    "required_components": ["CANCEL_TAKEOFF", "VIA_B_A3", "RUNWAY_34"]
  }
}
```

**Trigger**: Wind shift while lined up
**Effect**: Requires runway change
**Phase applicability**: Lineup phase

### Inbound/Join (CTAF) (19-22)

#### 19. Overhead Join Required vs Straight-In Discouraged
```json
{
  "id": "overhead_join_required_ctaf",
  "probability": 0.20,
  "guard": [
    {
      "lhs": "airport.tower_active",
      "op": "==",
      "rhs": false
    }
  ],
  "effects": [
    {
      "key": "join_type",
      "value": "overhead"
    }
  ],
  "adds_required_components": ["OVERHEAD_JOIN", "CIRCUIT_ALTITUDE"]
}
```

**Trigger**: CTAF operations with local procedures
**Effect**: Requires overhead join instead of straight-in
**Phase applicability**: Inbound CTAF phase
**Note**: No ATC override; student must know CTAF procedures

#### 20. Join from Non-Active Side with Altitude Buffer
```json
{
  "id": "crosswind_join_altitude",
  "probability": 0.15,
  "guard": [
    {
      "lhs": "circuit_direction",
      "op": "==",
      "rhs": "left"
    }
  ],
  "effects": [
    {
      "key": "join_altitude_m_agl",
      "value": 610
    }
  ],
  "adds_required_components": ["ALTITUDE_2000FT", "JOINING_CROSSWIND"]
}
```

**Trigger**: Joining from inactive side
**Effect**: Requires altitude buffer (≥150m above circuit height)
**Phase applicability**: Join CTAF phase

#### 21. Late Downwind Call Conflict
```json
{
  "id": "late_downwind_conflict",
  "probability": 0.18,
  "guard": [
    {
      "lhs": "traffic_snapshot.conflicts[0].event",
      "op": "==",
      "rhs": "downwind_conflict"
    }
  ],
  "effects": [
    {
      "key": "traffic_snapshot.conflicts",
      "value": [
        {
          "with_callsign": "VH-XYZ",
          "event": "downwind_same_position",
          "time_to_conflict_s": 30
        }
      ]
    }
  }
}
```

**Trigger**: Another aircraft makes late downwind call
**Effect**: Creates potential sequencing conflict (CTAF self-separation)
**Phase applicability**: Circuit CTAF phases

#### 22. Straight-In Within 5.6 km Without Prior Call → Block
```json
{
  "id": "straight_in_no_call_block",
  "probability": 0.15,
  "guard": [
    {
      "lhs": "join_type",
      "op": "==",
      "rhs": "straight_in"
    },
    {
      "lhs": "distance_km",
      "op": "<=",
      "rhs": 5.6
    }
  ],
  "effects": [
    {
      "key": "safety_violation",
      "value": "straight_in_no_broadcast"
    }
  ]
}
```

**Trigger**: Straight-in approach without proper broadcast
**Effect**: Safety gate violation (blocked turn)
**Phase applicability**: Inbound CTAF phase

### Circuit (CTAF) (23-26)

#### 23. Base-to-Final Compression
```json
{
  "id": "base_final_compression",
  "probability": 0.20,
  "guard": [
    {
      "lhs": "traffic_snapshot.conflicts[0].event",
      "op": "==",
      "rhs": "base_compression"
    }
  ],
  "effects": [
    {
      "key": "traffic_snapshot.conflicts",
      "value": [
        {
          "with_callsign": "VH-DEF",
          "event": "tight_base",
          "time_to_conflict_s": 20
        }
      ]
    }
  ],
  "adds_required_components": ["EXTENDING_DOWNWIND", "TRAFFIC_REFERENCE"]
}
```

**Trigger**: Traffic on base creates tight sequence
**Effect**: Student should extend downwind or announce intentions
**Phase applicability**: Circuit downwind/base CTAF

#### 24. Touch-and-Go vs Full Stop Ambiguity
```json
{
  "id": "touch_go_full_stop",
  "probability": 0.15,
  "guard": [],
  "effects": [
    {
      "key": "landing_type_query",
      "value": true
    }
  },
  "adds_required_components": ["LANDING_INTENTION"]
}
```

**Trigger**: Need to clarify landing intentions
**Effect**: Requires explicit touch-and-go or full stop broadcast
**Phase applicability**: Final CTAF phase

#### 25. Go-Around Self-Announce Due to Conflict
```json
{
  "id": "go_around_ctaf",
  "probability": 0.10,
  "guard": [
    {
      "lhs": "traffic_snapshot.conflicts[0].event",
      "op": "==",
      "rhs": "runway_occupied"
    }
  ],
  "effects": [
    {
      "key": "go_around_required",
      "value": true
    }
  ],
  "adds_required_components": ["GO_AROUND", "RUNWAY", "INTENTIONS"]
}
```

**Trigger**: Runway not clear on short final
**Effect**: Requires go-around broadcast on CTAF
**Phase applicability**: Final CTAF phase

#### 26. Vacate + "Clear of Runway" Call Omission
```json
{
  "id": "clear_runway_omission",
  "probability": 0.18,
  "guard": [],
  "effects": [
    {
      "key": "clear_runway_expected",
      "value": true
    }
  ],
  "adds_required_components": ["CLEAR_OF_RUNWAY", "LOCATION"]
}
```

**Trigger**: After landing, must broadcast clear of runway
**Effect**: Requires "clear of runway" call on CTAF
**Phase applicability**: Landing/vacate CTAF phase

### Emergency/Abnormals (27-30)

#### 27. Radio Congestion → "Unable, Stand By"
```json
{
  "id": "radio_congestion",
  "probability": 0.15,
  "guard": [
    {
      "lhs": "traffic_snapshot.density",
      "op": "==",
      "rhs": "heavy"
    }
  ],
  "effects": [
    {
      "key": "radio_congested",
      "value": true
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, standby.",
    "expected_readback": ["CALLSIGN"],
    "required_components": ["STANDBY_ACKNOWLEDGE"]
  }
}
```

**Trigger**: High traffic density
**Effect**: ATC delays response
**Phase applicability**: All controlled phases

#### 28. Partial Radio → Unreadable(3), Request Repeat
```json
{
  "id": "partial_radio_failure",
  "probability": 0.08,
  "guard": [],
  "effects": [
    {
      "key": "radio_quality",
      "value": "unreadable_3"
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, you're unreadable 3, say again slowly.",
    "expected_readback": ["CALLSIGN", "READBACK_SLOW"],
    "required_components": ["FULL_READBACK_SLOW"]
  }
}
```

**Trigger**: Radio quality degradation
**Effect**: Requires slow, clear readback
**Phase applicability**: All phases

#### 29. PAN/MAYDAY (Controlled) Priority Handling
```json
{
  "id": "emergency_priority",
  "probability": 0.05,
  "guard": [],
  "effects": [
    {
      "key": "emergency_aircraft",
      "value": "QFA789"
    },
    {
      "key": "priority_given",
      "value": true
    }
  ],
  "atc_override": {
    "transmission": "{CALLSIGN}, hold position, emergency aircraft on approach, I'll call you.",
    "expected_readback": ["CALLSIGN", "HOLD_POSITION"],
    "required_components": ["HOLD_POSITION", "ACKNOWLEDGE_EMERGENCY"]
  }
}
```

**Trigger**: Emergency aircraft in circuit
**Effect**: Student aircraft holds for emergency
**Phase applicability**: Taxi, lineup, circuit phases

#### 30. Precautionary Landing Advisory (CTAF)
```json
{
  "id": "precautionary_landing_ctaf",
  "probability": 0.05,
  "guard": [
    {
      "lhs": "airport.tower_active",
      "op": "==",
      "rhs": false
    }
  ],
  "effects": [
    {
      "key": "precautionary_aircraft",
      "value": "VH-ABC"
    }
  ]
}
```

**Trigger**: Aircraft broadcasting precautionary landing on CTAF
**Effect**: Student should give way and acknowledge
**Phase applicability**: All CTAF phases

## Branch Activation Flow

```
Turn Start
  ↓
Check Phase.Branches
  ↓
For each branch:
  ↓
  Evaluate Guard Criteria ─→ Fail ─→ Next Branch
  ↓ Pass
  ↓
  Roll Probability × Difficulty.Variability ─→ Miss ─→ Next Branch
  ↓ Hit
  ↓
  Apply Effects to Context
  ↓
  If ATC Override exists:
    - Use override transmission
    - Use override expected_readback
    - Add/modify required_components
  ↓
  Mark branch as activated in TurnState
  ↓
Continue Turn Processing
```

## Usage in Scenarios

### Example: Complete Phase with Multiple Branches

```json
{
  "id": "final_approach",
  "name": "Final Approach",
  "primary_freq_mhz": 118.1,
  "required_components": ["CALLSIGN", "RUNWAY", "LANDING_CLEARANCE"],
  "expected_readback": ["CALLSIGN", "RUNWAY", "CLEARED_LAND"],
  "branches": [
    {
      "id": "sequence_compression",
      "probability": 0.18,
      "guard": [
        {
          "lhs": "traffic_snapshot.conflicts[0].time_to_conflict_s",
          "op": "<=",
          "rhs": 60
        }
      ],
      "effects": [
        {
          "key": "sequence_tight",
          "value": true
        }
      ],
      "atc_override": {
        "transmission": "{CALLSIGN}, continue approach, be prepared for go-around.",
        "expected_readback": ["CALLSIGN", "CONTINUE", "GO_AROUND_READY"],
        "required_components": ["CONTINUE_APPROACH", "GO_AROUND_READY"]
      }
    },
    {
      "id": "wind_shear_alert",
      "probability": 0.10,
      "guard": [
        {
          "lhs": "weather_si.gust_mps",
          "op": ">=",
          "rhs": 10
        }
      ],
      "effects": [
        {
          "key": "wind_shear_present",
          "value": true
        }
      ],
      "atc_override": {
        "transmission": "{CALLSIGN}, wind shear alert, winds 250 at 25 gusting 35 knots.",
        "expected_readback": ["CALLSIGN", "WIND_ACKNOWLEDGE"],
        "required_components": ["WIND_SHEAR_ACKNOWLEDGE"]
      }
    }
  ],
  "safety_gates": [
    {
      "code": "landing_clearance_missing",
      "trigger": {
        "lhs": "components.LANDING_CLEARANCE",
        "op": "missing"
      },
      "action": "block",
      "controller_on_block": "{CALLSIGN}, you are not cleared to land, go around."
    }
  ]
}
```

## Best Practices

### 1. Probability Tuning
- Use higher probabilities (0.15-0.30) for common scenarios
- Use lower probabilities (0.05-0.10) for rare events
- Consider that `Probability × Difficulty.Variability` determines activation

### 2. Guard Criteria Design
- Keep guards simple and specific
- Use context state keys that are reliably populated
- Test guard evaluation with various state configurations

### 3. Effects Management
- Only modify state keys that are relevant to the branch
- Ensure effects don't create invalid state combinations
- Document state changes for debugging

### 4. ATC Override Guidelines
- Use override for significant procedural changes
- Ensure override transmissions are ICAO/CASA compliant
- Add new required_components when adding new requirements
- Test readback requirements thoroughly

### 5. Phase Applicability
- Document which phases each branch applies to
- Use entry_criteria to restrict branch activation by phase
- Consider phase progression when designing branch chains

## Testing Branch Scenarios

### Unit Testing
```csharp
[Fact]
public async Task WindShiftBranch_ActivatesOnCorrectConditions()
{
    var workbook = CreateWorkbook();
    var phase = workbook.Phases[0];
    phase.Branches.Add(new BranchSpec
    {
        Id = "wind_shift",
        Probability = 1.0, // Force activation for testing
        Guard = new List<Criterion>
        {
            new() { Lhs = "weather_si.wind_dir_deg", Op = ">=", Rhs = 200 }
        },
        Effects = new List<StateDelta>
        {
            new() { Key = "runway_in_use", Value = "20" }
        }
    });

    var request = CreateTurnRequest(workbook, windDir: 210);
    var response = await _turnService.ProcessTurnAsync(request);

    Assert.Equal("wind_shift", response.TurnState.BranchActivated);
    Assert.Contains("20", response.UpdatedState.GetProperty("runway_in_use").GetString());
}
```

### Integration Testing
- Test branches in realistic scenario flows
- Verify state consistency across phase transitions
- Confirm ATC overrides produce correct transmissions
- Validate required_components additions work correctly

## Troubleshooting

### Branch Not Activating
1. Check probability × variability value
2. Verify guard criteria match current state
3. Confirm phase entry_criteria allow branch
4. Review logs for guard evaluation results

### Incorrect State After Branch
1. Verify effects array contains correct state deltas
2. Check for state key typos
3. Ensure JSON value types match expected types
4. Review ApplyBranch implementation

### ATC Override Not Applied
1. Confirm atc_override object is present
2. Check that branch actually activated
3. Verify override transmission format
4. Test required_components additions

## References

- [TurnService Implementation](../backend/PilotSim.Server/Services/TurnService.cs)
- [Branch Data Structures](../backend/PilotSim.Core/Class1.cs)
- [TURNSERVICE_ARCHITECTURE.md](./TURNSERVICE_ARCHITECTURE.md)
- [CASA Radiotelephony Manual](https://www.casa.gov.au/search-centre/manuals/radiotelephony-manual)
