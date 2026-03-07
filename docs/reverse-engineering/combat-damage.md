# Combat Damage System

## Overview

The damage system calculates damage dealt from attacks, considering weapon stats, armor, armor penetration, and various modifiers. This document also covers debuff mechanics, damage reduction handlers, and status effect systems that modify combat outcomes.

## Namespace Reference

- **Effect Handlers**: `Menace.Tactical.Skills.Effects`
- **Analysis Tool**: Ghidra MCP

## Core Damage Functions

### EntityProperties$$GetDamage (0x18060bd60)

```c
float GetDamage(EntityProperties props) {
    float baseDamage = props.BaseDamage;      // +0x118
    float damageMult = Clamped(props.DamageMult);  // +0x11c
    return baseDamage * damageMult;
}
```

### EntityProperties$$GetDamageDropoff (0x18060bcd0)

```c
float GetDamageDropoff(EntityProperties props) {
    float baseDropoff = props.DamageDropoffBase;  // +0x120
    float dropoffMult = Clamped(props.DamageDropoffMult);  // +0x124
    return baseDropoff * dropoffMult;
}
```

### EntityProperties$$GetArmor (0x18060bb00)

Returns effective armor value from multiple armor zones.

```c
int GetArmor(EntityProperties props) {
    // Get max of all armor zones
    int armor = props.ArmorFront;    // +0x20
    if (armor <= props.ArmorSide) {  // +0x24
        armor = props.ArmorSide;
    }
    if (armor < props.ArmorBase) {   // +0x1c
        armor = props.ArmorBase;
    }

    float armorMult = Clamped(props.ArmorMult);  // +0x28
    return (int)(armorMult * (float)armor);
}
```

### EntityProperties$$GetArmorValue (0x18060bae0)

Gets armor for a specific facing direction.

```c
int GetArmorValue(EntityProperties props, int direction) {
    switch (direction) {
        case 0:  return props.ArmorBase;   // +0x1c
        case 1:  return props.ArmorFront;  // +0x20
        case 2:  return props.ArmorSide;   // +0x24
        default: return props.ArmorBase;
    }
}
```

### EntityProperties$$GetArmorPenetration (0x18060bab0)

```c
float GetArmorPenetration(EntityProperties props) {
    float basePen = props.ArmorPenBase;      // +0x100
    float penMult = Clamped(props.ArmorPenMult);  // +0x104
    return basePen * penMult;
}
```

### EntityProperties$$GetDamageToArmorDurability (0x18060bd30)

Damage that degrades armor durability (anti-armor damage).

```c
float GetDamageToArmorDurability(EntityProperties props) {
    float baseDmg = props.ArmorDurabilityDmgBase;  // +0x12c (300 decimal)
    float dmgMult = Clamped(props.ArmorDurabilityDmgMult);  // +0x130
    return baseDmg * dmgMult;
}
```

### EntityProperties$$GetDamageToArmorDurabilityDropoff (0x18060bd00)

```c
float GetDamageToArmorDurabilityDropoff(EntityProperties props) {
    float baseDropoff = props.ArmorDurDmgDropoffBase;  // +0x134
    float dropoffMult = Clamped(props.ArmorDurDmgDropoffMult);  // +0x138
    return baseDropoff * dropoffMult;
}
```

## DamageHandler$$ApplyDamage (0x180702970)

Main function that applies damage from a skill to an entity.

Key operations:
1. Gets attack EntityProperties from skill
2. Calculates final damage values with range modifiers
3. Creates DamageInfo struct
4. Applies damage to target entity

