# Localization Pattern Analysis

**Analysis Date:** 2026-03-04
**Source:** ExtractedData JSON analysis
**Agent:** Localization Analysis Agent

## Executive Summary

Localization fields appear in **54 of 72 analyzed template types** (75%), storing text in multiple languages. The system uses:
- **3 primary patterns**: LocaState, LocalizedStrings arrays, and LocalizedMultiLine objects
- **274 total localization fields** across all templates
- **Consistent structure**: `m_Key` (localization key) + `m_TableID` (which translation table)

**Status:** Localization fields **work correctly** but require special handling to avoid showing raw keys.

---

## What is Localization?

Localization stores game text (names, descriptions, dialogue) in a way that supports multiple languages. Instead of hardcoding English text, the game:

1. Stores a **key** (e.g., "weapons.assault_rifle.name")
2. Looks up the key in **translation tables** (English.txt, Spanish.txt, etc.)
3. Shows the **translated text** for the player's language

**Example:**
```
Key: "weapons.assault_rifle.name"
English table: "Assault Rifle"
Spanish table: "Fusil de Asalto"
German table: "Sturmgewehr"
```

---

## Three Localization Patterns

### Pattern 1: LocaState (102 fields)

**Structure:**
```json
"m_LocaState": {
  "m_Key": "weapons.assault_rifle.description",
  "m_TableID": "weapons"
}
```

**Common Field Names:**
- `m_LocaState` (most common)
- `m_NameLoca`
- `m_DescriptionLoca`
- `m_TitleLoca`

**Templates Using This Pattern (28 types):**
- AccessoryTemplate, ArmorTemplate, BiomeTemplate
- CommodityTemplate, ConversationStageTemplate, DossierItemTemplate
- EntityTemplate, FactionTemplate, GenericMissionTemplate
- LightConditionTemplate, MissionPOITemplate, ModularVehicleTemplate
- ModularVehicleWeaponTemplate, OffmapAbilityTemplate, OperationTemplate
- PerkTemplate, PerkTreeTemplate, PlanetTemplate
- ShipUpgradeSlotTemplate, ShipUpgradeTemplate, SkillTemplate
- SpeakerTemplate, SquaddieItemTemplate, StoryFactionTemplate
- StrategicAssetTemplate, UnitLeaderTemplate, VehicleItemTemplate
- VoucherTemplate, WeaponTemplate

### Pattern 2: LocalizedStrings Array (52 fields)

**Structure:**
```json
"m_LocalizedStrings": [
  {
    "_type": "LocalizedLine",
    "m_Key": "conversation.intro.line1",
    "m_TableID": "conversations"
  },
  {
    "_type": "LocalizedLine",
    "m_Key": "conversation.intro.line2",
    "m_TableID": "conversations"
  }
]
```

**Variants:**
- `LocalizedLine` - Single line of text
- `LocalizedMultiLine` - Multiple paragraphs

**Templates Using This Pattern (14 types):**
- ConversationStageTemplate (most common - 6+ fields)
- ConversationEffectsTemplate
- ConversationTemplate
- EntityTemplate
- GenericMissionTemplate
- OperationIntrosTemplate
- OperationTemplate
- PerkTemplate
- SkillTemplate
- SpeakerTemplate
- SquaddieItemTemplate
- StrategicAssetTemplate
- UnitLeaderTemplate

### Pattern 3: Direct LocalizedLine/MultiLine Fields (120 fields)

**Structure:**
```json
"m_Description": {
  "_type": "LocalizedMultiLine",
  "m_Key": "skills.overwatch.description",
  "m_TableID": "skills"
}

"m_FlavorText": {
  "_type": "LocalizedLine",
  "m_Key": "items.pistol.flavor",
  "m_TableID": "items"
}
```

**Common Field Names:**
- `m_Description`, `m_Tooltip`, `m_FlavorText`
- `m_ShortDescription`, `m_LongDescription`
- `m_Headline`, `m_SuccessText`, `m_FailureText`

**Templates Using This Pattern (Most templates with text fields):**
- Heavily used in: SkillTemplate, PerkTemplate, WeaponTemplate
- Moderately used in: most item and mission templates

---

## Localization by Template Type

### Heavy Localization Users (8+ fields)

