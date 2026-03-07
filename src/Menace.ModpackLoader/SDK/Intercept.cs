using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace Menace.SDK;

// ═══════════════════════════════════════════════════════════════════════════════
//  DELEGATE TYPES - Define signatures for each interceptor
// ═══════════════════════════════════════════════════════════════════════════════

#region Delegate Types

/// <summary>
/// Interceptor for float properties (Damage, Accuracy base values).
/// </summary>
/// <param name="props">The EntityProperties instance being queried</param>
/// <param name="owner">The Entity that owns these properties (may be null)</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void FloatIntercept(GameObj props, GameObj owner, ref float result);

/// <summary>
/// Interceptor for integer properties (Armor, Concealment, Detection, Vision).
/// </summary>
/// <param name="props">The EntityProperties instance being queried</param>
/// <param name="owner">The Entity that owns these properties (may be null)</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void IntIntercept(GameObj props, GameObj owner, ref int result);

/// <summary>
/// Result structure for hit chance calculations.
/// Contains all components that make up the final hit chance.
/// </summary>
public struct HitChanceResult
{
    /// <summary>Final computed hit chance (0.0 to 1.0, or 0 to 100 depending on game version)</summary>
    public float FinalChance;
    /// <summary>Base accuracy from attacker's EntityProperties</summary>
    public float BaseAccuracy;
    /// <summary>Cover multiplier applied to target (1.0 = no cover)</summary>
    public float CoverMult;
    /// <summary>Evasion/defense multiplier from target</summary>
    public float DefenseMult;
    /// <summary>Distance penalty applied</summary>
    public float DistancePenalty;
    /// <summary>Whether distance falloff was applied</summary>
    public bool HasDistanceFalloff;
    /// <summary>Whether target is guaranteed hit (e.g., AlwaysHit skill flag)</summary>
    public bool IsGuaranteedHit;
}

/// <summary>
/// Interceptor for skill hit chance calculations.
/// </summary>
/// <param name="skill">The skill being used</param>
/// <param name="attacker">The attacking entity</param>
/// <param name="target">The target entity (may be null for tile-targeted skills)</param>
/// <param name="result">The hit chance result - modify components to change calculation</param>
public delegate void HitChanceInterceptor(GameObj skill, GameObj attacker, GameObj target, ref HitChanceResult result);

/// <summary>
/// Result structure for expected damage calculations.
/// </summary>
public struct ExpectedDamageResult
{
    /// <summary>Final expected damage value</summary>
    public float Damage;
    /// <summary>Base damage from weapon/skill</summary>
    public float BaseDamage;
    /// <summary>Damage multiplier from attacker properties</summary>
    public float DamageMult;
    /// <summary>Armor penetration value</summary>
    public float ArmorPenetration;
    /// <summary>Target's armor value</summary>
    public float TargetArmor;
}

/// <summary>
/// Interceptor for skill expected damage calculations.
/// </summary>
/// <param name="skill">The skill being used</param>
/// <param name="attacker">The attacking entity</param>
/// <param name="target">The target entity</param>
/// <param name="result">The expected damage result - modify to change calculation</param>
public delegate void ExpectedDamageInterceptor(GameObj skill, GameObj attacker, GameObj target, ref ExpectedDamageResult result);

/// <summary>
/// Interceptor for cover multiplier calculations.
/// </summary>
/// <param name="skill">The skill being used</param>
/// <param name="attacker">The attacking entity</param>
/// <param name="target">The target entity</param>
/// <param name="result">The cover multiplier (1.0 = no cover effect)</param>
public delegate void CoverMultInterceptor(GameObj skill, GameObj attacker, GameObj target, ref float result);

/// <summary>
/// Interceptor for line of sight checks.
/// </summary>
/// <param name="observer">The observing actor</param>
/// <param name="target">The target entity being observed</param>
/// <param name="result">Whether line of sight exists - set to false to block, true to grant</param>
public delegate void LineOfSightInterceptor(GameObj observer, GameObj target, ref bool result);

/// <summary>
/// Interceptor for boolean skill properties (IsMovementSkill, IsInRange, etc.).
/// </summary>
/// <param name="skill">The skill instance being queried</param>
/// <param name="result">The boolean result - modify via ref to change the value</param>
public delegate void BoolInterceptor(GameObj skill, ref bool result);

/// <summary>
/// Interceptor for float skill properties (GetExpectedSuppression).
/// </summary>
/// <param name="skill">The skill instance being queried</param>
/// <param name="result">The float result - modify via ref to change the value</param>
public delegate void FloatSkillInterceptor(GameObj skill, ref float result);

/// <summary>
/// Interceptor for integer skill properties (GetActionPointCost, range values).
/// </summary>
/// <param name="skill">The skill instance being queried</param>
/// <param name="result">The integer result - modify via ref to change the value</param>
public delegate void IntSkillInterceptor(GameObj skill, ref int result);

/// <summary>
/// Interceptor for Entity float state methods (health percentage, armor durability).
/// </summary>
/// <param name="entity">The Entity instance being queried</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void EntityFloatStateInterceptor(GameObj entity, ref float result);

/// <summary>
/// Interceptor for Entity int/enum state methods (cover usage).
/// </summary>
/// <param name="entity">The Entity instance being queried</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void EntityIntStateInterceptor(GameObj entity, ref int result);

/// <summary>
/// Interceptor for Entity bool state methods (discovery state).
/// </summary>
/// <param name="entity">The Entity instance being queried</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void EntityBoolStateInterceptor(GameObj entity, ref bool result);

/// <summary>
/// Interceptor for Entity object state methods (Cover, Skill).
/// Returns IntPtr to allow modders to cast to appropriate type.
/// </summary>
/// <param name="entity">The Entity instance being queried</param>
/// <param name="result">The computed result pointer - modify via ref to change the value</param>
public delegate void EntityObjectStateInterceptor(GameObj entity, ref IntPtr result);

/// <summary>
/// Result structure for Vector2 values (scale range, etc.).
/// </summary>
public struct Vector2Result
{
    /// <summary>X component (min for range)</summary>
    public float X;
    /// <summary>Y component (max for range)</summary>
    public float Y;
}

/// <summary>
/// Interceptor for Entity Vector2 state methods (scale range).
/// </summary>
/// <param name="entity">The Entity instance being queried</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void EntityVector2StateInterceptor(GameObj entity, ref Vector2Result result);

/// <summary>
/// Interceptor for generic property value getter (takes EntityPropertyType parameter).
/// </summary>
/// <param name="props">The EntityProperties instance being queried</param>
/// <param name="owner">The Entity that owns these properties (may be null)</param>
/// <param name="propertyType">The EntityPropertyType enum value being queried</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void PropertyValueInterceptor(GameObj props, GameObj owner, int propertyType, ref float result);

// ═══════════════════════════════════════════════════════════════════════════════
//  TILE/MAP DELEGATE TYPES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interceptor for tile-to-tile line of sight checks (Tile.HasLineOfSightTo).
/// </summary>
/// <param name="fromTile">The source tile</param>
/// <param name="toTile">The destination tile</param>
/// <param name="result">Whether LoS exists between tiles</param>
public delegate void TileLoSInterceptor(GameObj fromTile, GameObj toTile, ref bool result);

/// <summary>
/// Interceptor for tile LoS blocker checks (Tile.IsBlockingLineOfSight).
/// </summary>
/// <param name="tile">The tile being checked</param>
/// <param name="result">Whether the tile blocks line of sight</param>
public delegate void TileBlockerInterceptor(GameObj tile, ref bool result);

/// <summary>
/// Interceptor for tile cover queries (Tile.GetCover).
/// </summary>
/// <param name="tile">The tile being queried</param>
/// <param name="direction">The direction (0-7) for cover check</param>
/// <param name="entity">The entity checking cover (may be null)</param>
/// <param name="result">The cover value (0-3)</param>
public delegate void TileCoverInterceptor(GameObj tile, int direction, GameObj entity, ref int result);

/// <summary>
/// Interceptor for tile cover mask queries (Tile.GetCoverMask).
/// </summary>
/// <param name="tile">The tile being queried</param>
/// <param name="result">Bitmask of cover directions (bits 0-3 = N/E/S/W)</param>
public delegate void TileCoverMaskInterceptor(GameObj tile, ref int result);

/// <summary>
/// Interceptor for entity-provided cover queries (Tile.GetEntityProvidedCover).
/// </summary>
/// <param name="tile">The tile being queried</param>
/// <param name="result">The cover value from entities on the tile</param>
public delegate void TileEntityCoverInterceptor(GameObj tile, ref int result);

/// <summary>
/// Interceptor for tile entry permission checks (Tile.CanBeEntered).
/// </summary>
/// <param name="tile">The tile being checked</param>
/// <param name="result">Whether the tile can be entered</param>
public delegate void TileEntryInterceptor(GameObj tile, ref bool result);

/// <summary>
/// Interceptor for entity-specific tile entry checks (Tile.CanBeEnteredBy).
/// </summary>
/// <param name="tile">The tile being checked</param>
/// <param name="entity">The entity attempting to enter</param>
/// <param name="result">Whether the entity can enter the tile</param>
public delegate void TileEntityEntryInterceptor(GameObj tile, GameObj entity, ref bool result);

/// <summary>
/// Interceptor for cover presence checks (BaseTile.HasCover).
/// </summary>
/// <param name="tile">The tile being checked</param>
/// <param name="result">Whether the tile has any cover</param>
public delegate void BaseTileCoverCheckInterceptor(GameObj tile, ref bool result);

/// <summary>
/// Interceptor for half cover checks (BaseTile.HasHalfCover).
/// </summary>
/// <param name="tile">The tile being checked</param>
/// <param name="result">Whether the tile has half cover</param>
public delegate void BaseTileHalfCoverInterceptor(GameObj tile, ref bool result);

/// <summary>
/// Interceptor for directional half cover checks (BaseTile.HasHalfCoverInDir).
/// </summary>
/// <param name="tile">The tile being checked</param>
/// <param name="direction">The direction to check (0-7)</param>
/// <param name="result">Whether half cover exists in that direction</param>
public delegate void BaseTileDirHalfCoverInterceptor(GameObj tile, int direction, ref bool result);

/// <summary>
/// Interceptor for movement blocking checks (BaseTile.IsMovementBlocked).
/// </summary>
/// <param name="tile">The tile being checked</param>
/// <param name="direction">The direction to check (0-7)</param>
/// <param name="result">Whether movement is blocked in that direction</param>
public delegate void BaseTileMovementBlockedInterceptor(GameObj tile, int direction, ref bool result);

/// <summary>
/// Interceptor for core LoS algorithm (LineOfSight.HasLineOfSight / RayTrace).
/// Static method - no instance parameter.
/// </summary>
/// <param name="fromTile">Source tile</param>
/// <param name="toTile">Target tile</param>
/// <param name="flags">LoS flags</param>
/// <param name="result">Whether LoS exists</param>
public delegate void LineOfSightRayTraceInterceptor(GameObj fromTile, GameObj toTile, int flags, ref bool result);

/// <summary>
/// Interceptor for corner detection (LineOfSight.IsNearTileCorner).
/// Static method - checks if position is near a tile corner.
/// </summary>
/// <param name="posX">X coordinate of position</param>
/// <param name="posY">Y coordinate of position</param>
/// <param name="result">Whether the position is near a corner</param>
public delegate void LineOfSightCornerInterceptor(float posX, float posY, ref bool result);

// -------------------------------------------------------------------------------
//  ACTOR STATE QUERY DELEGATES
// -------------------------------------------------------------------------------

/// <summary>
/// Interceptor for Actor integer state queries (morale state, tiles moved, etc.).
/// </summary>
/// <param name="actor">The Actor being queried</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void ActorIntStateInterceptor(GameObj actor, ref int result);

/// <summary>
/// Interceptor for Actor float state queries (morale percentage, suppression percentage).
/// </summary>
/// <param name="actor">The Actor being queried</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void ActorFloatStateInterceptor(GameObj actor, ref float result);

/// <summary>
/// Interceptor for Actor boolean state queries (IsActive, IsDying, IsStunned, etc.).
/// </summary>
/// <param name="actor">The Actor being queried</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void ActorBoolStateInterceptor(GameObj actor, ref bool result);

/// <summary>
/// Interceptor for Actor.IsDetectedByFaction which takes a faction parameter.
/// </summary>
/// <param name="actor">The Actor being queried</param>
/// <param name="faction">The faction ID checking detection</param>
/// <param name="result">Whether the actor is detected by the faction</param>
public delegate void ActorFactionDetectionInterceptor(GameObj actor, int faction, ref bool result);

/// <summary>
/// Interceptor for Actor.GetTurningCost which takes a target direction parameter.
/// </summary>
/// <param name="actor">The Actor being queried</param>
/// <param name="targetDirection">The target direction (0-7)</param>
/// <param name="result">The AP cost to turn to that direction</param>
public delegate void ActorTurningCostInterceptor(GameObj actor, int targetDirection, ref int result);

/// <summary>
/// Interceptor for Actor.GetSuppressionState which takes an additional suppression parameter.
/// </summary>
/// <param name="actor">The Actor being queried</param>
/// <param name="additionalSuppression">Additional suppression to factor in</param>
/// <param name="result">The suppression state (0=Normal, 1=Suppressed, 2=PinnedDown)</param>
public delegate void ActorSuppressionStateInterceptor(GameObj actor, float additionalSuppression, ref int result);

/// <summary>
/// Interceptor for Actor.GetMoraleMax which takes a multiplier parameter.
/// </summary>
/// <param name="actor">The Actor being queried</param>
/// <param name="multiplier">External multiplier applied to the result</param>
/// <param name="result">The maximum morale value</param>
public delegate void ActorMoraleMaxInterceptor(GameObj actor, float multiplier, ref int result);

// ═══════════════════════════════════════════════════════════════════════════════
//  MOVEMENT DELEGATE TYPES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interceptor for MovementType float properties that take a MovementMode parameter.
/// </summary>
/// <param name="movementType">The MovementType instance being queried</param>
/// <param name="movementMode">The movement mode (0=Walk, 1=Run, 2=Sprint)</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void MovementFloatInterceptor(GameObj movementType, int movementMode, ref float result);

/// <summary>
/// Interceptor for path cost calculations.
/// </summary>
/// <param name="movementType">The MovementType instance</param>
/// <param name="path">The path being evaluated</param>
/// <param name="actor">The actor traversing the path (may be null)</param>
/// <param name="result">The computed path cost - modify via ref to change the value</param>
public delegate void PathCostInterceptor(GameObj movementType, GameObj path, GameObj actor, ref int result);

/// <summary>
/// Result structure for path clipping operations.
/// </summary>
public struct ClipPathResult
{
    /// <summary>The actual cost consumed after clipping</summary>
    public int ActualCost;
    /// <summary>The maximum cost budget that was provided</summary>
    public int MaxCost;
    /// <summary>The index in the path where clipping occurred (-1 if not clipped)</summary>
    public int ClipIndex;
}

/// <summary>
/// Interceptor for path clipping by AP cost.
/// </summary>
/// <param name="movementType">The MovementType instance</param>
/// <param name="path">The path being clipped</param>
/// <param name="actor">The actor traversing the path</param>
/// <param name="result">The clip result - modify to change behavior</param>
public delegate void ClipPathInterceptor(GameObj movementType, GameObj path, GameObj actor, ref ClipPathResult result);

// ═══════════════════════════════════════════════════════════════════════════════
//  STRATEGY LAYER DELEGATE TYPES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interceptor for Strategy layer integer properties (AP, HP, Armor).
/// </summary>
/// <param name="instance">The Strategy object instance (UnitLeaderAttributes, BaseUnitLeader, Vehicle)</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void StrategyIntIntercept(GameObj instance, ref int result);

