# EntityState

`Menace.SDK.EntityState` -- Direct memory access module for manipulating entity state flags and visibility detection masks.

## Overview

EntityState provides low-level control over actor boolean states and faction-based detection systems. This module uses direct memory writes to modify actor state fields that control heavy weapon deployment, visibility, dying states, and other critical flags.

**Key Features:**
- Direct memory manipulation of actor state flags
- Faction-based detection mask control (32 factions supported)
- Heavy weapon deployment state management
- Death and map exit state control
- Comprehensive state querying via StateFlags structure

**Based on Ghidra reverse engineering:**
- Actor.m_IsHeavyWeaponDeployed @ 0x16F (bool)
- Actor.m_DetectedByFactionMask @ 0x138 (int32 bitmask)
- Actor.m_HiddenToAICache @ 0x1A4 (bool)
- Actor.m_IsDying @ 0x16A (bool)
- Actor.m_IsLeavingMap @ 0x16B (bool)

## Module Path

```csharp
using Menace.SDK;
// Access via: EntityState.MethodName(...)
```

## Methods

### SetHeavyWeaponDeployed

**Signature**: `bool SetHeavyWeaponDeployed(GameObj actor, bool deployed)`

**Description**: Set whether a heavy weapon is deployed. Deployed heavy weapons typically have different accuracy, range, and movement penalties.

**Parameters:**
- `actor` (GameObj): The actor with the heavy weapon
- `deployed` (bool): True to deploy, false to undeploy

**Returns**: True if state was set successfully

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Deploy heavy weapon (locks movement, increases accuracy)
if (EntityState.SetHeavyWeaponDeployed(actor, true))
{
    DevConsole.Log("Heavy weapon deployed - movement restricted");
}

// Undeploy to regain mobility
EntityState.SetHeavyWeaponDeployed(actor, false);
```

**Related:**
- [ToggleHeavyWeapon](#toggleheavyweapon) - Toggle deployment state
- [GetStateFlags](#getstateflags) - Query current deployment state

**Notes:**
- Writes to Actor+0x16F (m_IsHeavyWeaponDeployed)
- Game may enforce additional restrictions (AP cost, movement penalties)
- State persists until explicitly changed

---

### ToggleHeavyWeapon

**Signature**: `bool ToggleHeavyWeapon(GameObj actor)`

**Description**: Toggle heavy weapon deployment state between deployed and undeployed.

**Parameters:**
- `actor` (GameObj): The actor with the heavy weapon

**Returns**: True if state was toggled successfully

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Toggle deployment on/off with single call
if (EntityState.ToggleHeavyWeapon(actor))
{
    var flags = EntityState.GetStateFlags(actor);
    DevConsole.Log($"Deployment toggled: {flags.IsHeavyWeaponDeployed}");
}
```

