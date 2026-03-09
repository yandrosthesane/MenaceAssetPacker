# Combat Intercepts - Modifying Damage and Combat Mechanics

This guide covers using the `Intercept` system to modify combat mechanics, focusing on the `OnDamageApplied` event that allows complete control over damage application.

## What is OnDamageApplied?

`OnDamageApplied` is a combat intercept hook that fires **before** damage is actually applied to an entity. This allows you to:

- Modify damage values (critical hits, damage reduction)
- Cancel damage entirely (immunity, shields)
- Log damage events (analytics, achievements)
- Implement custom damage mechanics (reflection, DoT tracking)

## Getting Started

### Basic Setup

Create a C# modpack plugin and add the intercept handler in `OnInitialize`:

```csharp
using Menace.SDK;

namespace MyMod
{
    public class MyCombatMod : IModpackPlugin
    {
        public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
        {
            // Subscribe to damage application
            Intercept.OnDamageApplied += HandleDamageApplied;

            logger.Msg("Combat mod initialized!");
        }

        private void HandleDamageApplied(
            GameObj handler,
            GameObj target,
            GameObj attacker,
            GameObj skill,
            ref float damage,
            ref bool cancel)
        {
            if (target.IsNull || damage <= 0) return;

            // Your damage modification logic here
            DevConsole.Log($"Damage: {damage} to {target.GetName()}");
        }

        public void OnUnload()
        {
            // Always unsubscribe when your mod unloads
            Intercept.OnDamageApplied -= HandleDamageApplied;
        }
    }
}
```

## Example 1: Critical Hits

Implement a critical hit system with 10% chance for double damage:

```csharp
private void HandleDamageApplied(
    GameObj handler,
    GameObj target,
    GameObj attacker,
    GameObj skill,
    ref float damage,
    ref bool cancel)
{
    if (target.IsNull || damage <= 0) return;

    // 10% crit chance
    if (Random.value < 0.1f)
    {
        float originalDamage = damage;
        damage *= 2.0f;

        DevConsole.Log($"CRITICAL HIT! {originalDamage} -> {damage} to {target.GetName()}");

        // Optional: Show floating text
        TacticalEventHooks.ShowFloatingText(target.Pointer, "CRIT!", Color.red);
    }
}
```

### Advanced: Weapon-Specific Crit Chances

Different weapons have different crit chances:

```csharp
private void HandleDamageApplied(
    GameObj handler,
    GameObj target,
    GameObj attacker,
    GameObj skill,
    ref float damage,
    ref bool cancel)
{
    if (target.IsNull || skill.IsNull || damage <= 0) return;

    string weaponName = skill.GetName();
    float critChance = 0.05f; // Default 5%
    float critMultiplier = 2.0f; // Default 2x

    // Snipers: High crit chance, high multiplier
    if (weaponName.Contains("Sniper"))
    {
        critChance = 0.20f;
        critMultiplier = 2.5f;
    }
    // Shotguns: Low crit chance
    else if (weaponName.Contains("Shotgun"))
    {
        critChance = 0.03f;
        critMultiplier = 1.5f;
    }
    // Melee: Very high crit chance
    else if (weaponName.Contains("Melee") || weaponName.Contains("Knife"))
    {
        critChance = 0.25f;
        critMultiplier = 3.0f;
    }

    if (Random.value < critChance)
    {
        damage *= critMultiplier;
        DevConsole.Log($"{weaponName} CRITICAL! {critMultiplier}x damage");
    }
}
```

## Example 2: Boss Immunity Shield

Grant bosses a damage shield that must be broken first:

