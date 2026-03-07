# PatchSet

`Menace.SDK.PatchSet` -- Fluent builder for applying multiple Harmony patches with validation and scene awareness.

## Overview

PatchSet reduces the boilerplate of Harmony patching from 12-15 lines per patch to a single fluent chain. It provides:

- **Fluent API** -- Chain multiple patches in a readable format
- **Validation** -- Checks types and methods exist before patching
- **Error aggregation** -- Collects all failures and reports them together
- **Scene awareness** -- Optional deferred application until a specific scene loads

## Quick Start

```csharp
using Menace.SDK;

public class MyPlugin : IModpackPlugin
{
    private HarmonyLib.Harmony _harmony;

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        _harmony = harmony;

        // Apply patches immediately
        new PatchSet(_harmony, "MyPlugin")
            .Postfix("ActorComponent", "ApplyDamage", AfterDamage)
            .Postfix("ActorComponent", "ApplyHealing", AfterHealing)
            .Prefix("SkillHandler", "ExecuteSkill", BeforeSkill)
            .Apply();
    }

    private static void AfterDamage(object __instance, int damage)
    {
        DevConsole.Log($"Damage applied: {damage}");
    }

    private static void AfterHealing(object __instance, int amount)
    {
        DevConsole.Log($"Healing applied: {amount}");
    }

    private static bool BeforeSkill(object __instance)
    {
        // Return false to skip the original method
        return true;
    }
}
```

## API Reference

### Constructor

```csharp
public PatchSet(HarmonyLib.Harmony harmony, string modId)
```

Creates a new PatchSet bound to the given Harmony instance. The `modId` is used for error reporting.

### Methods

#### Postfix

```csharp
public PatchSet Postfix(string typeName, string methodName, Delegate handler)
public PatchSet Postfix(GameType type, string methodName, Delegate handler)
```

Queue a postfix patch. The handler runs after the original method.

#### Prefix

```csharp
public PatchSet Prefix(string typeName, string methodName, Delegate handler)
public PatchSet Prefix(GameType type, string methodName, Delegate handler)
```

Queue a prefix patch. Return `false` from the handler to skip the original method.

#### Apply

```csharp
public PatchResult Apply()
```

Apply all queued patches immediately. Returns a result object with success/failure counts.

#### ApplyOnScene

```csharp
public void ApplyOnScene(string sceneName)
```

Defer patch application until the specified scene loads. Useful when target types aren't available at startup.

#### Validate

```csharp
public PatchResult Validate()
```

Check that all types and methods exist without actually applying patches. Useful for dry-run testing.

### PatchResult

```csharp
public class PatchResult
{
    public int Applied { get; }      // Successfully applied patches
    public int Failed { get; }       // Failed patches
    public bool Success { get; }     // True if all patches applied
    public string[] Errors { get; }  // Error messages for failed patches
}
```

## Examples

### Scene-Deferred Patching

```csharp
public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
{
    // These patches will apply when "Tactical" scene loads
    new PatchSet(harmony, "MyMod")
        .Postfix("TacticalActor", "OnTurnStart", OnTurnStart)
        .Postfix("TacticalActor", "OnTurnEnd", OnTurnEnd)
        .ApplyOnScene("Tactical");
}
```

### Validation Before Apply

```csharp
var patches = new PatchSet(harmony, "MyMod")
    .Postfix("ActorComponent", "ApplyDamage", AfterDamage)
    .Postfix("MissingType", "MissingMethod", SomeHandler);

var validation = patches.Validate();
if (!validation.Success)
{
    foreach (var error in validation.Errors)
        _log.Warning(error);
}
else
{
    patches.Apply();
}
```

### Multiple Patches on Same Method

```csharp
new PatchSet(harmony, "MyMod")
    .Prefix("SkillHandler", "ExecuteSkill", LogBefore)
    .Postfix("SkillHandler", "ExecuteSkill", LogAfter)
    .Apply();
```

### Using GameType

```csharp
var actorType = GameType.Find("TacticalActor");
if (!actorType.IsValid) return;

new PatchSet(harmony, "MyMod")
    .Postfix(actorType, "TakeDamage", AfterTakeDamage)
    .Postfix(actorType, "Die", AfterDie)
    .Apply();
```

### Checking Results

```csharp
var result = new PatchSet(harmony, "MyMod")
    .Postfix("ActorComponent", "ApplyDamage", AfterDamage)
    .Postfix("ActorComponent", "ApplyHealing", AfterHealing)
    .Apply();

_log.Msg($"Applied {result.Applied}/{result.Applied + result.Failed} patches");

if (!result.Success)
{
    foreach (var error in result.Errors)
        ModError.Warn("MyMod", error);
}
```

## Comparison: Before and After

### Before (Manual Harmony)

```csharp
// 15 lines per patch
var targetType = GameState.FindManagedType("ActorComponent");
if (targetType == null)
{
    ModError.Warn("MyMod", "ActorComponent not found");
    return;
}

var method = targetType.GetMethod("ApplyDamage",
    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
if (method == null)
{
    ModError.Warn("MyMod", "ApplyDamage not found");
    return;
}

var patch = typeof(MyPlugin).GetMethod(nameof(AfterDamage),
    BindingFlags.Static | BindingFlags.NonPublic);

_harmony.Patch(method, postfix: new HarmonyMethod(patch));
```

### After (PatchSet)

```csharp
// 1 line per patch
new PatchSet(_harmony, "MyMod")
    .Postfix("ActorComponent", "ApplyDamage", AfterDamage)
    .Apply();
```

## Error Handling

PatchSet never throws exceptions. All failures are:
1. Logged to `ModError` with context
2. Included in the `PatchResult.Errors` array
3. Counted in `PatchResult.Failed`

Common failure reasons:
- Type not found in Assembly-CSharp
- Method not found on the resolved type
- Handler signature incompatible with target method
- Harmony internal error during patching

## Best Practices

### 1. Use Descriptive Mod IDs

```csharp
new PatchSet(harmony, "CombatOverhaul")  // Good
new PatchSet(harmony, "mod")              // Bad
```

### 2. Group Related Patches

```csharp
// Combat patches
new PatchSet(harmony, "MyMod")
    .Postfix("ActorComponent", "ApplyDamage", AfterDamage)
    .Postfix("ActorComponent", "ApplyHealing", AfterHealing)
    .Apply();

// UI patches (separate set for clarity)
new PatchSet(harmony, "MyMod")
    .Postfix("UIManager", "ShowPanel", AfterShowPanel)
    .Apply();
```

### 3. Check Results in Development

```csharp
#if DEBUG
var result = patches.Apply();
if (!result.Success)
    throw new Exception($"Patches failed: {string.Join(", ", result.Errors)}");
#else
patches.Apply();
#endif
```

## See Also

- [GamePatch](game-patch.md) -- Simpler single-patch API
- [Intercept](intercept.md) -- Event-based interception without manual patches
- [Patching Guide](../guides/patching-guide.md) -- Comprehensive patching guide
