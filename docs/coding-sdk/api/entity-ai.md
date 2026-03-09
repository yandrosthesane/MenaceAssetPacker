# EntityAI

`Menace.SDK.EntityAI` -- AI control and manipulation module for behavior forcing, AI pause/resume, and morale-based threat control.

## Overview

EntityAI provides direct control over AI decision-making through behavior score manipulation, AI pause/resume, and morale-based threat systems. This module enables custom AI behaviors, difficulty scaling, and tactical control.

**Key Features:**
- Force specific AI actions via behavior score manipulation
- Pause/resume all AI evaluation globally
- Morale-based threat and flee control
- Thread-safe operations for AI evaluation

**THREAD SAFETY WARNING:**
AI evaluation runs in parallel (multi-threaded). Most write methods are ONLY safe to call:
1. During TacticalEventHooks.OnTurnStart/OnTurnEnd
2. When AI.IsAnyFactionThinking() returns false
3. When the game is paused

Calling these during parallel evaluation WILL cause race conditions and crashes.

**Based on reverse engineering (agent a6148de):**
- TacticalManager.m_IsAIPaused at 0xB9 (byte/bool)
- Agent.m_Actor at 0x18, m_ActiveBehavior at 0x28, m_State at 0x3C
- Agent.m_Behaviors list at 0x20 (each behavior has Score at +0x18)
- Actor.m_Morale at 0x160 (float) - controls flee/aggressive states

## Module Path

```csharp
using Menace.SDK;
// Access via: EntityAI.MethodName(...)
```

## Morale Constants

```csharp
public const float MORALE_PANICKED = 0.0f;      // Triggers flee state
public const float MORALE_SHAKEN = 25.0f;       // Low morale
public const float MORALE_STEADY = 50.0f;       // Normal morale
public const float MORALE_CONFIDENT = 75.0f;    // High morale
public const float MORALE_FEARLESS = 100.0f;    // Blocks flee state
```

## Methods

### ForceNextAction

**Signature**: `AIResult ForceNextAction(GameObj actor, string actionType, GameObj target = default, int scoreBoost = 10000)`

**Description**: Force an actor to prioritize a specific action on their next turn by manipulating the Agent.m_Behaviors list. Boosts the score of behaviors matching the specified action type.

**THREAD SAFETY**: Call only during OnTurnStart/OnTurnEnd or when AI is not evaluating.

**Parameters:**
- `actor` (GameObj): The actor whose AI to manipulate
- `actionType` (string): Type of action to prioritize (e.g., "AttackBehavior", "MoveBehavior")
- `target` (GameObj): Optional target actor for targeted actions
- `scoreBoost` (int): Score boost to apply (default: 10000 to ensure selection)

**Returns**: AIResult indicating success or failure

**Example:**
```csharp
var enemy = FindActorByName("Enemy_Sniper");
var player = TacticalController.GetActiveActor();

// Force enemy to attack player
TacticalEventHooks.OnTurnStart += (actorPtr) =>
{
    if (new GameObj(actorPtr).Pointer == enemy.Pointer)
    {
        var result = EntityAI.ForceNextAction(enemy, "AttackBehavior", player);
        if (result.Success)
        {
            DevConsole.Log("Enemy forced to attack player");
        }
    }
};

// Force move to specific location
EntityAI.ForceNextAction(enemy, "MoveBehavior", targetTile, 15000);

// Force reload
EntityAI.ForceNextAction(enemy, "ReloadBehavior");
```

**Common Action Types:**
- `AttackBehavior` - Forces attack actions
- `MoveBehavior` - Forces movement
- `SkillBehavior` - Forces skill/ability use
- `ReloadBehavior` - Forces reload
- `WaitBehavior` - Forces wait/overwatch

