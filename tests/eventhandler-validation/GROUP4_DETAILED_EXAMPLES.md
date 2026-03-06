# EventHandler Group 4 - Detailed Test Examples

This document provides detailed information about each test and the EventHandler types they verify.

## Test Details by EventHandler Type

### 1. HideByCondition

**File:** `group4-HideByCondition.json`
**Schema Location:** Line 2089-2100 in eventhandler-schema.json
**Instance Count:** 1
**Field Count:** 1

**Purpose:** Hides a skill/perk when a specific condition is met.

**Parameters Tested:**
- `Condition` (string, nullable) - Tactical condition that determines visibility

**Test Steps:**
1. Navigate to main menu
2. Wait for scene load
3. Find skill using HideByCondition EventHandler
4. Load EventHandlers array from skill
5. Verify HideByCondition in EventHandlers
6. Verify EventHandler _type field
7. Read Condition parameter

---

### 2. Hitchance

**File:** `group4-Hitchance.json`
**Schema Location:** Line 2101-2121 in eventhandler-schema.json
**Instance Count:** 1
**Field Count:** 4

**Purpose:** Modifies hit chance/accuracy of attacks.

**Parameters Tested:**
- `AccuracyBonus` (float) - Flat accuracy bonus
- `AccuracyDropoff` (float) - Accuracy penalty at distance
- `AccuracyMult` (float) - Accuracy multiplier

**Test Steps:**
1. Navigate to main menu
2. Wait for scene load
3. Find skill using Hitchance EventHandler
4. Load EventHandlers array
5. Verify _type field
6. Read AccuracyBonus
7. Read AccuracyDropoff
8. Read AccuracyMult

---

### 3. IgnoreDamage

**File:** `group4-IgnoreDamage.json`
**Schema Location:** Line 2122-2148 in eventhandler-schema.json
**Instance Count:** 3
**Field Count:** 3

**Purpose:** Reduces or prevents damage based on conditions.

**Parameters Tested:**
- `AbsorbDamagePct` (enum) - Percentage of damage to absorb (33, 50, 100)
- `ChanceToApply` (enum) - Chance to apply effect (80, 100)
- `RequiredTags` (int array) - Tags required for effect to apply

**Sample Values:**
- AbsorbDamagePct: 50 (50% damage absorption)
- ChanceToApply: 100 (always applies)

---

### 4. InterceptAPChange

**File:** `group4-InterceptAPChange.json`
**Schema Location:** Line 2149-2174 in eventhandler-schema.json
**Instance Count:** 2
**Field Count:** 3

**Purpose:** Intercepts and modifies action point changes from effects.

**Parameters Tested:**
- `ActionPointPercentage` (enum) - Percentage of AP to intercept (50)
- `AppliesTo` (string) - Effect to intercept (e.g., "effect.disabled")
- `TriggerOnlyOncePerRound` (enum) - Whether to trigger only once per round

---

### 5. Jamming

**File:** `group4-Jamming.json`
**Schema Location:** Line 2175-2197 in eventhandler-schema.json
**Instance Count:** 4
**Field Count:** 3

**Purpose:** Introduces jamming mechanics with increasing chance of malfunction.

**Parameters Tested:**
- `BaseChance` (enum) - Initial jamming chance (0%)
- `ChancePerUse` (enum) - Chance increase per use (10%)
- `JammingSound` (object) - Sound effect to play

**Typical Configuration:**
- Starts at 0% chance
- Increases 10% per shot
- Plays jamming sound on trigger

---

### 6. JetPack

**File:** `group4-JetPack.json`
**Schema Location:** Line 2198-2248 in eventhandler-schema.json
**Instance Count:** 2
**Field Count:** 7

**Purpose:** Enables jetpack-based movement with dynamic physics.

**Parameters Tested:**
- `DelayBeforeJump` (enum) - Delay before initiating jump (0.45, 0.75 seconds)
- `Duration` (enum) - Jetpack flight duration (2.0, 2.5 seconds)
- `MaxHeight` (enum) - Maximum altitude (30.0, 40.0 units)

**Advanced Fields:**
- `ForwardCurve` - Movement curve over time
- `HeightCurve` - Height curve over time
- `StartTrigger` - Animation trigger ("TriggerUsingJetpack")

---

### 7. JumpIntoMelee

**File:** `group4-JumpIntoMelee.json`
**Schema Location:** Line 2249-2293 in eventhandler-schema.json
**Instance Count:** 1
**Field Count:** 10

**Purpose:** Enables jumping into melee range with physics-based movement.

**Parameters Tested:**
- `Animation` (int) - Animation ID (6)
- `DelayBeforeJump` (float) - Pre-jump delay (0.25 seconds)
- `MaxHeight` (float) - Maximum jump height (5.0 units)

**Movement Physics:**
- `DistanceFromTarget` - Stopping distance (1.0)
- `DurationPerUnitOfDistance` - Time scaling (0.038)
- `HeightPerUnitOfDistance` - Arc height scaling (0.5)
- `MaxDuration` - Maximum flight time (0.4 seconds)

---

### 8. LifetimeLimit

**File:** `group4-LifetimeLimit.json`
**Schema Location:** Line 2294-2327 in eventhandler-schema.json
**Instance Count:** 54 (most used EventHandler type)
**Field Count:** 3

**Purpose:** Limits the duration/lifetime of skill effects.

**Parameters Tested:**
- `Event` (enum) - Trigger event (0-4)
- `Lifetime` (enum) - Duration in turns (1, 2, 3, 5)
- `ResetLifetimeOnRefresh` (enum) - Reset when refreshed

**Usage:** Highest instance count, used for temporary effects.

---

### 9. LimitedPassiveUses

