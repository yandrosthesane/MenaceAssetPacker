# EntitySkills

`Menace.SDK.EntitySkills` -- Comprehensive skill manipulation module for adding, removing, modifying, and querying entity skills.

## Overview

EntitySkills provides complete control over an actor's skill container, cooldown management, and skill parameter modification. This module enables dynamic skill systems, cooldown manipulation, and skill state queries for advanced gameplay mechanics.

**Key Features:**
- Add/remove skills dynamically from actors
- Modify skill parameters (AP cost, range, cooldown)
- Enable/disable skills programmatically
- Query comprehensive skill state information
- Reset skill modifications to template defaults

**Based on Ghidra reverse engineering:**
- Skill.enabled @ +0x38 (bool)
- Skill.actionPointCost @ +0xA0 (int32)
- Skill ranges: minRange @ +0xB4, optimalRange @ +0xB8, maxRange @ +0xBC
- Skill.eventHandlers @ +0x48 (List of effect handlers)
- CooldownEffectHandler.remainingCooldown @ handler+0x20 (int32)

**Method Addresses:**
- SkillContainer.Add() @ 0x1806e76e0
- SkillContainer.RemoveSkillByIndex() @ 0x1806edfb0
- Skill.SetRanges() @ 0x1806e1d80
- Skill.ChangeActionPointCost() @ 0x1806d8190

## Module Path

```csharp
using Menace.SDK;
// Access via: EntitySkills.MethodName(...)
```

## Skill Container Operations

### AddSkill

**Signature**: `bool AddSkill(GameObj actor, string skillTemplateID)`

**Description**: Add a skill to an actor's skill container by template ID. The skill template must exist in the game's resources.

**Parameters:**
- `actor` (GameObj): The actor to add the skill to
- `skillTemplateID` (string): The skill template identifier (e.g., "skill.overwatch")

**Returns**: True if the skill was successfully added

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Add overwatch skill
if (EntitySkills.AddSkill(actor, "skill.overwatch"))
{
    DevConsole.Log("Overwatch skill added successfully");
}

// Add multiple skills
var newSkills = new[] { "skill.reload", "skill.grenade_throw", "skill.medkit" };
foreach (var skill in newSkills)
{
    EntitySkills.AddSkill(actor, skill);
}
```

**Related:**
- [RemoveSkill](#removeskill) - Remove skills
- [HasSkill](#hasskill) - Check skill existence
- [GetSkillIDs](#getskillids) - List all skills

**Notes:**
- Uses SkillContainer.Add() method at 0x1806e76e0
- Skill template must exist in game resources
- Does not check for duplicates - can add same skill multiple times
- Added skills inherit template defaults

---

### RemoveSkill

**Signature**: `bool RemoveSkill(GameObj actor, string skillID)`

**Description**: Remove a skill from an actor's skill container by skill ID. Finds the skill index and removes it.

**Parameters:**
- `actor` (GameObj): The actor to remove the skill from
- `skillID` (string): The skill identifier to remove

**Returns**: True if the skill was successfully removed

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Remove a specific skill
if (EntitySkills.RemoveSkill(actor, "skill.overwatch"))
{
    DevConsole.Log("Overwatch skill removed");
}

// Conditional removal based on class change
if (HasClassChanged(actor))
{
    var skillsToRemove = new[] { "skill.psionics", "skill.heavy_weapon" };
    foreach (var skill in skillsToRemove)
    {
        EntitySkills.RemoveSkill(actor, skill);
    }
}
```

