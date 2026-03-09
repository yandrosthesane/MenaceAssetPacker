# Entity & Element Lifecycle

Analysis of entity creation, element spawning, and death handling.

## Key Methods Found

### Entity Creation

**Entity$$Create** @ 18060ffc0
- Main factory method for creating entities
- Creates EntitySegments list
- Loops to create all elements via `CreateElement`
- Initializes hitpoints, tags, morale
- Returns fully constructed entity

**Flow:**
```
1. Create Entity GameObject
2. Initialize EntitySegments (List<EntitySegment> at +0x18)
3. Calculate element count from template
4. Loop: CreateElement() for each squad member
5. Add tags from template + items + skills
6. Calculate hitpoints (maxElements * hpPerElement * hpMult)
7. UpdateHitpoints()
```

### Element Spawning

**Entity$$CreateElement** @ 18060fb10
- Creates a single element (squad member, vehicle component)
- **KEY FINDING**: This is where elements are added to Entity.Elements list

**Signature:**
```csharp
Element CreateElement(
    GameObject prefab,        // param_2
    int squaddieId,           // param_3 - used to look up armor/visuals
    int hitpoints,            // param_4
    Vector2 scaleRange        // param_5
)
```

**Flow:**
```
1. Get EntityProperties template
2. If has squaddie data:
   - Determine gender from squaddie
   - Determine armor prefab from squaddie rank
   - Stitch head to body mesh
3. Instantiate prefab GameObject
4. AddComponent<Element>() to GameObject
5. Call Element.Create() to initialize
6. ADD TO LIST: param_1[4] (Entity.Elements at offset 0x20)
   - Uses List<Element>.Add() method
   - This is THE moment an element is "spawned"
7. Return element pointer
```

**Critical Code at offset 18060fdb0:**
```c
lVar11 = Method_System_Collections_Generic_List<Element>_Add__;
lVar8 = param_1[4];  // Entity.Elements list
*(int *)(lVar8 + 0x1c) = *(int *)(lVar8 + 0x1c) + 1;
// ... List.Add logic ...
*(longlong **)(lVar12 + 0x20 + (longlong)(int)uVar3 * 8) = plVar15;  // Add element
```

**Entity$$CreateElementFromSquaddie** @ 18060f970
- Wrapper for UnitActor (player squaddies)
- Gets squaddie data from BaseUnitLeader
- Extracts visual prefab from squaddie
- Calls CreateElement with squaddie-specific params

### Death Handling

**Actor$$OnElementDeath** @ 1805e2c50
- Called when an individual element dies
- Handles morale system on element loss

**Flow:**
```
1. Call Entity.OnElementDeath (base implementation)
2. Get EntityProperties
3. Calculate morale damage = (1.0 / elementCount) * DAT_182d8fe54
4. ApplyMorale(self, flag=0x08, damage)
5. If killer is enemy Actor:
   - Calculate morale boost = (1.0 / victimElementCount) * DAT_182d8ff94
   - ApplyMorale(killer, flag=0x10, boost)
```

**Note:** This is called for EACH element death, not just the final one.

**Element$$Die** @ 1805f85a0 (referenced but not decompiled)
- Likely calls OnElementDeath on parent entity
- Handles visual destruction

## Hook Opportunities

### 1. OnElementSpawned
**Hook:** `Entity$$CreateElement` @ 18060fdb0 (right after List.Add)

**Benefit:** Fires for every element added to any entity
- Squad members spawning (4 events for 4-man squad)
- Vehicle components
- Structure sections

**Signature:** `(IntPtr element, IntPtr parentEntity)`

### 2. OnEntityDeath
**Hook:** `Entity$$Die` or `Entity$$OnDeath`

**Benefit:** Fires when ANY entity dies (actors, structures, destructibles)

**Signature:** `(IntPtr entity)`

### 3. OnActorSpawned (Filter)
**Hook:** `OnEntitySpawned` + type check

**Benefit:** Only fires for Actor spawns

### 4. OnStructureDeath (Filter)
**Hook:** `OnEntityDeath` + type check

**Benefit:** Only fires for Structure destruction

## Memory Offsets

**Entity:**
```
+0x10 EntityID (int)
+0x18 Segments (List<EntitySegment>)
+0x20 Elements (List<Element>) ← KEY: Element list
+0x28 ElementCount (int)
+0x48 IsAlive (bool)
+0x50 CurrentHitpoints (int)
+0x58 MaxHitpoints (int)
```

**Element:**
```
+0x20 ModelIndex (int) - set to squaddieId param
+0x30 Entity (Entity*) - parent entity
+0x38 EntityProperties (EntityProperties*)
```

## Implementation Plan

Add to TacticalEventHooks.cs:

```csharp
// New events
public static event Action<IntPtr, IntPtr> OnElementSpawned;  // element, parent
public static event Action<IntPtr> OnEntityDeath;              // entity
public static event Action<IntPtr> OnActorSpawned;             // actor (filtered)
public static event Action<IntPtr> OnStructureDeath;           // structure (filtered)

// Hook Entity.CreateElement at 18060fdb0 (after List.Add)
private static void OnCreateElement_Postfix(object __instance, object result)
{
    var elementPtr = GetPointer(result);
    var entityPtr = GetPointer(__instance);

    OnElementSpawned?.Invoke(elementPtr, entityPtr);

    FireLuaEvent("element_spawned", new Dictionary<string, object>
    {
        ["element"] = GetName(result),
        ["element_ptr"] = elementPtr.ToInt64(),
        ["parent"] = GetName(__instance),
        ["parent_ptr"] = entityPtr.ToInt64()
    });
}

// Hook Entity.Die or Entity.OnDeath
private static void OnEntityDie_Postfix(object __instance)
{
    var entityPtr = GetPointer(__instance);

    OnEntityDeath?.Invoke(entityPtr);

    if (IsStructure(__instance))
    {
        OnStructureDeath?.Invoke(entityPtr);
    }

    FireLuaEvent("entity_death", new Dictionary<string, object>
    {
        ["entity"] = GetName(__instance),
        ["entity_ptr"] = entityPtr.ToInt64(),
        ["is_structure"] = IsStructure(__instance)
    });
}

// Filter existing OnEntitySpawned
private static void OnEntitySpawned_Postfix(object __instance, object entity)
{
    var entityPtr = GetPointer(entity);

    OnEntitySpawned?.Invoke(entityPtr);

    if (IsActor(entity))
    {
        OnActorSpawned?.Invoke(entityPtr);
    }

    // existing Lua event...
}
```

## Testing Scenarios

1. **Squad Spawn**: 4-man marine squad spawns
   - 1x OnEntitySpawned (entity)
   - 1x OnActorSpawned (filtered)
   - 4x OnElementSpawned (each marine)

2. **Building Destruction**: Multi-section building destroyed
   - Nx OnElementDeath (each section)
   - 1x OnEntityDeath (when last section dies)
   - 1x OnStructureDeath (filtered)

3. **Vehicle Spawn**: Tank spawns
   - 1x OnEntitySpawned
   - 1x OnActorSpawned
   - Nx OnElementSpawned (vehicle components)

4. **Squad Wipe**: 4-man squad eliminated
   - 4x OnElementDeath (each marine)
   - 1x OnActorKilled (when last marine dies, element count reaches 0)
   - 1x OnEntityDeath (same timing as OnActorKilled)

This gives us complete entity lifecycle coverage!
