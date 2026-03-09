# Tile Structure

## Overview

The Tile class represents individual map cells in tactical combat. Each tile tracks traversability, cover values, line-of-sight blocking, movement blocking, and enterability. The system uses counter-based mechanisms for LOS and movement blocking, allowing multiple sources to affect the same tile.

Based on reverse engineering findings from Phase 1 implementation (TileManipulation.cs).

## Architecture

```
TileMap
    └── Tile[,] m_Tiles (2D grid)

Tile (per cell)
    ├── flags: int32 (bitfield for traversable, etc.)
    ├── m_CoverValues: int32[8] (cover per direction)
    ├── m_IsEnterable: bool[4] (cardinal direction entry)
    ├── m_BlocksMovement: byte[8] (movement counter per direction)
    └── LOSBlockerCounter: byte (line-of-sight counter)
```

## Memory Layout

### Tile Offsets

| Offset | Type | Field Name | Description | Status |
|--------|------|------------|-------------|--------|
| 0x1C | int32 | flags | Bitfield for tile properties (bit 0 = blocked/not traversable) | ✅ Verified |
| 0x28 | int32[8] | m_CoverValues | Cover value per direction (0-7 clockwise from N) | ✅ Verified |
| 0x30 | byte[4] | m_IsEnterable | Enterable from cardinal directions (N, E, S, W) | ✅ Verified |
| 0x38 | byte[8] | m_BlocksMovement | Movement blocking counters per direction | ✅ Verified |
| 0x60 | byte | LOSBlockerCounter | Line-of-sight blocking counter | ✅ Verified |

### Array Sizes

```c
const int DIRECTIONS_8 = 8;  // Full compass (N, NE, E, SE, S, SW, W, NW)
const int DIRECTIONS_4 = 4;  // Cardinal directions (N, E, S, W)
```

## Direction Encoding

### 8-Direction System (Cover, Movement Blocking)

Used for m_CoverValues and m_BlocksMovement arrays:

| Index | Direction | Angle | Cardinal |
|-------|-----------|-------|----------|
| 0 | North (N) | 0° | Yes |
| 1 | Northeast (NE) | 45° | No |
| 2 | East (E) | 90° | Yes |
| 3 | Southeast (SE) | 135° | No |
| 4 | South (S) | 180° | Yes |
| 5 | Southwest (SW) | 225° | No |
| 6 | West (W) | 270° | Yes |
| 7 | Northwest (NW) | 315° | No |

### 4-Direction System (Enterability)

Used for m_IsEnterable array:

| Index | Direction | Corresponding 8-dir |
|-------|-----------|---------------------|
| 0 | North (N) | 0 |
| 1 | East (E) | 2 |
| 2 | South (S) | 4 |
| 3 | West (W) | 6 |

## Flags Bitfield

### Tile.flags (0x1C)

A 32-bit integer with bitflags:

| Bit | Mask | Name | Description |
|-----|------|------|-------------|
| 0 | 0x01 | IsBlocked / NotTraversable | Tile cannot be walked on |
| 1-31 | N/A | Unknown | Other tile properties (fog, visible, etc.) |

**Traversability:**
```c
// Read flags
int32 flags = *(int32*)(tile + 0x1C);

// Check if traversable
bool traversable = (flags & 0x01) == 0;  // Bit 0 clear = traversable

// Make traversable
flags &= ~0x01;  // Clear bit 0
*(int32*)(tile + 0x1C) = flags;

// Block traversal
flags |= 0x01;  // Set bit 0
*(int32*)(tile + 0x1C) = flags;
```

### IsTraversable() Method

Game method that checks bit 0:

```c
bool Tile::IsTraversable() {
    return (this->flags & 0x01) == 0;
}
```

## Cover System

### Cover Values Array (0x28)

An array of 8 int32 values, one per direction:

