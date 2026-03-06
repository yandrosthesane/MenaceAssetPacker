# EventHandler Schema Analysis Report

**Generated:** 2026-03-04
**Data Source:** PerkTemplate.json and SkillTemplate.json
**Output Schema:** `eventhandler-schema.json`

---

## Executive Summary

This report documents the comprehensive analysis of all EventHandler instances found in Menace's extracted game data. The schema provides type information, field definitions, and enum value ranges for all 117 EventHandler types discovered.

### Key Metrics

- **Total EventHandler Types:** 117
- **Total EventHandler Instances Analyzed:** 1,289
  - From PerkTemplate.json: 121 perks
  - From SkillTemplate.json: 589 skills
- **Total Unique Fields:** 374 fields across all types

### Field Type Distribution

| Type   | Count | Description |
|--------|-------|-------------|
| enum   | 194   | Numeric fields with limited distinct values (likely game constants) |
| string | 68    | Text fields (skill IDs, localization keys, etc.) |
| int    | 36    | Integer numeric fields |
| float  | 34    | Floating-point numeric fields |
| array  | 22    | List/array fields (tags, conditions, etc.) |
| object | 16    | Complex nested structures (vectors, curves, sound refs) |
| null   | 4     | Fields that are always null in observed data |

---

## Top 10 Most Common EventHandler Types

| Rank | Type | Instances | Description |
|------|------|-----------|-------------|
| 1 | Attack | 206 | Primary attack/damage system |
| 2 | ChangeProperty | 192 | Stat modifications |
| 3 | AddSkill | 118 | Apply additional skills/effects |
| 4 | DisplayText | 75 | UI text and tooltips |
| 5 | ChangePropertyConditional | 66 | Conditional stat changes |
| 6 | LifetimeLimit | 54 | Duration-limited effects |
| 7 | ModularVehicleSkill | 51 | Vehicle-specific skills |
| 8 | ClearTileEffectGroup | 49 | Remove area effects |
| 9 | SpawnTileEffect | 32 | Create area effects |
| 10 | AttackProc | 26 | Triggered attack effects |

---

## Enum Fields (Game Constants)

The analysis identified **194 enum fields** across all EventHandler types. These fields use numeric values that represent game constants or configuration options.

### Common Enum Patterns

#### Event Timing (found in multiple types)
- **AddSkill.Event:** `[0, 1, 2, 3, 4, 5, 6]` - When to apply the skill
- **DisplayText.Event:** `[0, 1, 2, 3]` - When to show text
- **SpawnTileEffect.Event:** `[0, 1, 2]` - When to spawn effect
- **LifetimeLimit.Event:** `[0, 1, 2, 3, 4]` - When to count lifetime

#### Boolean-like Enums (0 or 1)
Many fields use 0/1 values, likely representing false/true:
- **AddSkill.OnlyApplyOnHit:** `[0, 1]`
- **AddSkill.ShowHUDText:** `[0, 1]`
- **Attack.ApplyMode:** `[0, 1]`
- **DisableSkills.HideDisabledSkills:** `[0, 1]`
- **SpawnEntity.SpawnAsMinion:** `[0, 1, 2]` (extended to 3 states)

#### Damage Visualization
- **Attack.DamageVisualizationType:** `[0, 1]`
- **Attack.FatalityType:** `[0, 1, 2, 3, 4]`
- **Attack.DismemberArea:** `[0, 1, 8]`
- **Attack.ElementsHit:** `[0, 1, 2, 3, 9]`

#### Multiplier Enums
These fields often have a default value of 1.0:
- **Attack.AccuracyMult:** `[0.8, 0.85, 0.9, 1.0]`
- **Attack.DamageMult:** `[0.7, 1.0, 1.2]`
- **Attack.ArmorPenetrationMult:** `[0.0, 1.0]`

#### Dropoff Values
Negative dropoff values indicate range-based degradation:
- **Attack.AccuracyDropoff:** `[-12.0, -9.0, -8.0, -6.0, -5.0, -4.0, -3.0, 0.0, 1.0]`
- **Attack.DamageDropoff:** `[-1.5, -1.3, -1.0, -0.8, 0.0, 0.8]`
- **Attack.ArmorPenetrationDropoff:** `[-5.0, -2.5, -2.0, -1.6, -1.5, 0.0, 1.8]`

---

## Complex Field Types

### Array Fields (22 total)

Arrays are used for collections of related data, primarily tags and filters:

**Tag Arrays:**
- `AddSkill.TargetCannotHaveOneOfTheseTags` (string[])
- `AddSkill.TargetRequiresOneOfTheseTags` (string[])
- `Attack.TargetCannotHaveOneOfTheseTags` (string[])
- `Attack.TargetRequiresOneOfTheseTags` (string[])
- `DisableItem.ForbiddenTags` (string[])
- `DisableItem.RequiredTags` (string[])

**Skill Arrays:**
- `RemoveSkill.SkillsToRemove` (string[])
- `ToggleSkills.ActiveSkills` (array)

