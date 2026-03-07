# PointerCache

`Menace.SDK.PointerCache` -- Thread-safe IL2CPP IntPtr caching and lookup with null-safe access patterns.

## Overview

PointerCache simplifies working with IL2CPP object pointers across frames. Instead of tracking raw IntPtrs and checking for invalidation manually, PointerCache provides:

- **Automatic null checking** -- Safe access without explicit null guards
- **Optional expiration** -- Pointers can auto-invalidate after N frames
- **Thread-safe operations** -- Safe for use in async callbacks
- **Lookup helpers** -- Find cached objects by name or predicate

## Quick Start

```csharp
using Menace.SDK;

public class MyPlugin : IModpackPlugin
{
    private PointerCache<string> _actorCache;

    public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
    {
        // Create cache with string keys
        _actorCache = new PointerCache<string>();

        // Subscribe to tactical ready event
        GameState.TacticalReady += OnTacticalReady;
    }

    private void OnTacticalReady()
    {
        // Cache all actors by name
        var actors = GameQuery.FindAll("TacticalActor");
        foreach (var actor in actors)
        {
            string name = actor.GetName();
            _actorCache.Set(name, actor.Pointer);
        }
    }

    public void DoSomethingWithActor(string name)
    {
        // Safe access - returns IntPtr.Zero if not found or invalid
        IntPtr ptr = _actorCache.Get(name);
        if (ptr == IntPtr.Zero) return;

        var actor = new GameObj(ptr);
        // Use actor...
    }
}
```

## API Reference

### Constructor

```csharp
public PointerCache<TKey>()
public PointerCache<TKey>(int defaultExpirationFrames)
```

Creates a cache with optional default expiration. Keys can be any type (string, int, etc.).

### Core Methods

#### Set

```csharp
public void Set(TKey key, IntPtr pointer)
public void Set(TKey key, IntPtr pointer, int expirationFrames)
```

Store a pointer with optional expiration. If `expirationFrames` is set, the entry becomes invalid after that many frames.

#### Get

```csharp
public IntPtr Get(TKey key)
```

Retrieve a pointer by key. Returns `IntPtr.Zero` if:
- Key not found
- Pointer was null
- Entry has expired
- Underlying object is no longer alive

#### TryGet

```csharp
public bool TryGet(TKey key, out IntPtr pointer)
```

Safe retrieval pattern. Returns `true` if a valid pointer was found.

#### Remove

```csharp
public void Remove(TKey key)
public void Clear()
```

Remove specific entry or clear entire cache.

### Query Methods

#### Contains

```csharp
public bool Contains(TKey key)
```

Check if key exists and is valid (not expired, not null).

#### GetAll

```csharp
public IEnumerable<(TKey Key, IntPtr Pointer)> GetAll()
```

Enumerate all valid entries. Expired entries are automatically skipped.

#### Find

```csharp
public IntPtr Find(Func<TKey, IntPtr, bool> predicate)
```

Find first entry matching predicate. Returns `IntPtr.Zero` if none match.

#### FindAll

```csharp
public IEnumerable<IntPtr> FindAll(Func<TKey, IntPtr, bool> predicate)
```

Find all entries matching predicate.

### Lifecycle

#### Prune

```csharp
public int Prune()
```

Remove all expired entries. Returns count of removed entries.

#### IsAlive

```csharp
public bool IsAlive(TKey key)
```

Check if the cached object's IL2CPP `m_CachedPtr` is still valid.

## Examples

### Caching with Expiration

```csharp
// Cache expires after 60 frames (~1 second)
var cache = new PointerCache<int>(defaultExpirationFrames: 60);

cache.Set(1, somePointer);  // Uses default expiration
cache.Set(2, otherPointer, 120);  // Custom: expires after 2 seconds
```

### Actor Lookup by Name