```c
// Access cover for specific direction
int32 coverNorth = *(int32*)(tile + 0x28 + (0 * sizeof(int32)));
int32 coverEast = *(int32*)(tile + 0x28 + (2 * sizeof(int32)));

// Iterate all directions
for (int dir = 0; dir < 8; dir++) {
    int32 cover = *(int32*)(tile + 0x28 + (dir * sizeof(int32)));
    // Process cover value...
}
```

### Cover Value Encoding

| Value | Cover Type | Description |
|-------|----------|-------------|
| 0 | None | No cover from this direction |
| 1 | Half / Light | Partial protection, small cover bonus |
| 2 | Full / Heavy | Strong protection, large cover bonus |
| 3+ | Enhanced | Special cover (rare, possibly mod-added) |

**Interpretation:**
- Higher values = better protection from that direction
- Cover affects hit chance, damage reduction
- Cover checked from attacker's direction to defender

### Cover Direction Logic

Cover value at direction D protects from attacks coming **from** direction D:

```
Attacker (West) → [Cover=2 West] → Defender
                    ↑
              Full cover protects from west
```

Example:
```c
// Tile has full cover to the north
*(int32*)(tile + 0x28 + (0 * sizeof(int32))) = 2;

// Actor on this tile is protected from attacks from the north
// Attacks from south, east, west have no cover
```

## Enterability System

### IsEnterable Array (0x30)

An array of 4 bytes for cardinal directions:

```c
// Access enterability
byte enterableNorth = *(byte*)(tile + 0x30 + 0);  // N
byte enterableEast = *(byte*)(tile + 0x30 + 1);   // E
byte enterableSouth = *(byte*)(tile + 0x30 + 2);  // S
byte enterableWest = *(byte*)(tile + 0x30 + 3);   // W

// Set enterable from all directions
for (int i = 0; i < 4; i++) {
    *(byte*)(tile + 0x30 + i) = 1;
}
```

### Enterability Logic

Controls whether units can **enter** the tile from a given direction:

```
Actor at (X, Y-1) moving South
    ↓
[IsEnterable[North] == 1] at (X, Y)
    ↓
Movement allowed
```

**Use Cases:**
- One-way passages (enterable from one side only)
- Directional barriers (walls with openings)
- Dynamic blocking (temporary door closure)

## Movement Blocking System

### BlocksMovement Counters (0x38)

An array of 8 bytes (counters) for directional movement blocking:

```c
// Access movement blocking counter for direction
byte blocksNorth = *(byte*)(tile + 0x38 + 0);
byte blocksEast = *(byte*)(tile + 0x38 + 2);

// Increment counter (add blocker)
*(byte*)(tile + 0x38 + dir) = *(byte*)(tile + 0x38 + dir) + 1;

// Decrement counter (remove blocker)
byte current = *(byte*)(tile + 0x38 + dir);
if (current > 0) {
    *(byte*)(tile + 0x38 + dir) = current - 1;
}
```

### Counter-Based Blocking

Movement is blocked when counter > 0:

```c
bool IsMovementBlocked(int direction) {
    byte counter = *(byte*)(tile + 0x38 + direction);
    return counter > 0;
}
```

**Why Counters?**
- Multiple sources can block the same tile (walls, fire, units, etc.)
- Each blocker increments the counter
- Removing a blocker decrements the counter
- Tile is only unblocked when counter reaches 0

**Example:**
```c
// Wall adds movement blocker
IncrementBlocker(tile, DIR_NORTH);  // counter = 1, blocked

// Fire effect also blocks
IncrementBlocker(tile, DIR_NORTH);  // counter = 2, still blocked

// Fire dissipates
DecrementBlocker(tile, DIR_NORTH);  // counter = 1, still blocked (wall remains)

// Wall destroyed
DecrementBlocker(tile, DIR_NORTH);  // counter = 0, unblocked
```

## Line-of-Sight System

### LOSBlockerCounter (0x60)

A single byte counter for LOS blocking:

