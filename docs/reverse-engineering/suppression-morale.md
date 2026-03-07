# Suppression & Morale System

## Overview

Menace uses a suppression and morale system to model psychological effects in combat. Units can become suppressed from incoming fire and may break morale under sustained pressure.

## States

### SuppressionState Enum
```c
public enum SuppressionState {
    None = 0,       // Normal state
    Suppressed = 1, // Taking cover, reduced effectiveness
    PinnedDown = 2  // Severely impaired, cannot act
}
```

### MoraleState Enum
```c
public enum MoraleState {
    Panicked = 1,  // 0% morale (ratio <= 0), routing/fleeing
    Shaken = 2,    // 0% < morale <= ~50%, reduced effectiveness
    Steady = 3     // morale > ~50%, normal combat state
}
```

> **Note**: Earlier documentation referred to these as Fleeing/Wavering/Neutral. The actual enum values from Ghidra analysis confirm the names are Panicked/Shaken/Steady in `Menace.Tactical.MoraleState`.

#### Morale State Thresholds
| State | Condition | Notes |
|-------|-----------|-------|
| Panicked | `moraleRatio <= 0` | Commander units are immune |
| Shaken | `0 < moraleRatio <= ~0.5` | Threshold at `DAT_182d8fe40` |
| Steady | `moraleRatio > ~0.5` | Normal combat state |

### Morale Damage Flags
Bitmask flags passed to `ApplyMorale` to categorize damage source:

| Flag | Name | Description |
|------|------|-------------|
| `0x01` | DirectDamage | HP loss from attacks |
| `0x04` | AllyDeath | Positive morale for killing enemy |
| `0x08` | ElementDeath | Unit loses a soldier |
| `0x10` | KillBonus | Positive morale to killer |
| `0x20` | EnemyDeathNearby | Negative morale for enemies |
| `0x40` | AttackerMorale | Morale damage to attacker based on damage dealt |
| `0x100` | PanicAttack | Used by AttackMoraleHandler |

> **Flag Filtering**: Unit stats offset `0xA8` contains a mask; damage only applies if `(flag & mask) != 0`.

## Actor Class Fields

The `Actor` class (extends `Entity`) contains the core suppression/morale state:

| Offset | Type | Field | Notes |
|--------|------|-------|-------|
| +0x48 | byte | isAlive | Must be non-zero for morale operations |
| +0x4C | int | faction | 1=Player, 2=Enemy |
| +0x15C | float | m_Suppression | Current suppression value |
| +0x160 | float | m_Morale | Current morale value |
| +0x16A | byte | isInvulnerable | If non-zero, morale changes blocked |
| +0xD4 | int | m_CachedMoraleState | Cached MoraleState enum value |

## UnitStats Fields (Morale-Related)

The `UnitStats` class contains morale configuration for units:

| Offset | Type | Field | Notes |
|--------|------|-------|-------|
| +0x10 | int | elementCount | Number of soldiers (scales element death morale) |
| +0xA0 | float | baseMorale | Base maximum morale value |
| +0xA8 | uint | moraleFlags | Bitmask for morale damage type filtering |
| +0xAC | float | bonusMorale | Additional morale from buffs/equipment |
| +0xB0 | float | moraleMultiplier | Multiplier for max morale calculation |
| +0xB8 | float | outgoingMoraleMultiplier | Multiplier for morale damage dealt to enemies |
| +0xBC | float | moraleDamageMultiplier | Multiplier applied to incoming morale damage |
| +0xC0 | int | moraleStateModifier | Added to calculated morale state (shifts thresholds) |
| +0xEC bit 7 | bool | immuneToPanic | If set, actor cannot receive morale damage |
| +0xEC bit 8 | bool | immuneToDeathMorale | If set, death doesn't propagate morale effects |

## Key Methods

### Actor.GetSuppression (0x5DF7B0)
Returns the raw suppression value.

### Actor.GetSuppressionPct (0x5DF710)
Returns suppression as a percentage (0.0 - 1.0).

### Actor.GetSuppressionState (0x5DF730)
```c
SuppressionState GetSuppressionState(float additionalPct = 0) {
    float suppressionPct = GetSuppressionPct() + additionalPct;
    // Thresholds determined by TacticalConfig
    if (suppressionPct >= pinnedThreshold) return SuppressionState.PinnedDown;
    if (suppressionPct >= suppressedThreshold) return SuppressionState.Suppressed;
    return SuppressionState.None;
}
```

### Actor.ApplySuppression (0x5DDDA0)
```c
void ApplySuppression(float value, bool direct, Entity suppressor, Skill skill) {
    // Applies suppression damage to the actor
    // 'direct' flag determines if suppression resistance is applied
    // Updates suppression state and potentially triggers morale effects
}
```

