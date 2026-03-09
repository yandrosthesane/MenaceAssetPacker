using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Menace.SDK;

/// <summary>
/// SDK module for manipulating tile properties during tactical combat.
/// Provides methods to temporarily or permanently modify tile traversability, cover,
/// line-of-sight blocking, and movement blocking properties.
///
/// Based on reverse engineering findings from Ghidra analysis:
/// - Tile.flags @ +0x1C (bitfield, bit 0 = IsBlocked/NotTraversable)
/// - Tile.m_CoverValues @ +0x28 (int32[] array, 8 directions)
/// - Tile.m_BlocksMovement @ +0x38 (byte[] array, 8 directions, counter-based)
/// - Tile.m_IsEnterable @ +0x30 (bool[] array, 4 directions)
/// - Tile.LOSBlockerCounter @ +0x60 (byte, counter for LOS blocking)
/// - Tile.IsTraversable() - Returns !(flags &amp; 0x1)
/// - Tile.BlockLineOfSight() @ 0x1805cae00 - Increments counter at +0x60
/// - Tile.AddMovementBlocked(direction) - Increments counter at +0x38[direction]
///
/// DIRECTION ENCODING:
/// - 0 = North, 1 = NE, 2 = East, 3 = SE, 4 = South, 5 = SW, 6 = West, 7 = NW
/// - For IsEnterable (4 directions): 0 = North, 1 = East, 2 = South, 3 = West
///
/// COVER VALUES:
/// - 0 = None, 1 = Half/Light, 2 = Full/Heavy, 3+ = Enhanced
///
/// TEMPORARY OVERRIDES:
/// - Use turns parameter to specify duration (turns = -1 for permanent)
/// - Overrides are automatically restored after N turns via OnTurnEnd hook
/// - Multiple overrides on same tile will replace previous override
/// - Call UpdateOverrides() from TacticalEventHooks.OnTurnEnd to handle expiration
///
/// USAGE EXAMPLES:
/// <code>
/// // Make tile temporarily traversable for 2 turns
/// TileManipulation.SetTraversableOverride(tile, true, 2);
///
/// // Set full cover to the north for 3 turns
/// TileManipulation.SetCoverOverride(tile, 0, 2, 3);
///
/// // Permanently block line of sight through tile
/// TileManipulation.SetBlocksLOS(tile, true, -1);
///
/// // Make tile enterable only by specific actor
/// TileManipulation.SetEnterableBy(tile, actor, true);
/// </code>
/// </summary>
public static class TileManipulation
{
    // Tile field offsets from Ghidra findings
    private const uint OFFSET_TILE_FLAGS = 0x1C;
    private const uint OFFSET_TILE_COVER_VALUES = 0x28;
    private const uint OFFSET_TILE_IS_ENTERABLE = 0x30;
    private const uint OFFSET_TILE_BLOCKS_MOVEMENT = 0x38;
    private const uint OFFSET_TILE_LOS_BLOCKER_COUNTER = 0x60;

    // Tile flags
    private const uint FLAG_BLOCKED = 0x01;  // Bit 0: tile is not traversable

    // Array sizes
    private const int DIRECTIONS_8 = 8;
    private const int DIRECTIONS_4 = 4;

    // Override storage
    private static Dictionary<IntPtr, TileOverride> _overrides = new();
    private static Dictionary<IntPtr, Dictionary<IntPtr, EnterableOverride>> _enterableByActorOverrides = new();

    /// <summary>
    /// Tile override data for temporary modifications.
    /// </summary>
    private class TileOverride
    {
        public uint? OriginalFlags { get; set; }
        public int[] OriginalCoverValues { get; set; }
        public bool[] OriginalIsEnterable { get; set; }
        public byte OriginalLOSBlockerCounter { get; set; }
        public byte[] OriginalBlocksMovement { get; set; }
        public int TurnsRemaining { get; set; }
        public OverrideType Type { get; set; }
    }

    /// <summary>
    /// Per-actor enterable override.
    /// </summary>
    private class EnterableOverride
    {
        public bool OriginalValue { get; set; }
        public bool OverrideValue { get; set; }
    }