```c
// Pseudocode from decompilation
void ApplyDamage(SkillEventHandler handler) {
    Entity target = handler.GetEntity();
    EntityProperties attackProps = handler.AttackProperties;  // +0x18

    // Calculate damage with range modifier
    int distance = target.DistanceToAttacker;  // +0x14
    float baseDmg = attackProps.BaseDamage * distance;  // +0x68
    float minDamage = attackProps.MinDamage;  // +0x6c
    if (minDamage <= baseDmg) {
        minDamage = baseDmg;
    }

    // Similar for armor durability damage
    float armorDmg = target.ArmorElements * attackProps.ArmorDurabilityDmg;  // +0x70
    float minArmorDmg = attackProps.MinArmorDmg;  // +0x74

    // Create DamageInfo
    DamageInfo dmgInfo = new DamageInfo();

    // Set shot count from attack props
    int shotCount = attackProps.ShotCount;  // +0x5c
    int targetElements = target.ElementCount;  // +0x18
    float shotsPerElement = attackProps.ShotsPerElement;  // +0x60
    int totalShots = ceil(targetElements * shotsPerElement) + shotCount;
    if (totalShots < 1) totalShots = 1;

    dmgInfo.TotalShots = totalShots;  // +0x1c
    dmgInfo.Damage = minDamage + attackProps.BaseDamageBonus + minArmorDmg;  // +0x64, +0xc
    dmgInfo.ArmorPen = attackProps.ArmorPenetration;  // +0x78 -> +0x14
    dmgInfo.ArmorDurDmg = attackProps.ArmorDurabilityDmg;  // +0x7c -> +0x18

    // Copy additional properties
    dmgInfo.Unknown1 = attackProps.Unknown1;  // +0x80 -> +0x18
    dmgInfo.Unknown2 = attackProps.Unknown2;  // +0x84 -> +0x20
    dmgInfo.Unknown3 = attackProps.Unknown3;  // +0x88 -> +0x24
    dmgInfo.Unknown4 = attackProps.Unknown4;  // +0x8c -> +0x1c
    dmgInfo.CanDismember = attackProps.CanDismember;  // +0x90 -> +0x2e

    // Apply damage to entity
    target.OnDamageReceived(handler.Skill, dmgInfo);
}
```

## WeaponTemplate$$ApplyToEntityProperties (0x180563080)

How weapons modify EntityProperties for attacks:

```c
void ApplyToEntityProperties(WeaponTemplate weapon, EntityProperties props) {
    // Accuracy
    props.BaseAccuracy += weapon.AccuracyBonus;           // +0x14c -> +0x68
    props.AccuracyDropoffBase += weapon.AccuracyDropoff;  // +0x150 -> +0x70

    // Damage
    props.BaseDamage += weapon.DamageBonus;               // +0x154 -> +0x118
    props.DamageDropoffBase += weapon.DamageDropoff;      // +0x158 -> +0x120

    // Suppression/Other
    props.Unknown1 += weapon.Unknown1;                    // +0x15c -> +0x144
    props.Unknown2 += weapon.Unknown2;                    // +0x160 -> +0x148
    props.Unknown3 += weapon.Unknown3;                    // +0x164 -> +0x14c
    props.Unknown4 += weapon.Unknown4;                    // +0x168 -> +0x150

    // Armor penetration
    props.ArmorPenBase += weapon.ArmorPenBonus;          // +0x16c -> +0x100
    props.ArmorPenDropoff += weapon.ArmorPenDropoff;     // +0x170 -> +0x108

    // Armor durability damage
    props.ArmorDurDmgBase += weapon.ArmorDurDmgBonus;    // +0x174 -> +0x12c
    AddMult(props.ArmorDurDmgMult, weapon.ArmorDurDmgMult);  // +0x178 -> +0x130
    props.ArmorDurDmgDropoff += weapon.ArmorDurDmgDropoffBonus;  // +0x17c -> +0x134
    AddMult(props.ArmorDurDmgDropoffMult, weapon.ArmorDurDmgDropoffMult);  // +0x180 -> +0x138

    // Other modifiers
    props.Unknown5 += weapon.Unknown5;                   // +0x184 -> +0xe0
}
```

## ArmorTemplate$$ApplyToEntityProperties (0x1805488f0)

