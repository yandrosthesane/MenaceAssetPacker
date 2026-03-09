# Intercept

`Menace.SDK.Intercept` -- Central event registry for intercepting game methods with automatic Lua event firing.

## Overview

Intercept provides a declarative way to hook into game method calls without writing manual Harmony patches. Subscribe to events like `Intercept.OnGetDamage` to observe or modify game behavior.

Key features:
- **100+ interceptable methods** across entity properties, skills, combat, movement, AI, and more
- **Automatic Lua bridging** -- every C# event fires a corresponding Lua event
- **Clean unsubscription** -- remove handlers when your mod unloads
- **Type-safe delegates** -- each event has a specific signature with relevant parameters

## Quick Start

```csharp
using Menace.SDK;

public class MyPlugin : IModpackPlugin
{
    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Subscribe to damage calculations
        Intercept.OnGetDamage += OnDamageCalculated;

        // Subscribe to skill AP cost
        Intercept.OnSkillApCost += OnSkillApCost;
    }

    private void OnDamageCalculated(IntPtr instance, ref int result)
    {
        // Double all damage
        result *= 2;
    }

    private void OnSkillApCost(IntPtr skillPtr, IntPtr actorPtr, ref int result)
    {
        // Reduce all skill AP costs by 1
        if (result > 1) result--;
    }

    public void OnUnload()
    {
        // Clean up handlers
        Intercept.OnGetDamage -= OnDamageCalculated;
        Intercept.OnSkillApCost -= OnSkillApCost;
    }
}
```

## Event Categories

### Entity Properties

Property getter interceptors for entity stats. Modify `result` to change the returned value.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnGetDamage` | `(IntPtr, ref int)` | `property_damage` | Weapon/entity damage value |
| `OnGetAccuracy` | `(IntPtr, ref int)` | `property_accuracy` | Accuracy percentage |
| `OnGetArmor` | `(IntPtr, ref int)` | `property_armor` | Armor value |
| `OnGetMaxHealth` | `(IntPtr, ref int)` | `property_max_health` | Maximum hitpoints |
| `OnGetMoveRange` | `(IntPtr, ref int)` | `property_move_range` | Movement range in tiles |
| `OnGetSightRange` | `(IntPtr, ref int)` | `property_sight_range` | Vision range |
| `OnGetInitiative` | `(IntPtr, ref int)` | `property_initiative` | Turn order initiative |
| `OnGetWillpower` | `(IntPtr, ref int)` | `property_willpower` | Willpower stat |
| `OnGetStrength` | `(IntPtr, ref int)` | `property_strength` | Strength stat |
| `OnGetSpeed` | `(IntPtr, ref int)` | `property_speed` | Speed stat |
| `OnGetEndurance` | `(IntPtr, ref int)` | `property_endurance` | Endurance stat |

### Skill Interceptors

Hook into skill calculations and execution.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnSkillApCost` | `(IntPtr skill, IntPtr actor, ref int)` | `skill_ap_cost` | AP cost calculation |
| `OnSkillCooldown` | `(IntPtr skill, IntPtr actor, ref int)` | `skill_cooldown` | Cooldown duration |
| `OnSkillRange` | `(IntPtr skill, IntPtr actor, ref int)` | `skill_range` | Skill range |
| `OnSkillDamage` | `(IntPtr skill, IntPtr actor, ref int)` | `skill_damage` | Skill damage output |
| `OnSkillCanUse` | `(IntPtr skill, IntPtr actor, ref bool)` | `skill_can_use` | Whether skill is usable |
| `OnSkillExecute` | `(IntPtr skill, IntPtr actor)` | `skill_execute` | Skill execution (observe only) |

### Combat Actions

