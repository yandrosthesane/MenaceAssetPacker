# Skill System

## Overview

The skill system manages actor abilities, cooldowns, action point costs, and ranges. Skills are stored in a SkillContainer per actor and instantiated from SkillTemplate resources. Each skill has event handlers for effects like cooldowns, damage, and status application.

Based on reverse engineering findings from Phase 1 implementation (EntitySkills.cs).

## Architecture

```
SkillTemplate (ScriptableObject resource)
    ↓ (instantiated from)
Skill (runtime instance)
    ├── eventHandlers: List<EffectHandler>
    │   ├── CooldownEffectHandler
    │   ├── DamageEffectHandler
    │   └── StatusEffectHandler
    ├── enabled: bool
    ├── actionPointCost: int
    └── min/optimal/maxRange: int

SkillContainer (per actor)
    └── List<Skill> m_Skills
```

## Memory Layout

### Skill Class Offsets

| Offset | Type | Field Name | Description | Status |
|--------|------|------------|-------------|--------|
| 0x38 | byte | enabled | Skill is enabled and usable | ✅ Verified |
| 0x48 | List\<EffectHandler\> | eventHandlers | List of effect handlers (cooldown, damage, etc.) | ✅ Verified |
| 0xA0 | int32 | actionPointCost | AP cost to use skill | ✅ Verified |
| 0xB4 | int32 | minRange | Minimum effective range (tiles) | ✅ Verified |
| 0xB8 | int32 | optimalRange | Optimal range for accuracy bonuses | ✅ Verified |
| 0xBC | int32 | maxRange | Maximum range (tiles) | ✅ Verified |

### SkillTemplate Class Offsets

SkillTemplate is a ScriptableObject resource. Key properties:

| Property | Type | Description |
|----------|------|-------------|
| ActionPointCost | int32 | Default AP cost |
| MinRange | int32 | Default minimum range |
| OptimalRange | int32 | Default optimal range |
| MaxRange | int32 | Default maximum range |
| DefaultCooldown | int32 | Default cooldown turns |

**Access:** SkillTemplates are loaded via `Resources.FindObjectsOfTypeAll(typeof(SkillTemplate))`

### CooldownEffectHandler Offsets

| Offset | Type | Field Name | Description | Status |
|--------|------|------------|-------------|--------|
| 0x20 | int32 | remainingCooldown | Remaining turns before skill is available | ✅ Verified |

## Skill Container Structure

### SkillContainer Layout

```c
class SkillContainer {
    // ... base fields
    List<Skill> m_Skills;        // +0x?? (list of skill instances)
    Actor m_Owner;               // +0x?? (owning actor)
}
```

### SkillContainer Methods

| Address | Signature | Description |
|---------|-----------|-------------|
| 0x1806e76e0 | void Add(SkillTemplate template) | Add skill from template |
| 0x1806edfb0 | void RemoveSkillByIndex(int index) | Remove skill by index |
| N/A | List\<Skill\> GetAllSkills() | Get all skills in container |
| N/A | Skill GetSkillByID(string id, ...) | Find skill by ID string |

## Skill Class Methods

### Core Methods

| Address | Signature | Description |
|---------|-----------|-------------|
| 0x1806e1d80 | void SetRanges(int min, int optimal, int max) | Set all range values |
| 0x1806d8190 | void ChangeActionPointCost(int newCost) | Modify AP cost |
| N/A | bool IsUsable() | Check if skill can be used (AP, cooldown, etc.) |
| N/A | SkillTemplate GetTemplate() | Get the skill's source template |
| N/A | string GetID() | Get skill identifier string |

### Effect Handler Iteration

```c
// Skill.eventHandlers is a List<EffectHandler> at +0x48
IntPtr handlersPtr = *(IntPtr*)(skill + 0x48);
List<EffectHandler> handlers = new GameList(handlersPtr);

for (int i = 0; i < handlers.Count; i++) {
    EffectHandler handler = handlers[i];
    string typeName = handler.GetTypeName();

    if (typeName.Contains("CooldownEffectHandler")) {
        int cooldown = *(int32*)(handler + 0x20);
        // Modify cooldown...
    }
}
```

## Cooldown System

### Cooldown Handler Structure

