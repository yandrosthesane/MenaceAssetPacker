# Template Loading System

**Last Updated:** 2026-03-04
**Validation Status:** ✅ All 77 template types verified working

This document describes how the modpack system loads and patches game templates.

## Overview

The game uses a `DataTemplateLoader` system to load ScriptableObject templates from Resources folders. The modpack system patches these templates after they're loaded.

## Template Loading Process

1. **Game Initialization:** Game calls `DataTemplateLoader.GetAll<T>()` for each template type
2. **GetBaseFolder:** Returns the Resources path for that type (e.g., "Data/Weapons")
3. **LoadTemplates:** Loads all assets of type T from that path via `Resources.FindObjectsOfTypeAll()`
4. **Modpack Patching:** After templates load, ModpackLoader applies patches from modpack.json

## Template Types

**Total:** 77 template types exist in the game.

For the complete list with examples and usage, see:
**[Template Types Reference](../reference/template-types.md)**

### Most Common Types

| Category | Types | Example |
|----------|-------|---------|
| **Items** | WeaponTemplate, ArmorTemplate, AccessoryTemplate, CommodityTemplate, ItemTemplate | weapon.rifle, armor.shield |
| **Characters** | **UnitLeaderTemplate** (NOT CharacterTemplate!) | pilot.achilleas |
| **Tactical** | EntityTemplate (259 instances), SkillTemplate | entity.pirate_grunt |
| **Strategy** | ArmyTemplate, OperationTemplate, FactionTemplate, **PlanetTemplate** (NOT RegionTemplate!) | army.pirates |
| **Perks** | PerkTemplate, **PerkTreeTemplate** (NOT PerkTree or PerkNode!) | perk.weapon_mastery |

### ⚠️ Common Mistakes

These type names **do not exist** - use the correct names instead:

| ❌ Wrong | ✅ Correct |
|---------|-----------|
| CharacterTemplate | **UnitLeaderTemplate** |
| EquipmentTemplate | ArmorTemplate, WeaponTemplate, or AccessoryTemplate |
| ConsumableTemplate | **CommodityTemplate** |
| PerkTree | **PerkTreeTemplate** |
| PerkNode | **PerkTemplate** |
| SquadTemplate | Does not exist |
| RegionTemplate | **PlanetTemplate** |
| EncounterTemplate | EntityTemplate or GenericMissionTemplate |

## Diagnostic Tools

Use these console commands to test template loading:

### `debug.test_all_templates`

Tests **26 representative template types** (subset of the 77 total) and generates a comprehensive report:
- Shows which types load successfully
- Shows resource paths returned by GetBaseFolder()
- Shows sample template names
- Saves full report to `Logs/template_diagnostic.log`

Example output:
```
=== TEMPLATE LOADING DIAGNOSTIC REPORT ===
Test Time: 2026-03-04 10:30:00
Current Scene: MissionPreparation

SUMMARY: 26/26 types loaded successfully (0 failed)

=== SUCCESSFUL LOADS ===
✓ WeaponTemplate
    Path: Data/Weapons
    Count: 122
    Samples: weapon.rifle, weapon.pistol, weapon.shotgun

✓ UnitLeaderTemplate
    Path: Data/Characters
    Count: 18
    Samples: pilot.achilleas, pilot.bog, pilot.cao
```

### `debug.template_log`

Shows recent GetBaseFolder() and LoadTemplates() calls from Harmony patches.

### `debug.clear_template_log`

Clears the diagnostic log.

## Template Loading Fixes

**As of 2026-03-04:** All 77 template types load correctly with their proper names. No fixes are currently needed.

The previous issues were caused by using **incorrect type names** (like "CharacterTemplate" instead of "UnitLeaderTemplate").

### How Fixes Work

When template types have null/incorrect resource paths, `TemplateLoadingFixes` can patch `GetBaseFolder()` to return correct paths.

**File:** `src/Menace.ModpackLoader/TemplateLoading/TemplateLoadingFixes.cs`

```csharp
private static readonly Dictionary<string, string> KnownPathFixes = new()
{
    // Add entries here if you discover types with broken paths
    // via debug.test_all_templates

    // Currently empty - all types work!
};
```

## Scene Dependency

Templates load in different scenes based on when they're needed:

| Scene | Templates Available |
|-------|-------------------|
| **Title** | Basic types load |
| **MainMenu** | Most types available |
| **MissionPreparation** | All strategic types |
| **Tactical** | All tactical types (EntityTemplate, SkillTemplate, etc.) |

**Best practice:** Test template loading in MainMenu or later scenes.

## Template Discovery Process

How the 77 template types were discovered:

1. **Ghidra Decompilation:** Analyzed `DataTemplateLoader.GetBaseFolder()` binary code
2. **Constructor Search:** Found all `Template$$.ctor` constructors in game binary
3. **ExtractedData Verification:** Confirmed against actual JSON files in `UserData/ExtractedData/`
4. **Runtime Testing:** Verified via `Templates.FindAll()` in running game

Example verification:
```csharp
// Verified these work:
Templates.FindAll("UnitLeaderTemplate").Length  // Returns 18
Templates.FindAll("ArmorTemplate").Length       // Returns 42
Templates.FindAll("AccessoryTemplate").Length   // Returns 63
Templates.FindAll("EntityTemplate").Length      // Returns 259
```

## Usage in Modpacks

### Finding Templates

```csharp
// Use correct type names!
var characters = Templates.FindAll("UnitLeaderTemplate");
var armors = Templates.FindAll("ArmorTemplate");
var weapons = Templates.FindAll("WeaponTemplate");
```

### Patching Templates

In your `modpack.json`:

```json
{
  "patches": {
    "UnitLeaderTemplate": {
      "pilot.achilleas": {
        "MaxHealth": 200
      }
    },
    "WeaponTemplate": {
      "weapon.rifle": {
        "Damage": 50
      }
    }
  }
}
```

## Troubleshooting

### "No templates found" Error

**Cause:** You're using an incorrect type name.

**Solution:** Check the [Template Types Reference](../reference/template-types.md) for the correct name.

Common fixes:
- `CharacterTemplate` → `UnitLeaderTemplate`
- `PerkTree` → `PerkTreeTemplate`
- `RegionTemplate` → `PlanetTemplate`

### "Type not found in Assembly-CSharp" Error

**Cause:** The type name doesn't exist in the game binary.

**Solution:** Use `debug.test_all_templates` to see what types are actually available, or check the [Template Types Reference](../reference/template-types.md).

## See Also

- [Template Types Reference](../reference/template-types.md) - Complete list of all 77 types
- [Template Modding Guide](./template-modding.md) - How to modify templates
- [SDK Testing & Diagnostics](./testing-and-diagnostics.md) - Testing tools
