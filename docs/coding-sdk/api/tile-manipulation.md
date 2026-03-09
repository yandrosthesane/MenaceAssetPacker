# TileManipulation

`Menace.SDK.TileManipulation` -- Dynamic tile property manipulation for traversability, cover, line-of-sight, and movement blocking with temporary override support.

## Overview

TileManipulation provides comprehensive control over tile properties during tactical combat. Modify tile traversability, cover values, LOS blocking, and movement restrictions with support for temporary or permanent changes.

**Key Features:**
- Temporary and permanent tile modifications
- Counter-based LOS and movement blocking systems
- Per-direction cover and movement control
- Per-actor enterable overrides
- Automatic restoration after N turns

**Based on Ghidra reverse engineering:**
- Tile.flags @ +0x1C (bitfield, bit 0 = IsBlocked/NotTraversable)
- Tile.m_CoverValues @ +0x28 (int32[] array, 8 directions)
- Tile.m_BlocksMovement @ +0x38 (byte[] array, 8 directions, counter-based)
- Tile.m_IsEnterable @ +0x30 (bool[] array, 4 directions)
- Tile.LOSBlockerCounter @ +0x60 (byte, counter for LOS blocking)

**Direction Encoding:**
- 8 directions: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
- 4 directions (enterable): 0=N, 1=E, 2=S, 3=W

**Cover Values:**
- 0 = None
- 1 = Half/Light
- 2 = Full/Heavy
- 3+ = Enhanced

## Module Path

```csharp
using Menace.SDK;
// Access via: TileManipulation.MethodName(...)
```

## Tile Traversability

### SetTraversableOverride

**Signature**: `bool SetTraversableOverride(GameObj tile, bool traversable, int turns = -1)`

**Description**: Set whether a tile is traversable (can be walked on). Modifies bit 0 of the tile flags field.

**Parameters:**
- `tile` (GameObj): The tile to modify
- `traversable` (bool): True to make traversable, false to block
- `turns` (int): Number of turns before reverting (-1 for permanent, default)

**Returns**: True if successful

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

// Temporarily block tile for 3 turns (environmental hazard)
if (TileManipulation.SetTraversableOverride(tile, false, 3))
{
    DevConsole.Log("Tile blocked for 3 turns");
}

// Permanently make tile walkable (clear rubble)
TileManipulation.SetTraversableOverride(tile, true, -1);

// Create temporary barrier
var barrierTiles = GetTilesInLine(start, end);
foreach (var t in barrierTiles)
{
    TileManipulation.SetTraversableOverride(t, false, 5);
}
```

**Related:**
- [ClearTraversableOverride](#cleartraversableoverride) - Clear override
- [SetEnterable](#setenterable) - Control enterable state

**Notes:**
- Modifies Tile.flags @ +0x1C, bit 0
- turns=0 is invalid, use -1 for permanent or >0 for temporary
- Temporary overrides auto-restore via UpdateOverrides()
- Affects pathfinding and movement validation

---

### ClearTraversableOverride

**Signature**: `bool ClearTraversableOverride(GameObj tile)`

**Description**: Clear traversable override and restore original state.

**Parameters:**
- `tile` (GameObj): The tile to restore

**Returns**: True if successful

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

// Clear override immediately
if (TileManipulation.ClearTraversableOverride(tile))
{
    DevConsole.Log("Tile traversability restored");
}
```