```csharp
// Track boss shield HP
private static Dictionary<IntPtr, float> bossShields = new Dictionary<IntPtr, float>();

private void HandleDamageApplied(
    GameObj handler,
    GameObj target,
    GameObj attacker,
    GameObj skill,
    ref float damage,
    ref bool cancel)
{
    if (target.IsNull) return;

    string targetName = target.GetName();

    // Boss units have shields
    if (targetName.Contains("Boss"))
    {
        IntPtr targetPtr = target.Pointer;

        // Initialize shield if not present
        if (!bossShields.ContainsKey(targetPtr))
        {
            bossShields[targetPtr] = 500f; // 500 HP shield
            DevConsole.Log($"{targetName} shield activated: 500 HP");
        }

        float shieldHP = bossShields[targetPtr];

        if (shieldHP > 0)
        {
            // Damage goes to shield first
            float shieldDamage = Math.Min(damage, shieldHP);
            bossShields[targetPtr] -= shieldDamage;

            float remainingDamage = damage - shieldDamage;

            if (remainingDamage <= 0)
            {
                // Shield absorbed all damage
                cancel = true;
                DevConsole.Log($"{targetName} shield absorbed {shieldDamage} damage! Remaining: {bossShields[targetPtr]}");
            }
            else
            {
                // Shield broken - remaining damage goes through
                damage = remainingDamage;
                DevConsole.Log($"{targetName} shield BROKEN! {remainingDamage} damage goes through");
                bossShields.Remove(targetPtr);
            }
        }
    }
}
```

## Example 3: Flanking Damage Bonus

Deal extra damage when attacking from behind:

```csharp
private void HandleDamageApplied(
    GameObj handler,
    GameObj target,
    GameObj attacker,
    GameObj skill,
    ref float damage,
    ref bool cancel)
{
    if (target.IsNull || attacker.IsNull || damage <= 0) return;

    // Check if attacker is behind target
    int targetFacing = target.ReadField<int>(0x168); // Actor facing direction
    int attackDirection = GetAttackDirection(attacker, target);

    // Calculate angle difference
    int angleDiff = Math.Abs(targetFacing - attackDirection);
    if (angleDiff > 4) angleDiff = 8 - angleDiff; // Wrap around

    // Behind = 4, Side = 2-3, Front = 0-1
    if (angleDiff == 4)
    {
        // Flanked from behind - 50% bonus damage
        damage *= 1.5f;
        DevConsole.Log($"{target.GetName()} FLANKED from behind! +50% damage");
    }
    else if (angleDiff >= 2 && angleDiff <= 3)
    {
        // Side attack - 25% bonus damage
        damage *= 1.25f;
        DevConsole.Log($"{target.GetName()} flanked from side! +25% damage");
    }
}

private int GetAttackDirection(GameObj attacker, GameObj target)
{
    // Get tile positions
    var attackerTile = attacker.CallMethod<IntPtr>("GetTile");
    var targetTile = target.CallMethod<IntPtr>("GetTile");

    if (attackerTile == IntPtr.Zero || targetTile == IntPtr.Zero)
        return 0;

    var attackerTileObj = new GameObj(attackerTile);
    var targetTileObj = new GameObj(targetTile);

    int ax = attackerTileObj.ReadField<int>(0x10); // Tile X
    int ay = attackerTileObj.ReadField<int>(0x14); // Tile Y
    int tx = targetTileObj.ReadField<int>(0x10);
    int ty = targetTileObj.ReadField<int>(0x14);

    // Calculate direction (0-7)
    float angle = Mathf.Atan2(ay - ty, ax - tx) * Mathf.Rad2Deg;
    int direction = (int)((angle + 202.5f) / 45f) % 8;

    return direction;
}
```

## Example 4: Damage Reflection

Reflect a portion of damage back to the attacker:

```csharp
private void HandleDamageApplied(
    GameObj handler,
    GameObj target,
    GameObj attacker,
    GameObj skill,
    ref float damage,
    ref bool cancel)
{
    if (target.IsNull || attacker.IsNull || damage <= 0) return;

    string targetName = target.GetName();

    // Units with "Thorns" trait reflect 30% of damage
    if (targetName.Contains("Thorns") || targetName.Contains("Spiky"))
    {
        float reflectedDamage = damage * 0.3f;

        // Read attacker's current HP
        float attackerHP = attacker.ReadField<float>(0x54); // Entity+0x54 = current HP

        // Apply reflected damage
        float newHP = attackerHP - reflectedDamage;
        attacker.WriteField(0x54, newHP);

        DevConsole.Log($"{targetName} reflected {reflectedDamage} damage back to {attacker.GetName()}!");

        // Optional: Show visual feedback
        TacticalEventHooks.ShowFloatingText(attacker.Pointer, $"-{reflectedDamage}", Color.yellow);
    }
}
```