How armor modifies defensive EntityProperties:

```c
void ApplyToEntityProperties(ArmorTemplate armor, EntityProperties props) {
    // Base armor values (all zones get same bonus)
    props.ArmorBase += armor.ArmorBonus;   // +0x190 -> +0x1c
    props.ArmorFront += armor.ArmorBonus;  // +0x190 -> +0x20
    props.ArmorSide += armor.ArmorBonus;   // +0x190 -> +0x24

    // Armor durability
    props.ArmorDurability += armor.ArmorDurabilityBonus;  // +0x194 -> +0x2c

    // Dodge (inverted - high dodge penalty = low dodge)
    float dodgePenalty = Clamped(1.0 - armor.DodgePenalty);  // +0x198
    AddMult(props.DodgeMult, dodgePenalty);  // -> +0x8c

    // Health
    props.Health += armor.HealthBonus;  // +0x19c -> +0x14
    AddMult(props.HealthMult, armor.HealthMult);  // +0x1a0 -> +0x18

    // Accuracy (armor can affect aim)
    props.BaseAccuracy += armor.AccuracyBonus;  // +0x1a4 -> +0x68
    AddMult(props.AccuracyMult, armor.AccuracyMult);  // +0x1a8 -> +0x6c

    // Evasion
    AddMult(props.EvasionMult, armor.EvasionMult);  // +0x1ac -> +0x84

    // Various resistances
    props.ResistBase += armor.ResistBonus;  // +0x1b0 -> +0xa0
    AddMult(props.ResistMult, armor.ResistMult);  // +0x1b4 -> +0xa4

    // Morale
    props.MoraleBase += armor.MoraleBonus;  // +0x1b8 -> +0xc4
    AddMult(props.MoraleMult, armor.MoraleMult);  // +0x1bc -> +0xc8

    // Suppression resistance
    props.SuppressionResist += armor.SuppressionResistBonus;  // +0x1c0 -> +0xcc
    AddMult(props.SuppressionResistMult, armor.SuppressionResistMult);  // +0x1c4 -> +0xd0

    // Other
    props.Unknown += armor.Unknown;  // +0x1c8 -> +0xd4
    AddMult(props.UnknownMult, armor.UnknownMult);  // +0x1cc -> +0xd8
    AddMult(props.MoveSpeedMult, armor.MoveSpeedMult);  // +0x1d0 -> +0xdc

    // Additional bonuses
    props.SightRange += armor.SightRangeBonus;  // +0x1d4 -> +0x154
    AddMult(props.SightRangeMult, armor.SightRangeMult);  // +0x1d8 -> +0x158

    props.ActionPoints += armor.ActionPointBonus;  // +0x1dc -> +0x34
    props.MovementPoints += armor.MovementBonus;   // +0x1e4 -> +0x3c
    AddMult(props.MovementMult, armor.MovementMult);  // +0x1e0 -> +0x38
}
```

## Struct Offsets

### EntityProperties - Damage Related
| Offset | Type | Field | Notes |
|--------|------|-------|-------|
| +0x14 | int | Health | Current/max health |
| +0x18 | float | HealthMult | Health multiplier |
| +0x1c | int | ArmorBase | Base armor value |
| +0x20 | int | ArmorFront | Frontal armor |
| +0x24 | int | ArmorSide | Side armor |
| +0x28 | float | ArmorMult | Armor multiplier |
| +0x2c | float | ArmorDurability | Current armor durability |
| +0x100 | float | ArmorPenBase | Armor penetration base |
| +0x104 | float | ArmorPenMult | Armor penetration multiplier |
| +0x108 | float | ArmorPenDropoff | Armor pen dropoff per tile |
| +0x118 | float | BaseDamage | Base damage value |
| +0x11c | float | DamageMult | Damage multiplier |
| +0x120 | float | DamageDropoffBase | Damage dropoff base |
| +0x124 | float | DamageDropoffMult | Damage dropoff multiplier |
| +0x12c | float | ArmorDurDmgBase | Anti-armor damage base |
| +0x130 | float | ArmorDurDmgMult | Anti-armor damage mult |
| +0x134 | float | ArmorDurDmgDropoff | Anti-armor dropoff base |
| +0x138 | float | ArmorDurDmgDropoffMult | Anti-armor dropoff mult |