```csharp
private PointerCache<string> _actorCache = new();

public IntPtr FindActor(string name)
{
    // Check cache first
    if (_actorCache.TryGet(name, out IntPtr cached))
        return cached;

    // Cache miss - find and cache
    var actor = GameQuery.FindByName("TacticalActor", name);
    if (actor.HasValue)
    {
        _actorCache.Set(name, actor.Value.Pointer);
        return actor.Value.Pointer;
    }

    return IntPtr.Zero;
}
```

### Skill Pointer Tracking

```csharp
private PointerCache<string> _skillCache = new();

// Cache all skills for an actor
public void CacheActorSkills(IntPtr actorPtr)
{
    var actor = new GameObj(actorPtr);
    var skills = actor.ReadList("Skills");

    foreach (var skill in skills)
    {
        string skillId = skill.ReadString("SkillId");
        _skillCache.Set(skillId, skill.Pointer);
    }
}

// Later: get skill by ID
public IntPtr GetSkill(string skillId)
{
    return _skillCache.Get(skillId);
}
```

### Finding by Predicate

```csharp
// Find first wounded actor
IntPtr wounded = _actorCache.Find((name, ptr) =>
{
    var actor = new GameObj(ptr);
    int hp = actor.ReadInt("HitPoints");
    int maxHp = actor.ReadInt("MaxHitPoints");
    return hp < maxHp;
});

// Find all enemies
var enemies = _actorCache.FindAll((name, ptr) =>
{
    var actor = new GameObj(ptr);
    int faction = actor.ReadInt("Faction");
    return faction >= 5;  // Enemy factions
});
```

### Cleanup on Scene Change

```csharp
public void OnSceneLoaded(int buildIndex, string sceneName)
{
    // Clear stale pointers when scene changes
    _actorCache.Clear();
    _skillCache.Clear();
}
```

## Comparison: Before and After

### Before (Manual Tracking)

```csharp
// 6-8 lines for safe pointer access
private Dictionary<string, IntPtr> _actors = new();

public IntPtr GetActor(string name)
{
    if (!_actors.TryGetValue(name, out IntPtr ptr))
        return IntPtr.Zero;

    if (ptr == IntPtr.Zero)
        return IntPtr.Zero;

    // Check if object is still alive
    var obj = new GameObj(ptr);
    if (obj.IsNull || !obj.IsAlive)
    {
        _actors.Remove(name);
        return IntPtr.Zero;
    }

    return ptr;
}
```

### After (PointerCache)

```csharp
// 1-2 lines
private PointerCache<string> _actors = new();

public IntPtr GetActor(string name)
{
    return _actors.Get(name);  // All checks handled internally
}
```

## Thread Safety

All PointerCache operations are thread-safe:

```csharp
// Safe to call from any thread
Task.Run(() =>
{
    _cache.Set("key", ptr);
    var result = _cache.Get("key");
});
```

Internally, PointerCache uses `ConcurrentDictionary` and atomic operations.

## Best Practices

### 1. Use Appropriate Expiration

```csharp
// Short expiration for frequently-changing data
var targetCache = new PointerCache<int>(30);  // ~0.5 seconds

// No expiration for stable references
var templateCache = new PointerCache<string>();
```

### 2. Clear on Scene Changes

```csharp
GameState.SceneLoaded += (idx, name) =>
{
    _actorCache.Clear();
    _tileCache.Clear();
};
```

### 3. Prune Periodically for Long-Lived Caches

```csharp
// In your update loop or on a timer
if (frameCount % 300 == 0)  // Every ~5 seconds
{
    _cache.Prune();
}
```

### 4. Prefer TryGet for Control Flow

```csharp
// Better than checking Get() result
if (_cache.TryGet(key, out IntPtr ptr))
{
    var obj = new GameObj(ptr);
    // Use obj...
}
```

## See Also

- [GameObj](game-obj.md) -- Safe handle for IL2CPP objects
- [GameQuery](game-query.md) -- Finding objects in the scene
- [Intercept](intercept.md) -- Event-based interception with pointer access