**Related:**
- [SetHeavyWeaponDeployed](#setheavyweapondeployed) - Set specific state

---

### SetDetectedByFaction

**Signature**: `bool SetDetectedByFaction(GameObj actor, int faction, bool detected)`

**Description**: Set detection state for a specific faction. The game uses a 32-bit bitmask where each bit represents one faction's detection state.

**Parameters:**
- `actor` (GameObj): The actor to modify
- `faction` (int): Faction index (0-31)
- `detected` (bool): True to mark as detected, false to conceal

**Returns**: True if state was set successfully

**Example:**
```csharp
var enemyActor = FindActorByName("Enemy_Sniper");

// Reveal to faction 0 (player faction)
EntityState.SetDetectedByFaction(enemyActor, 0, true);

// Conceal from faction 1 (AI faction)
EntityState.SetDetectedByFaction(enemyActor, 1, false);

// Check what can see the actor
var flags = EntityState.GetStateFlags(enemyActor);
DevConsole.Log($"Detection mask: 0x{flags.DetectionMask:X8}");
```

**Related:**
- [RevealToAll](#revealtoall) - Reveal to all factions
- [ConcealFromAll](#concealfromall) - Conceal from all factions
- [EntityVisibility](entity-visibility.md) - Higher-level visibility control

**Notes:**
- Uses bitmask at Actor+0x138 (m_DetectedByFactionMask)
- Bit 0 = faction 0, bit 31 = faction 31
- Supports up to 32 factions simultaneously
- Thread-safe for single-faction modifications

---

### SetHiddenToAI

**Signature**: `bool SetHiddenToAI(GameObj actor, bool hidden)`

**Description**: Set whether actor is hidden from AI. Uses cached state field - may not affect all AI systems.

**Parameters:**
- `actor` (GameObj): The actor to modify
- `hidden` (bool): True to hide from AI, false to reveal

**Returns**: True if state was set successfully

**Example:**
```csharp
var stealthUnit = TacticalController.GetActiveActor();

// Hide from AI detection
if (EntityState.SetHiddenToAI(stealthUnit, true))
{
    DevConsole.Log("Unit is now hidden from AI");
}

// Later, reveal
EntityState.SetHiddenToAI(stealthUnit, false);
```

**Related:**
- [SetHiddenToPlayer](#sethiddentoplayer) - Hide from player visibility
- [EntityVisibility.ForceConcealedFrom](entity-visibility.md#forceconcealfrom) - Temporary concealment

**Notes:**
- Writes to Actor+0x1A4 (m_HiddenToAICache)
- This is a cache field - may not affect all AI subsystems
- For guaranteed AI concealment, use detection mask methods

---

### SetHiddenToPlayer

**Signature**: `bool SetHiddenToPlayer(GameObj actor, bool hidden)`

**Description**: Set whether actor is hidden from player visibility. Note: Offset needs verification via Ghidra.

**Parameters:**
- `actor` (GameObj): The actor to modify
- `hidden` (bool): True to hide from player, false to reveal

**Returns**: True if state was set successfully

**Example:**
```csharp
var hiddenEnemy = FindActorByName("Ambusher");

// Hide until triggered
EntityState.SetHiddenToPlayer(hiddenEnemy, true);

// On trigger event
TacticalEventHooks.OnTurnStart += (actorPtr) =>
{
    if (IsAmbushTriggered())
    {
        EntityState.SetHiddenToPlayer(hiddenEnemy, false);
        DevConsole.Log("Ambush revealed!");
    }
};
```

**Related:**
- [SetHiddenToAI](#sethiddentoai) - Hide from AI
- [SetDetectedByFaction](#setdetectedbyfaction) - Faction-based visibility

**Notes:**
- Uses estimated offset 0x1A5 - **requires Ghidra verification**
- Adjacent to m_HiddenToAICache
- May interact with fog of war systems

---

### RevealToAll

**Signature**: `bool RevealToAll(GameObj actor)`

**Description**: Reveal actor to all factions by setting all detection bits to 1.

**Parameters:**
- `actor` (GameObj): The actor to reveal

**Returns**: True if state was set successfully

**Example:**
```csharp
var vipUnit = FindActorByName("VIP_Target");

// Make VIP visible to everyone
if (EntityState.RevealToAll(vipUnit))
{
    DevConsole.Log("VIP is now visible to all factions");
}
```

**Related:**
- [ConcealFromAll](#concealfromall) - Opposite operation
- [SetDetectedByFaction](#setdetectedbyfaction) - Per-faction control

**Notes:**
- Sets m_DetectedByFactionMask to -1 (0xFFFFFFFF)
- All 32 faction bits set to 1
- Instant operation, no iteration

---

### ConcealFromAll

**Signature**: `bool ConcealFromAll(GameObj actor)`

**Description**: Conceal actor from all factions by clearing all detection bits to 0.

**Parameters:**
- `actor` (GameObj): The actor to conceal

**Returns**: True if state was set successfully

**Example:**
```csharp
var stealthUnit = TacticalController.GetActiveActor();

// Enter full stealth mode
if (EntityState.ConcealFromAll(stealthUnit))
{
    DevConsole.Log("Unit concealed from all factions");
}
```

**Related:**
- [RevealToAll](#revealtoall) - Opposite operation
- [SetDetectedByFaction](#setdetectedbyfaction) - Per-faction control

**Notes:**
- Sets m_DetectedByFactionMask to 0 (0x00000000)
- All 32 faction bits cleared
- Instant operation, no iteration

---

### SetDying

**Signature**: `bool SetDying(GameObj actor, bool dying)`

**Description**: Set whether actor is in dying state. Dying actors may trigger different animations, AI responses, and gameplay mechanics.

**Parameters:**
- `actor` (GameObj): The actor to modify
- `dying` (bool): True to mark as dying, false to clear

**Returns**: True if state was set successfully

**Example:**
```csharp
var woundedActor = FindActorByName("Wounded_Soldier");

// Trigger dying state
if (EntityState.SetDying(woundedActor, true))
{
    DevConsole.Log("Actor entered dying state");
}

// Revive/stabilize
EntityState.SetDying(woundedActor, false);
```

**Related:**
- [GetStateFlags](#getstateflags) - Query dying state

**Notes:**
- Writes to Actor+0x16A (m_IsDying)
- May trigger death animations and AI responses
- Does not instantly kill - use health manipulation for that

---

### SetLeavingMap

**Signature**: `bool SetLeavingMap(GameObj actor, bool leaving)`

**Description**: Set whether actor is leaving the map. Used for extraction zones and map exits.

**Parameters:**
- `actor` (GameObj): The actor to modify
- `leaving` (bool): True to mark as leaving, false to clear

**Returns**: True if state was set successfully

**Example:**
```csharp
var escapingActor = TacticalController.GetActiveActor();

// Mark as leaving (extraction)
if (EntityState.SetLeavingMap(escapingActor, true))
{
    DevConsole.Log("Actor marked for extraction");
}
```

**Related:**
- [GetStateFlags](#getstateflags) - Query leaving state

**Notes:**
- Writes to Actor+0x16B (m_IsLeavingMap)
- May prevent further actions
- Used in extraction/evac mechanics

---

### SetMinion

**Signature**: `bool SetMinion(GameObj actor, bool isMinion)`

**Description**: Set whether actor is a minion. Note: Offset needs verification via Ghidra - estimated based on structure.

**Parameters:**
- `actor` (GameObj): The actor to modify
- `isMinion` (bool): True to mark as minion, false to clear

**Returns**: True if state was set successfully

**Example:**
```csharp
var summonedUnit = SpawnActor("Summoned_Creature");

// Mark as minion (subordinate unit)
if (EntityState.SetMinion(summonedUnit, true))
{
    DevConsole.Log("Unit marked as minion");
}
```

**Notes:**
- Uses estimated offset 0x16D - **requires Ghidra verification**
- May affect AI behavior and unit classification
- Typically used for summoned/temporary units

---

### SetSelectableByPlayer

**Signature**: `bool SetSelectableByPlayer(GameObj actor, bool selectable)`

**Description**: Set whether actor can be selected by player. Note: Offset needs verification via Ghidra - estimated based on structure.

**Parameters:**
- `actor` (GameObj): The actor to modify
- `selectable` (bool): True to allow selection, false to prevent

**Returns**: True if state was set successfully

**Example:**
```csharp
var npcActor = FindActorByName("Friendly_NPC");

// Prevent player from selecting NPC
if (EntityState.SetSelectableByPlayer(npcActor, false))
{
    DevConsole.Log("NPC is no longer selectable");
}
```

**Notes:**
- Uses estimated offset 0x16E - **requires Ghidra verification**
- Useful for NPC units, objectives, or cinematic sequences
- Does not affect AI control

---

### GetStateFlags

**Signature**: `StateFlags GetStateFlags(GameObj actor)`

**Description**: Get all state flags for an actor in a single query.

**Parameters:**
- `actor` (GameObj): The actor to query

**Returns**: StateFlags structure with all state information

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();
var flags = EntityState.GetStateFlags(actor);

DevConsole.Log($"Heavy Weapon Deployed: {flags.IsHeavyWeaponDeployed}");
DevConsole.Log($"Detection Mask: 0x{flags.DetectionMask:X8}");
DevConsole.Log($"Hidden to AI: {flags.IsHiddenToAI}");
DevConsole.Log($"Is Dying: {flags.IsDying}");
DevConsole.Log($"Is Leaving Map: {flags.IsLeavingMap}");

// Check specific faction detection
bool detectedByPlayer = (flags.DetectionMask & (1 << 0)) != 0;
DevConsole.Log($"Detected by player faction: {detectedByPlayer}");
```

**Related:**
- All setter methods above

**Notes:**
- Single read operation for all state flags
- Returns empty StateFlags if actor is null
- Bitmask can be manually parsed for faction-specific detection

---

## StateFlags Structure

```csharp
public struct StateFlags
{
    public bool IsHeavyWeaponDeployed { get; set; }
    public int DetectionMask { get; set; }
    public bool IsHiddenToAI { get; set; }
    public bool IsDying { get; set; }
    public bool IsLeavingMap { get; set; }
}
```

**DetectionMask Bit Layout:**
- Bit 0 = Faction 0 detection
- Bit 1 = Faction 1 detection
- ...
- Bit 31 = Faction 31 detection

## Complete Example

```csharp
using Menace.SDK;

// Create a stealth unit that only enemy faction can detect
public void SetupStealthUnit()
{
    var stealthUnit = TacticalController.GetActiveActor();

    // Conceal from all factions first
    EntityState.ConcealFromAll(stealthUnit);

    // Reveal only to specific enemy faction (faction 2)
    EntityState.SetDetectedByFaction(stealthUnit, 2, true);

    // Ensure not hidden to AI (so enemy faction AI can react)
    EntityState.SetHiddenToAI(stealthUnit, false);

    // Query final state
    var flags = EntityState.GetStateFlags(stealthUnit);
    DevConsole.Log($"Stealth unit setup complete. Detection: 0x{flags.DetectionMask:X8}");
}

// Create a dying unit in extraction
public void ExtractDyingUnit(GameObj actor)
{
    // Mark as dying
    EntityState.SetDying(actor, true);

    // Mark as leaving map
    EntityState.SetLeavingMap(actor, true);

    // Hide from all factions during extraction
    EntityState.ConcealFromAll(actor);

    DevConsole.Log($"{actor.GetName()} is being extracted while dying");
}

// Heavy weapon deployment management
public void ManageHeavyWeapon(GameObj actor, bool shouldDeploy)
{
    var flags = EntityState.GetStateFlags(actor);

    if (shouldDeploy && !flags.IsHeavyWeaponDeployed)
    {
        if (EntityState.SetHeavyWeaponDeployed(actor, true))
        {
            DevConsole.Log("Heavy weapon deployed - accuracy bonus active");
        }
    }
    else if (!shouldDeploy && flags.IsHeavyWeaponDeployed)
    {
        if (EntityState.SetHeavyWeaponDeployed(actor, false))
        {
            DevConsole.Log("Heavy weapon undeployed - movement restored");
        }
    }
}
```

## See Also

- [EntityVisibility](entity-visibility.md) - Higher-level visibility control with temporary overrides
- [EntityAI](entity-ai.md) - AI behavior and threat manipulation
- [Intercept](intercept.md) - Event hooks for state changes
- [TacticalController](tactical-controller.md) - Entity selection and control