### Actor.SetSuppression (0x5E76D0)
Directly sets suppression value.

### Actor.ChangeSuppressionAndUpdateAP (0x5DE3B0)
Changes suppression and updates action points accordingly (suppressed units have reduced AP).

### Actor.GetMorale (0x5DF5C0)
Returns the raw morale value.

### Actor.GetMoralePct (0x5DF4A0)
Returns morale as a percentage.

### Actor.GetMoraleMax (0x1805DF330)
```c
float GetMoraleMax() {
    return (BaseMorale + BonusMorale) * MoraleMultiplier;
}
```

### Actor.GetMoralePct (0x1805DF4A0)
```c
float GetMoralePct() {
    return currentMorale / GetMoraleMax();
}
```

### Actor.GetMoraleState (0x1805DF4D0)
```c
MoraleState GetMoraleState() {
    float ratio = currentMorale / maxMorale;
    int state;
    if (ratio <= 0) state = MoraleState.Panicked;        // Commander immune
    else if (ratio <= 0.5) state = MoraleState.Shaken;   // DAT_182d8fe40
    else state = MoraleState.Steady;

    state += moraleStateModifier;  // From unit stats +0xC0
    return clamp(state, 1, 3);
}
```

### Actor.ApplyMorale (0x1805DD240)
```c
void ApplyMorale(uint moraleFlags, float amount) {
    // 1. Check actor is alive and not invulnerable
    // 2. Check panic immunity flag (0xEC bit 7)
    // 3. Filter by morale flags mask (unit stats 0xA8)
    // 4. Multiply amount by moraleDamageMultiplier (0xBC)
    // 5. Trigger SkillContainer.OnMoraleEvent for pre-processing
    // 6. Clamp result to [0, MoraleMax]
    // 7. Call SetMorale with final value
}
```

### Actor.SetMorale (0x1805E7150)
```c
void SetMorale(float value) {
    // 1. Validate actor state
    // 2. Clamp value to [0, MoraleMax]
    // 3. Store at offset 0x160
    // 4. Update SkillContainer
    // 5. If state changed from cached (0xD4):
    //    - Handle faction switching for panic/unpanic
    //    - Fire TacticalManager.OnMoraleStateChanged
    //    - Track statistics
    //    - Show UI text (Panicked/Shaken)
    //    - If entering Panicked: propagate panic to nearby allies
    // 6. Update actor visual state
}
```

## Entity Base Class

The `Entity` class provides the base interface:

| Offset | Type | Field | Notes |
|--------|------|-------|-------|
| +0x54 | int | m_Hitpoints | Current HP |
| +0x58 | int | m_HitpointsMax | Maximum HP |
| +0x5C | int | m_ArmorDurability | Current armor durability |
| +0x60 | int | m_ArmorDurabilityMax | Maximum armor durability |

### Entity.ApplySuppression (virtual, 0x610A70)
Base virtual method for suppression application.

### Entity.GetSuppressionState (virtual, 0x611950)
Base virtual method returning SuppressionState.None for non-Actor entities.

## EntityProperties - Suppression Related

From the ArmorTemplate analysis, these EntityProperties offsets relate to suppression:

| Offset | Type | Field | Notes |
|--------|------|-------|-------|
| +0xc4 | int | MoraleBase | Base morale value |
| +0xc8 | float | MoraleMult | Morale multiplier |
| +0xcc | int | SuppressionResist | Base suppression resistance |
| +0xd0 | float | SuppressionResistMult | Suppression resistance mult |
| +0xd4 | int | Unknown | Modified by armor |
| +0xd8 | float | UnknownMult | Modified by armor |

## Tactical Actions

### AssignSuppressionToEntityAction (TypeDefIndex: 2652)
```c
public class AssignSuppressionToEntityAction : TacticalAction {
    private readonly SuppressionState m_SuppressionState;  // +0x10

    // Forces a specific suppression state on entity
    void Execute() {
        AssignSuppression();
    }
}
```

### AssignMoraleToEntityAction (TypeDefIndex: 1650)
```c
public class AssignMoraleToEntityAction : TacticalAction {
    private readonly MoraleState m_MoraleState;  // +0x10

    void Execute() {
        AssignMorale();
    }
}
```

## TacticalConfig Fields

The TacticalConfig contains suppression-related settings:

| Field | Type | Notes |
|-------|------|-------|
| SuppressionImpactMult | float | Multiplier for suppression effects (+0x1D0) |
| (thresholds) | float | State transition thresholds |

## Conversation Triggers

Suppression/morale state changes can trigger conversations:
- `ConversationTriggerType.EntitySuppressed = 14`
- `ConversationTriggerType.EntityMoraleRecovered = 44`

