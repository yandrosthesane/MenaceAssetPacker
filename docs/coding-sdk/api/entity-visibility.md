# EntityVisibility

`Menace.SDK.EntityVisibility` -- Faction-based visibility and detection control with temporary override system.

## Overview

EntityVisibility provides high-level control over faction-based detection states with support for temporary visibility overrides. This module builds on EntityState's detection mask manipulation by adding duration-based visibility management.

**Key Features:**
- Per-faction visibility control
- Temporary visibility overrides (N turns)
- Automatic override expiration via turn tracking
- Direct detection mask manipulation

**Based on reverse engineering:**
- Actor.m_DetectedByFactionMask @ 0x138 (int32 bitmask, one bit per faction)
- Supports 32 factions (bits 0-31)

## Module Path

```csharp
using Menace.SDK;
// Access via: EntityVisibility.MethodName(...)
```

## Methods

### RevealToFaction

**Signature**: `bool RevealToFaction(GameObj actor, int factionIndex)`

**Description**: Reveal actor to a specific faction by setting the corresponding bit in the detection mask.

**Parameters:**
- `actor` (GameObj): The actor to reveal
- `factionIndex` (int): Faction index (0-31)

**Returns**: True if successful

**Example:**
```csharp
var enemy = FindActorByName("Hidden_Sniper");

// Reveal to player faction (faction 0)
if (EntityVisibility.RevealToFaction(enemy, 0))
{
    DevConsole.Log("Enemy revealed to player");
}

// Reveal to multiple factions
for (int i = 0; i < 3; i++)
{
    EntityVisibility.RevealToFaction(enemy, i);
}
```

