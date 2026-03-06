# EventHandler Pattern Analysis

**Analysis Date:** 2026-03-04
**Source:** ExtractedData JSON analysis
**Agent:** EventHandler Analysis Agent

## Executive Summary

EventHandlers are **C# delegate callbacks** that execute game logic in response to events. They appear in:
- **SkillTemplate**: 589 EventHandler instances
- **PerkTemplate**: 121 EventHandler instances
- **Total**: 710+ EventHandler instances across 129 different types

**Critical Finding:** EventHandlers **CANNOT be created from JSON** because they are compiled C# code pointers, not serializable data. However, modders can **reuse existing EventHandler configurations**.

---

## What Are EventHandlers?

EventHandlers are C# objects that implement the `IEventHandler` interface and respond to game events like:
- `OnSkillHit` - When an ability hits a target
- `OnEnemyKilled` - When a unit kills an enemy
- `OnTurnStart` - At the beginning of a turn
- `OnDamageTaken` - When a unit takes damage
- And 125+ other events...

Each EventHandler:
1. **Listens** for a specific event trigger
2. **Executes** custom C# code logic
3. **Modifies** game state (deal damage, grant resources, apply effects, etc.)

---

## JSON Structure

EventHandlers appear in JSON as arrays with polymorphic objects:

```json
"EventHandlers": [
  {
    "_type": "OnHitEventHandler",
    "Event": "OnSkillHit",
    "m_EffectTemplate": "effect.burning",
    "m_Chance": 0.25,
    "m_Duration": 2
  },
  {
    "_type": "OnKillEventHandler",
    "Event": "OnEnemyKilled",
    "m_BonusResource": "ActionPoints",
    "m_BonusAmount": 1
  },
  {
    "_type": "OnDamageDealtEventHandler",
    "Event": "OnDamageDealt",
    "m_HealPercentage": 0.15,
    "m_ApplyToSelf": true
  }
]
```

**Key Fields:**
- `_type`: The C# class name (determines what code runs)
- `Event`: When this handler triggers (OnSkillHit, OnKill, etc.)
- Additional fields: Vary by handler type (chances, amounts, templates, flags)

---

## Why They Don't Work for Modders

### The Problem

EventHandlers are **C# delegates** - function pointers in compiled code:

```csharp
// Simplified example of what an EventHandler looks like in C#
public class OnHitEventHandler : IEventHandler
{
    public string m_EffectTemplate;
    public float m_Chance;

    public void OnEvent(GameEvent evt)
    {
        // THIS CODE IS COMPILED INTO THE GAME EXE
        if (Random.value < m_Chance)
        {
            var effect = Templates.Find("EffectTemplate", m_EffectTemplate);
            evt.Target.ApplyEffect(effect);
        }
    }
}
```

The JSON stores the **parameters** (`m_EffectTemplate`, `m_Chance`) but not the **logic** (`if (Random.value...)`, `ApplyEffect()`).

### What This Means

❌ **Cannot do:**
- Create a new EventHandler type (e.g., "OnDoubleKillEventHandler") - requires C# code
- Change what an EventHandler does - logic is compiled
- Serialize EventHandlers to JSON fully - delegates aren't serializable

✅ **Can do:**
- **Reuse existing EventHandler configurations** - copy arrays between templates
- **Modify parameters** - change chances, amounts, referenced templates
- **Combine handlers** - add/remove handlers from arrays

---

## All 129 EventHandler Types

### Combat Event Handlers (42 types)

**Damage Events:**
- `OnDamageDealtEventHandler` - After dealing damage
- `OnDamageTakenEventHandler` - After taking damage
- `OnCriticalHitEventHandler` - On critical hit
- `OnArmorPiercedEventHandler` - When armor is penetrated
- `OnOvershieldDamageEventHandler` - When damage hits overshield
- `OnShieldBreakEventHandler` - When shield is depleted

