# Example Scenario Workbooks

This directory contains example ScenarioWorkbookV2 JSON files demonstrating various features and branch scenarios supported by the TurnService.

## Files

### scenario_simple_branch_example.json
**Purpose:** Minimal example demonstrating basic branch activation

**Features demonstrated:**
- Simple taxi request scenario
- Single branch: taxi route amendment
- Basic phase progression
- ATC override with custom transmission

**Use case:** Learning the basic structure of branches and how they activate

---

### scenario_ctaf_circuit_advanced.json
**Purpose:** Complete circuit scenario with multiple advanced features

**Features demonstrated:**
- Tower to CTAF transition (tower closure at dusk)
- Multiple branch types:
  - Taxi route changes
  - Traffic conflicts (crossing traffic, lineup with traffic on final)
  - Wind shift with runway change
  - Late downwind conflicts
  - Base-to-final compression
  - Sequence compression with go-around
  - Touch-and-go vs full stop clarification
  - Clear of runway broadcast requirement
- ATC and CTAF operations in same scenario
- Safety gates enforcement
- Completion criteria (success/fail major/fail minor)
- Comprehensive rubric with component scoring

**Use case:** Full training scenario for circuit operations with realistic variability

---

## Using These Examples

### Loading in Application
1. Copy JSON content
2. Create new Scenario in database
3. Set `InitialStateJson` to the workbook JSON
4. The system will automatically detect and use TurnService path

### Testing Branches
To test specific branches:
1. Set `variability: 1.0` in inputs (forces maximum branch activation)
2. Adjust branch `probability` to control activation chance
3. Modify `guard` criteria to match your test conditions

### Customization

#### Adjust Difficulty
```json
"inputs": {
  "variability": 0.5,  // 0.0 = scripted, 1.0 = max variability
  "load": 0.4          // 0.0 = light, 1.0 = heavy traffic
}
```

#### Add New Branches
See [BRANCH_MATRIX_SCENARIOS.md](../BRANCH_MATRIX_SCENARIOS.md) for templates of all 30+ branch types.

Example:
```json
{
  "id": "your_branch_id",
  "probability": 0.20,
  "guard": [
    {
      "lhs": "some_state_key",
      "op": "==",
      "rhs": "some_value"
    }
  ],
  "effects": [
    {
      "key": "state_to_modify",
      "value": "new_value"
    }
  ],
  "atc_override": {
    "transmission": "Custom ATC transmission",
    "expected_readback": ["COMPONENT1", "COMPONENT2"],
    "required_components": ["COMPONENT1", "COMPONENT2"]
  }
}
```

#### Modify Completion Criteria
```json
"completion": {
  "success_when_all": [
    {
      "lhs": "completed_phases",
      "op": "contains",
      "rhs": "final_phase_id"
    },
    {
      "lhs": "avg_readback_coverage",
      "op": ">=",
      "rhs": 0.80
    }
  ],
  "fail_major_when_any": [
    {
      "lhs": "safety_violations",
      "op": ">=",
      "rhs": 1
    }
  ]
}
```

## Creating New Scenarios

### Minimal Template
```json
{
  "meta": {
    "id": "your_scenario_id",
    "version": "2.0",
    "author": "Your Name"
  },
  "inputs": {
    "icao": "ICAO_CODE",
    "aircraft": "C172",
    "variability": 0.3,
    "load": 0.3
  },
  "context_resolved": {
    "airport": {
      "tower_active": true,
      "tower_mhz": 118.1
    },
    "runway_in_use": "16",
    "weather_si": {},
    "traffic_snapshot": {
      "density": "light",
      "actors": [],
      "conflicts": []
    }
  },
  "phases": [
    {
      "id": "phase1",
      "name": "Phase 1",
      "primary_freq_mhz": 118.1,
      "required_components": [],
      "expected_readback": [],
      "branches": []
    }
  ],
  "rubric": {
    "readback_policy": {
      "block_on_missing": ["RUNWAY"]
    }
  },
  "completion": {
    "end_phase_id": "end"
  }
}
```

## Validation

Before using a new scenario:
1. Validate JSON syntax (use online validator or IDE)
2. Check all phase IDs are unique
3. Verify `next_state.phase_id` references exist
4. Confirm completion criteria reference valid state keys
5. Test branch guard criteria with realistic state values

## Troubleshooting

### Branch Not Activating
- Check `probability × variability` (e.g., 0.15 × 0.4 = 0.06 = 6% chance)
- Verify `guard` criteria match current state
- Review logs for guard evaluation results

### Phase Not Advancing
- Confirm `next_state.phase_id` in phase definition
- Check ATC/Traffic response includes correct phase
- Verify phase ID exists in workbook

### Missing Required Components
- Add components to `required_components` array
- Define in `rubric.readback_policy.slot_definitions` if needed
- Use `adds_required_components` in branches to add conditionally

## References

- [TurnService Architecture](../TURNSERVICE_ARCHITECTURE.md) - Complete system documentation
- [Branch Matrix Scenarios](../BRANCH_MATRIX_SCENARIOS.md) - All 30+ branch type templates
- [ScenarioWorkbookV2 Specification](../../backend/PilotSim.Core/Class1.cs) - Data structure definitions

## Support

For issues or questions:
1. Check documentation in `docs/` directory
2. Review logs for detailed error messages
3. Validate JSON structure
4. Test with minimal scenario first

## Contributing

When adding new examples:
1. Use descriptive file names: `scenario_[type]_[feature].json`
2. Include comprehensive comments in `authoring_notes`
3. Document unique features in this README
4. Test thoroughly before committing
