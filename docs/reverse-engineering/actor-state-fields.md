# Actor State Fields

## Overview

Actor state fields control visibility, deployment status, and lifecycle states for tactical units. These boolean and bitmask fields determine detection, AI visibility, weapon states, and death/exit states.

Based on reverse engineering findings from Phase 1 implementation (EntityState.cs).

## Memory Layout

### Actor State Offsets

| Offset | Type | Field Name | Description | Status |
|--------|------|------------|-------------|--------|
| 0x138 | int32 | m_DetectedByFactionMask | Bitmask for faction detection (bit 0-31 = faction index) | ✅ Verified |
| 0x160 | float | m_Morale | Morale value (0.0-100.0, affects AI behavior) | ✅ Verified |
| 0x16A | byte | m_IsDying | Actor is in dying/bleeding out state | ✅ Verified |
| 0x16B | byte | m_IsLeavingMap | Actor is exiting the tactical map | ✅ Verified |
| 0x16D | byte | m_IsMinion | Actor is a minion/summoned unit | ⚠️ Needs Verification |
| 0x16E | byte | m_IsSelectable | Actor can be selected by player | ⚠️ Needs Verification |
| 0x16F | byte | m_IsHeavyWeaponDeployed | Heavy weapon deployment state | ✅ Verified |
| 0x1A4 | byte | m_HiddenToAICache | Cached visibility state for AI systems | ✅ Verified |
| 0x1A5 | byte | m_HiddenToPlayer | Visibility state for player rendering | ⚠️ Needs Verification |

## Detection System

### Faction Detection Bitmask

The `m_DetectedByFactionMask` field at 0x138 uses a 32-bit bitmask to track which factions have detected this actor:

```c
int32 detectionMask = *(int32*)(actor + 0x138);

// Check if faction 0 (player) has detected this actor
bool detectedByPlayer = (detectionMask & (1 << 0)) != 0;

// Set detected by faction 2
detectionMask |= (1 << 2);

// Clear detection for faction 1
detectionMask &= ~(1 << 1);
```

### Detection Operations

| Operation | Code | Description |
|-----------|------|-------------|
| Check Detection | `(mask & (1 << factionIndex)) != 0` | Test if faction has detected actor |
| Set Detected | `mask \|= (1 << factionIndex)` | Mark actor as detected by faction |
| Clear Detection | `mask &= ~(1 << factionIndex)` | Conceal actor from faction |
| Reveal to All | `mask = 0xFFFFFFFF` | Set all bits (detected by all factions) |
| Conceal from All | `mask = 0` | Clear all bits (not detected by any faction) |

**Faction Indices:**
- 0 = Player
- 1 = Enemy
- 2+ = Other factions (neutral, civilians, etc.)

## State Flags

### Boolean State Fields

All boolean state fields use byte storage (0 = false, 1 = true):

```c
// Read state
byte isDying = *(byte*)(actor + 0x16A);
bool dying = (isDying != 0);

// Write state
*(byte*)(actor + 0x16F) = heavyWeaponDeployed ? 1 : 0;
```

### Heavy Weapon Deployment

`m_IsHeavyWeaponDeployed` (0x16F) controls whether heavy weapons (LMGs, rocket launchers) are deployed:

- **Deployed (1):** Unit has setup bipod/tripod, increased accuracy, cannot move
- **Undeployed (0):** Normal movement and combat, reduced accuracy with heavy weapons

**Usage:**
```c
// Deploy heavy weapon
*(byte*)(actor + 0x16F) = 1;

// Check deployment state
bool deployed = *(byte*)(actor + 0x16F) != 0;
```

### Death and Exit States

**m_IsDying (0x16A):**
- Set when actor reaches 0 HP or bleeding out
- Triggers death animation and removal sequence
- Does not immediately destroy actor object

**m_IsLeavingMap (0x16B):**
- Set when actor exits the tactical map
- Used for extraction zones, fleeing units
- Prevents further AI actions

## Visibility System

### AI Visibility Cache

`m_HiddenToAICache` (0x1A4) is a cached boolean for AI visibility checks:

- **Hidden (1):** AI cannot see or target this actor
- **Visible (0):** Normal AI visibility rules apply

**Performance Note:** This is a *cache* field. The game may recalculate actual visibility from other sources (LOS, detection mask, etc.). Modifying this field provides temporary concealment but may be overwritten by game logic.

### Player Visibility

`m_HiddenToPlayer` (0x1A5) controls rendering visibility:

- **Hidden (1):** Actor model not rendered for player
- **Visible (0):** Actor rendered if in FOV

**Status:** Offset estimated based on adjacent field layout. Requires Ghidra verification.