## Role Requirements

Conversations can require specific suppression/morale states:

```c
public class SuppressionRoleRequirement : BaseRoleRequirement {
    public SuppressionState State;  // +0x14
}

public class MoraleRoleRequirement : BaseRoleRequirement {  // 0x180577050
    public CompareOperation compareOperation;  // +0x10
    public int targetValue;                    // +0x14 (MoraleState)
}
```

## Statistics Tracking

The game tracks suppression statistics:
- `UnitStatistic.SuppressionDealt = 23`
- `UnitStatistic.SuppressionReceived = 24`

## UI Elements

HUD elements for displaying suppression/morale:
- `m_SuppressionBar` - Progress bar showing suppression level
- `SuppressedIcon` - Icon when suppressed
- `PinnedDownIcon` - Icon when pinned down
- `StanceSuppressedIcon` - Stance indicator for suppression

## Gameplay Effects

When suppressed:
1. **Action Point Reduction**: `ChangeSuppressionAndUpdateAP` reduces available AP
2. **Movement Restriction**: PinnedDown state prevents most actions
3. **Accuracy Penalty**: Suppressed units have reduced accuracy
4. **Cover Seeking**: AI prioritizes getting to cover when suppressed

When morale breaks:
1. **Fleeing State**: Unit attempts to leave combat area
2. **Wavering State**: Unit has reduced effectiveness
3. **Recovery**: Morale can recover over time or via abilities

## Tag Types

- `TagType.SUPPRESSIVE = 5` - Tag for weapons/skills that cause suppression

## Morale Event System

### Core Events

#### TacticalManager.OnMoraleStateChanged (0x1806716B0)
Central event fired when any actor's morale state changes.

```c
// Delegate offset: +0x1C8
void InvokeOnMoraleStateChanged(Actor entity, MoraleState newState);
```

**Subscribers:**
- `TacticalBarksManager.OnMoraleStateChanged` (0x1806CCB70)
- `HandleLeaderAttributesHandler.OnMoraleStateChanged` (0x180706C90)
- `SkillContainer.OnMoraleEvent` (0x1806EB3E0)

#### Event Subscription
```c
// Add subscriber
TacticalManager.add_OnMoraleStateChanged    // 0x180677DD0

// Remove subscriber
TacticalManager.remove_OnMoraleStateChanged // 0x180679690
```

### Morale Triggers

#### OnDamageReceived (0x1805E2070)
Handles morale effects when actor takes damage.

| Target | Flag | Formula |
|--------|------|---------|
| Attacker | `0x40` | `DAT_182d8fba4 / max(DAT_182d8fb9c, attackerAccuracy)` |
| Self | `0x01` | `hpLostPct * DAT_182d8fd78 * DAT_182d8ffa8 * attackerOutgoingMultiplier` |

#### OnDeath (0x1805E2920)
Propagates morale effects when actor dies.

| Target | Flag | Formula | Notes |
|--------|------|---------|-------|
| Enemies (not same faction) | `0x20` | `DAT_182d8fba0 - (distance - 1)` | Fear, stronger when closer |
| Allies (same faction, different unit) | `0x04` | `(distance - 1) - DAT_182d8ff90, capped at DAT_182d8ffb0` | Revenge motivation |
| Allies (same squad) | `0x04` | `(distance - 1) - DAT_182d8fba0` | Can be negative if close |

> **Immunity**: Unit stats `0xEC` bit 8 prevents death morale propagation.

#### OnElementDeath (0x1805E2C70)
Called when a single element (soldier) in a unit dies.

| Target | Flag | Formula | Notes |
|--------|------|---------|-------|
| Self | `0x08` | `(DAT_182d8fb9c / elementCount) * DAT_182d8fe54` | Scales inversely with unit size |
| Killer | `0x10` | `(DAT_182d8fb9c / killerElementCount) * DAT_182d8ff94` | Kill bonus |

### Panic Propagation

When an actor enters Panicked state (1), nearby allies receive morale damage:

1. `Actor.SetMorale` detects state change to Panicked
2. Iterates all actors in `TacticalManager` actor list (offset 0xA8)
3. For each ally (same faction, alive, not self, not invulnerable):
   - Calculates distance via `BaseTile.GetDistanceTo`
   - Calls `ApplyMorale` on ally

> **Immunity**: Commander units are immune to panic state.

## Effect Handlers

### AttackMoraleHandler (0x1806FBC50)
Handles attack-based morale damage.

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | int | applyPanic | If non-zero, applies panic flag (0x100) |
| +0x5C | int | reduceMoraleLevel | Number of morale states to reduce |