/// <summary>
/// Interceptor for Strategy layer float properties (damage mult, health pct).
/// </summary>
/// <param name="instance">The Strategy object instance</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void StrategyFloatIntercept(GameObj instance, ref float result);

/// <summary>
/// Interceptor for Strategy layer boolean properties (promotion/demotion eligibility).
/// </summary>
/// <param name="instance">The Strategy object instance</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void StrategyBoolIntercept(GameObj instance, ref bool result);

/// <summary>
/// Interceptor for Strategy layer GetEntityProperty calls.
/// </summary>
/// <param name="instance">The BaseUnitLeader instance</param>
/// <param name="propertyType">The EntityPropertyType enum value being queried</param>
/// <param name="result">The computed result - modify via ref to change the value</param>
public delegate void StrategyEntityIntercept(GameObj instance, int propertyType, ref float result);

// ═══════════════════════════════════════════════════════════════════════════════
//  AI BEHAVIOR DELEGATE TYPES
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interceptor for AI float value calculations (attack scoring, threat evaluation).
/// </summary>
/// <param name="ai">The AI instance (AIBrain or evaluator)</param>
/// <param name="target">The target being evaluated (may be null)</param>
/// <param name="result">The computed value - modify via ref to change AI decision</param>
public delegate void AIFloatValueInterceptor(GameObj ai, GameObj target, ref float result);

/// <summary>
/// Interceptor for AI integer value calculations (priority scoring).
/// </summary>
/// <param name="ai">The AI instance</param>
/// <param name="target">The target being evaluated (may be null)</param>
/// <param name="result">The computed value - modify via ref to change AI decision</param>
public delegate void AIIntValueInterceptor(GameObj ai, GameObj target, ref int result);

/// <summary>
/// Interceptor for AI boolean decisions (should flee, should attack, etc.).
/// </summary>
/// <param name="ai">The AI instance</param>
/// <param name="context">Context object (skill, target, etc. - may be null)</param>
/// <param name="result">The decision result - modify via ref to change behavior</param>
public delegate void AIBoolDecisionInterceptor(GameObj ai, GameObj context, ref bool result);

#endregion

// ═══════════════════════════════════════════════════════════════════════════════
//  INTERCEPT - Central event registry and patch manager
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Central system for intercepting and modifying game property calculations.
///
/// This is the foundation of the Menace SDK's property modification system.
/// It allows modders to hook into property getters and modify results after
/// the game computes them, enabling powerful gameplay modifications.
///
/// Architecture:
/// - Static events for each interceptable property
/// - Automatic Harmony patching on Initialize()
/// - Dual C#/Lua support via LuaScriptEngine integration
/// - Context passing (entity owners, skill context, etc.)
///
/// C# Usage:
///   Intercept.OnGetDamage += (props, owner, ref result) => {
///       result *= 1.5f;  // 50% more damage
///   };
///
///   Intercept.OnGetConcealment += (props, owner, ref result) => {
///       if (IsNightTime()) result += 5;  // Bonus concealment at night
///   };
///
///   Intercept.OnGetHitChance += (skill, attacker, target, ref result) => {
///       result.FinalChance = Math.Min(result.FinalChance + 0.1f, 1.0f);  // +10% hit
///   };
///
/// Lua Usage:
///   on("property_damage", function(data)
///       data.result = data.result * 1.5
///   end)
///
/// Design Notes:
/// - All interceptors fire AFTER the game computes the base value
/// - Multiple interceptors stack (each sees the result of previous)
/// - Use ref parameters to modify values in place
/// - Owner resolution is best-effort (may be null in some contexts)
///
/// Based on Ghidra reverse engineering:
/// - EntityProperties.GetDamage @ 0x18060bd60: Damage (0x118) * DamageMult (0x11C)
/// - EntityProperties.GetAccuracy @ 0x18060b9f0: Accuracy (0x68) * AccuracyMult (0x6C)
/// - EntityProperties.GetArmor @ 0x18060bb40: max(0x1c, max(0x20, 0x24)) * ArmorMult (0x28)
/// - EntityProperties.GetConcealment @ 0x18060bc90: Concealment (0xD4) * ConcealMult (0xD8)
/// - EntityProperties.GetDetection @ 0x18060bd90: Detection (0xCC) * DetectMult (0xD0)
/// - Skill.GetHitchance @ 0x1806dba90: Complex calculation with accuracy, cover, defense
/// - Skill.GetCoverMult @ 0x1806d9bb0: Cover value lookup and evasion calculation
/// - Actor.HasLineOfSightTo @ 0x1805dfa10: Vision vs concealment + distance + cover
/// - Entity.GetHitpointsPct @ 0x180611720: Current HP / Max HP percentage
/// - Entity.GetArmorDurabilityPct @ 0x180610d10: Current armor / Max armor percentage
/// - Entity.GetCoverUsage @ 0x180610f10: Current cover state enum
/// - Entity.GetProvidedCover @ 0x180611870: Cover object this entity provides
/// - Entity.IsDiscovered @ 0x180612c90: Whether entity has been discovered
/// - Entity.GetLastSkillUsed @ 0x180611780: Last skill action tracking
/// - Entity.GetScaleRange @ 0x180611940: Min/max scale bounds as Vector2
/// </summary>
public static class Intercept
{
    private static bool _initialized;
    private static HarmonyLib.Harmony _harmony;

    // Cached types for performance
    private static Type _entityPropertiesType;
    private static Type _skillType;
    private static Type _actorType;
    private static Type _entityType;
    private static Type _tileType;
    private static Type _baseTileType;
    private static Type _lineOfSightType;
    // Movement types
    private static Type _movementTypeType;
    // Strategy layer types
    private static Type _unitLeaderAttributesType;
    private static Type _baseUnitLeaderType;
    private static Type _vehicleType;
    // AI types
    private static Type _aiBrainType;

    // Cached field offsets for owner resolution
    private const uint OFFSET_SKILL_CONTAINER = 0x18;  // Skill -> SkillContainer
    private const uint OFFSET_CONTAINER_ENTITY = 0x10; // SkillContainer -> Entity

    // ═══════════════════════════════════════════════════════════════════
    //  TIER 1 EVENTS - EntityProperties getters (most commonly needed)
    // ═══════════════════════════════════════════════════════════════════

    #region Tier 1: EntityProperties Events

    /// <summary>
    /// Fires after EntityProperties.GetDamage() computes the damage value.
    /// Formula: Damage (offset 0x118) * DamageMult (offset 0x11C, clamped to >=0)
    /// </summary>
    public static event FloatIntercept OnGetDamage;

    /// <summary>
    /// Fires after EntityProperties.GetAccuracy() computes the accuracy value.
    /// Formula: floor(Accuracy (0x68) * AccuracyMult (0x6C, clamped))
    /// </summary>
    public static event FloatIntercept OnGetAccuracy;

    /// <summary>
    /// Fires after EntityProperties.GetArmor() computes the armor value.
    /// Formula: max(Armor1, Armor2, Armor3) * ArmorMult (clamped)
    /// </summary>
    public static event IntIntercept OnGetArmor;

    /// <summary>
    /// Fires after EntityProperties.GetConcealment() computes the concealment value.
    /// Formula: floor(Concealment (0xD4) * ConcealMult (0xD8, clamped))
    /// Higher concealment = harder to detect.
    /// </summary>
    public static event IntIntercept OnGetConcealment;

    /// <summary>
    /// Fires after EntityProperties.GetDetection() computes the detection value.
    /// Formula: max(0, floor(Detection (0xCC) * DetectMult (0xD0, clamped)))
    /// Higher detection = better at spotting concealed units.
    /// </summary>
    public static event IntIntercept OnGetDetection;

    /// <summary>
    /// Fires after EntityProperties.GetVision() computes the vision range.
    /// Base vision at offset 0xC4, multiplier at 0xC8.
    /// </summary>
    public static event IntIntercept OnGetVision;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  TIER 1 EVENTS - Skill calculations (critical for combat mods)
    // ═══════════════════════════════════════════════════════════════════

    #region Tier 1: Skill Events

    /// <summary>
    /// Fires after Skill.GetHitchance() computes the hit chance.
    /// This is the central hit calculation combining accuracy, cover, evasion, and distance.
    /// Result contains all component values for fine-grained modification.
    /// </summary>
    public static event HitChanceInterceptor OnGetHitChance;

    /// <summary>
    /// Fires after Skill.GetExpectedDamage() computes expected damage.
    /// Factors in base damage, multipliers, armor penetration, and target armor.
    /// </summary>
    public static event ExpectedDamageInterceptor OnGetExpectedDamage;

    /// <summary>
    /// Fires after Skill.GetCoverMult() computes the cover multiplier.
    /// Lower values = more protection from cover.
    /// </summary>
    public static event CoverMultInterceptor OnGetCoverMult;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  TIER 1 EVENTS - Actor visibility (stealth/detection mods)
    // ═══════════════════════════════════════════════════════════════════

    #region Tier 1: Actor Events

    /// <summary>
    /// Fires after Actor.HasLineOfSightTo() determines visibility.
    /// Considers vision range, detection vs concealment, cover, and elevation.
    /// Set result to false to hide targets, true to reveal them.
    /// </summary>
    public static event LineOfSightInterceptor OnHasLineOfSightTo;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  ACTOR STATE QUERY EVENTS - Morale/Suppression
    // ═══════════════════════════════════════════════════════════════════

    #region Actor State: Morale/Suppression

    /// <summary>
    /// Fires after Actor.GetMoraleMax() computes maximum morale.
    /// Formula: (BaseMorale + BonusMorale) * MoraleMultiplier * param
    /// Address: 0x1805df330
    /// </summary>
    public static event ActorMoraleMaxInterceptor OnActorGetMoraleMax;

    /// <summary>
    /// Fires after Actor.GetMoralePct() computes morale percentage.
    /// Returns current morale (offset 0x160) divided by GetMoraleMax result.
    /// Address: 0x1805df4a0
    /// </summary>
    public static event ActorFloatStateInterceptor OnActorGetMoralePct;

    /// <summary>
    /// Fires after Actor.GetMoraleState() computes morale state.
    /// Returns: 1=Panicked (morale&lt;=0), 2=Shaken (morale&lt;=threshold), 3=Steady.
    /// Commanders (actor+0x4C=1) are immune to Panic.
    /// Address: 0x1805df4d0
    /// </summary>
    public static event ActorIntStateInterceptor OnActorGetMoraleState;

    /// <summary>
    /// Fires after Actor.GetSuppressionPct() computes suppression percentage.
    /// Returns suppression value (offset 0x15C) multiplied by a constant.
    /// Address: 0x1805df710
    /// </summary>
    public static event ActorFloatStateInterceptor OnActorGetSuppressionPct;

    /// <summary>
    /// Fires after Actor.GetSuppressionState() computes suppression state.
    /// Returns: 0=Normal, 1=Suppressed (if >= threshold1), 2=PinnedDown (if >= threshold2).
    /// Address: 0x1805df730
    /// </summary>
    public static event ActorSuppressionStateInterceptor OnActorGetSuppressionState;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  ACTOR STATE QUERY EVENTS - Action Points
    // ═══════════════════════════════════════════════════════════════════

    #region Actor State: Action Points

    /// <summary>
    /// Fires after Actor.GetActionPointsAtTurnStart() returns AP at turn start.
    /// Simple field read from offset 0x14C.
    /// Address: 0x1805df0c0
    /// </summary>
    public static event ActorIntStateInterceptor OnActorGetActionPointsAtTurnStart;

    /// <summary>
    /// Fires after Actor.GetTurningCost() computes AP cost to turn.
    /// Calculates based on direction difference and unit properties.
    /// Address: 0x1805df810
    /// </summary>
    public static event ActorTurningCostInterceptor OnActorGetTurningCost;

    /// <summary>
    /// Fires after Actor.GetTilesMovedThisTurn() returns tiles moved count.
    /// Simple field read from offset 0x154.
    /// Address: 0x1805df7c0
    /// </summary>
    public static event ActorIntStateInterceptor OnActorGetTilesMovedThisTurn;

    /// <summary>
    /// Fires after Actor.GetTimesAttackedSinceLastTurn() returns attack count.
    /// Simple field read from offset 0x140.
    /// Address: 0x1805df7d0
    /// </summary>
    public static event ActorIntStateInterceptor OnActorGetTimesAttackedSinceLastTurn;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  ACTOR STATE QUERY EVENTS - Boolean State Queries
    // ═══════════════════════════════════════════════════════════════════

    #region Actor State: Boolean Queries

    /// <summary>
    /// Fires after Actor.IsActive() checks if actor is the active/selected unit.
    /// Compares actor pointer against TacticalManager's active actor.
    /// Address: 0x1805e0110
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsActive;

    /// <summary>
    /// Fires after Actor.IsActionPointsSpent() checks if actor can still act.
    /// Complex check involving AP, skills, and movement options.
    /// Address: 0x1805dfe90
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsActionPointsSpent;

    /// <summary>
    /// Fires after Actor.IsDetectedByFaction() checks faction detection.
    /// Uses bitmask at offset 0x138 (DetectedByFactionMask).
    /// Address: 0x1805e0160
    /// </summary>
    public static event ActorFactionDetectionInterceptor OnActorIsDetectedByFaction;

    /// <summary>
    /// Fires after Actor.IsDying() checks if actor is in dying state.
    /// Simple field read from offset 0x16A.
    /// Address: 0x1805e0180
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsDying;

    /// <summary>
    /// Fires after Actor.IsHeavyWeaponDeployed() checks deployment state.
    /// Simple field read from offset 0x16F.
    /// Address: 0x1805e0190
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsHeavyWeaponDeployed;

    /// <summary>
    /// Fires after Actor.IsHiddenToAI() checks if hidden from AI factions.
    /// Uses cached state at offset 0x1A4 (0=unknown, 1=visible, 2=hidden).
    /// Address: 0x1805e01a0
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsHiddenToAI;

    /// <summary>
    /// Fires after Actor.IsHiddenToPlayer() checks if hidden from player.
    /// Delegates to IsHiddenToPlayerAtTile with current tile.
    /// Address: 0x1805e0750
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsHiddenToPlayer;

    /// <summary>
    /// Fires after Actor.IsInfantry() checks unit type.
    /// Returns true if UnitType (offset 0x88) == 2 and SubType (offset 0x8C) == 0.
    /// Address: 0x1805e0780
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsInfantry;

    /// <summary>
    /// Fires after Actor.IsLeavingMap() checks if actor is leaving the map.
    /// Simple field read from offset 0x16B.
    /// Address: 0x1805e07e0
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsLeavingMap;

    /// <summary>
    /// Fires after Actor.IsMinion() checks if actor is a minion.
    /// Returns true if MinionID (offset 0x1B0) != 0.
    /// Minions are spawned entities controlled by another actor's skills.
    /// Address: 0x1805e07f0
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsMinion;

    /// <summary>
    /// Fires after Actor.IsMoving() checks if actor is currently moving.
    /// Simple field read from offset 0x167.
    /// Address: 0x1805e0810
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsMoving;

    /// <summary>
    /// Fires after Actor.IsSelectableByPlayer() checks if player can select.
    /// Checks player control and turn-done status.
    /// Address: 0x1805e0820
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsSelectableByPlayer;

    /// <summary>
    /// Fires after Actor.IsStunned() checks stun status.
    /// Returns true if actor+0x16C flag set OR EntityProperties+0xEC bit 0 set.
    /// Address: 0x1805e0860
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsStunned;

    /// <summary>
    /// Fires after Actor.IsTurnDone() checks if turn is complete.
    /// Simple field read from offset 0x164.
    /// Address: 0x1805e08a0
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsTurnDone;

    /// <summary>
    /// Fires after Actor.IsTurret() checks if actor is a turret.
    /// Returns true if UnitType (offset 0x88) == 2 and SubType (offset 0x8C) == 2.
    /// Address: 0x1805e08b0
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsTurret;

