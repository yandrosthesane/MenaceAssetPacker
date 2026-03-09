# AI System

## Overview

The AI system manages tactical decision-making for non-player factions. It uses a behavior scoring system where each Agent evaluates multiple Behavior options and selects the highest-scored action. The TacticalManager coordinates AI execution across factions with pause/resume control.

Based on reverse engineering findings from Phase 1 implementation (EntityAI.cs).

**CRITICAL:** AI evaluation runs in parallel (multi-threaded). Most write operations are ONLY safe during turn events or when AI is paused.

## Architecture

```
TacticalManager (singleton)
    ├── m_IsAIPaused @ +0xB9
    └── Manages turn order and AI execution

Actor (tactical unit)
    └── m_Agent @ +0x18 → Agent

Agent (AI controller)
    ├── m_Actor @ +0x18 → Actor
    ├── m_Behaviors @ +0x20 → List<Behavior>
    ├── m_ActiveBehavior @ +0x28 → Behavior
    └── m_State @ +0x3C → AgentState

Behavior (AI action)
    └── Score @ +0x18 (float, higher = more likely)
```

## Memory Layout

### TacticalManager Offsets

| Offset | Type | Field Name | Description | Status |
|--------|------|------------|-------------|--------|
| 0xB9 | byte | m_IsAIPaused | AI evaluation paused globally | ✅ Verified |

### Actor AI Offsets

| Offset | Type | Field Name | Description | Status |
|--------|------|------------|-------------|--------|
| 0x18 | IntPtr | m_Agent | Pointer to AI Agent (if AI-controlled) | ✅ Verified |
| 0x160 | float | m_Morale | Morale value (controls flee/aggressive behavior) | ✅ Verified |

### Agent Offsets

| Offset | Type | Field Name | Description | Status |
|--------|------|------------|-------------|--------|
| 0x18 | IntPtr | m_Actor | Back-reference to owning Actor | ✅ Verified |
| 0x20 | List\<Behavior\> | m_Behaviors | List of available behaviors | ✅ Verified |
| 0x28 | IntPtr | m_ActiveBehavior | Currently executing behavior | ✅ Verified |
| 0x3C | int32 | m_State | Agent state enum | ✅ Verified |

### Behavior Offsets

| Offset | Type | Field Name | Description | Status |
|--------|------|------------|-------------|--------|
| 0x18 | float | Score | Behavior priority score (higher = preferred) | ✅ Verified |

## AI Pause System

### TacticalManager.m_IsAIPaused

The global AI pause flag at TacticalManager+0xB9:

```c
// Read pause state
byte isPaused = *(byte*)(tacticalManager + 0xB9);

// Pause AI
*(byte*)(tacticalManager + 0xB9) = 1;

// Resume AI
*(byte*)(tacticalManager + 0xB9) = 0;
```

### Methods

| Address | Signature | Description |
|---------|-----------|-------------|
| N/A | void SetAIPaused(bool paused) | Set AI pause state |
| N/A | bool IsAIPaused() | Query AI pause state |

**Effects of Pausing:**
- All AI faction turns halt
- Behavior evaluation stops mid-calculation
- Units remain frozen until resume
- Safe point for AI state manipulation

**Use Cases:**
- Debugging AI decisions
- Cutscenes and scripted events
- Safe modification of behavior scores
- Preventing AI actions during dialog

## Agent System

### Agent Structure

Each AI-controlled Actor has an Agent at Actor+0x18:

```c
// Get agent from actor
IntPtr agentPtr = *(IntPtr*)(actor + 0x18);
if (agentPtr == 0) {
    // Actor is not AI-controlled (player unit)
}

// Access agent fields
IntPtr actorBackRef = *(IntPtr*)(agent + 0x18);
IntPtr behaviorsListPtr = *(IntPtr*)(agent + 0x20);
IntPtr activeBehavior = *(IntPtr*)(agent + 0x28);
int32 state = *(int32*)(agent + 0x3C);
```

### Agent State

The m_State field at Agent+0x3C tracks AI processing state:

| State Value | Name | Description |
|-------------|------|-------------|
| 0 | Idle | No action being processed |
| 1 | Evaluating | Scoring behaviors |
| 2 | Executing | Performing selected action |
| 3 | Waiting | Waiting for turn |

**Note:** State enum values estimated. Verify via Ghidra decompilation.

## Behavior System

### Behavior Scoring

AI decision-making works by scoring all available behaviors and selecting the highest:

