# Action API Guide - Taking Control of the Battlefield

This comprehensive guide shows you how to use the new **Action API** system to directly control entities, manipulate tactical combat, and create powerful gameplay modifications. The Action API provides 52 methods across 6 modules that give you unprecedented control over the game.

## Table of Contents

- [Introduction](#introduction)
  - [What are Action APIs?](#what-are-action-apis)
  - [Action APIs vs Intercepts](#action-apis-vs-intercepts)
  - [When to Use Which](#when-to-use-which)
  - [The Six Modules](#the-six-modules)
- [Module Reference Table](#module-reference-table)
- [Common Patterns](#common-patterns)
- [Real-World Examples](#real-world-examples)
- [REPL Testing Guide](#repl-testing-guide)
- [Integration with Intercepts](#integration-with-intercepts)
- [Performance Considerations](#performance-considerations)
- [Troubleshooting](#troubleshooting)
- [Advanced Techniques](#advanced-techniques)

---

## Introduction

### What are Action APIs?

Action APIs are SDK modules that let you **do things** to the game state. They provide methods to:

- Move units around the battlefield
- Make AI decisions
- Modify skill parameters
- Change entity visibility
- Manipulate tile properties
- Control combat outcomes

Think of them as your modding toolkit - direct methods for changing the game state.

### Action APIs vs Intercepts

**Intercepts** (OnDamageApplied, OnMoveTo, etc.) are **passive observers** that fire when the game does something:

```csharp
// INTERCEPT: Observe and modify when damage happens
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    damage *= 2.0f; // Double all damage
};
```

**Actions** are **active controllers** that tell the game to do something:

```csharp
// ACTION: Make an entity take damage directly
EntityCombat.ApplyDamage(enemy, 50); // Deal 50 damage now
```

### When to Use Which

| Scenario | Use Intercepts | Use Actions |
|----------|----------------|-------------|
| Modify existing behavior | ✅ Yes | ❌ No |
| Create new behavior | ❌ Limited | ✅ Yes |
| React to game events | ✅ Yes | ❌ No |
| Control game directly | ❌ No | ✅ Yes |
| Conditional logic | ✅ Both | ✅ Both |

**Best Practice:** Combine them! Use intercepts to detect conditions, then use actions to respond:

```csharp
// Intercept to detect condition
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    if (damage > target.GetHealth() * 0.5f) // More than 50% HP damage
    {
        // Action to respond
        EntityAI.ForceFleeDecision(target); // Make them flee
    }
};
```

### The Six Modules

The Action API consists of six modules organized by functionality:

1. **EntityCombat** - Combat actions (attack, heal, suppression, morale)
2. **EntityMovement** - Movement control (move, teleport, facing, AP)
3. **EntitySkills** - Skill manipulation (add/remove, cooldowns, parameters)
4. **EntityState** - State flag control (deployment, detection, dying)
5. **EntityAI** - AI behavior control (force actions, pause AI, threat)
6. **EntityVisibility** - Visibility management (reveal, conceal, faction detection)
7. **TileManipulation** - Tile property control (traversable, cover, LOS)

---

## Module Reference Table

Quick reference for all 52 action methods:

### EntityCombat (14 methods)

| Method | Purpose | Returns |
|--------|---------|---------|
| `Attack(actor, target)` | Execute primary weapon attack | CombatResult |
| `UseAbility(actor, skillID, target)` | Use specific skill/ability | CombatResult |
| `GetSkills(actor)` | Get all available skills | List\<SkillInfo\> |
| `CanUseAbility(actor, skillID)` | Check if skill is usable | bool |
| `GetAttackRange(actor)` | Get primary weapon range | int |
| `ApplySuppression(actor, amount)` | Add suppression | bool |
| `SetSuppression(actor, value)` | Set suppression directly | bool |
| `GetSuppression(actor)` | Get current suppression | float |
| `SetMorale(actor, value)` | Set morale value | bool |
| `GetMorale(actor)` | Get current morale | float |
| `ApplyDamage(entity, damage)` | Deal damage to entity | bool |
| `Heal(entity, amount)` | Restore hitpoints | bool |
| `SetTurnDone(actor, done)` | Mark turn as complete | bool |
| `SetStunned(actor, stunned)` | Stun/unstun actor | bool |
| `GetCombatInfo(actor)` | Get comprehensive combat state | CombatInfo |

### EntityMovement (12 methods)

| Method | Purpose | Returns |
|--------|---------|---------|
| `MoveTo(actor, x, y, flags)` | Move actor to tile | MoveResult |
| `Teleport(actor, x, y)` | Instant teleport | MoveResult |
| `Stop(actor)` | Stop current movement | bool |
| `IsMoving(actor)` | Check if actor is moving | bool |
| `GetMovementRange(actor)` | Get reachable tiles | List\<(int, int)\> |
| `GetMovementRangeAsync(actor)` | Get reachable tiles (async) | Task\<List\<(int, int)\>\> |
| `GetPath(actor, destX, destY)` | Calculate path to destination | List\<(int, int)\> |
| `SetFacing(actor, direction)` | Set facing direction (0-7) | bool |
| `GetFacing(actor)` | Get current facing | int |
| `GetPosition(actor)` | Get current tile position | (int, int)? |
| `GetRemainingAP(actor)` | Get current action points | int |
| `SetAP(actor, ap)` | Set action points | bool |
| `GetTilesMovedThisTurn(actor)` | Get distance moved | int |
| `GetMovementInfo(actor)` | Get comprehensive movement state | MovementInfo |

### EntitySkills (13 methods)

| Method | Purpose | Returns |
|--------|---------|---------|
| `AddSkill(actor, templateID)` | Add skill to actor | bool |
| `RemoveSkill(actor, skillID)` | Remove skill from actor | bool |
| `HasSkill(actor, skillID)` | Check if actor has skill | bool |
| `GetSkillIDs(actor)` | Get all skill IDs | List\<string\> |
| `SetCooldown(actor, skillID, turns)` | Set skill cooldown | bool |
| `ResetCooldown(actor, skillID)` | Clear skill cooldown | bool |
| `ModifyCooldown(actor, skillID, delta)` | Adjust cooldown by delta | bool |
| `GetRemainingCooldown(actor, skillID)` | Get cooldown turns | int |
| `ModifySkillRange(actor, skillID, range)` | Change skill range | bool |
| `ModifySkillAPCost(actor, skillID, cost)` | Change AP cost | bool |
| `EnableSkill(actor, skillID)` | Enable skill usage | bool |
| `DisableSkill(actor, skillID)` | Disable skill usage | bool |
| `GetSkillState(actor, skillID)` | Get comprehensive skill info | SkillStateInfo |
| `ResetSkillModifications(actor, skillID)` | Restore template defaults | bool |

### EntityState (8 methods)

| Method | Purpose | Returns |
|--------|---------|---------|
| `SetHeavyWeaponDeployed(actor, deployed)` | Deploy/undeploy heavy weapon | bool |
| `ToggleHeavyWeapon(actor)` | Toggle deployment state | bool |
| `SetDetectedByFaction(actor, faction, detected)` | Set faction detection bit | bool |
| `SetHiddenToAI(actor, hidden)` | Hide from AI | bool |
| `SetHiddenToPlayer(actor, hidden)` | Hide from player | bool |
| `RevealToAll(actor)` | Reveal to all factions | bool |
| `ConcealFromAll(actor)` | Conceal from all factions | bool |
| `SetDying(actor, dying)` | Set dying state flag | bool |
| `SetLeavingMap(actor, leaving)` | Set leaving map flag | bool |
| `GetStateFlags(actor)` | Get all state flags | StateFlags |

### EntityAI (7 methods)

| Method | Purpose | Returns |
|--------|---------|---------|
| `ForceNextAction(actor, actionType, target, boost)` | Force AI to prioritize action | AIResult |
| `PauseAI(actor)` | Pause all AI evaluation | AIResult |
| `ResumeAI(actor)` | Resume AI evaluation | AIResult |
| `IsAIPaused(actor)` | Check if AI is paused | bool |
| `SetThreatValueOverride(actor, target, threat)` | Override threat perception | AIResult |
| `ClearThreatOverrides(actor)` | Clear threat overrides | AIResult |
| `ForceFleeDecision(actor)` | Force actor to flee | AIResult |
| `BlockFleeDecision(actor)` | Prevent actor from fleeing | AIResult |

### EntityVisibility (6 methods)

| Method | Purpose | Returns |
|--------|---------|---------|
| `RevealToFaction(actor, faction)` | Reveal to specific faction | bool |
| `ConcealFromFaction(actor, faction)` | Conceal from specific faction | bool |
| `SetDetectionMask(actor, bitmask)` | Set entire detection mask | bool |
| `GetDetectionMask(actor)` | Get detection bitmask | int |
| `ForceVisibleTo(actor, viewer, turns)` | Temporary visibility | bool |
| `ForceConcealedFrom(actor, viewer, turns)` | Temporary concealment | bool |

### TileManipulation (12 methods)

| Method | Purpose | Returns |
|--------|---------|---------|
| `SetTraversableOverride(tile, traversable, turns)` | Make tile walkable/blocked | bool |
| `ClearTraversableOverride(tile)` | Restore traversability | bool |
| `SetCoverOverride(tile, dir, cover, turns)` | Set directional cover | bool |
| `ClearCoverOverrides(tile)` | Restore all cover | bool |
| `SetEnterable(tile, enterable, turns)` | Set global entry permission | bool |
| `SetEnterableBy(tile, actor, enterable)` | Set per-actor entry permission | bool |
| `ClearEnterableByActor(tile, actor)` | Clear per-actor override | bool |
| `IsEnterableBy(tile, actor)` | Check if actor can enter | bool |
| `SetBlocksLOS(tile, blocks, turns)` | Block line of sight | bool |
| `SetBlocksMovement(tile, blocks, turns)` | Block all movement | bool |
| `SetBlocksMovementInDirection(tile, dir, blocks, turns)` | Block specific direction | bool |
| `ClearAllOverrides()` | Clear all tile overrides | void |
| `ClearTileOverrides(tile)` | Clear specific tile overrides | bool |

**Total: 72 methods** across 7 modules

---

## Common Patterns

### Pattern 1: Intercept Detection → Action Response

Use intercepts to detect conditions, then use actions to respond:

```csharp
public class AutoHealMod : IModpackPlugin
{
    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Detect low HP
        Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
        {
            if (target.IsNull) return;

            var info = EntityCombat.GetCombatInfo(target);
            var hpAfterDamage = info.CurrentHP - damage;

            // If HP would drop below 20%, trigger auto-heal
            if (hpAfterDamage < info.MaxHP * 0.2f && hpAfterDamage > 0)
            {
                // Cancel the damage
                cancel = true;

                // Heal to 50% instead
                var targetHP = info.MaxHP / 2;
                EntityCombat.Heal(target, targetHP - info.CurrentHP);

                DevConsole.Log($"{target.GetName()} auto-healed to 50% HP!");
            }
        };
    }
}
```

### Pattern 2: State Query → Conditional Action

Check state before taking action:

```csharp
public void OnTurnStart()
{
    var soldiers = GameQuery.FindAll("Soldier");

    foreach (var soldier in soldiers)
    {
        // Query state
        var combat = EntityCombat.GetCombatInfo(soldier);
        var movement = EntityMovement.GetMovementInfo(soldier);

        // Conditional actions based on state
        if (combat.Suppression > 50f && movement.CurrentAP > 0)
        {
            // Clear suppression for pinned units
            EntityCombat.SetSuppression(soldier, 0f);
            DevConsole.Log($"{soldier.GetName()} recovered from suppression!");
        }

        if (combat.Morale < 30f)
        {
            // Boost morale for shaken units
            EntityCombat.SetMorale(soldier, 60f);
            DevConsole.Log($"{soldier.GetName()} morale boosted!");
        }
    }
}
```

### Pattern 3: Multi-Step Action Sequences

Chain multiple actions together:

```csharp
public void ExecuteTacticalManeuver(GameObj actor, int targetX, int targetY)
{
    // Step 1: Check if we can reach the position
    var range = EntityMovement.GetMovementRange(actor);
    if (!range.Contains((targetX, targetY)))
    {
        DevConsole.Warn("Target out of range!");
        return;
    }

    // Step 2: Move to position
    var moveResult = EntityMovement.MoveTo(actor, targetX, targetY);
    if (!moveResult.Success)
    {
        DevConsole.Warn($"Movement failed: {moveResult.Error}");
        return;
    }

    // Step 3: Deploy heavy weapon if available
    var skills = EntityCombat.GetSkills(actor);
    if (skills.Any(s => s.Name.Contains("HeavyWeapon")))
    {
        EntityState.SetHeavyWeaponDeployed(actor, true);
    }

    // Step 4: Go into overwatch mode
    var overwatchSkill = skills.FirstOrDefault(s => s.Name.Contains("Overwatch"));
    if (overwatchSkill != null && overwatchSkill.CanUse)
    {
        EntityCombat.UseAbility(actor, overwatchSkill.Name);
    }

    DevConsole.Log($"{actor.GetName()} executed tactical maneuver!");
}
```

---

## Real-World Examples

### Example 1: Auto-Deploy Heavy Weapons

Automatically deploy heavy weapons for units at turn start if conditions are met:

```csharp
using Menace.SDK;

public class AutoDeployMod : IModpackPlugin
{
    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Hook turn start
        TacticalEventHooks.OnTurnStart += OnTurnStart;
        logger.Msg("Auto-deploy mod initialized!");
    }

    private void OnTurnStart(IntPtr actorPtr)
    {
        var actor = new GameObj(actorPtr);
        if (actor.IsNull) return;

        // Only for player units
        if (!actor.IsPlayerControlled()) return;

        // Check if unit has a heavy weapon skill
        var skills = EntityCombat.GetSkills(actor);
        var hasHeavyWeapon = skills.Any(s => s.Name.Contains("HeavyWeapon") ||
                                              s.Name.Contains("MachineGun"));

        if (!hasHeavyWeapon) return;

        // Check if already deployed
        var state = EntityState.GetStateFlags(actor);
        if (state.IsHeavyWeaponDeployed) return;

        // Check if we have enough AP (deploying costs AP)
        var combat = EntityCombat.GetCombatInfo(actor);
        if (combat.CurrentAP < 2) return;

        // Deploy the weapon!
        if (EntityState.SetHeavyWeaponDeployed(actor, true))
        {
            DevConsole.Log($"{actor.GetName()} auto-deployed heavy weapon!");

            // Deduct AP cost
            EntityMovement.SetAP(actor, combat.CurrentAP - 2);
        }
    }

    public void OnUnload()
    {
        TacticalEventHooks.OnTurnStart -= OnTurnStart;
    }
}
```

### Example 2: Custom Skill Cooldown System

Create a performance-based cooldown system that rewards critical hits:

```csharp
using Menace.SDK;

public class DynamicCooldownMod : IModpackPlugin
{
    private Dictionary<IntPtr, int> criticalHitStreak = new();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        Intercept.OnDamageApplied += OnDamageApplied;
        logger.Msg("Dynamic cooldown mod initialized!");
    }

    private void OnDamageApplied(
        GameObj handler,
        GameObj target,
        GameObj attacker,
        GameObj skill,
        ref float damage,
        ref bool cancel)
    {
        if (attacker.IsNull || target.IsNull) return;

        // Check for critical hit (damage > 100)
        bool isCritical = damage > 100f;

        if (isCritical)
        {
            // Track critical hit streak
            if (!criticalHitStreak.ContainsKey(attacker.Pointer))
                criticalHitStreak[attacker.Pointer] = 0;

            criticalHitStreak[attacker.Pointer]++;
            int streak = criticalHitStreak[attacker.Pointer];

            // Reduce cooldowns on all skills for streak bonus
            var skills = EntityCombat.GetSkills(attacker);
            foreach (var skillInfo in skills)
            {
                var currentCooldown = EntitySkills.GetRemainingCooldown(attacker, skillInfo.Name);
                if (currentCooldown > 0)
                {
                    // Reduce by 1 turn per 2 crits in streak
                    int reduction = streak / 2;
                    EntitySkills.ModifyCooldown(attacker, skillInfo.Name, -reduction);
                }
            }

            if (streak >= 3)
            {
                // Reset all cooldowns on 3+ crit streak!
                foreach (var skillInfo in skills)
                {
                    EntitySkills.ResetCooldown(attacker, skillInfo.Name);
                }

                DevConsole.Log($"{attacker.GetName()} critical streak! All cooldowns reset!");
                criticalHitStreak[attacker.Pointer] = 0; // Reset streak
            }
        }
        else
        {
            // Reset streak on non-crit
            criticalHitStreak[attacker.Pointer] = 0;
        }
    }

    public void OnUnload()
    {
        Intercept.OnDamageApplied -= OnDamageApplied;
        criticalHitStreak.Clear();
    }
}
```

### Example 3: Dynamic Threat-Based AI

Make enemies flee when heavily damaged, or become aggressive when winning:

```csharp
using Menace.SDK;

public class ThreatBasedAIMod : IModpackPlugin
{
    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // React to damage
        Intercept.OnDamageApplied += OnDamageApplied;

        // Check all actors each turn
        TacticalEventHooks.OnTurnStart += OnTurnStart;

        logger.Msg("Threat-based AI mod initialized!");
    }

    private void OnDamageApplied(
        GameObj handler,
        GameObj target,
        GameObj attacker,
        GameObj skill,
        ref float damage,
        ref bool cancel)
    {
        if (target.IsNull || attacker.IsNull) return;

        // Only affect AI-controlled units
        if (target.IsPlayerControlled()) return;

        var combat = EntityCombat.GetCombatInfo(target);
        var hpPctAfter = (combat.CurrentHP - damage) / (float)combat.MaxHP;

        // Heavily damaged enemies flee
        if (hpPctAfter < 0.3f)
        {
            EntityAI.ForceFleeDecision(target);
            DevConsole.Log($"{target.GetName()} is fleeing! ({hpPctAfter:P0} HP)");
        }
        // Lightly damaged enemies become aggressive
        else if (hpPctAfter > 0.7f)
        {
            EntityAI.BlockFleeDecision(target);
            EntityAI.SetThreatValueOverride(target, attacker, 20.0f); // Low threat = aggressive
            DevConsole.Log($"{target.GetName()} is aggressive! ({hpPctAfter:P0} HP)");
        }
    }

    private void OnTurnStart(IntPtr actorPtr)
    {
        var actor = new GameObj(actorPtr);
        if (actor.IsNull || actor.IsPlayerControlled()) return;

        // Check squad status
        var enemies = GameQuery.FindAll("Enemy");
        var aliveEnemies = enemies.Count(e => e.IsAlive);

        // If squad is down to 25% strength, all remaining units flee
        if (aliveEnemies < enemies.Count() * 0.25f)
        {
            foreach (var enemy in enemies.Where(e => e.IsAlive))
            {
                EntityAI.ForceFleeDecision(enemy);
            }

            DevConsole.Log("Enemy squad broken! All units fleeing!");
        }
    }

    public void OnUnload()
    {
        Intercept.OnDamageApplied -= OnDamageApplied;
        TacticalEventHooks.OnTurnStart -= OnTurnStart;
    }
}
```

### Example 4: Inventory Management System

Automatically transfer items between units and manage loot:

```csharp
using Menace.SDK;

public class InventoryManagerMod : IModpackPlugin
{
    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Transfer ammo from dead units
        Intercept.OnEntityDeath += OnEntityDeath;

        // Redistribute heavy items
        TacticalEventHooks.OnTurnStart += OnTurnStart;

        logger.Msg("Inventory manager initialized!");
    }

    private void OnEntityDeath(IntPtr entityPtr)
    {
        var deadUnit = new GameObj(entityPtr);
        if (deadUnit.IsNull || !deadUnit.IsPlayerControlled()) return;

        // Get the unit's inventory
        var inventory = Inventory.GetContainer(deadUnit);
        if (inventory.IsNull) return;

        // Find nearest living ally
        var position = EntityMovement.GetPosition(deadUnit);
        if (!position.HasValue) return;

        var allies = GameQuery.FindAll("Soldier")
            .Where(a => a.IsAlive && a.IsPlayerControlled())
            .OrderBy(a => GetDistance(EntityMovement.GetPosition(a), position.Value))
            .FirstOrDefault();

        if (allies.IsNull) return;

        // Transfer valuable items
        var items = Inventory.GetItems(inventory);
        foreach (var item in items)
        {
            if (IsValuableItem(item))
            {
                var allyInventory = Inventory.GetContainer(allies);
                if (Inventory.AddItem(allyInventory, item))
                {
                    DevConsole.Log($"Transferred {item.GetName()} from {deadUnit.GetName()} to {allies.GetName()}");
                }
            }
        }

        // Clear remaining items
        Inventory.ClearContainer(inventory);
    }

    private void OnTurnStart(IntPtr actorPtr)
    {
        var actor = new GameObj(actorPtr);
        if (actor.IsNull || !actor.IsPlayerControlled()) return;

        // If unit is carrying heavy items and has low AP, redistribute
        var movement = EntityMovement.GetMovementInfo(actor);
        if (movement.CurrentAP < 2)
        {
            var inventory = Inventory.GetContainer(actor);
            var heavyItems = Inventory.GetItems(inventory).Where(IsHeavyItem).ToList();

            if (heavyItems.Count > 0)
            {
                // Find nearby ally with more AP
                var position = EntityMovement.GetPosition(actor);
                if (!position.HasValue) return;

                var nearbyAllies = GameQuery.FindActorsInRadius(new GameObj(position.Value.x, position.Value.y), 3)
                    .Where(a => a.IsAlive && a.IsPlayerControlled() && a.Pointer != actor.Pointer)
                    .OrderByDescending(a => EntityMovement.GetRemainingAP(a))
                    .FirstOrDefault();

                if (!nearbyAllies.IsNull)
                {
                    var allyAP = EntityMovement.GetRemainingAP(nearbyAllies);
                    if (allyAP > movement.CurrentAP)
                    {
                        // Transfer heavy items
                        var allyInventory = Inventory.GetContainer(nearbyAllies);
                        foreach (var item in heavyItems.Take(3)) // Max 3 items
                        {
                            if (Inventory.TransferItem(inventory, allyInventory, item))
                            {
                                DevConsole.Log($"{actor.GetName()} gave {item.GetName()} to {nearbyAllies.GetName()}");
                            }
                        }
                    }
                }
            }
        }
    }

    private bool IsValuableItem(GameObj item)
    {
        if (item.IsNull) return false;
        var name = item.GetName();
        return name.Contains("Ammo") || name.Contains("Medkit") || name.Contains("Grenade");
    }

    private bool IsHeavyItem(GameObj item)
    {
        if (item.IsNull) return false;
        return item.GetName().Contains("Heavy") || item.GetName().Contains("Launcher");
    }

    private float GetDistance((int x, int y)? pos1, (int x, int y) pos2)
    {
        if (!pos1.HasValue) return float.MaxValue;
        var dx = pos1.Value.x - pos2.x;
        var dy = pos1.Value.y - pos2.y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    public void OnUnload()
    {
        Intercept.OnEntityDeath -= OnEntityDeath;
        TacticalEventHooks.OnTurnStart -= OnTurnStart;
    }
}
```

### Example 5: Temporary Terrain Changes

Create destructible cover and dynamic obstacles:

```csharp
using Menace.SDK;

public class DynamicTerrainMod : IModpackPlugin
{
    private Dictionary<IntPtr, int> coverDurability = new();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Reduce cover when hit
        Intercept.OnDamageApplied += OnDamageApplied;

        // Deploy smoke grenades
        Intercept.OnSkillExecute += OnSkillExecute;

        logger.Msg("Dynamic terrain mod initialized!");
    }

    private void OnDamageApplied(
        GameObj handler,
        GameObj target,
        GameObj attacker,
        GameObj skill,
        ref float damage,
        ref bool cancel)
    {
        if (target.IsNull) return;

        // Get the target's tile
        var position = EntityMovement.GetPosition(target);
        if (!position.HasValue) return;

        var tile = TileMap.GetTile(position.Value.x, position.Value.y);
        if (tile.IsNull) return;

        // Check cover in the direction of the attacker
        var attackerPos = EntityMovement.GetPosition(attacker);
        if (!attackerPos.HasValue) return;

        int direction = CalculateDirection(position.Value, attackerPos.Value);

        // Initialize durability for this tile if not tracked
        if (!coverDurability.ContainsKey(tile.Pointer))
        {
            coverDurability[tile.Pointer] = 100; // Start at 100% durability
        }

        // Reduce cover durability on each hit
        coverDurability[tile.Pointer] -= 10;

        if (coverDurability[tile.Pointer] <= 50)
        {
            // Reduce from full to half cover
            TileManipulation.SetCoverOverride(tile, direction, 1, -1); // Half cover
            DevConsole.Log($"Cover damaged at ({position.Value.x}, {position.Value.y})!");
        }

        if (coverDurability[tile.Pointer] <= 0)
        {
            // Destroy cover completely
            TileManipulation.SetCoverOverride(tile, direction, 0, -1); // No cover
            DevConsole.Log($"Cover destroyed at ({position.Value.x}, {position.Value.y})!");
        }
    }

    private void OnSkillExecute(IntPtr skillPtr, IntPtr actorPtr)
    {
        var skill = new GameObj(skillPtr);
        var actor = new GameObj(actorPtr);

        if (skill.IsNull || actor.IsNull) return;

        // Check if this is a smoke grenade
        if (!skill.GetName().Contains("Smoke")) return;

        // Get target position (where grenade landed)
        var position = EntityMovement.GetPosition(actor);
        if (!position.HasValue) return;

        // Block LOS in a 3x3 area for 3 turns
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var tile = TileMap.GetTile(position.Value.x + x, position.Value.y + y);
                if (!tile.IsNull)
                {
                    TileManipulation.SetBlocksLOS(tile, true, 3); // 3 turn smoke
                }
            }
        }

        DevConsole.Log($"Smoke grenade deployed at ({position.Value.x}, {position.Value.y})!");
    }

    private int CalculateDirection((int x, int y) from, (int x, int y) to)
    {
        var dx = to.x - from.x;
        var dy = to.y - from.y;

        // Simple 8-direction calculation
        if (dx > 0 && dy > 0) return 3; // SE
        if (dx > 0 && dy < 0) return 1; // NE
        if (dx > 0) return 2;           // E
        if (dx < 0 && dy > 0) return 5; // SW
        if (dx < 0 && dy < 0) return 7; // NW
        if (dx < 0) return 6;           // W
        if (dy > 0) return 4;           // S
        return 0;                       // N
    }

    public void OnUnload()
    {
        Intercept.OnDamageApplied -= OnDamageApplied;
        Intercept.OnSkillExecute -= OnSkillExecute;
        coverDurability.Clear();
    }
}
```

### Example 6: Visibility and Stealth System

Create cloaking, temporary detection, and faction-specific invisibility:

```csharp
using Menace.SDK;

public class StealthSystemMod : IModpackPlugin
{
    private HashSet<IntPtr> cloakedUnits = new();
    private Dictionary<IntPtr, int> detectionTimers = new();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Activate cloak on specific skill use
        Intercept.OnSkillExecute += OnSkillExecute;

        // Detect enemies when they attack
        Intercept.OnDamageApplied += OnDamageApplied;

        // Update detection timers
        TacticalEventHooks.OnTurnEnd += OnTurnEnd;

        logger.Msg("Stealth system initialized!");
    }

    private void OnSkillExecute(IntPtr skillPtr, IntPtr actorPtr)
    {
        var skill = new GameObj(skillPtr);
        var actor = new GameObj(actorPtr);

        if (skill.IsNull || actor.IsNull) return;

        // Check for "Cloak" skill
        if (skill.GetName().Contains("Cloak"))
        {
            // Conceal from all factions
            EntityState.ConcealFromAll(actor);
            cloakedUnits.Add(actor.Pointer);

            DevConsole.Log($"{actor.GetName()} activated cloak!");
        }

        // Check for "Scanner" skill
        if (skill.GetName().Contains("Scanner"))
        {
            // Reveal nearby enemies for 2 turns
            var position = EntityMovement.GetPosition(actor);
            if (!position.HasValue) return;

            var nearbyEnemies = GameQuery.FindActorsInRadius(
                TileMap.GetTile(position.Value.x, position.Value.y), 5);

            foreach (var enemy in nearbyEnemies)
            {
                if (enemy.IsNull || enemy.Pointer == actor.Pointer) return;
                if (enemy.GetFaction() == actor.GetFaction()) continue;

                // Force visible for 2 turns
                EntityVisibility.ForceVisibleTo(enemy, actor, 2);
                detectionTimers[enemy.Pointer] = 2;

                DevConsole.Log($"{actor.GetName()} detected {enemy.GetName()}!");
            }
        }
    }

    private void OnDamageApplied(
        GameObj handler,
        GameObj target,
        GameObj attacker,
        GameObj skill,
        ref float damage,
        ref bool cancel)
    {
        if (attacker.IsNull) return;

        // Attacking breaks cloak
        if (cloakedUnits.Contains(attacker.Pointer))
        {
            EntityState.RevealToAll(attacker);
            cloakedUnits.Remove(attacker.Pointer);

            DevConsole.Log($"{attacker.GetName()} cloak broken by attack!");
        }
    }

    private void OnTurnEnd(IntPtr actorPtr)
    {
        // Decrement detection timers
        var expiredDetections = new List<IntPtr>();

        foreach (var kvp in detectionTimers)
        {
            detectionTimers[kvp.Key]--;
            if (detectionTimers[kvp.Key] <= 0)
            {
                expiredDetections.Add(kvp.Key);
            }
        }

        // Remove expired detections
        foreach (var ptr in expiredDetections)
        {
            detectionTimers.Remove(ptr);
            DevConsole.Log("Detection timer expired");
        }
    }

    public void OnUnload()
    {
        Intercept.OnSkillExecute -= OnSkillExecute;
        Intercept.OnDamageApplied -= OnDamageApplied;
        TacticalEventHooks.OnTurnEnd -= OnTurnEnd;
        cloakedUnits.Clear();
        detectionTimers.Clear();
    }
}
```

### Example 7: Skill Modification System

Create weapon upgrades and dynamic skill enabling:

```csharp
using Menace.SDK;

public class SkillUpgradesMod : IModpackPlugin
{
    private Dictionary<IntPtr, int> killCount = new();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Track kills
        Intercept.OnEntityDeath += OnEntityDeath;

        // Apply upgrades on turn start
        TacticalEventHooks.OnTurnStart += OnTurnStart;

        logger.Msg("Skill upgrades mod initialized!");
    }

    private void OnEntityDeath(IntPtr entityPtr)
    {
        var deadEntity = new GameObj(entityPtr);
        if (deadEntity.IsNull) return;

        // Find who killed them (get last damage source)
        // (This is simplified - you'd track this via OnDamageApplied)
        var allActors = GameQuery.FindAll("Actor");
        var possibleKillers = allActors.Where(a => a.IsAlive && a.IsPlayerControlled());

        foreach (var killer in possibleKillers)
        {
            // Track kill count
            if (!killCount.ContainsKey(killer.Pointer))
                killCount[killer.Pointer] = 0;

            killCount[killer.Pointer]++;

            // Grant upgrades based on kill count
            ApplyKillCountUpgrades(killer, killCount[killer.Pointer]);
        }
    }

    private void ApplyKillCountUpgrades(GameObj actor, int kills)
    {
        var skills = EntityCombat.GetSkills(actor);

        // At 3 kills: Increase weapon range
        if (kills == 3)
        {
            foreach (var skill in skills.Where(s => s.IsAttack))
            {
                EntitySkills.ModifySkillRange(actor, skill.Name, skill.Range + 2);
                DevConsole.Log($"{actor.GetName()} weapon range increased!");
            }
        }

        // At 5 kills: Reduce AP costs
        if (kills == 5)
        {
            foreach (var skill in skills)
            {
                var newCost = Math.Max(1, skill.APCost - 1);
                EntitySkills.ModifySkillAPCost(actor, skill.Name, newCost);
                DevConsole.Log($"{actor.GetName()} AP costs reduced!");
            }
        }

        // At 10 kills: Unlock special skill
        if (kills == 10)
        {
            EntitySkills.AddSkill(actor, "skill.elite_shot");
            DevConsole.Log($"{actor.GetName()} unlocked Elite Shot!");
        }
    }

    private void OnTurnStart(IntPtr actorPtr)
    {
        var actor = new GameObj(actorPtr);
        if (actor.IsNull || !actor.IsPlayerControlled()) return;

        // Dynamic skill enabling based on state
        var combat = EntityCombat.GetCombatInfo(actor);
        var skills = EntitySkills.GetSkillIDs(actor);

        // Disable heavy weapons if not deployed
        var state = EntityState.GetStateFlags(actor);
        foreach (var skillID in skills.Where(s => s.Contains("HeavyWeapon")))
        {
            if (state.IsHeavyWeaponDeployed)
                EntitySkills.EnableSkill(actor, skillID);
            else
                EntitySkills.DisableSkill(actor, skillID);
        }

        // Enable "Desperate" skills only at low HP
        foreach (var skillID in skills.Where(s => s.Contains("Desperate")))
        {
            if (combat.HPPercent < 0.3f)
                EntitySkills.EnableSkill(actor, skillID);
            else
                EntitySkills.DisableSkill(actor, skillID);
        }

        // Berserker mode: Reset all cooldowns at very low HP
        if (combat.HPPercent < 0.15f)
        {
            foreach (var skillID in skills)
            {
                EntitySkills.ResetCooldown(actor, skillID);
            }
            DevConsole.Log($"{actor.GetName()} entered berserker mode!");
        }
    }

    public void OnUnload()
    {
        Intercept.OnEntityDeath -= OnEntityDeath;
        TacticalEventHooks.OnTurnStart -= OnTurnStart;
        killCount.Clear();
    }
}
```

### Example 8: AI Behavior Control

Create boss immunity to flee and berserker mechanics:

```csharp
using Menace.SDK;

public class AIBehaviorMod : IModpackPlugin
{
    private HashSet<IntPtr> bosses = new();
    private HashSet<IntPtr> berserkers = new();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Initialize on spawn
        Intercept.OnEntitySpawn += OnEntitySpawn;

        // Control behavior
        TacticalEventHooks.OnTurnStart += OnTurnStart;
        Intercept.OnDamageApplied += OnDamageApplied;

        logger.Msg("AI behavior mod initialized!");
    }

    private void OnEntitySpawn(IntPtr entityPtr)
    {
        var entity = new GameObj(entityPtr);
        if (entity.IsNull) return;

        var name = entity.GetName();

        // Mark bosses
        if (name.Contains("Boss") || name.Contains("Commander"))
        {
            bosses.Add(entity.Pointer);

            // Bosses never flee
            EntityAI.BlockFleeDecision(entity);

            // High morale
            EntityCombat.SetMorale(entity, EntityAI.MORALE_FEARLESS);

            DevConsole.Log($"{name} marked as boss - immune to flee");
        }

        // Mark berserkers
        if (name.Contains("Berserker"))
        {
            berserkers.Add(entity.Pointer);
            DevConsole.Log($"{name} marked as berserker");
        }
    }

    private void OnDamageApplied(
        GameObj handler,
        GameObj target,
        GameObj attacker,
        GameObj skill,
        ref float damage,
        ref bool cancel)
    {
        if (target.IsNull || attacker.IsNull) return;

        // Berserkers become more aggressive when damaged
        if (berserkers.Contains(target.Pointer))
        {
            var combat = EntityCombat.GetCombatInfo(target);
            var hpPctAfter = (combat.CurrentHP - damage) / (float)combat.MaxHP;

            // Lower HP = more aggressive
            if (hpPctAfter < 0.5f)
            {
                // Force attack behavior
                EntityAI.ForceNextAction(target, "AttackBehavior", attacker, 10000);

                // Boost damage for berserker rage
                damage *= 1.5f;

                DevConsole.Log($"{target.GetName()} enters berserker rage!");
            }
        }
    }

    private void OnTurnStart(IntPtr actorPtr)
    {
        var actor = new GameObj(actorPtr);
        if (actor.IsNull) return;

        // Boss behavior modifications
        if (bosses.Contains(actor.Pointer))
        {
            // Bosses always maintain high morale
            EntityCombat.SetMorale(actor, EntityAI.MORALE_FEARLESS);

            // Find nearest player unit and prioritize attacking them
            var playerUnits = GameQuery.FindAll("Soldier")
                .Where(s => s.IsAlive && s.IsPlayerControlled())
                .ToList();

            if (playerUnits.Count > 0)
            {
                var position = EntityMovement.GetPosition(actor);
                if (position.HasValue)
                {
                    var nearest = playerUnits
                        .OrderBy(p => GetDistance(EntityMovement.GetPosition(p), position.Value))
                        .First();

                    // Force attack on nearest player
                    EntityAI.ForceNextAction(actor, "AttackBehavior", nearest, 10000);
                }
            }
        }

        // Berserker behavior
        if (berserkers.Contains(actor.Pointer))
        {
            var combat = EntityCombat.GetCombatInfo(actor);

            // Low HP berserkers always attack
            if (combat.HPPercent < 0.5f)
            {
                EntityAI.BlockFleeDecision(actor);
                EntityAI.SetThreatValueOverride(actor, GameObj.Null, 0f); // No fear
            }
        }
    }

    private float GetDistance((int x, int y)? pos1, (int x, int y) pos2)
    {
        if (!pos1.HasValue) return float.MaxValue;
        var dx = pos1.Value.x - pos2.x;
        var dy = pos1.Value.y - pos2.y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    public void OnUnload()
    {
        Intercept.OnEntitySpawn -= OnEntitySpawn;
        TacticalEventHooks.OnTurnStart -= OnTurnStart;
        Intercept.OnDamageApplied -= OnDamageApplied;
        bosses.Clear();
        berserkers.Clear();
    }
}
```

### Example 9: State Flag Manipulation

Fake death mechanics and phase-out:

```csharp
using Menace.SDK;

public class StateManipulationMod : IModpackPlugin
{
    private Dictionary<IntPtr, int> fakeDeathTimers = new();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Intercept lethal damage
        Intercept.OnDamageApplied += OnDamageApplied;

        // Update fake death timers
        TacticalEventHooks.OnTurnEnd += OnTurnEnd;

        logger.Msg("State manipulation mod initialized!");
    }

    private void OnDamageApplied(
        GameObj handler,
        GameObj target,
        GameObj attacker,
        GameObj skill,
        ref float damage,
        ref bool cancel)
    {
        if (target.IsNull) return;

        var combat = EntityCombat.GetCombatInfo(target);
        var wouldDie = damage >= combat.CurrentHP;

        // Check for "FakeDeathPerks" (simplified check)
        if (wouldDie && target.GetName().Contains("Ninja"))
        {
            // Cancel the killing blow
            cancel = true;

            // Set to 1 HP
            EntityCombat.Heal(target, -(combat.CurrentHP - 1));

            // Set dying state to play death animation
            EntityState.SetDying(target, true);

            // Conceal from all enemies
            EntityState.ConcealFromAll(target);

            // Mark for revival in 2 turns
            fakeDeathTimers[target.Pointer] = 2;

            DevConsole.Log($"{target.GetName()} played dead!");
        }

        // Phase-out mechanic for critically damaged elites
        if (!wouldDie && combat.HPPercent < 0.2f && target.GetName().Contains("Elite"))
        {
            // Mark as leaving map
            EntityState.SetLeavingMap(target, true);

            // Make invisible
            EntityState.ConcealFromAll(target);

            // Teleport to extraction point (simplified)
            var extractionPoint = FindExtractionPoint();
            if (extractionPoint.HasValue)
            {
                EntityMovement.Teleport(target, extractionPoint.Value.x, extractionPoint.Value.y);
            }

            DevConsole.Log($"{target.GetName()} phased out!");
        }
    }

    private void OnTurnEnd(IntPtr actorPtr)
    {
        // Update fake death timers
        var expiredUnits = new List<IntPtr>();

        foreach (var kvp in fakeDeathTimers)
        {
            fakeDeathTimers[kvp.Key]--;

            if (fakeDeathTimers[kvp.Key] <= 0)
            {
                var unit = new GameObj(kvp.Key);
                if (!unit.IsNull)
                {
                    // Revive the unit!
                    EntityState.SetDying(unit, false);
                    EntityState.RevealToAll(unit);
                    EntityCombat.Heal(unit, 20); // Heal to 20 HP

                    DevConsole.Log($"{unit.GetName()} revived from fake death!");
                }

                expiredUnits.Add(kvp.Key);
            }
        }

        // Clean up expired timers
        foreach (var ptr in expiredUnits)
        {
            fakeDeathTimers.Remove(ptr);
        }
    }

    private (int x, int y)? FindExtractionPoint()
    {
        // Simplified - find a tile at map edge
        var map = TileMap.GetCurrentMap();
        if (map.IsNull) return null;

        var mapSize = TileMap.GetMapSize(map);
        return (mapSize.width - 1, mapSize.height / 2);
    }

    public void OnUnload()
    {
        Intercept.OnDamageApplied -= OnDamageApplied;
        TacticalEventHooks.OnTurnEnd -= OnTurnEnd;
        fakeDeathTimers.Clear();
    }
}
```

### Example 10: Tile-Based Mechanics

Create locked doors, area denial, and cover destruction:

```csharp
using Menace.SDK;

public class TileMechanicsMod : IModpackPlugin
{
    private HashSet<IntPtr> lockedDoors = new();
    private Dictionary<IntPtr, GameObj> doorKeys = new(); // door -> key item

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Initialize locked doors
        TacticalEventHooks.OnMissionStart += OnMissionStart;

        // Check door unlock
        Intercept.OnMoveTo += OnMoveTo;

        // Area denial damage
        TacticalEventHooks.OnTurnEnd += OnTurnEnd;

        logger.Msg("Tile mechanics mod initialized!");
    }

    private void OnMissionStart()
    {
        // Find and lock specific doors
        var doors = GameQuery.FindAll("Door");

        foreach (var door in doors.Where(d => d.GetName().Contains("Secure")))
        {
            var position = EntityMovement.GetPosition(door);
            if (!position.HasValue) continue;

            var tile = TileMap.GetTile(position.Value.x, position.Value.y);
            if (tile.IsNull) continue;

            // Lock the door - block traversal
            TileManipulation.SetTraversableOverride(tile, false, -1);
            lockedDoors.Add(tile.Pointer);

            // Create a key item (simplified - would spawn in world)
            DevConsole.Log($"Locked door at ({position.Value.x}, {position.Value.y})");
        }

        // Create danger zones (toxic areas)
        var hazardTiles = FindTilesWithTag("Hazard");
        foreach (var tile in hazardTiles)
        {
            // Block LOS
            TileManipulation.SetBlocksLOS(tile, true, -1);

            // Make slightly traversable but dangerous
            TileManipulation.SetTraversableOverride(tile, true, -1);
        }
    }

    private void OnMoveTo(GameObj actor, GameObj tile, int flags, ref bool cancel)
    {
        if (actor.IsNull || tile.IsNull) return;

        // Check if trying to enter locked door
        if (lockedDoors.Contains(tile.Pointer))
        {
            // Check if actor has the key
            var inventory = Inventory.GetContainer(actor);
            var items = Inventory.GetItems(inventory);

            bool hasKey = items.Any(i => i.GetName().Contains("Key"));

            if (hasKey)
            {
                // Unlock the door!
                TileManipulation.ClearTraversableOverride(tile);
                lockedDoors.Remove(tile.Pointer);

                DevConsole.Log($"{actor.GetName()} unlocked the door!");
            }
            else
            {
                // Block movement
                cancel = true;
                DevConsole.Log("Door is locked! Need a key.");
            }
        }
    }

    private void OnTurnEnd(IntPtr actorPtr)
    {
        var actor = new GameObj(actorPtr);
        if (actor.IsNull) return;

        var position = EntityMovement.GetPosition(actor);
        if (!position.HasValue) return;

        var tile = TileMap.GetTile(position.Value.x, position.Value.y);
        if (tile.IsNull) return;

        // Check if standing in hazard zone
        if (IsTileHazardous(tile))
        {
            // Deal damage each turn
            EntityCombat.ApplyDamage(actor, 10);

            // Apply suppression
            EntityCombat.ApplySuppression(actor, 20f);

            DevConsole.Log($"{actor.GetName()} took hazard damage!");
        }

        // Temporary cover destruction on explosions (simplified)
        var nearbyExplosions = CheckForNearbyExplosions(position.Value);
        if (nearbyExplosions)
        {
            // Destroy cover in all directions
            for (int dir = 0; dir < 8; dir++)
            {
                TileManipulation.SetCoverOverride(tile, dir, 0, -1);
            }

            DevConsole.Log($"Cover destroyed at ({position.Value.x}, {position.Value.y})!");
        }
    }

    private bool IsTileHazardous(GameObj tile)
    {
        // Simplified check - would check tile properties/tags
        return tile.GetName().Contains("Toxic") || tile.GetName().Contains("Fire");
    }

    private bool CheckForNearbyExplosions((int x, int y) position)
    {
        // Simplified - would check for recent explosion effects
        return false;
    }

    private List<GameObj> FindTilesWithTag(string tag)
    {
        // Simplified - would query tile system
        return new List<GameObj>();
    }

    public void OnUnload()
    {
        TacticalEventHooks.OnMissionStart -= OnMissionStart;
        Intercept.OnMoveTo -= OnMoveTo;
        TacticalEventHooks.OnTurnEnd -= OnTurnEnd;
        lockedDoors.Clear();
        doorKeys.Clear();
    }
}
```

---

## REPL Testing Guide

The REPL (Read-Eval-Print-Loop) console is perfect for testing Action API methods interactively. Access it with **F1** during tactical combat.

### Testing EntityCombat

```csharp
// Get all actors
var actors = GameQuery.FindAll("Actor");
var soldier = actors.First(a => a.GetName().Contains("Soldier"));

// Test combat info
var info = EntityCombat.GetCombatInfo(soldier);
Console.WriteLine($"HP: {info.CurrentHP}/{info.MaxHP}, Morale: {info.Morale}, Suppression: {info.Suppression}");

// Test suppression
EntityCombat.ApplySuppression(soldier, 50f);
Console.WriteLine($"New suppression: {EntityCombat.GetSuppression(soldier)}");

// Test skills
var skills = EntityCombat.GetSkills(soldier);
foreach (var skill in skills)
{
    Console.WriteLine($"{skill.Name}: AP={skill.APCost}, Range={skill.Range}, Cooldown={skill.CurrentCooldown}");
}

// Test damage
EntityCombat.ApplyDamage(soldier, 10);
Console.WriteLine($"After damage: {EntityCombat.GetCombatInfo(soldier).CurrentHP} HP");

// Test healing
EntityCombat.Heal(soldier, 20);
Console.WriteLine($"After heal: {EntityCombat.GetCombatInfo(soldier).CurrentHP} HP");
```

### Testing EntityMovement

```csharp
var soldier = GameQuery.FindAll("Soldier").First();

// Get current position
var pos = EntityMovement.GetPosition(soldier);
Console.WriteLine($"Position: ({pos.Value.x}, {pos.Value.y})");

// Get movement info
var movement = EntityMovement.GetMovementInfo(soldier);
Console.WriteLine($"Facing: {movement.DirectionName}, AP: {movement.CurrentAP}, Moving: {movement.IsMoving}");

// Get reachable tiles
var range = EntityMovement.GetMovementRange(soldier);
Console.WriteLine($"Can reach {range.Count} tiles");

// Test movement
var result = EntityMovement.MoveTo(soldier, pos.Value.x + 2, pos.Value.y + 1);
Console.WriteLine($"Move result: {result.Success}, Error: {result.Error}");

// Test teleport
EntityMovement.Teleport(soldier, 10, 10);
Console.WriteLine($"Teleported to: {EntityMovement.GetPosition(soldier)}");

// Set facing
EntityMovement.SetFacing(soldier, EntityMovement.DIR_NORTH);
Console.WriteLine($"Now facing: {EntityMovement.GetMovementInfo(soldier).DirectionName}");
```

### Testing EntitySkills

```csharp
var soldier = GameQuery.FindAll("Soldier").First();

// List all skills
var skillIDs = EntitySkills.GetSkillIDs(soldier);
foreach (var id in skillIDs)
{
    Console.WriteLine($"Skill: {id}");
}

// Get detailed skill info
var skillID = skillIDs.First();
var state = EntitySkills.GetSkillState(soldier, skillID);
Console.WriteLine($"{state.SkillID}: Enabled={state.IsEnabled}, AP={state.APCost}, Range={state.MinRange}-{state.MaxRange}, Cooldown={state.RemainingCooldown}");

// Modify skill parameters
EntitySkills.ModifySkillRange(soldier, skillID, 10);
EntitySkills.ModifySkillAPCost(soldier, skillID, 1);
Console.WriteLine($"Modified range to 10, AP to 1");

// Test cooldown management
EntitySkills.SetCooldown(soldier, skillID, 3);
Console.WriteLine($"Cooldown set to: {EntitySkills.GetRemainingCooldown(soldier, skillID)}");

EntitySkills.ResetCooldown(soldier, skillID);
Console.WriteLine($"Cooldown reset to: {EntitySkills.GetRemainingCooldown(soldier, skillID)}");

// Disable/enable
EntitySkills.DisableSkill(soldier, skillID);
Console.WriteLine($"Skill disabled: {EntitySkills.GetSkillState(soldier, skillID).IsEnabled}");

EntitySkills.EnableSkill(soldier, skillID);
Console.WriteLine($"Skill enabled: {EntitySkills.GetSkillState(soldier, skillID).IsEnabled}");
```

### Testing EntityState

```csharp
var soldier = GameQuery.FindAll("Soldier").First();

// Get all state flags
var state = EntityState.GetStateFlags(soldier);
Console.WriteLine($"Heavy weapon deployed: {state.IsHeavyWeaponDeployed}");
Console.WriteLine($"Detection mask: {state.DetectionMask:X}");
Console.WriteLine($"Hidden to AI: {state.IsHiddenToAI}");

// Toggle heavy weapon
EntityState.ToggleHeavyWeapon(soldier);
Console.WriteLine($"After toggle: {EntityState.GetStateFlags(soldier).IsHeavyWeaponDeployed}");

// Test detection
EntityState.RevealToAll(soldier);
Console.WriteLine($"Revealed to all: {EntityState.GetStateFlags(soldier).DetectionMask:X}");

EntityState.ConcealFromAll(soldier);
Console.WriteLine($"Concealed from all: {EntityState.GetStateFlags(soldier).DetectionMask:X}");

// Set faction detection
EntityState.SetDetectedByFaction(soldier, 0, true); // Reveal to faction 0
Console.WriteLine($"Detection mask: {EntityState.GetStateFlags(soldier).DetectionMask:X}");
```

### Testing EntityAI

```csharp
var enemy = GameQuery.FindAll("Enemy").First();
var player = GameQuery.FindAll("Soldier").First();

// Check AI pause state
Console.WriteLine($"AI paused: {EntityAI.IsAIPaused(enemy)}");

// Pause all AI
var result = EntityAI.PauseAI(enemy);
Console.WriteLine($"Pause result: {result.Success}");

// Resume AI
result = EntityAI.ResumeAI(enemy);
Console.WriteLine($"Resume result: {result.Success}");

// Force flee
result = EntityAI.ForceFleeDecision(enemy);
Console.WriteLine($"Flee forced: {result.Success}");

// Block flee
result = EntityAI.BlockFleeDecision(enemy);
Console.WriteLine($"Flee blocked: {result.Success}");

// Force action
result = EntityAI.ForceNextAction(enemy, "AttackBehavior", player, 10000);
Console.WriteLine($"Action forced: {result.Success}, Error: {result.Error}");
```

### Testing EntityVisibility

```csharp
var soldier = GameQuery.FindAll("Soldier").First();
var enemy = GameQuery.FindAll("Enemy").First();

// Get detection mask
var mask = EntityVisibility.GetDetectionMask(soldier);
Console.WriteLine($"Detection mask: {mask:X}");

// Reveal to specific faction
EntityVisibility.RevealToFaction(soldier, 1); // Faction 1
Console.WriteLine($"After reveal: {EntityVisibility.GetDetectionMask(soldier):X}");

// Conceal from specific faction
EntityVisibility.ConcealFromFaction(soldier, 1);
Console.WriteLine($"After conceal: {EntityVisibility.GetDetectionMask(soldier):X}");

// Temporary visibility
EntityVisibility.ForceVisibleTo(soldier, enemy, 2); // 2 turns
Console.WriteLine($"Forced visible for 2 turns");

// Check after 2 turns (manually advance turns in REPL)
```

### Testing TileManipulation

```csharp
// Get a tile
var tile = TileMap.GetTile(10, 10);
Console.WriteLine($"Tile at (10, 10): {tile.GetName()}");

// Test traversable override
TileManipulation.SetTraversableOverride(tile, false, 3); // Block for 3 turns
Console.WriteLine($"Tile blocked for 3 turns");

TileManipulation.ClearTraversableOverride(tile);
Console.WriteLine($"Tile unblocked");

// Test cover override
TileManipulation.SetCoverOverride(tile, 0, 2, -1); // Full cover north, permanent
Console.WriteLine($"Cover set to north");

// Test LOS blocking
TileManipulation.SetBlocksLOS(tile, true, 2); // Block LOS for 2 turns
Console.WriteLine($"LOS blocked for 2 turns");

// Check override timers
var remaining = TileManipulation.GetOverrideTurnsRemaining(tile);
Console.WriteLine($"Override turns remaining: {remaining}");

// Clear all
TileManipulation.ClearTileOverrides(tile);
Console.WriteLine($"All overrides cleared");
```

### Common REPL Errors and Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| `NullReferenceException` | GameObj is null | Add null checks: `if (obj.IsNull) return;` |
| `ArgumentOutOfRangeException` | Invalid faction/direction index | Check index ranges (factions: 0-31, directions: 0-7) |
| `InvalidOperationException` | AI is thinking (thread safety) | Wait for AI turn to complete or pause AI first |
| `OffsetVerificationFailure` | Memory offset mismatch | Update to latest modkit version |

---

## Integration with Intercepts

Action APIs and Intercepts work best together. Here's how they integrate:

### Intercept → Action Pairing Table

| Intercept Event | Paired Action Methods | Use Case |
|-----------------|----------------------|----------|
| `OnDamageApplied` | `ApplyDamage`, `Heal`, `SetSuppression` | React to damage with healing or suppression |
| `OnMoveTo` | `MoveTo`, `Teleport`, `SetFacing` | Redirect movement or teleport |
| `OnSkillExecute` | `UseAbility`, `SetCooldown`, `EnableSkill` | Chain skills or modify cooldowns |
| `OnTurnStart` | `SetAP`, `SetMorale`, `ForceNextAction` | Initialize turn state |
| `OnTurnEnd` | `ResetCooldown`, `Heal`, `ClearThreatOverrides` | Clean up or apply end-of-turn effects |
| `OnEntityDeath` | `RevealToAll`, `SetDying` | Fake death or loot drop mechanics |
| `OnAIEvaluate` | `ForceNextAction`, `PauseAI` | Control AI behavior |
| `OnTileTraversable` | `SetTraversableOverride` | Dynamic tile blocking |
| `OnPathfinding` | `SetBlocksMovement`, `SetCoverOverride` | Modify pathfinding results |

### Event Flow Diagram

```
Player Action (e.g., Attack)
    ↓
Intercept.OnSkillExecute fires
    ↓
Your handler detects skill type
    ↓
EntityCombat.UseAbility() (chain skill)
    ↓
Intercept.OnDamageApplied fires
    ↓
Your handler modifies damage
    ↓
EntityCombat.ApplySuppression() (add suppression)
    ↓
Intercept.OnSuppressionApplied fires
    ↓
Your handler checks suppression level
    ↓
EntityAI.ForceFleeDecision() (force flee if high)
    ↓
AI turn executes flee behavior
```

### Best Practices for Integration

1. **Use Intercepts for Detection**: Let intercepts tell you when things happen
2. **Use Actions for Response**: Let actions change the game state
3. **Chain Thoughtfully**: Avoid infinite loops (intercept triggers action that triggers intercept...)
4. **Performance**: Keep intercept handlers fast, do heavy work in actions
5. **Thread Safety**: Never call AI actions from parallel intercepts (OnAIEvaluate, OnPositionScore)

### Example: Complete Integration Pattern

```csharp
public class IntegratedMod : IModpackPlugin
{
    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // DETECT: Use intercept to detect condition
        Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
        {
            if (target.IsNull || attacker.IsNull) return;

            // QUERY: Use action to query state
            var combat = EntityCombat.GetCombatInfo(target);

            // DECIDE: Conditional logic
            if (damage >= combat.CurrentHP * 0.8f)
            {
                // RESPOND: Use actions to modify state
                EntityCombat.SetSuppression(attacker, 100f); // Max suppression
                EntityAI.ForceFleeDecision(attacker); // Force retreat

                // CHAIN: Trigger additional actions
                var nearbyAllies = FindNearbyAllies(attacker);
                foreach (var ally in nearbyAllies)
                {
                    EntityCombat.ApplySuppression(ally, 50f);
                }
            }
        };
    }
}
```

---

## Performance Considerations

### Thread Safety Warnings

Some Action APIs must **never** be called during parallel AI evaluation:

**UNSAFE during AI evaluation:**
- `EntityAI.ForceNextAction()`
- `EntityAI.SetThreatValueOverride()`
- `EntityAI.ForceFleeDecision()`
- `EntityAI.BlockFleeDecision()`
- `EntitySkills.ModifySkillRange()` (if AI is evaluating skills)
- `EntityMovement.MoveTo()` (if AI is pathfinding)

**SAFE to call anytime:**
- `EntityCombat.ApplyDamage()`
- `EntityCombat.Heal()`
- `EntityState.SetDetectedByFaction()`
- `EntityVisibility.RevealToFaction()`
- All query methods (`GetCombatInfo`, `GetSkillState`, etc.)

**How to check if AI is evaluating:**

```csharp
// Check before calling unsafe methods
if (AI.IsAnyFactionThinking())
{
    DevConsole.Warn("AI is evaluating - skipping unsafe operation");
    return;
}

// Safe to call AI manipulation
EntityAI.ForceNextAction(actor, "AttackBehavior");
```

**Best practice - use turn hooks:**

```csharp
// SAFE: Call during turn start/end (AI is not evaluating)
TacticalEventHooks.OnTurnStart += (actorPtr) =>
{
    var actor = new GameObj(actorPtr);
    EntityAI.ForceNextAction(actor, "AttackBehavior");
};
```

### Memory Overhead of Override Systems

The override systems (temporary visibility, tile manipulation) use memory. Best practices:

1. **Clean up expired overrides**: Call `UpdateOverrides()` from `OnTurnEnd`
2. **Limit override count**: Don't create thousands of temporary overrides
3. **Use permanent changes when possible**: `turns=-1` has no overhead
4. **Clear on mission end**: Reset all overrides when mission completes

```csharp
TacticalEventHooks.OnMissionEnd += () =>
{
    EntityVisibility.ClearAllOverrides();
    TileManipulation.ClearAllOverrides();
};
```

### Temporary vs Permanent Changes

| Change Type | Memory Cost | Use When |
|-------------|-------------|----------|
| Permanent (`turns=-1`) | None | State should persist forever |
| Short-term (1-3 turns) | Low | Brief effects (smoke, stun) |
| Long-term (10+ turns) | Medium | Mission-duration effects |

**Example of efficient temporary changes:**

```csharp
// GOOD: Short-duration temporary
TileManipulation.SetBlocksLOS(tile, true, 3); // 3 turns only

// BAD: Long-duration temporary (use permanent instead)
TileManipulation.SetBlocksLOS(tile, true, 100); // Just use -1!

// BETTER: Permanent
TileManipulation.SetBlocksLOS(tile, true, -1);
```

### Cleanup Best Practices

Always clean up in `OnUnload()`:

```csharp
public class MyMod : IModpackPlugin
{
    private List<GameObj> modifiedActors = new();

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // ... initialization
    }

    public void OnUnload()
    {
        // Clean up intercepts
        Intercept.OnDamageApplied -= MyHandler;

        // Reset all modified actors
        foreach (var actor in modifiedActors.Where(a => !a.IsNull))
        {
            EntitySkills.ResetSkillModifications(actor, "PrimaryAttack");
            EntityCombat.SetMorale(actor, 50f);
        }

        // Clear collections
        modifiedActors.Clear();

        // Clear global overrides
        EntityVisibility.ClearAllOverrides();
        TileManipulation.ClearAllOverrides();
    }
}
```

---

## Troubleshooting

### Common Errors and Fixes

#### 1. "Invalid actor or target"

**Cause**: GameObj is null or points to freed memory

**Fix**: Add null checks

```csharp
if (actor.IsNull || !actor.IsAlive)
{
    DevConsole.Warn("Actor is null or dead");
    return;
}
```

#### 2. "Cannot manipulate AI during evaluation (thread safety)"

**Cause**: Calling AI methods during parallel AI evaluation

**Fix**: Use turn hooks or check `AI.IsAnyFactionThinking()`

```csharp
// BAD: Might be called during AI evaluation
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    EntityAI.ForceFleeDecision(target); // UNSAFE!
};

// GOOD: Use turn hook
TacticalEventHooks.OnTurnStart += (actorPtr) =>
{
    var actor = new GameObj(actorPtr);
    EntityAI.ForceFleeDecision(actor); // SAFE
};
```

#### 3. "Offset verification failed"

**Cause**: Memory offsets changed between game versions

**Fix**: Update to latest modkit version

```bash
# Update modkit
git pull origin main
dotnet build
```

#### 4. "Skill not found"

**Cause**: Skill ID doesn't exist or is misspelled

**Fix**: List all skills first

```csharp
var skills = EntityCombat.GetSkills(actor);
foreach (var skill in skills)
{
    DevConsole.Log($"Available skill: {skill.Name}");
}

// Then use exact name
EntityCombat.UseAbility(actor, "skill.rifle_burst");
```

#### 5. "Path blocked or no AP"

**Cause**: Movement failed due to pathfinding or AP

**Fix**: Check movement range and AP first

```csharp
// Check if destination is reachable
var range = EntityMovement.GetMovementRange(actor);
if (!range.Contains((targetX, targetY)))
{
    DevConsole.Warn("Destination out of range");
    return;
}

// Check AP
var ap = EntityMovement.GetRemainingAP(actor);
if (ap < 2)
{
    DevConsole.Warn("Not enough AP to move");
    return;
}

// Now safe to move
EntityMovement.MoveTo(actor, targetX, targetY);
```

#### 6. "IL2CPP reflection failed"

**Cause**: Game type not found or method signature changed

**Fix**: Check that types are loaded

```csharp
try
{
    var result = EntityCombat.Attack(actor, target);
    if (!result.Success)
    {
        DevConsole.Warn($"Attack failed: {result.Error}");
    }
}
catch (Exception ex)
{
    DevConsole.Error($"IL2CPP reflection error: {ex.Message}");
    ModError.ReportInternal("MyMod", "Attack failed", ex);
}
```

### Build Errors

#### "Type 'GameObj' not found"

**Fix**: Add SDK using statement

```csharp
using Menace.SDK;
```

#### "Method 'EntityCombat.Attack' not found"

**Fix**: Update modkit references

```xml
<ItemGroup>
  <Reference Include="Menace.ModpackLoader">
    <HintPath>..\..\third_party\bundled\ModpackLoader\Menace.ModpackLoader.dll</HintPath>
  </Reference>
</ItemGroup>
```

---

## Advanced Techniques

### Chaining Multiple Actions

Create complex behavior sequences:

```csharp
public void ExecuteComboAttack(GameObj attacker, GameObj target)
{
    // Step 1: Move into range
    var targetPos = EntityMovement.GetPosition(target);
    if (!targetPos.HasValue) return;

    var attackerPos = EntityMovement.GetPosition(attacker);
    if (!attackerPos.HasValue) return;

    // Calculate optimal attack position (2 tiles away)
    var optimalPos = CalculateOptimalPosition(attackerPos.Value, targetPos.Value, 2);

    var moveResult = EntityMovement.MoveTo(attacker, optimalPos.x, optimalPos.y);
    if (!moveResult.Success)
    {
        DevConsole.Warn("Failed to position for combo");
        return;
    }

    // Step 2: Deploy heavy weapon if available
    var skills = EntityCombat.GetSkills(attacker);
    if (skills.Any(s => s.Name.Contains("HeavyWeapon")))
    {
        EntityState.SetHeavyWeaponDeployed(attacker, true);
    }

    // Step 3: Execute primary attack
    var attackResult = EntityCombat.Attack(attacker, target);
    if (!attackResult.Success)
    {
        DevConsole.Warn($"Attack failed: {attackResult.Error}");
        return;
    }

    // Step 4: If target survives, apply suppression
    if (target.IsAlive)
    {
        EntityCombat.ApplySuppression(target, 50f);
    }

    // Step 5: Chain grenade skill if available
    var grenadeSkill = skills.FirstOrDefault(s => s.Name.Contains("Grenade"));
    if (grenadeSkill != null && grenadeSkill.CanUse)
    {
        EntityCombat.UseAbility(attacker, grenadeSkill.Name, target);
    }
}

private (int x, int y) CalculateOptimalPosition((int x, int y) from, (int x, int y) to, int distance)
{
    // Simplified - calculate position 'distance' tiles from target
    var dx = to.x - from.x;
    var dy = to.y - from.y;
    var dist = Math.Sqrt(dx * dx + dy * dy);

    var ratio = distance / dist;
    return ((int)(from.x + dx * ratio), (int)(from.y + dy * ratio));
}
```

### State Machines Using Flags

Create complex behavior states:

```csharp
public enum UnitState
{
    Idle,
    Scouting,
    Engaging,
    Retreating,
    Reinforcing
}

private Dictionary<IntPtr, UnitState> unitStates = new();

public void UpdateUnitStateMachine(GameObj unit)
{
    if (!unitStates.ContainsKey(unit.Pointer))
        unitStates[unit.Pointer] = UnitState.Idle;

    var currentState = unitStates[unit.Pointer];
    var combat = EntityCombat.GetCombatInfo(unit);
    var movement = EntityMovement.GetMovementInfo(unit);

    switch (currentState)
    {
        case UnitState.Idle:
            // Transition to Scouting if full AP
            if (movement.CurrentAP >= movement.APAtTurnStart)
            {
                TransitionToScouting(unit);
            }
            break;

        case UnitState.Scouting:
            // Transition to Engaging if enemy spotted
            var enemies = FindNearbyEnemies(unit);
            if (enemies.Count > 0)
            {
                TransitionToEngaging(unit, enemies.First());
            }
            break;

        case UnitState.Engaging:
            // Transition to Retreating if low HP
            if (combat.HPPercent < 0.3f)
            {
                TransitionToRetreating(unit);
            }
            break;

        case UnitState.Retreating:
            // Transition to Reinforcing if reached rally point
            var position = EntityMovement.GetPosition(unit);
            if (IsAtRallyPoint(position))
            {
                TransitionToReinforcing(unit);
            }
            break;

        case UnitState.Reinforcing:
            // Transition back to Idle when healed
            if (combat.HPPercent > 0.7f)
            {
                TransitionToIdle(unit);
            }
            break;
    }
}

private void TransitionToScouting(GameObj unit)
{
    unitStates[unit.Pointer] = UnitState.Scouting;

    // Increase movement range
    var movement = EntityMovement.GetMovementInfo(unit);
    EntityMovement.SetAP(unit, movement.APAtTurnStart + 2);

    // Reveal more area
    EntityState.RevealToAll(unit);

    DevConsole.Log($"{unit.GetName()} -> Scouting");
}

private void TransitionToEngaging(GameObj unit, GameObj target)
{
    unitStates[unit.Pointer] = UnitState.Engaging;

    // Boost morale
    EntityCombat.SetMorale(unit, 80f);

    // Force attack action
    EntityAI.ForceNextAction(unit, "AttackBehavior", target, 10000);

    DevConsole.Log($"{unit.GetName()} -> Engaging {target.GetName()}");
}

private void TransitionToRetreating(GameObj unit)
{
    unitStates[unit.Pointer] = UnitState.Retreating;

    // Force flee
    EntityAI.ForceFleeDecision(unit);

    // Conceal from enemies
    EntityState.ConcealFromAll(unit);

    DevConsole.Log($"{unit.GetName()} -> Retreating");
}

private void TransitionToReinforcing(GameObj unit)
{
    unitStates[unit.Pointer] = UnitState.Reinforcing;

    // Heal over time
    EntityCombat.Heal(unit, 20);

    // Clear suppression
    EntityCombat.SetSuppression(unit, 0f);

    DevConsole.Log($"{unit.GetName()} -> Reinforcing");
}

private void TransitionToIdle(GameObj unit)
{
    unitStates[unit.Pointer] = UnitState.Idle;

    // Reveal to allies
    EntityState.RevealToAll(unit);

    DevConsole.Log($"{unit.GetName()} -> Idle");
}
```

### Complex AI Manipulation

Create squad-level AI coordination:

```csharp
public class SquadAICoordinator
{
    private Dictionary<int, List<GameObj>> squads = new();

    public void CoordinateSquad(int squadID)
    {
        if (!squads.ContainsKey(squadID)) return;

        var squad = squads[squadID].Where(u => !u.IsNull && u.IsAlive).ToList();
        if (squad.Count == 0) return;

        // Determine squad objective
        var objective = DetermineSquadObjective(squad);

        // Assign roles based on objective
        switch (objective)
        {
            case "Attack":
                CoordinateAttack(squad);
                break;
            case "Defend":
                CoordinateDefense(squad);
                break;
            case "Retreat":
                CoordinateRetreat(squad);
                break;
        }
    }

    private void CoordinateAttack(List<GameObj> squad)
    {
        // Find best target
        var enemies = FindVisibleEnemies(squad);
        if (enemies.Count == 0) return;

        var priorityTarget = enemies
            .OrderByDescending(e => CalculateThreatScore(e))
            .First();

        // Assign flanking positions
        var flankers = squad.Take(2).ToList();
        var mainForce = squad.Skip(2).ToList();

        // Flankers move to sides
        foreach (var flanker in flankers)
        {
            var flankPos = CalculateFlankPosition(priorityTarget);
            EntityMovement.MoveTo(flanker, flankPos.x, flankPos.y);
        }

        // Main force attacks directly
        foreach (var unit in mainForce)
        {
            EntityAI.ForceNextAction(unit, "AttackBehavior", priorityTarget, 10000);
        }
    }

    private void CoordinateDefense(List<GameObj> squad)
    {
        // Find defensive positions
        var defensivePositions = FindCoverPositions(squad.Count);

        for (int i = 0; i < squad.Count && i < defensivePositions.Count; i++)
        {
            var unit = squad[i];
            var pos = defensivePositions[i];

            // Move to defensive position
            EntityMovement.MoveTo(unit, pos.x, pos.y);

            // Deploy if has heavy weapon
            if (EntityCombat.GetSkills(unit).Any(s => s.Name.Contains("HeavyWeapon")))
            {
                EntityState.SetHeavyWeaponDeployed(unit, true);
            }

            // Go into overwatch
            var overwatchSkill = EntityCombat.GetSkills(unit)
                .FirstOrDefault(s => s.Name.Contains("Overwatch"));

            if (overwatchSkill != null)
            {
                EntityCombat.UseAbility(unit, overwatchSkill.Name);
            }
        }
    }

    private void CoordinateRetreat(List<GameObj> squad)
    {
        // All units retreat to rally point
        var rallyPoint = FindRallyPoint();

        foreach (var unit in squad)
        {
            // Force flee
            EntityAI.ForceFleeDecision(unit);

            // Provide smoke cover
            var smokeSkill = EntityCombat.GetSkills(unit)
                .FirstOrDefault(s => s.Name.Contains("Smoke"));

            if (smokeSkill != null && smokeSkill.CanUse)
            {
                EntityCombat.UseAbility(unit, smokeSkill.Name);
            }

            // Move to rally point
            if (rallyPoint.HasValue)
            {
                EntityMovement.MoveTo(unit, rallyPoint.Value.x, rallyPoint.Value.y);
            }
        }
    }

    private string DetermineSquadObjective(List<GameObj> squad)
    {
        // Calculate squad strength
        var avgHP = squad.Average(u => EntityCombat.GetCombatInfo(u).HPPercent);
        var avgMorale = squad.Average(u => EntityCombat.GetCombatInfo(u).Morale);

        // Decision logic
        if (avgHP < 0.3f || avgMorale < 30f)
            return "Retreat";

        if (avgHP > 0.7f && avgMorale > 60f)
            return "Attack";

        return "Defend";
    }

    // Helper methods (simplified)
    private List<GameObj> FindVisibleEnemies(List<GameObj> squad) => new();
    private float CalculateThreatScore(GameObj enemy) => 0f;
    private (int x, int y) CalculateFlankPosition(GameObj target) => (0, 0);
    private List<(int x, int y)> FindCoverPositions(int count) => new();
    private (int x, int y)? FindRallyPoint() => null;
}
```

### Override System Extensions

Extend the temporary override system:

```csharp
public class ExtendedOverrideSystem
{
    private class CompositeOverride
    {
        public List<Action> ApplyActions { get; set; } = new();
        public List<Action> RevertActions { get; set; } = new();
        public int TurnsRemaining { get; set; }
    }

    private Dictionary<string, CompositeOverride> namedOverrides = new();

    public void CreateNamedOverride(string name, int turns)
    {
        if (namedOverrides.ContainsKey(name))
        {
            DevConsole.Warn($"Override '{name}' already exists");
            return;
        }

        namedOverrides[name] = new CompositeOverride
        {
            TurnsRemaining = turns
        };
    }

    public void AddOverrideAction(string name, GameObj target, Action<GameObj> apply, Action<GameObj> revert)
    {
        if (!namedOverrides.ContainsKey(name)) return;

        var over = namedOverrides[name];
        over.ApplyActions.Add(() => apply(target));
        over.RevertActions.Add(() => revert(target));
    }

    public void ApplyNamedOverride(string name)
    {
        if (!namedOverrides.TryGetValue(name, out var over)) return;

        foreach (var action in over.ApplyActions)
        {
            action();
        }

        DevConsole.Log($"Applied override '{name}' for {over.TurnsRemaining} turns");
    }

    public void UpdateOverrides()
    {
        var expired = new List<string>();

        foreach (var kvp in namedOverrides)
        {
            kvp.Value.TurnsRemaining--;

            if (kvp.Value.TurnsRemaining <= 0)
            {
                // Revert all actions
                foreach (var action in kvp.Value.RevertActions)
                {
                    action();
                }

                expired.Add(kvp.Key);
                DevConsole.Log($"Override '{kvp.Key}' expired");
            }
        }

        foreach (var name in expired)
        {
            namedOverrides.Remove(name);
        }
    }
}

// Usage example:
var overrideSystem = new ExtendedOverrideSystem();

// Create "PowerUp" override lasting 3 turns
overrideSystem.CreateNamedOverride("PowerUp", 3);

// Add multiple effects
overrideSystem.AddOverrideAction("PowerUp", soldier,
    apply: u => EntityCombat.SetMorale(u, 100f),
    revert: u => EntityCombat.SetMorale(u, 50f));

overrideSystem.AddOverrideAction("PowerUp", soldier,
    apply: u => EntityMovement.SetAP(u, 10),
    revert: u => EntityMovement.SetAP(u, 4));

overrideSystem.AddOverrideAction("PowerUp", soldier,
    apply: u => EntityState.RevealToAll(u),
    revert: u => EntityState.ConcealFromAll(u));

// Apply all effects
overrideSystem.ApplyNamedOverride("PowerUp");

// Update each turn
TacticalEventHooks.OnTurnEnd += (_) => overrideSystem.UpdateOverrides();
```

---

## Conclusion

The Action API system provides unprecedented control over tactical combat through 72 methods across 7 modules. By combining intercepts for detection with actions for response, you can create complex, dynamic gameplay modifications that rival professional game features.

**Key Takeaways:**

1. **Intercepts detect, Actions respond** - Use them together
2. **Thread safety matters** - Never call AI methods during parallel evaluation
3. **Clean up after yourself** - Always unsubscribe and reset state in OnUnload()
4. **Test in REPL** - Use F1 console to test methods interactively
5. **Start simple** - Build complexity gradually

**Next Steps:**

- Read the [Intercept API documentation](../coding-sdk/api/intercept.md) for the detection side
- Check out the [Combat Intercepts guide](13-combat-intercepts.md) for OnDamageApplied examples
- Experiment in REPL with the methods that interest you
- Build your first mod combining intercepts and actions!

**Need Help?**

- Join the modding Discord
- Check the GitHub issues
- Read the API reference docs
- Ask in the REPL using `help EntityCombat`

Happy modding!