### WeaponTemplate
| Offset | Type | Field | Notes |
|--------|------|-------|-------|
| +0x13C | int | MinRange | From schema |
| +0x140 | int | IdealRange | From schema |
| +0x144 | int | MaxRange | From schema |
| +0x14c | float | AccuracyBonus | Base accuracy bonus |
| +0x150 | float | AccuracyDropoff | Accuracy drop per tile |
| +0x154 | float | DamageBonus | Base damage bonus |
| +0x158 | float | DamageDropoff | Damage drop per tile |
| +0x16c | float | ArmorPenBonus | Armor penetration bonus |
| +0x170 | float | ArmorPenDropoff | AP drop per tile |
| +0x174 | float | ArmorDurDmgBonus | Anti-armor damage |
| +0x178 | float | ArmorDurDmgMult | Anti-armor multiplier |
| +0x17c | float | ArmorDurDmgDropoff | Anti-armor dropoff |
| +0x180 | float | ArmorDurDmgDropoffMult | Anti-armor dropoff mult |
| +0x184 | float | Unknown | Applied to +0xe0 |

### ArmorTemplate
| Offset | Type | Field | Notes |
|--------|------|-------|-------|
| +0x190 | int | ArmorBonus | Added to all armor zones |
| +0x194 | int | ArmorDurabilityBonus | Added to durability |
| +0x198 | float | DodgePenalty | Reduces dodge (inverted) |
| +0x19c | int | HealthBonus | Added to health |
| +0x1a0 | float | HealthMult | Health multiplier |
| +0x1a4 | int | AccuracyBonus | Added to accuracy |
| +0x1a8 | float | AccuracyMult | Accuracy multiplier |

### DamageInfo Struct
| Offset | Type | Field | Notes |
|--------|------|-------|-------|
| +0xc | int | Damage | Final damage value |
| +0x14 | int | ArmorPenetration | AP value |
| +0x18 | int | ArmorDurabilityDamage | Anti-armor damage |
| +0x1c | int | TotalShots | Number of shots/hits |
| +0x2e | byte | CanDismember | Whether can cause dismemberment |

## Damage Flow

1. **Skill activation** triggers `DamageHandler$$ApplyDamage`
2. **EntityProperties** are built from:
   - Entity base stats
   - Equipped weapon stats
   - Active skill effects
   - Buffs/debuffs
3. **Damage calculation** considers:
   - Base damage × damage mult
   - Distance penalty (if applicable)
   - Shot count and elements