    [Flags]
    private enum OverrideType
    {
        None = 0,
        Traversable = 1,
        Cover = 2,
        Enterable = 4,
        LOS = 8,
        Movement = 16
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  PUBLIC API - Tile Manipulation Methods
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Set whether a tile is traversable (can be walked on).
    /// </summary>
    /// <param name="tile">The tile to modify</param>
    /// <param name="traversable">True to make traversable, false to block</param>
    /// <param name="turns">Number of turns before reverting (-1 for permanent)</param>
    /// <returns>True if successful</returns>
    /// <example>
    /// // Temporarily block a tile for 3 turns
    /// TileManipulation.SetTraversableOverride(tile, false, 3);
    ///
    /// // Permanently make a tile walkable
    /// TileManipulation.SetTraversableOverride(tile, true, -1);
    /// </example>
    public static bool SetTraversableOverride(GameObj tile, bool traversable, int turns = -1)
    {
        if (tile.IsNull)
        {
            ModError.WarnInternal("TileManipulation.SetTraversableOverride", "Tile is null");
            return false;
        }

        if (turns == 0)
        {
            ModError.WarnInternal("TileManipulation.SetTraversableOverride", "turns=0 is invalid, use -1 for permanent or >0 for temporary");
            return false;
        }

        try
        {
            // Read current flags
            var flags = (uint)Marshal.ReadInt32(tile.Pointer + (int)OFFSET_TILE_FLAGS);
            var originalFlags = flags;

            // Modify bit 0 (FLAG_BLOCKED)
            if (traversable)
                flags &= ~FLAG_BLOCKED;  // Clear bit 0 (make traversable)
            else
                flags |= FLAG_BLOCKED;   // Set bit 0 (block traversal)

            // Write modified flags back
            Marshal.WriteInt32(tile.Pointer + (int)OFFSET_TILE_FLAGS, (int)flags);

            // Store override if temporary
            if (turns > 0)
            {
                var over = GetOrCreateOverride(tile.Pointer);
                if (over.OriginalFlags == null)
                    over.OriginalFlags = originalFlags;
                over.TurnsRemaining = turns;
                over.Type |= OverrideType.Traversable;
            }

            return true;
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.SetTraversableOverride", "Failed to modify tile", ex);
            return false;
        }
    }