**Configuration Arrays:**
- `ChangeAPBasedOnHP.Thresholds` (array)
- `ChangeMovementCost.CostPerSurface` (array)
- `ChangePropertyAura.Properties` (array)
- `ChangePropertyConditional.Properties` (array)
- `ChangePropertyTarget.Properties` (array)

**Condition Arrays:**
- `ShowUnitHUDIcon.Conditions` (array)
- `LimitUsability.HiddenIfActorHasSkill` (array)
- `LimitUsability.NotHiddenIfActorHasSkill` (array)
- `LimitUsability.NotUsableIfActorHasSkill` (array)
- `LimitUsability.OnlyUsableIfActorHasSkill` (array)

### Object Fields (16 total)

Objects represent complex nested structures:

**3D Transforms:**
- `AttachTemporaryPrefab.LocalOffset` (Vector3)
- `AttachTemporaryPrefab.LocalRotation` (Quaternion)
- `AttachTemporaryPrefab.LocalScale` (Vector3)
- `SpawnGore.MinMaxScale` (MinMax)

**Animation Curves:**
- `JetPack.ForwardCurve` (AnimationCurve)
- `JetPack.HeightCurve` (AnimationCurve)
- `JumpIntoMelee.ForwardCurve` (AnimationCurve)
- `JumpIntoMelee.HeightCurve` (AnimationCurve)

**Sound References:**
- `PlaySound.SoundToPlay` (SoundReference)
- `PlaySoundOnEnabled.SoundToPlay` (SoundReference)
- `Regeneration.Sound` (SoundReference)
- `HeatCapacity.OverheatingSound` (SoundReference)
- `Jamming.JammingSound` (SoundReference)
- `Suppression.SoundWhenPinnedDown` (SoundReference)
- `Suppression.SoundWhenSuppressed` (SoundReference)
- `SuppressionConstruct.SoundWhenPinnedDown` (SoundReference)

---

## Types Requiring Manual Review

The following EventHandler types have very few instances (< 3) in the game data. These may need manual review to ensure the schema is complete:

### Single Instance Types (37 types)

These types appear only once in the entire dataset:

1. ApplyAuthorityDisciplineMod
2. Berserk
3. CameraShake
4. ChangeAttackCost
5. ChangeHeatCapacity
6. ChangePropertyAura
7. ChangePropertyConsecutive
8. ChangeSkillUseAmount
9. Charge
10. ClearTileEffect
11. CommandUseSkill
12. DamageArmorDurability
13. DisableByFlag
14. DisableItem
15. GrantBonusTurn
16. HideByCondition
17. Hitchance
18. JumpIntoMelee
19. LimitedPassiveUses
20. MoraleOverMaxEffect
21. MoveAndSelfDestroy
22. OverrideVolumeProfile
23. PlaySoundOnEnabled
24. Rally
25. RecallTarget
26. ReduceArmor
27. RestoreArmorDurability
28. ReturnOfServe
29. ShowHUDIcon
30. SpawnGore
31. SpawnPrefab
32. Suppression
33. SuppressionConstruct
34. ToggleSkills
35. TriggerSkillOnHpLost
36. UseSkill
37. VehicleMovement

### Two Instance Types (26 types)

These types appear twice in the dataset:

1. AddSkillOnUse
2. AttackOrder
3. CauseDefect
4. ChangeAPBasedOnHP
5. ChangeDropChance
6. ChangeGrowthPotential
7. ChangeMalfunctionChance
8. ChangePropertyTarget
9. ChangeSkillsWithTags
10. ChangeUsesPerSquaddie
11. ChargeInfantry
12. CommandMove
13. DropRecoverableObject
14. EjectEntity
15. EnemiesDropPickupOnDeath
16. FilterByMorale
17. InterceptAPChange
18. JetPack
19. LimitMaxHpLoss
20. OnElementKilled
21. RefillSquaddies
22. RemoveDefect
23. SetAnimatorBool
24. SetUsesPerElement
25. StanceDeployed
26. ThrivingUnderPressure

**Recommendation:** These rare types may represent:
- Special case mechanics
- Cut/unused features
- Boss-specific abilities
- Expansion/DLC content
- Debug/testing features

---

## Notable EventHandler Types

### Attack (206 instances)

The most complex and commonly used EventHandler type with 39 distinct fields:

**Core Damage:**
- `Damage`, `DamageMult`, `DamagePctCurrentHitpoints`, `DamagePctMaxHitpoints`

**Accuracy System:**
- `AccuracyBonus`, `AccuracyMult`, `AccuracyDropoff`, `AccuracyDropoffMult`

**Armor Penetration:**
- `ArmorPenetration`, `ArmorPenetrationMult`, `ArmorPenetrationDropoff`
- `DamageToArmorDurability`, `DamageToArmorDurabilityDropoff`

**Range Dropoff:**
- Multiple dropoff fields for damage, accuracy, and armor penetration at range

**Visual Effects:**
- `DamageVisualizationType`, `FatalityType`, `DismemberArea`, `ElementsHit`