4. **Armor resolution** (in target's OnDamageReceived):
   - Compare armor penetration vs armor value
   - Reduce damage based on armor effectiveness
   - Apply armor durability damage
5. **Final damage** applied to target health

---

## Debuff Handler System

### Base Classes

All skill effect handlers inherit from common base classes:

#### SkillEventHandler
Base class for all skill effect handlers.

| Offset | Type | Field |
|--------|------|-------|
| +0x10 | Skill* | skill_ptr |
| +0x18 | BaseEffect* | effect_data_ptr |

Helper methods: `GetEntity()`, `GetActor()`, `GetOwner()`

#### TileEffectHandler
Base class for tile-based effects.

| Offset | Type | Field |
|--------|------|-------|
| +0x10 | Tile* | tile_ptr |

### Event Lifecycle

Common event hooks for debuff handlers:

| Event | Description |
|-------|-------------|
| OnAdded | Called when effect first applied to entity |
| OnRemoved | Called when effect removed from entity |
| OnUpdate | Called each property update cycle |
| OnRoundStart | Called at start of tactical round |
| OnRoundEnd | Called at end of tactical round |
| OnTurnStart | Called at start of affected entity's turn |
| OnTurnEnd | Called at end of affected entity's turn |
| OnApply | Called when skill applies to tile/target |
| OnBeforeAnySkillUsed | Called before any skill is used |
| OnTargetHit | Called when attack hits target |
| OnBeingAttacked | Called when entity is being attacked |
| OnMissionStarted | Called at mission start |
| OnMissionFinished | Called at mission end |

---

## Damage Reduction Mechanics

### ReduceArmorHandler (0x1807186a0)

Reduces target armor effectiveness via stacking debuff.

**Handler Fields:**

| Offset | Type | Field |
|--------|------|-------|
| +0x18 | ReduceArmor* | effect_data_ptr |
| +0x20 | int | stack_count (default: 1) |

**Effect Data Fields:**

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | float | armor_reduction_pct | Percentage of armor reduced per stack |
| +0x5c | float | max_reduction | Maximum total armor reduction cap |

**Formula:**
```
reduction = min(StackCount * ArmorReductionPct, MaxReduction)
```

Events: `OnUpdate`

### ChangePropertyHandler (0x1806fee90)

Generic stat modifier for any entity property. Supports both additive and multiplicative modes.

**Handler Fields:**

| Offset | Type | Field |
|--------|------|-------|
| +0x18 | ChangeProperty* | effect_data_ptr |
| +0x20 | float | cached_value |
| +0x24 | int | last_value_provider_result |

**Effect Data Fields:**

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | int | trigger_type | 0=OnUpdate, 1=OnAdded |
| +0x5c | PropertyType | property_type | Enum for target property |
| +0x60 | int | value_additive | Flat value change |
| +0x64 | float | value_multiplicative | Multiplier value |
| +0x68 | IValueProvider* | value_provider | Optional dynamic scaler |
| +0x70 | int | tooltip_index | Display index (-1 = hidden) |
| +0x74 | bool | show_as_positive | Display formatting |

Uses `IsMultProperty()` to determine additive vs multiplicative mode.

Events: `OnUpdate`, `OnBeforeAnySkillUsed`, `OnBeingAttacked`

### ChangePropertyConditionalHandler (0x1806fe570)

Conditional stat modifier with activation requirements. Inherits from ChangePropertyHandler.

Events: `OnUpdate`, `OnBeforeAnySkillUsed`

---

## Armor Damage Formulas

### DamageOverTimeHandler (0x180702dc0)

Handles periodic damage (bleed, burn, poison effects).

**Handler Fields:**

| Offset | Type | Field |
|--------|------|-------|
| +0x10 | Skill* | skill_ptr |
| +0x18 | DamageOverTime* | effect_data_ptr |

**Effect Data Fields:**

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | int | flat_damage | Base flat damage per tick |
| +0x5c | float | percent_damage | Percentage of max HP per tick |
| +0x60 | float | flat_from_max_hp | Flat bonus from max HP |
| +0x64 | float | flat_from_max_hp_min | Minimum HP-based damage |
| +0x68 | float | percent_from_element_count | Scaling per element |
| +0x6c | float | min_from_element_count | Minimum element scaling |
| +0x74 | bool | armor_damage_only | Only damages armor durability |
| +0x78 | float | armor_dmg_flat | Flat armor damage |
| +0x7c | float | armor_dmg_pct_current | % of current armor as damage |
| +0x80 | float | armor_dmg_pct_from_element | Element-based armor damage |
| +0x84 | float | armor_dmg_flat_2 | Secondary flat armor damage |
| +0x88 | float | armor_dmg_pct_current_2 | Secondary current armor % |
| +0x8c | enum | fatality_type | Death animation type |
| +0x90 | int | element_hit_min_index | Minimum element index |
| +0x94 | bool | can_crit | Whether damage can crit |

**Damage Formula:**
```
damage = ceil(MaxHP * PercentDmg) + FlatDmg + ElementScaling
```

Events: `OnTurnEnd`

---

## Suppression System

### Suppression States

| State | Value | Effects |
|-------|-------|---------|
| Normal | 0 | No suppression effects |
| Suppressed | 1 | AP reduced by 30, accuracy penalty |
| Pinned | 2 | AP set to 0, cannot act unless has PinnedDown skill |

### SuppressionHandler (0x18071f790)

Core suppression state manager.

**Handler Fields:**

| Offset | Type | Field |
|--------|------|-------|
| +0x10 | Skill* | skill_ptr |
| +0x18 | Suppression* | effect_data_ptr |
| +0x20 | int | last_suppression_state |
| +0x24 | int | pinned_round_counter |

**Suppression Mechanics:**

| Stat | Value |
|------|-------|
| Suppressed AP Penalty | -30 |
| Pinned AP Value | 0 |
| Suppressed Accuracy Penalty | -0.15 (15%) |
| Pinned Mobility Penalty | Variable per round |

Events: `OnRoundStart`, `OnUpdate`

### ChangeSuppressionHandler (0x180700600)

Applies suppression value changes.

**Effect Data Fields:**

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | int | trigger_type | 0=OnApply, 1=OnAdded |
| +0x5c | int | suppression_value | Amount to change |
| +0x60 | bool | apply_to_origin | Apply to user instead of target |

Events: `OnAdded`, `OnApply`

### OnElementKilledHandler.ReduceSuppression (0x180717980)

Reduces suppression when killing enemies. Uses XOR to negate suppression value.

**Actor Suppression Fields:**

| Offset | Type | Field |
|--------|------|-------|
| +0x15c | float | current_suppression |

### Actor Suppression Properties

| Offset | Type | Field |
|--------|------|-------|
| +0x15c | float | suppression_value |
| entity_properties+0xEC bit 5 | flag | is_immune_flag |
| entity_properties+0xEC bit 6 | flag | is_pinned_immune_flag |

### Suppression Functions

| Function | Address |
|----------|---------|
| Actor.ApplySuppression | 0x1805ddda0 |
| Actor.ChangeSuppressionAndUpdateAP | 0x1805de3b0 |
| Actor.SetSuppression | 0x1805e76d0 |
| Actor.GetSuppressionState | 0x1805df730 |

---

## Action Point Debuffs

### ChangeActionPointsHandler (0x1806fd120)

Modifies available action points.

**Effect Data Fields:**

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | int | ap_delta | AP change amount (negative = reduction) |
| +0x5c | bool | force_skip_turn | Skip turn even if skills available |

Events: `OnAdded`, `OnRoundStart`, `OnUpdate`

**Mechanics:**
- Can be modified by `InterceptAPChangeHandler`
- Triggers `SkipTurn` if AP=0 and no usable skills (unless force flag set)

---

## Morale Debuffs

### ChangeMoraleHandler (0x1806fdd20)

Sets or modifies morale value.

**Effect Data Fields:**

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x5c | int | morale_target | 2=50% of max, 3=100% of max |
| +0x60 | int | condition_type | 1=only if above, 2=only if below |

Events: `OnAdded`, `OnApply`

### AttackMoraleHandler (0x1806fbb70)

Morale damage from attacks.

Events: `OnApply`

---

## Disable Effects

### DisableSkillsHandler (0x180704860)

Disables matching skills on target.

**Handler Fields:**

| Offset | Type | Field |
|--------|------|-------|
| +0x18 | DisableSkills* | effect_data_ptr |
| +0x20 | List<Skill>* | disabled_skills_list (cached for re-enabling) |

**Effect Data Fields:**

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | ISkillFilter* | skill_filter | Filter to match skills |
| +0x60 | bool | also_disable_usable_by_player | Additional disable flag |

**Skill Fields Modified:**

| Offset | Type | Field |
|--------|------|-------|
| +0x38 | bool | enabled |
| +0x98 | bool | usable_by_player |

Events: `OnAdded`, `OnRemoved`, `OnRoundStart`

### DisableItemHandler (0x1807042c0)

Disables item's skills.

**Item Fields:**

| Offset | Type | Field |
|--------|------|-------|
| +0x30 | IEnumerable<BaseSkill> | skills_list |

**Skill Fields Modified:**

| Offset | Type | Field |
|--------|------|-------|
| +0x38 | bool | disabled |

Events: `OnAdded`, `OnRemoved`

### DisableByFlagHandler (0x180704200)

Conditional disable based on flags.

Events: `IsEnabled` check

---

## Stun System

### Actor Stun Fields

| Offset | Type | Field |
|--------|------|-------|
| +0x16c | bool | is_visually_stunned |
| entity_properties+0xEC bit 0 | flag | is_stunned |

### Actor.IsStunned (0x1805e0860)

Returns true if visual stun flag OR property stun bit is set.

### DisabledOrStunnedCondition (0x1806d1680)

Skill condition checking stun state.

---

## Bleedout System

### BleedOutTileEffectHandler (0x180685910)

Manages unit bleedout countdown.

**Handler Fields:**

| Offset | Type | Field |
|--------|------|-------|
| +0x10 | Tile* | tile_ptr |
| +0x18 | int | rounds_remaining |
| +0x28 | BleedOutTileEffect* | effect_data_ptr |
| +0x30 | BaseUnitLeader* | unit_leader_ptr |
| +0x38 | BleedingWorldSpaceIcon* | world_space_icon |
| +0x40 | bool | was_tile_passable |

**Effect Data Fields:**

| Offset | Type | Field |
|--------|------|-------|
| +0xB8 | int | total_bleedout_rounds |
| +0xBC | Stem.ID | bleedout_tick_sound |
| +0xCC | Stem.ID | death_sound |

Events: `OnAdded`, `OnRoundStart`, `OnMissionPreFinished`, `OnRemoved`

**Mechanics:**
- OnAdded: Sets HealthStatus=1, creates UI icon, marks tile occupied
- OnRoundStart: Decrements counter, kills if 0, updates UI
- Revival: Via StopBleedoutHandler

### StopBleedoutHandler (0x18071e720)

Stabilizes bleeding out unit.

Events: `OnUse`, `OnVerifyTarget`

---

## Key Actor Properties Summary

| Offset | Type | Field |
|--------|------|-------|
| +0x5c | int | armor_durability |
| +0x15c | float | suppression_value |
| +0x160 | float | morale |
| +0x16c | bool | is_stunned_visual |

---

## TacticalManager Events

Events fired through TacticalManager for debuff notifications:

| Event | Address |
|-------|---------|
| OnSuppressionApplied | 0x180678770 |
| OnSuppressed | 0x1806786c0 |
| OnBleedingOut | 0x1806776f0 |
| OnArmorChanged | via InvokeOnArmorChanged |

---

## Related Function Addresses

### Suppression Functions

| Function | Address |
|----------|---------|
| Actor.ApplySuppression | 0x1805ddda0 |
| Actor.ChangeSuppressionAndUpdateAP | 0x1805de3b0 |
| Actor.SetSuppression | 0x1805e76d0 |
| Actor.GetSuppressionState | 0x1805df730 |

### Damage Functions

| Function | Address/Notes |
|----------|---------------|
| Entity.TakeDamage | Virtual at *entity+0x5A8 |
| Entity.SetArmorDurability | via decompilation |

### Action Point Functions

| Function | Address/Notes |
|----------|---------------|
| Actor.SetActionPoints | via decompilation |
| Actor.SkipTurn | via decompilation |

### Morale Functions

| Function | Address/Notes |
|----------|---------------|
| Actor.SetMorale | via decompilation |
| Actor.GetMoraleMax | via decompilation |