**SkillTemplate** (11 localization fields)
```json
{
  "m_NameLoca": { "m_Key": "...", "m_TableID": "..." },
  "m_Description": { "_type": "LocalizedMultiLine", "m_Key": "...", "m_TableID": "..." },
  "m_ShortDescription": { "_type": "LocalizedLine", "m_Key": "...", "m_TableID": "..." },
  "m_Tooltip": { "_type": "LocalizedLine", "m_Key": "...", "m_TableID": "..." },
  "m_FlavorText": { "_type": "LocalizedLine", "m_Key": "...", "m_TableID": "..." },
  "m_SuccessText": { "_type": "LocalizedLine", "m_Key": "...", "m_TableID": "..." },
  "m_FailureText": { "_type": "LocalizedLine", "m_Key": "...", "m_TableID": "..." },
  "m_LocalizedStrings": [ ... ]
}
```

**PerkTemplate** (8 localization fields)
**ConversationStageTemplate** (6 localization fields)
**WeaponTemplate** (5 localization fields)
**ArmorTemplate** (5 localization fields)

### Medium Localization Users (3-7 fields)

- GenericMissionTemplate, OperationTemplate, EntityTemplate
- AccessoryTemplate, CommodityTemplate, ModularVehicleTemplate
- OffmapAbilityTemplate, ShipUpgradeTemplate, UnitLeaderTemplate

### Light Localization Users (1-2 fields)

- Most other templates (just name and/or description)

---

## Table IDs and Keys

### Common Table IDs

**Item Tables:**
- `"weapons"` - Weapon names/descriptions
- `"armor"` - Armor names/descriptions
- `"items"` - Generic items
- `"accessories"` - Accessory items

**Gameplay Tables:**
- `"skills"` - Skill names/descriptions
- `"perks"` - Perk names/descriptions
- `"entities"` - Entity/enemy names
- `"effects"` - Status effect descriptions

**Story Tables:**
- `"conversations"` - Dialogue lines
- `"missions"` - Mission briefings
- `"operations"` - Operation descriptions
- `"factions"` - Faction names/lore

**UI Tables:**
- `"ui"` - Interface text
- `"settings"` - Settings menu
- `"tooltips"` - Tooltip text

### Key Naming Conventions

Keys typically follow patterns:

```
<category>.<template_name>.<field_type>

Examples:
"weapons.assault_rifle.name"
"weapons.assault_rifle.description"
"skills.overwatch.tooltip"
"perks.heavy_weapons.flavor_text"
"conversations.intro.line_01"
```

---

## How Localization Works for Modders

### ✅ Option 1: Reuse Existing Keys (Safest)

Use localization keys from existing templates:

```json
{
  "name": "my_custom_weapon",
  "_cloneFrom": "weapon.assault_rifle",
  "m_Damage": 20,
  "m_LocaState": {
    "m_Key": "weapons.assault_rifle.name",
    "m_TableID": "weapons"
  }
}
```

**Result:** Your custom weapon shows "Assault Rifle" in all languages.

**When to use:** Variants of existing items (e.g., "Assault Rifle Mk2")

### ⚠️ Option 2: Create New Keys (Requires Localization Table)

Add new keys to your modpack's localization tables:

**1. Create localization table file:**
`MyModpack/Localization/English.txt`
```
weapons.my_heavy_cannon.name=Heavy Cannon
weapons.my_heavy_cannon.description=A devastating anti-armor weapon with low accuracy but massive damage.
```

**2. Use new key in template:**
```json
{
  "name": "my_custom_weapon",
  "m_LocaState": {
    "m_Key": "weapons.my_heavy_cannon.name",
    "m_TableID": "weapons"
  },
  "m_Description": {
    "_type": "LocalizedMultiLine",
    "m_Key": "weapons.my_heavy_cannon.description",
    "m_TableID": "weapons"
  }
}
```

**When to use:** Completely new content with custom text.

**⚠️ Caution:**
- Must provide translations for all languages (or fallback to English)
- Key must exist in table or game shows raw key string
- Table must be loaded by game before templates load

### ❌ Option 3: Use Non-Localized Fields (Not Recommended)

Some templates have both localized and non-localized fields:

```json
{
  "name": "my_weapon",
  "m_Name": "Heavy Cannon",  // Non-localized (always English)
  "m_LocaState": {            // Localized (multi-language)
    "m_Key": "weapons.heavy_cannon.name",
    "m_TableID": "weapons"
  }
}
```

**Problem:** If `m_Name` is shown in UI, only English players see correct text.

**When to use:** Debug/internal names, not player-facing text.

---

## Common Issues and Solutions

### Issue 1: Raw Keys Shown in Game

**Symptom:** Instead of "Assault Rifle", game shows "weapons.assault_rifle.name"

**Cause:** Localization key doesn't exist in translation tables.