    /// <summary>
    /// Fires after Actor.IsVehicle() checks if actor is a vehicle.
    /// Returns true if UnitType (offset 0x88) == 2 and SubType (offset 0x8C) == 1.
    /// Address: 0x1805e0900
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorIsVehicle;

    /// <summary>
    /// Fires after Actor.CanEnterAnyAdjacentVehicle() checks vehicle entry.
    /// Checks if infantry can enter an adjacent allied vehicle.
    /// Address: 0x1805de260
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorCanEnterAnyAdjacentVehicle;

    /// <summary>
    /// Fires after Actor.HasActed() checks if actor has acted this turn.
    /// Compares AP at turn start with current AP, or checks acted flag (offset 0x171).
    /// Address: 0x1805df9f0
    /// </summary>
    public static event ActorBoolStateInterceptor OnActorHasActed;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  TIER 2 EVENTS - Skill range and cost properties
    // ═══════════════════════════════════════════════════════════════════

    #region Tier 2: Skill Range and Cost Events

    /// <summary>
    /// Fires after Skill.GetExpectedSuppression() computes suppression preview.
    /// @ 0x1806db2d0: Complex calculation involving cover, discipline, and skill modifiers.
    /// </summary>
    public static event FloatSkillInterceptor OnGetExpectedSuppression;

    /// <summary>
    /// Fires after Skill.GetActionPointCost() computes the AP cost.
    /// @ 0x1806d8e80: Base cost (0xa0) modified by actor properties (0x44, 0x48).
    /// </summary>
    public static event IntSkillInterceptor OnGetActionPointCost;

    /// <summary>
    /// Fires after Skill.GetIdealRangeBase() computes the optimal engagement range.
    /// @ 0x1806dbea0: Returns weapon's ideal range (0x144) or skill template range (0x12C).
    /// </summary>
    public static event IntSkillInterceptor OnGetIdealRangeBase;

    /// <summary>
    /// Fires after Skill.GetMaxRangeBase() computes the maximum engagement range.
    /// @ 0x1806dc8b0: Returns weapon's max range or skill template max range (0x130).
    /// </summary>
    public static event IntSkillInterceptor OnGetMaxRangeBase;

    /// <summary>
    /// Fires after Skill.GetMinRangeBase() computes the minimum engagement range.
    /// @ 0x1806dc980: Returns weapon's min range or skill template min range (0x128).
    /// </summary>
    public static event IntSkillInterceptor OnGetMinRangeBase;

    /// <summary>
    /// Fires after Skill.IsInRange() checks if target is within range.
    /// @ 0x1806de4f0: Checks distance >= min range (0xb4) and <= max range (0xbc) + bonus.
    /// </summary>
    public static event BoolInterceptor OnIsInRange;

    /// <summary>
    /// Fires after Skill.IsInRangeShape() checks shape-based range validity.
    /// @ 0x1806de390: Checks if target tile is within skill's shape range (0x120).
    /// </summary>
    public static event BoolInterceptor OnIsInRangeShape;

    /// <summary>
    /// Fires after Skill.IsMovementSkill() checks if skill is a movement type.
    /// @ 0x1806de730: Iterates skill handlers looking for IMovementSkill interface.
    /// </summary>
    public static event BoolInterceptor OnIsMovementSkill;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  TIER 2 EVENTS - EntityProperties combat dropoff and penetration
    // ═══════════════════════════════════════════════════════════════════

    #region Tier 2: EntityProperties Combat Events

    /// <summary>
    /// Fires after EntityProperties.GetDamageDropoff() computes damage falloff over distance.
    /// Address: 0x18060bcd0
    /// Used for weapons that lose damage at range.
    /// </summary>
    public static event FloatIntercept OnGetDamageDropoff;

    /// <summary>
    /// Fires after EntityProperties.GetDamageToArmorDurability() computes armor-piercing damage.
    /// Address: 0x18060bd30
    /// Determines how much damage is dealt to armor durability on hit.
    /// </summary>
    public static event FloatIntercept OnGetDamageToArmorDurability;

    /// <summary>
    /// Fires after EntityProperties.GetDamageToArmorDurabilityDropoff() computes armor damage over range.
    /// Address: 0x18060bd00
    /// Armor durability damage falloff at distance.
    /// </summary>
    public static event FloatIntercept OnGetDamageToArmorDurabilityDropoff;

    /// <summary>
    /// Fires after EntityProperties.GetAccuracyDropoff() computes accuracy loss over distance.
    /// Address: 0x18060b9c0
    /// Higher values = faster accuracy decay at range.
    /// </summary>
    public static event FloatIntercept OnGetAccuracyDropoff;

    /// <summary>
    /// Fires after EntityProperties.GetArmorPenetration() computes AP values.
    /// Address: 0x18060bab0
    /// Determines ability to bypass target armor.
    /// </summary>
    public static event FloatIntercept OnGetArmorPenetration;

    /// <summary>
    /// Fires after EntityProperties.GetArmorPenetrationDropoff() computes AP falloff.
    /// Address: 0x18060ba80
    /// Armor penetration decay at distance.
    /// </summary>
    public static event FloatIntercept OnGetArmorPenetrationDropoff;

    /// <summary>
    /// Fires after EntityProperties.GetSuppression() computes suppression application rate.
    /// Address: 0x18060c780
    /// How much suppression this entity applies when attacking.
    /// </summary>
    public static event FloatIntercept OnGetSuppression;

    /// <summary>
    /// Fires after EntityProperties.GetDiscipline() computes morale resistance.
    /// Address: 0x18060bdd0
    /// Higher discipline = more resistant to suppression and panic.
    /// </summary>
    public static event FloatIntercept OnGetDiscipline;

    /// <summary>
    /// Fires after EntityProperties.GetHitpointsPerElement() computes HP per squad member.
    /// Address: 0x18060be10
    /// For squad-based entities, HP of each individual member.
    /// </summary>
    public static event IntIntercept OnGetHitpointsPerElement;

    /// <summary>
    /// Fires after EntityProperties.GetMaxHitpoints() computes total maximum HP.
    /// Address: 0x18060be40
    /// Total hitpoint pool for the entity.
    /// </summary>
    public static event IntIntercept OnGetMaxHitpoints;

    /// <summary>
    /// Fires after EntityProperties.GetActionPoints() computes AP pool size.
    /// Address: 0x18060ba20
    /// Maximum action points available per turn.
    /// </summary>
    public static event IntIntercept OnGetActionPoints;

    /// <summary>
    /// Fires after EntityProperties.GetMovementCostModifier() computes movement cost multiplier.
    /// Address: 0x18060bec0
    /// Multiplier applied to tile movement costs.
    /// </summary>
    public static event FloatIntercept OnGetMovementCostModifier;

    /// <summary>
    /// Fires after EntityProperties.GetPropertyValue() computes a generic property value.
    /// Address: 0x18060bef0
    /// Takes an EntityPropertyType parameter to determine which property to query.
    /// Use propertyType to filter for specific properties.
    /// </summary>
    public static event PropertyValueInterceptor OnGetPropertyValue;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  TIER 2 EVENTS - Entity state methods (combat status tracking)
    // ═══════════════════════════════════════════════════════════════════

    #region Tier 2: Entity State Events

    /// <summary>
    /// Fires after Entity.GetHitpointsPct() computes the health percentage.
    /// Address: 0x180611720
    /// Returns: float (0.0 to 1.0 representing current HP / max HP)
    /// Use cases: Health-based ability triggers, UI modifications, AI behavior
    /// </summary>
    public static event EntityFloatStateInterceptor OnGetHitpointsPct;

    /// <summary>
    /// Fires after Entity.GetArmorDurabilityPct() computes the armor condition.
    /// Address: 0x180610d10
    /// Returns: float (0.0 to 1.0 representing current armor / max armor)
    /// Use cases: Armor degradation effects, repair triggers, visual feedback
    /// </summary>
    public static event EntityFloatStateInterceptor OnGetArmorDurabilityPct;

    /// <summary>
    /// Fires after Entity.GetCoverUsage() returns the cover state enum.
    /// Address: 0x180610f10
    /// Returns: int (CoverUsage enum: None=0, Half=1, Full=2, etc.)
    /// Use cases: Cover-based bonuses, tactical AI, stealth mechanics
    /// </summary>
    public static event EntityIntStateInterceptor OnGetCoverUsage;

    /// <summary>
    /// Fires after Entity.GetProvidedCover() returns the cover object this entity provides.
    /// Address: 0x180611870
    /// Returns: IntPtr to Cover object (may be null/zero if no cover provided)
    /// Use cases: Dynamic cover systems, destructible cover, cover quality mods
    /// </summary>
    public static event EntityObjectStateInterceptor OnGetProvidedCover;

    /// <summary>
    /// Fires after Entity.IsDiscovered() returns the discovery state.
    /// Address: 0x180612c90
    /// Returns: bool (whether the entity has been discovered by enemies)
    /// Use cases: Stealth systems, fog of war, ambush mechanics
    /// </summary>
    public static event EntityBoolStateInterceptor OnIsDiscovered;

    /// <summary>
    /// Fires after Entity.GetLastSkillUsed() returns the last skill action.
    /// Address: 0x180611780
    /// Returns: IntPtr to Skill object (may be null if no skill used)
    /// Use cases: Combo systems, action tracking, reaction triggers
    /// </summary>
    public static event EntityObjectStateInterceptor OnGetLastSkillUsed;

    /// <summary>
    /// Fires after Entity.GetScaleRange() returns the scale bounds.
    /// Address: 0x180611940
    /// Returns: Vector2Result (X=min scale, Y=max scale)
    /// Use cases: Size-based mechanics, visual scaling, hitbox modifications
    /// </summary>
    public static event EntityVector2StateInterceptor OnGetScaleRange;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  TILE EVENTS - Tile query interceptors
    // ═══════════════════════════════════════════════════════════════════

    #region Tile Events

    /// <summary>
    /// Fires after Tile.HasLineOfSightTo() checks LoS between tiles.
    /// Address: 0x180681d70. Handles structures with cover type 4.
    /// Calls LineOfSight algorithm bidirectionally.
    /// </summary>
    public static event TileLoSInterceptor OnTileHasLineOfSightTo;

    /// <summary>
    /// Fires after Tile.IsBlockingLineOfSight() checks if tile blocks LoS.
    /// Address: 0x180681e50. Simple check of boolean flag at offset 0x60.
    /// </summary>
    public static event TileBlockerInterceptor OnTileIsBlockingLineOfSight;

    /// <summary>
    /// Fires after Tile.GetCover() retrieves cover value for direction.
    /// Address: 0x180680b20. Cover array at 0x28, checks adjacent tiles
    /// for actors providing cover. Returns 0-3 cover level.
    /// </summary>
    public static event TileCoverInterceptor OnTileGetCover;

    /// <summary>
    /// Fires after Tile.GetCoverMask() computes cover bitmask.
    /// Address: 0x180680a50. Returns bitmask where bits 0-3 represent
    /// cover in N/E/S/W directions.
    /// </summary>
    public static event TileCoverMaskInterceptor OnTileGetCoverMask;

    /// <summary>
    /// Fires after Tile.GetEntityProvidedCover() gets cover from entities.
    /// Address: 0x180681a30. Returns cover value provided by entities
    /// occupying the tile.
    /// </summary>
    public static event TileEntityCoverInterceptor OnTileGetEntityProvidedCover;

    /// <summary>
    /// Fires after Tile.CanBeEntered() checks entry permission.
    /// Address: 0x180680950. Checks if tile's structure allows entry.
    /// </summary>
    public static event TileEntryInterceptor OnTileCanBeEntered;

    /// <summary>
    /// Fires after Tile.CanBeEnteredBy() checks entity-specific entry.
    /// Address: 0x180680920. Checks if specific entity can enter the tile.
    /// </summary>
    public static event TileEntityEntryInterceptor OnTileCanBeEnteredBy;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  BASETILE EVENTS - BaseTile query interceptors
    // ═══════════════════════════════════════════════════════════════════

    #region BaseTile Events

    /// <summary>
    /// Fires after BaseTile.HasCover() checks cover presence.
    /// Address: 0x1805caa80. Iterates cover array at 0x28 checking
    /// for any non-zero cover values.
    /// </summary>
    public static event BaseTileCoverCheckInterceptor OnBaseTileHasCover;

    /// <summary>
    /// Fires after BaseTile.HasHalfCover() checks for half cover.
    /// Address: 0x1805cab30. Checks if tile has half cover in any direction.
    /// </summary>
    public static event BaseTileHalfCoverInterceptor OnBaseTileHasHalfCover;

    /// <summary>
    /// Fires after BaseTile.HasHalfCoverInDir() checks directional half cover.
    /// Address: 0x1805caaf0. Half cover array at 0x30, indexed by direction/2.
    /// </summary>
    public static event BaseTileDirHalfCoverInterceptor OnBaseTileHasHalfCoverInDir;

    /// <summary>
    /// Fires after BaseTile.IsMovementBlocked() checks movement obstruction.
    /// Address: 0x1805cae00. Movement block array at 0x38, indexed by direction.
    /// </summary>
    public static event BaseTileMovementBlockedInterceptor OnBaseTileIsMovementBlocked;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  LINEOFSIGHT EVENTS - LineOfSight static method interceptors
    // ═══════════════════════════════════════════════════════════════════

    #region LineOfSight Events

    /// <summary>
    /// Fires after LineOfSight.HasLineOfSight() (RayTrace) performs LoS check.
    /// Address: 0x18051df40. Core LoS raycast algorithm. Iterates along line
    /// from source to target checking blocking flags (bit 11 at offset 0x1c).
    /// Handles structures and directional blockers. Max 1000 iterations.
    /// </summary>
    public static event LineOfSightRayTraceInterceptor OnLineOfSightRayTrace;

    /// <summary>
    /// Fires after LineOfSight.IsNearTileCorner() checks corner proximity.
    /// Address: 0x18051e4b0. Used for cover calculations and positioning.
    /// </summary>
    public static event LineOfSightCornerInterceptor OnLineOfSightIsNearTileCorner;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  MOVEMENT EVENTS - MovementType calculations
    // ═══════════════════════════════════════════════════════════════════

    #region Movement Events

    /// <summary>
    /// Fires after MovementType.GetMaxMovementSpeed() computes movement speed.
    /// Controls how fast units can move on the tactical map.
    /// </summary>
    public static event MovementFloatInterceptor OnGetMaxMovementSpeed;

    /// <summary>
    /// Fires after MovementType.GetTotalPathCost() computes total AP cost for a path.
    /// Used for pathfinding and movement planning.
    /// </summary>
    public static event PathCostInterceptor OnGetTotalPathCost;

    /// <summary>
    /// Fires after MovementType.GetTurnSpeed() computes turn animation speed.
    /// </summary>
    public static event MovementFloatInterceptor OnGetTurnSpeed;

    /// <summary>
    /// Fires after MovementType.GetSlowdownDistance() computes deceleration distance.
    /// </summary>
    public static event MovementFloatInterceptor OnGetSlowdownDistance;

    /// <summary>
    /// Fires after MovementType.GetMaxAngleTurnSlowdown() computes turn penalty.
    /// </summary>
    public static event MovementFloatInterceptor OnGetMaxAngleTurnSlowdown;

    /// <summary>
    /// Fires after MovementType.ClipPathToCost() clips a path to a max AP budget.
    /// Used to limit movement to available action points.
    /// </summary>
    public static event ClipPathInterceptor OnClipPathToCost;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  STRATEGY LAYER EVENTS - UnitLeaderAttributes, BaseUnitLeader, Vehicle
    // ═══════════════════════════════════════════════════════════════════

    #region Strategy: UnitLeaderAttributes Events

    /// <summary>
    /// Fires after UnitLeaderAttributes.GetActionPoints() computes base AP.
    /// This is the base action points for the unit leader on the strategy map.
    /// </summary>
    public static event StrategyIntIntercept OnStrategyGetActionPoints;