Intercept damage and combat effect application.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnDamageApplied` | `(GameObj handler, GameObj target, GameObj attacker, GameObj skill, ref float damage, ref bool cancel)` | `damage_applied` | Damage application before hitpoints are reduced (supports modification and cancellation) |

### Equipment Systems

Item container, property modification, and equipment stat bonus interceptors.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnItemAdd` | `(GameObj container, GameObj item, ref bool expandSlots, ref bool cancel)` | `item_add` | Fires when an item is added to a container (Address: 0x180821c80). Use for equipment restrictions, class-based loadouts, inventory limits. |
| `OnPropertyUpdate` | `(GameObj properties, int propertyType, ref int amount)` | `property_update` | Fires when additive stat bonuses are applied from items/skills/effects (Address: 0x18060d320). Master hook for ALL property modifications. Use for scaling equipment bonuses, capping stat stacking, difficulty-based modifiers. |
| `OnPropertyUpdateMult` | `(GameObj properties, int propertyType, ref float multiplier)` | `property_update_mult` | **Master hook for ALL multiplicative stat modifiers** (Address: 0x18060cc80). Fires when multiplier bonuses are applied. **STACKING: Multipliers stack additively as percentages** using `value += (mult - 1.0)`. Two +50% bonuses (1.5, 1.5) = 2.0x total, NOT 2.25x. See [Multiplier Property Types](#multiplier-property-types) for enum values. Use for difficulty-based scaling, damage multipliers, conditional bonuses. |

### Actor State

Monitor and modify actor state changes.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnMoraleApplied` | `(GameObj actor, int eventType, ref float amount, ref bool cancel)` | `morale_applied` | Morale change before application (supports cancel) |
| `OnSuppressionApplied` | `(GameObj actor, GameObj attacker, ref float amount, ref bool isFriendlyFire, ref bool cancel)` | `actor_suppression_applied` | Suppression before application (supports cancel) |
| `OnActorGetMoraleMax` | `(GameObj actor, float multiplier, ref int result)` | N/A | Maximum morale calculation |
| `OnActorGetMoralePct` | `(GameObj actor, ref float result)` | N/A | Morale percentage (0.0-1.0) |
| `OnActorGetMoraleState` | `(GameObj actor, ref int result)` | N/A | Morale state (1=Panicked, 2=Shaken, 3=Steady) |
| `OnActorGetSuppressionPct` | `(GameObj actor, ref float result)` | N/A | Suppression percentage |
| `OnActorGetSuppressionState` | `(GameObj actor, float additionalSuppression, ref int result)` | N/A | Suppression state (0=Normal, 1=Suppressed, 2=PinnedDown) |

### Entity State

Entity lifecycle and state events.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnEntitySpawn` | `(IntPtr entity)` | `entity_spawn` | Entity spawned |
| `OnEntityDeath` | `(IntPtr entity)` | `entity_death` | Entity died |
| `OnEntityRevive` | `(IntPtr entity)` | `entity_revive` | Entity revived |
| `OnEntityStateChange` | `(IntPtr entity, int state)` | `entity_state_change` | State changed |

### Tile & Map

Tile queries and map interactions.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnTileGetCover` | `(IntPtr tile, int dir, ref int)` | `tile_cover` | Cover value in direction |
| `OnTileIsBlocked` | `(IntPtr tile, ref bool)` | `tile_blocked` | Whether tile is impassable |
| `OnTileGetElevation` | `(IntPtr tile, ref float)` | `tile_elevation` | Tile elevation |
| `OnBaseTileGetMoveCost` | `(IntPtr tile, ref int)` | `basetile_move_cost` | Base movement cost |
| `OnLineOfSightCheck` | `(IntPtr from, IntPtr to, ref bool)` | `los_check` | LOS between points |

### Movement

Movement calculations and validation.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnPathfinding` | `(GameObj process, GameObj start, GameObj end, ref IntPtr pathResult, ref bool cancel)` | `pathfinding` | **Pathfinding hook** - fires when pathfinding calculates a route. Address: 0x180660c20. Set `cancel=true` to prevent pathfinding. Enables custom pathfinding, restricted zones, and forced routes. |
| `OnTileTraversable` | `(GameObj process, GameObj tile, ref bool result)` | `tile_traversable` | **Tile traversability check** - fires during pathfinding to check if a tile can be traversed. Address: 0x180662860. **WARNING: Called very frequently** - keep handlers fast! Modify `result` to block/allow tiles dynamically. Enables weather-based terrain restrictions, custom movement blocking, and dynamic tile access. |
| `OnMoveTo` | `(GameObj actor, GameObj tile, int flags, ref bool cancel)` | `actor_move_to` | **Master movement hook** - fires BEFORE all actor movement. Set `cancel=true` to prevent movement. Allows movement restrictions, teleportation, and tracking. |
| `OnMovementCostCalc` | `(IntPtr actor, IntPtr tile, ref int)` | `move_cost` | Movement cost to tile |
| `OnMovementRangeCalc` | `(IntPtr actor, ref int)` | `move_range` | Total movement range |
| `OnCanMoveTo` | `(IntPtr actor, IntPtr tile, ref bool)` | `can_move_to` | Whether actor can move to tile |

### Strategy

Campaign and strategy layer interceptors.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnMissionReward` | `(IntPtr mission, ref int)` | `mission_reward` | Mission completion reward |
| `OnItemValue` | `(IntPtr item, ref int)` | `item_value` | Item trade value |
| `OnLeaderXpGain` | `(IntPtr leader, ref int)` | `leader_xp` | XP gain amount |

### AI Behavior

AI decision-making interceptors.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnAIAttackScore` | `(IntPtr agent, IntPtr target, ref float)` | `ai_attack_score` | Attack evaluation score |
| `OnAIThreatValue` | `(IntPtr agent, IntPtr threat, ref float)` | `ai_threat_value` | Threat evaluation |
| `OnAIActionPriority` | `(IntPtr agent, IntPtr action, ref float)` | `ai_action_priority` | Action priority weight |
| `OnAIShouldFlee` | `(IntPtr agent, ref bool)` | `ai_should_flee` | Flee decision |
| `OnAIEvaluate` | `(GameObj agent, ref bool cancel)` | `ai_evaluate` | AI agent evaluation start (supports cancel) ⚠️ Thread-safe required |
| `OnPositionScore` | `(GameObj criterion, GameObj tile, ref float score)` | `position_score` | AI position evaluation scoring ⚠️ **PARALLEL** - Thread-safe required! |
| `OnSkillIsUsable` | `(GameObj skill, GameObj actor, ref bool result)` | `skill_is_usable` | Check if skill is usable by actor (Address: 0x1806deb10) |

## Delegate Signatures

All events use one of these delegate types:

```csharp
// Property getter - modify result to change the returned value
public delegate void PropertyIntHandler(IntPtr instance, ref int result);
public delegate void PropertyFloatHandler(IntPtr instance, ref float result);
public delegate void PropertyBoolHandler(IntPtr instance, ref bool result);

// Skill interceptors - access both skill and actor
public delegate void SkillIntHandler(IntPtr skill, IntPtr actor, ref int result);
public delegate void SkillBoolHandler(IntPtr skill, IntPtr actor, ref bool result);
public delegate void SkillVoidHandler(IntPtr skill, IntPtr actor);

// Two-target handlers (e.g., LOS checks)
public delegate void DualPtrBoolHandler(IntPtr a, IntPtr b, ref bool result);
public delegate void DualPtrFloatHandler(IntPtr a, IntPtr b, ref float result);
```

## Working with IntPtr

The `IntPtr` parameters are IL2CPP object pointers. Use `GameObj` to access fields:

```csharp
Intercept.OnGetDamage += (IntPtr ptr, ref int result) =>
{
    var obj = new GameObj(ptr);
    if (obj.IsNull) return;

    string name = obj.GetName();
    DevConsole.Log($"{name} damage: {result}");

    // Double damage for "Shotgun" weapons
    if (name.Contains("Shotgun"))
        result *= 2;
};
```

## Lua Equivalent

Every C# intercept event fires a corresponding Lua event:

```lua
-- Lua equivalent of Intercept.OnGetDamage
on("property_damage", function(data)
    log("Damage intercepted: " .. data.result)
    -- Note: Lua cannot modify the result (observe only)
end)

-- Skill interception
on("skill_ap_cost", function(data)
    log("Skill AP cost: " .. data.result)
end)
```

## Detailed Examples

### Morale System - OnMoraleApplied

The `OnMoraleApplied` event fires **before** morale changes are applied to an actor, allowing complete control over morale mechanics.

#### Signature

```csharp
public delegate void MoraleApplicationInterceptor(
    GameObj actor,      // The actor receiving morale change
    int eventType,      // MoraleEventType bitmask (cause of morale change)
    ref float amount,   // Morale amount to apply (can be positive or negative)
    ref bool cancel     // Set to true to cancel the morale change entirely
);
```

#### Basic Usage - Leader Morale Protection

Leaders are more resistant to morale loss:

```csharp
Intercept.OnMoraleApplied += (actor, eventType, ref amount, ref cancel) =>
{
    if (actor.IsNull) return;

    string name = actor.GetName();

    // Leaders resist 25% of morale loss
    if (name.Contains("Leader") && amount < 0)
    {
        amount *= 0.75f;
        DevConsole.Log($"{name} resisted morale loss: {amount}");
    }
};
```

#### Advanced - Rally Mechanics

Prevent morale loss when a leader is nearby:

```csharp
Intercept.OnMoraleApplied += (actor, eventType, ref amount, ref cancel) =>
{
    if (actor.IsNull || amount >= 0) return; // Only process morale loss

    // Check for nearby leader
    var nearbyActors = GetActorsInRadius(actor, 3); // 3 tile radius
    bool hasLeaderNearby = nearbyActors.Any(a =>
        !a.IsNull && a.GetName().Contains("Leader"));

    if (hasLeaderNearby)
    {
        // Leader presence prevents morale loss
        cancel = true;
        DevConsole.Log($"{actor.GetName()} rallied by nearby leader!");
    }
};
```

#### Elite Unit Morale Immunity

Veterans ignore certain morale events:

```csharp
Intercept.OnMoraleApplied += (actor, eventType, ref amount, ref cancel) =>
{
    if (actor.IsNull) return;

    // Get entity properties to check veteran status
    var props = actor.CallMethod<IntPtr>("GetEntityProperties");
    if (props == IntPtr.Zero) return;

    var propsObj = new GameObj(props);
    string templateName = propsObj.GetName();

    // Veterans are immune to suppression-related morale loss (eventType == 2)
    if (templateName.Contains("Veteran") && eventType == 2)
    {
        cancel = true;
        DevConsole.Log($"{actor.GetName()} (Veteran) immune to suppression morale loss");
    }
};
```

#### Morale Cascading

Spread morale changes to nearby units:

```csharp
Intercept.OnMoraleApplied += (actor, eventType, ref amount, ref cancel) =>
{
    if (actor.IsNull) return;

    // Only cascade significant morale changes
    if (Math.Abs(amount) < 5f) return;

    var nearbyActors = GetActorsInRadius(actor, 2);
    foreach (var nearbyActor in nearbyActors)
    {
        if (nearbyActor.IsNull || nearbyActor.GetPointer() == actor.GetPointer())
            continue;

        // Apply 30% of the morale change to nearby allies
        float cascadeAmount = amount * 0.3f;
        ApplyMoraleToActor(nearbyActor, eventType, cascadeAmount);
    }
};
```

#### Morale Event Types

The `eventType` parameter is a bitmask that indicates the cause of the morale change:

```csharp
// Common morale event types (from reverse engineering):
// 0x01 - Combat-related morale change
// 0x02 - Suppression-related morale loss
// 0x04 - Ally death nearby
// 0x08 - Enemy death nearby
// 0x10 - Mission objective related
// (Note: These are approximate - verify in-game)

Intercept.OnMoraleApplied += (actor, eventType, ref amount, ref cancel) =>
{
    // Different responses based on event type
    if ((eventType & 0x04) != 0) // Ally death
    {
        DevConsole.Log($"{actor.GetName()} morale affected by ally death: {amount}");
    }
    else if ((eventType & 0x08) != 0) // Enemy death
    {
        DevConsole.Log($"{actor.GetName()} morale boosted by enemy death: {amount}");
    }
};
```

#### Integration with Game State

The morale application process (from Ghidra decompilation @ 0x1805dd240):

1. **Early Exits**: Method returns if actor is invalid or disabled
2. **Morale Immunity Check**: EntityProps+0xEC bit 7 - if set, morale changes are ignored
3. **Event Type Validation**: Validates `eventType` against allowed morale events (EntityProps+0xA8 bitmask)
4. **Multiplier Applied**: Amount multiplied by EntityProps+0xBC (morale multiplier)
5. **Skill Container Event**: Calls SkillContainer.OnMoraleEvent(eventType, amount, 0)
6. **Intercept Fires Here**: `OnMoraleApplied` event fires with modified amount
7. **Clamping**: Final morale clamped between 0.0 and GetMoraleMax()
8. **SetMorale**: Final value stored at Actor+0x160 via SetMorale()

### Damage Application - OnDamageApplied

The `OnDamageApplied` event fires **before** damage is actually applied to an entity's hitpoints, providing complete control over damage mechanics. This is the core combat intercept point in the game.

#### Signature

```csharp
public delegate void DamageApplicationInterceptor(
    GameObj handler,    // The DamageHandler instance (effect data at handler+0x18)
    GameObj target,     // The entity receiving damage
    GameObj attacker,   // The entity dealing damage (skill owner)
    GameObj skill,      // The skill being used (may be null for direct damage)
    ref float damage,   // The total HP damage being applied (modify via ref)
    ref bool cancel     // Set to true to completely prevent damage application
);
```

#### Basic Usage - Critical Hits

Implement a critical hit system with random chance:

```csharp
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    if (target.IsNull || damage <= 0) return;

    // 10% chance for 2x critical hit
    if (Random.value < 0.1f)
    {
        damage *= 2.0f;
        DevConsole.Log($"CRITICAL HIT! {damage} damage to {target.GetName()}");

        // Optional: Visual feedback
        TacticalEventHooks.ShowFloatingText(target.Pointer, "CRITICAL!", Color.red);
    }
};
```

#### Damage Immunity

Grant complete damage immunity to specific units or bosses:

```csharp
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref bool cancel) =>
{
    if (target.IsNull) return;

    string targetName = target.GetName();

    // Boss units are immune to damage until shield is down
    if (targetName.Contains("Boss"))
    {
        var shieldActive = target.ReadField<bool>(0x1A0); // Custom shield field

        if (shieldActive)
        {
            cancel = true;
            DevConsole.Log($"{targetName} shield absorbed the attack!");
        }
    }
};
```

#### Conditional Damage Reduction

Reduce damage based on target properties:

```csharp
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    if (target.IsNull || attacker.IsNull) return;

    // Veterans take 25% less damage
    string targetName = target.GetName();
    if (targetName.Contains("Veteran"))
    {
        damage *= 0.75f;
        DevConsole.Log($"{targetName} veteran training reduced damage to {damage}");
    }

    // Flanking attacks deal 50% more damage
    bool isFlanked = CheckIfFlanked(target, attacker);
    if (isFlanked)
    {
        damage *= 1.5f;
        DevConsole.Log($"{targetName} flanked! Damage increased to {damage}");
    }
};
```

#### Damage Reflection

Reflect a portion of damage back to the attacker:

```csharp
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    if (target.IsNull || attacker.IsNull) return;

    // Units with "Thorns" trait reflect 20% of damage
    if (target.GetName().Contains("Thorns"))
    {
        float reflectedDamage = damage * 0.2f;

        // Apply damage back to attacker
        var attackerEntity = attacker;
        float currentHP = attackerEntity.ReadField<float>(0x54);
        attackerEntity.WriteField(0x54, currentHP - reflectedDamage);

        DevConsole.Log($"Thorns reflected {reflectedDamage} damage back to {attacker.GetName()}");
    }
};
```

#### Damage Over Time Tracking

Track all damage events for analytics or achievements:

```csharp
// Global damage tracker
static Dictionary<string, float> totalDamageDealt = new Dictionary<string, float>();

Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    if (cancel || attacker.IsNull) return;

    string attackerName = attacker.GetName();

    if (!totalDamageDealt.ContainsKey(attackerName))
        totalDamageDealt[attackerName] = 0f;

    totalDamageDealt[attackerName] += damage;

    // Check for "Destroyer" achievement (10000 damage dealt)
    if (totalDamageDealt[attackerName] >= 10000f)
    {
        DevConsole.Log($"{attackerName} unlocked DESTROYER achievement!");
    }
};
```

#### Context-Aware Damage Modification

Modify damage based on skill type and distance:

```csharp
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    if (skill.IsNull || target.IsNull || attacker.IsNull) return;

    string skillName = skill.GetName();

    // Explosive weapons deal bonus damage in close quarters
    if (skillName.Contains("Grenade") || skillName.Contains("Explosive"))
    {
        float distance = GetDistanceBetween(attacker, target);

        if (distance <= 2f) // Within 2 tiles
        {
            damage *= 1.3f; // +30% damage
            DevConsole.Log($"Close-range explosive! +30% damage: {damage}");
        }
    }

    // Sniper rifles lose damage in close range
    if (skillName.Contains("Sniper"))
    {
        float distance = GetDistanceBetween(attacker, target);

        if (distance < 5f)
        {
            damage *= 0.7f; // -30% damage
            DevConsole.Log($"Sniper too close! -30% damage: {damage}");
        }
    }
};
```

#### Integration with Game State

The damage application process (from Ghidra decompilation @ 0x180702970):

1. **Target Resolution**: Handler calls GetEntity(0) to get target entity
2. **Damage Calculation**:
   - Read effect data from handler+0x18
   - HP damage = DamageFlatAmount (0x64) + max(currentHP * pctCurrent, minCurrent) + max(maxHP * pctMax, minMax)
   - Hit count = FlatDamageBase (0x5c) + ceil(elementCount * elementsHitPct)
3. **Intercept Fires Here**: `OnDamageApplied` event with calculated damage
4. **Cancellation Check**: If cancel=true, method returns without applying damage
5. **Apply to Entity**: Calls Entity.ApplyDamage with DamageInfo structure
6. **Armor Damage**: Applies armor durability damage if specified
7. **Event Logging**: DevCombatLog.ReportHit logs the damage event

#### Damage Formula Components

The damage calculation uses multiple components from the effect data (handler+0x18):

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| 0x5c | FlatDamageBase | int | Base flat damage added to hit count |
| 0x60 | ElementsHitPercentage | float | Fraction (0-1) of target elements to hit |
| 0x64 | DamageFlatAmount | float | Flat HP damage |
| 0x68 | DamagePctCurrentHitpoints | float | % of current HP as damage |
| 0x6c | DamagePctCurrentHitpointsMin | float | Minimum floor for current HP% damage |
| 0x70 | DamagePctMaxHitpoints | float | % of max HP as damage |
| 0x74 | DamagePctMaxHitpointsMin | float | Minimum floor for max HP% damage |
| 0x78 | DamageToArmor | float | Flat armor durability damage |
| 0x7c | ArmorDmgPctCurrent | float | % of current armor as damage |
| 0x84 | ArmorPenetration | float | Reduces effective armor |
| 0x90 | CanCrit | bool | Whether damage can critically strike |

#### Best Practices

1. **Always null-check**: Target, attacker, and skill can be null
2. **Preserve original damage**: Store original value if you need to reference it
3. **Use cancel sparingly**: Canceling damage can break game balance
4. **Log modifications**: Use DevConsole.Log for debugging damage changes
5. **Performance**: Keep calculations lightweight - this fires for every damage event

### Suppression System - OnSuppressionApplied

Similar to morale, `OnSuppressionApplied` intercepts suppression before it's applied:

```csharp
Intercept.OnSuppressionApplied += (actor, attacker, ref amount, ref isFriendlyFire, ref cancel) =>
{
    if (actor.IsNull) return;

    // Heavy weapon teams ignore suppression from small arms
    if (actor.GetName().Contains("HeavyWeapon"))
    {
        amount *= 0.5f;
        DevConsole.Log($"{actor.GetName()} heavily armored - reduced suppression");
    }

    // Prevent friendly fire suppression in certain conditions
    if (isFriendlyFire && IsInProtectedZone(actor))
    {
        cancel = true;
        DevConsole.Log("Friendly fire suppression prevented in safe zone");
    }
};
```


## OnPropertyUpdate - Equipment Stat Modification

The `OnPropertyUpdate` event fires when **additive** property bonuses are applied to an entity from items, skills, passive effects, and other sources. This is the master hook for intercepting and modifying ALL stat modifications across all property types.

### EntityPropertyType Reference

The `propertyType` parameter uses the `EntityPropertyType` enum (0-70+):

| Type | Name | Offset | Data Type | Description |
|------|------|--------|-----------|-------------|
| 0 | MaxHitpoints | +0xC4 | int | Maximum health points |
| 1 | Accuracy | +0xA0 | float | Base hit chance |
| 2 | SightRange | +0xD4 | int | Vision distance |
| 3 | MovementRange | +0x68 | float | Movement distance per turn |
| 4 | ActionPoints | +0x1C | int | Actions available per turn |
| 5 | MovementCost | +0x14 | int | AP cost to move |
| 6 | Initiative | +0x34 | int | Turn order priority |
| 7 | Concealment | +0xCC | int | Stealth rating |
| 8 | Discipline | +0x10 | int | Morale/suppression resistance |
| 29 | Damage | +0x118 | float | Weapon damage bonus |
| ... | ... | ... | ... | 60+ additional property types |

### Example 1: Double HP Bonuses from Items

Grant double health bonuses from all equipment:

```csharp
Intercept.OnPropertyUpdate += (properties, propertyType, ref amount) =>
{
    if (propertyType == 0) // MaxHitpoints
    {
        amount *= 2;
        DevConsole.Log($"HP bonus doubled: {amount / 2} -> {amount}");
    }
};
```

### Example 2: Cap Movement Bonuses

Prevent excessive movement stacking by capping bonuses:

```csharp
Intercept.OnPropertyUpdate += (properties, propertyType, ref amount) =>
{
    if (propertyType == 3) // MovementRange
    {
        if (amount > 3)
        {
            DevConsole.Log($"Movement bonus capped from {amount} to 3");
            amount = 3;
        }
    }
};
```

### Example 3: Difficulty-Based Damage Scaling

Scale damage bonuses based on game difficulty:

```csharp
private float difficultyMultiplier = 0.5f; // Hard mode = 50% bonuses

Intercept.OnPropertyUpdate += (properties, propertyType, ref amount) =>
{
    if (propertyType == 29) // Damage bonus
    {
        var original = amount;
        amount = (int)(amount * difficultyMultiplier);
        DevConsole.Log($"Damage bonus scaled by difficulty: {original} -> {amount}");
    }
};
```

### Example 4: Conditional Property Filtering

Apply bonuses only for specific unit types:

```csharp
Intercept.OnPropertyUpdate += (properties, propertyType, ref amount) =>
{
    if (properties.IsNull) return;

    // Get the entity owner (may require owner resolution)
    var ownerPtr = properties.GetField<IntPtr>("_owner");
    if (ownerPtr == IntPtr.Zero) return;

    var owner = new GameObj(ownerPtr);
    var unitName = owner.GetName();

    // Heavy units get reduced initiative bonuses
    if (unitName.Contains("Heavy") && propertyType == 6) // Initiative
    {
        amount = (int)(amount * 0.5f);
        DevConsole.Log($"Heavy unit initiative penalty: {amount * 2} -> {amount}");
    }
};
```

### Example 5: Item Set Bonuses

Grant extra bonuses when multiple items from a set are equipped:

```csharp
private Dictionary<IntPtr, int> veteranItemCount = new();

Intercept.OnPropertyUpdate += (properties, propertyType, ref amount) =>
{
    if (properties.IsNull) return;

    var propsPtr = properties.Ptr;

    // Track "Veteran" item bonuses
    if (!veteranItemCount.ContainsKey(propsPtr))
        veteranItemCount[propsPtr] = 0;

    // If 3+ veteran items equipped, grant +50% to all bonuses
    if (veteranItemCount[propsPtr] >= 3)
    {
        var original = amount;
        amount = (int)(amount * 1.5f);
        DevConsole.Log($"Veteran set bonus! {original} -> {amount}");
    }
};
```

### Example 6: Property Type Logging & Analytics

Track which property types are being modified:

```csharp
private Dictionary<int, int> propertyModifications = new();

Intercept.OnPropertyUpdate += (properties, propertyType, ref amount) =>
{
    if (!propertyModifications.ContainsKey(propertyType))
        propertyModifications[propertyType] = 0;

    propertyModifications[propertyType] += amount;

    DevConsole.Log($"Property {propertyType} modified by {amount} (total: {propertyModifications[propertyType]})");
};
```

**Use Cases:**
- Equipment balancing and scaling
- Difficulty modifiers for stat bonuses
- Item set bonuses and synergies
- Class-specific equipment restrictions
- Stat cap enforcement
- Analytics and debugging


## OnPropertyUpdateMult - Multiplicative Stat Modifiers

The `OnPropertyUpdateMult` event fires when **multiplicative** property bonuses are applied to an entity. This is the master hook for ALL multiplier modifications including accuracy multipliers, damage multipliers, movement multipliers, and more.

**Address:** `0x18060cc80`

### Multiplier Stacking Formula

**CRITICAL:** Multipliers stack **additively as percentages**, NOT multiplicatively!

The game uses: `value += (mult - 1.0)`

**Example:**
- Base: `1.0`
- First +50% bonus (1.5): `1.0 + (1.5 - 1.0) = 1.5`
- Second +50% bonus (1.5): `1.5 + (1.5 - 1.0) = 2.0` ✓ (NOT 2.25)

This prevents exponential stacking abuse.

### Multiplier Values

| Value | Meaning | Effect |
|-------|---------|--------|
| `1.0` | Base (no change) | No bonus or penalty |
| `1.5` | +50% bonus | 50% increase |
| `2.0` | +100% bonus | Doubles the value |
| `0.5` | -50% penalty | Halves the value |
| `0.0` | -100% penalty | Completely removes value |

### Multiplier Property Types

The `propertyType` parameter uses the `EntityPropertyType` enum (multiplicative properties):

| Type | Name | Offset | Description |
|------|------|--------|-------------|
| 9 | MovementRangeMult | +0x6C | Movement distance multiplier |
| 10 | AccuracyMult | +0x84 | Hit chance multiplier |
| 11 | DamageMult | +0x8C | Damage output multiplier |
| 12 | ArmorPenMult | +0xDC | Armor penetration multiplier |
| 13 | ActionPointsMult | +0x28 | AP regeneration multiplier |
| 14 | VisionMult | +0x74 | Sight range multiplier |
| 15-28 | Various | Various | Other multiplier types |
| 35-36 | Various | Various | Additional multipliers |
| 41, 43-44 | Various | Various | More multipliers |
| 46-47, 49 | Various | Various | Special multipliers |
| 52, 55, 57 | Various | Various | Additional types |
| 60, 64, 67, 71 | Various | Various | Extended multipliers |

### Example 1: Difficulty-Based Accuracy Scaling

Grant bonus hit chance on easy mode, penalty on hard mode:

```csharp
private float difficultyAccuracyMod = 1.2f; // Easy mode: +20% hit chance

Intercept.OnPropertyUpdateMult += (properties, propertyType, ref multiplier) =>
{
    if (propertyType == 10) // AccuracyMult
    {
        var original = multiplier;
        multiplier *= difficultyAccuracyMod;
        DevConsole.Log($"Accuracy multiplier scaled: {original:F2} -> {multiplier:F2}");
    }
};
```

**Hard Mode Example:**
```csharp
difficultyAccuracyMod = 0.8f; // Hard mode: -20% hit chance
```

### Example 2: Elite Unit Damage Bonuses

Grant veteran units +50% damage output:

```csharp
Intercept.OnPropertyUpdateMult += (properties, propertyType, ref multiplier) =>
{
    if (propertyType == 11 && !properties.IsNull) // DamageMult
    {
        var ownerPtr = properties.GetField<IntPtr>("_owner");
        if (ownerPtr == IntPtr.Zero) return;

        var owner = new GameObj(ownerPtr);
        var unitName = owner.GetName();

        if (unitName.Contains("Elite") || unitName.Contains("Veteran"))
        {
            // Add +50% damage (remember: stacks additively!)
            var bonus = 1.5f;
            multiplier += (bonus - 1.0f); // Adds 0.5 to current multiplier
            DevConsole.Log($"Elite unit damage bonus: +50%");
        }
    }
};
```

### Example 3: Conditional Movement Multipliers

Grant speed bonuses based on health percentage:

```csharp
Intercept.OnPropertyUpdateMult += (properties, propertyType, ref multiplier) =>
{
    if (propertyType == 9 && !properties.IsNull) // MovementRangeMult
    {
        var ownerPtr = properties.GetField<IntPtr>("_owner");
        if (ownerPtr == IntPtr.Zero) return;

        var owner = new GameObj(ownerPtr);

        // Get health percentage
        var hp = owner.GetField<float>("_hitpoints");
        var maxHp = owner.GetField<float>("_maxHitpoints");
        var hpPct = hp / maxHp;

        if (hpPct < 0.3f) // Below 30% HP
        {
            // "Desperate sprint" bonus: +25% movement when wounded
            multiplier += 0.25f;
            DevConsole.Log($"Desperate sprint! Movement +25% (HP: {hpPct:P0})");
        }
    }
};
```

### Example 4: Equipment-Based Multiplier Scaling

Scale weapon accuracy based on equipment quality:

```csharp
Intercept.OnPropertyUpdateMult += (properties, propertyType, ref multiplier) =>
{
    if (propertyType == 10 && !properties.IsNull) // AccuracyMult
    {
        var ownerPtr = properties.GetField<IntPtr>("_owner");
        if (ownerPtr == IntPtr.Zero) return;

        var owner = new GameObj(ownerPtr);

        // Check for "Scope" item equipped
        var hasScope = CheckForEquippedItem(owner, "Scope");

        if (hasScope)
        {
            // Scope grants +15% accuracy
            multiplier += 0.15f;
            DevConsole.Log($"Scope equipped: Accuracy +15%");
        }
    }
};

private bool CheckForEquippedItem(GameObj entity, string itemName)
{
    // Implementation to check entity's inventory
    return false; // Placeholder
}
```

### Example 5: Global Damage Multiplier (Difficulty Mode)

Apply global damage scaling for all units:

```csharp
private float globalDamageScale = 1.0f; // 1.0 = normal, 0.75 = hard mode, 1.5 = easy mode

Intercept.OnPropertyUpdateMult += (properties, propertyType, ref multiplier) =>
{
    if (propertyType == 11) // DamageMult
    {
        var original = multiplier;

        // Scale ALL damage multipliers
        multiplier *= globalDamageScale;

        if (globalDamageScale != 1.0f)
        {
            DevConsole.Log($"Global damage scaling: {original:F2} -> {multiplier:F2}");
        }
    }
};

// Set difficulty
public void SetDifficulty(string difficulty)
{
    switch (difficulty)
    {
        case "Easy":
            globalDamageScale = 1.5f; // +50% player damage
            break;
        case "Normal":
            globalDamageScale = 1.0f;
            break;
        case "Hard":
            globalDamageScale = 0.75f; // -25% player damage
            break;
    }
}
```

### Example 6: Armor Penetration Nerfs

Reduce armor penetration for balance:

```csharp
Intercept.OnPropertyUpdateMult += (properties, propertyType, ref multiplier) =>
{
    if (propertyType == 12) // ArmorPenMult
    {
        // Cap armor pen multipliers at 1.5x (maximum +50% bonus)
        if (multiplier > 1.5f)
        {
            var original = multiplier;
            multiplier = 1.5f;
            DevConsole.Log($"ArmorPen capped: {original:F2} -> {multiplier:F2}");
        }
    }
};
```

### Example 7: Analytics & Multiplier Tracking

Track all multiplier modifications for debugging:

```csharp
private Dictionary<int, List<float>> multiplierHistory = new();

Intercept.OnPropertyUpdateMult += (properties, propertyType, ref multiplier) =>
{
    if (!multiplierHistory.ContainsKey(propertyType))
        multiplierHistory[propertyType] = new List<float>();

    multiplierHistory[propertyType].Add(multiplier);

    DevConsole.Log($"Property {propertyType} multiplier: {multiplier:F2} " +
                   $"(count: {multiplierHistory[propertyType].Count})");
};

// Print summary
public void PrintMultiplierSummary()
{
    foreach (var kvp in multiplierHistory)
    {
        var avg = kvp.Value.Average();
        var max = kvp.Value.Max();
        var min = kvp.Value.Min();
        DevConsole.Log($"Property {kvp.Key}: Avg={avg:F2}, Min={min:F2}, Max={max:F2}");
    }
}
```

### Understanding Additive Stacking

**Why additive stacking matters:**

```csharp
// WRONG ASSUMPTION (multiplicative):
// Item 1: 1.5x damage
// Item 2: 1.5x damage
// Total: 1.5 * 1.5 = 2.25x ❌

// CORRECT (additive percentages):
// Item 1: 1.5x = +50% = adds 0.5
// Item 2: 1.5x = +50% = adds 0.5
// Total: 1.0 + 0.5 + 0.5 = 2.0x ✓

Intercept.OnPropertyUpdateMult += (properties, propertyType, ref multiplier) =>
{
    // When you set multiplier, remember it stacks with OTHER calls!
    // If you want to add a +30% bonus:
    multiplier += 0.3f; // Correct

    // NOT:
    // multiplier *= 1.3f; // This would scale OTHER bonuses too!
};
```

**Use Cases:**
- Difficulty-based accuracy/damage scaling
- Elite unit bonuses (damage, movement, vision)
- Conditional multipliers (health-based, equipment-based)
- Equipment bonuses (scopes, armor upgrades)
- Balance patches (nerf/buff specific multipliers)
- Global game mode modifiers


## OnSuppressionApplied - Advanced Examples

The `OnSuppressionApplied` event fires **before** suppression is applied to an actor, allowing you to modify, redirect, or cancel suppression entirely.

### Example 1: Suppression Immunity for Veterans

Grant experienced units partial or full immunity to suppression:

```csharp
Intercept.OnSuppressionApplied += (actor, attacker, ref amount, ref isFriendlyFire, ref cancel) =>
{
    var actorObj = actor;
    if (actorObj.IsNull) return;

    var name = actorObj.GetName();

    // Veterans have 50% suppression resistance
    if (name.Contains("Veteran"))
    {
        amount *= 0.5f;
        DevConsole.Log($"{name} resists suppression! Reduced to {amount}");
    }

    // Elite units are immune to suppression
    if (name.Contains("Elite"))
    {
        cancel = true;
        DevConsole.Log($"{name} is immune to suppression!");
    }
};
```

### Example 2: Cascading Suppression

Apply partial suppression to nearby allies when a unit is heavily suppressed:

```csharp
Intercept.OnSuppressionApplied += (actor, attacker, ref amount, ref isFriendlyFire, ref cancel) =>
{
    // Only cascade heavy suppression (>50)
    if (amount <= 50f) return;

    // Get actor's position
    var actorObj = actor;
    if (actorObj.IsNull) return;

    var tile = actorObj.GetTile();
    if (tile.IsNull) return;

    // Find adjacent allies
    var adjacent = GameQuery.FindActorsInRadius(tile, 1);
    foreach (var ally in adjacent)
    {
        if (ally.IsNull || ally.Ptr == actor.Ptr) continue;

        var allyActor = ally.GetActor();
        if (allyActor.IsNull) continue;

        // Check same faction
        if (allyActor.GetFaction() == actorObj.GetActor().GetFaction())
        {
            // Apply 25% of suppression to adjacent allies
            float cascadeAmount = amount * 0.25f;
            DevConsole.Log($"Cascading {cascadeAmount} suppression to nearby {allyActor.GetName()}");
            // Note: You would need to manually apply suppression here via game methods
        }
    }
};
```

### Example 3: Friendly Fire Detection & Reporting

Log and modify friendly fire suppression incidents:

```csharp
Intercept.OnSuppressionApplied += (actor, attacker, ref amount, ref isFriendlyFire, ref cancel) =>
{
    if (!isFriendlyFire) return;

    var actorName = actor.IsNull ? "Unknown" : actor.GetName();
    var attackerName = attacker.IsNull ? "Unknown" : attacker.GetName();

    DevConsole.Warn($"FRIENDLY FIRE: {attackerName} suppressing {actorName} with {amount} suppression");

    // Reduce friendly fire suppression by 75%
    amount *= 0.25f;
};
```

### Example 4: Morale-Linked Suppression Resistance

High morale units resist suppression better:

```csharp
Intercept.OnSuppressionApplied += (actor, attacker, ref amount, ref isFriendlyFire, ref cancel) =>
{
    var actorObj = actor.GetActor();
    if (actorObj.IsNull) return;

    // Get morale percentage (0.0 to 1.0)
    var moralePct = actorObj.GetMoralePct();

    // High morale = better suppression resistance
    // At 100% morale: 40% resistance
    // At 50% morale: 20% resistance
    // At 0% morale: 0% resistance
    float resistancePct = moralePct * 0.4f;
    amount *= (1.0f - resistancePct);

    DevConsole.Log($"{actorObj.GetName()} morale {moralePct:P0} reduces suppression by {resistancePct:P0}");
};
```

### Example 5: Analytics & Tracking

Track all suppression events for post-mission analysis:

```csharp
private List<SuppressionEvent> _suppressionLog = new List<SuppressionEvent>();

Intercept.OnSuppressionApplied += (actor, attacker, ref amount, ref isFriendlyFire, ref cancel) =>
{
    _suppressionLog.Add(new SuppressionEvent
    {
        ActorName = actor.IsNull ? null : actor.GetName(),
        AttackerName = attacker.IsNull ? null : attacker.GetName(),
        Amount = amount,
        IsFriendlyFire = isFriendlyFire,
        WasCancelled = cancel,
        Timestamp = Time.time
    });
};

// At end of mission
public void OnMissionEnd()
{
    DevConsole.Log($"Total suppression events: {_suppressionLog.Count}");
    var friendlyFire = _suppressionLog.Count(e => e.IsFriendlyFire);
    DevConsole.Log($"Friendly fire incidents: {friendlyFire}");
}
```

### Lua Example

```lua
on("actor_suppression_applied", function(data)
    local actor_ptr = data.actor_ptr
    local amount = data.amount
    local is_ff = data.is_friendly_fire

    if is_ff then
        log("Friendly fire suppression: " .. amount)
    end

    -- Note: Lua receives the final values after C# handlers run
    -- Lua cannot modify amount or cancel the event
end)
```

### Key Points

- **Timing**: Fires BEFORE suppression is applied (prefix patch)
- **Modification**: You can modify `amount` and `isFriendlyFire` via ref parameters
- **Cancellation**: Set `cancel = true` to completely prevent suppression application
- **Attacker**: May be null if suppression source is environmental or indirect
- **Formula**: Base game applies discipline modifier `(1 - discipline * 0.01)` after your handler
- **Storage**: Suppression is stored at Actor+0x15C in game memory

## Usage Examples

### Movement Restriction Example

Prevent actors from moving into specific terrain types:

```csharp
using Menace.SDK;

public class MovementRestrictionMod : IModpackPlugin
{
    private HashSet<IntPtr> _restrictedTiles = new HashSet<IntPtr>();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Subscribe to movement hook
        Intercept.OnMoveTo += OnActorMoveTo;

        // Mark certain tiles as restricted (example)
        // In reality, you'd identify tiles via map query or tile properties
    }

    private void OnActorMoveTo(GameObj actor, GameObj tile, int flags, ref bool cancel)
    {
        // Null checks
        if (actor.IsNull || tile.IsNull) return;

        // Check if tile is restricted
        if (_restrictedTiles.Contains(tile.Ptr))
        {
            // Cancel movement
            cancel = true;

            // Optional: Show feedback to player
            if (IsPlayerControlled(actor))
            {
                ShowMessage("Cannot move there - terrain is blocked!");
            }
        }

        // Example: Prevent movement during certain conditions
        if (IsActorStunned(actor))
        {
            cancel = true;
        }
    }

    private bool IsPlayerControlled(GameObj actor)
    {
        // Check if actor is player-controlled
        // Implementation depends on game API
        return actor.GetField<bool>("IsPlayerControlled");
    }

    private bool IsActorStunned(GameObj actor)
    {
        // Check stun status
        return actor.GetField<bool>("IsStunned");
    }

    public void OnUnload()
    {
        Intercept.OnMoveTo -= OnActorMoveTo;
    }
}
```

### Teleportation Example

Intercept movement to create teleportation pads:

```csharp
private void OnActorMoveTo(GameObj actor, GameObj tile, int flags, ref bool cancel)
{
    if (actor.IsNull || tile.IsNull) return;

    // Check if tile is a teleporter
    var tileData = GameQuery.ReadStructAt<TileData>(tile.Ptr);
    if (tileData.IsTeleporter)
    {
        // Cancel normal movement
        cancel = true;

        // Teleport to destination
        var destinationTile = GetTeleportDestination(tile);
        if (!destinationTile.IsNull)
        {
            TeleportActorTo(actor, destinationTile);
        }
    }
}
```

### Tile Traversability Example

Block tiles dynamically based on custom conditions:

```csharp
using Menace.SDK;

public class WeatherTerrainMod : IModpackPlugin
{
    private HashSet<IntPtr> _floodedTiles = new();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Subscribe to tile traversability checks
        Intercept.OnTileTraversable += OnTileTraversable;
    }

    private void OnTileTraversable(GameObj process, GameObj tile, ref bool result)
    {
        if (tile.IsNull) return;

        // PERFORMANCE: Keep this fast - called for every tile during pathfinding

        // Block flooded tiles
        if (_floodedTiles.Contains(tile.Ptr))
        {
            result = false;
            return;
        }

        // Block tiles with custom "LAVA" flag
        if (HasCustomFlag(tile, "LAVA"))
        {
            result = false;
        }
    }

    public void FloodTile(GameObj tile)
    {
        // Add tile to flooded set
        _floodedTiles.Add(tile.Ptr);

        // Force pathfinding recalculation if needed
        InvalidatePathCache();
    }

    public void OnUnload()
    {
        Intercept.OnTileTraversable -= OnTileTraversable;
    }
}
```

**Performance Note:** OnTileTraversable is called thousands of times per turn. Use fast lookups (HashSet, Dictionary) and avoid expensive operations like file I/O or complex calculations.

### Pathfinding Restriction Example

Prevent pathfinding through restricted zones:

```csharp
using Menace.SDK;
using System.Collections.Generic;

public class PathfindingRestrictionMod : IModpackPlugin
{
    private HashSet<IntPtr> _restrictedZones = new HashSet<IntPtr>();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Subscribe to pathfinding hook
        Intercept.OnPathfinding += OnPathfindingCalculation;
    }

    private void OnPathfindingCalculation(GameObj process, GameObj start, GameObj end,
        ref IntPtr pathResult, ref bool cancel)
    {
        // Null checks
        if (start.IsNull || end.IsNull) return;

        // Check if destination is in a restricted zone
        if (_restrictedZones.Contains(end.Pointer))
        {
            // Cancel pathfinding - no path will be found
            cancel = true;
            return;
        }

        // Example: Log pathfinding requests for debugging
        var startPos = start.ReadField<Vector3>("Position");
        var endPos = end.ReadField<Vector3>("Position");
        SdkLogger.Msg($"Pathfinding: {startPos} -> {endPos}");
    }

    public void OnUnload()
    {
        Intercept.OnPathfinding -= OnPathfindingCalculation;
    }
}
```

### Skill Usability Restrictions Example

Control when skills are usable based on custom conditions:

```csharp
using Menace.SDK;
using System.Collections.Generic;

public class SkillRestrictionMod : IModpackPlugin
{
    // Define class-restricted skills (skill name -> allowed unit types)
    private Dictionary<string, HashSet<string>> _classRestrictedSkills = new Dictionary<string, HashSet<string>>();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Subscribe to skill usability check
        Intercept.OnSkillIsUsable += OnSkillIsUsable;

        // Define class restrictions
        _classRestrictedSkills["Psionics"] = new HashSet<string> { "Psionic", "Commander" };
        _classRestrictedSkills["HeavyWeapons"] = new HashSet<string> { "Heavy", "Support" };
    }

    private void OnSkillIsUsable(GameObj skill, GameObj actor, ref bool result)
    {
        // Null checks
        if (skill.IsNull || actor.IsNull) return;

        // If skill is already unusable, no need to check further
        if (!result) return;

        // Get skill and actor names
        string skillName = skill.GetName();
        string actorClass = GetActorClass(actor);

        // Check class restrictions
        if (_classRestrictedSkills.TryGetValue(skillName, out var allowedClasses))
        {
            if (!allowedClasses.Contains(actorClass))
            {
                // Restrict skill usage
                result = false;
                return;
            }
        }

        // Example: Conditional availability based on morale
        float morale = GetActorMorale(actor);
        if (skillName.Contains("Desperate") && morale > 0.3f)
        {
            // "Desperate" skills only available at low morale
            result = false;
            return;
        }

        // Example: Equipment requirements
        if (skillName == "SniperShot" && !HasSniperRifle(actor))
        {
            result = false;
            return;
        }

        // Example: Cooldown modification for veterans
        if (IsVeteran(actor))
        {
            // Veterans can use skills more frequently
            // This would require modifying cooldown state, which
            // should be done via a separate cooldown intercept
        }
    }

    private string GetActorClass(GameObj actor)
    {
        // Read actor's unit class from properties
        // Implementation depends on game structure
        return actor.GetField<string>("UnitClass") ?? "Infantry";
    }

    private float GetActorMorale(GameObj actor)
    {
        // Read current morale percentage
        return actor.GetField<float>("MoralePct");
    }

    private bool HasSniperRifle(GameObj actor)
    {
        // Check if actor has sniper rifle equipped
        var weaponPtr = actor.ReadPtr(0x20); // Example offset
        if (weaponPtr == IntPtr.Zero) return false;

        var weapon = new GameObj(weaponPtr);
        return weapon.GetName().Contains("Sniper");
    }

    private bool IsVeteran(GameObj actor)
    {
        // Check veteran status
        return actor.GetField<int>("ExperienceLevel") >= 3;
    }

    public void OnUnload()
    {
        Intercept.OnSkillIsUsable -= OnSkillIsUsable;
    }
}
```

### AI Difficulty Modifier Example

Modify AI behavior evaluation to implement difficulty levels:

```csharp
using Menace.SDK;
using System.Threading;

public class AIBehaviorMod : IModpackPlugin
{
    private float _aiDifficultyMultiplier = 1.0f;
    private readonly object _lockObject = new object();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Subscribe to AI evaluation hook
        Intercept.OnAIEvaluate += OnAIEvaluate;

        // Set difficulty based on player choice
        SetAIDifficulty(DifficultyLevel.Hard);
    }

    private void OnAIEvaluate(GameObj agent, ref bool cancel)
    {
        // CRITICAL: This may be called in parallel - use thread-safe code!
        if (agent.IsNull) return;

        // Thread-safe check of difficulty multiplier
        float multiplier;
        lock (_lockObject)
        {
            multiplier = _aiDifficultyMultiplier;
        }

        // Example: On easy difficulty, randomly skip some AI turns
        if (multiplier < 1.0f)
        {
            // Use thread-safe random (ThreadStatic or ThreadLocal)
            if (ThreadSafeRandom.NextDouble() > multiplier)
            {
                // Cancel evaluation - AI does nothing this turn
                cancel = true;
                SdkLogger.Msg($"[AI] Skipped evaluation for {agent.GetName()} (difficulty: {multiplier})");
            }
        }

        // Example: Force specific behavior based on custom logic
        var actor = agent.GetField<GameObj>("m_actor");
        if (!actor.IsNull)
        {
            var health = actor.GetField<float>("m_health");
            var maxHealth = actor.GetField<float>("m_maxHealth");

            // Force AI to skip turn if critically wounded and on easy mode
            if (health / maxHealth < 0.2f && multiplier < 1.0f)
            {
                cancel = true;
            }
        }
    }

    public void SetAIDifficulty(DifficultyLevel level)
    {
        lock (_lockObject)
        {
            _aiDifficultyMultiplier = level switch
            {
                DifficultyLevel.Easy => 0.7f,    // 30% chance to skip turns
                DifficultyLevel.Normal => 1.0f,
                DifficultyLevel.Hard => 1.0f,    // No skipping
                _ => 1.0f
            };
        }
        SdkLogger.Msg($"[AI] Difficulty set to {level} (multiplier: {_aiDifficultyMultiplier})");
    }

    public void OnUnload()
    {
        Intercept.OnAIEvaluate -= OnAIEvaluate;
    }

    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard
    }
}

// Thread-safe random number generator
public static class ThreadSafeRandom
{
    [ThreadStatic]
    private static Random _random;

    public static double NextDouble()
    {
        if (_random == null)
            _random = new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId);
        return _random.NextDouble();
    }
}
```

**Threading Warning**: `OnAIEvaluate` uses parallel execution via `System.Threading.Tasks`. Multiple agents may evaluate simultaneously. Your handler code MUST be thread-safe:
- Use locks for shared state
- Use `ThreadStatic` or `ThreadLocal` for per-thread data
- Avoid modifying shared collections without synchronization
- Keep the handler fast - blocking will slow down all AI

## Best Practices

### 1. Always Unsubscribe

```csharp
public void OnUnload()
{
    Intercept.OnGetDamage -= MyDamageHandler;
}
```

### 2. Keep Handlers Fast

Intercept handlers run on the game thread during method execution. Slow handlers will cause lag:

```csharp
// GOOD - fast check
Intercept.OnGetDamage += (ptr, ref int result) =>
{
    if (_doubleDamageEnabled) result *= 2;
};

// BAD - expensive operation in hot path
Intercept.OnGetDamage += (ptr, ref int result) =>
{
    var weapons = GameQuery.FindAll("WeaponTemplate"); // Slow!
    // ...
};
```

### 3. Use Null Checks

```csharp
Intercept.OnSkillApCost += (IntPtr skill, IntPtr actor, ref int result) =>
{
    if (skill == IntPtr.Zero || actor == IntPtr.Zero) return;

    var skillObj = new GameObj(skill);
    if (skillObj.IsNull) return;

    // Safe to proceed
};
```

## See Also

- [PatchSet](patchset.md) -- Fluent Harmony patching for custom method hooks
- [GamePatch](game-patch.md) -- Basic Harmony patching
- [Lua Scripting](../../modding-guides/11-lua-scripting.md) -- Lua intercept events

### Equipment System - OnItemAdd

The `OnItemAdd` event fires **before** an item is added to a container, allowing complete control over equipment and inventory management.

#### Signature

```csharp
public delegate void ItemAddInterceptor(
    GameObj container,      // The ItemContainer instance
    GameObj item,           // The Item being added
    ref bool expandSlots,   // Whether to expand slots if container is full (can be modified)
    ref bool cancel         // Set to true to prevent item addition
);
```

#### Basic Usage - Class-Based Equipment Restrictions

Prevent certain unit classes from equipping specific item types:

```csharp
Intercept.OnItemAdd += (container, item, ref expandSlots, ref cancel) =>
{
    if (container.IsNull || item.IsNull) return;

    // Get container owner
    var ownerPtr = container.ReadPtr(0x18); // Container+0x18 = owner entity
    if (ownerPtr == IntPtr.Zero) return;

    var owner = new GameObj(ownerPtr);
    string unitClass = GetUnitClass(owner);

    // Get item template to check type
    var templatePtr = item.ReadPtr(0x10);
    if (templatePtr == IntPtr.Zero) return;

    var template = new GameObj(templatePtr);
    string itemName = template.GetName();

    // Scouts cannot use heavy weapons
    if (unitClass == "Scout" && itemName.Contains("HeavyWeapon"))
    {
        cancel = true;
        DevConsole.Log("Scouts cannot use heavy weapons!");
        return;
    }

    // Medics can only use medical equipment and sidearms
    if (unitClass == "Medic" &&
        !itemName.Contains("Medical") &&
        !itemName.Contains("Pistol"))
    {
        cancel = true;
        DevConsole.Log("Medics can only use medical equipment and sidearms!");
        return;
    }
};
```

#### Inventory Slot Management

Control container auto-expansion behavior:

```csharp
Intercept.OnItemAdd += (container, item, ref expandSlots, ref cancel) =>
{
    if (container.IsNull) return;

    string containerName = container.GetName();

    // Disable auto-expand for limited storage containers
    if (containerName.Contains("LimitedStorage") ||
        containerName.Contains("QuickSlot"))
    {
        expandSlots = false;
        DevConsole.Log($"{containerName} cannot auto-expand");
    }

    // Force expansion for dynamic containers
    if (containerName.Contains("DynamicBackpack"))
    {
        expandSlots = true;
    }
};
```

#### Weight-Based Inventory Limits

Implement a weight-based inventory system:

```csharp
private Dictionary<IntPtr, float> _containerWeights = new Dictionary<IntPtr, float>();
private const float MAX_WEIGHT = 100f;

Intercept.OnItemAdd += (container, item, ref expandSlots, ref cancel) =>
{
    if (container.IsNull || item.IsNull) return;

    // Get item weight from template
    var templatePtr = item.ReadPtr(0x10);
    if (templatePtr == IntPtr.Zero) return;

    var template = new GameObj(templatePtr);
    float itemWeight = template.ReadFloat(0x50); // Assuming weight at offset 0x50

    // Get current container weight
    var containerPtr = container.Pointer;
    if (!_containerWeights.ContainsKey(containerPtr))
        _containerWeights[containerPtr] = 0f;

    float currentWeight = _containerWeights[containerPtr];

    // Check weight limit
    if (currentWeight + itemWeight > MAX_WEIGHT)
    {
        cancel = true;
        DevConsole.Log($"Container overweight! {currentWeight + itemWeight}/{MAX_WEIGHT} kg");
        return;
    }

    // Update weight tracking
    _containerWeights[containerPtr] += itemWeight;
};
```

#### Item Type Filtering

Block specific item types from certain containers:

```csharp
Intercept.OnItemAdd += (container, item, ref expandSlots, ref cancel) =>
{
    if (container.IsNull || item.IsNull) return;

    // Get container and item types
    string containerName = container.GetName();

    var templatePtr = item.ReadPtr(0x10);
    if (templatePtr == IntPtr.Zero) return;

    var template = new GameObj(templatePtr);
    int itemType = template.ReadInt(0xE8); // ItemType enum at template+0xE8

    // Weapon racks only accept weapons (types 0-2)
    if (containerName.Contains("WeaponRack") && itemType > 2)
    {
        cancel = true;
        DevConsole.Log("Weapon racks only accept weapons!");
        return;
    }

    // Medical containers only accept medical items (type 5)
    if (containerName.Contains("MedicalBag") && itemType != 5)
    {
        cancel = true;
        DevConsole.Log("Medical containers only accept medical items!");
        return;
    }

    // Block ammo from personal inventory (force storage in ammo pouches)
    if (containerName.Contains("PersonalInventory") && itemType == 7)
    {
        cancel = true;
        DevConsole.Log("Ammo must be stored in ammo pouches!");
        return;
    }
};
```

#### Unique Item Enforcement

Prevent multiple copies of unique items:

```csharp
private HashSet<string> _uniqueItemNames = new HashSet<string>
{
    "Legendary_Sword",
    "Ancient_Artifact",
    "Commander_Badge"
};

Intercept.OnItemAdd += (container, item, ref expandSlots, ref cancel) =>
{
    if (container.IsNull || item.IsNull) return;

    // Get item name
    var templatePtr = item.ReadPtr(0x10);
    if (templatePtr == IntPtr.Zero) return;

    var template = new GameObj(templatePtr);
    string itemName = template.GetName();

    // Check if item is unique
    if (_uniqueItemNames.Contains(itemName))
    {
        // Check if container already has this unique item
        var ownerPtr = container.ReadPtr(0x18);
        if (ownerPtr != IntPtr.Zero)
        {
            var owner = new GameObj(ownerPtr);
            if (HasUniqueItem(owner, itemName))
            {
                cancel = true;
                DevConsole.Log($"Cannot add {itemName} - unique item already equipped!");
                return;
            }
        }
    }
};

private bool HasUniqueItem(GameObj owner, string itemName)
{
    // Implementation would scan all containers for the unique item
    // This is a simplified example
    return false;
}
```

#### Loadout Validation

Enforce class-based loadout requirements:

```csharp
private Dictionary<string, List<string>> _requiredLoadouts = new Dictionary<string, List<string>>
{
    ["Sniper"] = new List<string> { "SniperRifle", "Sidearm", "Ghillie" },
    ["Assault"] = new List<string> { "AssaultRifle", "Grenade", "Armor" },
    ["Medic"] = new List<string> { "MedicalKit", "Sidearm", "Stimpack" }
};

Intercept.OnItemAdd += (container, item, ref expandSlots, ref cancel) =>
{
    if (container.IsNull || item.IsNull) return;

    // Get owner class
    var ownerPtr = container.ReadPtr(0x18);
    if (ownerPtr == IntPtr.Zero) return;

    var owner = new GameObj(ownerPtr);
    string unitClass = GetUnitClass(owner);

    // Get item category
    var templatePtr = item.ReadPtr(0x10);
    if (templatePtr == IntPtr.Zero) return;

    var template = new GameObj(templatePtr);
    string itemCategory = GetItemCategory(template);

    // Check if this item type is allowed for this class
    if (_requiredLoadouts.TryGetValue(unitClass, out var allowedCategories))
    {
        if (!allowedCategories.Any(cat => itemCategory.Contains(cat)))
        {
            cancel = true;
            DevConsole.Log($"{unitClass} cannot equip {itemCategory}!");
            return;
        }
    }
};
```

#### Integration with Game State

The item addition process (from Ghidra decompilation @ 0x180821c80):

1. **Null Validation**: Returns false if item is null or already has a container
2. **Vehicle Items**: Special handling for modular vehicle items (types 8, 9, 10)
3. **Slot Search**: Iterates through container slots for the item's type
4. **Full Container Check**: If no empty slots found:
   - If `expandSlots == false`: Returns 0 (failure)
   - If `expandSlots == true`: Calls AddSlots() and retries recursively
5. **Intercept Fires Here**: `OnItemAdd` event fires before placement
6. **Cancellation Check**: If cancel=true, returns without adding item
7. **Item Placement**: Sets item in empty slot via set_Item()
8. **Callback**: Calls OnItemAdded() to notify container update
9. **Returns**: 1 (success)

#### Container Structure

Key offsets for ItemContainer (from decompilation):

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| 0x10 | Slots | List<List<Item>> | Container slots organized by item type |
| 0x18 | Owner | Entity | Entity that owns this container |
| 0x20 | ModularVehicle | ItemsModularVehicle | Vehicle item reference (if applicable) |

#### Item Structure

Key offsets for Item (from decompilation):

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| 0x10 | Template | DataTemplate | Item template (for name, type, stats) |
| 0x28 | Container | ItemContainer | Current container (null if unequipped) |

#### Best Practices

1. **Always null-check**: Container and item can be null in edge cases
2. **Check item template**: Template contains type, category, and restrictions
3. **Read owner carefully**: Owner may be null for storage containers
4. **Use expandSlots wisely**: Setting to false prevents auto-expansion
5. **Log restrictions**: Help players understand why items can't be equipped
6. **Performance**: Keep handlers fast - called on every equip action

### AI Position Scoring Example

Modify AI position evaluation to implement custom positioning logic:

```csharp
using Menace.SDK;
using System.Threading;

public class AIPositioningMod : IModpackPlugin
{
    // CRITICAL: All fields must be thread-safe as OnPositionScore fires in parallel!
    private volatile bool _preferHighGround = true;
    private volatile bool _preferFlanking = true;

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Subscribe to position scoring hook
        Intercept.OnPositionScore += OnPositionScore;
    }

    private void OnPositionScore(GameObj criterion, GameObj tile, ref float score)
    {
        // WARNING: Called in PARALLEL for multiple tiles simultaneously!
        // MUST be thread-safe - NO state modification, only reads!
        // Keep this FAST - called 20-50+ times per agent per turn!

        // Null checks
        if (criterion.IsNull || tile.IsNull) return;

        // Example 1: High ground bonus
        if (_preferHighGround)
        {
            // Get tile elevation (assuming tile has height field)
            var elevation = tile.GetField<float>("m_elevation");
            if (elevation > 5.0f)
            {
                // Bonus for high ground (thread-safe modification of ref parameter)
                score += 10.0f;
            }
        }

        // Example 2: Flanking position bonus
        if (_preferFlanking)
        {
            // Check if this position would flank enemies
            // This is a simplified example - real implementation would need
            // to query nearby enemy positions safely
            var isFlanking = CheckIfFlankingPosition(tile);
            if (isFlanking)
            {
                score += 15.0f;
            }
        }

        // Example 3: Environmental penalties
        // Check tile type and apply penalties
        var tileType = tile.GetField<int>("m_type");
        if (tileType == 3) // Assuming 3 = hazardous terrain
        {
            score -= 20.0f;
        }

        // Example 4: Formation bonuses - stay near allies
        var nearbyAllies = CountNearbyAllies(tile);
        if (nearbyAllies > 0)
        {
            score += nearbyAllies * 5.0f; // +5 per nearby ally
        }
    }

    private bool CheckIfFlankingPosition(GameObj tile)
    {
        // CRITICAL: This must be thread-safe!
        // Read-only queries are safe, but avoid heavy computation
        // For performance, keep this simple
        return false; // Placeholder
    }

    private int CountNearbyAllies(GameObj tile)
    {
        // CRITICAL: Thread-safe read-only operation
        return 0; // Placeholder
    }

    public void OnUnload()
    {
        Intercept.OnPositionScore -= OnPositionScore;
    }
}
```

**Thread Safety Notes for OnPositionScore:**

1. **PARALLEL EXECUTION**: This event fires on Unity job threads for EVERY tile being evaluated (20-50+ per agent)
2. **NO STATE MODIFICATION**: Don't modify game state or shared variables - only read and modify the `score` parameter
3. **NO HEAVY COMPUTATION**: Keep logic fast - avoid loops, complex queries, or expensive operations
4. **NO I/O**: Never do file operations, network calls, or logging in this handler
5. **READ-ONLY**: Game state reads are safe, but modifications will cause race conditions
6. **VOLATILE/LOCKS**: If you need to read configuration, use `volatile` fields or proper locking

**Use Cases:**
- Custom positioning preferences (high ground, flanking, formations)
- Environmental awareness (avoid hazards, prefer cover)
- Tactical bonuses (support positions, overlapping fields of fire)
- Formation-based AI (maintain unit cohesion)