## Example 5: Achievement Tracking

Track total damage dealt for achievements:

```csharp
private static Dictionary<string, float> damageDealt = new Dictionary<string, float>();
private static bool destroyerAchievementUnlocked = false;

private void HandleDamageApplied(
    GameObj handler,
    GameObj target,
    GameObj attacker,
    GameObj skill,
    ref float damage,
    ref bool cancel)
{
    if (cancel || attacker.IsNull || damage <= 0) return;

    string attackerName = attacker.GetName();

    // Track damage dealt
    if (!damageDealt.ContainsKey(attackerName))
        damageDealt[attackerName] = 0f;

    damageDealt[attackerName] += damage;

    // Check for "Destroyer" achievement (10000 total damage)
    if (!destroyerAchievementUnlocked && damageDealt[attackerName] >= 10000f)
    {
        destroyerAchievementUnlocked = true;
        DevConsole.Log($"ACHIEVEMENT UNLOCKED: Destroyer ({attackerName} dealt 10000+ damage)");

        // Optional: Show UI notification
        TacticalController.ShowNotification("Achievement Unlocked: Destroyer!");
    }

    // Log top damage dealers every 100 damage
    if ((int)damageDealt[attackerName] % 100 == 0)
    {
        var topDamagers = damageDealt.OrderByDescending(kv => kv.Value).Take(3);
        DevConsole.Log("Top Damage Dealers:");
        foreach (var entry in topDamagers)
        {
            DevConsole.Log($"  {entry.Key}: {entry.Value:F1}");
        }
    }
}
```

## Example 6: Range-Based Damage Modification

Modify damage based on distance between attacker and target:

```csharp
private void HandleDamageApplied(
    GameObj handler,
    GameObj target,
    GameObj attacker,
    GameObj skill,
    ref float damage,
    ref bool cancel)
{
    if (target.IsNull || attacker.IsNull || skill.IsNull || damage <= 0) return;

    string weaponName = skill.GetName();
    float distance = GetDistanceBetween(attacker, target);

    // Shotguns: Devastating at close range, weak at long range
    if (weaponName.Contains("Shotgun"))
    {
        if (distance <= 3f)
        {
            damage *= 1.5f; // +50% at close range
            DevConsole.Log($"Shotgun close range! +50% damage");
        }
        else if (distance >= 8f)
        {
            damage *= 0.5f; // -50% at long range
            DevConsole.Log($"Shotgun long range! -50% damage");
        }
    }
    // Sniper rifles: Best at medium-long range
    else if (weaponName.Contains("Sniper"))
    {
        if (distance < 5f)
        {
            damage *= 0.7f; // -30% too close
            DevConsole.Log($"Sniper too close! -30% damage");
        }
        else if (distance >= 10f && distance <= 20f)
        {
            damage *= 1.3f; // +30% optimal range
            DevConsole.Log($"Sniper optimal range! +30% damage");
        }
    }
    // Grenades/Explosives: Bonus at close-medium range
    else if (weaponName.Contains("Grenade") || weaponName.Contains("Explosive"))
    {
        if (distance <= 5f)
        {
            damage *= 1.2f; // +20% in enclosed spaces
            DevConsole.Log($"Explosive close quarters! +20% damage");
        }
    }
}

private float GetDistanceBetween(GameObj obj1, GameObj obj2)
{
    // Get tile positions
    var tile1Ptr = obj1.CallMethod<IntPtr>("GetTile");
    var tile2Ptr = obj2.CallMethod<IntPtr>("GetTile");

    if (tile1Ptr == IntPtr.Zero || tile2Ptr == IntPtr.Zero)
        return 0f;

    var tile1 = new GameObj(tile1Ptr);
    var tile2 = new GameObj(tile2Ptr);

    int x1 = tile1.ReadField<int>(0x10);
    int y1 = tile1.ReadField<int>(0x14);
    int x2 = tile2.ReadField<int>(0x10);
    int y2 = tile2.ReadField<int>(0x14);

    return Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
}
```