    /// <summary>
    /// Clear traversable override and restore original state.
    /// </summary>
    /// <param name="tile">The tile to restore</param>
    /// <returns>True if successful</returns>
    public static bool ClearTraversableOverride(GameObj tile)
    {
        if (tile.IsNull)
            return false;

        try
        {
            if (!_overrides.TryGetValue(tile.Pointer, out var over) || over.OriginalFlags == null)
                return false;

            // Restore original flags
            Marshal.WriteInt32(tile.Pointer + (int)OFFSET_TILE_FLAGS, (int)over.OriginalFlags.Value);
            over.Type &= ~OverrideType.Traversable;
            over.OriginalFlags = null;

            // Remove override if no more active types
            if (over.Type == OverrideType.None)
                _overrides.Remove(tile.Pointer);

            return true;
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.ClearTraversableOverride", "Failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Set cover value for a specific direction.
    /// </summary>
    /// <param name="tile">The tile to modify</param>
    /// <param name="direction">Direction index (0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW)</param>
    /// <param name="cover">Cover value (0=None, 1=Half, 2=Full, 3+=Enhanced)</param>
    /// <param name="turns">Number of turns before reverting (-1 for permanent)</param>
    /// <returns>True if successful</returns>
    /// <example>
    /// // Add full cover to the north for 2 turns
    /// TileManipulation.SetCoverOverride(tile, TileMap.DIR_NORTH, 2, 2);
    ///
    /// // Remove cover from the east permanently
    /// TileManipulation.SetCoverOverride(tile, TileMap.DIR_EAST, 0, -1);
    /// </example>
    public static bool SetCoverOverride(GameObj tile, int direction, int cover, int turns = -1)
    {
        if (tile.IsNull)
        {
            ModError.WarnInternal("TileManipulation.SetCoverOverride", "Tile is null");
            return false;
        }

        if (direction < 0 || direction >= DIRECTIONS_8)
        {
            ModError.WarnInternal("TileManipulation.SetCoverOverride", $"Invalid direction {direction}, must be 0-7");
            return false;
        }

        if (turns == 0)
        {
            ModError.WarnInternal("TileManipulation.SetCoverOverride", "turns=0 is invalid, use -1 for permanent or >0 for temporary");
            return false;
        }

        try
        {
            // Calculate offset for the specific direction in the cover array
            // m_CoverValues is int32[] with 8 elements
            var coverArrayOffset = OFFSET_TILE_COVER_VALUES + (direction * sizeof(int));

            // Store original values if temporary override
            if (turns > 0)
            {
                var over = GetOrCreateOverride(tile.Pointer);
                if (over.OriginalCoverValues == null)
                {
                    // Store all original cover values
                    over.OriginalCoverValues = new int[DIRECTIONS_8];
                    for (int i = 0; i < DIRECTIONS_8; i++)
                    {
                        over.OriginalCoverValues[i] = Marshal.ReadInt32(
                            tile.Pointer + (int)(OFFSET_TILE_COVER_VALUES + i * sizeof(int)));
                    }
                }
                over.TurnsRemaining = turns;
                over.Type |= OverrideType.Cover;
            }

            // Write new cover value
            Marshal.WriteInt32(tile.Pointer + (int)coverArrayOffset, cover);

            return true;
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.SetCoverOverride", "Failed to modify cover", ex);
            return false;
        }
    }

    /// <summary>
    /// Clear all cover overrides and restore original values.
    /// </summary>
    /// <param name="tile">The tile to restore</param>
    /// <returns>True if successful</returns>
    public static bool ClearCoverOverrides(GameObj tile)
    {
        if (tile.IsNull)
            return false;

        try
        {
            if (!_overrides.TryGetValue(tile.Pointer, out var over) || over.OriginalCoverValues == null)
                return false;

            // Restore all original cover values
            for (int i = 0; i < DIRECTIONS_8; i++)
            {
                var offset = OFFSET_TILE_COVER_VALUES + (i * sizeof(int));
                Marshal.WriteInt32(tile.Pointer + (int)offset, over.OriginalCoverValues[i]);
            }

            over.Type &= ~OverrideType.Cover;
            over.OriginalCoverValues = null;

            // Remove override if no more active types
            if (over.Type == OverrideType.None)
                _overrides.Remove(tile.Pointer);

            return true;
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.ClearCoverOverrides", "Failed", ex);
            return false;
        }
    }

    /// <summary>
    /// Set whether a tile is enterable from cardinal directions.
    /// </summary>
    /// <param name="tile">The tile to modify</param>
    /// <param name="enterable">True to allow entry, false to block</param>
    /// <param name="turns">Number of turns before reverting (-1 for permanent)</param>
    /// <returns>True if successful</returns>
    /// <remarks>
    /// This affects all 4 cardinal directions (N, E, S, W).
    /// For per-direction control, modify m_IsEnterable array directly.
    /// </remarks>
    /// <example>
    /// // Block entry to tile for 1 turn
    /// TileManipulation.SetEnterable(tile, false, 1);
    /// </example>
    public static bool SetEnterable(GameObj tile, bool enterable, int turns = -1)
    {
        if (tile.IsNull)
        {
            ModError.WarnInternal("TileManipulation.SetEnterable", "Tile is null");
            return false;
        }

        if (turns == 0)
        {
            ModError.WarnInternal("TileManipulation.SetEnterable", "turns=0 is invalid, use -1 for permanent or >0 for temporary");
            return false;
        }

        try
        {
            // m_IsEnterable is bool[] with 4 elements (cardinal directions)
            if (turns > 0)
            {
                var over = GetOrCreateOverride(tile.Pointer);
                if (over.OriginalIsEnterable == null)
                {
                    // Store original values
                    over.OriginalIsEnterable = new bool[DIRECTIONS_4];
                    for (int i = 0; i < DIRECTIONS_4; i++)
                    {
                        over.OriginalIsEnterable[i] = Marshal.ReadByte(
                            tile.Pointer + (int)(OFFSET_TILE_IS_ENTERABLE + i)) != 0;
                    }
                }
                over.TurnsRemaining = turns;
                over.Type |= OverrideType.Enterable;
            }

            // Write new values for all 4 directions
            byte value = enterable ? (byte)1 : (byte)0;
            for (int i = 0; i < DIRECTIONS_4; i++)
            {
                Marshal.WriteByte(tile.Pointer + (int)(OFFSET_TILE_IS_ENTERABLE + i), value);
            }

            return true;
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.SetEnterable", "Failed to modify enterable", ex);
            return false;
        }
    }

    /// <summary>
    /// Set whether a tile is enterable by a specific actor.
    /// Creates a per-actor override that takes precedence over global tile settings.
    /// </summary>
    /// <param name="tile">The tile to modify</param>
    /// <param name="actor">The actor to grant/deny access</param>
    /// <param name="enterable">True to allow entry, false to block</param>
    /// <returns>True if successful</returns>
    /// <remarks>
    /// This creates a permanent per-actor override. To remove, call ClearEnterableByActor().
    /// Per-actor overrides are checked before global tile enterable state.
    /// </remarks>
    /// <example>
    /// // Allow specific actor to enter blocked tile
    /// TileManipulation.SetEnterableBy(tile, specialActor, true);
    ///
    /// // Block specific actor from entering tile
    /// TileManipulation.SetEnterableBy(tile, enemy, false);
    /// </example>
    public static bool SetEnterableBy(GameObj tile, GameObj actor, bool enterable)
    {
        if (tile.IsNull || actor.IsNull)
        {
            ModError.WarnInternal("TileManipulation.SetEnterableBy", "Tile or actor is null");
            return false;
        }

        try
        {
            // Get or create per-tile actor override dictionary
            if (!_enterableByActorOverrides.TryGetValue(tile.Pointer, out var actorDict))
            {
                actorDict = new Dictionary<IntPtr, EnterableOverride>();
                _enterableByActorOverrides[tile.Pointer] = actorDict;
            }

            // Store override for this actor
            if (!actorDict.TryGetValue(actor.Pointer, out var over))
            {
                over = new EnterableOverride();
                actorDict[actor.Pointer] = over;
            }

            over.OverrideValue = enterable;
            return true;
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.SetEnterableBy", "Failed to set per-actor override", ex);
            return false;
        }
    }

    /// <summary>
    /// Clear per-actor enterable override.
    /// </summary>
    /// <param name="tile">The tile to modify</param>
    /// <param name="actor">The actor to clear override for</param>
    /// <returns>True if override was found and cleared</returns>
    public static bool ClearEnterableByActor(GameObj tile, GameObj actor)
    {
        if (tile.IsNull || actor.IsNull)
            return false;

        if (_enterableByActorOverrides.TryGetValue(tile.Pointer, out var actorDict))
        {
            if (actorDict.Remove(actor.Pointer))
            {
                if (actorDict.Count == 0)
                    _enterableByActorOverrides.Remove(tile.Pointer);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a tile is enterable by a specific actor (respects per-actor overrides).
    /// </summary>
    /// <param name="tile">The tile to check</param>
    /// <param name="actor">The actor to check</param>
    /// <returns>True if actor can enter tile</returns>
    public static bool IsEnterableBy(GameObj tile, GameObj actor)
    {
        if (tile.IsNull || actor.IsNull)
            return false;

        // Check per-actor override first
        if (_enterableByActorOverrides.TryGetValue(tile.Pointer, out var actorDict))
        {
            if (actorDict.TryGetValue(actor.Pointer, out var over))
                return over.OverrideValue;
        }

        // Fall back to tile's default enterable state
        // Check first cardinal direction as representative
        try
        {
            return Marshal.ReadByte(tile.Pointer + (int)OFFSET_TILE_IS_ENTERABLE) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Set whether a tile blocks line of sight.
    /// Uses the counter-based system (increments/decrements LOSBlockerCounter).
    /// </summary>
    /// <param name="tile">The tile to modify</param>
    /// <param name="blocks">True to block LOS, false to unblock</param>
    /// <param name="turns">Number of turns before reverting (-1 for permanent)</param>
    /// <returns>True if successful</returns>
    /// <remarks>
    /// The game uses a counter system at +0x60. Each "add blocker" increments the counter,
    /// each "remove blocker" decrements it. LOS is blocked when counter > 0.
    /// This method increments/decrements appropriately and tracks temporary changes.
    /// </remarks>
    /// <example>
    /// // Block LOS through tile for 2 turns
    /// TileManipulation.SetBlocksLOS(tile, true, 2);
    ///
    /// // Permanently make tile transparent
    /// TileManipulation.SetBlocksLOS(tile, false, -1);
    /// </example>
    public static bool SetBlocksLOS(GameObj tile, bool blocks, int turns = -1)
    {
        if (tile.IsNull)
        {
            ModError.WarnInternal("TileManipulation.SetBlocksLOS", "Tile is null");
            return false;
        }

        if (turns == 0)
        {
            ModError.WarnInternal("TileManipulation.SetBlocksLOS", "turns=0 is invalid, use -1 for permanent or >0 for temporary");
            return false;
        }

        try
        {
            // Read current counter
            var counter = Marshal.ReadByte(tile.Pointer + (int)OFFSET_TILE_LOS_BLOCKER_COUNTER);
            var originalCounter = counter;

            // Modify counter
            if (blocks)
            {
                if (counter < 255)
                    counter++;
            }
            else
            {
                if (counter > 0)
                    counter--;
            }

            // Write back
            Marshal.WriteByte(tile.Pointer + (int)OFFSET_TILE_LOS_BLOCKER_COUNTER, counter);

            // Store override if temporary
            if (turns > 0)
            {
                var over = GetOrCreateOverride(tile.Pointer);
                over.OriginalLOSBlockerCounter = originalCounter;
                over.TurnsRemaining = turns;
                over.Type |= OverrideType.LOS;
            }

            return true;
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.SetBlocksLOS", "Failed to modify LOS blocker", ex);
            return false;
        }
    }

    /// <summary>
    /// Set whether a tile blocks movement in all directions.
    /// Uses the counter-based system (increments/decrements m_BlocksMovement counters).
    /// </summary>
    /// <param name="tile">The tile to modify</param>
    /// <param name="blocks">True to block movement, false to unblock</param>
    /// <param name="turns">Number of turns before reverting (-1 for permanent)</param>
    /// <returns>True if successful</returns>
    /// <remarks>
    /// The game uses per-direction counters at +0x38 (byte[8]).
    /// This method modifies all 8 direction counters simultaneously.
    /// For per-direction control, use SetBlocksMovementInDirection().
    /// </remarks>
    /// <example>
    /// // Block all movement through tile for 1 turn
    /// TileManipulation.SetBlocksMovement(tile, true, 1);
    /// </example>
    public static bool SetBlocksMovement(GameObj tile, bool blocks, int turns = -1)
    {
        if (tile.IsNull)
        {
            ModError.WarnInternal("TileManipulation.SetBlocksMovement", "Tile is null");
            return false;
        }

        if (turns == 0)
        {
            ModError.WarnInternal("TileManipulation.SetBlocksMovement", "turns=0 is invalid, use -1 for permanent or >0 for temporary");
            return false;
        }

        try
        {
            // m_BlocksMovement is byte[] with 8 elements (one per direction)
            if (turns > 0)
            {
                var over = GetOrCreateOverride(tile.Pointer);
                if (over.OriginalBlocksMovement == null)
                {
                    // Store original values
                    over.OriginalBlocksMovement = new byte[DIRECTIONS_8];
                    for (int i = 0; i < DIRECTIONS_8; i++)
                    {
                        over.OriginalBlocksMovement[i] = Marshal.ReadByte(
                            tile.Pointer + (int)(OFFSET_TILE_BLOCKS_MOVEMENT + i));
                    }
                }
                over.TurnsRemaining = turns;
                over.Type |= OverrideType.Movement;
            }

            // Modify all direction counters
            for (int i = 0; i < DIRECTIONS_8; i++)
            {
                var offset = OFFSET_TILE_BLOCKS_MOVEMENT + i;
                var counter = Marshal.ReadByte(tile.Pointer + (int)offset);

                if (blocks)
                {
                    if (counter < 255)
                        counter++;
                }
                else
                {
                    if (counter > 0)
                        counter--;
                }

                Marshal.WriteByte(tile.Pointer + (int)offset, counter);
            }

            return true;
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.SetBlocksMovement", "Failed to modify movement blockers", ex);
            return false;
        }
    }

    /// <summary>
    /// Set whether a tile blocks movement in a specific direction.
    /// </summary>
    /// <param name="tile">The tile to modify</param>
    /// <param name="direction">Direction index (0-7)</param>
    /// <param name="blocks">True to block movement, false to unblock</param>
    /// <param name="turns">Number of turns before reverting (-1 for permanent)</param>
    /// <returns>True if successful</returns>
    public static bool SetBlocksMovementInDirection(GameObj tile, int direction, bool blocks, int turns = -1)
    {
        if (tile.IsNull)
        {
            ModError.WarnInternal("TileManipulation.SetBlocksMovementInDirection", "Tile is null");
            return false;
        }

        if (direction < 0 || direction >= DIRECTIONS_8)
        {
            ModError.WarnInternal("TileManipulation.SetBlocksMovementInDirection", $"Invalid direction {direction}, must be 0-7");
            return false;
        }

        if (turns == 0)
        {
            ModError.WarnInternal("TileManipulation.SetBlocksMovementInDirection", "turns=0 is invalid, use -1 for permanent or >0 for temporary");
            return false;
        }

        try
        {
            var offset = OFFSET_TILE_BLOCKS_MOVEMENT + direction;

            if (turns > 0)
            {
                var over = GetOrCreateOverride(tile.Pointer);
                if (over.OriginalBlocksMovement == null)
                {
                    // Store all original values
                    over.OriginalBlocksMovement = new byte[DIRECTIONS_8];
                    for (int i = 0; i < DIRECTIONS_8; i++)
                    {
                        over.OriginalBlocksMovement[i] = Marshal.ReadByte(
                            tile.Pointer + (int)(OFFSET_TILE_BLOCKS_MOVEMENT + i));
                    }
                }
                over.TurnsRemaining = turns;
                over.Type |= OverrideType.Movement;
            }

            // Modify counter for this direction
            var counter = Marshal.ReadByte(tile.Pointer + (int)offset);

            if (blocks)
            {
                if (counter < 255)
                    counter++;
            }
            else
            {
                if (counter > 0)
                    counter--;
            }

            Marshal.WriteByte(tile.Pointer + (int)offset, counter);
            return true;
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.SetBlocksMovementInDirection", "Failed", ex);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  OVERRIDE MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Update all temporary overrides. Called automatically from TacticalEventHooks.OnTurnEnd.
    /// Decrements turn counters and restores original values when expired.
    /// </summary>
    internal static void UpdateOverrides()
    {
        var expired = new List<IntPtr>();

        foreach (var kvp in _overrides)
        {
            var tilePtr = kvp.Key;
            var over = kvp.Value;

            over.TurnsRemaining--;

            if (over.TurnsRemaining <= 0)
            {
                // Restore all overridden values
                RestoreOverride(tilePtr, over);
                expired.Add(tilePtr);
            }
        }

        // Remove expired overrides
        foreach (var ptr in expired)
        {
            _overrides.Remove(ptr);
        }
    }

    /// <summary>
    /// Clear all tile manipulation overrides immediately.
    /// </summary>
    public static void ClearAllOverrides()
    {
        foreach (var kvp in _overrides)
        {
            RestoreOverride(kvp.Key, kvp.Value);
        }

        _overrides.Clear();
        _enterableByActorOverrides.Clear();
    }

    /// <summary>
    /// Clear all overrides for a specific tile.
    /// </summary>
    /// <param name="tile">The tile to clear overrides for</param>
    /// <returns>True if overrides were found and cleared</returns>
    public static bool ClearTileOverrides(GameObj tile)
    {
        if (tile.IsNull)
            return false;

        bool hadOverride = false;

        if (_overrides.TryGetValue(tile.Pointer, out var over))
        {
            RestoreOverride(tile.Pointer, over);
            _overrides.Remove(tile.Pointer);
            hadOverride = true;
        }

        if (_enterableByActorOverrides.Remove(tile.Pointer))
        {
            hadOverride = true;
        }

        return hadOverride;
    }

    /// <summary>
    /// Get remaining turns for a tile's override.
    /// </summary>
    /// <param name="tile">The tile to check</param>
    /// <returns>Remaining turns, or -1 if no override exists</returns>
    public static int GetOverrideTurnsRemaining(GameObj tile)
    {
        if (tile.IsNull)
            return -1;

        if (_overrides.TryGetValue(tile.Pointer, out var over))
            return over.TurnsRemaining;

        return -1;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  INTERNAL HELPERS
    // ═══════════════════════════════════════════════════════════════════════════════

    private static TileOverride GetOrCreateOverride(IntPtr tilePtr)
    {
        if (!_overrides.TryGetValue(tilePtr, out var over))
        {
            over = new TileOverride { Type = OverrideType.None };
            _overrides[tilePtr] = over;
        }
        return over;
    }

    private static void RestoreOverride(IntPtr tilePtr, TileOverride over)
    {
        try
        {
            // Restore flags (traversable)
            if (over.OriginalFlags.HasValue)
            {
                Marshal.WriteInt32(tilePtr + (int)OFFSET_TILE_FLAGS, (int)over.OriginalFlags.Value);
            }

            // Restore cover values
            if (over.OriginalCoverValues != null)
            {
                for (int i = 0; i < DIRECTIONS_8; i++)
                {
                    var offset = OFFSET_TILE_COVER_VALUES + (i * sizeof(int));
                    Marshal.WriteInt32(tilePtr + (int)offset, over.OriginalCoverValues[i]);
                }
            }

            // Restore enterable values
            if (over.OriginalIsEnterable != null)
            {
                for (int i = 0; i < DIRECTIONS_4; i++)
                {
                    var value = over.OriginalIsEnterable[i] ? (byte)1 : (byte)0;
                    Marshal.WriteByte(tilePtr + (int)(OFFSET_TILE_IS_ENTERABLE + i), value);
                }
            }

            // Restore LOS blocker counter
            if ((over.Type & OverrideType.LOS) != 0)
            {
                Marshal.WriteByte(tilePtr + (int)OFFSET_TILE_LOS_BLOCKER_COUNTER, over.OriginalLOSBlockerCounter);
            }

            // Restore movement blockers
            if (over.OriginalBlocksMovement != null)
            {
                for (int i = 0; i < DIRECTIONS_8; i++)
                {
                    Marshal.WriteByte(tilePtr + (int)(OFFSET_TILE_BLOCKS_MOVEMENT + i), over.OriginalBlocksMovement[i]);
                }
            }
        }
        catch (Exception ex)
        {
            ModError.ReportInternal("TileManipulation.RestoreOverride", "Failed to restore tile state", ex);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    //  INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initialize tile manipulation system. Called automatically from ModpackLoaderMod.
    /// Sets up OnTurnEnd hook to handle temporary overrides.
    /// </summary>
    internal static void Initialize()
    {
        // Hook into turn end to update overrides
        TacticalEventHooks.OnTurnEnd += (actorPtr) =>
        {
            UpdateOverrides();
        };
    }
}