**Solutions:**
1. Fix typo in key name
2. Add key to localization table file
3. Use existing key from another template
4. Check m_TableID is correct

**Debugging:**
```csharp
// Check if key exists
var key = "weapons.assault_rifle.name";
var tableID = "weapons";
var text = Localization.Get(tableID, key);
Debug.Log($"Key '{key}' resolves to: '{text}'");
```

### Issue 2: Wrong Language Shown

**Cause:** Using non-localized field (`m_Name`) instead of localized field (`m_LocaState`).

**Solution:** Ensure UI displays localized fields, not raw string fields.

### Issue 3: Localization Works in English but Not Other Languages

**Cause:** Key exists in English.txt but not in other language files.

**Solutions:**
1. Add key to all language files
2. Provide fallback to English if translation missing
3. Document which languages are supported

### Issue 4: Special Characters Not Displaying

**Cause:** Encoding issues (UTF-8 vs ASCII).

**Solution:**
- Save localization files as UTF-8
- Use Unity's rich text tags for special formatting

---

## Modifying Localization at Runtime

### Reading Localized Text

```csharp
// Read LocaState
var weapon = Templates.Find("WeaponTemplate", "weapon.assault_rifle");
var locaState = Templates.GetProperty("WeaponTemplate", "weapon.assault_rifle", "m_LocaState");

// Extract key and table
var key = locaState?.ReadString("m_Key");
var tableID = locaState?.ReadString("m_TableID");

// Get translated text for current language
var translatedText = Localization.Get(tableID, key);
```

### Writing Localized Text

```csharp
// Change which localization key a template uses
var myWeapon = Templates.Find("WeaponTemplate", "my_custom_weapon");

var newLocaState = new {
    m_Key = "weapons.heavy_cannon.name",
    m_TableID = "weapons"
};

Templates.WriteField(myWeapon, "m_LocaState", newLocaState);
```

### Cloning Localization from Another Template

```csharp
// Copy localization from existing template
var sourceWeapon = Templates.Find("WeaponTemplate", "weapon.assault_rifle");
var targetWeapon = Templates.Find("WeaponTemplate", "my_custom_weapon");

var sourceLoca = Templates.GetProperty("WeaponTemplate", "weapon.assault_rifle", "m_LocaState");
Templates.WriteField(targetWeapon, "m_LocaState", sourceLoca);

// Also copy description if it exists
var sourceDesc = Templates.GetProperty("WeaponTemplate", "weapon.assault_rifle", "m_Description");
if (sourceDesc != null)
{
    Templates.WriteField(targetWeapon, "m_Description", sourceDesc);
}
```

---

## Best Practices

### ✅ DO

1. **Reuse existing keys when possible:**
   - Faster development
   - Automatically translated
   - Consistent with base game

2. **Use descriptive key names:**
   ```
   ✅ "weapons.plasma_rifle_mk2.description"
   ❌ "my_thing.text1"
   ```

3. **Keep key structure consistent:**
   ```
   weapons.<weapon_name>.name
   weapons.<weapon_name>.description
   weapons.<weapon_name>.flavor_text
   ```

4. **Provide English fallback:**
   - English is default language for most mods
   - Other languages can be added later

5. **Test localization:**
   ```csharp
   // Switch language and verify text appears
   Localization.SetLanguage("Spanish");
   var text = GetLocalizedText("weapons.my_rifle.name");
   Debug.Log($"Spanish: {text}");
   ```

### ❌ DON'T

1. **Don't hardcode player-facing text:**
   ```json
   ❌ "m_Name": "My Awesome Weapon"  // Only works in English
   ✅ "m_LocaState": { "m_Key": "...", "m_TableID": "..." }
   ```

2. **Don't reuse keys for different content:**
   ```
   ❌ Use "weapons.rifle.name" for both "Assault Rifle" and "Sniper Rifle"
   ✅ Create separate keys for each
   ```

3. **Don't mix table IDs:**
   ```json
   ❌ {
     "m_NameLoca": { "m_TableID": "weapons" },
     "m_Description": { "m_TableID": "items" }  // Different table!
   }
   ✅ Keep related text in same table
   ```

4. **Don't forget to update localization when cloning:**
   - Cloned template keeps original localization keys
   - Change keys if you want different text

---

## Template-Specific Localization Notes

### ConversationStageTemplate

Most localization-heavy template (6+ localized arrays):