## Best Practices

### 1. Always Null-Check

```csharp
if (target.IsNull || attacker.IsNull) return;
```

### 2. Check for Zero Damage

```csharp
if (damage <= 0) return;
```

### 3. Preserve Original Values

```csharp
float originalDamage = damage;
// ... modifications ...
DevConsole.Log($"Damage: {originalDamage} -> {damage}");
```

### 4. Use Cancel Sparingly

Canceling damage can break game balance. Use it for well-defined mechanics:
- Boss immunity shields
- Invulnerability phases
- Tutorial missions

### 5. Log for Debugging

```csharp
DevConsole.Log($"Damage modified: {damage} to {target.GetName()}");
```

### 6. Clean Up Resources

Always unsubscribe in `OnUnload()`:

```csharp
public void OnUnload()
{
    Intercept.OnDamageApplied -= HandleDamageApplied;
}
```

## Integration with Other Systems

### Combine with TacticalEventHooks

```csharp
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    // Check if target will die
    float currentHP = target.ReadField<float>(0x54);

    if (damage >= currentHP)
    {
        // About to kill - trigger event
        DevConsole.Log($"{target.GetName()} will die from this hit!");

        // Maybe prevent death and leave them at 1 HP
        if (target.GetName().Contains("Hero"))
        {
            damage = currentHP - 1f;
            DevConsole.Log("Hero saved at 1 HP!");
        }
    }
};
```

### Combine with Template System

```csharp
Intercept.OnDamageApplied += (handler, target, attacker, skill, ref damage, ref cancel) =>
{
    if (target.IsNull) return;

    // Get entity properties to check template
    var propsPtr = target.CallMethod<IntPtr>("GetEntityProperties");
    if (propsPtr == IntPtr.Zero) return;

    var props = new GameObj(propsPtr);
    string templateName = props.GetName();

    // Template-based damage resistance
    if (templateName.Contains("HeavyArmor"))
    {
        damage *= 0.8f; // 20% damage reduction
    }
    else if (templateName.Contains("LightArmor"))
    {
        damage *= 1.2f; // 20% more damage taken
    }
};
```

## Troubleshooting

### Damage Not Modifying

1. Check if event is actually firing:
   ```csharp
   DevConsole.Log("OnDamageApplied fired!");
   ```

2. Verify subscription in Initialize:
   ```csharp
   Intercept.OnDamageApplied += HandleDamageApplied;
   DevConsole.Log("Subscribed to OnDamageApplied");
   ```

3. Check if handler is returning early:
   ```csharp
   DevConsole.Log($"Damage before: {damage}");
   // ... modifications ...
   DevConsole.Log($"Damage after: {damage}");
   ```

### Performance Issues

If the intercept is causing lag:

1. Minimize calculations per event
2. Cache frequently accessed data
3. Use early returns for irrelevant events
4. Profile with DevConsole.Log to find bottlenecks

## Further Reading

- [Intercept API Reference](../coding-sdk/api/intercept.md) - Complete API documentation
- [TacticalEventHooks](../coding-sdk/api/tactical-event-hooks.md) - Entity lifecycle events
- [GameObj](../coding-sdk/api/game-obj.md) - Memory access and field reading
- [Template Modding](08-template-modding.md) - Modifying base unit stats

## Summary

`OnDamageApplied` is the most powerful combat intercept hook in the Menace SDK. It gives you complete control over:

- Damage values (critical hits, reductions, bonuses)
- Damage cancellation (immunity, shields)
- Damage tracking (analytics, achievements)
- Custom combat mechanics (reflection, flanking, range modifiers)

Use it wisely to create compelling combat modifications!
