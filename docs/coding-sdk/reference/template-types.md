# Template Types Reference

**Last Updated:** 2026-03-04
**Source:** ExtractedData directory + Runtime verification
**Total Types:** 77

## Overview

This is the complete list of all template types that exist in the game. Use these **exact type names** when calling the Templates SDK API.

## Template Type Categories

### Items & Equipment (10 types)

| Type | Description | Example Instances |
|------|-------------|-------------------|
| `AccessoryTemplate` | Weapon accessories, attachments | accessory.ammo_armor_piercing (63 total) |
| `ArmorTemplate` | Armor pieces for characters | armor.shield (42 total) |
| `CommodityTemplate` | Trade goods, consumables | commodity.civilian_supplies (28 total) |
| `DossierItemTemplate` | Intel/dossier items | (4 total) |
| `ItemFilterTemplate` | Item category filters | (35 total) |
| `ItemTemplate` | Generic items | (236 total) |
| `SquaddieItemTemplate` | Squad recruitment items | (2 total) |
| `VehicleItemTemplate` | Vehicle-related items | (12 total) |
| `VoucherTemplate` | Special voucher items | (3 total) |
| `WeaponTemplate` | Weapons | weapon.sword (122 total) |

### Characters & Units (3 types)

| Type | Description | Example Instances |
|------|-------------|-------------------|
| `UnitLeaderTemplate` | **SquadLeaders/Characters** | pilot.achilleas, pilot.bog (18 total) |
| `UnitRankTemplate` | Unit rank definitions | (3 total) |
| `SquaddieItemTemplate` | Squad member recruitment | (2 total) |

**Note:** There is NO `CharacterTemplate` - use `UnitLeaderTemplate` for characters!

### Strategy Layer (16 types)

| Type | Description |
|------|-------------|
| `ArmyTemplate` | Enemy army compositions |
| `BiomeTemplate` | Biome/environment types |
| `ConversationEffectsTemplate` | Conversation outcome effects |
| `EmotionalStateTemplate` | Character emotional states |
| `EnemyAssetTemplate` | Enemy strategic assets |
| `FactionTemplate` | Faction definitions |
| `GlobalDifficultyTemplate` | Global difficulty settings |
| `LightConditionTemplate` | Lighting conditions |
| `MissionDifficultyTemplate` | Mission difficulty modifiers |
| `MissionPOITemplate` | Mission points of interest |
| `MissionPreviewConfigTemplate` | Mission preview UI config |
| `OperationDurationTemplate` | Operation duration tiers |
| `OperationIntrosTemplate` | Operation intro cinematics |
| `OperationTemplate` | Strategic operations |
| `PlanetTemplate` | Planets/regions |
| `StoryFactionTemplate` | Story-specific factions |
| `StrategicAssetTemplate` | Strategic layer assets |

**Note:** There is NO `RegionTemplate` - use `PlanetTemplate` for regions!

### Missions (1 type)

| Type | Description |
|------|-------------|
| `GenericMissionTemplate` | All mission types (1.2MB file - massive!) |

**Note:** There is NO separate `EncounterTemplate` - encounters are in `GenericMissionTemplate` or `EntityTemplate`

### Tactical/Combat (13 types)

| Type | Description | Example Count |
|------|-------------|---------------|
| `AIWeightsTemplate` | AI behavior weights | 3 |
| `AnimatorParameterNameTemplate` | Animation parameter names | 1 |
| `DefectTemplate` | Unit defect types | 12 |
| `ElementAnimatorTemplate` | UI element animations | 33 |
| `EntityTemplate` | **All tactical entities/units** | 259 |
| `HalfCoverTemplate` | Half-cover definitions | 2 |
| `InsideCoverTemplate` | Cover mechanics | 1 |
| `RagdollTemplate` | Ragdoll physics configs | 100 |
| `SkillTemplate` | **All skills/abilities** | 6.3MB file - huge! |
| `SkillUsesDisplayTemplate` | Skill use display config | 3 |
| `SurfaceTypeTemplate` | Surface type definitions | 5 |
| `TileEffectTemplate` | Tile effect definitions | (in binary) |
| `WeatherTemplate` | Weather conditions | 6 |
| `WindControlsTemplate` | Wind effect controls | 1 |

### Map Generation (2 types)

| Type | Description |
|------|-------------|
| `ChunkTemplate` | Map generation chunks (394KB - lots of data) |
| `EnvironmentFeatureTemplate` | Environment feature placement |

### Vehicles (2 types)

| Type | Description |
|------|-------------|
| `ModularVehicleTemplate` | Modular vehicle configurations |
| `ModularVehicleWeaponTemplate` | Vehicle weapon loadouts (84KB) |

### Perks & Upgrades (4 types)