CooldownEffectHandler extends EffectHandler and manages skill cooldown state:

```c
class CooldownEffectHandler : EffectHandler {
    // EffectHandler base fields    // +0x00 - 0x1F
    int32 remainingCooldown;        // +0x20 (turns remaining)
    int32 maxCooldown;              // +0x24 (max cooldown value, estimated)
}
```

### Cooldown Operations

**Read Cooldown:**
```c
IntPtr handlerPtr = FindCooldownHandler(skill);
int32 remaining = *(int32*)(handlerPtr + 0x20);
```

**Set Cooldown:**
```c
*(int32*)(handlerPtr + 0x20) = turns;
```

**Reset Cooldown:**
```c
*(int32*)(handlerPtr + 0x20) = 0;  // Skill immediately available
```

### Cooldown Lifecycle

1. **Skill Use:** When skill is activated, cooldown handler sets `remainingCooldown` to max value
2. **Turn End:** Each turn end, `remainingCooldown` decrements by 1
3. **Ready State:** When `remainingCooldown` reaches 0, skill is usable again
4. **IsUsable Check:** Skill.IsUsable() returns false if `remainingCooldown > 0`

## Skill Parameters

### Range System

Skills have three range values:

```c
// At skill instance
int32 minRange = *(int32*)(skill + 0xB4);     // Minimum distance
int32 optimalRange = *(int32*)(skill + 0xB8); // Best accuracy range
int32 maxRange = *(int32*)(skill + 0xBC);     // Maximum distance

// Modify all ranges
void SetRanges(Skill* skill, int min, int optimal, int max) {
    *(int32*)(skill + 0xB4) = min;
    *(int32*)(skill + 0xB8) = optimal;
    *(int32*)(skill + 0xBC) = max;
}
```

**Range Interpretation:**
- **minRange:** Minimum distance to target (0 = melee/adjacent)
- **optimalRange:** Range with maximum accuracy bonus
- **maxRange:** Beyond this distance, skill cannot be used

### Action Point Cost

AP cost determines how expensive the skill is to use:

```c
// Read AP cost
int32 cost = *(int32*)(skill + 0xA0);

// Modify AP cost
*(int32*)(skill + 0xA0) = newCost;

// Or use method (recommended)
ChangeActionPointCost(skill, newCost);  // @ 0x1806d8190
```

**AP Cost Guidelines:**
- 0 = Free action (e.g., toggle overwatch)
- 1-2 = Quick actions (reload, take cover)
- 3-4 = Standard actions (move, single shot)
- 5+ = Powerful abilities (grenades, special skills)

### Enabled State

The `enabled` field at 0x38 controls whether the skill appears in the UI and can be used:

```c
// Check enabled
byte enabled = *(byte*)(skill + 0x38);
bool isEnabled = (enabled != 0);

// Enable skill
*(byte*)(skill + 0x38) = 1;

// Disable skill
*(byte*)(skill + 0x38) = 0;
```

**Use Cases:**
- Disable skills temporarily (e.g., during stun)
- Hide unavailable skills from UI
- Conditional skill availability (e.g., heavy weapon must be deployed)

## SDK Implementation

The EntitySkills.cs SDK module provides comprehensive skill manipulation:

### Container Operations

```csharp
// Add skill from template
EntitySkills.AddSkill(actor, "skill.overwatch");

// Remove skill
EntitySkills.RemoveSkill(actor, "skill.grenade");

// Check if actor has skill
bool hasSkill = EntitySkills.HasSkill(actor, "skill.reload");

// Get all skill IDs
List<string> skillIDs = EntitySkills.GetSkillIDs(actor);
```

### Cooldown Management

```csharp
// Set cooldown (3 turns)
EntitySkills.SetCooldown(actor, "skill.grenade", 3);

// Reset cooldown (make immediately available)
EntitySkills.ResetCooldown(actor, "skill.overwatch");

// Modify cooldown by delta
EntitySkills.ModifyCooldown(actor, "skill.reload", -1); // Reduce by 1 turn

// Query remaining cooldown
int remaining = EntitySkills.GetRemainingCooldown(actor, "skill.grenade");
```

### Parameter Modification