**Related:**
- [ConcealFromFaction](#concealfromfaction) - Hide from faction
- [SetDetectionMask](#setdetectionmask) - Set entire mask at once
- [EntityState.SetDetectedByFaction](entity-state.md#setdetectedbyfaction) - Lower-level version

**Notes:**
- Modifies Actor+0x138 (m_DetectedByFactionMask)
- Uses bitwise OR to set faction bit
- Faction index must be 0-31
- Thread-safe for single-faction modifications

---

### ConcealFromFaction

**Signature**: `bool ConcealFromFaction(GameObj actor, int factionIndex)`

**Description**: Conceal actor from a specific faction by clearing the corresponding bit in the detection mask.

**Parameters:**
- `actor` (GameObj): The actor to conceal
- `factionIndex` (int): Faction index (0-31)

**Returns**: True if successful

**Example:**
```csharp
var stealthUnit = TacticalController.GetActiveActor();

// Conceal from enemy faction (faction 1)
if (EntityVisibility.ConcealFromFaction(stealthUnit, 1))
{
    DevConsole.Log("Unit concealed from enemies");
}

// Enter stealth - hide from all enemy factions
var enemyFactions = new[] { 1, 2, 3 };
foreach (var faction in enemyFactions)
{
    EntityVisibility.ConcealFromFaction(stealthUnit, faction);
}
```

**Related:**
- [RevealToFaction](#revealtofaction) - Reveal to faction
- [SetDetectionMask](#setdetectionmask) - Set entire mask at once

**Notes:**
- Modifies Actor+0x138 (m_DetectedByFactionMask)
- Uses bitwise AND NOT to clear faction bit
- Faction index must be 0-31
- Does not affect other faction bits

---

### SetDetectionMask

**Signature**: `bool SetDetectionMask(GameObj actor, int bitmask)`

**Description**: Set the entire detection mask at once, replacing all faction detection bits.

**Parameters:**
- `actor` (GameObj): The actor to modify
- `bitmask` (int): The detection bitmask (one bit per faction)

**Returns**: True if successful

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Reveal to factions 0, 1, and 4
int mask = (1 << 0) | (1 << 1) | (1 << 4);
EntityVisibility.SetDetectionMask(actor, mask);

// Reveal to all factions
EntityVisibility.SetDetectionMask(actor, -1); // 0xFFFFFFFF

// Conceal from all factions
EntityVisibility.SetDetectionMask(actor, 0);

// Query current mask
int currentMask = EntityVisibility.GetDetectionMask(actor);
DevConsole.Log($"Detection mask: 0x{currentMask:X8}");
```

**Related:**
- [GetDetectionMask](#getdetectionmask) - Query current mask
- [RevealToFaction](#revealtofaction) - Modify single faction

**Notes:**
- Replaces entire mask - previous state is lost
- Bit 0 = faction 0, bit 31 = faction 31
- Use bitwise operations to construct mask

---

### GetDetectionMask

**Signature**: `int GetDetectionMask(GameObj actor)`

**Description**: Get the current detection mask showing which factions can detect the actor.

**Parameters:**
- `actor` (GameObj): The actor to query

**Returns**: The detection bitmask (one bit per faction)

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();
int mask = EntityVisibility.GetDetectionMask(actor);

// Check specific faction detection
bool detectedByPlayer = (mask & (1 << 0)) != 0;
bool detectedByEnemy = (mask & (1 << 1)) != 0;

DevConsole.Log($"Detected by player: {detectedByPlayer}");
DevConsole.Log($"Detected by enemy: {detectedByEnemy}");

// Count detecting factions
int detectionCount = 0;
for (int i = 0; i < 32; i++)
{
    if ((mask & (1 << i)) != 0)
        detectionCount++;
}
DevConsole.Log($"Detected by {detectionCount} factions");
```

**Related:**
- [SetDetectionMask](#setdetectionmask) - Set mask
- [EntityState.GetStateFlags](entity-state.md#getstateflags) - Get all state flags

**Notes:**
- Reads Actor+0x138 (m_DetectedByFactionMask)
- Returns 0 if actor is null
- Use bitwise operations to check individual factions

---

### ForceVisibleTo

**Signature**: `bool ForceVisibleTo(GameObj actor, GameObj viewer, int turns = 1)`

**Description**: Force actor to be visible to a specific viewer for N turns. Uses temporary override system that restores original visibility after expiration.

**Parameters:**
- `actor` (GameObj): The actor to make visible
- `viewer` (GameObj): The viewing actor
- `turns` (int): Number of turns to maintain visibility (default: 1)

**Returns**: True if successful

**Example:**
```csharp
var hiddenEnemy = FindActorByName("Cloaked_Assassin");
var player = TacticalController.GetActiveActor();

// Reveal for 2 turns (detection ability)
if (EntityVisibility.ForceVisibleTo(hiddenEnemy, player, 2))
{
    DevConsole.Log("Enemy revealed for 2 turns");
}

// Flare effect - reveal to all allies for 3 turns
var allies = FindAllAlliedActors();
foreach (var ally in allies)
{
    EntityVisibility.ForceVisibleTo(hiddenEnemy, ally, 3);
}
```

**Related:**
- [ForceConcealedFrom](#forceconcealedfrom) - Temporary concealment
- [ClearAllOverrides](#clearalloverrides) - Clear all overrides

**Notes:**
- Stores original detection mask
- Automatically restores after N turns via UpdateOverrides()
- Call UpdateOverrides() from TacticalEventHooks.OnTurnEnd
- Only affects viewer's faction
- Multiple overrides replace previous override

---

### ForceConcealedFrom

**Signature**: `bool ForceConcealedFrom(GameObj actor, GameObj viewer, int turns = 1)`

**Description**: Force actor to be concealed from a specific viewer for N turns. Uses temporary override system that restores original visibility after expiration.

**Parameters:**
- `actor` (GameObj): The actor to conceal
- `viewer` (GameObj): The viewing actor
- `turns` (int): Number of turns to maintain concealment (default: 1)

**Returns**: True if successful

**Example:**
```csharp
var stealthUnit = TacticalController.GetActiveActor();
var enemy = FindActorByName("Enemy_Guard");

// Activate stealth for 3 turns
if (EntityVisibility.ForceConcealedFrom(stealthUnit, enemy, 3))
{
    DevConsole.Log("Stealth activated for 3 turns");
}

// Smoke grenade effect - conceal from all enemies
var enemies = FindAllEnemyActors();
foreach (var enemy in enemies)
{
    EntityVisibility.ForceConcealedFrom(stealthUnit, enemy, 2);
}
```

**Related:**
- [ForceVisibleTo](#forcevisibleto) - Temporary reveal
- [ClearAllOverrides](#clearalloverrides) - Clear all overrides

**Notes:**
- Stores original detection mask
- Automatically restores after N turns via UpdateOverrides()
- Call UpdateOverrides() from TacticalEventHooks.OnTurnEnd
- Only affects viewer's faction
- Multiple overrides replace previous override

---

### ClearAllOverrides

**Signature**: `void ClearAllOverrides()`

**Description**: Clear all visibility overrides immediately, restoring original detection states for all actors.

**Example:**
```csharp
// Clear all visibility overrides (e.g., combat ended)
EntityVisibility.ClearAllOverrides();
DevConsole.Log("All visibility overrides cleared");
```

**Related:**
- [ForceVisibleTo](#forcevisibleto) - Create overrides
- [ForceConcealedFrom](#forceconcealedfrom) - Create overrides

**Notes:**
- Restores all actors to their original detection masks
- Clears all pending overrides
- Safe to call at any time
- Does not affect permanent visibility changes

---

## Automatic Override Management

The module automatically tracks and expires temporary overrides. To enable this, hook into turn events:

```csharp
using Menace.SDK;

// Initialize in your mod
TacticalEventHooks.OnTurnEnd += (actorPtr) =>
{
    // UpdateOverrides is called automatically by the SDK
    // No manual call needed - this is handled internally
};
```

**UpdateOverrides() (Internal):**
- Decrements turn counters for all active overrides
- Restores original detection mask when turns reach 0
- Removes expired overrides from tracking

## Complete Example

```csharp
using Menace.SDK;

// Stealth system implementation
public class StealthSystem
{
    public void ActivateCloaking(GameObj actor, int duration)
    {
        var enemies = FindAllEnemyActors();

        // Conceal from all enemies
        foreach (var enemy in enemies)
        {
            EntityVisibility.ForceConcealedFrom(actor, enemy, duration);
        }

        DevConsole.Log($"Cloaking activated for {duration} turns");
    }

    public void ActivateDetection(GameObj actor, int radius, int duration)
    {
        var allies = FindActorsInRadius(actor, radius);
        var enemies = FindAllEnemyActors();

        // Reveal all enemies to all nearby allies
        foreach (var ally in allies)
        {
            foreach (var enemy in enemies)
            {
                EntityVisibility.ForceVisibleTo(enemy, ally, duration);
            }
        }

        DevConsole.Log($"Detection activated: {enemies.Count} enemies revealed");
    }

    public void CreateFactionVisibilityMatrix()
    {
        var actors = FindAllActors();

        foreach (var actor in actors)
        {
            int mask = EntityVisibility.GetDetectionMask(actor);

            DevConsole.Log($"{actor.GetName()} visibility:");
            for (int i = 0; i < 8; i++)
            {
                bool visible = (mask & (1 << i)) != 0;
                DevConsole.Log($"  Faction {i}: {(visible ? "Visible" : "Hidden")}");
            }
        }
    }

    public void ImplementFogOfWar()
    {
        var playerFaction = 0;
        var allActors = FindAllActors();

        foreach (var actor in allActors)
        {
            var actorFaction = GetActorFaction(actor);

            if (actorFaction != playerFaction)
            {
                // Conceal enemies unless in player LOS
                if (!IsInPlayerLOS(actor))
                {
                    EntityVisibility.ConcealFromFaction(actor, playerFaction);
                }
                else
                {
                    EntityVisibility.RevealToFaction(actor, playerFaction);
                }
            }
        }
    }
}

// Ability-based visibility control
public class VisibilityAbilities
{
    // Recon scan ability
    public void UseReconScan(GameObj actor)
    {
        var nearbyEnemies = FindEnemiesInRadius(actor, 10);

        foreach (var enemy in nearbyEnemies)
        {
            // Reveal to actor's faction for 3 turns
            EntityVisibility.ForceVisibleTo(enemy, actor, 3);
            DevConsole.Log($"Scanned: {enemy.GetName()}");
        }
    }

    // Smoke grenade ability
    public void ThrowSmokeGrenade(GameObj actor, GameObj targetTile)
    {
        var affectedActors = FindActorsInRadius(targetTile, 2);
        var enemies = FindAllEnemyActors();

        // Conceal all units in smoke from all enemies
        foreach (var affected in affectedActors)
        {
            foreach (var enemy in enemies)
            {
                EntityVisibility.ForceConcealedFrom(affected, enemy, 2);
            }
        }

        DevConsole.Log($"Smoke grenade: {affectedActors.Count} units concealed");
    }

    // Stealth suit activation
    public void ActivateStealthSuit(GameObj actor)
    {
        var currentMask = EntityVisibility.GetDetectionMask(actor);

        // Store original visibility
        actor.SetField("OriginalDetectionMask", currentMask);

        // Conceal from all factions
        EntityVisibility.SetDetectionMask(actor, 0);

        DevConsole.Log("Stealth suit activated");
    }

    public void DeactivateStealthSuit(GameObj actor)
    {
        var originalMask = actor.GetField<int>("OriginalDetectionMask");

        // Restore original visibility
        EntityVisibility.SetDetectionMask(actor, originalMask);

        DevConsole.Log("Stealth suit deactivated");
    }
}
```

## Detection Mask Bit Reference

```
Bit 0  = Faction 0 (typically player)
Bit 1  = Faction 1 (typically AI enemy)
Bit 2  = Faction 2
...
Bit 31 = Faction 31

Bitmask Examples:
0x00000001 = Visible only to faction 0
0x00000003 = Visible to factions 0 and 1
0xFFFFFFFF = Visible to all factions
0x00000000 = Invisible to all factions
```

## See Also

- [EntityState](entity-state.md) - Lower-level state flag control
- [EntityAI](entity-ai.md) - AI behavior and threat manipulation
- [TacticalEventHooks](tactical-event-hooks.md) - Turn start/end hooks for override expiration
- [Intercept](intercept.md) - Event hooks for detection changes