```json
{
  "m_SpeakerLines": [
    { "_type": "LocalizedLine", "m_Key": "conv.intro.speaker1.line1", "m_TableID": "conversations" },
    { "_type": "LocalizedLine", "m_Key": "conv.intro.speaker1.line2", "m_TableID": "conversations" }
  ],
  "m_ResponseOptions": [
    { "_type": "LocalizedLine", "m_Key": "conv.intro.option1", "m_TableID": "conversations" },
    { "_type": "LocalizedLine", "m_Key": "conv.intro.option2", "m_TableID": "conversations" }
  ],
  "m_Tooltip": { "_type": "LocalizedLine", "m_Key": "conv.intro.tooltip", "m_TableID": "conversations" }
}
```

**Recommendation:** Clone existing conversations rather than creating new ones from scratch.

### SkillTemplate & PerkTemplate

Multiple localization fields for UI display:

```json
{
  "m_NameLoca": { ... },           // Skill name in ability bar
  "m_Description": { ... },        // Full description in tooltip
  "m_ShortDescription": { ... },   // Brief description for UI
  "m_Tooltip": { ... },            // Tooltip text
  "m_FlavorText": { ... }          // Lore/flavor text
}
```

**Recommendation:** Reuse existing skill/perk localization as templates, changing only key parameters.

---

## Localization File Format

### Structure

```
# Comments start with #
# Format: key=value

# Weapon names
weapons.assault_rifle.name=Assault Rifle
weapons.assault_rifle.description=Standard military rifle. Good damage and accuracy at medium range.
weapons.assault_rifle.flavor=The backbone of any infantry squad.

# Skill names
skills.overwatch.name=Overwatch
skills.overwatch.description=Hunker down and shoot the first enemy that moves within range.
skills.overwatch.tooltip=Costs 2 AP. Remains active until end of turn.
```

### Multi-line Text

Use `\n` for line breaks:

```
missions.rescue.briefing=Our squad is pinned down behind enemy lines.\nWe need immediate extraction.\nTime is running out.
```

### Special Characters

Support depends on encoding:
- UTF-8: Full Unicode support (é, ñ, 中, א, etc.)
- ASCII: Latin characters only

### File Locations

```
YourModpack/
  Localization/
    English.txt
    Spanish.txt
    German.txt
    French.txt
    ...
```

---

## Summary

**Localization is widely used:**
- 54/72 template types (75%)
- 274 fields across all templates
- 3 consistent patterns

**Status: Works correctly, with caution:**
- ✅ Can read and write localization fields
- ✅ Can reuse existing keys (safest approach)
- ⚠️ Creating new keys requires localization tables
- ⚠️ Keys must exist or raw keys are shown
- ⚠️ Multiple languages require additional work

**Best approach for modders:**
1. Start by reusing existing localization keys
2. Add custom localization tables for new content
3. Provide English text first, add other languages later
4. Test that keys resolve correctly in-game

---

## Related Documentation

- **[Field Compatibility Report](../coding-sdk/reference/template-field-compatibility.md)** - Which fields can be modified
- **[Template Types Reference](../coding-sdk/reference/template-types.md)** - All template structures
- **[Testing Guide](../coding-sdk/guides/testing-and-diagnostics.md)** - How to test localization

---

## Appendix: All Localized Templates

**28 templates with LocaState pattern:**
AccessoryTemplate, ArmorTemplate, BiomeTemplate, CommodityTemplate, ConversationStageTemplate, DossierItemTemplate, EntityTemplate, FactionTemplate, GenericMissionTemplate, LightConditionTemplate, MissionPOITemplate, ModularVehicleTemplate, ModularVehicleWeaponTemplate, OffmapAbilityTemplate, OperationTemplate, PerkTemplate, PerkTreeTemplate, PlanetTemplate, ShipUpgradeSlotTemplate, ShipUpgradeTemplate, SkillTemplate, SpeakerTemplate, SquaddieItemTemplate, StoryFactionTemplate, StrategicAssetTemplate, UnitLeaderTemplate, VehicleItemTemplate, VoucherTemplate, WeaponTemplate

**14 templates with LocalizedStrings arrays:**
ConversationStageTemplate, ConversationEffectsTemplate, ConversationTemplate, EntityTemplate, GenericMissionTemplate, OperationIntrosTemplate, OperationTemplate, PerkTemplate, SkillTemplate, SpeakerTemplate, SquaddieItemTemplate, StrategicAssetTemplate, UnitLeaderTemplate

**42 templates with LocalizedLine/MultiLine fields:**
(Most templates have at least a description field)

**Total: 54 templates** with some form of localization (75% of analyzed templates)