| Type | Description | Example Count |
|------|-------------|---------------|
| `PerkTemplate` | Individual perks | 1.2MB file |
| `PerkTreeTemplate` | Perk tree structures | 17 |
| `ShipUpgradeSlotTemplate` | Ship upgrade slot definitions | 1 |
| `ShipUpgradeTemplate` | Ship upgrade options | 51 |

**Note:** There is NO `PerkNode` - use `PerkTemplate` for individual perks!

### Conversations & Story (3 types)

| Type | Description |
|------|-------------|
| `ConversationStageTemplate` | Conversation stages |
| `ConversationTemplate` | Dialogue trees (9.3MB - massive!) |
| `SpeakerTemplate` | Conversation speakers (91KB) |

### Rewards & Loot (2 types)

| Type | Description |
|------|-------------|
| `RewardTableTemplate` | Reward/loot tables (340KB) |
| `OffmapAbilityTemplate` | Offmap ability definitions |

### Player Settings (5 types)

| Type | Description |
|------|-------------|
| `BoolPlayerSettingTemplate` | Boolean settings |
| `DisplayIndexPlayerSettingTemplate` | Display index settings |
| `IntPlayerSettingTemplate` | Integer settings |
| `KeyBindPlayerSettingTemplate` | Key binding settings |
| `ListPlayerSettingTemplate` | List-based settings |
| `ResolutionPlayerSettingTemplate` | Resolution settings |

### Visuals & Audio (8 types)

| Type | Description |
|------|-------------|
| `AnimationSequenceTemplate` | Animation sequences |
| `AnimationSoundTemplate` | Animation sound effects |
| `PrefabListTemplate` | Prefab collections |
| `PropertyDisplayConfigTemplate` | UI property display config |
| `SurfaceDecalsTemplate` | Surface decal definitions |
| `SurfaceEffectsTemplate` | Surface visual effects |
| `SurfaceSoundsTemplate` | Surface sound effects |
| `VideoTemplate` | Video/cinematic assets (108KB) |

### Other (1 type)

| Type | Description |
|------|-------------|
| `TagTemplate` | Tag system definitions |

## Common Mistakes

### ❌ Wrong Names (Don't Use These)

| ❌ Incorrect | ✅ Correct |
|-------------|-----------|
| `CharacterTemplate` | `UnitLeaderTemplate` |
| `EquipmentTemplate` | `AccessoryTemplate`, `ArmorTemplate`, or `WeaponTemplate` |
| `ConsumableTemplate` | `CommodityTemplate` |
| `PerkTree` | `PerkTreeTemplate` |
| `PerkNode` | `PerkTemplate` |
| `SquadTemplate` | Does not exist (maybe `ArmyTemplate`?) |
| `RegionTemplate` | `PlanetTemplate` |
| `EncounterTemplate` | `EntityTemplate` or `GenericMissionTemplate` |

## Usage Examples

### Finding Templates

```csharp
// Characters
var characters = Templates.FindAll("UnitLeaderTemplate");
// Returns 18 SquadLeaders like pilot.achilleas, pilot.bog, etc.

// Armor
var armors = Templates.FindAll("ArmorTemplate");
// Returns 42 armor pieces

// Weapons
var weapons = Templates.FindAll("WeaponTemplate");
// Returns 122 weapons

// Entities (tactical units)
var entities = Templates.FindAll("EntityTemplate");
// Returns 259 tactical entities

// Accessories (weapon mods, etc.)
var accessories = Templates.FindAll("AccessoryTemplate");
// Returns 63 accessories

// Commodities (trade goods)
var commodities = Templates.FindAll("CommodityTemplate");
// Returns 28 commodities like civilian_supplies
```

### Finding Specific Instance

```csharp
var achilleas = Templates.Find("UnitLeaderTemplate", "pilot.achilleas");
var heavyArmor = Templates.Find("ArmorTemplate", "armor.heavy_plating");
var rifle = Templates.Find("WeaponTemplate", "weapon.rifle");
```

### Reading Properties

```csharp
// Get a character's max health
var maxHp = Templates.GetProperty<int>("UnitLeaderTemplate", "pilot.achilleas", "MaxHealth");

// Get weapon damage
var damage = Templates.GetProperty<int>("WeaponTemplate", "weapon.rifle", "Damage");

// Get armor defense
var defense = Templates.GetProperty<int>("ArmorTemplate", "armor.shield", "Defense");
```

## How Names Were Discovered

1. **Ghidra Analysis:** Decompiled `DataTemplateLoader.GetBaseFolder()` and searched for all `Template$$.ctor` constructors
2. **ExtractedData:** Verified against actual extracted JSON files in `UserData/ExtractedData/`
3. **Runtime Testing:** Confirmed via Templates.FindAll() in running game

## See Also

- [Templates API Reference](../api/templates.md) - SDK methods for working with templates
- [Template Modding Guide](../guides/template-modding.md) - How to modify templates in modpacks