    /// <summary>
    /// Fires after UnitLeaderAttributes.GetHitpointsPerElement() computes HP per element.
    /// Determines hitpoints for each element/soldier in the squad.
    /// </summary>
    public static event StrategyIntIntercept OnStrategyGetHitpointsPerElement;

    /// <summary>
    /// Fires after UnitLeaderAttributes.GetDamageSustainedMult() computes damage taken modifier.
    /// Multiplier applied to incoming damage (lower = more resistant).
    /// </summary>
    public static event StrategyFloatIntercept OnStrategyGetDamageSustainedMult;

    #endregion

    #region Strategy: BaseUnitLeader Events

    /// <summary>
    /// Fires after BaseUnitLeader.GetHitpointsPct() computes leader health percentage.
    /// Returns current health as a percentage (0.0 to 1.0).
    /// </summary>
    public static event StrategyFloatIntercept OnStrategyGetHitpointsPct;

    /// <summary>
    /// Fires after BaseUnitLeader.CanBePromoted() determines promotion eligibility.
    /// Set to true to allow promotion, false to prevent.
    /// </summary>
    public static event StrategyBoolIntercept OnStrategyCanBePromoted;

    /// <summary>
    /// Fires after BaseUnitLeader.CanBeDemoted() determines demotion eligibility.
    /// Set to true to allow demotion, false to prevent.
    /// </summary>
    public static event StrategyBoolIntercept OnStrategyCanBeDemoted;

    /// <summary>
    /// Fires after BaseUnitLeader.GetEntityProperty() retrieves a generic property.
    /// This is a flexible property getter that takes an EntityPropertyType parameter.
    /// </summary>
    public static event StrategyEntityIntercept OnStrategyGetEntityProperty;

    #endregion

    #region Strategy: Vehicle Events

    /// <summary>
    /// Fires after Vehicle.GetArmor() computes vehicle armor value.
    /// Returns the armor rating for the vehicle on the strategy map.
    /// </summary>
    public static event StrategyIntIntercept OnStrategyGetVehicleArmor;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  AI BEHAVIOR EVENTS - AIBrain decision making
    // ═══════════════════════════════════════════════════════════════════

    #region AI Behavior Events

    /// <summary>
    /// Fires after AI evaluates attack scoring for a target.
    /// Higher values make the target more attractive.
    /// </summary>
    public static event AIFloatValueInterceptor OnAIGetAttackScore;

    /// <summary>
    /// Fires after AI evaluates threat level of a target.
    /// Higher values indicate more dangerous targets.
    /// </summary>
    public static event AIFloatValueInterceptor OnAIGetThreatValue;

    /// <summary>
    /// Fires after AI evaluates priority of an action.
    /// Higher values increase likelihood of action being chosen.
    /// </summary>
    public static event AIIntValueInterceptor OnAIGetActionPriority;

    /// <summary>
    /// Fires after AI decides whether to flee.
    /// Return true to force flee, false to prevent.
    /// </summary>
    public static event AIBoolDecisionInterceptor OnAIShouldFlee;

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initialize the Intercept system.
    /// Call this from ModpackLoaderMod after game assembly is loaded.
    /// </summary>
    /// <param name="harmony">Harmony instance for patching</param>
    public static void Initialize(HarmonyLib.Harmony harmony)
    {
        if (_initialized) return;

        _harmony = harmony;

        try
        {
            var gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

            if (gameAssembly == null)
            {
                SdkLogger.Warning("[Intercept] Assembly-CSharp not found");
                return;
            }

            // Cache types for postfix patches
            _entityPropertiesType = gameAssembly.GetType("Menace.Tactical.EntityProperties");
            _skillType = gameAssembly.GetType("Menace.Tactical.Skills.Skill");
            _actorType = gameAssembly.GetType("Menace.Tactical.Actor");
            _entityType = gameAssembly.GetType("Menace.Tactical.Entity");
            _tileType = gameAssembly.GetType("Menace.Tactical.Tile");
            _baseTileType = gameAssembly.GetType("Menace.Tactical.BaseTile");
            _lineOfSightType = gameAssembly.GetType("Tactical.LineOfSight");
            // Movement types
            _movementTypeType = gameAssembly.GetType("Menace.Tactical.MovementType");
            // Strategy layer types
            _unitLeaderAttributesType = gameAssembly.GetType("Menace.Strategy.UnitLeaderAttributes");
            _baseUnitLeaderType = gameAssembly.GetType("Menace.Strategy.BaseUnitLeader");
            _vehicleType = gameAssembly.GetType("Menace.Strategy.Vehicle");
            // AI types
            _aiBrainType = gameAssembly.GetType("Menace.Tactical.AI.AIBrain");

            if (_entityPropertiesType == null)
            {
                SdkLogger.Warning("[Intercept] EntityProperties type not found");
                return;
            }

            // Apply Tier 1 patches
            int patchCount = 0;

            // EntityProperties patches - Tier 1
            patchCount += PatchEntityPropertyMethod("GetDamage", nameof(GetDamage_Postfix));
            patchCount += PatchEntityPropertyMethod("GetAccuracy", nameof(GetAccuracy_Postfix));
            patchCount += PatchEntityPropertyMethod("GetArmor", nameof(GetArmor_Postfix));
            patchCount += PatchEntityPropertyMethod("GetConcealment", nameof(GetConcealment_Postfix));
            patchCount += PatchEntityPropertyMethod("GetDetection", nameof(GetDetection_Postfix));
            patchCount += PatchEntityPropertyMethod("GetVision", nameof(GetVision_Postfix));

            // EntityProperties patches - Tier 2: Combat dropoff and penetration
            patchCount += PatchEntityPropertyMethod("GetDamageDropoff", nameof(GetDamageDropoff_Postfix));
            patchCount += PatchEntityPropertyMethod("GetDamageToArmorDurability", nameof(GetDamageToArmorDurability_Postfix));
            patchCount += PatchEntityPropertyMethod("GetDamageToArmorDurabilityDropoff", nameof(GetDamageToArmorDurabilityDropoff_Postfix));
            patchCount += PatchEntityPropertyMethod("GetAccuracyDropoff", nameof(GetAccuracyDropoff_Postfix));
            patchCount += PatchEntityPropertyMethod("GetArmorPenetration", nameof(GetArmorPenetration_Postfix));
            patchCount += PatchEntityPropertyMethod("GetArmorPenetrationDropoff", nameof(GetArmorPenetrationDropoff_Postfix));
            patchCount += PatchEntityPropertyMethod("GetSuppression", nameof(GetSuppression_Postfix));
            patchCount += PatchEntityPropertyMethod("GetDiscipline", nameof(GetDiscipline_Postfix));
            patchCount += PatchEntityPropertyMethod("GetHitpointsPerElement", nameof(GetHitpointsPerElement_Postfix));
            patchCount += PatchEntityPropertyMethod("GetMaxHitpoints", nameof(GetMaxHitpoints_Postfix));
            patchCount += PatchEntityPropertyMethod("GetActionPoints", nameof(GetActionPoints_Postfix));
            patchCount += PatchEntityPropertyMethod("GetMovementCostModifier", nameof(GetMovementCostModifier_Postfix));
            patchCount += PatchEntityPropertyMethodWithParam("GetPropertyValue", nameof(GetPropertyValue_Postfix));

            // Skill patches (Tier 1)
            if (_skillType != null)
            {
                patchCount += PatchSkillMethod("GetHitchance", nameof(GetHitchance_Postfix));
                patchCount += PatchSkillMethod("GetCoverMult", nameof(GetCoverMult_Postfix));
                // GetExpectedDamage has overloads - patch both
                patchCount += PatchSkillMethodOverloads("GetExpectedDamage", nameof(GetExpectedDamage_Postfix));

                // Tier 2: Skill range and cost patches
                patchCount += PatchSkillMethodOverloads("GetExpectedSuppression", nameof(GetExpectedSuppression_Postfix));
                patchCount += PatchSkillMethod("GetActionPointCost", nameof(GetActionPointCost_Postfix));
                patchCount += PatchSkillMethod("GetIdealRangeBase", nameof(GetIdealRangeBase_Postfix));
                patchCount += PatchSkillMethod("GetMaxRangeBase", nameof(GetMaxRangeBase_Postfix));
                patchCount += PatchSkillMethod("GetMinRangeBase", nameof(GetMinRangeBase_Postfix));
                patchCount += PatchSkillMethod("IsInRange", nameof(IsInRange_Postfix));
                patchCount += PatchSkillMethod("IsInRangeShape", nameof(IsInRangeShape_Postfix));
                patchCount += PatchSkillMethod("IsMovementSkill", nameof(IsMovementSkill_Postfix));
            }

            // Actor patches
            if (_actorType != null)
            {
                patchCount += PatchActorMethod("HasLineOfSightTo", nameof(HasLineOfSightTo_Postfix));
                // TODO: Actor state interceptors (Morale, Suppression, ActionPoints, Boolean queries)
                // require postfix implementations before enabling
            }

            // Entity state patches
            if (_entityType != null)
            {
                patchCount += PatchEntityMethod("GetHitpointsPct", nameof(GetHitpointsPct_Postfix));
                patchCount += PatchEntityMethod("GetArmorDurabilityPct", nameof(GetArmorDurabilityPct_Postfix));
                patchCount += PatchEntityMethod("GetCoverUsage", nameof(GetCoverUsage_Postfix));
                patchCount += PatchEntityMethod("GetProvidedCover", nameof(GetProvidedCover_Postfix));
                patchCount += PatchEntityMethod("IsDiscovered", nameof(IsDiscovered_Postfix));
                patchCount += PatchEntityMethod("GetLastSkillUsed", nameof(GetLastSkillUsed_Postfix));
                patchCount += PatchEntityMethod("GetScaleRange", nameof(GetScaleRange_Postfix));
            }

            // Tile patches
            if (_tileType != null)
            {
                patchCount += PatchTileMethod("HasLineOfSightTo", nameof(TileHasLineOfSightTo_Postfix));
                patchCount += PatchTileMethod("IsBlockingLineOfSight", nameof(TileIsBlockingLineOfSight_Postfix));
                patchCount += PatchTileMethod("GetCover", nameof(TileGetCover_Postfix));
                patchCount += PatchTileMethod("GetCoverMask", nameof(TileGetCoverMask_Postfix));
                patchCount += PatchTileMethod("GetEntityProvidedCover", nameof(TileGetEntityProvidedCover_Postfix));
                patchCount += PatchTileMethod("CanBeEntered", nameof(TileCanBeEntered_Postfix));
                patchCount += PatchTileMethod("CanBeEnteredBy", nameof(TileCanBeEnteredBy_Postfix));
            }

            // BaseTile patches
            if (_baseTileType != null)
            {
                patchCount += PatchBaseTileMethod("HasCover", nameof(BaseTileHasCover_Postfix));
                patchCount += PatchBaseTileMethod("HasHalfCover", nameof(BaseTileHasHalfCover_Postfix));
                patchCount += PatchBaseTileMethod("HasHalfCoverInDir", nameof(BaseTileHasHalfCoverInDir_Postfix));
                patchCount += PatchBaseTileMethod("IsMovementBlocked", nameof(BaseTileIsMovementBlocked_Postfix));
            }

            // LineOfSight patches (static methods)
            if (_lineOfSightType != null)
            {
                patchCount += PatchLineOfSightStaticMethod("HasLineOfSight", nameof(LineOfSightRayTrace_Postfix));
                patchCount += PatchLineOfSightStaticMethod("IsNearTileCorner", nameof(LineOfSightIsNearTileCorner_Postfix));
            }
            else
            {
                // Try alternative namespace
                _lineOfSightType = gameAssembly.GetType("Menace.Tactical.LineOfSight");
                if (_lineOfSightType != null)
                {
                    patchCount += PatchLineOfSightStaticMethod("HasLineOfSight", nameof(LineOfSightRayTrace_Postfix));
                    patchCount += PatchLineOfSightStaticMethod("IsNearTileCorner", nameof(LineOfSightIsNearTileCorner_Postfix));
                }
            }

            // Apply Movement patches
            if (_movementTypeType != null)
            {
                patchCount += PatchMovementTypeMethod("GetMaxMovementSpeed", nameof(GetMaxMovementSpeed_Postfix));
                patchCount += PatchMovementTypeMethod("GetTotalPathCost", nameof(GetTotalPathCost_Postfix));
                patchCount += PatchMovementTypeMethod("GetTurnSpeed", nameof(GetTurnSpeed_Postfix));
                patchCount += PatchMovementTypeMethod("GetSlowdownDistance", nameof(GetSlowdownDistance_Postfix));
                patchCount += PatchMovementTypeMethod("GetMaxAngleTurnSlowdown", nameof(GetMaxAngleTurnSlowdown_Postfix));
                patchCount += PatchMovementTypeMethod("ClipPathToCost", nameof(ClipPathToCost_Postfix));
            }
            else
            {
                SdkLogger.Warning("[Intercept] MovementType not found - movement patches skipped");
            }

            // Apply Strategy layer patches
            if (_unitLeaderAttributesType != null)
            {
                patchCount += PatchStrategyMethod(_unitLeaderAttributesType, "GetActionPoints", nameof(StrategyGetActionPoints_Postfix));
                patchCount += PatchStrategyMethod(_unitLeaderAttributesType, "GetHitpointsPerElement", nameof(StrategyGetHitpointsPerElement_Postfix));
                patchCount += PatchStrategyMethod(_unitLeaderAttributesType, "GetDamageSustainedMult", nameof(StrategyGetDamageSustainedMult_Postfix));
            }
            else
            {
                SdkLogger.Warning("[Intercept] UnitLeaderAttributes not found - Strategy patches skipped");
            }

            if (_baseUnitLeaderType != null)
            {
                patchCount += PatchStrategyMethod(_baseUnitLeaderType, "GetHitpointsPct", nameof(StrategyGetHitpointsPct_Postfix));
                patchCount += PatchStrategyMethod(_baseUnitLeaderType, "CanBePromoted", nameof(StrategyCanBePromoted_Postfix));
                patchCount += PatchStrategyMethod(_baseUnitLeaderType, "CanBeDemoted", nameof(StrategyCanBeDemoted_Postfix));
                patchCount += PatchStrategyMethodWithParam(_baseUnitLeaderType, "GetEntityProperty", nameof(StrategyGetEntityProperty_Postfix));
            }
            else
            {
                SdkLogger.Warning("[Intercept] BaseUnitLeader not found - Strategy patches skipped");
            }

            if (_vehicleType != null)
            {
                patchCount += PatchStrategyMethod(_vehicleType, "GetArmor", nameof(StrategyGetVehicleArmor_Postfix));
            }

            // Apply AI behavior patches
            if (_aiBrainType != null)
            {
                patchCount += PatchAIMethod("GetAttackScore", nameof(AIGetAttackScore_Postfix));
                patchCount += PatchAIMethod("GetThreatValue", nameof(AIGetThreatValue_Postfix));
                patchCount += PatchAIMethod("GetActionPriority", nameof(AIGetActionPriority_Postfix));
                patchCount += PatchAIMethod("ShouldFlee", nameof(AIShouldFlee_Postfix));
            }
            else
            {
                SdkLogger.Warning("[Intercept] AIBrain not found - AI patches skipped");
            }

            _initialized = true;
            SdkLogger.Msg($"[Intercept] Initialized with {patchCount} property hooks");

            // Register console commands for debugging
            RegisterConsoleCommands();
        }
        catch (Exception ex)
        {
            SdkLogger.Error($"[Intercept] Failed to initialize: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PATCH HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private static int PatchEntityPropertyMethod(string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = _entityPropertiesType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: EntityProperties.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch EntityProperties.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchEntityPropertyMethodWithParam(string methodName, string patchMethodName)
    {
        try
        {
            // Find method with EntityPropertyType parameter
            var targetMethod = _entityPropertiesType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length >= 1);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: EntityProperties.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch EntityProperties.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchSkillMethod(string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = _skillType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: Skill.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch Skill.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchSkillMethodOverloads(string methodName, string patchMethodName)
    {
        try
        {
            var methods = _skillType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == methodName)
                .ToList();

            if (methods.Count == 0)
            {
                SdkLogger.Warning($"[Intercept] No overloads found: Skill.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            int count = 0;
            foreach (var method in methods)
            {
                try
                {
                    _harmony.Patch(method, postfix: new HarmonyMethod(patchMethod));
                    count++;
                }
                catch
                {
                    // Some overloads may not be patchable, continue
                }
            }
            return count;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch Skill.{methodName} overloads: {ex.Message}");
            return 0;
        }
    }

    private static int PatchActorMethod(string methodName, string patchMethodName)
    {
        try
        {
            // HasLineOfSightTo has specific signature, find the right overload
            var targetMethod = _actorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length >= 1);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: Actor.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch Actor.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchActorMethodSimple(string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = _actorType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);

            if (targetMethod == null)
            {
                // Try without parameter restriction
                targetMethod = _actorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == 0);
            }

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: Actor.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch Actor.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchActorMethodWithParam(string methodName, string patchMethodName, Type paramType)
    {
        try
        {
            var targetMethod = _actorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == methodName &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == paramType);

            if (targetMethod == null)
            {
                // Try any single-parameter overload
                targetMethod = _actorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == 1);
            }

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: Actor.{methodName}({paramType.Name})");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch Actor.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchEntityMethod(string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = _entityType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: Entity.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch Entity.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchTileMethod(string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = _tileType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == methodName);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: Tile.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch Tile.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchBaseTileMethod(string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = _baseTileType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == methodName);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: BaseTile.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch BaseTile.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchLineOfSightStaticMethod(string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = _lineOfSightType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == methodName);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: LineOfSight.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch LineOfSight.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchMovementTypeMethod(string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = _movementTypeType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: MovementType.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch MovementType.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchStrategyMethod(Type strategyType, string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = strategyType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: {strategyType.Name}.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch {strategyType.Name}.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchStrategyMethodWithParam(Type strategyType, string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = strategyType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length >= 1);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: {strategyType.Name}.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch {strategyType.Name}.{methodName}: {ex.Message}");
            return 0;
        }
    }

    private static int PatchAIMethod(string methodName, string patchMethodName)
    {
        try
        {
            var targetMethod = _aiBrainType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (targetMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Method not found: AIBrain.{methodName}");
                return 0;
            }

            var patchMethod = typeof(Intercept).GetMethod(patchMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            if (patchMethod == null)
            {
                SdkLogger.Warning($"[Intercept] Patch method not found: {patchMethodName}");
                return 0;
            }

            _harmony.Patch(targetMethod, postfix: new HarmonyMethod(patchMethod));
            return 1;
        }
        catch (Exception ex)
        {
            SdkLogger.Warning($"[Intercept] Failed to patch AIBrain.{methodName}: {ex.Message}");
            return 0;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════

    private static IntPtr GetPointer(object obj)
    {
        if (obj == null) return IntPtr.Zero;
        if (obj is Il2CppObjectBase il2cppObj)
            return il2cppObj.Pointer;
        return IntPtr.Zero;
    }

    /// <summary>
    /// Attempt to resolve the Entity that owns an EntityProperties instance.
    /// This is a best-effort lookup - may return null in some contexts.
    /// </summary>
    private static GameObj TryResolveOwner(IntPtr propsPtr)
    {
        // EntityProperties doesn't have a direct back-reference to its owner
        // In combat calculations, the owner is typically available as a separate parameter
        // This method exists for cases where we only have the properties pointer

        // For now, return null - owner resolution happens in the patch methods
        // where context is available
        return GameObj.Null;
    }

    /// <summary>
    /// Fire a Lua event for property interception.
    /// </summary>
    private static void FireLuaEvent(string eventName, Dictionary<string, object> data)
    {
        try
        {
            LuaScriptEngine.Instance?.FireEventWithTable(eventName, data);
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"Lua event '{eventName}' failed: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HARMONY POSTFIX PATCHES - EntityProperties
    // ═══════════════════════════════════════════════════════════════════

    #region EntityProperties Postfixes

    private static void GetDamage_Postfix(object __instance, ref float __result)
    {
        if (OnGetDamage == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetDamage.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetDamage handler failed: {ex.Message}");
                }
            }

            __result = result;

            // Fire Lua event
            FireLuaEvent("property_damage", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetDamage_Postfix failed: {ex.Message}");
        }
    }

    private static void GetAccuracy_Postfix(object __instance, ref float __result)
    {
        if (OnGetAccuracy == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetAccuracy.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetAccuracy handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_accuracy", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetAccuracy_Postfix failed: {ex.Message}");
        }
    }

    private static void GetArmor_Postfix(object __instance, ref int __result)
    {
        if (OnGetArmor == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetArmor.GetInvocationList().Cast<IntIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetArmor handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_armor", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetArmor_Postfix failed: {ex.Message}");
        }
    }

    private static void GetConcealment_Postfix(object __instance, ref int __result)
    {
        if (OnGetConcealment == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetConcealment.GetInvocationList().Cast<IntIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetConcealment handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_concealment", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetConcealment_Postfix failed: {ex.Message}");
        }
    }

    private static void GetDetection_Postfix(object __instance, ref int __result)
    {
        if (OnGetDetection == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetDetection.GetInvocationList().Cast<IntIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetDetection handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_detection", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetDetection_Postfix failed: {ex.Message}");
        }
    }

    private static void GetVision_Postfix(object __instance, ref int __result)
    {
        if (OnGetVision == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetVision.GetInvocationList().Cast<IntIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetVision handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_vision", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetVision_Postfix failed: {ex.Message}");
        }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  HARMONY POSTFIX PATCHES - EntityProperties (Tier 2)
    // ═══════════════════════════════════════════════════════════════════

    #region EntityProperties Postfixes (Tier 2)

    private static void GetDamageDropoff_Postfix(object __instance, ref float __result)
    {
        if (OnGetDamageDropoff == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetDamageDropoff.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetDamageDropoff handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_damage_dropoff", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetDamageDropoff_Postfix failed: {ex.Message}");
        }
    }

    private static void GetDamageToArmorDurability_Postfix(object __instance, ref float __result)
    {
        if (OnGetDamageToArmorDurability == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetDamageToArmorDurability.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetDamageToArmorDurability handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_damage_to_armor_durability", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetDamageToArmorDurability_Postfix failed: {ex.Message}");
        }
    }

    private static void GetDamageToArmorDurabilityDropoff_Postfix(object __instance, ref float __result)
    {
        if (OnGetDamageToArmorDurabilityDropoff == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetDamageToArmorDurabilityDropoff.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetDamageToArmorDurabilityDropoff handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_damage_to_armor_durability_dropoff", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetDamageToArmorDurabilityDropoff_Postfix failed: {ex.Message}");
        }
    }