**Related:**
- [AddSkill](#addskill) - Add skills
- [HasSkill](#hasskill) - Check before removing

**Notes:**
- Uses SkillContainer.RemoveSkillByIndex() at 0x1806edfb0
- Iterates skills to find matching ID, then removes by index
- Returns false if skill not found
- Does not affect other instances if skill added multiple times

---

### HasSkill

**Signature**: `bool HasSkill(GameObj actor, string skillID)`

**Description**: Check if an actor has a specific skill.

**Parameters:**
- `actor` (GameObj): The actor to check
- `skillID` (string): The skill identifier to look for

**Returns**: True if the actor has the skill

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Check before using skill-specific logic
if (EntitySkills.HasSkill(actor, "skill.overwatch"))
{
    DevConsole.Log("Actor can use overwatch");
    EnableOverwatchUI(actor);
}

// Conditional ability unlocks
if (GetActorLevel(actor) >= 5 && !EntitySkills.HasSkill(actor, "skill.advanced_tactics"))
{
    EntitySkills.AddSkill(actor, "skill.advanced_tactics");
    DevConsole.Log("Advanced tactics unlocked!");
}
```

**Related:**
- [GetSkillIDs](#getskillids) - Get all skill IDs
- [GetSkillState](#getskillstate) - Get detailed skill info

---

### GetSkillIDs

**Signature**: `List<string> GetSkillIDs(GameObj actor)`

**Description**: Get all skill IDs for an actor.

**Parameters:**
- `actor` (GameObj): The actor to query

**Returns**: List of skill IDs

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();
var skillIDs = EntitySkills.GetSkillIDs(actor);

DevConsole.Log($"Actor has {skillIDs.Count} skills:");
foreach (var skillID in skillIDs)
{
    var state = EntitySkills.GetSkillState(actor, skillID);
    DevConsole.Log($"  - {skillID}: AP={state.APCost}, Range={state.MaxRange}, CD={state.RemainingCooldown}");
}
```

**Related:**
- [HasSkill](#hasskill) - Check specific skill
- [GetSkillState](#getskillstate) - Get detailed info

---

## Cooldown Management

### SetCooldown

**Signature**: `bool SetCooldown(GameObj actor, string skillID, int turns)`

**Description**: Set the cooldown for a specific skill. Cooldown is stored in CooldownEffectHandler at handler+0x20.

**Parameters:**
- `actor` (GameObj): The actor whose skill cooldown to set
- `skillID` (string): The skill identifier
- `turns` (int): Number of turns for the cooldown

**Returns**: True if the cooldown was successfully set

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Set 3-turn cooldown on powerful skill
if (EntitySkills.SetCooldown(actor, "skill.ultimate_ability", 3))
{
    DevConsole.Log("Ultimate ability on 3-turn cooldown");
}

// Implement cooldown reduction perk
if (HasPerk(actor, "CooldownReduction"))
{
    var currentCD = EntitySkills.GetRemainingCooldown(actor, "skill.grenade_throw");
    EntitySkills.SetCooldown(actor, "skill.grenade_throw", currentCD - 1);
}
```

**Related:**
- [ResetCooldown](#resetcooldown) - Set to 0
- [ModifyCooldown](#modifycooldown) - Modify by delta
- [GetRemainingCooldown](#getremainingcooldown) - Query cooldown

**Notes:**
- Searches skill.eventHandlers list at +0x48 for CooldownEffectHandler
- Writes to handler+0x20 (remainingCooldown)
- Returns false if skill has no cooldown handler
- Does not enforce minimum/maximum bounds

---

### ResetCooldown

**Signature**: `bool ResetCooldown(GameObj actor, string skillID)`

**Description**: Reset the cooldown for a specific skill (set to 0).

**Parameters:**
- `actor` (GameObj): The actor whose skill cooldown to reset
- `skillID` (string): The skill identifier

**Returns**: True if the cooldown was successfully reset

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Instant cooldown reset on power-up pickup
if (EntitySkills.ResetCooldown(actor, "skill.ultimate_ability"))
{
    DevConsole.Log("Ultimate ability ready!");
}

// Reset all cooldowns (for testing or power-up)
var skills = EntitySkills.GetSkillIDs(actor);
foreach (var skillID in skills)
{
    EntitySkills.ResetCooldown(actor, skillID);
}
```

**Related:**
- [SetCooldown](#setcooldown) - Set specific value
- [ModifyCooldown](#modifycooldown) - Adjust by amount

---

### ModifyCooldown

**Signature**: `bool ModifyCooldown(GameObj actor, string skillID, int delta)`

**Description**: Modify the cooldown for a specific skill by a delta amount (can be negative).

**Parameters:**
- `actor` (GameObj): The actor whose skill cooldown to modify
- `skillID` (string): The skill identifier
- `delta` (int): Amount to add to current cooldown (can be negative)

**Returns**: True if the cooldown was successfully modified

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Reduce cooldown by 1 turn (veteran perk)
if (EntitySkills.ModifyCooldown(actor, "skill.tactical_scan", -1))
{
    DevConsole.Log("Veteran cooldown reduction applied");
}

// Increase cooldown as penalty
if (ActorIsStunned(actor))
{
    EntitySkills.ModifyCooldown(actor, "skill.attack", 2);
}
```

**Related:**
- [SetCooldown](#setcooldown) - Set absolute value
- [GetRemainingCooldown](#getremainingcooldown) - Query current value

**Notes:**
- Automatically clamps result to minimum of 0
- Returns false if skill not found or has no cooldown

---

### GetRemainingCooldown

**Signature**: `int GetRemainingCooldown(GameObj actor, string skillID)`

**Description**: Get the remaining cooldown turns for a specific skill.

**Parameters:**
- `actor` (GameObj): The actor to query
- `skillID` (string): The skill identifier

**Returns**: Remaining cooldown turns, or 0 if skill has no cooldown or is ready

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

var cooldown = EntitySkills.GetRemainingCooldown(actor, "skill.ultimate_ability");
if (cooldown > 0)
{
    DevConsole.Log($"Ultimate ready in {cooldown} turns");
}
else
{
    DevConsole.Log("Ultimate ready to use!");
}

// Display cooldown status for all skills
var skills = EntitySkills.GetSkillIDs(actor);
foreach (var skillID in skills)
{
    var cd = EntitySkills.GetRemainingCooldown(actor, skillID);
    if (cd > 0)
    {
        DevConsole.Log($"{skillID}: {cd} turns");
    }
}
```

**Related:**
- [SetCooldown](#setcooldown) - Modify cooldown
- [GetSkillState](#getskillstate) - Get all skill info including cooldown

**Notes:**
- Reads from CooldownEffectHandler.remainingCooldown at handler+0x20
- Returns 0 if skill has no cooldown handler
- Safe to call repeatedly

---

## Skill Parameter Modification

### ModifySkillRange

**Signature**: `bool ModifySkillRange(GameObj actor, string skillID, int newRange)`

**Description**: Modify the range parameters for a specific skill. Can use Skill.SetRanges() method or direct field writes.

**Parameters:**
- `actor` (GameObj): The actor whose skill to modify
- `skillID` (string): The skill identifier
- `newRange` (int): New maximum range value

**Returns**: True if the range was successfully modified

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Increase sniper range
if (EntitySkills.ModifySkillRange(actor, "skill.sniper_shot", 20))
{
    DevConsole.Log("Sniper range extended to 20 tiles");
}

// Dynamic range based on equipment
if (HasScope(actor))
{
    EntitySkills.ModifySkillRange(actor, "skill.aimed_shot", 12);
}
else
{
    EntitySkills.ModifySkillRange(actor, "skill.aimed_shot", 8);
}
```

**Related:**
- [ResetSkillModifications](#resetskillmodifications) - Reset to template defaults
- [GetSkillState](#getskillstate) - Query current range

**Notes:**
- Uses Skill.SetRanges() at 0x1806e1d80 if available
- Falls back to direct field writes at +0xB4 (min), +0xB8 (optimal), +0xBC (max)
- Sets min=0, optimal=newRange, max=newRange
- Does not validate against template or balance constraints

---

### ModifySkillAPCost

**Signature**: `bool ModifySkillAPCost(GameObj actor, string skillID, int newCost)`

**Description**: Modify the Action Point cost for a specific skill. Can use Skill.ChangeActionPointCost() method or direct write.

**Parameters:**
- `actor` (GameObj): The actor whose skill to modify
- `skillID` (string): The skill identifier
- `newCost` (int): New AP cost value

**Returns**: True if the AP cost was successfully modified

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Reduce skill cost (efficiency perk)
if (HasPerk(actor, "Efficient"))
{
    EntitySkills.ModifySkillAPCost(actor, "skill.reload", 1); // Down from 2
    EntitySkills.ModifySkillAPCost(actor, "skill.grenade_throw", 2); // Down from 3
}

// Increase cost as penalty
if (ActorIsWounded(actor))
{
    var skills = EntitySkills.GetSkillIDs(actor);
    foreach (var skillID in skills)
    {
        var state = EntitySkills.GetSkillState(actor, skillID);
        EntitySkills.ModifySkillAPCost(actor, skillID, state.APCost + 1);
    }
}
```

**Related:**
- [ResetSkillModifications](#resetskillmodifications) - Reset to template defaults
- [Intercept.OnSkillApCost](intercept.md#skill-interceptors) - Intercept AP cost calculations

**Notes:**
- Uses Skill.ChangeActionPointCost() at 0x1806d8190 if available
- Falls back to direct field write at +0xA0
- Does not enforce minimum (can set to 0)
- Permanent until reset or restored

---

### EnableSkill

**Signature**: `bool EnableSkill(GameObj actor, string skillID)`

**Description**: Enable a specific skill. Writes true to Skill.enabled at +0x38.

**Parameters:**
- `actor` (GameObj): The actor whose skill to enable
- `skillID` (string): The skill identifier

**Returns**: True if the skill was successfully enabled

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Enable skill on level up
if (ActorReachedLevel(actor, 5))
{
    if (EntitySkills.EnableSkill(actor, "skill.advanced_tactics"))
    {
        DevConsole.Log("Advanced tactics unlocked!");
    }
}

// Enable skills based on equipment
if (HasWeaponType(actor, "Sniper"))
{
    EntitySkills.EnableSkill(actor, "skill.sniper_shot");
}
```

**Related:**
- [DisableSkill](#disableskill) - Disable skills
- [GetSkillState](#getskillstate) - Check enabled state

**Notes:**
- Writes to Skill.enabled at +0x38
- Does not check IsUsable() conditions
- Skills can be enabled but still unusable due to other constraints

---

### DisableSkill

**Signature**: `bool DisableSkill(GameObj actor, string skillID)`

**Description**: Disable a specific skill. Writes false to Skill.enabled at +0x38.

**Parameters:**
- `actor` (GameObj): The actor whose skill to disable
- `skillID` (string): The skill identifier

**Returns**: True if the skill was successfully disabled

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Disable skill as penalty (stunned)
if (ActorIsStunned(actor))
{
    var skills = EntitySkills.GetSkillIDs(actor);
    foreach (var skillID in skills)
    {
        if (!IsBasicSkill(skillID))
        {
            EntitySkills.DisableSkill(actor, skillID);
        }
    }
}

// Disable heavy weapon skills when not deployed
if (!IsHeavyWeaponDeployed(actor))
{
    EntitySkills.DisableSkill(actor, "skill.heavy_barrage");
}
```

**Related:**
- [EnableSkill](#enableskill) - Enable skills
- [GetSkillState](#getskillstate) - Check enabled state

**Notes:**
- Writes to Skill.enabled at +0x38
- Disabled skills cannot be used even if otherwise usable
- Does not affect cooldown or other parameters

---

## Skill State Queries

### GetSkillState

**Signature**: `SkillStateInfo GetSkillState(GameObj actor, string skillID)`

**Description**: Get comprehensive state information for a specific skill including enabled state, AP cost, range, cooldown, and usability.

**Parameters:**
- `actor` (GameObj): The actor to query
- `skillID` (string): The skill identifier

**Returns**: SkillStateInfo object, or null if skill not found

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();
var state = EntitySkills.GetSkillState(actor, "skill.grenade_throw");

if (state != null)
{
    DevConsole.Log($"Skill: {state.SkillID}");
    DevConsole.Log($"  Enabled: {state.IsEnabled}");
    DevConsole.Log($"  Usable: {state.IsUsable}");
    DevConsole.Log($"  AP Cost: {state.APCost}");
    DevConsole.Log($"  Range: {state.MinRange}-{state.MaxRange} (optimal: {state.OptimalRange})");
    DevConsole.Log($"  Cooldown: {state.RemainingCooldown} turns");
    DevConsole.Log($"  Template: {state.TemplateName}");
}

// Check if skill is ready to use
if (state.IsEnabled && state.RemainingCooldown == 0 && state.IsUsable)
{
    DevConsole.Log("Skill ready for use!");
}
```

**Related:**
- [GetSkillIDs](#getskillids) - List all skills
- [GetRemainingCooldown](#getremainingcooldown) - Query only cooldown

**Notes:**
- Returns null if skill not found
- IsUsable checks game's usability conditions
- Single method to get complete skill state

---

### ResetSkillModifications

**Signature**: `bool ResetSkillModifications(GameObj actor, string skillID)`

**Description**: Reset all modifications to a skill, restoring template defaults. Reads values from the skill's template and applies them.

**Parameters:**
- `actor` (GameObj): The actor whose skill to reset
- `skillID` (string): The skill identifier

**Returns**: True if the skill was successfully reset

**Example:**
```csharp
var actor = TacticalController.GetActiveActor();

// Reset skill after temporary buff expires
if (BuffExpired(actor, "SkillBoost"))
{
    EntitySkills.ResetSkillModifications(actor, "skill.tactical_scan");
    DevConsole.Log("Skill boost expired, skill reset to normal");
}

// Reset all skills (e.g., on class respec)
var skills = EntitySkills.GetSkillIDs(actor);
foreach (var skillID in skills)
{
    EntitySkills.ResetSkillModifications(actor, skillID);
}
```

**Related:**
- [ModifySkillRange](#modifyskillrange) - Modify range
- [ModifySkillAPCost](#modifyskillapcost) - Modify AP cost

**Notes:**
- Restores AP cost, ranges, and cooldown to template defaults
- Ensures skill is enabled
- Does not remove the skill from the actor

---

## SkillStateInfo Structure

```csharp
public class SkillStateInfo
{
    public string SkillID { get; set; }
    public bool IsEnabled { get; set; }
    public int APCost { get; set; }
    public int MinRange { get; set; }
    public int OptimalRange { get; set; }
    public int MaxRange { get; set; }
    public int RemainingCooldown { get; set; }
    public bool IsUsable { get; set; }
    public string TemplateName { get; set; }
}
```

## Complete Example

```csharp
using Menace.SDK;

// Dynamic skill system based on equipment and level
public class DynamicSkillManager
{
    public void UpdateActorSkills(GameObj actor)
    {
        var level = GetActorLevel(actor);
        var weaponType = GetEquippedWeaponType(actor);

        // Add skills based on level
        if (level >= 3 && !EntitySkills.HasSkill(actor, "skill.tactical_awareness"))
        {
            EntitySkills.AddSkill(actor, "skill.tactical_awareness");
            DevConsole.Log("Tactical Awareness unlocked!");
        }

        // Modify skills based on weapon
        if (weaponType == "Sniper")
        {
            // Increase range for sniper skills
            EntitySkills.ModifySkillRange(actor, "skill.aimed_shot", 15);
            EntitySkills.ModifySkillAPCost(actor, "skill.aimed_shot", 2); // Increase cost
        }
        else if (weaponType == "Shotgun")
        {
            // Reduce range, reduce cost
            EntitySkills.ModifySkillRange(actor, "skill.aimed_shot", 4);
            EntitySkills.ModifySkillAPCost(actor, "skill.aimed_shot", 1);
        }
    }

    public void ApplyCooldownReduction(GameObj actor, float reductionPct)
    {
        var skills = EntitySkills.GetSkillIDs(actor);
        foreach (var skillID in skills)
        {
            var currentCD = EntitySkills.GetRemainingCooldown(actor, skillID);
            if (currentCD > 0)
            {
                int reduction = (int)(currentCD * reductionPct);
                EntitySkills.ModifyCooldown(actor, skillID, -reduction);
            }
        }
    }

    public void DisableSkillsDuringStun(GameObj actor)
    {
        var skills = EntitySkills.GetSkillIDs(actor);
        foreach (var skillID in skills)
        {
            // Keep basic attack enabled
            if (skillID != "skill.basic_attack")
            {
                EntitySkills.DisableSkill(actor, skillID);
            }
        }
    }

    public void PrintSkillSummary(GameObj actor)
    {
        var skills = EntitySkills.GetSkillIDs(actor);
        DevConsole.Log($"=== {actor.GetName()} Skills ({skills.Count}) ===");

        foreach (var skillID in skills)
        {
            var state = EntitySkills.GetSkillState(actor, skillID);
            if (state == null) continue;

            string status = state.IsEnabled ? "Enabled" : "Disabled";
            if (state.RemainingCooldown > 0)
                status += $" (CD: {state.RemainingCooldown})";

            DevConsole.Log($"{skillID}: {status}, AP={state.APCost}, Range={state.MaxRange}");
        }
    }
}

// Skill cooldown management for turns
TacticalEventHooks.OnTurnStart += (actorPtr) =>
{
    var actor = new GameObj(actorPtr);
    var skills = EntitySkills.GetSkillIDs(actor);

    // Display ready skills
    foreach (var skillID in skills)
    {
        var cd = EntitySkills.GetRemainingCooldown(actor, skillID);
        if (cd == 1) // Will be ready this turn
        {
            DevConsole.Log($"{skillID} ready!");
        }
    }
};
```

## See Also

- [Intercept](intercept.md) - Skill interceptors (OnSkillApCost, OnSkillCooldown, OnSkillExecute)
- [EntityState](entity-state.md) - Actor state manipulation
- [EntityAI](entity-ai.md) - AI behavior control
- [TacticalController](tactical-controller.md) - Entity selection and control