```c
// Read counter
byte losCounter = *(byte*)(tile + 0x60);
bool blocksLOS = (losCounter > 0);

// Add LOS blocker
*(byte*)(tile + 0x60) = *(byte*)(tile + 0x60) + 1;

// Remove LOS blocker
byte current = *(byte*)(tile + 0x60);
if (current > 0) {
    *(byte*)(tile + 0x60) = current - 1;
}
```

### LOS Blocking Logic

Same counter-based system as movement blocking:

- **Counter = 0:** Tile is transparent (does not block LOS)
- **Counter > 0:** Tile blocks LOS

**Sources of LOS Blocking:**
- Walls and structures
- Smoke grenades
- Dense vegetation
- Dynamic effects (explosions, debris)

### Methods

| Address | Signature | Description |
|---------|-----------|-------------|
| 0x1805cae00 | void BlockLineOfSight() | Increment LOS counter (+1) |
| N/A | void UnblockLineOfSight() | Decrement LOS counter (-1) |

## Methods

### Tile Methods

| Address | Signature | Description |
|---------|-----------|-------------|
| N/A | bool IsTraversable() | Check if tile can be walked on (!(flags & 0x01)) |
| 0x1805cae00 | void BlockLineOfSight() | Add LOS blocker (counter++) |
| N/A | void UnblockLineOfSight() | Remove LOS blocker (counter--) |
| N/A | void AddMovementBlocked(int dir) | Add movement blocker for direction |
| N/A | void RemoveMovementBlocked(int dir) | Remove movement blocker for direction |
| N/A | int GetCover(int direction) | Get cover value for direction |
| N/A | void SetCover(int direction, int value) | Set cover value for direction |

### TileMap Methods

| Method | Description |
|--------|-------------|
| Tile GetTile(int x, int y) | Get tile at grid coordinates |
| Tile GetTileAtWorldPos(Vector3 pos) | Get tile at world position |
| bool IsValidTile(int x, int y) | Check if coordinates are in bounds |

## SDK Implementation

The TileManipulation.cs SDK module provides comprehensive tile modification:

### Traversability

```csharp
// Block tile for 3 turns
TileManipulation.SetTraversableOverride(tile, traversable: false, turns: 3);

// Permanently make walkable
TileManipulation.SetTraversableOverride(tile, traversable: true, turns: -1);

// Clear override
TileManipulation.ClearTraversableOverride(tile);
```

### Cover Modification

```csharp
// Add full cover to the north for 2 turns
TileManipulation.SetCoverOverride(tile, direction: 0, cover: 2, turns: 2);

// Remove all cover permanently
for (int dir = 0; dir < 8; dir++) {
    TileManipulation.SetCoverOverride(tile, dir, cover: 0, turns: -1);
}

// Clear all cover overrides
TileManipulation.ClearCoverOverrides(tile);
```

### Enterability

```csharp
// Block entry to tile for 1 turn (all directions)
TileManipulation.SetEnterable(tile, enterable: false, turns: 1);

// Per-actor enterable control
TileManipulation.SetEnterableBy(tile, actor: specialUnit, enterable: true);

// Check if specific actor can enter
bool canEnter = TileManipulation.IsEnterableBy(tile, actor);

// Clear per-actor override
TileManipulation.ClearEnterableByActor(tile, actor);
```

### Line-of-Sight

```csharp
// Block LOS through tile for 2 turns
TileManipulation.SetBlocksLOS(tile, blocks: true, turns: 2);

// Permanently make transparent
TileManipulation.SetBlocksLOS(tile, blocks: false, turns: -1);
```

### Movement Blocking

```csharp
// Block all movement through tile for 1 turn
TileManipulation.SetBlocksMovement(tile, blocks: true, turns: 1);

// Block only north direction
TileManipulation.SetBlocksMovementInDirection(tile, direction: 0, blocks: true, turns: 2);
```

### Override Management

```csharp
// Called automatically from OnTurnEnd hook
TileManipulation.UpdateOverrides();  // Decrement turn counters

// Manual override clearing
TileManipulation.ClearAllOverrides();
TileManipulation.ClearTileOverrides(tile);

// Query remaining turns
int remaining = TileManipulation.GetOverrideTurnsRemaining(tile);
```