    private static void GetAccuracyDropoff_Postfix(object __instance, ref float __result)
    {
        if (OnGetAccuracyDropoff == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetAccuracyDropoff.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetAccuracyDropoff handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_accuracy_dropoff", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetAccuracyDropoff_Postfix failed: {ex.Message}");
        }
    }

    private static void GetArmorPenetration_Postfix(object __instance, ref float __result)
    {
        if (OnGetArmorPenetration == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetArmorPenetration.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetArmorPenetration handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_armor_penetration", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetArmorPenetration_Postfix failed: {ex.Message}");
        }
    }

    private static void GetArmorPenetrationDropoff_Postfix(object __instance, ref float __result)
    {
        if (OnGetArmorPenetrationDropoff == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetArmorPenetrationDropoff.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetArmorPenetrationDropoff handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_armor_penetration_dropoff", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetArmorPenetrationDropoff_Postfix failed: {ex.Message}");
        }
    }

    private static void GetSuppression_Postfix(object __instance, ref float __result)
    {
        if (OnGetSuppression == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetSuppression.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetSuppression handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_suppression", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetSuppression_Postfix failed: {ex.Message}");
        }
    }

    private static void GetDiscipline_Postfix(object __instance, ref float __result)
    {
        if (OnGetDiscipline == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetDiscipline.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetDiscipline handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_discipline", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetDiscipline_Postfix failed: {ex.Message}");
        }
    }

    private static void GetHitpointsPerElement_Postfix(object __instance, ref int __result)
    {
        if (OnGetHitpointsPerElement == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetHitpointsPerElement.GetInvocationList().Cast<IntIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetHitpointsPerElement handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_hitpoints_per_element", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetHitpointsPerElement_Postfix failed: {ex.Message}");
        }
    }

    private static void GetMaxHitpoints_Postfix(object __instance, ref int __result)
    {
        if (OnGetMaxHitpoints == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetMaxHitpoints.GetInvocationList().Cast<IntIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetMaxHitpoints handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_max_hitpoints", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetMaxHitpoints_Postfix failed: {ex.Message}");
        }
    }

    private static void GetActionPoints_Postfix(object __instance, ref int __result)
    {
        if (OnGetActionPoints == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetActionPoints.GetInvocationList().Cast<IntIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetActionPoints handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_action_points", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetActionPoints_Postfix failed: {ex.Message}");
        }
    }

    private static void GetMovementCostModifier_Postfix(object __instance, ref float __result)
    {
        if (OnGetMovementCostModifier == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetMovementCostModifier.GetInvocationList().Cast<FloatIntercept>())
            {
                try
                {
                    handler(props, owner, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetMovementCostModifier handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_movement_cost_modifier", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetMovementCostModifier_Postfix failed: {ex.Message}");
        }
    }

    private static void GetPropertyValue_Postfix(object __instance, ref float __result, int propertyType)
    {
        if (OnGetPropertyValue == null) return;

        try
        {
            var propsPtr = GetPointer(__instance);
            var props = new GameObj(propsPtr);
            var owner = TryResolveOwner(propsPtr);
            var result = __result;

            foreach (var handler in OnGetPropertyValue.GetInvocationList().Cast<PropertyValueInterceptor>())
            {
                try
                {
                    handler(props, owner, propertyType, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetPropertyValue handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("property_value", new Dictionary<string, object>
            {
                ["props_ptr"] = propsPtr.ToInt64(),
                ["property_type"] = propertyType,
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetPropertyValue_Postfix failed: {ex.Message}");
        }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  HARMONY POSTFIX PATCHES - Skill (Tier 1)
    // ═══════════════════════════════════════════════════════════════════

    #region Skill Postfixes (Tier 1)

    private static void GetHitchance_Postfix(object __instance, object __result,
        object fromTile, object toTile, object target, object attackProps, object defenseProps,
        bool useDropoff, object targetEntity, bool targetInsideActor)
    {
        if (OnGetHitChance == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);

            // Extract attacker from skill
            var attacker = GameObj.Null;
            try
            {
                // skill.GetActor() or skill.SkillContainer.Entity
                var skillObj = new GameObj(skillPtr);
                var containerPtr = skillObj.ReadPtr(OFFSET_SKILL_CONTAINER);
                if (containerPtr != IntPtr.Zero)
                {
                    var entityPtr = System.Runtime.InteropServices.Marshal.ReadIntPtr(containerPtr + (int)OFFSET_CONTAINER_ENTITY);
                    if (entityPtr != IntPtr.Zero)
                        attacker = new GameObj(entityPtr);
                }
            }
            catch { }

            var targetObj = new GameObj(GetPointer(targetEntity ?? target));

            // Parse result struct - GetHitchance returns a HitchanceResult struct
            // The struct has: FinalChance, BaseAccuracy, CoverMult, DefenseMult, DistancePenalty, flags
            var result = new HitChanceResult();

            if (__result is Il2CppObjectBase resultObj)
            {
                var resultPtr = resultObj.Pointer;
                // HitchanceResult layout (from Ghidra analysis):
                // 0x00: float FinalChance
                // 0x04: float BaseAccuracy
                // 0x08: float CoverMult
                // 0x0C: float DefenseMult
                // 0x10: float DistancePenalty
                // 0x14: byte flags (HasDistanceFalloff, IsGuaranteedHit)
                result.FinalChance = System.Runtime.InteropServices.Marshal.PtrToStructure<float>(resultPtr);
                result.BaseAccuracy = System.Runtime.InteropServices.Marshal.PtrToStructure<float>(resultPtr + 4);
                result.CoverMult = System.Runtime.InteropServices.Marshal.PtrToStructure<float>(resultPtr + 8);
                result.DefenseMult = System.Runtime.InteropServices.Marshal.PtrToStructure<float>(resultPtr + 12);
                result.DistancePenalty = System.Runtime.InteropServices.Marshal.PtrToStructure<float>(resultPtr + 20);
                var flags = System.Runtime.InteropServices.Marshal.ReadByte(resultPtr + 16);
                result.IsGuaranteedHit = (flags & 1) != 0;
                result.HasDistanceFalloff = System.Runtime.InteropServices.Marshal.ReadByte(resultPtr + 17) != 0;
            }

            foreach (var handler in OnGetHitChance.GetInvocationList().Cast<HitChanceInterceptor>())
            {
                try
                {
                    handler(skill, attacker, targetObj, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetHitChance handler failed: {ex.Message}");
                }
            }

            // Write back modified result
            if (__result is Il2CppObjectBase resultObjWrite)
            {
                var resultPtr = resultObjWrite.Pointer;
                System.Runtime.InteropServices.Marshal.StructureToPtr(result.FinalChance, resultPtr, false);
                // Note: We only modify FinalChance; other fields are informational
            }

            FireLuaEvent("skill_hitchance", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["final_chance"] = result.FinalChance,
                ["base_accuracy"] = result.BaseAccuracy,
                ["cover_mult"] = result.CoverMult,
                ["defense_mult"] = result.DefenseMult
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetHitchance_Postfix failed: {ex.Message}");
        }
    }

    private static void GetCoverMult_Postfix(object __instance, ref float __result,
        object fromTile, object toTile, object target, object defenseProps, bool targetInsideActor)
    {
        if (OnGetCoverMult == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);
            var targetObj = new GameObj(GetPointer(target));

            // Extract attacker
            var attacker = GameObj.Null;
            try
            {
                var skillObj = new GameObj(skillPtr);
                var containerPtr = skillObj.ReadPtr(OFFSET_SKILL_CONTAINER);
                if (containerPtr != IntPtr.Zero)
                {
                    var entityPtr = System.Runtime.InteropServices.Marshal.ReadIntPtr(containerPtr + (int)OFFSET_CONTAINER_ENTITY);
                    if (entityPtr != IntPtr.Zero)
                        attacker = new GameObj(entityPtr);
                }
            }
            catch { }

            var result = __result;

            foreach (var handler in OnGetCoverMult.GetInvocationList().Cast<CoverMultInterceptor>())
            {
                try
                {
                    handler(skill, attacker, targetObj, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetCoverMult handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("skill_covermult", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["cover_mult"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetCoverMult_Postfix failed: {ex.Message}");
        }
    }

    private static void GetExpectedDamage_Postfix(object __instance, ref float __result)
    {
        if (OnGetExpectedDamage == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);

            // Limited context in this overload
            var result = new ExpectedDamageResult { Damage = __result };

            foreach (var handler in OnGetExpectedDamage.GetInvocationList().Cast<ExpectedDamageInterceptor>())
            {
                try
                {
                    handler(skill, GameObj.Null, GameObj.Null, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetExpectedDamage handler failed: {ex.Message}");
                }
            }

            __result = result.Damage;

            FireLuaEvent("skill_expected_damage", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["damage"] = result.Damage
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetExpectedDamage_Postfix failed: {ex.Message}");
        }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  HARMONY POSTFIX PATCHES - Skill (Tier 2)
    // ═══════════════════════════════════════════════════════════════════

    #region Skill Postfixes (Tier 2)

    private static void GetExpectedSuppression_Postfix(object __instance, ref float __result)
    {
        if (OnGetExpectedSuppression == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);
            var result = __result;

            foreach (var handler in OnGetExpectedSuppression.GetInvocationList().Cast<FloatSkillInterceptor>())
            {
                try
                {
                    handler(skill, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetExpectedSuppression handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("skill_expected_suppression", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetExpectedSuppression_Postfix failed: {ex.Message}");
        }
    }

    private static void GetActionPointCost_Postfix(object __instance, ref int __result)
    {
        if (OnGetActionPointCost == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);
            var result = __result;

            foreach (var handler in OnGetActionPointCost.GetInvocationList().Cast<IntSkillInterceptor>())
            {
                try
                {
                    handler(skill, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetActionPointCost handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("skill_ap_cost", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetActionPointCost_Postfix failed: {ex.Message}");
        }
    }

    private static void GetIdealRangeBase_Postfix(object __instance, ref int __result)
    {
        if (OnGetIdealRangeBase == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);
            var result = __result;

            foreach (var handler in OnGetIdealRangeBase.GetInvocationList().Cast<IntSkillInterceptor>())
            {
                try
                {
                    handler(skill, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetIdealRangeBase handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("skill_ideal_range", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetIdealRangeBase_Postfix failed: {ex.Message}");
        }
    }

    private static void GetMaxRangeBase_Postfix(object __instance, ref int __result)
    {
        if (OnGetMaxRangeBase == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);
            var result = __result;

            foreach (var handler in OnGetMaxRangeBase.GetInvocationList().Cast<IntSkillInterceptor>())
            {
                try
                {
                    handler(skill, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetMaxRangeBase handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("skill_max_range", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetMaxRangeBase_Postfix failed: {ex.Message}");
        }
    }

    private static void GetMinRangeBase_Postfix(object __instance, ref int __result)
    {
        if (OnGetMinRangeBase == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);
            var result = __result;

            foreach (var handler in OnGetMinRangeBase.GetInvocationList().Cast<IntSkillInterceptor>())
            {
                try
                {
                    handler(skill, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetMinRangeBase handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("skill_min_range", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetMinRangeBase_Postfix failed: {ex.Message}");
        }
    }

    private static void IsInRange_Postfix(object __instance, ref bool __result)
    {
        if (OnIsInRange == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);
            var result = __result;

            foreach (var handler in OnIsInRange.GetInvocationList().Cast<BoolInterceptor>())
            {
                try
                {
                    handler(skill, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnIsInRange handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("skill_is_in_range", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"IsInRange_Postfix failed: {ex.Message}");
        }
    }

    private static void IsInRangeShape_Postfix(object __instance, ref bool __result)
    {
        if (OnIsInRangeShape == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);
            var result = __result;

            foreach (var handler in OnIsInRangeShape.GetInvocationList().Cast<BoolInterceptor>())
            {
                try
                {
                    handler(skill, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnIsInRangeShape handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("skill_is_in_range_shape", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"IsInRangeShape_Postfix failed: {ex.Message}");
        }
    }

    private static void IsMovementSkill_Postfix(object __instance, ref bool __result)
    {
        if (OnIsMovementSkill == null) return;

        try
        {
            var skillPtr = GetPointer(__instance);
            var skill = new GameObj(skillPtr);
            var result = __result;

            foreach (var handler in OnIsMovementSkill.GetInvocationList().Cast<BoolInterceptor>())
            {
                try
                {
                    handler(skill, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnIsMovementSkill handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("skill_is_movement", new Dictionary<string, object>
            {
                ["skill_ptr"] = skillPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"IsMovementSkill_Postfix failed: {ex.Message}");
        }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  HARMONY POSTFIX PATCHES - Actor
    // ═══════════════════════════════════════════════════════════════════

    #region Actor Postfixes

    private static void HasLineOfSightTo_Postfix(object __instance, ref bool __result, object entity)
    {
        if (OnHasLineOfSightTo == null) return;

        try
        {
            var observerPtr = GetPointer(__instance);
            var targetPtr = GetPointer(entity);
            var observer = new GameObj(observerPtr);
            var target = new GameObj(targetPtr);
            var result = __result;

            foreach (var handler in OnHasLineOfSightTo.GetInvocationList().Cast<LineOfSightInterceptor>())
            {
                try
                {
                    handler(observer, target, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnHasLineOfSightTo handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("actor_los", new Dictionary<string, object>
            {
                ["observer_ptr"] = observerPtr.ToInt64(),
                ["target_ptr"] = targetPtr.ToInt64(),
                ["has_los"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"HasLineOfSightTo_Postfix failed: {ex.Message}");
        }
    }

    // Actor state query postfixes - Morale/Suppression
    private static void GetMoraleMax_Postfix(object __instance, ref int __result, float multiplier) { if (OnActorGetMoraleMax == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorGetMoraleMax.GetInvocationList().Cast<ActorMoraleMaxInterceptor>()) { try { h(actor, multiplier, ref r); } catch { } } __result = r; } catch { } }
    private static void GetMoralePct_Postfix(object __instance, ref float __result) { if (OnActorGetMoralePct == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorGetMoralePct.GetInvocationList().Cast<ActorFloatStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void GetMoraleState_Postfix(object __instance, ref int __result) { if (OnActorGetMoraleState == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorGetMoraleState.GetInvocationList().Cast<ActorIntStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void GetSuppressionPct_Postfix(object __instance, ref float __result) { if (OnActorGetSuppressionPct == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorGetSuppressionPct.GetInvocationList().Cast<ActorFloatStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void GetSuppressionState_Postfix(object __instance, ref int __result, float additionalSuppression) { if (OnActorGetSuppressionState == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorGetSuppressionState.GetInvocationList().Cast<ActorSuppressionStateInterceptor>()) { try { h(actor, additionalSuppression, ref r); } catch { } } __result = r; } catch { } }

    // Actor state query postfixes - Action Points
    private static void GetActionPointsAtTurnStart_Postfix(object __instance, ref int __result) { if (OnActorGetActionPointsAtTurnStart == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorGetActionPointsAtTurnStart.GetInvocationList().Cast<ActorIntStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void GetTurningCost_Postfix(object __instance, ref int __result, int targetDirection) { if (OnActorGetTurningCost == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorGetTurningCost.GetInvocationList().Cast<ActorTurningCostInterceptor>()) { try { h(actor, targetDirection, ref r); } catch { } } __result = r; } catch { } }
    private static void GetTilesMovedThisTurn_Postfix(object __instance, ref int __result) { if (OnActorGetTilesMovedThisTurn == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorGetTilesMovedThisTurn.GetInvocationList().Cast<ActorIntStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void GetTimesAttackedSinceLastTurn_Postfix(object __instance, ref int __result) { if (OnActorGetTimesAttackedSinceLastTurn == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorGetTimesAttackedSinceLastTurn.GetInvocationList().Cast<ActorIntStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }

    // Actor state query postfixes - Boolean state queries
    private static void ActorIsActive_Postfix(object __instance, ref bool __result) { if (OnActorIsActive == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsActive.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsActionPointsSpent_Postfix(object __instance, ref bool __result) { if (OnActorIsActionPointsSpent == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsActionPointsSpent.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsDetectedByFaction_Postfix(object __instance, ref bool __result, byte faction) { if (OnActorIsDetectedByFaction == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsDetectedByFaction.GetInvocationList().Cast<ActorFactionDetectionInterceptor>()) { try { h(actor, faction, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsDying_Postfix(object __instance, ref bool __result) { if (OnActorIsDying == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsDying.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsHeavyWeaponDeployed_Postfix(object __instance, ref bool __result) { if (OnActorIsHeavyWeaponDeployed == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsHeavyWeaponDeployed.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsHiddenToAI_Postfix(object __instance, ref bool __result) { if (OnActorIsHiddenToAI == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsHiddenToAI.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsHiddenToPlayer_Postfix(object __instance, ref bool __result) { if (OnActorIsHiddenToPlayer == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsHiddenToPlayer.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsInfantry_Postfix(object __instance, ref bool __result) { if (OnActorIsInfantry == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsInfantry.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsLeavingMap_Postfix(object __instance, ref bool __result) { if (OnActorIsLeavingMap == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsLeavingMap.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsMinion_Postfix(object __instance, ref bool __result) { if (OnActorIsMinion == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsMinion.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsMoving_Postfix(object __instance, ref bool __result) { if (OnActorIsMoving == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsMoving.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsSelectableByPlayer_Postfix(object __instance, ref bool __result) { if (OnActorIsSelectableByPlayer == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsSelectableByPlayer.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsStunned_Postfix(object __instance, ref bool __result) { if (OnActorIsStunned == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsStunned.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsTurnDone_Postfix(object __instance, ref bool __result) { if (OnActorIsTurnDone == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsTurnDone.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsTurret_Postfix(object __instance, ref bool __result) { if (OnActorIsTurret == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsTurret.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorIsVehicle_Postfix(object __instance, ref bool __result) { if (OnActorIsVehicle == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorIsVehicle.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorCanEnterAnyAdjacentVehicle_Postfix(object __instance, ref bool __result) { if (OnActorCanEnterAnyAdjacentVehicle == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorCanEnterAnyAdjacentVehicle.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }
    private static void ActorHasActed_Postfix(object __instance, ref bool __result) { if (OnActorHasActed == null) return; try { var a = GetPointer(__instance); var actor = new GameObj(a); var r = __result; foreach (var h in OnActorHasActed.GetInvocationList().Cast<ActorBoolStateInterceptor>()) { try { h(actor, ref r); } catch { } } __result = r; } catch { } }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  HARMONY POSTFIX PATCHES - Entity State Methods
    // ═══════════════════════════════════════════════════════════════════

    #region Entity State Postfixes

    private static void GetHitpointsPct_Postfix(object __instance, ref float __result)
    {
        if (OnGetHitpointsPct == null) return;

        try
        {
            var entityPtr = GetPointer(__instance);
            var entity = new GameObj(entityPtr);
            var result = __result;

            foreach (var handler in OnGetHitpointsPct.GetInvocationList().Cast<EntityFloatStateInterceptor>())
            {
                try
                {
                    handler(entity, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetHitpointsPct handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("entity_hitpoints_pct", new Dictionary<string, object>
            {
                ["entity_ptr"] = entityPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetHitpointsPct_Postfix failed: {ex.Message}");
        }
    }

    private static void GetArmorDurabilityPct_Postfix(object __instance, ref float __result)
    {
        if (OnGetArmorDurabilityPct == null) return;

        try
        {
            var entityPtr = GetPointer(__instance);
            var entity = new GameObj(entityPtr);
            var result = __result;

            foreach (var handler in OnGetArmorDurabilityPct.GetInvocationList().Cast<EntityFloatStateInterceptor>())
            {
                try
                {
                    handler(entity, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetArmorDurabilityPct handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("entity_armor_durability_pct", new Dictionary<string, object>
            {
                ["entity_ptr"] = entityPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetArmorDurabilityPct_Postfix failed: {ex.Message}");
        }
    }

    private static void GetCoverUsage_Postfix(object __instance, ref int __result)
    {
        if (OnGetCoverUsage == null) return;

        try
        {
            var entityPtr = GetPointer(__instance);
            var entity = new GameObj(entityPtr);
            var result = __result;

            foreach (var handler in OnGetCoverUsage.GetInvocationList().Cast<EntityIntStateInterceptor>())
            {
                try
                {
                    handler(entity, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetCoverUsage handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("entity_cover_usage", new Dictionary<string, object>
            {
                ["entity_ptr"] = entityPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetCoverUsage_Postfix failed: {ex.Message}");
        }
    }

    private static void GetProvidedCover_Postfix(object __instance, ref object __result)
    {
        if (OnGetProvidedCover == null) return;

        try
        {
            var entityPtr = GetPointer(__instance);
            var entity = new GameObj(entityPtr);
            var resultPtr = GetPointer(__result);

            foreach (var handler in OnGetProvidedCover.GetInvocationList().Cast<EntityObjectStateInterceptor>())
            {
                try
                {
                    handler(entity, ref resultPtr);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetProvidedCover handler failed: {ex.Message}");
                }
            }

            // Note: Cannot easily change the object reference in postfix for Il2Cpp objects
            // The interceptor can read/log but modification requires prefix or native patching

            FireLuaEvent("entity_provided_cover", new Dictionary<string, object>
            {
                ["entity_ptr"] = entityPtr.ToInt64(),
                ["cover_ptr"] = resultPtr.ToInt64()
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetProvidedCover_Postfix failed: {ex.Message}");
        }
    }

    private static void IsDiscovered_Postfix(object __instance, ref bool __result)
    {
        if (OnIsDiscovered == null) return;

        try
        {
            var entityPtr = GetPointer(__instance);
            var entity = new GameObj(entityPtr);
            var result = __result;

            foreach (var handler in OnIsDiscovered.GetInvocationList().Cast<EntityBoolStateInterceptor>())
            {
                try
                {
                    handler(entity, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnIsDiscovered handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("entity_is_discovered", new Dictionary<string, object>
            {
                ["entity_ptr"] = entityPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"IsDiscovered_Postfix failed: {ex.Message}");
        }
    }

    private static void GetLastSkillUsed_Postfix(object __instance, ref object __result)
    {
        if (OnGetLastSkillUsed == null) return;

        try
        {
            var entityPtr = GetPointer(__instance);
            var entity = new GameObj(entityPtr);
            var resultPtr = GetPointer(__result);

            foreach (var handler in OnGetLastSkillUsed.GetInvocationList().Cast<EntityObjectStateInterceptor>())
            {
                try
                {
                    handler(entity, ref resultPtr);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetLastSkillUsed handler failed: {ex.Message}");
                }
            }

            // Note: Cannot easily change the object reference in postfix for Il2Cpp objects
            // The interceptor can read/log but modification requires prefix or native patching

            FireLuaEvent("entity_last_skill_used", new Dictionary<string, object>
            {
                ["entity_ptr"] = entityPtr.ToInt64(),
                ["skill_ptr"] = resultPtr.ToInt64()
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetLastSkillUsed_Postfix failed: {ex.Message}");
        }
    }

    private static void GetScaleRange_Postfix(object __instance, ref object __result)
    {
        if (OnGetScaleRange == null) return;

        try
        {
            var entityPtr = GetPointer(__instance);
            var entity = new GameObj(entityPtr);

            // Parse Vector2 result
            var result = new Vector2Result();
            if (__result is Il2CppObjectBase resultObj)
            {
                var resultPtr = resultObj.Pointer;
                // Vector2 layout: X at offset 0, Y at offset 4
                result.X = System.Runtime.InteropServices.Marshal.PtrToStructure<float>(resultPtr);
                result.Y = System.Runtime.InteropServices.Marshal.PtrToStructure<float>(resultPtr + 4);
            }
            else if (__result != null)
            {
                // Try to extract from value type via reflection
                var resultType = __result.GetType();
                var xField = resultType.GetField("x") ?? resultType.GetField("X");
                var yField = resultType.GetField("y") ?? resultType.GetField("Y");
                if (xField != null) result.X = Convert.ToSingle(xField.GetValue(__result));
                if (yField != null) result.Y = Convert.ToSingle(yField.GetValue(__result));
            }

            foreach (var handler in OnGetScaleRange.GetInvocationList().Cast<EntityVector2StateInterceptor>())
            {
                try
                {
                    handler(entity, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnGetScaleRange handler failed: {ex.Message}");
                }
            }

            // Write back modified result if possible
            if (__result is Il2CppObjectBase resultObjWrite)
            {
                var resultPtr = resultObjWrite.Pointer;
                System.Runtime.InteropServices.Marshal.StructureToPtr(result.X, resultPtr, false);
                System.Runtime.InteropServices.Marshal.StructureToPtr(result.Y, resultPtr + 4, false);
            }

            FireLuaEvent("entity_scale_range", new Dictionary<string, object>
            {
                ["entity_ptr"] = entityPtr.ToInt64(),
                ["min_scale"] = result.X,
                ["max_scale"] = result.Y
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"GetScaleRange_Postfix failed: {ex.Message}");
        }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  HARMONY POSTFIX PATCHES - Tile
    // ═══════════════════════════════════════════════════════════════════

    #region Tile Postfixes

    private static void TileHasLineOfSightTo_Postfix(object __instance, ref bool __result, object toTile, byte flags)
    {
        if (OnTileHasLineOfSightTo == null) return;

        try
        {
            var fromTilePtr = GetPointer(__instance);
            var toTilePtr = GetPointer(toTile);
            var fromTileObj = new GameObj(fromTilePtr);
            var toTileObj = new GameObj(toTilePtr);
            var result = __result;

            foreach (var handler in OnTileHasLineOfSightTo.GetInvocationList().Cast<TileLoSInterceptor>())
            {
                try
                {
                    handler(fromTileObj, toTileObj, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnTileHasLineOfSightTo handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("tile_has_los", new Dictionary<string, object>
            {
                ["from_tile_ptr"] = fromTilePtr.ToInt64(),
                ["to_tile_ptr"] = toTilePtr.ToInt64(),
                ["has_los"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"TileHasLineOfSightTo_Postfix failed: {ex.Message}");
        }
    }

    private static void TileIsBlockingLineOfSight_Postfix(object __instance, ref bool __result)
    {
        if (OnTileIsBlockingLineOfSight == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var tile = new GameObj(tilePtr);
            var result = __result;

            foreach (var handler in OnTileIsBlockingLineOfSight.GetInvocationList().Cast<TileBlockerInterceptor>())
            {
                try
                {
                    handler(tile, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnTileIsBlockingLineOfSight handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("tile_blocking_los", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["is_blocking"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"TileIsBlockingLineOfSight_Postfix failed: {ex.Message}");
        }
    }

    private static void TileGetCover_Postfix(object __instance, ref int __result, int direction, object entity, object param4, bool param5)
    {
        if (OnTileGetCover == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var entityPtr = GetPointer(entity);
            var tile = new GameObj(tilePtr);
            var entityObj = new GameObj(entityPtr);
            var result = __result;

            foreach (var handler in OnTileGetCover.GetInvocationList().Cast<TileCoverInterceptor>())
            {
                try
                {
                    handler(tile, direction, entityObj, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnTileGetCover handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("tile_get_cover", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["direction"] = direction,
                ["cover"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"TileGetCover_Postfix failed: {ex.Message}");
        }
    }

    private static void TileGetCoverMask_Postfix(object __instance, ref int __result)
    {
        if (OnTileGetCoverMask == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var tile = new GameObj(tilePtr);
            var result = __result;

            foreach (var handler in OnTileGetCoverMask.GetInvocationList().Cast<TileCoverMaskInterceptor>())
            {
                try
                {
                    handler(tile, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnTileGetCoverMask handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("tile_get_cover_mask", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["cover_mask"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"TileGetCoverMask_Postfix failed: {ex.Message}");
        }
    }

    private static void TileGetEntityProvidedCover_Postfix(object __instance, ref int __result)
    {
        if (OnTileGetEntityProvidedCover == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var tile = new GameObj(tilePtr);
            var result = __result;

            foreach (var handler in OnTileGetEntityProvidedCover.GetInvocationList().Cast<TileEntityCoverInterceptor>())
            {
                try
                {
                    handler(tile, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnTileGetEntityProvidedCover handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("tile_entity_cover", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["cover"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"TileGetEntityProvidedCover_Postfix failed: {ex.Message}");
        }
    }

    private static void TileCanBeEntered_Postfix(object __instance, ref bool __result)
    {
        if (OnTileCanBeEntered == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var tile = new GameObj(tilePtr);
            var result = __result;

            foreach (var handler in OnTileCanBeEntered.GetInvocationList().Cast<TileEntryInterceptor>())
            {
                try
                {
                    handler(tile, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnTileCanBeEntered handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("tile_can_enter", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["can_enter"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"TileCanBeEntered_Postfix failed: {ex.Message}");
        }
    }

    private static void TileCanBeEnteredBy_Postfix(object __instance, ref bool __result, object entity)
    {
        if (OnTileCanBeEnteredBy == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var entityPtr = GetPointer(entity);
            var tile = new GameObj(tilePtr);
            var entityObj = new GameObj(entityPtr);
            var result = __result;

            foreach (var handler in OnTileCanBeEnteredBy.GetInvocationList().Cast<TileEntityEntryInterceptor>())
            {
                try
                {
                    handler(tile, entityObj, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnTileCanBeEnteredBy handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("tile_can_enter_by", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["entity_ptr"] = entityPtr.ToInt64(),
                ["can_enter"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"TileCanBeEnteredBy_Postfix failed: {ex.Message}");
        }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  HARMONY POSTFIX PATCHES - BaseTile
    // ═══════════════════════════════════════════════════════════════════

    #region BaseTile Postfixes

    private static void BaseTileHasCover_Postfix(object __instance, ref bool __result)
    {
        if (OnBaseTileHasCover == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var tile = new GameObj(tilePtr);
            var result = __result;

            foreach (var handler in OnBaseTileHasCover.GetInvocationList().Cast<BaseTileCoverCheckInterceptor>())
            {
                try
                {
                    handler(tile, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnBaseTileHasCover handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("basetile_has_cover", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["has_cover"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"BaseTileHasCover_Postfix failed: {ex.Message}");
        }
    }

    private static void BaseTileHasHalfCover_Postfix(object __instance, ref bool __result)
    {
        if (OnBaseTileHasHalfCover == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var tile = new GameObj(tilePtr);
            var result = __result;

            foreach (var handler in OnBaseTileHasHalfCover.GetInvocationList().Cast<BaseTileHalfCoverInterceptor>())
            {
                try
                {
                    handler(tile, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnBaseTileHasHalfCover handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("basetile_has_half_cover", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["has_half_cover"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"BaseTileHasHalfCover_Postfix failed: {ex.Message}");
        }
    }

    private static void BaseTileHasHalfCoverInDir_Postfix(object __instance, ref bool __result, int direction)
    {
        if (OnBaseTileHasHalfCoverInDir == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var tile = new GameObj(tilePtr);
            var result = __result;

            foreach (var handler in OnBaseTileHasHalfCoverInDir.GetInvocationList().Cast<BaseTileDirHalfCoverInterceptor>())
            {
                try
                {
                    handler(tile, direction, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnBaseTileHasHalfCoverInDir handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("basetile_has_half_cover_dir", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["direction"] = direction,
                ["has_half_cover"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"BaseTileHasHalfCoverInDir_Postfix failed: {ex.Message}");
        }
    }

    private static void BaseTileIsMovementBlocked_Postfix(object __instance, ref bool __result, int direction)
    {
        if (OnBaseTileIsMovementBlocked == null) return;

        try
        {
            var tilePtr = GetPointer(__instance);
            var tile = new GameObj(tilePtr);
            var result = __result;

            foreach (var handler in OnBaseTileIsMovementBlocked.GetInvocationList().Cast<BaseTileMovementBlockedInterceptor>())
            {
                try
                {
                    handler(tile, direction, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnBaseTileIsMovementBlocked handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("basetile_movement_blocked", new Dictionary<string, object>
            {
                ["tile_ptr"] = tilePtr.ToInt64(),
                ["direction"] = direction,
                ["is_blocked"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"BaseTileIsMovementBlocked_Postfix failed: {ex.Message}");
        }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  HARMONY POSTFIX PATCHES - LineOfSight
    // ═══════════════════════════════════════════════════════════════════

    #region LineOfSight Postfixes

    private static void LineOfSightRayTrace_Postfix(ref bool __result, object fromTile, object toTile, byte flags)
    {
        if (OnLineOfSightRayTrace == null) return;

        try
        {
            var fromTilePtr = GetPointer(fromTile);
            var toTilePtr = GetPointer(toTile);
            var fromTileObj = new GameObj(fromTilePtr);
            var toTileObj = new GameObj(toTilePtr);
            var result = __result;

            foreach (var handler in OnLineOfSightRayTrace.GetInvocationList().Cast<LineOfSightRayTraceInterceptor>())
            {
                try
                {
                    handler(fromTileObj, toTileObj, flags, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnLineOfSightRayTrace handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("los_raytrace", new Dictionary<string, object>
            {
                ["from_tile_ptr"] = fromTilePtr.ToInt64(),
                ["to_tile_ptr"] = toTilePtr.ToInt64(),
                ["flags"] = (int)flags,
                ["has_los"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"LineOfSightRayTrace_Postfix failed: {ex.Message}");
        }
    }

    private static void LineOfSightIsNearTileCorner_Postfix(ref bool __result, float posX, float posY)
    {
        if (OnLineOfSightIsNearTileCorner == null) return;

        try
        {
            var result = __result;

            foreach (var handler in OnLineOfSightIsNearTileCorner.GetInvocationList().Cast<LineOfSightCornerInterceptor>())
            {
                try
                {
                    handler(posX, posY, ref result);
                }
                catch (Exception ex)
                {
                    ModError.WarnInternal("Intercept", $"OnLineOfSightIsNearTileCorner handler failed: {ex.Message}");
                }
            }

            __result = result;

            FireLuaEvent("los_near_corner", new Dictionary<string, object>
            {
                ["pos_x"] = posX,
                ["pos_y"] = posY,
                ["is_near_corner"] = result
            });
        }
        catch (Exception ex)
        {
            ModError.WarnInternal("Intercept", $"LineOfSightIsNearTileCorner_Postfix failed: {ex.Message}");
        }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  MOVEMENT POSTFIXES
    // ═══════════════════════════════════════════════════════════════════

    #region Movement Postfixes

    private static void GetMaxMovementSpeed_Postfix(object __instance, ref float __result, int mode)
    {
        if (OnGetMaxMovementSpeed == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnGetMaxMovementSpeed.GetInvocationList().Cast<MovementFloatInterceptor>())
            {
                try { handler(instance, mode, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnGetMaxMovementSpeed handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("movement_max_speed", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["movement_mode"] = mode,
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"GetMaxMovementSpeed_Postfix failed: {ex.Message}"); }
    }

    private static void GetTotalPathCost_Postfix(object __instance, ref int __result, object path, object movementFlags, object actor, object facing)
    {
        if (OnGetTotalPathCost == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var pathPtr = GetPointer(path);
            var actorPtr = GetPointer(actor);
            var instance = new GameObj(instancePtr);
            var pathObj = new GameObj(pathPtr);
            var actorObj = new GameObj(actorPtr);
            var result = __result;

            foreach (var handler in OnGetTotalPathCost.GetInvocationList().Cast<PathCostInterceptor>())
            {
                try { handler(instance, pathObj, actorObj, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnGetTotalPathCost handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("movement_path_cost", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["path_ptr"] = pathPtr.ToInt64(),
                ["actor_ptr"] = actorPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"GetTotalPathCost_Postfix failed: {ex.Message}"); }
    }

    private static void GetTurnSpeed_Postfix(object __instance, ref float __result, int mode)
    {
        if (OnGetTurnSpeed == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnGetTurnSpeed.GetInvocationList().Cast<MovementFloatInterceptor>())
            {
                try { handler(instance, mode, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnGetTurnSpeed handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("movement_turn_speed", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["movement_mode"] = mode,
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"GetTurnSpeed_Postfix failed: {ex.Message}"); }
    }

    private static void GetSlowdownDistance_Postfix(object __instance, ref float __result, int mode)
    {
        if (OnGetSlowdownDistance == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnGetSlowdownDistance.GetInvocationList().Cast<MovementFloatInterceptor>())
            {
                try { handler(instance, mode, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnGetSlowdownDistance handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("movement_slowdown_distance", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["movement_mode"] = mode,
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"GetSlowdownDistance_Postfix failed: {ex.Message}"); }
    }

    private static void GetMaxAngleTurnSlowdown_Postfix(object __instance, ref float __result, int mode)
    {
        if (OnGetMaxAngleTurnSlowdown == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnGetMaxAngleTurnSlowdown.GetInvocationList().Cast<MovementFloatInterceptor>())
            {
                try { handler(instance, mode, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnGetMaxAngleTurnSlowdown handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("movement_max_angle_turn_slowdown", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["movement_mode"] = mode,
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"GetMaxAngleTurnSlowdown_Postfix failed: {ex.Message}"); }
    }

    private static void ClipPathToCost_Postfix(object __instance, ref int __result, object path, object lastTile, object lastTileIndex, object movementFlags, int maxCost, object actor, object facing, bool modifyPath)
    {
        if (OnClipPathToCost == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var pathPtr = GetPointer(path);
            var actorPtr = GetPointer(actor);
            var instance = new GameObj(instancePtr);
            var pathObj = new GameObj(pathPtr);
            var actorObj = new GameObj(actorPtr);

            var clipResult = new ClipPathResult
            {
                ActualCost = __result,
                MaxCost = maxCost,
                ClipIndex = -1
            };

            foreach (var handler in OnClipPathToCost.GetInvocationList().Cast<ClipPathInterceptor>())
            {
                try { handler(instance, pathObj, actorObj, ref clipResult); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnClipPathToCost handler failed: {ex.Message}"); }
            }

            __result = clipResult.ActualCost;
            FireLuaEvent("movement_clip_path", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["path_ptr"] = pathPtr.ToInt64(),
                ["actor_ptr"] = actorPtr.ToInt64(),
                ["actual_cost"] = clipResult.ActualCost,
                ["max_cost"] = clipResult.MaxCost,
                ["clip_index"] = clipResult.ClipIndex
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"ClipPathToCost_Postfix failed: {ex.Message}"); }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  STRATEGY POSTFIXES
    // ═══════════════════════════════════════════════════════════════════

    #region Strategy UnitLeaderAttributes Postfixes

    private static void StrategyGetActionPoints_Postfix(object __instance, ref int __result)
    {
        if (OnStrategyGetActionPoints == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnStrategyGetActionPoints.GetInvocationList().Cast<StrategyIntIntercept>())
            {
                try { handler(instance, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnStrategyGetActionPoints handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("strategy_action_points", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"StrategyGetActionPoints_Postfix failed: {ex.Message}"); }
    }

    private static void StrategyGetHitpointsPerElement_Postfix(object __instance, ref int __result)
    {
        if (OnStrategyGetHitpointsPerElement == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnStrategyGetHitpointsPerElement.GetInvocationList().Cast<StrategyIntIntercept>())
            {
                try { handler(instance, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnStrategyGetHitpointsPerElement handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("strategy_hitpoints_per_element", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"StrategyGetHitpointsPerElement_Postfix failed: {ex.Message}"); }
    }

    private static void StrategyGetDamageSustainedMult_Postfix(object __instance, ref float __result)
    {
        if (OnStrategyGetDamageSustainedMult == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnStrategyGetDamageSustainedMult.GetInvocationList().Cast<StrategyFloatIntercept>())
            {
                try { handler(instance, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnStrategyGetDamageSustainedMult handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("strategy_damage_sustained_mult", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"StrategyGetDamageSustainedMult_Postfix failed: {ex.Message}"); }
    }

    #endregion

    #region Strategy BaseUnitLeader Postfixes

    private static void StrategyGetHitpointsPct_Postfix(object __instance, ref float __result)
    {
        if (OnStrategyGetHitpointsPct == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnStrategyGetHitpointsPct.GetInvocationList().Cast<StrategyFloatIntercept>())
            {
                try { handler(instance, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnStrategyGetHitpointsPct handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("strategy_hitpoints_pct", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"StrategyGetHitpointsPct_Postfix failed: {ex.Message}"); }
    }

    private static void StrategyCanBePromoted_Postfix(object __instance, ref bool __result)
    {
        if (OnStrategyCanBePromoted == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnStrategyCanBePromoted.GetInvocationList().Cast<StrategyBoolIntercept>())
            {
                try { handler(instance, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnStrategyCanBePromoted handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("strategy_can_be_promoted", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"StrategyCanBePromoted_Postfix failed: {ex.Message}"); }
    }

    private static void StrategyCanBeDemoted_Postfix(object __instance, ref bool __result)
    {
        if (OnStrategyCanBeDemoted == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnStrategyCanBeDemoted.GetInvocationList().Cast<StrategyBoolIntercept>())
            {
                try { handler(instance, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnStrategyCanBeDemoted handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("strategy_can_be_demoted", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"StrategyCanBeDemoted_Postfix failed: {ex.Message}"); }
    }

    private static void StrategyGetEntityProperty_Postfix(object __instance, ref float __result, object propertyType)
    {
        if (OnStrategyGetEntityProperty == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            int propertyTypeInt = 0;
            if (propertyType != null)
            {
                try { propertyTypeInt = Convert.ToInt32(propertyType); }
                catch { }
            }

            foreach (var handler in OnStrategyGetEntityProperty.GetInvocationList().Cast<StrategyEntityIntercept>())
            {
                try { handler(instance, propertyTypeInt, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnStrategyGetEntityProperty handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("strategy_entity_property", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["property_type"] = propertyTypeInt,
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"StrategyGetEntityProperty_Postfix failed: {ex.Message}"); }
    }

    #endregion

    #region Strategy Vehicle Postfixes

    private static void StrategyGetVehicleArmor_Postfix(object __instance, ref int __result)
    {
        if (OnStrategyGetVehicleArmor == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnStrategyGetVehicleArmor.GetInvocationList().Cast<StrategyIntIntercept>())
            {
                try { handler(instance, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnStrategyGetVehicleArmor handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("strategy_vehicle_armor", new Dictionary<string, object>
            {
                ["instance_ptr"] = instancePtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"StrategyGetVehicleArmor_Postfix failed: {ex.Message}"); }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  AI BEHAVIOR POSTFIXES
    // ═══════════════════════════════════════════════════════════════════

    #region AI Behavior Postfixes

    private static void AIGetAttackScore_Postfix(object __instance, ref float __result, object target)
    {
        if (OnAIGetAttackScore == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var targetPtr = GetPointer(target);
            var instance = new GameObj(instancePtr);
            var targetObj = new GameObj(targetPtr);
            var result = __result;

            foreach (var handler in OnAIGetAttackScore.GetInvocationList().Cast<AIFloatValueInterceptor>())
            {
                try { handler(instance, targetObj, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnAIGetAttackScore handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("ai_attack_score", new Dictionary<string, object>
            {
                ["ai_ptr"] = instancePtr.ToInt64(),
                ["target_ptr"] = targetPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"AIGetAttackScore_Postfix failed: {ex.Message}"); }
    }

    private static void AIGetThreatValue_Postfix(object __instance, ref float __result, object target)
    {
        if (OnAIGetThreatValue == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var targetPtr = GetPointer(target);
            var instance = new GameObj(instancePtr);
            var targetObj = new GameObj(targetPtr);
            var result = __result;

            foreach (var handler in OnAIGetThreatValue.GetInvocationList().Cast<AIFloatValueInterceptor>())
            {
                try { handler(instance, targetObj, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnAIGetThreatValue handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("ai_threat_value", new Dictionary<string, object>
            {
                ["ai_ptr"] = instancePtr.ToInt64(),
                ["target_ptr"] = targetPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"AIGetThreatValue_Postfix failed: {ex.Message}"); }
    }

    private static void AIGetActionPriority_Postfix(object __instance, ref int __result, object action)
    {
        if (OnAIGetActionPriority == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var actionPtr = GetPointer(action);
            var instance = new GameObj(instancePtr);
            var actionObj = new GameObj(actionPtr);
            var result = __result;

            foreach (var handler in OnAIGetActionPriority.GetInvocationList().Cast<AIIntValueInterceptor>())
            {
                try { handler(instance, actionObj, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnAIGetActionPriority handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("ai_action_priority", new Dictionary<string, object>
            {
                ["ai_ptr"] = instancePtr.ToInt64(),
                ["action_ptr"] = actionPtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"AIGetActionPriority_Postfix failed: {ex.Message}"); }
    }

    private static void AIShouldFlee_Postfix(object __instance, ref bool __result)
    {
        if (OnAIShouldFlee == null) return;

        try
        {
            var instancePtr = GetPointer(__instance);
            var instance = new GameObj(instancePtr);
            var result = __result;

            foreach (var handler in OnAIShouldFlee.GetInvocationList().Cast<AIBoolDecisionInterceptor>())
            {
                try { handler(instance, GameObj.Null, ref result); }
                catch (Exception ex) { ModError.WarnInternal("Intercept", $"OnAIShouldFlee handler failed: {ex.Message}"); }
            }

            __result = result;
            FireLuaEvent("ai_should_flee", new Dictionary<string, object>
            {
                ["ai_ptr"] = instancePtr.ToInt64(),
                ["result"] = result
            });
        }
        catch (Exception ex) { ModError.WarnInternal("Intercept", $"AIShouldFlee_Postfix failed: {ex.Message}"); }
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════
    //  CONSOLE COMMANDS
    // ═══════════════════════════════════════════════════════════════════

    private static void RegisterConsoleCommands()
    {
        DevConsole.RegisterCommand("propertyinterceptors", "", "List registered property interceptors", args =>
        {
            var lines = new List<string> { "Registered Property Interceptors:" };

            void AddEventInfo(string name, Delegate evt)
            {
                var count = evt?.GetInvocationList()?.Length ?? 0;
                if (count > 0)
                    lines.Add($"  {name}: {count} handler(s)");
            }

            // Tier 1: EntityProperties
            AddEventInfo("OnGetDamage", OnGetDamage);
            AddEventInfo("OnGetAccuracy", OnGetAccuracy);
            AddEventInfo("OnGetArmor", OnGetArmor);
            AddEventInfo("OnGetConcealment", OnGetConcealment);
            AddEventInfo("OnGetDetection", OnGetDetection);
            AddEventInfo("OnGetVision", OnGetVision);

            // Tier 1: Skill
            AddEventInfo("OnGetHitChance", OnGetHitChance);
            AddEventInfo("OnGetExpectedDamage", OnGetExpectedDamage);
            AddEventInfo("OnGetCoverMult", OnGetCoverMult);

            // Tier 1: Actor
            AddEventInfo("OnHasLineOfSightTo", OnHasLineOfSightTo);

            // Tier 2: Skill Range and Cost
            AddEventInfo("OnGetExpectedSuppression", OnGetExpectedSuppression);
            AddEventInfo("OnGetActionPointCost", OnGetActionPointCost);
            AddEventInfo("OnGetIdealRangeBase", OnGetIdealRangeBase);
            AddEventInfo("OnGetMaxRangeBase", OnGetMaxRangeBase);
            AddEventInfo("OnGetMinRangeBase", OnGetMinRangeBase);
            AddEventInfo("OnIsInRange", OnIsInRange);
            AddEventInfo("OnIsInRangeShape", OnIsInRangeShape);
            AddEventInfo("OnIsMovementSkill", OnIsMovementSkill);

            // Tier 2: Entity State
            AddEventInfo("OnGetHitpointsPct", OnGetHitpointsPct);
            AddEventInfo("OnGetArmorDurabilityPct", OnGetArmorDurabilityPct);
            AddEventInfo("OnGetCoverUsage", OnGetCoverUsage);
            AddEventInfo("OnGetProvidedCover", OnGetProvidedCover);
            AddEventInfo("OnIsDiscovered", OnIsDiscovered);
            AddEventInfo("OnGetLastSkillUsed", OnGetLastSkillUsed);
            AddEventInfo("OnGetScaleRange", OnGetScaleRange);

            // Tile
            AddEventInfo("OnTileHasLineOfSightTo", OnTileHasLineOfSightTo);
            AddEventInfo("OnTileIsBlockingLineOfSight", OnTileIsBlockingLineOfSight);
            AddEventInfo("OnTileGetCover", OnTileGetCover);
            AddEventInfo("OnTileGetCoverMask", OnTileGetCoverMask);
            AddEventInfo("OnTileGetEntityProvidedCover", OnTileGetEntityProvidedCover);
            AddEventInfo("OnTileCanBeEntered", OnTileCanBeEntered);
            AddEventInfo("OnTileCanBeEnteredBy", OnTileCanBeEnteredBy);

            // BaseTile
            AddEventInfo("OnBaseTileHasCover", OnBaseTileHasCover);
            AddEventInfo("OnBaseTileHasHalfCover", OnBaseTileHasHalfCover);
            AddEventInfo("OnBaseTileHasHalfCoverInDir", OnBaseTileHasHalfCoverInDir);
            AddEventInfo("OnBaseTileIsMovementBlocked", OnBaseTileIsMovementBlocked);

            // LineOfSight
            AddEventInfo("OnLineOfSightRayTrace", OnLineOfSightRayTrace);
            AddEventInfo("OnLineOfSightIsNearTileCorner", OnLineOfSightIsNearTileCorner);

            // Movement
            AddEventInfo("OnGetMaxMovementSpeed", OnGetMaxMovementSpeed);
            AddEventInfo("OnGetTotalPathCost", OnGetTotalPathCost);
            AddEventInfo("OnGetTurnSpeed", OnGetTurnSpeed);
            AddEventInfo("OnGetSlowdownDistance", OnGetSlowdownDistance);
            AddEventInfo("OnGetMaxAngleTurnSlowdown", OnGetMaxAngleTurnSlowdown);
            AddEventInfo("OnClipPathToCost", OnClipPathToCost);

            // Strategy Layer
            AddEventInfo("OnStrategyGetActionPoints", OnStrategyGetActionPoints);
            AddEventInfo("OnStrategyGetHitpointsPerElement", OnStrategyGetHitpointsPerElement);
            AddEventInfo("OnStrategyGetDamageSustainedMult", OnStrategyGetDamageSustainedMult);
            AddEventInfo("OnStrategyGetHitpointsPct", OnStrategyGetHitpointsPct);
            AddEventInfo("OnStrategyCanBePromoted", OnStrategyCanBePromoted);
            AddEventInfo("OnStrategyCanBeDemoted", OnStrategyCanBeDemoted);
            AddEventInfo("OnStrategyGetEntityProperty", OnStrategyGetEntityProperty);
            AddEventInfo("OnStrategyGetVehicleArmor", OnStrategyGetVehicleArmor);

            // AI Behavior
            AddEventInfo("OnAIGetAttackScore", OnAIGetAttackScore);
            AddEventInfo("OnAIGetThreatValue", OnAIGetThreatValue);
            AddEventInfo("OnAIGetActionPriority", OnAIGetActionPriority);
            AddEventInfo("OnAIShouldFlee", OnAIShouldFlee);

            return lines.Count > 1 ? string.Join("\n", lines) : "No interceptors registered";
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  EXTENSION GUIDE - How to add more interceptors
    // ═══════════════════════════════════════════════════════════════════

    /*
     * ADDING NEW PROPERTY INTERCEPTORS
     *
     * To add interception for a new property (Tier 2-4), follow these steps:
     *
     * 1. DELEGATE TYPE
     *    Define a delegate with the appropriate signature:
     *
     *    public delegate void MyIntercept(GameObj context, ref ResultType result);
     *
     * 2. EVENT
     *    Add a static event in the appropriate tier region:
     *
     *    public static event MyIntercept OnGetMyProperty;
     *
     * 3. PATCH METHOD
     *    Add a postfix patch method following the pattern:
     *
     *    private static void GetMyProperty_Postfix(object __instance, ref ReturnType __result)
     *    {
     *        if (OnGetMyProperty == null) return;
     *
     *        try
     *        {
     *            var ptr = GetPointer(__instance);
     *            var obj = new GameObj(ptr);
     *            var result = __result;
     *
     *            foreach (var handler in OnGetMyProperty.GetInvocationList().Cast<MyIntercept>())
     *            {
     *                try { handler(obj, ref result); }
     *                catch (Exception ex) { ModError.WarnInternal(...); }
     *            }
     *
     *            __result = result;
     *            FireLuaEvent("property_myproperty", new Dictionary<string, object> { ... });
     *        }
     *        catch (Exception ex) { ModError.WarnInternal(...); }
     *    }
     *
     * 4. INITIALIZATION
     *    Add the patch call in Initialize():
     *
     *    patchCount += PatchEntityPropertyMethod("GetMyProperty", nameof(GetMyProperty_Postfix));
     *
     * 5. DOCUMENTATION
     *    Add XML documentation explaining:
     *    - What the property represents
     *    - The formula used to compute it (from Ghidra analysis)
     *    - Relevant memory offsets
     *    - Use cases for modification
     *
     * TIER GUIDELINES:
     * - Tier 1: Core combat properties (damage, accuracy, armor, hit chance, LOS)
     * - Tier 2: Movement properties (AP costs, movement range, terrain modifiers)
     * - Tier 3: Status effect properties (suppression, morale, stun duration)
     * - Tier 4: Specialized properties (malfunction chance, cooldown reduction, etc.)
     *
     * GHIDRA ANALYSIS WORKFLOW:
     * 1. Use mcp__ghidra__search_functions_by_name to find the method
     * 2. Use mcp__ghidra__decompile_function to understand the formula
     * 3. Document the offsets and calculations in comments
     * 4. Test with the console commands to verify patching works
     */
}

// ═══════════════════════════════════════════════════════════════════════════════
//  TODO: TIER 3-4 PROPERTIES (spawn agents to implement these)
// ═══════════════════════════════════════════════════════════════════════════════

/*
 * TIER 2: Movement Properties (DONE - Skill range/cost interceptors implemented)
 * - Skill.GetExpectedSuppression @ 0x1806db2d0: Suppression preview
 * - Skill.GetActionPointCost @ 0x1806d8e80: AP cost calculation
 * - Skill.GetIdealRangeBase @ 0x1806dbea0: Optimal engagement range
 * - Skill.GetMaxRangeBase @ 0x1806dc8b0: Maximum range
 * - Skill.GetMinRangeBase @ 0x1806dc980: Minimum range
 * - Skill.IsInRange @ 0x1806de4f0: Range validity check
 * - Skill.IsInRangeShape @ 0x1806de390: Shape-based range check
 * - Skill.IsMovementSkill @ 0x1806de730: Movement skill type check
 *
 * TIER 3: Combat Status Properties
 * - EntityProperties.GetSuppression @ 0x18060c780
 * - EntityProperties.GetDiscipline @ 0x18060bdd0
 * - EntityProperties.GetArmorPenetration @ 0x18060bab0
 * - EntityProperties.GetDamageDropoff @ 0x18060bcd0
 * - EntityProperties.GetAccuracyDropoff @ 0x18060b9c0
 *
 * TIER 4: Specialized Properties
 * - EntityProperties.GetHitpointsPerElement @ 0x18060be10
 * - EntityProperties.GetMaxHitpoints @ 0x18060be40
 * - EntityProperties.GetArmorDurabilityPerElement @ 0x18060ba50
 * - EntityProperties.GetDamageToArmorDurability @ 0x18060bd30
 * - Skill.GetCooldown
 *
 * Each tier should be implemented by a separate agent to parallelize work.
 */