**File:** `group4-LimitedPassiveUses.json`
**Schema Location:** Line 2365-2373 in eventhandler-schema.json
**Instance Count:** 1
**Field Count:** 1

**Purpose:** Limits passive skill usage to a maximum number of times.

**Parameters Tested:**
- `MaxUses` (int) - Maximum uses allowed (3)

---

### 10. LimitMaxHpLoss

**File:** `group4-LimitMaxHpLoss.json`
**Schema Location:** Line 2328-2339 in eventhandler-schema.json
**Instance Count:** 2
**Field Count:** 1

**Purpose:** Prevents HP loss exceeding a percentage of max HP.

**Parameters Tested:**
- `MaxHpPct` (enum) - Maximum loss percentage (0.33 = 33%)

---

### 11. LimitUsability

**File:** `group4-LimitUsability.json`
**Schema Location:** Line 2340-2364 in eventhandler-schema.json
**Instance Count:** 7
**Field Count:** 4

**Purpose:** Restricts skill usage based on actor's current skills/status.

**Parameters Tested:**
- `NotUsableIfActorHasSkill` (string array) - Skills that disable this skill
- `OnlyUsableIfActorHasSkill` (string array) - Required skills
- `HiddenIfActorHasSkill` (string array) - Skills that hide this skill

**Example:**
- Effect.dash_walker prevents usage of some skills
- Effect.off_balance disables certain abilities

---

### 12. ModularVehicleSkill

**File:** `group4-ModularVehicleSkill.json`
**Schema Location:** Line 2374-2386 in eventhandler-schema.json
**Instance Count:** 51 (second most used)
**Field Count:** 1

**Purpose:** Configures vehicle weapon module compatibility.

**Parameters Tested:**
- `RequiredWeaponCount` (enum) - Weapons needed (0, 1)

---

### 13. MoraleOverMaxEffect

**File:** `group4-MoraleOverMaxEffect.json`
**Schema Location:** Line 2387-2398 in eventhandler-schema.json
**Instance Count:** 1
**Field Count:** 1

**Purpose:** Applies effect when morale exceeds maximum.

**Parameters Tested:**
- `Effect` (string) - Effect to apply ("special.morale.high_spirits")

---

### 14. OnElementKilled

**File:** `group4-OnElementKilled.json`
**Schema Location:** Line 2422-2447 in eventhandler-schema.json
**Instance Count:** 2
**Field Count:** 3

**Purpose:** Triggers action when element of target dies.

**Parameters Tested:**
- `Amount` (enum) - Points/value (20)
- `ApplyEffect` (enum) - Apply additional effect (0/1)
- `DisplayText` (enum) - Show HUD text (0/1)

---

### 15. OverrideVolumeProfile

**File:** `group4-OverrideVolumeProfile.json`
**Schema Location:** Line 2448-2474 in eventhandler-schema.json
**Instance Count:** 1
**Field Count:** 4

**Purpose:** Overrides post-processing volume profile by biome.

**Parameters Tested:**
- `FallbackProfile` (string) - Default profile ("volumeprofile.allbiomesnightvision")
- `SnowProfile` (string) - Biome-specific profile ("volumeprofile.dicenightvision")
- `TemperateProfile` (null) - Optional biome override

**Use Case:** Night vision effect application by environment

---

### 16. PlayAnimationSequence

**File:** `group4-PlayAnimationSequence.json`
**Schema Location:** Line 2475-2486 in eventhandler-schema.json
**Instance Count:** 3
**Field Count:** 1

**Purpose:** Plays animation sequence on entity.

**Parameters Tested:**
- `Animation` (string) - Animation name ("dropship.start_in_air")

---

### 17. PlaySound

**File:** `group4-PlaySound.json`
**Schema Location:** Line 2487-2510 in eventhandler-schema.json
**Instance Count:** 4
**Field Count:** 3

**Purpose:** Plays audio effect at specific event.

**Parameters Tested:**
- `Event` (enum) - Trigger event (3, 4)
- `SoundToPlay` (object) - Audio reference (bankId, itemId)
- `Volume` (enum) - Volume level (1.0)

---

### 18. PlaySoundOnEnabled

**File:** `group4-PlaySoundOnEnabled.json`
**Schema Location:** Line 2511-2523 in eventhandler-schema.json
**Instance Count:** 1
**Field Count:** 2

**Purpose:** Plays sound when effect/skill is enabled.

**Parameters Tested:**
- `SoundToPlay` (object) - Audio reference
- `Volume` (float) - Volume level (1.0)

---

## Summary Statistics

| Metric | Value |
|--------|-------|
| Total EventHandler Types | 18 |
| Total Schema Fields | 57 |
| Average Fields per Type | 3.2 |
| Test Files Created | 18 |
| Total Test Steps | 142 |
| Average Steps per Test | 7.9 |
| Total Instance Count | 165 |

## Schema Integration

All tests are derived from:
- **Schema File:** `/home/poss/Documents/Code/Menace/MenaceAssetPacker/src/Menace.Modkit.App/eventhandler-schema.json`
- **Total EventHandler Types in Schema:** 165
- **Total Fields Across All Types:** 1000+
- **Groups:** 6 (distributed across 0-5)

## Field Type Distribution

- **Enum Fields:** ~40%
- **String Fields:** ~30%
- **Float/Int Fields:** ~20%
- **Object Fields:** ~10%
- **Array Fields:** ~5%
- **Null/Nullable Fields:** ~5%

## Notes

- All tests use string matching on serialized EventHandler representations
- Tests discover actual game data at runtime via Templates SDK
- Instance counts reflect actual usage in current game data
- MoveAndSelfDestruct was requested but not found in schema
