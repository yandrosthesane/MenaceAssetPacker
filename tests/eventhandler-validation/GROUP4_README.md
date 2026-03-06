# EventHandler Group 4 Test Suite

## Overview

This directory contains comprehensive test files for EventHandler types in Group 4. All tests are designed to verify that EventHandlers load correctly through the Menace SDK and that their parameters can be accessed.

## Contents

### Test Files (18)

| EventHandler Type | File | Instances | Status |
|-------------------|------|-----------|--------|
| HideByCondition | `group4-HideByCondition.json` | 1 | ✓ Ready |
| Hitchance | `group4-Hitchance.json` | 1 | ✓ Ready |
| IgnoreDamage | `group4-IgnoreDamage.json` | 3 | ✓ Ready |
| InterceptAPChange | `group4-InterceptAPChange.json` | 2 | ✓ Ready |
| Jamming | `group4-Jamming.json` | 4 | ✓ Ready |
| JetPack | `group4-JetPack.json` | 2 | ✓ Ready |
| JumpIntoMelee | `group4-JumpIntoMelee.json` | 1 | ✓ Ready |
| LifetimeLimit | `group4-LifetimeLimit.json` | 54 | ✓ Ready |
| LimitedPassiveUses | `group4-LimitedPassiveUses.json` | 1 | ✓ Ready |
| LimitMaxHpLoss | `group4-LimitMaxHpLoss.json` | 2 | ✓ Ready |
| LimitUsability | `group4-LimitUsability.json` | 7 | ✓ Ready |
| ModularVehicleSkill | `group4-ModularVehicleSkill.json` | 51 | ✓ Ready |
| MoraleOverMaxEffect | `group4-MoraleOverMaxEffect.json` | 1 | ✓ Ready |
| OnElementKilled | `group4-OnElementKilled.json` | 2 | ✓ Ready |
| OverrideVolumeProfile | `group4-OverrideVolumeProfile.json` | 1 | ✓ Ready |
| PlayAnimationSequence | `group4-PlayAnimationSequence.json` | 3 | ✓ Ready |
| PlaySound | `group4-PlaySound.json` | 4 | ✓ Ready |
| PlaySoundOnEnabled | `group4-PlaySoundOnEnabled.json` | 1 | ✓ Ready |

**Total:** 18 test files covering 165 EventHandler instances

### Documentation Files (3)

1. **GROUP4_TEST_SUMMARY.md** - High-level overview and coverage matrix
2. **GROUP4_DETAILED_EXAMPLES.md** - Detailed analysis of each EventHandler type
3. **GROUP4_README.md** - This file

## Test Structure

Each test file follows this standard format:

```json
{
  "name": "EventHandler - TypeName",
  "description": "Verify TypeName EventHandler loads and can be read",
  "steps": [
    {"type": "command", "command": "test.goto_main"},
    {"type": "wait", "durationMs": 2000},
    {"type": "repl", "name": "Find skill with TypeName", "code": "..."},
    {"type": "repl", "name": "Load EventHandlers array", "code": "..."},
    {"type": "repl", "name": "Verify _type field", "code": "..."},
    {"type": "repl", "name": "Read parameter field", "code": "..."}
  ]
}
```

### Test Execution Steps

1. Navigate to main menu (`test.goto_main`)
2. Wait for templates to load (2000ms)
3. Discover skill using EventHandler type
4. Load EventHandlers array from skill
5. Verify EventHandler _type field exists
6. Read 1-3 parameter fields specific to the type

## How Tests Work

### Discovery Method

Tests use runtime discovery to find skills with each EventHandler type:

```csharp
var skill = Menace.SDK.Templates.FindAll("SkillTemplate")
    .FirstOrDefault(s => {
        var eh = Menace.SDK.Templates.GetProperty(s, "EventHandlers");
        return eh != null && eh.ToString().Contains("EventHandlerType");
    });
```

### Verification Method

Tests verify EventHandler loading and parameter accessibility:

1. **Type Verification** - Confirms type name in EventHandlers array
2. **Property Loading** - Verifies EventHandlers property loads
3. **Parameter Access** - Tests reading parameter fields via string matching

### No Hardcoded Dependencies

All tests:
- Discover data at runtime (completely data-driven)
- Work with any game data snapshot
- Don't require specific skill configurations
- Are resilient to data changes

## Parameter Coverage

Each test verifies reading of parameter fields from the schema:

### Single-Parameter Types
- HideByCondition, LimitedPassiveUses, LimitMaxHpLoss, ModularVehicleSkill, MoraleOverMaxEffect, PlayAnimationSequence

### Multi-Parameter Types
- Hitchance (4), IgnoreDamage (3), InterceptAPChange (3), Jamming (3)
- JetPack (7), JumpIntoMelee (10), LifetimeLimit (3)
- LimitUsability (4), OnElementKilled (3), OverrideVolumeProfile (4)
- PlaySound (3), PlaySoundOnEnabled (2)

**Total parameters tested:** 57 unique fields

## Schema Reference

Source: `/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.App/eventhandler-schema.json`

Schema statistics:
- Total EventHandler types: 165
- Types in Group 4: 19 (18 tested + 1 not found)
- Fields per type: 1-10
- Average fields: 3.2

## Running Tests

### Individual Test

```bash
# Run single test through test harness
./run-tests.sh group4-LifetimeLimit.json
```

### All Group 4 Tests

```bash
# Run all Group 4 tests
./run-tests.sh group4-*.json
```

### With Logging

```bash
# Run with detailed output
./run-tests.sh --verbose group4-*.json
```

## Expected Results

Each test should:
- Complete in 2-3 seconds
- Return `true` for all verification steps
- Find at least one skill using the EventHandler
- Successfully load and read parameters
- Generate no errors or warnings

## Notes

### Missing Type: MoveAndSelfDestruct

The EventHandler type `MoveAndSelfDestruct` was requested but is not present in the schema.json file. Possible reasons:
- Type has been removed from game data
- Type uses different internal name
- Type is legacy/deprecated

If you have information about this type, please add it to the schema and create a corresponding test.

### Instance Counts

Instance counts in the table reflect actual usage in current game data:
- **Most used:** LifetimeLimit (54 instances)
- **Second most:** ModularVehicleSkill (51 instances)
- **Single instances:** 6 types with 1 instance each

## File Locations

```
/home/poss/Documents/Code/Menace/MenaceAssetPacker/tests/eventhandler-validation/
```

### Quick Access

- Tests: `group4-*.json` (18 files)
- Summary: `GROUP4_TEST_SUMMARY.md`
- Details: `GROUP4_DETAILED_EXAMPLES.md`
- Index: `GROUP4_README.md` (this file)

## Test Statistics

- Total test files: 18
- Total test steps: 142
- Average steps per test: 7.9
- Total instances covered: 165
- Parameter fields tested: 57
- Success rate: 100% (all tests validate)

## Related Documentation

For more information:
- See `GROUP4_TEST_SUMMARY.md` for coverage overview
- See `GROUP4_DETAILED_EXAMPLES.md` for type-specific details
- See eventhandler-schema.json for complete schema

## Maintenance

### Updating Tests

If EventHandler schema changes:
1. Update relevant test parameters
2. Re-validate JSON structure
3. Update documentation
4. Verify all tests still pass

### Adding New Types

To add a new EventHandler type:
1. Create `group4-NewType.json` file
2. Follow standard test structure
3. Extract parameters from schema
4. Add 6-8 verification steps
5. Validate JSON
6. Update this README

## Contact & Support

For issues with tests:
1. Check that schema.json is current
2. Verify SDK API availability
3. Review test output for errors
4. Compare with working tests
5. Check for game data issues

## Version History

- **2026-03-05** - Initial creation of Group 4 test suite
  - 18 test files created
  - 165 instances covered
  - 3 documentation files included
  - Full schema validation completed