```
For each turn:
1. Agent.EvaluateBehaviors()
   ├── For each behavior in m_Behaviors:
   │   ├── Calculate score based on:
   │   │   ├── Tactical situation
   │   │   ├── Target priority
   │   │   ├── Risk assessment
   │   │   └── Morale state
   │   └── Store in behavior.Score @ +0x18
   │
2. SelectBestBehavior()
   └── behavior = m_Behaviors.Max(b => b.Score)

3. ExecuteBehavior(behavior)
   └── Set m_ActiveBehavior, perform action
```

### Behavior Types

Common behavior class names (from decompilation):

| Behavior Type | Purpose | Typical Score Range |
|---------------|---------|---------------------|
| AttackBehavior | Shoot at enemy | 100-500 |
| MoveBehavior | Move to position | 50-200 |
| SkillBehavior | Use ability/skill | 75-300 |
| ReloadBehavior | Reload weapon | 150-250 |
| WaitBehavior | Overwatch/wait | 25-100 |
| HealBehavior | Use medkit | 200-400 |
| FleeBehavior | Retreat from combat | 0-1000 (morale dependent) |

**Score Interpretation:**
- Base scores calculated by game logic
- Higher score = more likely to be selected
- Modifiers applied for morale, suppression, distance

### Manipulating Behavior Scores

To force a specific action, boost the target behavior's score:

```c
// Find behavior by type name
List<Behavior> behaviors = GetBehaviors(agent);
for (int i = 0; i < behaviors.Count; i++) {
    Behavior b = behaviors[i];
    string typeName = GetTypeName(b);

    if (typeName.Contains("AttackBehavior")) {
        // Read current score
        float currentScore = *(float*)(b + 0x18);

        // Boost to ensure selection
        *(float*)(b + 0x18) = currentScore + 10000.0f;
    }
}
```

**Warning:** Behavior score manipulation is NOT thread-safe. Only modify scores when:
1. AI is paused via `m_IsAIPaused`
2. During `OnTurnStart`/`OnTurnEnd` events
3. When `AI.IsAnyFactionThinking()` returns false

## Morale-Based AI Control

### Morale System

Since the game lacks a direct per-target threat system, morale at Actor+0x160 acts as a behavioral proxy:

```c
// Read morale
float morale = *(float*)(actor + 0x160);

// Set morale (convert float to int32 bits for writing)
union { float f; int32 i; } converter;
converter.f = 50.0f;
*(int32*)(actor + 0x160) = converter.i;
```

### Morale Thresholds

| Morale Value | AI Behavior State | Effects |
|--------------|-------------------|---------|
| 0.0 | Panicked | FleeBehavior score +1000, may switch faction |
| 1.0 - 24.9 | Shaken | Defensive posture, reduced attack scores |
| 25.0 - 49.9 | Cautious | Normal behavior with slight defensive bias |
| 50.0 - 74.9 | Steady | Default behavior |
| 75.0 - 100.0 | Confident | Aggressive, attack scores boosted |
| 100.0 | Fearless | Immune to panic, never flees |

### Morale Constants

```c
const float MORALE_PANICKED = 0.0f;
const float MORALE_SHAKEN = 25.0f;
const float MORALE_STEADY = 50.0f;
const float MORALE_CONFIDENT = 75.0f;
const float MORALE_FEARLESS = 100.0f;
```

### Using Morale as Threat Proxy

```c
// Force defensive/flee behavior (high perceived threat)
SetMorale(actor, MORALE_PANICKED);  // Will flee next turn

// Force aggressive behavior (low perceived threat)
SetMorale(actor, MORALE_FEARLESS);  // Will never flee, aggressive

// Inverse relationship: High threat → Low morale
float threat = 80.0f;  // 0-100 scale
float morale = 100.0f - threat;  // = 20.0 (shaken, defensive)
SetMorale(actor, morale);
```

## Methods

### TacticalManager Methods

| Address | Signature | Description |
|---------|-----------|-------------|
| N/A | void SetAIPaused(bool paused) | Pause/resume all AI evaluation |
| N/A | bool IsAIPaused() | Check if AI is paused |
| N/A | TacticalManager Get() or Instance | Get singleton instance |

### Agent Methods

| Address | Signature | Description |
|---------|-----------|-------------|
| N/A | void EvaluateBehaviors() | Score all available behaviors |
| N/A | Behavior SelectBestBehavior() | Choose highest-scored behavior |
| N/A | void ExecuteBehavior(Behavior b) | Perform selected action |

### Morale Methods (Actor)

| Address | Signature | Description |
|---------|-----------|-------------|
| 0x1805dd240 | void ApplyMorale(MoraleEventType type, float amount) | Apply morale change |
| 0x1805e6d90 | void SetMorale(float value) | Set morale directly |
| 0x1805df4a0 | float GetMoralePct() | Get morale as percentage |
| 0x1805df4d0 | MoraleState GetMoraleState() | Get morale state enum |
| 0x1805df330 | float GetMoraleMax() | Get max morale value |

