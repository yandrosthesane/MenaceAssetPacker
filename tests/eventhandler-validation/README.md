# EventHandler Validation Tests

## Overview

This directory contains comprehensive validation tests for all 109 EventHandler types found in Menace's game data.

**Total Coverage:** 109/117 types (93.2%)
- Missing: MoveAndSelfDestruct (does not exist in current schema)
- Instances Covered: 1,289 EventHandler instances

## Quick Start

### Run All Tests (6 batches)

```bash
# Run each group sequentially
test_run tests/eventhandler-validation/RUN_GROUP1.json
test_run tests/eventhandler-validation/RUN_GROUP2.json
test_run tests/eventhandler-validation/RUN_GROUP3.json
test_run tests/eventhandler-validation/RUN_GROUP4.json
test_run tests/eventhandler-validation/RUN_GROUP5.json
test_run tests/eventhandler-validation/RUN_GROUP6.json
```

### Run Individual Groups

Each batch file tests 12-20 types in ~10-15 seconds:

- **RUN_GROUP1.json** - 20 types (AddItemSlot → ChangeAttackCost)
- **RUN_GROUP2.json** - 19 types (ChangePropertyAura → Cooldown)
- **RUN_GROUP3.json** - 20 types (Damage → HeatCapacity)
- **RUN_GROUP4.json** - 18 types (HideByCondition → PlaySoundOnEnabled)
- **RUN_GROUP5.json** - 20 types (Rally → SpawnGore)
- **RUN_GROUP6.json** - 12 types (SpawnPrefab → VentHeat)

## Test Structure

### Batch Tests (RUN_GROUP*.json)

Each batch test:
1. Navigates to main menu once
2. Loads all SkillTemplate/PerkTemplate instances
3. Searches for each EventHandler type
4. Reports pass/fail for each type
5. Returns overall success (all types found)

**Advantages:**
- Fast (single scene load)
- Clear reporting
- Easy to run

### Individual Tests (group*-*.json)

Each individual test file (109 total):
1. Navigates to main menu
2. Finds specific skill/perk using the EventHandler type
3. Loads EventHandlers array
4. Verifies _type field
5. Reads 1-3 parameter fields

**Use when:**
- Debugging a specific type
- Need detailed parameter validation
- Investigating failures

## File Naming

- **Batch tests:** `RUN_GROUP{N}.json` (6 files)
- **Individual tests:** `group{N}-{TypeName}.json` (109 files)

## Expected Results

### All Groups Should Pass

Each group test should report:
```
Group N Results: X/X types found
  TypeName1: ✓
  TypeName2: ✓
  ...
```

### Known Limitation

**MoveAndSelfDestruct** does not exist in the current game (schema shows 0 instances).
- This is expected and not a test failure
- May be legacy/removed functionality

## What Gets Tested

### Discovery Test
- Can we find at least one skill/perk using this EventHandler type?
- Tests that the type exists in game data

### Loading Test
- Can we load the EventHandlers array via Templates.GetProperty()?
- Tests SDK functionality

### Structure Test
- Is the _type field accessible?
- Tests reflection/IL2CPP casting

### Parameter Test (individual tests only)
- Can we read type-specific parameter fields?
- Tests that EventHandler data is accessible

## Troubleshooting

### "0/20 types found"

**Cause:** Templates not loaded
**Fix:** Wait longer after scene load, or check if game data extracted

### "Cannot cast to Il2CppSystem.Object[]"

**Cause:** IL2CPP casting issue
**Fix:** Verify ModpackLoader SDK is loaded correctly

### Individual test fails but batch passes

**Expected:** Batch tests only verify type exists, individual tests read parameters
**Action:** Check parameter field names in eventhandler-schema.json

## Coverage Report

### By Instance Count (Top 10)

1. Attack - 206 instances ✓
2. ChangeProperty - 192 instances ✓
3. AddSkill - 118 instances ✓
4. DisplayText - 75 instances ✓
5. ChangePropertyConditional - 66 instances ✓
6. LifetimeLimit - 54 instances ✓
7. ModularVehicleSkill - 51 instances ✓
8. ClearTileEffectGroup - 49 instances ✓
9. SpawnTileEffect - 32 instances ✓
10. AttackProc - 26 instances ✓

**Top 10 = 67% of all EventHandler instances**

### By Rarity

- **Common (10+ instances):** 30 types
- **Uncommon (3-9 instances):** 24 types
- **Rare (1-2 instances):** 55 types

## Documentation

- **eventhandler-schema.json** - Complete type definitions
- **eventhandler-schema-report.md** - Analysis and statistics
- **eventhandler-schema-README.md** - Usage guide
- **working-docs/EVENTHANDLER_SCHEMA_STATUS.md** - Project status

## Next Steps

1. Run all 6 batch tests
2. Verify 100% pass rate
3. Investigate any failures
4. Document results in working-docs/
5. Update modding guides with EventHandler examples

## Estimated Runtime

- **Each batch test:** 10-15 seconds
- **All 6 batches:** ~1-2 minutes total
- **Individual tests (if needed):** ~5-10 minutes for all 109

**Recommendation:** Run batch tests first for quick validation, then run individual tests only if debugging specific types.
