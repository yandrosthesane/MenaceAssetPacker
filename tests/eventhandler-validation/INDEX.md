# EventHandler Validation Tests - Complete Index

## Quick Navigation

### Test Organization by Group

- **Group 1:** (6 types) - Basic action handlers
- **Group 2:** (6 types) - Passive/buff handlers
- **Group 3:** (20 types) - Core gameplay handlers ← YOU ARE HERE
- **Group 4:** (TBD)
- **Group 5:** (TBD)
- **Group 6:** (TBD)

---

## Group 3 Test Files (20 types)

### Category: Damage Handlers

| File | Type | Instances | Sample Skill | Parameters |
|------|------|-----------|--------------|------------|
| group3-Damage.json | Damage | 5 | active.shoot_assault_rifle | DamageFlatAmount, DamageVisualizationType, ArmorPenetration |
| group3-DamageArmorDurability.json | DamageArmorDurability | 1 | effect.acid | DamageFlatAmount, DamagePercentageOfMaxDurability |
| group3-DamageOverTime.json | DamageOverTime | 7 | damage_effect.medium_fire | DamagePctMaxHitpoints, DamageVisualizationType |

### Category: Effect/Action Handlers

| File | Type | Instances | Sample Skill | Parameters |
|------|------|-----------|--------------|------------|
| group3-Deathrattle.json | Deathrattle | 7 | active.detonate_carrier | Skill, Chance |
| group3-DestroyProps.json | DestroyProps | 11 | active.explosive_charge | Radius |
| group3-DropRecoverableObject.json | DropRecoverableObject | 2 | effect.volatile_explosive | ObjectTileEffect, ObjectUnitHUDIcon |
| group3-EjectEntity.json | EjectEntity | 2 | active.ram_vehicle | IsSpecialAIVersion, ConferSkillToEjectedEntity |
| group3-EmitAura.json | EmitAura | 4 | effect.aura.inspiring_presence | SkillToAdd, ExcludeSelf |
| group3-EnemiesDropPickupOnDeath.json | EnemiesDropPickupOnDeath | 2 | effect.scavenger | Pickup, SpawnDelay |
| group3-GainActionPoints.json | GainActionPoints | 5 | active.quick_sprint | ActionPoints, AlwaysIncreaseMaxActionPoints |

### Category: Disable/Restriction Handlers

| File | Type | Instances | Sample Skill | Parameters |
|------|------|-----------|--------------|------------|
| group3-DisableByFlag.json | DisableByFlag | 1 | active.jetpack | Flag |
| group3-DisableItem.json | DisableItem | 1 | effect.tethered | RequiredLogic, RequiredTags |
| group3-DisableSkills.json | DisableSkills | 7 | effect.suppressed | SkillFilter, HideDisabledSkills |
| group3-DisplayText.json | DisplayText | 75 | effect.overwatch | DefaultText, LocaKey, Event |

### Category: Filter/Condition Handlers

| File | Type | Instances | Sample Skill | Parameters |
|------|------|-----------|--------------|------------|
| group3-FilterByCondition.json | FilterByCondition | 5 | active.brace_for_impact | Condition |
| group3-FilterByMorale.json | FilterByMorale | 2 | effect.desperate | AppliesOnlyToState |
| group3-FilterByStance.json | FilterByStance | 3 | effect.ready_aim | AppliesOnlyToStance |
| group3-GainEffectOnSkillUse.json | GainEffectOnSkillUse | 6 | effect.firing_line | Effect, SkillFilter |
| group3-GrantBonusTurn.json | GrantBonusTurn | 1 | effect.blaze_of_glory | ActionPointsAvailable |

### Category: Specialized Handlers

| File | Type | Instances | Sample Skill | Parameters |
|------|------|-----------|--------------|------------|
| group3-HeatCapacity.json | HeatCapacity | 10 | active.shoot_laser_rifle | MaxHeat, HeatPerUse, HeatDissipationPerTurn |

---

## Documentation Files

### GROUP3_TEST_SUMMARY.md
Comprehensive test documentation including:
- Detailed description of each test
- Parameter listings with types
- API usage examples
- Schema statistics
- Notes on test structure

### EVENTHANDLER_GROUP3_COMPLETION_REPORT.md
Executive report including:
- Project status and summary
- Detailed status of each type
- Coverage statistics
- Validation checklist
- Quality metrics
- Next steps recommendations

