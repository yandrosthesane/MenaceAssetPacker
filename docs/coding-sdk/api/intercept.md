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

### Actor State

Monitor and modify actor state changes.

| Event | Signature | Lua Event | Description |
|-------|-----------|-----------|-------------|
| `OnActorTakeDamage` | `(IntPtr actor, ref int damage)` | `actor_take_damage` | Damage being applied |
| `OnActorHeal` | `(IntPtr actor, ref int amount)` | `actor_heal` | Healing being applied |
| `OnActorSuppression` | `(IntPtr actor, ref int amount)` | `actor_suppression` | Suppression being applied |
| `OnActorMoraleChange` | `(IntPtr actor, ref int delta)` | `actor_morale_change` | Morale change amount |
| `OnActorApChange` | `(IntPtr actor, ref int delta)` | `actor_ap_change` | AP change amount |

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