## SDK Implementation

The EntityAI.cs SDK module provides safe AI manipulation:

### Pause Control

```csharp
// Pause all AI
var result = EntityAI.PauseAI(anyActor);
if (result.Success) {
    // AI is now paused, safe to manipulate state
}

// Resume AI
EntityAI.ResumeAI(anyActor);

// Check pause state
bool paused = EntityAI.IsAIPaused(anyActor);
```

### Behavior Forcing

```csharp
// Force actor to attack specific target next turn
EntityAI.ForceNextAction(enemy, "AttackBehavior", target: player, scoreBoost: 10000);

// Force movement (no specific target)
EntityAI.ForceNextAction(enemy, "MoveBehavior", scoreBoost: 5000);

// Force skill use
EntityAI.ForceNextAction(enemy, "SkillBehavior", scoreBoost: 8000);
```

### Morale Manipulation

```csharp
// Force flee decision
EntityAI.ForceFleeDecision(enemy);  // Sets morale to 0.0

// Block flee decision
EntityAI.BlockFleeDecision(boss);  // Sets morale to 100.0

// Set threat perception (via morale proxy)
EntityAI.SetThreatValueOverride(enemy, target: player, threat: 80.0f);

// Clear threat overrides
EntityAI.ClearThreatOverrides(enemy);
```

## Thread Safety

**CRITICAL:** AI evaluation runs in parallel across multiple threads. Modifying AI state during evaluation causes race conditions and crashes.

### Safe Modification Points

**✅ SAFE:**
1. During `TacticalEventHooks.OnTurnStart`
2. During `TacticalEventHooks.OnTurnEnd`
3. When `AI.IsAnyFactionThinking()` returns false
4. When AI is paused via `m_IsAIPaused`
5. When game is paused (Unity Time.timeScale = 0)

**❌ UNSAFE:**
- During AI faction turns (while thinking)
- During behavior evaluation
- During action execution
- Randomly during gameplay without checks

### Safe Modification Pattern

```csharp
TacticalEventHooks.OnTurnStart += (actorPtr) => {
    // SAFE: AI not evaluating during turn start
    var actor = new GameObj(actorPtr);

    if (ShouldForceAction(actor)) {
        EntityAI.ForceNextAction(actor, "AttackBehavior", scoreBoost: 10000);
    }
};
```

### Unsafe Modification (DO NOT DO)

```csharp
// ❌ UNSAFE: Random point during gameplay
void OnButtonClick() {
    // AI may be thinking right now - RACE CONDITION!
    EntityAI.ForceNextAction(enemy, "AttackBehavior");
}
```

## Notes

### Verification Status

All offsets verified through:
1. Ghidra decompilation of AI classes
2. Runtime testing in EntityAI.cs
3. Successful behavior manipulation in tactical combat

**Version:** Verified for game version 34.0.1 (March 2026)

### Behavior Score Guidelines

When forcing behaviors via score boosting:

- **Boost Amount:** 10,000 is generally sufficient to override normal scoring
- **Relative Boosting:** Add to current score rather than setting absolute value
- **Multiple Behaviors:** Boost all matching behaviors (e.g., all AttackBehaviors with different targets)
- **Score Persistence:** Boosted scores persist until next evaluation cycle (usually next turn)

### Limitations

**No Per-Target Threat System:**
The game does not have a per-target threat/aggro system. Morale is a global per-actor value affecting overall behavior, not specific target priority.

**Workarounds:**
1. Use behavior score boosting to force specific targets
2. Manipulate morale to influence aggressive/defensive stance
3. Modify detection mask to hide/reveal specific actors

**No Direct Behavior Creation:**
Cannot dynamically create new Behavior instances. Can only manipulate existing behaviors in the Agent's m_Behaviors list.

### Future Research

1. Document complete AgentState enum values
2. Map behavior scoring algorithm (weight factors, distance calculations)
3. Research behavior target selection (how AI picks targets)
4. Document AI personality system (if present)
5. Investigate formation AI (squad movement coordination)
6. Map overwatch/reaction fire AI logic
7. Document flee target selection (where AI flees to)
8. Research AI cheating systems (difficulty-based bonuses)

## See Also

- [actor-state-fields.md](actor-state-fields.md) - Morale field details
- [actor-system.md](actor-system.md) - Actor class structure
- [suppression-morale.md](suppression-morale.md) - Morale mechanics and panic
- [ai-decisions.md](ai-decisions.md) - High-level AI decision tree documentation