```csharp
// Modify range
EntitySkills.ModifySkillRange(actor, "skill.shoot", newRange: 15);

// Modify AP cost
EntitySkills.ModifySkillAPCost(actor, "skill.grenade", newCost: 2);

// Enable/disable skill
EntitySkills.EnableSkill(actor, "skill.overwatch");
EntitySkills.DisableSkill(actor, "skill.reload");
```

### State Queries

```csharp
// Get comprehensive skill state
var state = EntitySkills.GetSkillState(actor, "skill.shoot");
if (state != null) {
    Console.WriteLine($"Enabled: {state.IsEnabled}");
    Console.WriteLine($"AP Cost: {state.APCost}");
    Console.WriteLine($"Range: {state.MinRange}-{state.MaxRange}");
    Console.WriteLine($"Cooldown: {state.RemainingCooldown}");
    Console.WriteLine($"Usable: {state.IsUsable}");
}

// Reset all modifications
EntitySkills.ResetSkillModifications(actor, "skill.shoot");
```

## Common Skill IDs

Based on game decompilation, common skill identifiers:

| Skill ID | Description |
|----------|-------------|
| skill.shoot | Standard shooting attack |
| skill.overwatch | Overwatch/reaction fire |
| skill.reload | Reload weapon |
| skill.grenade | Throw grenade |
| skill.medkit | Use medical item |
| skill.breach | Breach door/wall |
| skill.hunker | Take cover/defensive stance |
| skill.sprint | Sprint movement |

**Note:** Actual IDs vary by game version and mods. Use `EntitySkills.GetSkillIDs()` to enumerate available skills.

## Event Handlers

### Handler Types

Skills can have multiple effect handlers:

| Handler Type | Purpose | Key Fields |
|--------------|---------|------------|
| CooldownEffectHandler | Manages skill cooldown | remainingCooldown @ +0x20 |
| DamageEffectHandler | Applies damage on hit | damageAmount, damageType |
| StatusEffectHandler | Applies status effects | statusTemplate, duration |
| HealEffectHandler | Restores health | healAmount |
| SuppressionEffectHandler | Applies suppression | suppressionAmount |

### Finding Specific Handlers

```c
// Iterate eventHandlers list to find specific handler type
List<EffectHandler> handlers = GetHandlers(skill);

for (int i = 0; i < handlers.Count; i++) {
    EffectHandler handler = handlers[i];
    string typeName = GetTypeName(handler);

    if (typeName.Contains("DamageEffectHandler")) {
        // Found damage handler
        ModifyDamage(handler, newAmount);
    }
}
```

## Notes

### Offset Stability

All offsets verified through:
1. Ghidra decompilation analysis
2. Runtime testing in EntitySkills.cs
3. Successful skill manipulation in tactical combat

**Version:** Offsets verified for game version 34.0.1 (March 2026)

### Thread Safety

Skill modifications are safe during:
1. `TacticalEventHooks.OnTurnStart`/`OnTurnEnd`
2. When actor is not actively executing skill
3. When game is paused

Modifying skills during skill execution (damage calculation, effect application) may cause undefined behavior.

### Performance Notes

- **Template Lookup:** `Resources.FindObjectsOfTypeAll()` is expensive. Cache templates if doing batch operations.
- **Skill Iteration:** SkillContainer.GetAllSkills() returns a list copy. Cache the result if enumerating multiple times.
- **Handler Search:** Searching eventHandlers list is O(n). Cache handler pointers if modifying repeatedly.

### Fallback Methods

EntitySkills.cs provides dual implementation paths:
1. **Primary:** Use IL2CPP reflection to call game methods (SetRanges, ChangeActionPointCost)
2. **Fallback:** Direct memory writes if reflection fails

This ensures compatibility across different game versions and IL2CPP configurations.

### Future Research

1. Document DamageEffectHandler structure and damage calculation
2. Map StatusEffectHandler fields and status effect system
3. Research skill targeting system (valid targets, range checks)
4. Document skill animation triggers and VFX system
5. Investigate skill upgrade/modification system (if present)
6. Map skill prerequisite/requirement system

## See Also

- [actor-system.md](actor-system.md) - Actor class and SkillContainer integration
- [turn-action-system.md](turn-action-system.md) - Action point system and skill execution
- [skills-effects.md](skills-effects.md) - Skill effects and damage calculation