**Environmental:**
- `DestroyHalfCover`, `IsHalfCoverDestroyedOnAOECenterTileOnly`

**Suppression:**
- `SuppressionDealtMult`, `SuppressionDropoffAOE`

### ChangeProperty (192 instances)

Stat modification system with:
- `Property` (string) - Which property to modify
- `Amount` (float) - Modification amount
- `Trigger` (enum: 0, 1, 2) - When to apply
- `TooltipPlaceholderIndex` (enum: -1, 0) - UI display index
- `IncludePlusSign` (enum: 0, 1) - Format positive values with +

### DisplayText (75 instances)

UI text system:
- `Event` (enum: 0, 1, 2, 3) - When to display
- `LocaKey` (string) - Localization key
- `DefaultText` (string, nullable) - Fallback text
- `Icon` (string, nullable) - Icon reference

---

## Common String Patterns

### Skill References
Many string fields reference game skills/effects:
- `AddSkill.SkillToAdd`: e.g., "effect.berserk", "effect.drive_by"
- `RemoveSkill.SkillsToRemove`: Array of skill IDs

### Filters and Conditions
Some fields use special markers for dynamic content:
- `"(ITacticalCondition)"` - Placeholder for condition logic
- `"(ISkillFilter)"` - Placeholder for skill filtering

### Localization Keys
- Typically prefixed with category: e.g., "skill.", "effect.", "perk."

---

## Schema Usage

The generated `eventhandler-schema.json` file can be used for:

1. **Modding Tools:** Provide autocomplete and validation for EventHandler creation
2. **Documentation:** Auto-generate documentation for modders
3. **Type Checking:** Validate modded content before game import
4. **Code Generation:** Generate C# classes or TypeScript interfaces
5. **IDE Support:** Enable IntelliSense in JSON/YAML editors

### Schema Structure

```json
{
  "eventHandlerTypes": {
    "TypeName": {
      "fields": {
        "FieldName": {
          "type": "string|int|float|bool|enum|array|object",
          "nullable": true,           // Optional: field can be null
          "values": [0, 1, 2],        // For enum types: possible values
          "elementType": "string",    // For array types: element type
          "commonValues": ["a", "b"], // For strings: frequently seen values
          "sample": <value>           // Example value from real data
        }
      },
      "instanceCount": 123
    }
  }
}
```

---

## Ambiguous/Problematic Findings

### Fields with Unknown Element Types

Some arrays had mixed or undetermined element types in the limited sample data:
- Check individual type definitions in the schema for `"elementType": "mixed"`

### Nullable Fields Without Clear Pattern

15 fields are marked as nullable, but the pattern isn't always clear:
- Some may be optional configuration
- Others may be legacy/deprecated fields
- Review specific usage in game data for context

### High Enum Value Ranges

Some enum fields have many possible values:
- **AddSkill.Event:** 7 values (0-6)
- **Attack.ElementsHit:** 5 distinct values including 9
- **Attack.FatalityType:** 5 values (0-4)

These may actually be bitmask flags rather than enums.

### Object Fields Without Type Info

The schema identifies object fields but doesn't provide internal structure. These need:
- Manual inspection of game data
- Decompilation of game code
- Separate schema generation for nested types

---

## Recommendations

1. **For Modders:**
   - Refer to the schema for required/optional fields
   - Use common types (Attack, ChangeProperty, AddSkill) as templates
   - Test rare types thoroughly as they have limited examples

2. **For Tool Developers:**
   - Implement schema validation before game import
   - Provide enum value dropdowns in editors
   - Handle nullable fields appropriately
   - Consider manual review for types with < 3 instances

3. **For Future Analysis:**
   - Cross-reference with game code to confirm enum meanings
   - Identify bitmask fields vs. true enums
   - Generate sub-schemas for nested object types
   - Track schema changes across game versions

4. **For Documentation:**
   - Create separate detailed docs for top 10 types
   - Document enum value meanings with code/community help
   - Provide working examples for each major type

---

## File Locations

- **Schema File:** `/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.App/eventhandler-schema.json`
- **Analysis Script:** `/home/poss/Documents/Code/Menace/MenaceAssetPacker/scripts/analyze_eventhandlers.py`
- **Source Data:**
  - `/home/poss/.steam/debian-installation/steamapps/common/Menace/UserData/ExtractedData/PerkTemplate.json`
  - `/home/poss/.steam/debian-installation/steamapps/common/Menace/UserData/ExtractedData/SkillTemplate.json`

---

## Conclusion

This comprehensive schema provides a solid foundation for understanding and working with Menace's EventHandler system. With 117 types and 374 unique fields documented, modders and tool developers now have detailed type information for creating and validating game content.

The high number of enum fields (194) indicates a well-structured, configuration-driven system. The presence of complex types (arrays and objects) shows sophisticated gameplay mechanics around targeting, conditions, and audio-visual effects.

While most types are well-represented in the data (top 10 types account for 869 of 1,289 instances), the 63 types with fewer than 3 instances warrant additional investigation to ensure complete schema coverage.