**Related:**
- [SetTraversableOverride](#settraversableoverride) - Set override

---

## Cover System

### SetCoverOverride

**Signature**: `bool SetCoverOverride(GameObj tile, int direction, int cover, int turns = -1)`

**Description**: Set cover value for a specific direction. Cover is stored as int32 array with 8 directional values.

**Parameters:**
- `tile` (GameObj): The tile to modify
- `direction` (int): Direction index (0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW)
- `cover` (int): Cover value (0=None, 1=Half, 2=Full, 3+=Enhanced)
- `turns` (int): Number of turns before reverting (-1 for permanent, default)

**Returns**: True if successful

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

// Add full cover to the north for 2 turns
if (TileManipulation.SetCoverOverride(tile, TileMap.DIR_NORTH, 2, 2))
{
    DevConsole.Log("Temporary full cover added");
}

// Remove cover from the east permanently
TileManipulation.SetCoverOverride(tile, TileMap.DIR_EAST, 0, -1);

// Create cover wall
var wallTiles = GetTilesInLine(start, end);
foreach (var t in wallTiles)
{
    TileManipulation.SetCoverOverride(t, TileMap.DIR_SOUTH, 2, -1);
}

// Dynamic cover based on environment
for (int dir = 0; dir < 8; dir++)
{
    var coverValue = CalculateCoverForDirection(tile, dir);
    TileManipulation.SetCoverOverride(tile, dir, coverValue, -1);
}
```

**Related:**
- [ClearCoverOverrides](#clearcoveroverrides) - Clear all cover overrides

**Notes:**
- Modifies Tile.m_CoverValues @ +0x28 (int32[8])
- Direction must be 0-7
- turns=0 is invalid
- Temporary overrides auto-restore via UpdateOverrides()

---

### ClearCoverOverrides

**Signature**: `bool ClearCoverOverrides(GameObj tile)`

**Description**: Clear all cover overrides and restore original values.

**Parameters:**
- `tile` (GameObj): The tile to restore

**Returns**: True if successful

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

// Clear all cover overrides
if (TileManipulation.ClearCoverOverrides(tile))
{
    DevConsole.Log("Cover restored to original values");
}
```

**Related:**
- [SetCoverOverride](#setcoveroverride) - Set cover

---

## Enterable Control

### SetEnterable

**Signature**: `bool SetEnterable(GameObj tile, bool enterable, int turns = -1)`

**Description**: Set whether a tile is enterable from cardinal directions (N, E, S, W). Affects all 4 cardinal directions simultaneously.

**Parameters:**
- `tile` (GameObj): The tile to modify
- `enterable` (bool): True to allow entry, false to block
- `turns` (int): Number of turns before reverting (-1 for permanent, default)

**Returns**: True if successful

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

// Block entry to tile for 1 turn
if (TileManipulation.SetEnterable(tile, false, 1))
{
    DevConsole.Log("Tile entry blocked for 1 turn");
}

// Permanently allow entry
TileManipulation.SetEnterable(tile, true, -1);
```

**Related:**
- [SetEnterableBy](#setenterableby) - Per-actor enterable control

**Notes:**
- Modifies Tile.m_IsEnterable @ +0x30 (bool[4])
- Affects all 4 cardinal directions
- For per-direction control, modify m_IsEnterable array directly
- turns=0 is invalid

---

### SetEnterableBy

**Signature**: `bool SetEnterableBy(GameObj tile, GameObj actor, bool enterable)`

**Description**: Set whether a tile is enterable by a specific actor. Creates a per-actor override that takes precedence over global tile settings.

**Parameters:**
- `tile` (GameObj): The tile to modify
- `actor` (GameObj): The actor to grant/deny access
- `enterable` (bool): True to allow entry, false to block

**Returns**: True if successful

**Example:**
```csharp
var restrictedTile = TileMap.GetTileAt(5, 10);
var vipActor = FindActorByName("VIP");
var enemyActor = FindActorByName("Enemy");

// Allow VIP to enter blocked tile
TileManipulation.SetEnterableBy(restrictedTile, vipActor, true);

// Block enemy from entering tile
TileManipulation.SetEnterableBy(restrictedTile, enemyActor, false);

// Create restricted zone
var restrictedZone = GetTilesInArea(startX, startY, endX, endY);
foreach (var t in restrictedZone)
{
    TileManipulation.SetEnterableBy(t, enemyActor, false);
}
```

**Related:**
- [ClearEnterableByActor](#clearenterablebyactor) - Clear per-actor override
- [IsEnterableBy](#isenterableby) - Check if enterable

**Notes:**
- Creates permanent per-actor override
- Overrides take precedence over global tile enterable state
- Call ClearEnterableByActor() to remove override
- Useful for restricted zones and unit-specific pathfinding

---

### ClearEnterableByActor

**Signature**: `bool ClearEnterableByActor(GameObj tile, GameObj actor)`

**Description**: Clear per-actor enterable override.

**Parameters:**
- `tile` (GameObj): The tile to modify
- `actor` (GameObj): The actor to clear override for

**Returns**: True if override was found and cleared

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);
var actor = TacticalController.GetActiveActor();

// Clear override
if (TileManipulation.ClearEnterableByActor(tile, actor))
{
    DevConsole.Log("Per-actor override cleared");
}
```

**Related:**
- [SetEnterableBy](#setenterableby) - Create override

---

### IsEnterableBy

**Signature**: `bool IsEnterableBy(GameObj tile, GameObj actor)`

**Description**: Check if a tile is enterable by a specific actor (respects per-actor overrides).

**Parameters:**
- `tile` (GameObj): The tile to check
- `actor` (GameObj): The actor to check

**Returns**: True if actor can enter tile

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);
var actor = TacticalController.GetActiveActor();

if (TileManipulation.IsEnterableBy(tile, actor))
{
    DevConsole.Log("Tile is accessible");
}
else
{
    DevConsole.Log("Tile is blocked for this actor");
}
```

**Related:**
- [SetEnterableBy](#setenterableby) - Set per-actor state

**Notes:**
- Checks per-actor override first
- Falls back to tile's default enterable state
- Returns first cardinal direction as representative

---

## Line of Sight Control

### SetBlocksLOS

**Signature**: `bool SetBlocksLOS(GameObj tile, bool blocks, int turns = -1)`

**Description**: Set whether a tile blocks line of sight. Uses counter-based system at +0x60 - each "add blocker" increments, each "remove blocker" decrements. LOS is blocked when counter > 0.

**Parameters:**
- `tile` (GameObj): The tile to modify
- `blocks` (bool): True to block LOS, false to unblock
- `turns` (int): Number of turns before reverting (-1 for permanent, default)

**Returns**: True if successful

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

// Block LOS through tile for 2 turns (smoke grenade)
if (TileManipulation.SetBlocksLOS(tile, true, 2))
{
    DevConsole.Log("Smoke blocks LOS for 2 turns");
}

// Permanently make tile transparent
TileManipulation.SetBlocksLOS(tile, false, -1);

// Create smoke wall
var smokeTiles = GetTilesInLine(start, end);
foreach (var t in smokeTiles)
{
    TileManipulation.SetBlocksLOS(t, true, 3);
}
```

**Related:**
- [GetOverrideTurnsRemaining](#getoverrideturnsremaining) - Check remaining turns

**Notes:**
- Uses counter at Tile.LOSBlockerCounter @ +0x60
- Increments/decrements counter (not set to absolute value)
- Counter clamped 0-255
- LOS blocked when counter > 0
- turns=0 is invalid

---

## Movement Blocking

### SetBlocksMovement

**Signature**: `bool SetBlocksMovement(GameObj tile, bool blocks, int turns = -1)`

**Description**: Set whether a tile blocks movement in all directions. Uses per-direction counters at +0x38 (byte[8]). Modifies all 8 direction counters simultaneously.

**Parameters:**
- `tile` (GameObj): The tile to modify
- `blocks` (bool): True to block movement, false to unblock
- `turns` (int): Number of turns before reverting (-1 for permanent, default)

**Returns**: True if successful

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

// Block all movement through tile for 1 turn
if (TileManipulation.SetBlocksMovement(tile, true, 1))
{
    DevConsole.Log("Movement blocked for 1 turn");
}

// Create impassable wall
var wallTiles = GetTilesInLine(start, end);
foreach (var t in wallTiles)
{
    TileManipulation.SetBlocksMovement(t, true, -1);
}
```

**Related:**
- [SetBlocksMovementInDirection](#setblocksmovementindirection) - Per-direction control

**Notes:**
- Modifies Tile.m_BlocksMovement @ +0x38 (byte[8])
- Increments/decrements all 8 direction counters
- Counters clamped 0-255
- Movement blocked when counter > 0
- For per-direction control, use SetBlocksMovementInDirection()
- turns=0 is invalid

---

### SetBlocksMovementInDirection

**Signature**: `bool SetBlocksMovementInDirection(GameObj tile, int direction, bool blocks, int turns = -1)`

**Description**: Set whether a tile blocks movement in a specific direction.

**Parameters:**
- `tile` (GameObj): The tile to modify
- `direction` (int): Direction index (0-7)
- `blocks` (bool): True to block movement, false to unblock
- `turns` (int): Number of turns before reverting (-1 for permanent, default)

**Returns**: True if successful

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

// Block movement from the north only
TileManipulation.SetBlocksMovementInDirection(tile, TileMap.DIR_NORTH, true, -1);

// Create one-way passage
TileManipulation.SetBlocksMovementInDirection(tile, TileMap.DIR_EAST, true, -1);
TileManipulation.SetBlocksMovementInDirection(tile, TileMap.DIR_WEST, false, -1);

// Temporary directional block
for (int dir = 0; dir < 4; dir++)
{
    TileManipulation.SetBlocksMovementInDirection(tile, dir, true, 2);
}
```

**Related:**
- [SetBlocksMovement](#setblocksmovement) - Block all directions

**Notes:**
- Modifies Tile.m_BlocksMovement @ +0x38 (byte[8])
- Direction must be 0-7
- Increments/decrements specific direction counter
- Counter clamped 0-255
- turns=0 is invalid

---

## Override Management

### ClearTileOverrides

**Signature**: `bool ClearTileOverrides(GameObj tile)`

**Description**: Clear all overrides for a specific tile.

**Parameters:**
- `tile` (GameObj): The tile to clear overrides for

**Returns**: True if overrides were found and cleared

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

// Clear all overrides on tile
if (TileManipulation.ClearTileOverrides(tile))
{
    DevConsole.Log("All tile overrides cleared");
}
```

**Related:**
- [ClearAllOverrides](#clearalloverrides) - Clear all tiles

---

### ClearAllOverrides

**Signature**: `void ClearAllOverrides()`

**Description**: Clear all tile manipulation overrides immediately for all tiles.

**Example:**
```csharp
// Clear all tile overrides (e.g., combat ended)
TileManipulation.ClearAllOverrides();
DevConsole.Log("All tile overrides cleared");
```

**Related:**
- [ClearTileOverrides](#cleartileoverrides) - Clear specific tile

---

### GetOverrideTurnsRemaining

**Signature**: `int GetOverrideTurnsRemaining(GameObj tile)`

**Description**: Get remaining turns for a tile's override.

**Parameters:**
- `tile` (GameObj): The tile to check

**Returns**: Remaining turns, or -1 if no override exists

**Example:**
```csharp
var tile = TileMap.GetTileAt(5, 10);

int remaining = TileManipulation.GetOverrideTurnsRemaining(tile);
if (remaining > 0)
{
    DevConsole.Log($"Override expires in {remaining} turns");
}
else if (remaining == -1)
{
    DevConsole.Log("No override active");
}
```

**Related:**
- All override methods

---

## Automatic Override Management

The module automatically tracks and expires temporary overrides. To enable this, the SDK hooks into turn events:

```csharp
using Menace.SDK;

// Automatic initialization (internal to SDK)
TileManipulation.Initialize();

// UpdateOverrides() is called automatically from TacticalEventHooks.OnTurnEnd
// No manual call needed
```

**UpdateOverrides() (Internal):**
- Decrements turn counters for all active overrides
- Restores original tile state when turns reach 0
- Removes expired overrides from tracking

## Complete Example

```csharp
using Menace.SDK;

// Dynamic environmental hazards
public class EnvironmentalHazardSystem
{
    public void CreateFireWall(GameObj startTile, GameObj endTile, int duration)
    {
        var tiles = GetTilesInLine(startTile, endTile);

        foreach (var tile in tiles)
        {
            // Block movement
            TileManipulation.SetBlocksMovement(tile, true, duration);

            // Block LOS (smoke from fire)
            TileManipulation.SetBlocksLOS(tile, true, duration);

            // Make tile untraversable
            TileManipulation.SetTraversableOverride(tile, false, duration);
        }

        DevConsole.Log($"Fire wall created: {tiles.Count} tiles for {duration} turns");
    }

    public void CreateSmokeCloud(GameObj centerTile, int radius, int duration)
    {
        var tiles = GetTilesInRadius(centerTile, radius);

        foreach (var tile in tiles)
        {
            // Block LOS but not movement
            TileManipulation.SetBlocksLOS(tile, true, duration);
        }

        DevConsole.Log($"Smoke cloud: {tiles.Count} tiles for {duration} turns");
    }

    public void CreateCoverWall(GameObj startTile, GameObj endTile)
    {
        var tiles = GetTilesInLine(startTile, endTile);

        foreach (var tile in tiles)
        {
            // Add full cover to both sides
            TileManipulation.SetCoverOverride(tile, TileMap.DIR_NORTH, 2, -1);
            TileManipulation.SetCoverOverride(tile, TileMap.DIR_SOUTH, 2, -1);

            // Make tile traversable but block movement through
            TileManipulation.SetTraversableOverride(tile, true, -1);
            TileManipulation.SetBlocksMovement(tile, true, -1);
        }

        DevConsole.Log($"Cover wall created: {tiles.Count} tiles");
    }

    public void CreateRestrictedZone(GameObj[] tiles, GameObj[] allowedActors)
    {
        foreach (var tile in tiles)
        {
            // Make tile globally untraversable
            TileManipulation.SetTraversableOverride(tile, false, -1);

            // Allow specific actors to enter
            foreach (var actor in allowedActors)
            {
                TileManipulation.SetEnterableBy(tile, actor, true);
            }
        }

        DevConsole.Log($"Restricted zone: {tiles.Length} tiles, {allowedActors.Length} allowed");
    }
}

// Tactical abilities using tile manipulation
public class TacticalAbilities
{
    // Smoke grenade ability
    public void UseSmokeGrenade(GameObj targetTile)
    {
        var affectedTiles = GetTilesInRadius(targetTile, 2);

        foreach (var tile in affectedTiles)
        {
            TileManipulation.SetBlocksLOS(tile, true, 3);
        }

        DevConsole.Log($"Smoke grenade: {affectedTiles.Count} tiles affected");
    }

    // Fortify position ability
    public void FortifyPosition(GameObj actorTile)
    {
        // Add cover in all directions
        for (int dir = 0; dir < 8; dir++)
        {
            TileManipulation.SetCoverOverride(actorTile, dir, 2, 5);
        }

        DevConsole.Log("Position fortified for 5 turns");
    }

    // Create barrier ability
    public void CreateBarrier(GameObj startTile, GameObj endTile)
    {
        var tiles = GetTilesInLine(startTile, endTile);

        foreach (var tile in tiles)
        {
            TileManipulation.SetBlocksMovement(tile, true, 3);
            TileManipulation.SetBlocksLOS(tile, true, 3);
        }

        DevConsole.Log($"Barrier created: {tiles.Count} tiles for 3 turns");
    }

    // One-way door
    public void CreateOneWayDoor(GameObj doorTile, int allowedDirection)
    {
        // Block movement in all directions except one
        for (int dir = 0; dir < 8; dir++)
        {
            if (dir != allowedDirection)
            {
                TileManipulation.SetBlocksMovementInDirection(doorTile, dir, true, -1);
            }
        }

        DevConsole.Log($"One-way door: allows movement from direction {allowedDirection}");
    }
}

// Environmental state tracking
public class TileStateManager
{
    public void PrintTileStatus(GameObj tile)
    {
        DevConsole.Log("=== Tile Status ===");

        int remaining = TileManipulation.GetOverrideTurnsRemaining(tile);
        if (remaining > 0)
        {
            DevConsole.Log($"Override expires in {remaining} turns");
        }
        else if (remaining == -1)
        {
            DevConsole.Log("No temporary overrides");
        }

        // Check specific states
        var actor = TacticalController.GetActiveActor();
        bool enterable = TileManipulation.IsEnterableBy(tile, actor);
        DevConsole.Log($"Enterable by {actor.GetName()}: {enterable}");
    }

    public void ClearAllHazards()
    {
        TileManipulation.ClearAllOverrides();
        DevConsole.Log("All environmental hazards cleared");
    }
}
```

## Direction Constants

```csharp
// 8-direction constants (for cover, movement blocking)
DIR_NORTH = 0
DIR_NORTHEAST = 1
DIR_EAST = 2
DIR_SOUTHEAST = 3
DIR_SOUTH = 4
DIR_SOUTHWEST = 5
DIR_WEST = 6
DIR_NORTHWEST = 7

// 4-direction constants (for enterable)
DIR_NORTH = 0
DIR_EAST = 1
DIR_SOUTH = 2
DIR_WEST = 3
```

## See Also

- [TileMap](tile-map.md) - Tile queries and pathfinding
- [Intercept](intercept.md) - Tile interceptors (OnTileGetCover, OnTileIsBlocked, OnTileTraversable)
- [TacticalEventHooks](tactical-event-hooks.md) - Turn events for override expiration
- [EntityAI](entity-ai.md) - AI pathfinding behavior