## Morale System

### Morale Value

`m_Morale` (0x160) is a float field controlling AI behavior states:

| Morale Range | State | Behavior |
|--------------|-------|----------|
| 0.0 | Panicked | Flee from combat, may switch factions |
| 1.0 - 24.9 | Shaken | Defensive posture, reduced effectiveness |
| 25.0 - 74.9 | Steady | Normal behavior |
| 75.0 - 100.0 | Confident/Fearless | Aggressive, immune to panic |

**Float Storage:**
```c
// Read morale
float morale = *(float*)(actor + 0x160);

// Write morale (using bit conversion)
int moraleInt = BitConverter.SingleToInt32Bits(50.0f);
*(int32*)(actor + 0x160) = moraleInt;
```

### Morale-Based Threat Proxy

Since the game has no direct per-target threat system, morale can be used as a proxy:
- **High Threat → Low Morale:** Makes AI defensive/fleeing
- **Low Threat → High Morale:** Makes AI aggressive/confident

## Methods

### Related Actor Methods

| Address | Signature | Description |
|---------|-----------|-------------|
| 0x1805e0160 | bool IsDetectedByFaction(byte faction) | Check if actor detected by faction |
| 0x1805e01a0 | bool IsHiddenToAI() | Check if hidden from AI |
| 0x1805e0750 | bool IsHiddenToPlayer() | Check if hidden from player |
| 0x1805e0190 | bool IsHeavyWeaponDeployed() | Check weapon deployment state |
| 0x1805e0180 | bool IsDying() | Check if actor is dying |
| 0x1805e07e0 | bool IsLeavingMap() | Check if actor is exiting map |
| 0x1805df4d0 | MoraleState GetMoraleState() | Get morale state enum |
| 0x1805dd240 | void ApplyMorale(MoraleEventType type, float amount) | Apply morale change |
| 0x1805e6d90 | void SetMorale(float value) | Set morale directly |

## SDK Implementation

The EntityState.cs SDK module provides safe access to these fields:

```csharp
// Detection manipulation
EntityState.SetDetectedByFaction(actor, factionIndex: 0, detected: true);
EntityState.RevealToAll(actor);
EntityState.ConcealFromAll(actor);

// Heavy weapon control
EntityState.SetHeavyWeaponDeployed(actor, deployed: true);
EntityState.ToggleHeavyWeapon(actor);

// Visibility control
EntityState.SetHiddenToAI(actor, hidden: true);
EntityState.SetHiddenToPlayer(actor, hidden: true); // Needs verification

// Lifecycle states
EntityState.SetDying(actor, dying: true);
EntityState.SetLeavingMap(actor, leaving: true);

// Query all states
var flags = EntityState.GetStateFlags(actor);
if (flags.IsHeavyWeaponDeployed) { /* ... */ }
```

## Notes

### Verification Status

- **✅ Verified:** Offsets tested and confirmed working in EntityState.cs implementation
- **⚠️ Needs Verification:** Estimated offsets based on structure analysis, not yet tested in practice
- **📝 Reference Only:** From decompilation but not yet used in any implementation

### Thread Safety

Most state field writes are safe during:
1. `TacticalEventHooks.OnTurnStart`/`OnTurnEnd`
2. When `AI.IsAnyFactionThinking()` returns false
3. When game is paused

Detection mask and visibility flags can cause rendering/AI issues if modified during parallel evaluation.

### Adjacent State Fields

The boolean state flags cluster (0x16A-0x170) contains several fields not yet fully documented:

| Offset | Est. Field | Status |
|--------|-----------|--------|
| 0x16C | m_IsStunned | ✅ Verified (see actor-system.md) |
| 0x16D | m_IsMinion | ⚠️ Estimated |
| 0x16E | m_IsSelectable | ⚠️ Estimated |
| 0x170 | Unknown | 📝 Padding or flag |
| 0x171 | m_HasActed | ✅ Verified (see actor-system.md) |

### Future Research

1. Verify `m_HiddenToPlayer` offset (estimated at 0x1A5)
2. Verify `m_IsMinion` offset (estimated at 0x16D)
3. Verify `m_IsSelectable` offset (estimated at 0x16E)
4. Document byte at 0x170 (padding or additional flag?)
5. Research relationship between HiddenToAICache and actual AI visibility calculation
6. Document morale thresholds for panic spread to nearby units

## See Also

- [actor-system.md](actor-system.md) - Complete Actor class layout
- [ai-system.md](ai-system.md) - AI behavior and morale system details
- [suppression-morale.md](suppression-morale.md) - Morale/suppression mechanics