### ChangeMoraleHandler (0x1806FDE10)
Sets actor morale to specific state. Used for buffs/debuffs.

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | int | applyToTarget | 1 = apply to tile target |
| +0x5C | int | targetState | Target MoraleState (2=Shaken, 3=Steady) |
| +0x60 | int | comparisonMode | 1=only if higher, 2=only if lower |

### MoraleOverMaxEffectHandler (0x180716860)
Adds/removes a skill based on whether morale is at maximum.

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | SkillTemplate* | effectSkillTemplate | Skill to add at max morale |

### FilterByMoraleHandler (0x1807057F0)
Conditionally enables/hides skills based on morale state.

| Offset | Type | Field | Description |
|--------|------|-------|-------------|
| +0x58 | int | requiredState | MoraleState required for skill |

### HandleLeaderAttributesHandler (0x180706C90)
Triggers leader attribute activation (trigger type 2) on morale state change.

| Offset | Type | Field |
|--------|------|-------|
| +0x20 | UnitLeaderAttributes* | leaderAttributes |

## Conditions

### MoraleStateCondition (0x1806D3EB0)
Skill condition checking if actor's morale matches criteria.

| Offset | Type | Field | Values |
|--------|------|-------|--------|
| +0x18 | int | comparisonMode | 0=Equal, 1=GreaterOrEqual, 2=LessOrEqual |
| +0x1C | int | targetState | MoraleState enum value |

## Global Constants

| Symbol | Description |
|--------|-------------|
| `DAT_182d8fe40` | Shaken threshold (~0.5 or 50%) |
| `DAT_182d8fb9c` | Base morale constant (element death calculations) |
| `DAT_182d8fba0` | Death fear range (distance falloff) |
| `DAT_182d8fba4` | Attacker morale boost base |
| `DAT_182d8fd78` | HP damage to morale multiplier |
| `DAT_182d8fe54` | Element death self morale multiplier |
| `DAT_182d8ff90` | Ally death distance offset |
| `DAT_182d8ff94` | Kill bonus morale multiplier |
| `DAT_182d8ffa8` | Secondary HP damage morale multiplier |
| `DAT_182d8ffb0` | Max positive morale from ally death |
| `DAT_182d90248` | Shaken state morale percentage (~0.5) |

## Function Address Reference

### Core Morale Functions
| Function | Address |
|----------|---------|
| `Actor.ApplyMorale` | `0x1805DD240` |
| `Actor.SetMorale` | `0x1805E7150` |
| `Actor.GetMoraleState` | `0x1805DF4D0` |
| `Actor.GetMoralePct` | `0x1805DF4A0` |
| `Actor.GetMoraleMax` | `0x1805DF330` |

### Morale Triggers
| Function | Address |
|----------|---------|
| `Actor.OnDamageReceived` | `0x1805E2070` |
| `Actor.OnDeath` | `0x1805E2920` |
| `Actor.OnElementDeath` | `0x1805E2C70` |

### Event System
| Function | Address |
|----------|---------|
| `TacticalManager.InvokeOnMoraleStateChanged` | `0x1806716B0` |
| `TacticalManager.add_OnMoraleStateChanged` | `0x180677DD0` |
| `TacticalManager.remove_OnMoraleStateChanged` | `0x180679690` |
| `Skill.OnMoraleEvent` | `0x1806E0410` |
| `SkillContainer.OnMoraleEvent` | `0x1806EB3E0` |

### Effect Handlers
| Function | Address |
|----------|---------|
| `AttackMoraleHandler.OnApply` | `0x1806FBC50` |
| `AttackMoraleHandler.ApplyMorale` | `0x1806FBB70` |
| `ChangeMoraleHandler.OnApply` | `0x1806FDE10` |
| `ChangeMoraleHandler.ApplyMorale` | `0x1806FDD20` |
| `MoraleOverMaxEffectHandler.OnMoraleEvent` | `0x180716860` |
| `MoraleOverMaxEffectHandler.AddEffect` | `0x180716740` |
| `MoraleOverMaxEffectHandler.RemoveEffect` | `0x180716A10` |
| `FilterByMoraleHandler.IsEnabled` | `0x1807057F0` |
| `FilterByMoraleHandler.IsHidden` | `0x180705870` |
| `HandleLeaderAttributesHandler.OnMoraleStateChanged` | `0x180706C90` |

### Conditions & Requirements
| Function | Address |
|----------|---------|
| `MoraleStateCondition.IsTrue` | `0x1806D3EB0` |
| `MoraleRoleRequirement.FulfillsRequirement` | `0x180577050` |

### UI Integration
| Function | Address |
|----------|---------|
| `TacticalBarksManager.OnMoraleStateChanged` | `0x1806CCB70` |
| `DevCombatLog.ReportMoraleChanged` | `0x1805D5A60` |