### INDEX.md (This File)
Navigation guide and quick reference for all Group 3 tests

---

## Running the Tests

### Prerequisites
- Menace Asset Packer environment loaded
- Game templates available via Menace.SDK.Templates
- Test runner supporting REPL and command steps

### Test Execution
Each test can be run independently:

```bash
# Run a single test
test.load tests/eventhandler-validation/group3-Damage.json

# Run all Group 3 tests
for test in tests/eventhandler-validation/group3-*.json; do
  test.load "$test"
done
```

### Expected Output
Each test should produce:
- ✓ Command executed (test.goto_main)
- ✓ Wait completed (2000ms)
- ✓ Skill found with handler type
- ✓ EventHandlers array loaded
- ✓ _type field verified
- ✓ Parameters read successfully

---

## API Reference

### Core API Methods

#### FindAll
```csharp
Menace.SDK.Templates.FindAll("SkillTemplate")
// Returns: Array of skill templates
```

#### GetProperty
```csharp
// Get top-level property
Menace.SDK.Templates.GetProperty("SkillTemplate", skillName, "EventHandlers")

// Get handler property
Menace.SDK.Templates.GetProperty(
    "SkillTemplate",
    skillName + "|EventHandlers[0]",
    "_type")

// Get handler parameter
Menace.SDK.Templates.GetProperty(
    "SkillTemplate",
    skillName + "|EventHandlers[0]",
    "ParameterName")
```

### Common Parameter Types

| Type | Examples | Notes |
|------|----------|-------|
| String | "effect.berserk", "Blaze of Glory!" | Text parameters |
| Float | 0.15, 1000.0, -50.0 | Decimal values |
| Integer | 100, 40, 32768 | Whole numbers |
| Enum | 0, 1, 2, 3 | Choice values |
| Array | [tag1, tag2], [] | Collections |
| Object | {bankId: 7, itemId: 1668096867} | Complex objects |

---

## Statistics

### Total Coverage
- **EventHandler Types:** 20/20 (100%)
- **Test Files:** 20
- **Schema Instances:** 137 total
- **Average Instances per Type:** 6.85

### Most Used Handlers
1. DisplayText (75 instances - 54.7%)
2. DestroyProps (11 instances - 8.0%)
3. HeatCapacity (10 instances - 7.3%)
4. DamageOverTime (7 instances - 5.1%)
5. Deathrattle (7 instances - 5.1%)

### Least Used Handlers
- DisableByFlag (1 instance)
- DisableItem (1 instance)
- GrantBonusTurn (1 instance)

---

## Common Issues & Troubleshooting

### Issue: "Template not found"
- Check spelling of skill name
- Verify skill is in SkillTemplate database
- Use FindAll() to discover available skills

### Issue: "_type field is null"
- Verify EventHandlers array is not empty
- Check array index is valid (0 to Count-1)
- Some skills may not have EventHandlers

### Issue: "Parameter not found"
- Verify parameter name spelling (case-sensitive)
- Check schema for valid parameters
- Some parameters may be optional/nullable

### Issue: "Array is empty"
- Script may be searching wrong collection
- Try using FindAll() to locate alternative skills
- Verify handler type exists in game data

---

## Best Practices

1. **Run in Order:** Execute tests in numerical order (Damage → HeatCapacity)
2. **Check Dependencies:** Some tests may require game state setup
3. **Validate Output:** Check that parameters return non-null values
4. **Review Schema:** Reference eventhandler-schema.json for field info
5. **Document Results:** Keep test execution logs for debugging

---

## Related Files

- Schema: `/src/Menace.Modkit.App/eventhandler-schema.json`
- Template Tests: `/tests/template-validation/*.json`
- API Docs: Menace.SDK documentation
- Examples: `/examples/` directory

---

## Version Information

- **Suite Version:** 1.0
- **Created:** 2026-03-05
- **Compatible:** Menace Asset Packer v32.0.6+
- **Test Framework:** REPL + Command execution
- **Total Files:** 23 (20 tests + 3 docs)

---

## Feedback & Updates

When new EventHandler types are added:
1. Add entry to eventhandler-schema.json
2. Create corresponding test file
3. Update this index
4. Update GROUP3_TEST_SUMMARY.md if applicable

---

**Last Updated:** 2026-03-05
**Maintainer:** Menace Development Team
**Status:** Complete & Ready for Testing