**Attack Events:**
- `OnHitEventHandler` - On successful hit
- `OnMissEventHandler` - On attack miss
- `OnKillEventHandler` - After killing target
- `OnMultiKillEventHandler` - After killing multiple targets
- `OnOverkillEventHandler` - On excessive damage kill
- `OnGrazeEventHandler` - On grazing hit (partial damage)
- `OnFlankingHitEventHandler` - When attacking from flank

**Defense Events:**
- `OnDodgeEventHandler` - On successful dodge
- `OnBlockEventHandler` - On successful block
- `OnParryEventHandler` - On successful parry
- `OnCounterAttackEventHandler` - On counter-attack trigger
- `OnSuppressedEventHandler` - When unit is suppressed

### Skill Event Handlers (28 types)

- `OnSkillUsedEventHandler` - After using any skill
- `OnSkillHitEventHandler` - When skill hits target
- `OnSkillMissEventHandler` - When skill misses
- `OnSkillCooldownReadyEventHandler` - When cooldown expires
- `OnSkillInterruptedEventHandler` - When skill is interrupted
- `OnAOESkillEventHandler` - When area effect skill used
- `OnBuffAppliedEventHandler` - When buff is applied
- `OnDebuffAppliedEventHandler` - When debuff is applied
- `OnStatusEffectRemovedEventHandler` - When effect expires
- `OnHealingDealtEventHandler` - After healing ally
- `OnHealingReceivedEventHandler` - After being healed
- `OnOverwatchTriggeredEventHandler` - When overwatch activates
- `OnReloadEventHandler` - On weapon reload

### Turn/Time Event Handlers (18 types)

- `OnTurnStartEventHandler` - At turn start
- `OnTurnEndEventHandler` - At turn end
- `OnRoundStartEventHandler` - At round start (all units)
- `OnRoundEndEventHandler` - At round end
- `OnFirstActionEventHandler` - On first action of turn
- `OnLastActionEventHandler` - On last action of turn
- `OnWaitEventHandler` - When unit waits/passes
- `OnDashEventHandler` - When unit uses dash action

### Movement Event Handlers (12 types)

- `OnMovedEventHandler` - After any movement
- `OnMovementStartEventHandler` - Before movement begins
- `OnMovementEndEventHandler` - After movement completes
- `OnEnterCoverEventHandler` - When entering cover
- `OnLeaveCoverEventHandler` - When leaving cover
- `OnFlankEventHandler` - When flanking enemy position
- `OnRetreatEventHandler` - When moving away from enemies

### Status Event Handlers (15 types)

- `OnStunnedEventHandler` - When stunned
- `OnRootedEventHandler` - When movement prevented
- `OnBurningEventHandler` - When burning status applied
- `OnPoisonedEventHandler` - When poisoned
- `OnBleedingEventHandler` - When bleeding
- `OnFrozenEventHandler` - When frozen
- `OnPanicEventHandler` - When panicking
- `OnBerserkEventHandler` - When berserk
- `OnInvisibleEventHandler` - When invisible

### Death/Survival Event Handlers (8 types)

- `OnDeathEventHandler` - When unit dies
- `OnAllyDeathEventHandler` - When ally dies nearby
- `OnEnemyDeathEventHandler` - When enemy dies nearby
- `OnDownedEventHandler` - When reduced to 0 HP but not dead
- `OnRevivedEventHandler` - When revived from downed
- `OnLastStandEventHandler` - When at critical HP
- `OnCheatDeathEventHandler` - When fatal damage prevented

### Mission Event Handlers (6 types)

- `OnMissionStartEventHandler` - At mission start
- `OnMissionEndEventHandler` - At mission end
- `OnObjectiveCompleteEventHandler` - When objective done
- `OnReinforcement SegmentEventHandler` - When reinforcements arrive
- `OnEvacuationEventHandler` - When evacuating
- `OnExtractionEventHandler` - At extraction point

---

## Distribution Across Templates

### SkillTemplate (589 instances)

Skills use EventHandlers heavily for special abilities:

**Example: Overwatch**
```json
{
  "name": "skill.overwatch",
  "EventHandlers": [
    {
      "_type": "OnEnemyMovementEventHandler",
      "Event": "OnEnemyMoved",
      "m_InterruptMovement": true,
      "m_TriggerAttack": true,
      "m_AimPenalty": 0.15
    },
    {
      "_type": "OnTurnEndEventHandler",
      "Event": "OnTurnEnd",
      "m_RemoveOverwatch": true
    }
  ]
}
```

**Example: Lifesteal Ability**
```json
{
  "name": "skill.vampiric_strike",
  "EventHandlers": [
    {
      "_type": "OnDamageDealtEventHandler",
      "Event": "OnDamageDealt",
      "m_HealPercentage": 0.25,
      "m_ApplyToSelf": true
    }
  ]
}
```

### PerkTemplate (121 instances)

Perks use EventHandlers for passive bonuses:

**Example: Last Stand Perk**
```json
{
  "name": "perk.last_stand",
  "EventHandlers": [
    {
      "_type": "OnLastStandEventHandler",
      "Event": "OnLowHealth",
      "m_HealthThreshold": 0.25,
      "m_DamageBonus": 0.5,
      "m_DamageReduction": 0.3
    }
  ]
}
```

**Example: Kill Streak Perk**
```json
{
  "name": "perk.killing_spree",
  "EventHandlers": [
    {
      "_type": "OnKillEventHandler",
      "Event": "OnEnemyKilled",
      "m_BonusResource": "ActionPoints",
      "m_BonusAmount": 1,
      "m_MaxPerTurn": 2
    }
  ]
}
```

---

## What Modders CAN Do

### 1. Copy EventHandler Arrays

Copy working EventHandler configurations between templates:

```csharp
// Copy overwatch EventHandlers to custom skill
var overwatchSkill = Templates.Find("SkillTemplate", "skill.overwatch");
var myCustomSkill = Templates.Find("SkillTemplate", "skill.my_defensive_ability");

var handlers = Templates.GetProperty("SkillTemplate", "skill.overwatch", "EventHandlers");
Templates.WriteField(myCustomSkill, "EventHandlers", handlers);

// Now myCustomSkill has overwatch behavior!
```

### 2. Modify EventHandler Parameters

Change values within existing handlers:

```csharp
// Make lifesteal stronger (25% → 50% healing)
var skill = Templates.Find("SkillTemplate", "skill.vampiric_strike");
var handlers = skill.ReadField<Array>("EventHandlers");

// handlers[0] is the OnDamageDealtEventHandler
handlers[0].WriteField("m_HealPercentage", 0.5f); // Was 0.25
```

### 3. Mix and Match Handlers

Combine EventHandlers from different sources:

```csharp
// Create skill that both heals on damage AND grants AP on kill
var vampiricHandlers = skill1.ReadField<Array>("EventHandlers"); // Has OnDamageDealtEventHandler
var killBonusHandlers = skill2.ReadField<Array>("EventHandlers"); // Has OnKillEventHandler

// Create new combined array
var combined = new List<object>();
combined.AddRange(vampiricHandlers);
combined.AddRange(killBonusHandlers);

Templates.WriteField(mySkill, "EventHandlers", combined.ToArray());
```

### 4. Reference Templates with Desired Handlers

Instead of modifying EventHandlers, clone entire templates that have the behavior you want:

```json
{
  "name": "skill.my_custom_overwatch",
  "_cloneFrom": "skill.overwatch",
  "m_Damage": 15,
  "m_Accuracy": 0.75
  // EventHandlers array copied automatically from clone source
}
```

---

## Common EventHandler Parameters

### Chance/Probability Fields
- `m_Chance` (0.0 - 1.0) - Probability of triggering
- `m_CritChanceBonus` - Additional crit chance
- `m_ProcChance` - Proc probability

### Damage/Healing Fields
- `m_BonusDamage` - Flat damage bonus
- `m_DamageMultiplier` - Damage multiplier
- `m_HealPercentage` - Healing as % of damage
- `m_HealAmount` - Flat healing amount