**Related:**
- [PauseAI](#pauseai) - Pause AI for safe manipulation
- [SetThreatValueOverride](#setthreatvalueoverride) - Influence targeting

**Notes:**
- Finds behaviors matching actionType by typename
- Boosts Score field at behavior+0x18
- AI selects highest-scored behavior on evaluation
- Thread-unsafe - check AI.IsAnyFactionThinking() first
- Returns error if called during AI evaluation

---

### PauseAI

**Signature**: `AIResult PauseAI(GameObj actor)`

**Description**: Pause all AI evaluation and execution by setting TacticalManager.m_IsAIPaused to true.

**THREAD SAFETY**: Safe to call at any time (pauses parallel evaluation).

**Parameters:**
- `actor` (GameObj): Any actor (used to get TacticalManager instance)

**Returns**: AIResult indicating success or failure

**Example:**
```csharp
var anyActor = TacticalController.GetActiveActor();

// Pause AI for cutscene
if (EntityAI.PauseAI(anyActor).Success)
{
    DevConsole.Log("AI paused for cutscene");
    PlayCutscene();
    EntityAI.ResumeAI(anyActor);
}

// Pause AI for debugging
EntityAI.PauseAI(anyActor);
// Manipulate AI state...
EntityAI.ResumeAI(anyActor);
```

**Related:**
- [ResumeAI](#resumeai) - Resume AI execution
- [IsAIPaused](#isaipaused) - Check pause state

**Notes:**
- Sets TacticalManager.m_IsAIPaused at +0xB9 to true
- Halts all AI faction turns and behavior evaluation
- Units remain frozen until ResumeAI is called
- Safe for debugging and state manipulation

---

### ResumeAI

**Signature**: `AIResult ResumeAI(GameObj actor)`

**Description**: Resume AI evaluation and execution after a pause by setting TacticalManager.m_IsAIPaused to false.

**THREAD SAFETY**: Safe to call at any time.

**Parameters:**
- `actor` (GameObj): Any actor (used to get TacticalManager instance)

**Returns**: AIResult indicating success or failure

**Example:**
```csharp
var anyActor = TacticalController.GetActiveActor();

// Resume after pause
if (EntityAI.ResumeAI(anyActor).Success)
{
    DevConsole.Log("AI resumed");
}
```

**Related:**
- [PauseAI](#pauseai) - Pause AI execution
- [IsAIPaused](#isaipaused) - Check pause state

---

### IsAIPaused

**Signature**: `bool IsAIPaused(GameObj actor)`

**Description**: Check if AI is currently paused by reading TacticalManager.m_IsAIPaused.

**THREAD SAFETY**: Safe to call at any time.

**Parameters:**
- `actor` (GameObj): Any actor (used to get TacticalManager instance)

**Returns**: True if AI is paused, false otherwise

**Example:**
```csharp
var anyActor = TacticalController.GetActiveActor();

if (EntityAI.IsAIPaused(anyActor))
{
    DevConsole.Log("AI is currently paused");
}
else
{
    DevConsole.Log("AI is running");
}

// Safe AI manipulation pattern
if (!EntityAI.IsAIPaused(anyActor))
{
    EntityAI.PauseAI(anyActor);
    // Manipulate AI state...
    EntityAI.ResumeAI(anyActor);
}
```

**Related:**
- [PauseAI](#pauseai) - Pause AI
- [ResumeAI](#resumeai) - Resume AI

---

### SetThreatValueOverride

**Signature**: `AIResult SetThreatValueOverride(GameObj actor, GameObj target, float threat)`

**Description**: Override an actor's threat perception by manipulating morale. Since the game has no direct per-target threat system, this uses morale as a proxy to influence AI decision-making.

**THREAD SAFETY**: Call only during OnTurnStart/OnTurnEnd or when AI is not evaluating.

**Parameters:**
- `actor` (GameObj): The actor whose threat perception to override
- `target` (GameObj): The target actor (currently unused - morale is global per actor)
- `threat` (float): Threat value (0-100, higher = more threatened = lower morale)

**Returns**: AIResult indicating success or failure

**Example:**
```csharp
var enemy = FindActorByName("Enemy_Guard");
var player = TacticalController.GetActiveActor();

// Make enemy defensive (high threat = low morale)
EntityAI.SetThreatValueOverride(enemy, player, 80.0f);
// Enemy morale set to 20.0f (defensive behavior)

// Make enemy aggressive (low threat = high morale)
EntityAI.SetThreatValueOverride(enemy, player, 20.0f);
// Enemy morale set to 80.0f (aggressive behavior)

// Clear threat override
EntityAI.ClearThreatOverrides(enemy);
```

**Threat to Morale Conversion:**
- High threat (75-100) → Low morale (0-25) → Defensive/flee behavior
- Medium threat (25-75) → Medium morale (25-75) → Normal behavior
- Low threat (0-25) → High morale (75-100) → Aggressive behavior

**Related:**
- [ClearThreatOverrides](#clearthreatoverrides) - Reset to default
- [ForceFleeDecision](#forceflee decision) - Direct flee control

**Notes:**
- Writes morale directly to Actor+0x160
- Uses inverse relationship: morale = 100 - threat
- Game has no per-target threat - affects overall behavior
- Thread-unsafe - check AI.IsAnyFactionThinking() first

---

### ClearThreatOverrides

**Signature**: `AIResult ClearThreatOverrides(GameObj actor)`

**Description**: Clear all threat overrides for an actor by resetting morale to default steady state (50.0f).

**THREAD SAFETY**: Call only during OnTurnStart/OnTurnEnd or when AI is not evaluating.

**Parameters:**
- `actor` (GameObj): The actor to clear threat overrides for

**Returns**: AIResult indicating success or failure

**Example:**
```csharp
var enemy = FindActorByName("Enemy_Soldier");

// Clear threat overrides after combat
EntityAI.ClearThreatOverrides(enemy);
DevConsole.Log("Enemy morale reset to steady state");
```

**Related:**
- [SetThreatValueOverride](#setthreatvalueoverride) - Set threat level

**Notes:**
- Sets morale to MORALE_STEADY (50.0f)
- Restores default AI behavior
- Thread-unsafe - check AI.IsAnyFactionThinking() first

---

### ForceFleeDecision

**Signature**: `AIResult ForceFleeDecision(GameObj actor)`

**Description**: Force an actor to make a flee decision by setting morale to panicked state (0.0f). The AI will prioritize flee/retreat behaviors.

**THREAD SAFETY**: Call only during OnTurnStart/OnTurnEnd or when AI is not evaluating.

**Parameters:**
- `actor` (GameObj): The actor to force into flee state

**Returns**: AIResult indicating success or failure

**Example:**
```csharp
var enemy = FindActorByName("Enemy_Scout");

// Force flee when outnumbered
if (GetEnemyCount() < GetPlayerCount() / 2)
{
    EntityAI.ForceFleeDecision(enemy);
    DevConsole.Log("Enemy fleeing - outnumbered");
}

// Force flee on trigger
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    if (target.Pointer == enemy.Pointer && damage > 50.0f)
    {
        EntityAI.ForceFleeDecision(enemy);
        DevConsole.Log("Enemy fleeing after heavy damage");
    }
};
```

**Flee Behavior Effects:**
- Prioritize moving away from enemies
- Avoid engaging in combat
- Seek cover at maximum range
- May attempt to leave map

**Related:**
- [BlockFleeDecision](#blockfleedecision) - Prevent fleeing
- [SetThreatValueOverride](#setthreatvalueoverride) - Adjust threat level

**Notes:**
- Sets morale to MORALE_PANICKED (0.0f)
- More reliable than direct behavior override
- Uses game's native flee mechanism
- Thread-unsafe - check AI.IsAnyFactionThinking() first

---

### BlockFleeDecision

**Signature**: `AIResult BlockFleeDecision(GameObj actor)`

**Description**: Prevent an actor from fleeing by setting morale to fearless state (100.0f). High morale prevents AI from entering panic/flee behaviors.

**THREAD SAFETY**: Call only during OnTurnStart/OnTurnEnd or when AI is not evaluating.

**Parameters:**
- `actor` (GameObj): The actor to prevent from fleeing

**Returns**: AIResult indicating success or failure

**Example:**
```csharp
var boss = FindActorByName("Boss_Commander");

// Boss never flees
EntityAI.BlockFleeDecision(boss);
DevConsole.Log("Boss will never flee");

// Elite units never panic
var eliteUnits = FindActorsByTag("Elite");
foreach (var unit in eliteUnits)
{
    EntityAI.BlockFleeDecision(unit);
}
```

**Fearless Behavior Effects:**
- Never enter panic/flee state
- Maintain aggressive posture even under heavy fire
- Prioritize attack behaviors over retreat
- Ignore morale penalties

**Related:**
- [ForceFleeDecision](#forceflee decision) - Force fleeing
- [SetThreatValueOverride](#setthreatvalueoverride) - Adjust threat level

**Notes:**
- Sets morale to MORALE_FEARLESS (100.0f)
- Blocks all flee behaviors
- Useful for boss units and elite enemies
- Thread-unsafe - check AI.IsAnyFactionThinking() first

---

## AIResult Structure

```csharp
public class AIResult
{
    public bool Success { get; set; }
    public string Error { get; set; }

    public static AIResult Failed(string error);
    public static AIResult Ok();
}
```

**Usage:**
```csharp
var result = EntityAI.ForceNextAction(actor, "AttackBehavior");
if (!result.Success)
{
    DevConsole.Log($"AI manipulation failed: {result.Error}");
}
```

## Complete Example

```csharp
using Menace.SDK;

// AI difficulty system
public class AIDifficultyManager
{
    public enum Difficulty { Easy, Normal, Hard }
    private Difficulty _currentDifficulty = Difficulty.Normal;

    public void SetDifficulty(Difficulty difficulty)
    {
        _currentDifficulty = difficulty;
        ApplyDifficultyToAllEnemies();
    }

    private void ApplyDifficultyToAllEnemies()
    {
        var enemies = FindAllEnemyActors();

        foreach (var enemy in enemies)
        {
            switch (_currentDifficulty)
            {
                case Difficulty.Easy:
                    // Low morale = more defensive
                    EntityAI.SetThreatValueOverride(enemy, null, 60.0f);
                    break;

                case Difficulty.Normal:
                    EntityAI.ClearThreatOverrides(enemy);
                    break;

                case Difficulty.Hard:
                    // High morale = very aggressive
                    EntityAI.BlockFleeDecision(enemy);
                    break;
            }
        }
    }

    // Boss AI behavior
    public void SetupBossAI(GameObj boss)
    {
        // Boss never flees
        EntityAI.BlockFleeDecision(boss);

        // Force aggressive behavior
        EntityAI.SetThreatValueOverride(boss, null, 10.0f);

        DevConsole.Log("Boss AI configured: fearless and aggressive");
    }

    // Strategic AI control
    public void ManipulateTacticalBehavior(GameObj actor, string tactic)
    {
        // Only manipulate during turn start (thread-safe)
        TacticalEventHooks.OnTurnStart += (actorPtr) =>
        {
            if (new GameObj(actorPtr).Pointer != actor.Pointer) return;

            switch (tactic)
            {
                case "Ambush":
                    // Wait in position
                    EntityAI.ForceNextAction(actor, "WaitBehavior");
                    break;

                case "Retreat":
                    // Force flee
                    EntityAI.ForceFleeDecision(actor);
                    break;

                case "Aggressive":
                    // Force attack on player
                    var player = TacticalController.GetActiveActor();
                    EntityAI.ForceNextAction(actor, "AttackBehavior", player);
                    break;

                case "Support":
                    // Force skill use (heal/buff)
                    EntityAI.ForceNextAction(actor, "SkillBehavior");
                    break;
            }
        };
    }

    // Safe AI manipulation helper
    public void SafeAIManipulation(Action manipulationAction)
    {
        var anyActor = TacticalController.GetActiveActor();

        // Check if AI is thinking
        if (AI.IsAnyFactionThinking())
        {
            DevConsole.Warn("Cannot manipulate AI during evaluation");
            return;
        }

        // Pause AI for safety
        if (!EntityAI.PauseAI(anyActor).Success)
        {
            DevConsole.Warn("Failed to pause AI");
            return;
        }

        try
        {
            // Perform manipulation
            manipulationAction();
        }
        finally
        {
            // Always resume AI
            EntityAI.ResumeAI(anyActor);
        }
    }
}

// Combat scenario example
public class CombatScenarioManager
{
    public void SetupAmbush()
    {
        var enemies = FindActorsByTag("Ambushers");

        foreach (var enemy in enemies)
        {
            // Make ambushers aggressive
            EntityAI.SetThreatValueOverride(enemy, null, 20.0f);

            // Force wait until player is in range
            EntityAI.ForceNextAction(enemy, "WaitBehavior");
        }

        // Trigger ambush on player detection
        Intercept.OnEntityStateChange += (entityPtr, state) =>
        {
            var entity = new GameObj(entityPtr);
            if (IsPlayerDetectedByAmbushers())
            {
                foreach (var enemy in enemies)
                {
                    var player = TacticalController.GetActiveActor();
                    EntityAI.ForceNextAction(enemy, "AttackBehavior", player);
                }
            }
        };
    }

    public void CreateFleeing Civilian()
    {
        var civilian = SpawnActor("Civilian");

        // Civilian always flees
        EntityAI.ForceFleeDecision(civilian);

        // Run towards extraction zone
        TacticalEventHooks.OnTurnStart += (actorPtr) =>
        {
            if (new GameObj(actorPtr).Pointer == civilian.Pointer)
            {
                var extractionZone = FindNearestExtractionZone(civilian);
                EntityAI.ForceNextAction(civilian, "MoveBehavior", extractionZone);
            }
        };
    }
}
```

## Thread Safety Guidelines

**Safe Operations (anytime):**
- `PauseAI()`
- `ResumeAI()`
- `IsAIPaused()`

**Unsafe Operations (requires synchronization):**
- `ForceNextAction()` - Modify behavior scores
- `SetThreatValueOverride()` - Write morale
- `ClearThreatOverrides()` - Write morale
- `ForceFleeDecision()` - Write morale
- `BlockFleeDecision()` - Write morale

**Safe Timing Windows:**
1. `TacticalEventHooks.OnTurnStart` - Before AI evaluation
2. `TacticalEventHooks.OnTurnEnd` - After AI evaluation
3. When `AI.IsAnyFactionThinking()` returns false
4. After `PauseAI()` succeeds

**Example Safe Pattern:**
```csharp
TacticalEventHooks.OnTurnStart += (actorPtr) =>
{
    // Safe - called before AI evaluation
    var actor = new GameObj(actorPtr);
    EntityAI.ForceNextAction(actor, "AttackBehavior");
};
```

## See Also

- [EntityState](entity-state.md) - Actor state flags and visibility
- [EntitySkills](entity-skills.md) - Skill manipulation
- [Intercept](intercept.md) - AI behavior interceptors (OnAIEvaluate, OnPositionScore)
- [TacticalEventHooks](tactical-event-hooks.md) - Turn start/end hooks
- [AI](ai.md) - AI faction queries