## Temporary Override System

### Override Mechanism

TileManipulation.cs implements a temporary override system:

1. **Store Original Values:** When setting temporary override, original field values are saved
2. **Apply Override:** Modify tile fields directly
3. **Track Duration:** Store turn counter for automatic restoration
4. **Auto-Restore:** On turn end, decrement counters and restore expired overrides

### Turn Duration

The `turns` parameter controls override lifetime:

- **turns = -1:** Permanent modification (no restoration)
- **turns = 1-N:** Temporary modification (restored after N turns)
- **turns = 0:** Invalid (use -1 or positive value)

### Override Storage

```csharp
private class TileOverride {
    public uint? OriginalFlags;           // For traversable
    public int[] OriginalCoverValues;     // For cover (8 directions)
    public bool[] OriginalIsEnterable;    // For enterable (4 directions)
    public byte OriginalLOSBlockerCounter; // For LOS
    public byte[] OriginalBlocksMovement; // For movement (8 directions)
    public int TurnsRemaining;
    public OverrideType Type;             // Which fields are overridden
}
```

### Per-Actor Enterability

In addition to global tile overrides, per-actor enterable state is tracked separately:

```csharp
// Allows specific actors to bypass tile blocking
TileManipulation.SetEnterableBy(tile, vipUnit, enterable: true);

// Now only vipUnit can enter this tile
// All other actors see it as blocked
```

## Notes

### Verification Status

All offsets verified through:
1. Ghidra decompilation of Tile class
2. Runtime testing in TileManipulation.cs
3. Successful tile modification in tactical combat

**Version:** Verified for game version 34.0.1 (March 2026)

### Counter Safety

When using counter-based systems (LOS, movement blocking):

**✅ DO:**
- Increment when adding blocker
- Decrement when removing blocker
- Check counter > 0 before decrement
- Use SDK methods (handle overflow/underflow)

**❌ DON'T:**
- Set counter to absolute value (breaks multi-source tracking)
- Decrement below 0 (causes underflow)
- Assume counter = 1 means single blocker (could be multiple)

### Thread Safety

Tile modifications are generally safe (no parallel tile evaluation). However:

**SAFE:**
- Modify tiles during turn events
- Modify tiles from main thread

**UNSAFE:**
- Modify tiles during pathfinding (if running in background)
- Modify tiles during LOS calculation (if threaded)

### Performance Notes

- **Override Tracking:** Uses Dictionary\<IntPtr, TileOverride\> for O(1) lookup
- **Auto-Restore:** UpdateOverrides() iterates all active overrides each turn (O(n))
- **Per-Actor Overrides:** Nested dictionaries for per-tile, per-actor tracking

### Limitations

**No Sub-Tile Positioning:**
Tile system is grid-based (no fractional positions). For smooth movement, actors interpolate between tile centers.

**Cover vs. Facing:**
Cover values are directional but don't account for actor facing. An actor facing north gets cover based on attacker's direction, not actor's facing.

**Enterability vs. Traversability:**
- **Traversability (flags bit 0):** Can the tile be walked on at all?
- **Enterability (m_IsEnterable):** Can the tile be entered from a specific direction?

Both must be satisfied for movement.

### Future Research

1. Document remaining flags bits (bits 1-31 at 0x1C)
2. Research cover calculation algorithm (how cover affects hit chance)
3. Map tile height/elevation system (if present)
4. Document tile effects (fire, smoke, etc.) and their counter integration
5. Research dynamic tile destruction (walls, structures)
6. Map tile visibility/fog-of-war system
7. Document tile event system (on enter, on exit triggers)

## See Also

- [tile-map-system.md](tile-map-system.md) - TileMap grid structure and navigation
- [pathfinding-system.md](pathfinding-system.md) - How pathfinding uses tile properties
- [line-of-sight.md](line-of-sight.md) - LOS calculation and blocker system
- [tile-effects.md](tile-effects.md) - Tile effects (fire, smoke, etc.)