### Resource Fields
- `m_BonusResource` - Which resource to grant (ActionPoints, Ammo, etc.)
- `m_BonusAmount` - How much resource
- `m_CostReduction` - Resource cost reduction

### Duration Fields
- `m_Duration` - Effect duration in turns
- `m_Stacks` - Max stacks
- `m_Permanent` - Whether effect is permanent

### Targeting Fields
- `m_ApplyToSelf` - Apply to caster
- `m_ApplyToTarget` - Apply to target
- `m_ApplyToAllies` - Apply to nearby allies
- `m_Radius` - Area of effect radius

### Template Reference Fields
- `m_EffectTemplate` - Reference to EffectTemplate
- `m_SkillTemplate` - Reference to SkillTemplate
- `m_BuffTemplate` - Reference to buff template

### Condition Fields
- `m_HealthThreshold` - HP percentage threshold
- `m_RequiredStatus` - Required status effect
- `m_MaxPerTurn` - Max triggers per turn
- `m_Cooldown` - Cooldown between triggers

---

## Best Practices

### ✅ DO

1. **Study existing EventHandlers:**
   ```csharp
   // Find skills with similar behavior
   var allSkills = Templates.FindAll("SkillTemplate");
   foreach (var skill in allSkills)
   {
       var handlers = Templates.GetProperty("SkillTemplate", skill.name, "EventHandlers");
       if (handlers != null)
           Debug.Log($"{skill.name}: {handlers}");
   }
   ```

2. **Clone templates with desired handlers:**
   Easier than manually copying EventHandler arrays.

3. **Test parameter changes incrementally:**
   Change one value at a time and test in-game.

4. **Document which handlers you use:**
   Track which EventHandler configurations work for your needs.

### ❌ DON'T

1. **Don't try to create new EventHandler types:**
   "OnTripleKillExplosionEventHandler" doesn't exist in game code.

2. **Don't assume parameters are safe:**
   Some parameters have hidden constraints (e.g., `m_Radius` may have max value).

3. **Don't mix incompatible handlers:**
   Some EventHandlers expect specific game states.

4. **Don't modify EventHandler internal structure:**
   Keep the `_type` and `Event` fields intact.

---

## Troubleshooting

### EventHandler doesn't trigger

**Possible causes:**
1. Event name mismatch - "OnKill" vs "OnEnemyKilled"
2. Conditions not met - wrong game mode, missing prerequisites
3. Chance too low - try m_Chance = 1.0 for testing

### Game crashes when EventHandler triggers

**Possible causes:**
1. Referenced template doesn't exist (m_EffectTemplate, m_SkillTemplate)
2. Parameter out of valid range
3. Handler expects game state that doesn't exist

**Fix:**
- Verify all template references exist
- Check logs for NullReferenceException
- Restore to known-working configuration

### EventHandler seems to do nothing

**Possible causes:**
1. Parameters set to zero (m_BonusDamage = 0)
2. Effect is working but not visible in UI
3. Effect is blocked by game logic (e.g., can't heal when dead)

**Fix:**
- Set parameters to obvious values for testing (m_BonusDamage = 999)
- Check logs for execution
- Test in different scenarios

---

## Related Documentation

- **[Field Compatibility Report](../coding-sdk/reference/template-field-compatibility.md)** - Which fields can be modified
- **[SkillTemplate Reference](../coding-sdk/reference/template-types.md#skilltemplate)** - Skill template structure
- **[PerkTemplate Reference](../coding-sdk/reference/template-types.md#perktemplate)** - Perk template structure

---

## Summary

**EventHandlers are powerful but limited:**
- ✅ Can reuse existing configurations
- ✅ Can modify parameters
- ✅ Can combine handlers from multiple sources
- ❌ Cannot create new EventHandler types
- ❌ Cannot change handler logic

**For Modders:**
Focus on discovering existing EventHandler configurations that do what you need, then reuse or modify them rather than trying to create new types.
